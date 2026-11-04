using System.Security.Cryptography;
using System.Text;
using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <param name="Added">false إن كان للمُبلِّغ نفسه بلاغ مفتوح على الفرصة نفسها (لا يُكرَّر).</param>
    /// <param name="OpenReporters">عدد المُبلِّغين المختلفين ذوي البلاغات المفتوحة.</param>
    /// <param name="ApplyHidden">بلغ العدد الحد فتوقّف زر التقديم حتى مراجعة المدير.</param>
    public record ReportResult(bool Added, int OpenReporters, bool ApplyHidden);

    /// <summary>بلاغات المشاكل على الفرص (المرحلة 3.1) — انظر Models/OpportunityReport.</summary>
    public interface IOpportunityReportService
    {
        /// <summary>null إن لم توجد الفرصة.</summary>
        Task<ReportResult?> SubmitAsync(int opportunityId, OpportunityReportReason reason, string? details, string reporterKey, string? userId, string? email, CancellationToken ct = default);

        /// <summary>عدد المُبلِّغين المختلفين ذوي بلاغات مفتوحة على الفرصة.</summary>
        Task<int> OpenReportersAsync(int opportunityId, CancellationToken ct = default);

        /// <summary>هل زر التقديم موقوف بسبب البلاغات؟</summary>
        Task<bool> IsApplyHiddenAsync(int opportunityId, CancellationToken ct = default);

        Task<List<OpportunityReport>> ListAsync(bool openOnly, int take = 200, CancellationToken ct = default);

        Task<bool> ResolveAsync(int reportId, OpportunityReportStatus status, string adminUserId, CancellationToken ct = default);

        /// <summary>يغلق كل البلاغات المفتوحة على الفرصة بحالة واحدة؛ يعيد عددها.</summary>
        Task<int> ResolveAllAsync(int opportunityId, OpportunityReportStatus status, string adminUserId, CancellationToken ct = default);

        Task<int> OpenCountAsync(CancellationToken ct = default);
    }

    public class OpportunityReportService : IOpportunityReportService
    {
        private readonly ApplicationDbContext _db;

        public OpportunityReportService(ApplicationDbContext db) => _db = db;

        /// <summary>هوية المُبلِّغ: العضو بمعرّفه، والزائر ببصمة IP والمتصفح (لا يُخزَّن IP نفسه).</summary>
        public static string ReporterKeyFor(string? userId, string? ip, string? userAgent)
        {
            if (!string.IsNullOrEmpty(userId)) return "u:" + (userId.Length > 60 ? userId[..60] : userId);
            var raw = (ip ?? "?") + "|" + (userAgent ?? "");
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return "g:" + Convert.ToHexString(hash)[..40].ToLowerInvariant();
        }

        public static bool IsApplyHidden(int openReporters) => openReporters >= OpportunityReport.HideThreshold;

        public async Task<ReportResult?> SubmitAsync(int opportunityId, OpportunityReportReason reason, string? details, string reporterKey, string? userId, string? email, CancellationToken ct = default)
        {
            var exists = await _db.Opportunities.AnyAsync(o => o.Id == opportunityId, ct);
            if (!exists) return null;
            if (!Enum.IsDefined(reason)) reason = OpportunityReportReason.Other;

            details = details?.Trim();
            if (string.IsNullOrEmpty(details)) details = null;
            else if (details.Length > OpportunityReport.MaxDetails) details = details[..OpportunityReport.MaxDetails];

            email = email?.Trim();
            if (string.IsNullOrEmpty(email) || email.Length > 256 || !email.Contains('@')) email = null;

            if (string.IsNullOrEmpty(reporterKey)) reporterKey = ReporterKeyFor(userId, null, null);
            if (reporterKey.Length > 64) reporterKey = reporterKey[..64];

            var duplicate = await _db.OpportunityReports.AnyAsync(
                r => r.OpportunityId == opportunityId && r.ReporterKey == reporterKey && r.Status == OpportunityReportStatus.Open, ct);

            if (!duplicate)
            {
                _db.OpportunityReports.Add(new OpportunityReport
                {
                    OpportunityId = opportunityId,
                    Reason = reason,
                    Details = details,
                    UserId = string.IsNullOrEmpty(userId) ? null : userId,
                    Email = email,
                    ReporterKey = reporterKey
                });
                await _db.SaveChangesAsync(ct);
            }

            var open = await OpenReportersAsync(opportunityId, ct);
            var hidden = IsApplyHidden(open);
            if (hidden && !duplicate) await NotifyAdminsAsync(opportunityId, open, ct);
            return new ReportResult(!duplicate, open, hidden);
        }

        public Task<int> OpenReportersAsync(int opportunityId, CancellationToken ct = default) =>
            _db.OpportunityReports
                .Where(r => r.OpportunityId == opportunityId && r.Status == OpportunityReportStatus.Open)
                .Select(r => r.ReporterKey)
                .Distinct()
                .CountAsync(ct);

        public async Task<bool> IsApplyHiddenAsync(int opportunityId, CancellationToken ct = default) =>
            IsApplyHidden(await OpenReportersAsync(opportunityId, ct));

        public Task<List<OpportunityReport>> ListAsync(bool openOnly, int take = 200, CancellationToken ct = default)
        {
            var q = _db.OpportunityReports.AsNoTracking().Include(r => r.Opportunity).AsQueryable();
            q = openOnly
                ? q.Where(r => r.Status == OpportunityReportStatus.Open).OrderByDescending(r => r.CreatedAt)
                : q.OrderBy(r => r.Status).ThenByDescending(r => r.CreatedAt);
            return q.Take(Math.Clamp(take, 1, 1000)).ToListAsync(ct);
        }

        public async Task<bool> ResolveAsync(int reportId, OpportunityReportStatus status, string adminUserId, CancellationToken ct = default)
        {
            if (status == OpportunityReportStatus.Open) return false;
            var report = await _db.OpportunityReports.FirstOrDefaultAsync(r => r.Id == reportId, ct);
            if (report == null) return false;
            Close(report, status, adminUserId);
            await _db.SaveChangesAsync(ct);
            await MarkAdminNotificationsReadIfClearAsync(report.OpportunityId, ct);
            return true;
        }

        public async Task<int> ResolveAllAsync(int opportunityId, OpportunityReportStatus status, string adminUserId, CancellationToken ct = default)
        {
            if (status == OpportunityReportStatus.Open) return 0;
            var open = await _db.OpportunityReports
                .Where(r => r.OpportunityId == opportunityId && r.Status == OpportunityReportStatus.Open)
                .ToListAsync(ct);
            foreach (var r in open) Close(r, status, adminUserId);
            await _db.SaveChangesAsync(ct);
            await MarkAdminNotificationsReadIfClearAsync(opportunityId, ct);
            return open.Count;
        }

        public Task<int> OpenCountAsync(CancellationToken ct = default) =>
            _db.OpportunityReports.CountAsync(r => r.Status == OpportunityReportStatus.Open, ct);

        private static void Close(OpportunityReport report, OpportunityReportStatus status, string adminUserId)
        {
            report.Status = status;
            report.ResolvedAt = DateTime.Now;
            report.ResolvedByUserId = string.IsNullOrEmpty(adminUserId) ? null : adminUserId;
        }

        /// <summary>إشعار داخلي (ودفع) لكل مدير حين يبلغ عدد المُبلِّغين الحد — مرة واحدة لكل فرصة حتى يُقرأ.</summary>
        private async Task NotifyAdminsAsync(int opportunityId, int openReporters, CancellationToken ct)
        {
            var adminIds = await (from ur in _db.UserRoles
                                  join r in _db.Roles on ur.RoleId equals r.Id
                                  where r.Name == "Admin"
                                  select ur.UserId)
                .Distinct()
                .ToListAsync(ct);
            if (adminIds.Count == 0) return;

            var unread = await _db.Notifications
                .Where(n => n.Kind == NotificationKind.OpportunityReported && n.OpportunityId == opportunityId && n.ReadAt == null)
                .ToListAsync(ct);
            foreach (var n in unread) n.Count = openReporters;

            var notified = unread.Select(n => n.UserId).ToHashSet();
            foreach (var adminId in adminIds.Where(id => !notified.Contains(id)))
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    Kind = NotificationKind.OpportunityReported,
                    OpportunityId = opportunityId,
                    Count = openReporters
                });
            }
            await _db.SaveChangesAsync(ct);
        }

        private async Task MarkAdminNotificationsReadIfClearAsync(int opportunityId, CancellationToken ct)
        {
            if (IsApplyHidden(await OpenReportersAsync(opportunityId, ct))) return;
            var open = await _db.Notifications
                .Where(n => n.Kind == NotificationKind.OpportunityReported && n.OpportunityId == opportunityId && n.ReadAt == null)
                .ToListAsync(ct);
            if (open.Count == 0) return;
            foreach (var n in open) n.ReadAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
        }
    }
}
