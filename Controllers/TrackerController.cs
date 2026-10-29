using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Foras_Khadra.Controllers
{
    /// <summary>تتبّع التقديم (2-1) ورفيق التقديم (2-2) على الفرص المحفوظة. كل الإجراءات تخص المستخدم الحالي فقط.</summary>
    [Authorize]
    public class TrackerController : Controller
    {
        private readonly IApplicationTracker _tracker;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrackerController(IApplicationTracker tracker, UserManager<ApplicationUser> userManager)
        {
            _tracker = tracker;
            _userManager = userManager;
        }

        // POST: /Tracker/SetStatus — من قائمة الحالة على بطاقة الفرصة المحفوظة
        [HttpPost]
        public async Task<IActionResult> SetStatus(int id, ApplicationStatus status, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (Enum.IsDefined(status))
                await _tracker.SetStatusAsync(userId, id, status);

            return Back(returnUrl);
        }

        // GET: /Tracker/Prepare/5 — صفحة رفيق التقديم لفرصة محفوظة
        [HttpGet]
        public async Task<IActionResult> Prepare(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var saved = await _tracker.GetWithChecklistAsync(userId, id);
            if (saved == null)
                return RedirectToAction("Details", "Opportunity", new { id });

            return View(saved);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleItem(int id, int itemId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _tracker.ToggleItemAsync(userId, itemId);
            return RedirectToAction(nameof(Prepare), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(int id, string text)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _tracker.AddItemAsync(userId, id, text);
            return RedirectToAction(nameof(Prepare), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int id, int itemId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _tracker.RemoveItemAsync(userId, itemId);
            return RedirectToAction(nameof(Prepare), new { id });
        }

        private IActionResult Back(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Dashboard", "User");
    }
}
