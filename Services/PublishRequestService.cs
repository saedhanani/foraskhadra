using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>
    /// دورة طلب النشر من المنظمات: وصول الطلب (إشعار للمديرين + رسالة استلام للمنظمة) → تأخذه منصة الكتابة
    /// (GET /api/team/requests) وتربطه بالفرصة التي تكتبها (requestId) → عند نشر الفرصة يُغلق الطلب وتصل المنظمةَ رسالةٌ بالرابط.
    /// </summary>
    public interface IPublishRequestService
    {
        Task<PublishRequest> CreateAsync(PublishRequest request, CancellationToken ct = default);
        /// <summary>الطلبات المفتوحة (جديدة أو قيد الكتابة) — للوحة المدير.</summary>
        Task<int> OpenCountAsync(CancellationToken ct = default);
        /// <summary>منصة الكتابة أو المدير أخذ الطلب.</summary>
        Task<bool> TakeAsync(int id, CancellationToken ct = default);
        /// <summary>ربط الطلب بالفرصة التي كُتبت منه؛ إن كانت منشورة أصلًا يُغلق الطلب فورًا.</summary>
        Task<bool> LinkOpportunityAsync(int requestId, int opportunityId, CancellationToken ct = default);
        /// <summary>يُستدعى عند كل نشر فرصة: إن كانت مرتبطة بطلب مفتوح يُغلق ويُبلَّغ صاحبه بالرابط.</summary>
        Task MarkPublishedAsync(int opportunityId, CancellationToken ct = default);
        Task<bool> RejectAsync(int id, string? note, bool spam, CancellationToken ct = default);
    }

    public class PublishRequestService : IPublishRequestService
    {
        public const int MaxNoteLength = 500;

        private readonly ApplicationDbContext _db;
        private readonly IEmailQueue _emails;
        private readonly string _baseUrl;

        public PublishRequestService(ApplicationDbContext db, IEmailQueue emails, IConfiguration cfg)
        {
            _db = db;
            _emails = emails;
            _baseUrl = (cfg["BaseUrl"] ?? "https://www.foraskhadra.com/").TrimEnd('/');
        }

        public async Task<PublishRequest> CreateAsync(PublishRequest request, CancellationToken ct = default)
        {
            request.Status = PublishRequestStatus.New;
            request.CreatedAt = DateTime.Now;
            request.Language = CultureRouting.Normalize(request.Language);
            _db.PublishRequests.Add(request);
            await _db.SaveChangesAsync(ct);

            await NotifyAdminsAsync(request, ct);

            var mail = PartnerEmails.Received(request.Language, request.OrganizationName, request.EstimatedReach, GatewayLink(request.Language));
            _emails.Enqueue(request.ContactEmail, mail.Subject, mail.Html);
            return request;
        }

        public Task<int> OpenCountAsync(CancellationToken ct = default) =>
            _db.PublishRequests.CountAsync(r => r.Status == PublishRequestStatus.New || r.Status == PublishRequestStatus.InProgress, ct);

        public async Task<bool> TakeAsync(int id, CancellationToken ct = default)
        {
            var request = await _db.PublishRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (request == null || request.Status != PublishRequestStatus.New) return false;
            request.Status = PublishRequestStatus.InProgress;
            request.TakenAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> LinkOpportunityAsync(int requestId, int opportunityId, CancellationToken ct = default)
        {
            var request = await _db.PublishRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
            var opp = await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == opportunityId, ct);
            if (request == null || opp == null) return false;
            if (request.Status == PublishRequestStatus.Published || request.Status == PublishRequestStatus.Spam) return false;

            request.OpportunityId = opportunityId;
            if (request.Status == PublishRequestStatus.New)
            {
                request.Status = PublishRequestStatus.InProgress;
                request.TakenAt = DateTime.Now;
            }
            await _db.SaveChangesAsync(ct);

            if (opp.Status == OpportunityStatus.Published) await MarkPublishedAsync(opportunityId, ct);
            return true;
        }

        public async Task MarkPublishedAsync(int opportunityId, CancellationToken ct = default)
        {
            var open = await _db.PublishRequests
                .Where(r => r.OpportunityId == opportunityId && r.Status != PublishRequestStatus.Published && r.Status != PublishRequestStatus.Spam)
                .ToListAsync(ct);
            if (open.Count == 0) return;

            var opp = await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == opportunityId, ct);
            if (opp == null || opp.Status != OpportunityStatus.Published) return;

            foreach (var request in open)
            {
                request.Status = PublishRequestStatus.Published;
                request.ClosedAt = DateTime.Now;
                var title = TitleFor(opp, request.Language);
                var link = $"{_baseUrl}{CultureRouting.Prefix(request.Language)}/Opportunity/Details/{opp.Id}/{SlugHelper.Make(title)}";
                var mail = PartnerEmails.Published(request.Language, request.OrganizationName, title, link);
                _emails.Enqueue(request.ContactEmail, mail.Subject, mail.Html);
                await MarkAdminNotificationsReadAsync(request.Id, ct);
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> RejectAsync(int id, string? note, bool spam, CancellationToken ct = default)
        {
            var request = await _db.PublishRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (request == null || request.Status == PublishRequestStatus.Published) return false;

            note = note?.Trim();
            if (!string.IsNullOrEmpty(note) && note.Length > MaxNoteLength) note = note[..MaxNoteLength];
            request.Status = spam ? PublishRequestStatus.Spam : PublishRequestStatus.Rejected;
            request.AdminNote = note;
            request.ClosedAt = DateTime.Now;
            await MarkAdminNotificationsReadAsync(request.Id, ct);
            await _db.SaveChangesAsync(ct);

            // المزعج لا يُراسَل؛ الرفض العادي يصل المنظمة مع السبب
            if (!spam)
            {
                var mail = PartnerEmails.Rejected(request.Language, request.OrganizationName, note, GatewayLink(request.Language));
                _emails.Enqueue(request.ContactEmail, mail.Subject, mail.Html);
            }
            return true;
        }

        private string GatewayLink(string lang) => $"{_baseUrl}{CultureRouting.Prefix(lang)}/for-organizations";

        private static string TitleFor(Opportunity opp, string lang)
        {
            var title = lang == "en" ? opp.TitleEn : lang == "fr" ? opp.TitleFr : opp.TitleAr;
            return string.IsNullOrWhiteSpace(title) ? opp.TitleAr : title;
        }

        /// <summary>إشعار جرس + دفع لكل مدير (LinkParams = رقم الطلب، Label = اسم الجهة).</summary>
        private async Task NotifyAdminsAsync(PublishRequest request, CancellationToken ct)
        {
            var adminIds = await (from ur in _db.UserRoles
                                  join r in _db.Roles on ur.RoleId equals r.Id
                                  where r.Name == "Admin"
                                  select ur.UserId)
                .Distinct()
                .ToListAsync(ct);
            if (adminIds.Count == 0) return;

            foreach (var adminId in adminIds)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    Kind = NotificationKind.PublishRequestNew,
                    Label = request.OrganizationName,
                    LinkParams = request.Id.ToString(),
                    Count = 1
                });
            }
            await _db.SaveChangesAsync(ct);
        }

        private async Task MarkAdminNotificationsReadAsync(int requestId, CancellationToken ct)
        {
            var key = requestId.ToString();
            var open = await _db.Notifications
                .Where(n => n.Kind == NotificationKind.PublishRequestNew && n.LinkParams == key && n.ReadAt == null)
                .ToListAsync(ct);
            foreach (var n in open) n.ReadAt = DateTime.Now;
        }
    }
}
