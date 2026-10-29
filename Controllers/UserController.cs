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
        private readonly IApplicationTracker _tracker;
        private readonly IMatchService _match;

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISavedOpportunityService savedService,
            IApplicationTracker tracker,
            IMatchService match)
        {
            _context = context;
            _userManager = userManager;
            _savedService = savedService;
            _tracker = tracker;
            _match = match;
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

            // 4-1 "جديد منذ آخر زيارة": فجوة تزيد على 30 دقيقة = زيارة جديدة، فتصبح الزيارة السابقة مرجع "الجديد"
            var now = DateTime.Now;
            if (user.LastSeenAt == null || now - user.LastSeenAt.Value > TimeSpan.FromMinutes(30))
                user.PreviousVisitAt = user.LastSeenAt;
            user.LastSeenAt = now;
            await _context.SaveChangesAsync();

            var newSince = user.PreviousVisitAt;
            var newSinceCount = newSince.HasValue
                ? await _context.Opportunities.CountAsync(o => o.Status == OpportunityStatus.Published && o.PublishDate > newSince.Value)
                : 0;

            var saved = await _savedService.ListAsync(user.Id);
            var savedIds = saved.Select(o => o.Id).ToHashSet();

            var interestTypes = InterestMap.TypesFor(user.Interests);
            var profile = await _match.ProfileAsync(user, isIndividual: true)
                          ?? new MatchProfile(interestTypes, null, null);

            var recommended = new List<Opportunity>();
            if (interestTypes.Count > 0)
            {
                var candidates = await _context.Opportunities
                    .Include(o => o.AvailableCountries)
                    .Where(o => o.Status == OpportunityStatus.Published
                                && o.Type != null && interestTypes.Contains(o.Type.Value)
                                && (o.EndDate == null || o.EndDate >= today))
                    .OrderByDescending(o => o.PublishDate)
                    .Take(40)
                    .AsNoTracking()
                    .ToListAsync();

                // المحفوظة لها قسمها؛ ما لا يقبل جنسية المستخدم يُستبعد (3-1)؛ الأعلى تطابقًا ثم الأحدث
                recommended = candidates
                    .Where(o => !savedIds.Contains(o.Id))
                    .Where(o => MatchScorer.Eligible(profile, o) != false)
                    .OrderByDescending(o => MatchScorer.Score(profile, o))
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

            var savedSearches = await _context.SavedSearches
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var savedShown = saved.OrderByDescending(o => o.IsDeadlineSoon).Take(6).ToList();
            var stats = await _tracker.StatsAsync(user.Id);

            var shownAll = recommended.Concat(savedShown).Concat(latest).ToList();
            var completion = ProfileCompletion.Compute(user);

            var vm = new UserDashboardViewModel
            {
                NewSince = newSince,
                NewSinceCount = newSinceCount,
                Profile = completion,
                Achievements = Helpers.Achievements.Compute(stats, completion.Percent),
                SaveCounts = await _savedService.SaveCountsAsync(shownAll.Select(o => o.Id)),
                MatchScores = _match.Scores(profile, shownAll),
                MatchesNationality = profile.HasLocation,
                SavedSearches = savedSearches,
                Tracker = await _tracker.CardsAsync(user.Id, savedShown.Select(o => o.Id)),
                AppliedCount = stats.Applied,
                AcceptedCount = stats.Accepted,
                Recent = (await _tracker.RecentAsync(user.Id, 5)).Where(o => !savedIds.Contains(o.Id)).ToList(),
                FirstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.FullName,
                InterestNames = interestTypes.Select(t => t.GetDisplayName(culture)).ToList(),
                ExpiringSaved = saved.Where(o => o.IsDeadlineSoon).OrderBy(o => o.EndDate).ToList(),
                Recommended = recommended,
                Saved = savedShown,
                SavedCount = saved.Count,
                Latest = latest,
                SavedIds = savedIds
            };

            return View(vm);
        }
    }
}
