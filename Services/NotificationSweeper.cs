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

        /// <summary>تذكير اليوم الأخير: ما ينتهي اليوم أو غدًا.</summary>
        public const int LastDayWindowDays = 1;

        public NotificationSweeper(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int> SweepAsync(CancellationToken ct = default)
        {
            var created = 0;
            created += await SweepSavedDeadlinesAsync(ct);
            created += await SweepLastDayAsync(ct);
            created += await SweepInterestsAsync(ct);
            created += await SweepSavedSearchesAsync(ct);
            created += await SweepCycleWatchesAsync(ct);
            created += await SweepProgramInterestsAsync(ct);
            created += await SweepWeeklySummariesAsync(ct);
            if (created > 0 || _db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync(ct);
            return created;
        }

        /// <summary>فرصة محفوظة دخل موعدها نافذة الأيام السبعة → إشعار واحد لكل (مستخدم، فرصة).
        /// ما ينتهي اليوم أو غدًا يأخذ تذكير اليوم الأخير وحده (SweepLastDayAsync) حتى لا يصل تذكيران معًا.</summary>
        private async Task<int> SweepSavedDeadlinesAsync(CancellationToken ct)
        {
            var today = DateTime.Now.Date;
            var windowStart = today.AddDays(LastDayWindowDays + 1);
            var windowEnd = today.AddDays(DeadlineWindowDays);

            // من قدّم فعلًا لا يحتاج تذكيرًا بالموعد
            var due = await _db.SavedOpportunities
                .Where(s => s.Status == ApplicationStatus.NotApplied
                            && s.Opportunity!.Status == OpportunityStatus.Published
                            && s.Opportunity.DeadlineType == DeadlineType.SpecificDate
                            && s.Opportunity.EndDate != null
                            && s.Opportunity.EndDate >= windowStart
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

        /// <summary>فرصة محفوظة لم يُقدَّم عليها وينتهي موعدها اليوم أو غدًا → تذكير أخير واحد لكل (مستخدم، فرصة).</summary>
        private async Task<int> SweepLastDayAsync(CancellationToken ct)
        {
            var today = DateTime.Now.Date;
            var windowEnd = today.AddDays(LastDayWindowDays);

            var due = await _db.SavedOpportunities
                .Where(s => s.Status == ApplicationStatus.NotApplied
                            && s.Opportunity!.Status == OpportunityStatus.Published
                            && s.Opportunity.DeadlineType == DeadlineType.SpecificDate
                            && s.Opportunity.EndDate != null
                            && s.Opportunity.EndDate >= today
                            && s.Opportunity.EndDate <= windowEnd
                            && !_db.Notifications.Any(n => n.UserId == s.UserId
                                                           && n.Kind == NotificationKind.SavedDeadlineLastDay
                                                           && n.OpportunityId == s.OpportunityId))
                .Select(s => new { s.UserId, s.OpportunityId })
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var s in due)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = s.UserId,
                    Kind = NotificationKind.SavedDeadlineLastDay,
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

        /// <summary>مراقبة الدورة القادمة (3-3): تنبيه واحد إذا أُعيد فتح الفرصة نفسها، أو نُشرت بعد المراقبة فرصة جديدة بعنوان مطابق.</summary>
        private async Task<int> SweepCycleWatchesAsync(CancellationToken ct)
        {
            var today = DateTime.Now.Date;
            var created = 0;

            var watches = await _db.CycleWatches
                .Where(w => w.NotifiedAt == null)
                .Include(w => w.Opportunity)
                .ToListAsync(ct);

            foreach (var watch in watches)
            {
                var old = watch.Opportunity;
                if (old == null) continue;

                int? reopenedId = null;

                // (أ) الفرصة نفسها عادت مفتوحة: موعدها صار في المستقبل، أو أُزيل الموعد المحدد
                // (المراقبة لا تُنشأ إلا على فرصة منتهية الموعد، فأي من الحالتين يعني إعادة فتح)
                var reopenedItself = old.Status == OpportunityStatus.Published
                    && (old.DeadlineType != DeadlineType.SpecificDate
                        || (old.EndDate.HasValue && old.EndDate.Value.Date >= today));
                if (reopenedItself)
                    reopenedId = old.Id;

                // (ب) فرصة جديدة بعنوان مطابق نُشرت بعد بدء المراقبة
                if (reopenedId == null)
                {
                    var terms = TitleTerms(old.TitleAr);
                    if (terms.Count > 0)
                    {
                        var q = _db.Opportunities
                            .Where(o => o.Id != old.Id
                                        && o.Status == OpportunityStatus.Published
                                        && o.PublishDate > watch.CreatedAt
                                        && (o.EndDate == null || o.EndDate >= today));
                        foreach (var term in terms)
                        {
                            var pattern = $"%{term}%";
                            q = q.Where(o => EF.Functions.Like(o.SearchText, pattern));
                        }
                        reopenedId = await q.OrderByDescending(o => o.PublishDate).Select(o => (int?)o.Id).FirstOrDefaultAsync(ct);
                    }
                }

                if (reopenedId == null) continue;

                _db.Notifications.Add(new Notification
                {
                    UserId = watch.UserId,
                    Kind = NotificationKind.CycleReopened,
                    OpportunityId = reopenedId,
                    Label = old.TitleAr
                });
                watch.NotifiedAt = DateTime.Now;
                created++;
            }

            return created;
        }

        /// <summary>اهتمام ببرنامج (5-5): حين يكون باب التقديم مفتوحًا، كل مهتم لم يُنبَّه بعد يأخذ تنبيهًا واحدًا.</summary>
        private async Task<int> SweepProgramInterestsAsync(CancellationToken ct)
        {
            var due = await _db.ProgramInterests
                .Where(p => p.NotifiedAt == null && p.Program!.IsPublished && p.Program.ApplicationsOpen)
                .Include(p => p.Program)
                .ToListAsync(ct);

            foreach (var p in due)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = p.UserId,
                    Kind = NotificationKind.ProgramOpened,
                    Label = p.Program!.TitleAr,
                    LinkParams = p.Program.Slug
                });
                p.NotifiedAt = DateTime.Now;
            }

            return due.Count;
        }

        /// <summary>ملخص الأسبوع (5-1): إشعار واحد كل 7 أيام لمن لديه اهتمامات أو محفوظات، ولا يُرسل إن لم يكن ثمة ما يُقال.</summary>
        private async Task<int> SweepWeeklySummariesAsync(CancellationToken ct)
        {
            var now = DateTime.Now;
            var weekAgo = now.AddDays(-WeeklySummaryService.Days);
            var today = now.Date;
            var created = 0;

            var users = await _db.Users
                .Where(u => (u.InterestsJson != null && u.InterestsJson != "" && u.InterestsJson != "[]")
                            || _db.SavedOpportunities.Any(s => s.UserId == u.Id))
                .ToListAsync(ct);

            foreach (var user in users)
            {
                var lastSummary = await _db.Notifications
                    .Where(n => n.UserId == user.Id && n.Kind == NotificationKind.WeeklySummary)
                    .MaxAsync(n => (DateTime?)n.CreatedAt, ct);
                if (lastSummary.HasValue && lastSummary.Value > weekAgo) continue;

                var types = InterestMap.TypesFor(user.Interests);
                var newMatching = types.Count == 0 ? 0 : await _db.Opportunities.CountAsync(o =>
                    o.Status == OpportunityStatus.Published && o.PublishDate >= weekAgo
                    && o.Type != null && types.Contains(o.Type.Value)
                    && (o.EndDate == null || o.EndDate >= today), ct);

                var deadlines = await _db.SavedOpportunities.CountAsync(s =>
                    s.UserId == user.Id && s.Status == ApplicationStatus.NotApplied
                    && s.Opportunity!.Status == OpportunityStatus.Published
                    && s.Opportunity.DeadlineType == DeadlineType.SpecificDate
                    && s.Opportunity.EndDate >= today && s.Opportunity.EndDate <= today.AddDays(WeeklySummaryService.Days), ct);

                if (newMatching == 0 && deadlines == 0) continue;

                _db.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Kind = NotificationKind.WeeklySummary,
                    Count = newMatching
                });
                created++;
            }

            return created;
        }

        /// <summary>أطول 4 كلمات ذات معنى من العنوان (3 أحرف فأكثر) — بصمة لمطابقة الدورة الجديدة.</summary>
        public static List<string> TitleTerms(string? title) =>
            SearchIndexer.Terms(title)
                .Where(t => t.Length >= 3)
                .OrderByDescending(t => t.Length)
                .Take(4)
                .ToList();

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
