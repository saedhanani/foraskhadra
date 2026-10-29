using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    public interface IPushDispatcher
    {
        /// <summary>يرسل إشعارات الجرس التي لم تُعالج بعد كإشعارات دفع لأجهزة أصحابها. يعيد عدد الإشعارات المرسلة.</summary>
        Task<int> DispatchPendingAsync(CancellationToken ct = default);

        /// <summary>إشعار "تم تفعيل التنبيهات ✓" فور الاشتراك — يؤكد للمستخدم أن السلسلة تعمل.</summary>
        Task<PushResult> SendWelcomeAsync(PushSubscription subscription, CancellationToken ct = default);
    }

    /// <summary>يحوّل إشعارات NotificationSweeper إلى إشعارات دفع. يستدعيه NotificationSweepHostedService كل 20 دقيقة.
    /// لا يرسل ليلًا بتوقيت المستخدم (يؤجّل للدورة التالية)، ولا أكثر من MaxPerUserPerRun في الدورة، ويتجاوز ما مضى عليه يومان.</summary>
    public class PushDispatcher : IPushDispatcher
    {
        public const int MaxAgeHours = 48;
        public const int MaxPerUserPerRun = 3;
        public const int AwakeFromHour = 8;
        public const int AwakeUntilHour = 22;

        private readonly ApplicationDbContext _db;
        private readonly IWebPushSender _sender;

        public PushDispatcher(ApplicationDbContext db, IWebPushSender sender)
        {
            _db = db;
            _sender = sender;
        }

        /// <summary>هل الساعة الآن بين 8 صباحًا و10 مساءً بتوقيت جهاز المشترك؟</summary>
        public static bool IsAwake(PushSubscription subscription, DateTime utcNow)
        {
            var localHour = utcNow.AddMinutes(-subscription.TzOffsetMinutes).Hour;
            return localHour >= AwakeFromHour && localHour < AwakeUntilHour;
        }

        public async Task<int> DispatchPendingAsync(CancellationToken ct = default)
        {
            var pending = await _db.Notifications
                .Where(n => n.PushedAt == null)
                .Include(n => n.Opportunity)
                .OrderBy(n => n.CreatedAt)
                .Take(500)
                .ToListAsync(ct);
            if (pending.Count == 0) return 0;

            var userIds = pending.Select(n => n.UserId).Distinct().ToList();
            var subscriptions = (await _db.PushSubscriptions.Where(s => userIds.Contains(s.UserId)).ToListAsync(ct))
                .GroupBy(s => s.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var staleBefore = now.AddHours(-MaxAgeHours);
            var sent = 0;

            foreach (var userNotifications in pending.GroupBy(n => n.UserId))
            {
                if (!subscriptions.TryGetValue(userNotifications.Key, out var userSubs))
                {
                    // لا أجهزة مشتركة: يكفي الجرس داخل الموقع
                    foreach (var n in userNotifications) n.PushedAt = now;
                    continue;
                }

                var perUser = 0;
                foreach (var n in userNotifications)
                {
                    var awake = userSubs.Where(s => IsAwake(s, utcNow)).ToList();
                    if (n.CreatedAt < staleBefore || perUser >= MaxPerUserPerRun)
                    {
                        n.PushedAt = now; // قديم أو كثير: يبقى في الجرس فقط
                        continue;
                    }
                    if (awake.Count == 0) continue; // ليل عند المستخدم: نعيد المحاولة في الدورة التالية

                    var delivered = false;
                    foreach (var subscription in awake)
                    {
                        var message = NotificationText.For(n, subscription.Lang);
                        if (message == null) break;

                        var result = await _sender.SendAsync(subscription, message.ToJson(), ct);
                        if (result == PushResult.Sent)
                        {
                            subscription.LastSentAt = now;
                            delivered = true;
                        }
                        else if (result == PushResult.Gone)
                        {
                            _db.PushSubscriptions.Remove(subscription);
                            userSubs.Remove(subscription);
                        }
                    }

                    n.PushedAt = now;
                    perUser++;
                    if (delivered) sent++;
                }
            }

            await _db.SaveChangesAsync(ct);
            return sent;
        }

        public async Task<PushResult> SendWelcomeAsync(PushSubscription subscription, CancellationToken ct = default)
        {
            var result = await _sender.SendAsync(subscription, NotificationText.Welcome(subscription.Lang).ToJson(), ct);
            if (result == PushResult.Sent) subscription.LastSentAt = DateTime.Now;
            return result;
        }
    }
}
