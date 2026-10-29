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
    /// المشاريع الرائدة: صفحة عامة لكل مشروع /Programs/{slug}، وإدارة (قائمة/إضافة/تعديل/حذف) للأدمن.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ProgramsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUploadService _images;
        private readonly IProgramMenu _menu;
        private readonly IApplicationTracker _tracker;

        public ProgramsController(ApplicationDbContext context, IImageUploadService images, IProgramMenu menu, IApplicationTracker tracker)
        {
            _context = context;
            _images = images;
            _menu = menu;
            _tracker = tracker;
        }

        // ================= DETAILS (public) =================
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();

            var program = await _context.FlagshipPrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug);
            if (program == null) return NotFound();
            if (!program.IsPublished && !User.IsInRole("Admin")) return NotFound();

            // ترتيب المشروع بين المنشورة ("مشروع رائد 1 / 3")
            var publishedOrder = await _context.FlagshipPrograms
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
                .Select(p => p.Id)
                .ToListAsync();
            ViewBag.Position = publishedOrder.IndexOf(program.Id) + 1;
            ViewBag.Total = publishedOrder.Count;

            var activities = await _context.Activities
                .AsNoTracking()
                .Where(a => a.ProgramId == program.Id && a.IsPublished)
                .OrderByDescending(a => a.StartDate)
                .ToListAsync();
            ViewBag.Activities = activities;

            // 5-5 "سجّل اهتمامك": حالة المستخدم الحالي وعدد المهتمين
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ViewBag.IsInterested = userId != null && await _tracker.IsInterestedAsync(userId, program.Id);
            ViewBag.InterestCount = await _tracker.ProgramInterestCountAsync(program.Id);

            return View(program);
        }

        // ================= INDEX (admin) =================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var programs = await _context.FlagshipPrograms
                .AsNoTracking()
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
                .ToListAsync();
            return View(programs);
        }

        // ================= CREATE =================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var next = await _context.FlagshipPrograms.AnyAsync() ? await _context.FlagshipPrograms.MaxAsync(p => p.SortOrder) + 1 : 1;
            return View(new FlagshipProgram { SortOrder = next });
        }

        [HttpPost]
        public async Task<IActionResult> Create(FlagshipProgram program, IFormFile? HeroImage, List<IFormFile>? GalleryImages)
        {
            if (!await ApplyAsync(program, program, HeroImage, GalleryImages, null)) return View(program);

            program.CreatedAt = DateTime.Now;
            _context.FlagshipPrograms.Add(program);
            await _context.SaveChangesAsync();
            _menu.Invalidate();
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var program = await _context.FlagshipPrograms.FindAsync(id);
            if (program == null) return NotFound();
            return View(program);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, FlagshipProgram program, IFormFile? HeroImage, List<IFormFile>? GalleryImages, string[]? RemoveGallery)
        {
            var existing = await _context.FlagshipPrograms.FindAsync(id);
            if (existing == null) return NotFound();

            var wasOpen = existing.ApplicationsOpen;
            if (!await ApplyAsync(existing, program, HeroImage, GalleryImages, RemoveGallery))
            {
                program.Gallery = existing.Gallery; // لإعادة عرض المعرض الحالي مع الأخطاء
                return View(program);
            }

            // فتح الباب من جديد = دورة جديدة: كل المهتمين يستحقون تنبيهًا جديدًا
            if (!wasOpen && existing.ApplicationsOpen)
                await _context.ProgramInterests
                    .Where(p => p.ProgramId == existing.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.NotifiedAt, (DateTime?)null));

            await _context.SaveChangesAsync();
            _menu.Invalidate();
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var program = await _context.FlagshipPrograms.FindAsync(id);
            if (program != null)
            {
                _context.FlagshipPrograms.Remove(program);
                await _context.SaveChangesAsync();
                _menu.Invalidate();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>ينسخ النموذج إلى الكيان بعد التحقق والتعقيم ورفع الصور. false = أخطاء في ModelState.</summary>
        private async Task<bool> ApplyAsync(FlagshipProgram target, FlagshipProgram form, IFormFile? heroImage, List<IFormFile>? galleryImages, string[]? removeGallery)
        {
            // الـ slug: من الحقل أو من العنوان الإنجليزي، ويجب أن يكون فريدًا
            var slug = SlugHelper.Make(string.IsNullOrWhiteSpace(form.Slug) ? form.TitleEn : form.Slug, 60);
            if (string.IsNullOrEmpty(slug) || slug.Any(c => c > 127))
                ModelState.AddModelError(nameof(FlagshipProgram.Slug), "مقطع الرابط يجب أن يكون بحروف لاتينية وأرقام وشرطات فقط");
            else if (await _context.FlagshipPrograms.AnyAsync(p => p.Slug == slug && p.Id != target.Id))
                ModelState.AddModelError(nameof(FlagshipProgram.Slug), "مقطع الرابط مستخدم لمشروع آخر");
            ModelState.Remove(nameof(FlagshipProgram.Slug));

            if (!string.IsNullOrWhiteSpace(form.ExternalLink)
                && !(Uri.TryCreate(form.ExternalLink.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https")))
                ModelState.AddModelError(nameof(FlagshipProgram.ExternalLink), "الرابط يجب أن يبدأ بـ http:// أو https://");

            if (heroImage != null)
            {
                var error = _images.Validate(heroImage);
                if (error != null) ModelState.AddModelError("HeroImage", error);
            }
            foreach (var file in galleryImages ?? new List<IFormFile>())
            {
                var error = _images.Validate(file);
                if (error != null) ModelState.AddModelError("GalleryImages", $"{file.FileName}: {error}");
            }

            if (!ModelState.IsValid) return false;

            if (heroImage != null)
                target.HeroImagePath = await _images.SaveAsync(heroImage, "uploads");

            var gallery = target.Gallery.Where(g => removeGallery == null || !removeGallery.Contains(g)).ToList();
            foreach (var file in galleryImages ?? new List<IFormFile>())
                gallery.Add(await _images.SaveAsync(file, "uploads"));
            target.Gallery = gallery;

            target.Slug = slug!;
            target.SortOrder = form.SortOrder;
            target.IsPublished = form.IsPublished;
            target.ApplicationsOpen = form.ApplicationsOpen;
            target.MenuTitleAr = form.MenuTitleAr.Trim();
            target.MenuTitleEn = form.MenuTitleEn.Trim();
            target.MenuTitleFr = form.MenuTitleFr.Trim();
            target.TitleAr = form.TitleAr.Trim();
            target.TitleEn = form.TitleEn.Trim();
            target.TitleFr = form.TitleFr.Trim();
            target.SubtitleAr = Clean(form.SubtitleAr);
            target.SubtitleEn = Clean(form.SubtitleEn);
            target.SubtitleFr = Clean(form.SubtitleFr);
            target.HeadlineValue = Clean(form.HeadlineValue);
            target.HeadlineLabelAr = Clean(form.HeadlineLabelAr);
            target.HeadlineLabelEn = Clean(form.HeadlineLabelEn);
            target.HeadlineLabelFr = Clean(form.HeadlineLabelFr);
            target.IntroAr = SafeHtml.Clean(form.IntroAr);
            target.IntroEn = SafeHtml.Clean(form.IntroEn);
            target.IntroFr = SafeHtml.Clean(form.IntroFr);
            target.OutcomesHeadingAr = Clean(form.OutcomesHeadingAr);
            target.OutcomesHeadingEn = Clean(form.OutcomesHeadingEn);
            target.OutcomesHeadingFr = Clean(form.OutcomesHeadingFr);
            target.OutcomesAr = string.IsNullOrWhiteSpace(form.OutcomesAr) ? null : SafeHtml.Clean(form.OutcomesAr);
            target.OutcomesEn = string.IsNullOrWhiteSpace(form.OutcomesEn) ? null : SafeHtml.Clean(form.OutcomesEn);
            target.OutcomesFr = string.IsNullOrWhiteSpace(form.OutcomesFr) ? null : SafeHtml.Clean(form.OutcomesFr);
            target.PartnersAr = Clean(form.PartnersAr);
            target.PartnersEn = Clean(form.PartnersEn);
            target.PartnersFr = Clean(form.PartnersFr);
            target.ExternalLink = Clean(form.ExternalLink);
            target.Stats = form.Stats.Where(s => !s.IsEmpty).ToList();
            target.Timeline = form.Timeline.Where(m => !m.IsEmpty).ToList();
            return true;
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
