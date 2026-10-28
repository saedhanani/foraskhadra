using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// نشاطات فرص خضراء: الإدارة (قائمة/إضافة/تعديل/حذف) للأدمن فقط، وصفحة التفاصيل عامة.
    /// القائمة العامة في Home/News (رابط "نشاطاتنا" في القائمة).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUploadService _images;

        public ActivitiesController(ApplicationDbContext context, IImageUploadService images)
        {
            _context = context;
            _images = images;
        }

        // ================= INDEX (admin) =================
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(1, page);

            var query = _context.Activities.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.TitleAr.Contains(search) || a.TitleEn.Contains(search) || a.TitleFr.Contains(search));

            int totalItems = await query.CountAsync();
            var activities = await query
                .OrderByDescending(a => a.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            ViewBag.Search = search;
            return View(activities);
        }

        // ================= DETAILS (public) =================
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var activity = await _context.Activities.AsNoTracking().Include(a => a.Program).FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null) return NotFound();
            // غير المنشور يراه الأدمن فقط
            if (!activity.IsPublished && !User.IsInRole("Admin")) return NotFound();

            return View(activity);
        }

        /// <summary>قائمة المشاريع الرائدة لحقل "يتبع المشروع" في النموذج.</summary>
        private async Task LoadProgramsAsync()
        {
            ViewBag.Programs = await _context.FlagshipPrograms
                .AsNoTracking()
                .OrderBy(p => p.SortOrder)
                .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = p.Id.ToString(), Text = p.MenuTitleAr })
                .ToListAsync();
        }

        // ================= CREATE =================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadProgramsAsync();
            return View(new Activity());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Activity activity, IFormFile? Image)
        {
            if (!await ApplyAsync(activity, activity, Image))
            {
                await LoadProgramsAsync();
                return View(activity);
            }

            activity.CreatedAt = DateTime.Now;
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return NotFound();
            await LoadProgramsAsync();
            return View(activity);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Activity activity, IFormFile? Image)
        {
            var existing = await _context.Activities.FindAsync(id);
            if (existing == null) return NotFound();

            if (!await ApplyAsync(existing, activity, Image))
            {
                await LoadProgramsAsync();
                return View(activity);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE (بدون صفحة) =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity != null)
            {
                _context.Activities.Remove(activity);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>ينسخ حقول النموذج إلى الكيان بعد التحقق والتعقيم ورفع الصورة. false = أخطاء في ModelState.</summary>
        private async Task<bool> ApplyAsync(Activity target, Activity form, IFormFile? image)
        {
            if (form.EndDate.HasValue && form.EndDate.Value.Date < form.StartDate.Date)
                ModelState.AddModelError(nameof(Activity.EndDate), "تاريخ النهاية قبل تاريخ البداية");

            if (!string.IsNullOrWhiteSpace(form.ExternalLink)
                && !(Uri.TryCreate(form.ExternalLink.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https")))
                ModelState.AddModelError(nameof(Activity.ExternalLink), "الرابط يجب أن يبدأ بـ http:// أو https://");

            if (image != null)
            {
                var error = _images.Validate(image);
                if (error != null) ModelState.AddModelError("Image", error);
            }

            if (!ModelState.IsValid) return false;

            if (image != null)
                target.ImagePath = await _images.SaveAsync(image, "uploads");

            target.TitleAr = form.TitleAr.Trim();
            target.TitleEn = form.TitleEn.Trim();
            target.TitleFr = form.TitleFr.Trim();
            // تعقيم HTML القادم من المحرر قبل التخزين
            target.DescriptionAr = SafeHtml.Clean(form.DescriptionAr);
            target.DescriptionEn = SafeHtml.Clean(form.DescriptionEn);
            target.DescriptionFr = SafeHtml.Clean(form.DescriptionFr);
            target.StartDate = form.StartDate.Date;
            target.EndDate = form.EndDate?.Date;
            target.Location = string.IsNullOrWhiteSpace(form.Location) ? null : form.Location.Trim();
            target.ExternalLink = string.IsNullOrWhiteSpace(form.ExternalLink) ? null : form.ExternalLink.Trim();
            target.IsPublished = form.IsPublished;
            target.ProgramId = form.ProgramId;
            return true;
        }
    }
}
