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
        private readonly IReferralService _referrals;
        private readonly IWeeklySummaryService _weekly;

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISavedOpportunityService savedService,
            IApplicationTracker tracker,
            IMatchService match,
            IReferralService referrals,
            IWeeklySummaryService weekly)
        {
            _context = context;
            _userManager = userManager;
            _savedService = savedService;
            _tracker = tracker;
            _match = match;
            _referrals = referrals;
            _weekly = weekly;
        }

        // GET: User/Weekly — ملخص أسبوعك (5-1)، يُحسب عند الطلب من الأيام السبعة الماضية
        public async Task<IActionResult> Weekly()
        {
            if (User.IsInRole("Organization"))
                return RedirectToAction("Dashboard", "Organization");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var profile = (await _match.ProfileAsync(user, isIndividual: true))!;
            var summary = await _weekly.BuildAsync(user, profile);

            ViewData["MatchScores"] = _match.Scores(profile, summary.NewMatching.Concat(summary.DeadlinesSoon));
            ViewData["SaveCounts"] = await _savedService.SaveCountsAsync(summary.NewMatching.Select(o => o.Id));
            ViewBag.SavedIds = await _savedService.SavedIdsAsync(user.Id, summary.NewMatching.Select(o => o.Id));
            ViewBag.FirstName = string.IsNullOrWhiteSpace(user.FirstName) ? user.FullName : user.FirstName;
            return View(summary);
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
            var profile = (await _match.ProfileAsync(user, isIndividual: true))!;

            // حالة التقديم لكل المحفوظات: من قدّم لا يُذكَّر بالموعد في شريط "تقترب"
            var cards = await _tracker.CardsAsync(user.Id, savedIds);
            bool NotApplied(Opportunity o) => !(cards.TryGetValue(o.Id, out var c) && c.Status.HasApplied());

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

            // 5-3 السفراء و 5-2 بطاقة الإنجاز
            var referralCode = await _referrals.EnsureCodeAsync(user);
            var referralCount = await _referrals.CountAsync(user.Id);
            var latestAccepted = await _context.SavedOpportunities
                .AsNoTracking()
                .Where(s => s.UserId == user.Id && s.Status == ApplicationStatus.Accepted && s.Opportunity!.Status == OpportunityStatus.Published)
                .OrderByDescending(s => s.StatusChangedAt)
                .Select(s => s.Opportunity!)
                .FirstOrDefaultAsync();

            var vm = new UserDashboardViewModel
            {
                ReferralCode = referralCode,
                ReferralCount = referralCount,
                LatestAccepted = latestAccepted,
                NewSince = newSince,
                NewSinceCount = newSinceCount,
                Profile = completion,
                Achievements = Helpers.Achievements.Compute(stats, completion.Percent, referralCount),
                SaveCounts = await _savedService.SaveCountsAsync(shownAll.Select(o => o.Id)),
                MatchScores = _match.Scores(profile, shownAll),
                MatchesNationality = profile.HasLocation,
                SavedSearches = savedSearches,
                Tracker = cards,
                AppliedCount = stats.Applied,
                AcceptedCount = stats.Accepted,
                Recent = (await _tracker.RecentAsync(user.Id, 5)).Where(o => !savedIds.Contains(o.Id)).ToList(),
                FirstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.FullName,
                InterestNames = interestTypes.Select(t => t.GetDisplayName(culture)).ToList(),
                ExpiringSaved = saved.Where(o => o.IsDeadlineSoon && NotApplied(o)).OrderBy(o => o.EndDate).ToList(),
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
