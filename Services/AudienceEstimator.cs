using System.Text.Json;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Foras_Khadra.Services
{
    /// <summary>أرقام الموقع المعروضة للمنظمات: الأعضاء، الفرص المفتوحة، دول الأعضاء.</summary>
    public sealed record SiteNumbers(int Members, int OpenOpportunities, int MemberCountries);

    /// <summary>
    /// "ستصل فرصتك إلى ~N عضو مؤهَّل": عدد الأعضاء الذين تطابق اهتماماتهم نوع الفرصة وتطابق جنسيتهم أو بلد إقامتهم
    /// جمهورها. يُحسب من بيانات التسجيل الحالية (الاهتمامات + البلد والجنسية) ويُخزَّن ساعة؛ البوصلة لاحقًا تجعله أدق.
    /// </summary>
    public interface IAudienceEstimator
    {
        /// <param name="countryIso">ISO-2، أو XA = الوطن العربي، أو null/XW = الجميع.</param>
        Task<int> EstimateAsync(OpportunityType? type, string? countryIso, CancellationToken ct = default);
        Task<SiteNumbers> NumbersAsync(CancellationToken ct = default);
    }

    public sealed class AudienceEstimator : IAudienceEstimator
    {
        /// <summary>الدول العربية الـ22 (ISO 3166-1 alpha-2).</summary>
        public static readonly HashSet<string> ArabIso = new(StringComparer.OrdinalIgnoreCase)
        {
            "DZ", "BH", "KM", "DJ", "EG", "IQ", "JO", "KW", "LB", "LY", "MR",
            "MA", "OM", "PS", "QA", "SA", "SO", "SD", "SY", "TN", "AE", "YE"
        };

        private const string CacheKey = "audience:snapshot";
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        private sealed record Profile(HashSet<OpportunityType> Types, string? NationalityIso, string? ResidenceIso);
        private sealed record Snapshot(List<Profile> Profiles, int OpenOpportunities);

        public AudienceEstimator(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<int> EstimateAsync(OpportunityType? type, string? countryIso, CancellationToken ct = default)
        {
            var snapshot = await LoadAsync(ct);
            var iso = string.IsNullOrWhiteSpace(countryIso) ? null : countryIso.Trim().ToUpperInvariant();
            if (iso == "XW") iso = null;
            return snapshot.Profiles.Count(p => (type == null || p.Types.Contains(type.Value)) && Matches(p, iso));
        }

        public async Task<SiteNumbers> NumbersAsync(CancellationToken ct = default)
        {
            var snapshot = await LoadAsync(ct);
            var countries = snapshot.Profiles
                .SelectMany(p => new[] { p.NationalityIso, p.ResidenceIso })
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            return new SiteNumbers(snapshot.Profiles.Count, snapshot.OpenOpportunities, countries);
        }

        private static bool Matches(Profile p, string? iso)
        {
            if (iso == null) return true;
            if (iso == FacetParser.ArabWorldCode)
                return (p.NationalityIso != null && ArabIso.Contains(p.NationalityIso))
                    || (p.ResidenceIso != null && ArabIso.Contains(p.ResidenceIso));
            return string.Equals(p.NationalityIso, iso, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.ResidenceIso, iso, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<Snapshot> LoadAsync(CancellationToken ct)
        {
            if (_cache.TryGetValue(CacheKey, out Snapshot? cached) && cached != null) return cached;

            var countries = await _db.Countries.AsNoTracking().ToListAsync(ct);
            var users = await _db.Users.AsNoTracking()
                .Where(u => u.Role == UserRole.User)
                .Select(u => new { u.InterestsJson, u.Country, u.Nationality })
                .ToListAsync(ct);

            // أسماء البلدان تتكرر كثيرًا (كما خزّنها التسجيل بلغات مختلفة)؛ نحلّ كل اسم مرة واحدة
            var memo = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? Iso(string? name)
            {
                if (string.IsNullOrWhiteSpace(name)) return null;
                if (!memo.TryGetValue(name, out var iso))
                {
                    iso = MatchScorer.ResolveCountry(name, countries)?.IsoCode;
                    memo[name] = iso;
                }
                return iso;
            }

            var profiles = users
                .Select(u => new Profile(InterestMap.TypesFor(ParseInterests(u.InterestsJson)).ToHashSet(), Iso(u.Nationality), Iso(u.Country)))
                .ToList();

            var today = DateTime.Today;
            var open = await _db.Opportunities.AsNoTracking().CountAsync(o =>
                o.Status == OpportunityStatus.Published
                && (o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today), ct);

            var snapshot = new Snapshot(profiles, open);
            _cache.Set(CacheKey, snapshot, Ttl);
            return snapshot;
        }

        private static List<string> ParseInterests(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
            catch (JsonException) { return new List<string>(); }
        }
    }
}
