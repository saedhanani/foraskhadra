using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    public record ReachRow(int Id, string Title, OpportunityStatus Status, DateTime? EndDate, DateTime PublishDate, int Views, int Clicks, int Saves)
    {
        public int ClickRatePercent => Views > 0 ? (int)Math.Round(100.0 * Clicks / Views) : 0;
    }

    public record ReachDay(DateOnly Day, int Views, int Clicks);

    /// <summary>تقرير الوصول لمنظمة (المرحلة 3.2): مشاهدات صفحات فرصها ونقرات «قدّم الآن» من OpportunityDailyStats.</summary>
    public record ReachReport(
        string OrganizationName,
        int Days,
        DateOnly From,
        DateOnly To,
        int Views,
        int Clicks,
        int Saves,
        int Followers,
        int PublishedCount,
        List<ReachRow> Rows,
        List<ReachDay> Series)
    {
        public int ClickRatePercent => Views > 0 ? (int)Math.Round(100.0 * Clicks / Views) : 0;
        public ReachRow? Top => Rows.Count > 0 ? Rows[0] : null;
    }

    public interface IOrgReachReport
    {
        /// <summary>null إن لم يكن للمستخدم منظمة. days = 7 أو 30.</summary>
        Task<ReachReport?> BuildAsync(string orgUserId, int days, string lang, DateOnly? today = null, CancellationToken ct = default);
    }

    public class OrgReachReport : IOrgReachReport
    {
        public static readonly int[] AllowedDays = { 7, 30 };

        private readonly ApplicationDbContext _db;

        public OrgReachReport(ApplicationDbContext db) => _db = db;

        public async Task<ReachReport?> BuildAsync(string orgUserId, int days, string lang, DateOnly? today = null, CancellationToken ct = default)
        {
            if (!AllowedDays.Contains(days)) days = 30;
            var org = await _db.Organizations.AsNoTracking()
                .Where(o => o.UserId == orgUserId)
                .Select(o => new { o.Id, o.Name })
                .FirstOrDefaultAsync(ct);
            if (org == null) return null;

            var to = today ?? DateOnly.FromDateTime(DateTime.Now);
            var from = to.AddDays(-(days - 1));

            var opps = await _db.Opportunities.AsNoTracking()
                .Where(o => o.CreatedByUserId == orgUserId)
                .Select(o => new { o.Id, o.TitleAr, o.TitleEn, o.TitleFr, o.Status, o.EndDate, o.PublishDate })
                .ToListAsync(ct);
            var ids = opps.Select(o => o.Id).ToList();

            var stats = ids.Count == 0
                ? new List<OpportunityDailyStat>()
                : await _db.OpportunityDailyStats.AsNoTracking()
                    .Where(s => ids.Contains(s.OpportunityId) && s.Day >= from && s.Day <= to)
                    .ToListAsync(ct);

            var saves = ids.Count == 0
                ? new Dictionary<int, int>()
                : await _db.SavedOpportunities.AsNoTracking()
                    .Where(s => ids.Contains(s.OpportunityId))
                    .GroupBy(s => s.OpportunityId)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            var followers = await _db.OrganizationFollows.CountAsync(f => f.OrganizationId == org.Id, ct);

            var byOpp = stats.GroupBy(s => s.OpportunityId)
                .ToDictionary(g => g.Key, g => (Views: g.Sum(s => s.Views), Clicks: g.Sum(s => s.ApplyClicks)));

            var rows = opps.Select(o =>
                {
                    var title = lang == "en" ? o.TitleEn : lang == "fr" ? o.TitleFr : o.TitleAr;
                    var st = byOpp.GetValueOrDefault(o.Id);
                    return new ReachRow(o.Id, title, o.Status, o.EndDate, o.PublishDate, st.Views, st.Clicks, saves.GetValueOrDefault(o.Id));
                })
                .OrderByDescending(r => r.Views).ThenByDescending(r => r.Clicks).ThenByDescending(r => r.PublishDate)
                .ToList();

            var byDay = stats.GroupBy(s => s.Day).ToDictionary(g => g.Key, g => (Views: g.Sum(s => s.Views), Clicks: g.Sum(s => s.ApplyClicks)));
            var series = Enumerable.Range(0, days)
                .Select(i => from.AddDays(i))
                .Select(d => { var v = byDay.GetValueOrDefault(d); return new ReachDay(d, v.Views, v.Clicks); })
                .ToList();

            return new ReachReport(
                org.Name, days, from, to,
                rows.Sum(r => r.Views), rows.Sum(r => r.Clicks), rows.Sum(r => r.Saves),
                followers,
                opps.Count(o => o.Status == OpportunityStatus.Published),
                rows, series);
        }
    }
}
