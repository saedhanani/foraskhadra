using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    public interface INotificationSweeper
    {
        /// <summary>يفحص المواعيد والاهتمامات والبحوث المحفوظة ويولّد الإشعارات. يعيد عدد الإشعارات الجديدة.</summary>
        Task<int> SweepAsync(CancellationToken ct = default);
    }

    /// <summary>مولّد الإشعارات الداخلية — يستدعيه NotificationSweepHostedService دوريًا.
    /// النص لا يُخزَّن هنا؛ يُصاغ بلغة الواجهة وقت العرض في _NotificationMenu.</summary>
    public class NotificationSweeper : INotificationSweeper
    {
        private readonly ApplicationDbContext _db;

        /// <summary>نافذة "ينتهي قريبًا" بالأيام — نفس نافذة شارة IsDeadlineSoon.</summary>
        public const int DeadlineWindowDays = 7;

        public NotificationSweeper(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int> SweepAsync(CancellationToken ct = default)
        {
            var created = 0;
            created += await SweepSavedDeadlinesAsync(ct);
            created += await SweepInterestsAsync(ct);
            created += await SweepSavedSearchesAsync(ct);
            if (created > 0 || _db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync(ct);
            return created;
        }

        /// <summary>فرصة محفوظة دخل موعدها نافذة الأيام السبعة → إشعار واحد لكل (مستخدم، فرصة).</summary>
        private async Task<int> SweepSavedDeadlinesAsync(CancellationToken ct)
        {
            var today = DateTime.Now.Date;
            var windowEnd = today.AddDays(DeadlineWindowDays);

            var due = await _db.SavedOpportunities
                .Where(s => s.Opportunity!.Status == OpportunityStatus.Published
                            && s.Opportunity.DeadlineType == DeadlineType.SpecificDate
                            && s.Opportunity.EndDate != null
                            && s.Opportunity.EndDate >= today
                            && s.Opportunity.EndDate <= windowEnd
                            && !_db.Notifications.Any(n => n.UserId == s.UserId
                                                           && n.Kind == NotificationKind.SavedDeadlineSoon
                                                           && n.OpportunityId == s.OpportunityId))
                .Select(s => new { s.UserId, s.OpportunityId })
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var s in due)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = s.UserId,
                    Kind = NotificationKind.SavedDeadlineSoon,
                    OpportunityId = s.OpportunityId
                });
            }

            return due.Count;
        }

        /// <summary>فرص نُشرت بعد آخر تنبيه وتطابق اهتمامات المستخدم → إشعار مجمّع واحد.
        /// أول مسح للمستخدم يضبط خط الأساس فقط حتى لا يُغرَق بالقديم.</summary>
        private async Task<int> SweepInterestsAsync(CancellationToken ct)
        {
            var now = DateTime.Now;
            var created = 0;

            var users = await _db.Users
                .Where(u => u.InterestsJson != null && u.InterestsJson != "" && u.InterestsJson != "[]")
                .ToListAsync(ct);

            foreach (var user in users)
            {
                if (user.LastInterestNotifiedAt == null)
                {
                    user.LastInterestNotifiedAt = now; // خط أساس — بلا إشعار
                    continue;
                }

                var types = InterestMap.TypesFor(user.Interests);
                if (types.Count == 0) continue;

                var since = user.LastInterestNotifiedAt.Value;
                var count = await _db.Opportunities
                    .CountAsync(o => o.Status == OpportunityStatus.Published
                                     && o.PublishDate > since
                                     && o.Type != null && types.Contains(o.Type.Value)
                                     && (o.EndDate == null || o.EndDate >= now), ct);

                if (count == 0) continue;

                _db.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Kind = NotificationKind.InterestMatches,
                    Count = count
                });
                user.LastInterestNotifiedAt = now;
                created++;
            }

            return created;
        }

        /// <summary>فرص نُشرت بعد آخر فحص وتطابق بحثًا محفوظًا → إشعار مجمّع برابط البحث نفسه.</summary>
        private async Task<int> SweepSavedSearchesAsync(CancellationToken ct)
        {
            var now = DateTime.Now;
            var created = 0;

            var searches = await _db.SavedSearches.ToListAsync(ct);

            foreach (var search in searches)
            {
                var query = _db.Opportunities
                    .Where(o => o.Status == OpportunityStatus.Published
                                && o.PublishDate > search.LastRunAt
                                && (o.EndDate == null || o.EndDate >= now));

                foreach (var term in SearchIndexer.Terms(search.Query))
                {
                    var pattern = $"%{term}%";
                    query = query.Where(o => EF.Functions.Like(o.SearchText, pattern));
                }

                var types = ParseTypes(search.TypesCsv);
                if (types.Count > 0)
                    query = query.Where(o => o.Type != null && types.Contains(o.Type.Value));

                if (!string.IsNullOrEmpty(search.Country))
                    query = query.Where(o => o.AvailableCountries.Any(c =>
                        c.NameEn == search.Country || c.NameAr == search.Country || c.NameFr == search.Country));

                var count = await query.CountAsync(ct);
                search.LastRunAt = now;
                if (count == 0) continue;

                _db.Notifications.Add(new Notification
                {
                    UserId = search.UserId,
                    Kind = NotificationKind.SavedSearchMatches,
                    Count = count,
                    Label = BuildLabel(search),
                    LinkParams = BuildLinkParams(search)
                });
                created++;
            }

            return created;
        }

        public static List<OpportunityType> ParseTypes(string? typesCsv) =>
            (typesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => Enum.TryParse<OpportunityType>(t, out _))
                .Select(Enum.Parse<OpportunityType>)
                .Distinct()
                .ToList();

        /// <summary>وصف قصير للبحث يظهر في نص الإشعار وفي قائمة البحوث المحفوظة.</summary>
        public static string BuildLabel(SavedSearch s)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(s.Query)) parts.Add(s.Query.Trim());
            if (!string.IsNullOrEmpty(s.Country)) parts.Add(s.Country);
            return parts.Count > 0 ? string.Join(" · ", parts) : s.TypesCsv.Replace(",", " · ");
        }

        /// <summary>معاملات رابط صفحة كل الفرص المطابقة للبحث (بلا علامة الاستفهام).</summary>
        public static string BuildLinkParams(SavedSearch s)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(s.Query))
                parts.Add("search=" + Uri.EscapeDataString(s.Query.Trim()));
            foreach (var t in ParseTypes(s.TypesCsv))
                parts.Add("type=" + t);
            if (!string.IsNullOrEmpty(s.Country))
                parts.Add("country=" + Uri.EscapeDataString(s.Country));
            return string.Join("&", parts);
        }
    }
}
