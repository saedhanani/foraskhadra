using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Foras_Khadra.Services
{
    public interface IMatchService
    {
        /// <summary>ملف الترشيح للمستخدم، أو null إن لم يكن فردًا مسجّلًا (زائر/منظمة/إدارة).</summary>
        Task<MatchProfile?> ProfileAsync(ApplicationUser? user, bool isIndividual);
        /// <summary>نسبة التطابق لكل فرصة (المفتاح OpportunityId). تتطلب تحميل AvailableCountries.</summary>
        Dictionary<int, int> Scores(MatchProfile profile, IEnumerable<Opportunity> opportunities);
    }

    /// <summary>يبني MatchProfile من بيانات المستخدم وجدول الدول (مخزّن مؤقتًا — 24 صفًا لا تتغير).</summary>
    public class MatchService : IMatchService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private const string CountriesKey = "match:countries";

        public MatchService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<MatchProfile?> ProfileAsync(ApplicationUser? user, bool isIndividual)
        {
            if (user == null || !isIndividual) return null;

            var countries = await _cache.GetOrCreateAsync(CountriesKey, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return await _db.Countries.AsNoTracking().ToListAsync();
            }) ?? new List<Country>();

            return new MatchProfile(
                InterestMap.TypesFor(user.Interests),
                MatchScorer.ResolveCountry(user.Nationality, countries)?.Id,
                MatchScorer.ResolveCountry(user.Country, countries)?.Id);
        }

        public Dictionary<int, int> Scores(MatchProfile profile, IEnumerable<Opportunity> opportunities) =>
            opportunities.Distinct().ToDictionary(o => o.Id, o => MatchScorer.Score(profile, o));
    }
}
