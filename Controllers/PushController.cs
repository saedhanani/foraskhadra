using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>اشتراكات إشعارات الدفع — تستدعيها بطاقة "فعّل التنبيهات" (Views/Shared/_PushCard.cshtml و site.js) برمز CSRF في الترويسة.</summary>
    [Authorize]
    public class PushController : Controller
    {
        /// <summary>حدّ الأجهزة لكل مستخدم — الأقدم يُحذف.</summary>
        public const int MaxSubscriptionsPerUser = 10;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPushDispatcher _dispatcher;

        public PushController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IPushDispatcher dispatcher)
        {
            _db = db;
            _userManager = userManager;
            _dispatcher = dispatcher;
        }

        public record SubscribeRequest(string? Endpoint, string? P256dh, string? Auth, int? Tz);
        public record UnsubscribeRequest(string? Endpoint);

        // POST: /Push/Subscribe — يحفظ اشتراك هذا الجهاز (أو يحدّثه) ويرسل إشعار ترحيب لأول مرة
        [HttpPost]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();
            if (!PushEndpoints.IsAllowed(request.Endpoint) || !PushEndpoints.KeysAreValid(request.P256dh, request.Auth))
                return BadRequest();

            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var tz = Math.Clamp(request.Tz ?? 0, -840, 840);

            var subscription = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);
            var isNew = subscription == null || subscription.UserId != userId;
            if (subscription == null)
            {
                subscription = new PushSubscription { Endpoint = request.Endpoint! };
                _db.PushSubscriptions.Add(subscription);
            }
            subscription.UserId = userId;
            subscription.P256dh = request.P256dh!;
            subscription.Auth = request.Auth!;
            subscription.Lang = lang;
            subscription.TzOffsetMinutes = tz;
            if (isNew) subscription.CreatedAt = DateTime.Now;

            // حدّ الأجهزة: احذف الأقدم
            var others = await _db.PushSubscriptions
                .Where(s => s.UserId == userId && s.Endpoint != request.Endpoint)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            _db.PushSubscriptions.RemoveRange(others.Skip(MaxSubscriptionsPerUser - 1));

            await _db.SaveChangesAsync();

            if (!isNew) return Json(new { ok = true });

            var result = await _dispatcher.SendWelcomeAsync(subscription);
            if (result == PushResult.Gone)
            {
                _db.PushSubscriptions.Remove(subscription);
                await _db.SaveChangesAsync();
                return BadRequest();
            }
            await _db.SaveChangesAsync();
            return Json(new { ok = true, welcome = result == PushResult.Sent });
        }

        // POST: /Push/Unsubscribe — زر "إيقاف" على هذا الجهاز
        [HttpPost]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _db.PushSubscriptions
                .Where(s => s.UserId == userId && s.Endpoint == request.Endpoint)
                .ExecuteDeleteAsync();
            return Json(new { ok = true });
        }
    }
}
