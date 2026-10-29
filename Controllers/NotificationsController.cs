using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>جرس الإشعارات: القائمة المنسدلة وتعليم المقروء.</summary>
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Notifications/Menu — محتوى القائمة المنسدلة (يُجلب عند فتح الجرس)
        [HttpGet]
        public async Task<IActionResult> Menu()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var items = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(12)
                .Include(n => n.Opportunity)
                .AsNoTracking()
                .ToListAsync();

            return PartialView("_NotificationMenu", items);
        }

        // POST: /Notifications/MarkAllRead — يُستدعى بعد فتح القائمة
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _db.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.Now));

            return Ok();
        }
    }
}
