using System.Globalization;
using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    public sealed record GuideCard(ApplicationGuide Guide, int OpenCount);
    public sealed record GuideDetailsVM(ApplicationGuide Guide, List<Opportunity> Open, Opportunity? LastCycle, bool IsMember);

    /// <summary>
    /// أدلة التقديم للبرامج المتكررة (المرحلة 4.1): /guides و/guides/{slug} بثلاث لغات، مرتبطة بـOpportunity.ProgramKey.
    /// «حمّل القائمة إلى متابعتي» يحفظ أحدث فرصة من البرنامج ويفتح رفيق التقديم بمهام الدليل.
    /// </summary>
    public class GuidesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISavedOpportunityService _saved;
        private readonly IApplicationTracker _tracker;

        public GuidesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ISavedOpportunityService saved, IApplicationTracker tracker)
        {
            _db = db;
            _userManager = userManager;
            _saved = saved;
            _tracker = tracker;
        }

        private static string Lang => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private static string T(string ar, string en, string fr) => Lang == "en" ? en : Lang == "fr" ? fr : ar;

        [AllowAnonymous]
        [HttpGet("/{culture:culture}/guides")]
        [HttpGet("/guides", Order = 1)]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now.Date;
            var guides = await _db.ApplicationGuides.AsNoTracking().Where(g => g.IsPublished).OrderBy(g => g.SortOrder).ThenBy(g => g.TitleAr).ToListAsync();
            var keys = guides.Select(g => g.ProgramKey).ToList();
            var open = await _db.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published && o.ProgramKey != null && keys.Contains(o.ProgramKey)
                    && (o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today))
                .GroupBy(o => o.ProgramKey!)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            ViewData["Title"] = T("أدلة التقديم على البرامج الكبرى", "Application guides for the major programs", "Guides de candidature aux grands programmes");
            ViewData["Description"] = T("شروط بالعربية، جدول زمني، نموذج خطاب، أخطاء شائعة، ونصائح فائزين من الوطن العربي لتشيفنينغ وDAAD وإيراسموس والمنح التركية وMEXT وفولبرايت وغيرها.",
                "Conditions, timeline, letter template, common mistakes and tips from winners in the Arab world for Chevening, DAAD, Erasmus Mundus, Türkiye Scholarships, MEXT, Fulbright and more.",
                "Conditions, calendrier, modèle de lettre, erreurs fréquentes et conseils de lauréats du monde arabe pour Chevening, DAAD, Erasmus Mundus, les bourses turques, MEXT, Fulbright et d'autres.");
            return View(guides.Select(g => new GuideCard(g, open.GetValueOrDefault(g.ProgramKey))).ToList());
        }

        [AllowAnonymous]
        [HttpGet("/{culture:culture}/guides/{slug}")]
        [HttpGet("/guides/{slug}", Order = 1)]
        public async Task<IActionResult> Details(string slug)
        {
            var guide = await _db.ApplicationGuides.AsNoTracking().FirstOrDefaultAsync(g => g.Slug == slug && g.IsPublished);
            if (guide == null) return NotFound();

            var today = DateTime.Now.Date;
            var cycles = await _db.Opportunities.AsNoTracking()
                .Include(o => o.AvailableCountries)
                .Where(o => o.Status == OpportunityStatus.Published && o.ProgramKey == guide.ProgramKey)
                .OrderByDescending(o => o.PublishDate)
                .Take(12)
                .ToListAsync();
            var open = cycles.Where(o => o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today).ToList();
            var last = open.Count == 0 ? cycles.FirstOrDefault() : null;

            ViewData["Title"] = guide.Title(Lang);
            ViewData["Description"] = guide.Summary(Lang) ?? guide.Title(Lang);
            ViewData["Canonical"] = $"{Request.Scheme}://{Request.Host}" + (Lang == "ar" ? "" : "/" + Lang) + "/guides/" + guide.Slug;
            ViewBag.Tracked = TempData["GuideTracked"] as string;
            return View(new GuideDetailsVM(guide, open, last, User.IsInRole("User")));
        }

        /// <summary>يحفظ أحدث فرصة من البرنامج (إن لم تكن محفوظة) ويفتح رفيق التقديم؛ مهام الدليل تُنشأ هناك مع القائمة.</summary>
        [Authorize(Roles = "User")]
        [HttpPost("/{culture:culture}/guides/{slug}/track")]
        [HttpPost("/guides/{slug}/track", Order = 1)]
        public async Task<IActionResult> Track(string slug)
        {
            var guide = await _db.ApplicationGuides.AsNoTracking().FirstOrDefaultAsync(g => g.Slug == slug && g.IsPublished);
            if (guide == null) return NotFound();
            var userId = _userManager.GetUserId(User)!;
            var today = DateTime.Now.Date;

            var target = await _db.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published && o.ProgramKey == guide.ProgramKey)
                .OrderByDescending(o => o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today)
                .ThenByDescending(o => o.PublishDate)
                .Select(o => new { o.Id })
                .FirstOrDefaultAsync();
            if (target == null)
            {
                TempData["GuideTracked"] = "none";
                return RedirectToAction(nameof(Details), new { slug });
            }

            if (!await _saved.IsSavedAsync(userId, target.Id)) await _saved.ToggleAsync(userId, target.Id);
            await _tracker.GetWithChecklistAsync(userId, target.Id); // ينشئ الأوراق الافتراضية + مهام الدليل
            return RedirectToAction("Prepare", "Tracker", new { id = target.Id });
        }
    }
}
