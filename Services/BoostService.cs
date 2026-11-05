using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>
    /// «عزّز للمؤهَّلين» (المرحلة 4.5): يرسل إشعار BoostedMatch (جرس + دفع) لكل عضو له ملف أهلية تنطبق عليه شروط الفرصة
    /// حسب CompassMatcher، مرة واحدة لكل (عضو، فرصة)، ويسجّل الدفعة في OpportunityBoosts للفوترة والمراجعة.
    /// </summary>
    public interface IBoostService
    {
        /// <summary>عدد الأعضاء المؤهَّلين الآن (قبل الإرسال) — للعرض في اللوحة.</summary>
        Task<int> EstimateAsync(int opportunityId, CancellationToken ct = default);

        /// <summary>null إن لم توجد الفرصة أو لم تكن منشورة ومفتوحة.</summary>
        Task<OpportunityBoost?> SendAsync(int opportunityId, string? adminUserId, string? note, CancellationToken ct = default);
    }

    public class BoostService : IBoostService
    {
        private readonly ApplicationDbContext _db;

        public BoostService(ApplicationDbContext db) => _db = db;

        public async Task<int> EstimateAsync(int opportunityId, CancellationToken ct = default) =>
            (await EligibleUserIdsAsync(opportunityId, ct))?.Count ?? 0;

        public async Task<OpportunityBoost?> SendAsync(int opportunityId, string? adminUserId, string? note, CancellationToken ct = default)
        {
            var eligible = await EligibleUserIdsAsync(opportunityId, ct);
            if (eligible == null) return null;

            var already = await _db.Notifications
                .Where(n => n.Kind == NotificationKind.BoostedMatch && n.OpportunityId == opportunityId)
                .Select(n => n.UserId)
                .ToListAsync(ct);
            var skip = already.ToHashSet();

            var sent = 0;
            foreach (var userId in eligible.Where(id => !skip.Contains(id)))
            {
                _db.Notifications.Add(new Notification { UserId = userId, Kind = NotificationKind.BoostedMatch, OpportunityId = opportunityId, Count = 1 });
                sent++;
            }
            var boost = new OpportunityBoost
            {
                OpportunityId = opportunityId,
                Recipients = sent,
                CreatedByUserId = adminUserId,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(200, note.Trim().Length)]
            };
            _db.OpportunityBoosts.Add(boost);
            await _db.SaveChangesAsync(ct);
            return boost;
        }

        /// <summary>الأعضاء (دور User) الذين يملكون ملف أهلية وتنطبق عليهم الشروط الصلبة للفرصة. null إن كانت الفرصة غير متاحة.</summary>
        private async Task<List<string>?> EligibleUserIdsAsync(int opportunityId, CancellationToken ct)
        {
            var today = DateTime.Now.Date;
            var opp = await _db.Opportunities.AsNoTracking()
                .Include(o => o.AvailableCountries)
                .Include(o => o.Fields)
                .FirstOrDefaultAsync(o => o.Id == opportunityId, ct);
            if (opp == null || opp.Status != OpportunityStatus.Published) return null;
            if (opp.DeadlineType == DeadlineType.SpecificDate && opp.EndDate.HasValue && opp.EndDate.Value.Date < today) return null;

            var profiles = await _db.EligibilityProfiles.AsNoTracking().ToListAsync(ct);
            var result = new List<string>();
            foreach (var p in profiles)
            {
                var match = CompassMatcher.Evaluate(p.ToAnswers(), opp, today, "ar");
                if (match != null && match.Eligible) result.Add(p.UserId);
            }
            return result;
        }
    }
}
