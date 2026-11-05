using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>
    /// لوحة مؤشرات المدير (المرحلة 5.1): أرقام المدة المختارة من الجداول الموجودة بلا عدّادات جديدة —
    /// تسجيلات، إكمال البوصلة، تحويل بوابة المنظمات، فرص/يوم، زمن المراجعة، المحتوى الأسبوعي.
    /// المشاهدات ونقرات /go/ والبحث بصفر نتائج تأتي من Analytics في الصفحة نفسها (/Admin/Analytics).
    /// </summary>
    public interface IKpiReport
    {
        Task<KpiSnapshot> BuildAsync(DateOnly from, DateOnly to, DateTime now, CancellationToken ct = default);
    }

    /// <summary>أرقام مدة واحدة مع أهداف المرحلة 5 (عمليات المحتوى): 15 فرصة/يوم، مراجعة خلال 24 ساعة، مقال أو دليل أسبوعيًا.</summary>
    public sealed record KpiSnapshot
    {
        public const int OpportunitiesPerDayTarget = 15;
        public const int ReviewHoursTarget = 24;
        public const int ContentPerWeekTarget = 1;
        /// <summary>المسار المطبَّع لبوابة المنظمات في PageViewDailyStats.</summary>
        public const string GatewayPath = "/for-organizations";

        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public int Days => To.DayNumber - From.DayNumber + 1;

        // التسجيلات (حسابات أفراد)
        public int NewUsers { get; init; }
        public int NewUsersConfirmed { get; init; }
        public int NewUsersPrevious { get; init; }

        // إكمال البوصلة
        public int CompassProfiles { get; init; }
        public int CompassStarts { get; init; }
        public int CompassResults { get; init; }

        // تحويل بوابة المنظمات
        public int GatewayViews { get; init; }
        public int PublishRequests { get; init; }
        public int PublishRequestsPublished { get; init; }
        public int PublishRequestsRejected { get; init; }
        public double? PublishRequestMedianHours { get; init; }
        public int NewOrganizations { get; init; }

        // فرص/يوم
        public int OpportunitiesPublished { get; init; }
        public List<(DateOnly Day, int Count)> OpportunitiesPerDay { get; init; } = new();

        // زمن المراجعة
        public int Reviewed { get; init; }
        public double? ReviewMedianHours { get; init; }
        public int Pending { get; init; }
        public double? OldestPendingHours { get; init; }
        public int Drafts { get; init; }

        // المحتوى
        public int ArticlesPublished { get; init; }
        public int GuidesAdded { get; init; }

        public double OpportunitiesPerDayAverage => Days <= 0 ? 0 : (double)OpportunitiesPublished / Days;
        public double Weeks => Math.Max(1, Days / 7.0);
        public double ContentPerWeek => (ArticlesPublished + GuidesAdded) / Weeks;
        public bool MeetsOpportunitiesTarget => OpportunitiesPerDayAverage >= OpportunitiesPerDayTarget;
        /// <summary>null حين لا توجد مراجعات مقيسة في المدة.</summary>
        public bool? MeetsReviewTarget => ReviewMedianHours.HasValue ? ReviewMedianHours.Value <= ReviewHoursTarget : null;
        public bool MeetsContentTarget => ContentPerWeek >= ContentPerWeekTarget;

        public static KpiSnapshot Empty(DateOnly from, DateOnly to) => new() { From = from, To = to };
    }

    public class KpiReport : IKpiReport
    {
        private readonly ApplicationDbContext _db;

        public KpiReport(ApplicationDbContext db) => _db = db;

        public async Task<KpiSnapshot> BuildAsync(DateOnly from, DateOnly to, DateTime now, CancellationToken ct = default)
        {
            if (to < from) (from, to) = (to, from);
            var days = to.DayNumber - from.DayNumber + 1;
            var start = from.ToDateTime(TimeOnly.MinValue);
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var previousStart = start.AddDays(-days);

            // التسجيلات: من جدول المستخدمين لا من الأحداث (الأحداث بدأت مع التحليلات فقط)
            var users = _db.Users.AsNoTracking().Where(u => u.Role == UserRole.User);
            var newUsers = await users.CountAsync(u => u.CreatedAt >= start && u.CreatedAt < end, ct);
            var newUsersConfirmed = await users.CountAsync(u => u.CreatedAt >= start && u.CreatedAt < end && u.EmailConfirmed, ct);
            var newUsersPrevious = await users.CountAsync(u => u.CreatedAt >= previousStart && u.CreatedAt < start, ct);

            // البوصلة: ملفات أهلية جديدة + نسبة من بدأ إلى من رأى النتيجة (من الأحداث)
            var compassProfiles = await _db.EligibilityProfiles.AsNoTracking().CountAsync(p => p.CreatedAt >= start && p.CreatedAt < end, ct);
            var events = await _db.SiteEventDailyStats.AsNoTracking()
                .Where(e => e.Day >= from && e.Day <= to && e.Label == null && (e.Event == "compass.start" || e.Event == "compass.result"))
                .GroupBy(e => e.Event)
                .Select(g => new { g.Key, Total = g.Sum(e => e.Count) })
                .ToListAsync(ct);
            int Ev(string name) => events.FirstOrDefault(e => e.Key == name)?.Total ?? 0;

            // بوابة المنظمات: زيارات الصفحة → طلبات نشر → نُشر/رُفض، وزمن الإغلاق، ومنظمات سجّلت حسابًا
            var gatewayViews = await _db.PageViewDailyStats.AsNoTracking()
                .Where(v => v.Day >= from && v.Day <= to && v.Path == KpiSnapshot.GatewayPath)
                .SumAsync(v => (int?)v.Views, ct) ?? 0;
            var requests = await _db.PublishRequests.AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
                .Select(r => new { r.Status, r.CreatedAt, r.ClosedAt })
                .ToListAsync(ct);
            var requestHours = requests.Where(r => r.ClosedAt.HasValue && r.ClosedAt.Value >= r.CreatedAt)
                .Select(r => (r.ClosedAt!.Value - r.CreatedAt).TotalHours).ToList();
            var newOrganizations = await _db.Organizations.AsNoTracking().CountAsync(o => o.CreatedAt >= start && o.CreatedAt < end, ct);

            // فرص/يوم: المنشورة بتاريخ نشر داخل المدة
            var publishDates = await _db.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published && o.PublishDate >= start && o.PublishDate < end)
                .Select(o => o.PublishDate)
                .ToListAsync(ct);
            var perDay = Enumerable.Range(0, days).Select(i => from.AddDays(i))
                .Select(d => (Day: d, Count: publishDates.Count(p => DateOnly.FromDateTime(p) == d)))
                .ToList();

            // زمن المراجعة: ReviewedAt − SubmittedAt (الفرص التي دخلت الطابور بعد المرحلة 5)؛ المرفوضة القديمة: ReviewedAt − PublishDate
            var reviewed = await _db.Opportunities.AsNoTracking()
                .Where(o => o.ReviewedAt != null && o.ReviewedAt >= start && o.ReviewedAt < end)
                .Select(o => new { o.Status, o.ReviewedAt, o.SubmittedAt, o.PublishDate })
                .ToListAsync(ct);
            var reviewHours = new List<double>();
            foreach (var o in reviewed)
            {
                var submitted = o.SubmittedAt ?? (o.Status == OpportunityStatus.Rejected ? o.PublishDate : (DateTime?)null);
                if (submitted == null) continue;
                var hours = (o.ReviewedAt!.Value - submitted.Value).TotalHours;
                if (hours >= 0) reviewHours.Add(hours);
            }
            var pendingSince = await _db.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.PendingReview)
                .Select(o => o.SubmittedAt ?? o.PublishDate)
                .ToListAsync(ct);
            var drafts = await _db.Opportunities.AsNoTracking().CountAsync(o => o.Status == OpportunityStatus.Draft, ct);

            // المحتوى الأسبوعي: مقالات + أدلة تقديم
            var articles = await _db.Articles.AsNoTracking().CountAsync(a => a.PublishDate >= start && a.PublishDate < end, ct);
            var guides = await _db.ApplicationGuides.AsNoTracking().CountAsync(g => g.IsPublished && g.CreatedAt >= start && g.CreatedAt < end, ct);

            return new KpiSnapshot
            {
                From = from, To = to,
                NewUsers = newUsers, NewUsersConfirmed = newUsersConfirmed, NewUsersPrevious = newUsersPrevious,
                CompassProfiles = compassProfiles, CompassStarts = Ev("compass.start"), CompassResults = Ev("compass.result"),
                GatewayViews = gatewayViews,
                PublishRequests = requests.Count,
                PublishRequestsPublished = requests.Count(r => r.Status == PublishRequestStatus.Published),
                PublishRequestsRejected = requests.Count(r => r.Status == PublishRequestStatus.Rejected),
                PublishRequestMedianHours = Median(requestHours),
                NewOrganizations = newOrganizations,
                OpportunitiesPublished = publishDates.Count, OpportunitiesPerDay = perDay,
                Reviewed = reviewed.Count, ReviewMedianHours = Median(reviewHours),
                Pending = pendingSince.Count,
                OldestPendingHours = pendingSince.Count == 0 ? null : Math.Max(0, (now - pendingSince.Min()).TotalHours),
                Drafts = drafts,
                ArticlesPublished = articles, GuidesAdded = guides
            };
        }

        /// <summary>الوسيط (أقل تأثرًا بطلب واحد نُسي أسابيع من المتوسط). null لقائمة فارغة.</summary>
        public static double? Median(IReadOnlyList<double> values)
        {
            if (values.Count == 0) return null;
            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }
    }
}
