using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>ملخص أسبوع المستخدم (البند 5-1) — يُحسب عند الطلب من بيانات الأيام السبعة الماضية، لا يُخزَّن.</summary>
    public class WeeklySummary
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        /// <summary>كل ما نُشر هذا الأسبوع.</summary>
        public int NewTotal { get; set; }
        /// <summary>ما نُشر هذا الأسبوع ويطابق اهتمامات المستخدم ويقبل جنسيته.</summary>
        public int NewMatchingCount { get; set; }
        public List<Opportunity> NewMatching { get; set; } = new();
        /// <summary>محفوظات تنتهي خلال 7 أيام.</summary>
        public List<Opportunity> DeadlinesSoon { get; set; } = new();
        public int AppliedThisWeek { get; set; }
        public int SavedThisWeek { get; set; }

        public bool HasAnything => NewMatchingCount > 0 || DeadlinesSoon.Count > 0 || AppliedThisWeek > 0 || SavedThisWeek > 0;
    }

    public interface IWeeklySummaryService
    {
        Task<WeeklySummary> BuildAsync(ApplicationUser user, MatchProfile profile);
    }

    public class WeeklySummaryService : IWeeklySummaryService
    {
        private readonly ApplicationDbContext _db;
        public const int Days = 7;

        public WeeklySummaryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<WeeklySummary> BuildAsync(ApplicationUser user, MatchProfile profile)
        {
            var now = DateTime.Now;
            var from = now.AddDays(-Days);
            var today = now.Date;
            var s = new WeeklySummary { From = from, To = now };

            s.NewTotal = await _db.Opportunities
                .CountAsync(o => o.Status == OpportunityStatus.Published && o.PublishDate >= from);

            if (profile.HasInterests)
            {
                var types = profile.InterestTypes.ToList();
                var candidates = await _db.Opportunities
                    .Include(o => o.AvailableCountries)
                    .AsNoTracking()
                    .Where(o => o.Status == OpportunityStatus.Published && o.PublishDate >= from
                                && o.Type != null && types.Contains(o.Type.Value)
                                && (o.EndDate == null || o.EndDate >= today))
                    .OrderByDescending(o => o.PublishDate)
                    .ToListAsync();

                var matching = candidates.Where(o => MatchScorer.Eligible(profile, o) != false).ToList();
                s.NewMatchingCount = matching.Count;
                s.NewMatching = matching
                    .OrderByDescending(o => MatchScorer.Score(profile, o))
                    .ThenByDescending(o => o.PublishDate)
                    .Take(6)
                    .ToList();
            }

            // Include ثم Select في الذاكرة: EF لا يقبل Include بعد Select على مسار تنقّل
            var windowEnd = today.AddDays(Days);
            var dueSaved = await _db.SavedOpportunities
                .AsNoTracking()
                .Where(x => x.UserId == user.Id && x.Status == ApplicationStatus.NotApplied
                            && x.Opportunity!.Status == OpportunityStatus.Published
                            && x.Opportunity.DeadlineType == DeadlineType.SpecificDate
                            && x.Opportunity.EndDate >= today && x.Opportunity.EndDate <= windowEnd)
                .OrderBy(x => x.Opportunity!.EndDate)
                .Include(x => x.Opportunity!).ThenInclude(o => o.AvailableCountries)
                .ToListAsync();
            s.DeadlinesSoon = dueSaved.Select(x => x.Opportunity!).ToList();

            s.AppliedThisWeek = await _db.SavedOpportunities
                .CountAsync(x => x.UserId == user.Id && x.Status != ApplicationStatus.NotApplied && x.StatusChangedAt >= from);
            s.SavedThisWeek = await _db.SavedOpportunities
                .CountAsync(x => x.UserId == user.Id && x.SavedAt >= from);

            return s;
        }
    }
}
