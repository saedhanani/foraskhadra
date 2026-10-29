using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISavedOpportunityService _savedService;

        /// <summary>مفتاح الاهتمام (كما يُخزَّن في InterestsJson عند التسجيل) → نوع الفرصة المقابل.</summary>
        private static readonly Dictionary<string, OpportunityType> InterestTypeMap = new()
        {
            ["competitions"] = OpportunityType.Competitions,
            ["conferences"] = OpportunityType.Conferences,
            ["volunteer_opportunities"] = OpportunityType.Volunteering,
            ["jobs"] = OpportunityType.Jobs,
            ["grants"] = OpportunityType.Scholarships,
            ["fellowships"] = OpportunityType.Fellowships,
            ["training_opportunities"] = OpportunityType.Internships
        };

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISavedOpportunityService savedService)
        {
            _context = context;
            _userManager = userManager;
            _savedService = savedService;
        }

        // GET: User/Dashboard — صفحة "صفحتي": مقترحة حسب الاهتمامات + المحفوظة مع حالة الموعد + أحدث الفرص
        public async Task<IActionResult> Dashboard()
        {
            // للمنظمات والإدارة لوحاتهما الخاصة
            if (User.IsInRole("Organization"))
                return RedirectToAction("Dashboard", "Organization");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var today = DateTime.Now.Date;
            var saved = await _savedService.ListAsync(user.Id);
            var savedIds = saved.Select(o => o.Id).ToHashSet();

            var interestTypes = user.Interests
                .Where(InterestTypeMap.ContainsKey)
                .Select(k => InterestTypeMap[k])
                .Distinct()
                .ToList();

            var recommended = new List<Opportunity>();
            if (interestTypes.Count > 0)
            {
                var candidates = await _context.Opportunities
                    .Include(o => o.AvailableCountries)
                    .Where(o => o.Status == OpportunityStatus.Published
                                && o.Type != null && interestTypes.Contains(o.Type.Value)
                                && (o.EndDate == null || o.EndDate >= today))
                    .OrderByDescending(o => o.PublishDate)
                    .Take(18)
                    .AsNoTracking()
                    .ToListAsync();

                // المحفوظة لها قسمها؛ وما ينتهي قريبًا يتقدّم على الأحدث
                recommended = candidates
                    .Where(o => !savedIds.Contains(o.Id))
                    .OrderByDescending(o => o.IsDeadlineSoon)
                    .ThenByDescending(o => o.PublishDate)
                    .Take(6)
                    .ToList();
            }

            var shownIds = savedIds.Concat(recommended.Select(o => o.Id)).ToHashSet();
            var latest = (await _context.Opportunities
                    .Include(o => o.AvailableCountries)
                    .Where(o => o.Status == OpportunityStatus.Published)
                    .OrderByDescending(o => o.PublishDate)
                    .Take(12)
                    .AsNoTracking()
                    .ToListAsync())
                .Where(o => !shownIds.Contains(o.Id))
                .Take(6)
                .ToList();

            var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            var vm = new UserDashboardViewModel
            {
                FirstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.FullName,
                InterestNames = interestTypes.Select(t => t.GetDisplayName(culture)).ToList(),
                ExpiringSaved = saved.Where(o => o.IsDeadlineSoon).OrderBy(o => o.EndDate).ToList(),
                Recommended = recommended,
                Saved = saved.OrderByDescending(o => o.IsDeadlineSoon).Take(6).ToList(),
                SavedCount = saved.Count,
                Latest = latest,
                SavedIds = savedIds
            };

            return View(vm);
        }
    }
}
