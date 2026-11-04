using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>«تابع هذه المنظمة» (المرحلة 3.2) — انظر Models/OrganizationFollow.</summary>
    public interface IOrganizationFollowService
    {
        /// <summary>يعيد الحالة الجديدة: true = صار متابعًا.</summary>
        Task<bool> ToggleAsync(string userId, int organizationId, CancellationToken ct = default);

        Task<bool> IsFollowingAsync(string userId, int organizationId, CancellationToken ct = default);

        Task<int> FollowerCountAsync(int organizationId, CancellationToken ct = default);

        /// <summary>
        /// بعد نشر فرصة لمنظمة: إشعار OrganizationPublished لكل متابع لم يُشعَر بهذه الفرصة بعد. يعيد عدد الإشعارات.
        /// آمن للاستدعاء المتكرر (الاعتماد، «حفظ ونشر»، النشر المباشر للموثَّقة).
        /// </summary>
        Task<int> NotifyFollowersAsync(int opportunityId, CancellationToken ct = default);
    }

    public class OrganizationFollowService : IOrganizationFollowService
    {
        private readonly ApplicationDbContext _db;

        public OrganizationFollowService(ApplicationDbContext db) => _db = db;

        public async Task<bool> ToggleAsync(string userId, int organizationId, CancellationToken ct = default)
        {
            var existing = await _db.OrganizationFollows.FirstOrDefaultAsync(f => f.UserId == userId && f.OrganizationId == organizationId, ct);
            if (existing != null)
            {
                _db.OrganizationFollows.Remove(existing);
                await _db.SaveChangesAsync(ct);
                return false;
            }

            if (!await _db.Organizations.AnyAsync(o => o.Id == organizationId, ct)) return false;
            _db.OrganizationFollows.Add(new OrganizationFollow { UserId = userId, OrganizationId = organizationId });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { /* سباق نقرتين: الصف موجود */ }
            return true;
        }

        public Task<bool> IsFollowingAsync(string userId, int organizationId, CancellationToken ct = default) =>
            _db.OrganizationFollows.AnyAsync(f => f.UserId == userId && f.OrganizationId == organizationId, ct);

        public Task<int> FollowerCountAsync(int organizationId, CancellationToken ct = default) =>
            _db.OrganizationFollows.CountAsync(f => f.OrganizationId == organizationId, ct);

        public async Task<int> NotifyFollowersAsync(int opportunityId, CancellationToken ct = default)
        {
            var opp = await _db.Opportunities.AsNoTracking()
                .Where(o => o.Id == opportunityId)
                .Select(o => new { o.Status, o.CreatedByUserId })
                .FirstOrDefaultAsync(ct);
            if (opp == null || opp.Status != OpportunityStatus.Published || string.IsNullOrEmpty(opp.CreatedByUserId)) return 0;

            var org = await _db.Organizations.AsNoTracking()
                .Where(o => o.UserId == opp.CreatedByUserId)
                .Select(o => new { o.Id, o.Name })
                .FirstOrDefaultAsync(ct);
            if (org == null) return 0;

            var followers = await _db.OrganizationFollows
                .Where(f => f.OrganizationId == org.Id)
                .Select(f => f.UserId)
                .ToListAsync(ct);
            if (followers.Count == 0) return 0;

            var already = await _db.Notifications
                .Where(n => n.Kind == NotificationKind.OrganizationPublished && n.OpportunityId == opportunityId)
                .Select(n => n.UserId)
                .ToListAsync(ct);
            var skip = already.ToHashSet();

            var added = 0;
            foreach (var userId in followers.Where(id => !skip.Contains(id)))
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Kind = NotificationKind.OrganizationPublished,
                    OpportunityId = opportunityId,
                    Count = 1,
                    Label = org.Name,
                    LinkParams = org.Id.ToString()
                });
                added++;
            }
            if (added > 0) await _db.SaveChangesAsync(ct);
            return added;
        }
    }
}
