using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>الحقول المهيكلة كما تصل من منصة الكتابة (docs/PIPELINE-PROMPT-v2.md). كل حقل اختياري: null = لا تغيير.</summary>
    public class FacetDto
    {
        public string? FundingType { get; set; }
        public List<string>? DegreeLevels { get; set; }
        public List<string>? Fields { get; set; }
        public List<string>? GreenThemes { get; set; }
        /// <summary>رموز ISO-2، أو XA = كل الوطن العربي، XW = كل العالم.</summary>
        public List<string>? EligibleCountries { get; set; }
        public string? HostCountry { get; set; }
        public bool? IsRemote { get; set; }
        public bool? IsOnline { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string? LanguageRequirement { get; set; }
        public string? LanguageScore { get; set; }
        public bool? HasApplicationFee { get; set; }
        public bool? ForWomenOnly { get; set; }
        public string? StartDate { get; set; }
        public string? Venue { get; set; }
        public HostOrganizationDto? HostOrganization { get; set; }
        public string? EmploymentType { get; set; }
        public string? ProgramKey { get; set; }
        public Dictionary<string, List<string>>? BenefitsSummary { get; set; }
        /// <summary>رقم طلب النشر من بوابة المنظمات (المرحلة 1.2) — يُقبل ويُتجاهل حتى حينها.</summary>
        public int? RequestId { get; set; }

        public class HostOrganizationDto
        {
            public string? Name { get; set; }
            public string? Url { get; set; }
            public string? LogoUrl { get; set; }
        }
    }

    /// <summary>
    /// يحوّل FacetDto إلى حقول الفرصة بتحقق صارم: قيمة غير معروفة = خطأ باسم الحقل (ترده الواجهة 400)،
    /// بدل تخمين صامت يفسد المطابقة. الحقول غير المرسلة لا تُلمس.
    /// </summary>
    public static class FacetParser
    {
        public const string ArabWorldCode = "XA";
        public const string WorldCode = "XW";
        public const int ArabWorldCountryId = 2;
        public const int WorldCountryId = 1;
        public const int MaxSummaryItems = 6;
        public const int MaxSummaryItemLength = 80;

        private static readonly Regex ProgramKeyPattern = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
        private static readonly Nager.Country.CountryProvider Countries = new();

        public static readonly string[] FundingNames = Enum.GetNames<FundingType>();
        public static readonly string[] DegreeNames = { "HighSchool", "Bachelor", "Master", "PhD", "Postdoc", "Professional", "Any" };
        public static readonly string[] ThemeNames = Enum.GetNames<GreenThemes>();
        public static readonly string[] LanguageNames = { "Unknown", "None", "IELTS", "TOEFL", "Either", "French", "Other" };
        public static readonly string[] EmploymentNames = { "FULL_TIME", "PART_TIME", "CONTRACTOR", "TEMPORARY", "INTERN", "VOLUNTEER" };

        public sealed record Error(string Code, object? Allowed = null);

        /// <summary>يطبّق الحقول المرسلة على الفرصة. null = نجاح؛ وإلا خطأ واحد (الأول).</summary>
        public static Error? Apply(Opportunity o, FacetDto f, IReadOnlyCollection<Country> countries, IReadOnlyCollection<Field> fields)
        {
            if (f.FundingType != null)
            {
                if (!Enum.TryParse<FundingType>(f.FundingType.Trim(), true, out var ft)) return new("bad_funding", FundingNames);
                o.FundingType = ft;
            }

            if (f.DegreeLevels != null)
            {
                var levels = DegreeLevels.None;
                foreach (var raw in f.DegreeLevels)
                {
                    var name = (raw ?? "").Trim();
                    if (name.Equals("Any", StringComparison.OrdinalIgnoreCase)) { levels = DegreeLevels.Any; continue; }
                    if (!Enum.TryParse<DegreeLevels>(name, true, out var lv) || lv == DegreeLevels.None || lv == DegreeLevels.Any)
                        return new("bad_degree", DegreeNames);
                    levels |= lv;
                }
                o.DegreeLevels = levels;
            }

            if (f.GreenThemes != null)
            {
                var themes = GreenThemes.None;
                foreach (var raw in f.GreenThemes)
                {
                    var name = (raw ?? "").Trim();
                    if (!Enum.TryParse<GreenThemes>(name, true, out var th)) return new("bad_theme", ThemeNames);
                    themes |= th;
                }
                o.GreenThemes = themes;
            }

            if (f.Fields != null)
            {
                var chosen = new List<Field>();
                foreach (var raw in f.Fields)
                {
                    var key = (raw ?? "").Trim().ToLowerInvariant();
                    var match = fields.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (match == null) return new("bad_field", fields.Select(x => x.Key).ToArray());
                    if (!chosen.Contains(match)) chosen.Add(match);
                }
                o.Fields = chosen;
            }

            if (f.EligibleCountries != null)
            {
                if (f.EligibleCountries.Count == 0) return new("bad_country", "eligibleCountries must not be empty");
                var available = new List<Country>();
                var others = new List<string>();
                foreach (var raw in f.EligibleCountries)
                {
                    var code = NormalizeCode(raw);
                    if (code == null) return new("bad_country", raw);
                    if (code == ArabWorldCode) { AddOnce(available, countries.FirstOrDefault(c => c.Id == ArabWorldCountryId)); continue; }
                    if (code == WorldCode) { AddOnce(available, countries.FirstOrDefault(c => c.Id == WorldCountryId)); continue; }
                    if (!IsRealIso(code)) return new("bad_country", raw);
                    var arab = countries.FirstOrDefault(c => c.IsoCode == code);
                    if (arab != null) AddOnce(available, arab);
                    else if (!others.Contains(code)) others.Add(code);
                }
                o.AvailableCountries = available;
                o.EligibleCountriesIso = others.Count == 0 ? null : string.Join(",", others);
            }

            if (f.HostCountry != null)
            {
                if (string.IsNullOrWhiteSpace(f.HostCountry)) o.HostCountryIso = null;
                else
                {
                    var code = NormalizeCode(f.HostCountry);
                    if (code == null || !IsRealIso(code)) return new("bad_host_country", f.HostCountry);
                    o.HostCountryIso = code;
                }
            }

            if (f.IsRemote.HasValue) o.IsRemote = f.IsRemote.Value;
            if (f.IsOnline.HasValue) o.IsOnline = f.IsOnline.Value;

            if (f.MinAge.HasValue || f.MaxAge.HasValue)
            {
                var min = f.MinAge ?? o.MinAge;
                var max = f.MaxAge ?? o.MaxAge;
                if ((min.HasValue && (min < 10 || min > 99)) || (max.HasValue && (max < 10 || max > 99)) || (min.HasValue && max.HasValue && min > max))
                    return new("bad_age", "10..99 and minAge <= maxAge");
                o.MinAge = min;
                o.MaxAge = max;
            }

            if (f.LanguageRequirement != null)
            {
                var name = f.LanguageRequirement.Trim();
                // Unknown = المصدر لم يذكر اللغة (وهي الافتراضي عند حذف الحقل)؛ None = قال صراحةً إنها غير مطلوبة.
                if (!Enum.TryParse<LanguageRequirement>(name, true, out var lr))
                    return new("bad_language", LanguageNames);
                o.LanguageRequirement = lr;
            }
            if (f.LanguageScore != null) o.LanguageScore = Trim(f.LanguageScore, 100);

            if (f.HasApplicationFee.HasValue) o.HasApplicationFee = f.HasApplicationFee.Value;
            if (f.ForWomenOnly.HasValue) o.ForWomenOnly = f.ForWomenOnly.Value;

            if (f.StartDate != null)
            {
                if (string.IsNullOrWhiteSpace(f.StartDate)) o.StartDate = null;
                else if (DateTime.TryParseExact(f.StartDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sd)) o.StartDate = sd;
                else return new("bad_start_date", "yyyy-MM-dd");
            }
            if (f.Venue != null) o.Venue = Trim(f.Venue, 200);

            if (f.HostOrganization != null)
            {
                var h = f.HostOrganization;
                var name = Trim(h.Name, 200);
                if (name == null) return new("bad_host_organization", "name is required");
                string? url = null, logo = null;
                if (!string.IsNullOrWhiteSpace(h.Url)) { url = HttpUrl.Normalize(h.Url); if (url == null) return new("bad_host_url", h.Url); }
                if (!string.IsNullOrWhiteSpace(h.LogoUrl)) { logo = HttpUrl.Normalize(h.LogoUrl); if (logo == null) return new("bad_host_logo", h.LogoUrl); }
                o.HostOrganizationName = name;
                o.HostOrganizationUrl = url;
                o.HostOrganizationLogoUrl = logo;
            }

            if (f.EmploymentType != null)
            {
                if (string.IsNullOrWhiteSpace(f.EmploymentType)) o.EmploymentType = EmploymentType.None;
                else
                {
                    var et = ParseEmployment(f.EmploymentType);
                    if (et == null) return new("bad_employment", EmploymentNames);
                    o.EmploymentType = et.Value;
                }
            }

            if (f.ProgramKey != null)
            {
                var key = f.ProgramKey.Trim().ToLowerInvariant();
                if (key.Length == 0) o.ProgramKey = null;
                else if (key.Length > 60 || !ProgramKeyPattern.IsMatch(key)) return new("bad_program_key", "lowercase letters, digits and dashes, e.g. daad-epos");
                else o.ProgramKey = key;
            }

            if (f.BenefitsSummary != null)
            {
                var clean = new Dictionary<string, List<string>>();
                foreach (var lang in new[] { "ar", "en", "fr" })
                {
                    if (!f.BenefitsSummary.TryGetValue(lang, out var items) || items == null) continue;
                    var list = items.Select(x => Trim(x, MaxSummaryItemLength)).Where(x => x != null).Select(x => x!).Take(MaxSummaryItems).ToList();
                    if (list.Count > 0) clean[lang] = list;
                }
                o.BenefitsSummaryJson = clean.Count == 0 ? null
                    : JsonSerializer.Serialize(clean, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            }

            return null;
        }

        public static EmploymentType? ParseEmployment(string raw) => raw.Trim().ToUpperInvariant().Replace('-', '_') switch
        {
            "FULL_TIME" or "FULLTIME" => Models.EmploymentType.FullTime,
            "PART_TIME" or "PARTTIME" => Models.EmploymentType.PartTime,
            "CONTRACTOR" => Models.EmploymentType.Contractor,
            "TEMPORARY" => Models.EmploymentType.Temporary,
            "INTERN" => Models.EmploymentType.Intern,
            "VOLUNTEER" => Models.EmploymentType.Volunteer,
            _ => null
        };

        /// <summary>قيمة Google JobPosting المقابلة (FULL_TIME ...) أو null.</summary>
        public static string? ToJobPostingValue(EmploymentType t) => t switch
        {
            Models.EmploymentType.FullTime => "FULL_TIME",
            Models.EmploymentType.PartTime => "PART_TIME",
            Models.EmploymentType.Contractor => "CONTRACTOR",
            Models.EmploymentType.Temporary => "TEMPORARY",
            Models.EmploymentType.Intern => "INTERN",
            Models.EmploymentType.Volunteer => "VOLUNTEER",
            _ => null
        };

        /// <summary>رموز الدول المؤهَّلة كما تُعرض/تُصدَّر: XA و XW والرموز العربية وغيرها.</summary>
        public static IEnumerable<string> EligibleCodes(Opportunity o)
        {
            foreach (var c in o.AvailableCountries ?? Enumerable.Empty<Country>())
            {
                if (c.Id == WorldCountryId) yield return WorldCode;
                else if (c.Id == ArabWorldCountryId) yield return ArabWorldCode;
                else if (!string.IsNullOrEmpty(c.IsoCode)) yield return c.IsoCode;
            }
            if (!string.IsNullOrEmpty(o.EligibleCountriesIso))
                foreach (var code in o.EligibleCountriesIso.Split(',', StringSplitOptions.RemoveEmptyEntries)) yield return code;
        }

        public static string? NormalizeCode(string? raw)
        {
            var code = (raw ?? "").Trim().ToUpperInvariant();
            return code.Length == 2 && code.All(ch => ch >= 'A' && ch <= 'Z') ? code : null;
        }

        public static bool IsRealIso(string code)
        {
            try { return Countries.GetCountry(code) != null; }
            catch { return false; }
        }

        private static string? Trim(string? v, int max)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            v = v.Trim();
            return v.Length > max ? v[..max] : v;
        }

        private static void AddOnce(List<Country> list, Country? c)
        {
            if (c != null && !list.Contains(c)) list.Add(c);
        }
    }
}
