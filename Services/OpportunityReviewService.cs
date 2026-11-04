using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>
    /// مراجعة فرص المنظمات قبل النشر: المنظمة غير الموثَّقة تبدأ فرصها PendingReview ولا تظهر للزوار
    /// حتى يعتمدها المدير؛ الموثَّقة تنشر مباشرة؛ الموقوفة تُخفى كل فرصها.
    /// </summary>
    public interface IOpportunityReviewService
    {
        /// <summary>الحالة التي تأخذها فرصة تنشئها أو تعدّلها هذه المنظمة.</summary>
        OpportunityStatus StatusFor(Organization org);

        /// <summary>إشعار داخلي (ودفع) لكل مدير بأن فرصة بانتظار المراجعة — مرة واحدة لكل فرصة حتى تُقرأ.</summary>
        Task NotifyAdminsAsync(int opportunityId, CancellationToken ct = default);

        /// <summary>نشر فرصة معلّقة/مرفوضة/مسودة؛ اختياريًا توثيق منظمتها فتنشر لاحقًا بلا مراجعة.</summary>
        Task<bool> ApproveAsync(int opportunityId, bool verifyOrganization, CancellationToken ct = default);

        Task<bool> RejectAsync(int opportunityId, string? note, CancellationToken ct = default);

        Task<bool> SetOrganizationVerifiedAsync(int organizationId, bool verified, CancellationToken ct = default);

        /// <summary>درجة التوثيق المعروضة (3.1): Verified فأعلى تنشر بلا مراجعة (IsVerified يتبعها).</summary>
        Task<bool> SetOrganizationTierAsync(int organizationId, OrganizationTier tier, CancellationToken ct = default);

        /// <summary>الإيقاف يحوّل فرصها المنشورة إلى Suspended فورًا؛ رفعه يعيدها Published.</summary>
        Task<bool> SetOrganizationSuspendedAsync(int organizationId, bool suspended, CancellationToken ct = default);

        Task<int> PendingCountAsync(CancellationToken ct = default);
    }

    public class OpportunityReviewService : IOpportunityReviewService
    {
        public const int MaxNoteLength = 1000;

        private readonly ApplicationDbContext _db;

        private readonly IPublishRequestService? _requests;
        private readonly IOrganizationFollowService? _follows;

        /// <param name="requests">اختياري: يُغلق طلب النشر المرتبط ويراسل المنظمة عند الاعتماد (الاختبارات تمرّر null).</param>
        /// <param name="follows">اختياري: يُشعر متابعي المنظمة بالفرصة المنشورة (3.2).</param>
        public OpportunityReviewService(ApplicationDbContext db, IPublishRequestService? requests = null, IOrganizationFollowService? follows = null)
        {
            _db = db;
            _requests = requests;
            _follows = follows;
        }

        public OpportunityStatus StatusFor(Organization org) =>
            org.IsVerified ? OpportunityStatus.Published : OpportunityStatus.PendingReview;

        public async Task NotifyAdminsAsync(int opportunityId, CancellationToken ct = default)
        {
            var adminIds = await (from ur in _db.UserRoles
                                  join r in _db.Roles on ur.RoleId equals r.Id
                                  where r.Name == "Admin"
                                  select ur.UserId)
                .Distinct()
                .ToListAsync(ct);
            if (adminIds.Count == 0) return;

            var alreadyNotified = await _db.Notifications
                .Where(n => n.Kind == NotificationKind.ReviewPending && n.OpportunityId == opportunityId && n.ReadAt == null)
                .Select(n => n.UserId)
                .ToListAsync(ct);

            foreach (var adminId in adminIds.Except(alreadyNotified))
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    Kind = NotificationKind.ReviewPending,
                    OpportunityId = opportunityId,
                    Count = 1
                });
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> ApproveAsync(int opportunityId, bool verifyOrganization, CancellationToken ct = default)
        {
            var opp = await _db.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunityId, ct);
            if (opp == null || opp.Status == OpportunityStatus.Published || opp.Status == OpportunityStatus.Suspended)
                return false;

            opp.Status = OpportunityStatus.Published;
            opp.PublishDate = DateTime.Now; // تظهر كفرصة جديدة لحظة الاعتماد، كما عند نشر مسودة المنصة
            opp.ReviewedAt = DateTime.Now;
            opp.ReviewNote = null;

            if (verifyOrganization)
            {
                var org = await _db.Organizations.FirstOrDefaultAsync(o => o.UserId == opp.CreatedByUserId, ct);
                if (org != null)
                {
                    org.IsVerified = true;
                    if (org.VerificationTier < OrganizationTier.Verified) org.VerificationTier = OrganizationTier.Verified;
                }
            }

            await MarkReviewNotificationsReadAsync(opportunityId, ct);
            await _db.SaveChangesAsync(ct);
            if (_requests != null) await _requests.MarkPublishedAsync(opportunityId, ct);
            if (_follows != null) await _follows.NotifyFollowersAsync(opportunityId, ct);
            return true;
        }

        public async Task<bool> RejectAsync(int opportunityId, string? note, CancellationToken ct = default)
        {
            var opp = await _db.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunityId, ct);
            if (opp == null || opp.Status == OpportunityStatus.Suspended) return false;

            note = note?.Trim();
            if (!string.IsNullOrEmpty(note) && note.Length > MaxNoteLength) note = note[..MaxNoteLength];

            opp.Status = OpportunityStatus.Rejected;
            opp.ReviewNote = string.IsNullOrEmpty(note) ? null : note;
            opp.ReviewedAt = DateTime.Now;

            await MarkReviewNotificationsReadAsync(opportunityId, ct);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SetOrganizationVerifiedAsync(int organizationId, bool verified, CancellationToken ct = default)
        {
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
            if (org == null) return false;
            org.IsVerified = verified;
            // الدرجة المعروضة تتبع التوثيق: التوثيق يرفعها إلى «موثَّقة» على الأقل، وإلغاؤه يعيد «موثَّقة» إلى «مسجَّلة» (الشريك يبقى شريكًا)
            if (verified && org.VerificationTier < OrganizationTier.Verified) org.VerificationTier = OrganizationTier.Verified;
            else if (!verified && org.VerificationTier == OrganizationTier.Verified) org.VerificationTier = OrganizationTier.Registered;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SetOrganizationTierAsync(int organizationId, OrganizationTier tier, CancellationToken ct = default)
        {
            if (!Enum.IsDefined(tier)) return false;
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
            if (org == null) return false;
            org.VerificationTier = tier;
            org.IsVerified = tier >= OrganizationTier.Verified;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SetOrganizationSuspendedAsync(int organizationId, bool suspended, CancellationToken ct = default)
        {
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
            if (org == null) return false;
            if (org.IsSuspended == suspended) return true;

            org.IsSuspended = suspended;
            if (org.UserId != null)
            {
                var from = suspended ? OpportunityStatus.Published : OpportunityStatus.Suspended;
                var to = suspended ? OpportunityStatus.Suspended : OpportunityStatus.Published;
                var affected = await _db.Opportunities
                    .Where(o => o.CreatedByUserId == org.UserId && o.Status == from)
                    .ToListAsync(ct);
                foreach (var o in affected) o.Status = to;
            }
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public Task<int> PendingCountAsync(CancellationToken ct = default) =>
            _db.Opportunities.CountAsync(o => o.Status == OpportunityStatus.PendingReview, ct);

        private async Task MarkReviewNotificationsReadAsync(int opportunityId, CancellationToken ct)
        {
            var open = await _db.Notifications
                .Where(n => n.Kind == NotificationKind.ReviewPending && n.OpportunityId == opportunityId && n.ReadAt == null)
                .ToListAsync(ct);
            foreach (var n in open) n.ReadAt = DateTime.Now;
        }
    }
}
