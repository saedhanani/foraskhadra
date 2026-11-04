using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Helpers
{
    /// <summary>معاملات قوائم الفرص (كل الفرص، صفحات الأنواع، صفحات النية) كما تأتي في الاستعلام — المرحلة 2.2.</summary>
    public class OpportunityQuery
    {
        public string? Search { get; set; }
        /// <summary>أسماء OpportunityType (المعامل القديم "type" يتكرر).</summary>
        public List<string> Type { get; set; } = new();
        /// <summary>"دولتي": رمز ISO-2 أو XA، أو اسم دولة كما في الروابط القديمة.</summary>
        public string? Country { get; set; }
        public string? Funding { get; set; }
        public string? Degree { get; set; }
        public List<string> Field { get; set; } = new();
        public string? Theme { get; set; }
        /// <summary>بلد الاستضافة ISO-2.</summary>
        public string? Host { get; set; }
        public bool Remote { get; set; }
        public bool NoIelts { get; set; }
        /// <summary>ينتهي خلال N يومًا: 7 أو 30 أو 90.</summary>
        public int? Within { get; set; }
        /// <summary>open (الافتراضي: المفتوحة أولًا ثم الأحدث) أو newest أو deadline.</summary>
        public string? Sort { get; set; }
        public int Page { get; set; } = 1;
    }

    /// <summary>القيم المفهومة بعد التحقق — ما لا يُفهم يُهمل بصمت.</summary>
    public sealed class ParsedFilter
    {
        public string Search { get; set; } = string.Empty;
        public List<OpportunityType> Types { get; set; } = new();
        public string? CountryIso { get; set; }
        /// <summary>اسم دولة لم يُحلّ إلى رمز (روابط قديمة) — مطابقة بالاسم كما كان.</summary>
        public string? CountryName { get; set; }
        public FundingType? Funding { get; set; }
        public DegreeLevels? Degree { get; set; }
        public List<string> Fields { get; set; } = new();
        public GreenThemes? Theme { get; set; }
        public string? Host { get; set; }
        public bool Remote { get; set; }
        public bool NoIelts { get; set; }
        public int? Within { get; set; }
        public string Sort { get; set; } = "open";

        /// <summary>أي معيار غير النوع (يجعل الصفحة noindex).</summary>
        public bool HasCriteriaBeyondType =>
            Search.Length > 0 || CountryIso != null || CountryName != null || Funding != null || Degree != null
            || Fields.Count > 0 || Theme != null || Host != null || Remote || NoIelts || Within != null || Sort != "open";

        public bool HasCriteria => Types.Count > 0 || HasCriteriaBeyondType;

        /// <summary>نوع واحد ولا شيء آخر: صفحته الخاصة /opportunities/{slug} هي القانونية.</summary>
        public bool IsSingleTypeOnly => Types.Count == 1 && !HasCriteriaBeyondType;
    }

    public static class OpportunityFilter
    {
        public static readonly int[] WithinOptions = { 7, 30, 90 };
        public static readonly string[] SortOptions = { "open", "newest", "deadline" };

        public static ParsedFilter Parse(OpportunityQuery q, IReadOnlyCollection<Country> countries)
        {
            var p = new ParsedFilter { Search = (q.Search ?? string.Empty).Trim() };
            if (p.Search.Length > 100) p.Search = p.Search[..100];
            p.Types = q.Type
                .Select(t => Enum.TryParse<OpportunityType>(t?.Trim(), true, out var v) ? v : (OpportunityType?)null)
                .Where(v => v != null).Select(v => v!.Value).Distinct().ToList();
            if (Enum.TryParse<FundingType>(q.Funding?.Trim(), true, out var funding) && funding != FundingType.Unknown) p.Funding = funding;
            p.Degree = CompassMatcher.ParseDegree(q.Degree);
            p.Fields = q.Field.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim().ToLowerInvariant()).Distinct().Take(5).ToList();
            if (Enum.TryParse<GreenThemes>(q.Theme?.Trim(), true, out var theme) && theme != GreenThemes.None) p.Theme = theme;
            p.Host = FacetParser.NormalizeCode(q.Host);
            p.Remote = q.Remote;
            p.NoIelts = q.NoIelts;
            p.Within = q.Within is 7 or 30 or 90 ? q.Within : null;
            p.Sort = SortOptions.Contains(q.Sort ?? string.Empty) ? q.Sort! : "open";

            if (!string.IsNullOrWhiteSpace(q.Country))
            {
                var raw = q.Country.Trim();
                var iso = FacetParser.NormalizeCode(raw);
                // رمز ISO صحيح حتى لو لم يكن في جدول الدول (غير العربية تأتي في EligibleCountriesIso فقط)
                if (iso != null && (iso == FacetParser.ArabWorldCode || iso == FacetParser.WorldCode
                                    || countries.Any(c => string.Equals(c.IsoCode, iso, StringComparison.OrdinalIgnoreCase)) || FacetParser.IsRealIso(iso)))
                    p.CountryIso = iso;
                else
                {
                    var resolved = MatchScorer.ResolveCountry(raw, countries);
                    if (resolved?.IsoCode != null) p.CountryIso = resolved.IsoCode.ToUpperInvariant();
                    else p.CountryName = raw.Length > 80 ? raw[..80] : raw;
                }
            }
            return p;
        }

        /// <summary>يطبّق كل المعايير على استعلام الفرص (المنشورة فقط) — الترتيب في Sort.</summary>
        public static IQueryable<Opportunity> Apply(IQueryable<Opportunity> source, ParsedFilter p, DateTime today)
        {
            today = today.Date;
            var q = source.Where(o => o.Status == OpportunityStatus.Published);

            // بحث نصي كامل: كل كلمة (مطبّعة) يجب أن ترد في نص الفرصة المطبّع — انظر Helpers/SearchIndexer
            foreach (var term in SearchIndexer.Terms(p.Search))
            {
                var pattern = $"%{term}%";
                q = q.Where(o => EF.Functions.Like(o.SearchText, pattern));
            }

            if (p.Types.Count > 0)
            {
                var types = p.Types;
                q = q.Where(o => o.Type.HasValue && types.Contains(o.Type.Value));
            }

            // دولتي: XW تقبل الجميع، XA تقبل كل عربي، وإلا رمز محدد في الدول أو في EligibleCountriesIso
            if (p.CountryIso != null)
            {
                var iso = p.CountryIso;
                if (iso == FacetParser.WorldCode)
                    q = q.Where(o => o.AvailableCountries.Any(c => c.Id == FacetParser.WorldCountryId));
                else if (iso == FacetParser.ArabWorldCode)
                    q = q.Where(o => o.AvailableCountries.Any(c => c.Id == FacetParser.WorldCountryId || c.Id == FacetParser.ArabWorldCountryId));
                else
                {
                    var isArab = CompassMatcher.ArabIso.Contains(iso);
                    var padded = "," + iso + ",";
                    q = q.Where(o => o.AvailableCountries.Any(c => c.Id == FacetParser.WorldCountryId || (isArab && c.Id == FacetParser.ArabWorldCountryId) || c.IsoCode == iso)
                                     || (o.EligibleCountriesIso != null && ("," + o.EligibleCountriesIso + ",").Contains(padded)));
                }
            }
            else if (p.CountryName != null)
            {
                var name = p.CountryName;
                q = q.Where(o => o.AvailableCountries.Any(c => c.NameEn == name || c.NameAr == name || c.NameFr == name));
            }

            if (p.Funding != null) { var f = p.Funding.Value; q = q.Where(o => o.FundingType == f); }
            if (p.Degree != null) { var d = p.Degree.Value; q = q.Where(o => (o.DegreeLevels & d) == d); }
            if (p.Fields.Count > 0) { var keys = p.Fields; q = q.Where(o => o.Fields.Any(f => keys.Contains(f.Key))); }
            if (p.Theme != null) { var t = p.Theme.Value; q = q.Where(o => (o.GreenThemes & t) == t); }
            if (p.Host != null) { var h = p.Host; q = q.Where(o => o.HostCountryIso == h); }
            if (p.Remote) q = q.Where(o => o.IsRemote || o.IsOnline);
            if (p.NoIelts) q = q.Where(o => o.LanguageRequirement == LanguageRequirement.None); // None فقط، لا Unknown
            if (p.Within != null)
            {
                var until = today.AddDays(p.Within.Value);
                q = q.Where(o => o.DeadlineType == DeadlineType.SpecificDate && o.EndDate != null && o.EndDate >= today && o.EndDate <= until);
            }
            return q;
        }

        /// <summary>open: المفتوحة أولًا ثم الأحدث؛ deadline: الأقرب موعدًا (المفتوحة أولًا، بلا موعد بعدها، المنتهية آخرًا)؛ newest.</summary>
        public static IQueryable<Opportunity> Sort(IQueryable<Opportunity> q, string sort, DateTime today)
        {
            today = today.Date;
            return sort switch
            {
                "newest" => q.OrderByDescending(o => o.PublishDate),
                "deadline" => q.OrderByDescending(o => o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today)
                               .ThenBy(o => o.EndDate == null ? 1 : 0)
                               .ThenBy(o => o.EndDate)
                               .ThenByDescending(o => o.PublishDate),
                _ => q.OrderByDescending(o => o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today)
                      .ThenByDescending(o => o.PublishDate),
            };
        }
    }
}
