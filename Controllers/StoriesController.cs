using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    public sealed record ImpactVM(List<ImpactStat> Stats, List<SuccessStory> Stories, int AcceptedTotal, int MembersTotal, int OpenTotal, int CountriesTotal);

    public class StoryForm
    {
        public int? OpportunityId { get; set; }
        [StringLength(200)] public string? ProgramName { get; set; }
        [Required, StringLength(SuccessStory.MaxStory, MinimumLength = 80)] public string Story { get; set; } = string.Empty;
        [StringLength(300)] public string? Advice { get; set; }
        public StoryDisplayName DisplayName { get; set; }
        public bool ConsentPublic { get; set; }
    }

    /// <summary>
    /// صفحة الأثر وقصص النجاح (المرحلة 4.2): /impact عامة (أرقام الأثر + القصص المعتمدة)، و/stories/new للعضو الذي قُبل
    /// (من المتابعة) بموافقة صريحة على النشر؛ القصة لا تظهر قبل اعتماد المدير من /Admin/Stories.
    /// </summary>
    public class StoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public StoriesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private static string Lang => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private static string T(string ar, string en, string fr) => Lang == "en" ? en : Lang == "fr" ? fr : ar;

        [AllowAnonymous]
        [HttpGet("/{culture:culture}/impact")]
        [HttpGet("/impact", Order = 1)]
        public async Task<IActionResult> Impact()
        {
            var today = DateTime.Now.Date;
            var stats = await _db.ImpactStats.AsNoTracking().Where(s => s.IsVisible).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
            var stories = await _db.SuccessStories.AsNoTracking().Include(s => s.Opportunity)
                .Where(s => s.Status == StoryStatus.Approved)
                .OrderByDescending(s => s.Featured).ThenByDescending(s => s.ReviewedAt)
                .Take(30).ToListAsync();
            var accepted = await _db.SavedOpportunities.CountAsync(s => s.Status == ApplicationStatus.Accepted);
            var members = await _db.Users.CountAsync(u => u.Role == UserRole.User);
            var open = await _db.Opportunities.CountAsync(o => o.Status == OpportunityStatus.Published && (o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today));
            var countries = await _db.Users.Where(u => u.Role == UserRole.User && u.Country != null && u.Country != "").Select(u => u.Country).Distinct().CountAsync();

            ViewData["Title"] = T("أثرنا وقصص النجاح", "Our impact and success stories", "Notre impact et les histoires de réussite");
            ViewData["Description"] = T("شباب من الوطن العربي قُبلوا في منح وزمالات ووظائف عبر فرص خضراء — أرقامنا وقصصهم بكلماتهم.", "Young people from the Arab world accepted into scholarships, fellowships and jobs through Foras Khadra — our numbers and their stories in their own words.", "Des jeunes du monde arabe admis à des bourses, fellowships et emplois via Foras Khadra — nos chiffres et leurs histoires.");
            ViewBag.Submitted = TempData["StorySubmitted"] as string;
            return View(new ImpactVM(stats, stories, accepted, members, open, countries));
        }

        [Authorize(Roles = "User")]
        [HttpGet("/{culture:culture}/stories/new")]
        [HttpGet("/stories/new", Order = 1)]
        public async Task<IActionResult> New(int? opportunityId)
        {
            ViewData["Title"] = T("شارك قصتك", "Share your story", "Partagez votre histoire");
            ViewBag.Opportunity = opportunityId.HasValue ? await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == opportunityId) : null;
            return View(new StoryForm { OpportunityId = opportunityId });
        }

        [Authorize(Roles = "User")]
        [HttpPost("/{culture:culture}/stories/new")]
        [HttpPost("/stories/new", Order = 1)]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> New(StoryForm form)
        {
            var userId = _userManager.GetUserId(User)!;
            if (!form.ConsentPublic) ModelState.AddModelError(nameof(form.ConsentPublic), T("الموافقة على النشر مطلوبة.", "Consent to publish is required.", "Le consentement à la publication est requis."));
            if (form.OpportunityId == null && string.IsNullOrWhiteSpace(form.ProgramName))
                ModelState.AddModelError(nameof(form.ProgramName), T("اكتب اسم البرنامج أو الفرصة.", "Write the program or opportunity name.", "Indiquez le nom du programme ou de l'opportunité."));
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = T("شارك قصتك", "Share your story", "Partagez votre histoire");
                ViewBag.Opportunity = form.OpportunityId.HasValue ? await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == form.OpportunityId) : null;
                return View(form);
            }

            var user = await _userManager.GetUserAsync(User);
            var pendingCount = await _db.SuccessStories.CountAsync(s => s.UserId == userId && s.Status == StoryStatus.Pending);
            if (pendingCount >= 3) { TempData["StorySubmitted"] = "limit"; return RedirectToAction(nameof(Impact)); }

            _db.SuccessStories.Add(new SuccessStory
            {
                UserId = userId,
                OpportunityId = form.OpportunityId,
                ProgramName = string.IsNullOrWhiteSpace(form.ProgramName) ? null : form.ProgramName.Trim(),
                Story = form.Story.Trim(),
                Advice = string.IsNullOrWhiteSpace(form.Advice) ? null : form.Advice.Trim(),
                DisplayName = form.DisplayName,
                ConsentPublic = true,
                Lang = Lang,
                CountryIso = null,
                ShownName = ShownNameFor(user, form.DisplayName)
            });
            await _db.SaveChangesAsync();
            TempData["StorySubmitted"] = "sent";
            return RedirectToAction(nameof(Impact));
        }

        public static string ShownNameFor(ApplicationUser? user, StoryDisplayName choice) => choice switch
        {
            StoryDisplayName.Anonymous => "",
            StoryDisplayName.FullName => (user?.FullName ?? user?.FirstName ?? "").Trim(),
            _ => (user?.FirstName ?? (user?.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "").Trim()
        };
    }
}
