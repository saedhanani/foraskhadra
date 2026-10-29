using System.Globalization;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Foras_Khadra.Services;


namespace Foras_Khadra.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // إضافة DbContext
        private readonly ISavedOpportunityService _saved;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMatchService _match;
        private readonly IReferralService _referrals;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, ISavedOpportunityService saved,
            UserManager<ApplicationUser> userManager, IMatchService match, IReferralService referrals)
        {
            _logger = logger;
            _context = context;
            _saved = saved;
            _userManager = userManager;
            _match = match;
            _referrals = referrals;
        }

        /// <summary>لوحة سفراء الشهر (5-3): أعلى 10 داعين، بالاسم الأول فقط.</summary>
        public async Task<IActionResult> Ambassadors() =>
            View(await _referrals.MonthlyBoardAsync(10));

        /// <summary>نسب التطابق (المرحلة 3) للفرد المسجّل فقط؛ فارغة للزائر والمنظمات والإدارة.</summary>
        private async Task<Dictionary<int, int>> MatchScoresFor(IEnumerable<Opportunity> opportunities)
        {
            if (User.Identity?.IsAuthenticated != true || User.IsInRole("Organization") || User.IsInRole("Admin"))
                return new Dictionary<int, int>();

            var user = await _userManager.GetUserAsync(User);
            var profile = await _match.ProfileAsync(user, isIndividual: true);
            return profile == null ? new Dictionary<int, int>() : _match.Scores(profile, opportunities);
        }

        /// <summary>معرّفات الفرص المحفوظة من بين المعروضة، لتلوين زر الحفظ على البطاقات (فارغة للزائر).</summary>
        private async Task<HashSet<int>> SavedIdsFor(IEnumerable<Opportunity> opportunities)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId == null
                ? new HashSet<int>()
                : await _saved.SavedIdsAsync(userId, opportunities.Select(o => o.Id));
        }

  public IActionResult Index(bool filterByAvailable = false)
{
    var latestArticles = _context.Articles
        .OrderByDescending(a => a.PublishDate)
        .Take(9)
        .AsNoTracking()
        .ToList();
//تحذير استلام الفرص
//اذا تم تفعيل الفلتره في الواجهه الاماميه نقوم بفلتره الوصف

    var opportunitiesQuery = _context.Opportunities.Where(o => o.Status == OpportunityStatus.Published).AsQueryable();

    if (filterByAvailable)
    {
        opportunitiesQuery = opportunitiesQuery.Where(o => o.EndDate == null || o.EndDate >= DateTime.Now);
    }

    var latestOpportunities = opportunitiesQuery
        .OrderByDescending(o => o.PublishDate)
        .Take(9)
        .AsNoTracking()
        .ToList();

    var organizations = _context.Organizations
        .Where(o => !string.IsNullOrEmpty(o.LogoPath)) 
        .Select(o => new Organization
        {
            Id = o.Id,
            Name = o.Name,
            LogoPath = o.LogoPath
        })
        .ToList();

    var model = new HomeViewModel
    {
        LatestArticles = latestArticles,
        LatestOpportunities = latestOpportunities,
        Organizations = organizations
    };

    return View(model);
}
        /// <summary>صفحة "نشاطاتنا": المشاريع الرائدة في المقدمة ثم النشاطات المنشورة (القادمة ثم السابقة)، مع فلتر بالمشروع.</summary>
        public async Task<IActionResult> News(string? program = null)
        {
            var programs = await _context.FlagshipPrograms
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
                .ToListAsync();

            var query = _context.Activities
                .AsNoTracking()
                .Include(a => a.Program)
                .Where(a => a.IsPublished);

            var selected = string.IsNullOrWhiteSpace(program) ? null : programs.FirstOrDefault(p => p.Slug == program);
            if (selected != null)
                query = query.Where(a => a.ProgramId == selected.Id);

            var activities = await query.ToListAsync();

            var impactStats = await _context.ImpactStats
                .AsNoTracking()
                .Where(s => s.IsVisible)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                .ToListAsync();

            var model = new ActivitiesPageViewModel
            {
                Programs = programs,
                ImpactStats = impactStats,
                SelectedProgram = selected,
                Upcoming = activities.Where(a => a.IsUpcoming).OrderBy(a => a.StartDate).ToList(),
                Past = activities.Where(a => !a.IsUpcoming).OrderByDescending(a => a.StartDate).ToList()
            };
            return View(model);
        }

        public IActionResult PolicyPrivacy()
        {
            return View();
        }

        public IActionResult TermsAndConditions()
        {
            return View();
        }

        public IActionResult UsagePolicy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult StatusCode(int code)
        {
            var model = new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View("Error", model);
        }

        public IActionResult AboutUs()
        {
            var members = _context.TeamMember
                                  .AsNoTracking()
                                  .OrderByDescending(m => m.Membership)
                                  .ToList();
            return View(members); // ستبحث عن Views/Home/AboutUs.cshtml
        }

        public IActionResult Articles()
        {
            var articles = _context.Articles
            .AsNoTracking()
            .OrderByDescending(a => a.PublishDate)
            .ToList();

            var model = new HomeArticlesViewModel
            {
                LatestArticles = articles.Take(5).ToList(),
                OtherArticles = articles.Skip(5).ToList()
            };

            return View(model);
        }

        // ================= الفرص حسب النوع مع فلترة الدولة =================

        // ================= الفرص حسب النوع مع فلترة الدولة =================
        private const int OpportunitiesPageSize = 12;

        /// <summary>يقطع الاستعلام إلى صفحة واحدة ويحسب حالة الانتهاء لعناصرها.</summary>
        private static List<Opportunity> PageOf(IQueryable<Opportunity> query, int page, out int totalCount)
        {
            totalCount = query.Count();
            var items = query
                .OrderByDescending(o => o.PublishDate)   // من الأحدث للأقدم
                .Skip((page - 1) * OpportunitiesPageSize)
                .Take(OpportunitiesPageSize)
                .ToList();

            // ================== إضافة حالة الانتهاء ==================
            foreach (var opp in items)
            {
                opp.IsExpired = opp.EndDate.HasValue && opp.EndDate.Value < DateTime.Now;
            }
            return items;
        }

        private async Task<IActionResult> GetOpportunitiesByType(OpportunityType type, string country, string typeName, int page)
        {
            var query = _context.Opportunities
                                .Include(o => o.AvailableCountries)
                                .AsNoTracking()
                                .Where(o => o.Status == OpportunityStatus.Published && o.Type == type);

            if (!string.IsNullOrEmpty(country))
            {
                query = query.Where(o =>
                    o.AvailableCountries.Any(c =>
                        c.NameEn == country ||
                        c.NameAr == country ||
                        c.NameFr == country));
            }

            page = Math.Max(1, page);
            var opportunities = PageOf(query, page, out var totalCount);

            var model = new AllOpportunitiesViewModel
            {
                Opportunities = opportunities,
                SelectedCountry = country,
                SelectedTypes = new List<OpportunityType> { type },
                Page = page,
                PageSize = OpportunitiesPageSize,
                TotalCount = totalCount
            };

            ViewBag.TypeName = typeName;
            ViewBag.SavedIds = await SavedIdsFor(opportunities);
            return View("AllOpportunities", model);
        }
        // مثال لكل نوع:
        public Task<IActionResult> Trainings(string country = null, int page = 1)
        {
            return GetOpportunitiesByType(Foras_Khadra.Models.OpportunityType.Internships, country, "فرص التدريب", page);
        }

        public Task<IActionResult> Jobs(string country = null, int page = 1)
        {
            return GetOpportunitiesByType(Foras_Khadra.Models.OpportunityType.Jobs, country, "الوظائف", page);
        }

        public Task<IActionResult> Competitions(string country = null, int page = 1)
        {
            return GetOpportunitiesByType(Foras_Khadra.Models.OpportunityType.Competitions, country, "المسابقات", page);
        }

        public Task<IActionResult> Conferences(string country = null, int page = 1)
        {
            return GetOpportunitiesByType(Foras_Khadra.Models.OpportunityType.Conferences, country, "المؤتمرات", page);
        }

        public Task<IActionResult> Volunteering(string country = null, int page = 1)
        {
            return GetOpportunitiesByType(Foras_Khadra.Models.OpportunityType.Volunteering, country, "فرص التطوع", page);
        }

        public Task<IActionResult> Fellowships(string country = null, int page = 1)
        {
            return GetOpportunitiesByType(Foras_Khadra.Models.OpportunityType.Fellowships, country, "الزمالات", page);
        }

        public Task<IActionResult> Scholarships(string country = null, int page = 1)
        {
            return GetOpportunitiesByType(Foras_Khadra.Models.OpportunityType.Scholarships, country, "المنح الدراسية", page);
        }

        // ================= صفحة كل الفرص العامة =================
        public async Task<IActionResult> AllOpportunities(string country, string[] type, string search, int page = 1)
        {
            var query = _context.Opportunities
                                .Include(o => o.AvailableCountries)
                                .AsNoTracking()
                                .Where(o => o.Status == OpportunityStatus.Published)
                                .AsQueryable();

            // بحث نصي كامل: كل كلمة (مطبّعة) يجب أن ترد في نص الفرصة المطبّع — انظر Helpers/SearchIndexer
            foreach (var term in SearchIndexer.Terms(search))
            {
                var pattern = $"%{term}%";
                query = query.Where(o => EF.Functions.Like(o.SearchText, pattern));
            }

            // فلترة حسب النوع
            if (type != null && type.Any())
            {
                // تحويل النصوص إلى Enum
                var selectedTypes = type
                    .Where(t => Enum.TryParse<OpportunityType>(t, out _))
                    .Select(t => Enum.Parse<OpportunityType>(t))
                    .ToList();

                query = query.Where(o => o.Type.HasValue && selectedTypes.Contains(o.Type.Value));
            }

            // فلترة حسب الدولة
            if (!string.IsNullOrEmpty(country))
            {
                query = query.Where(o =>
                    o.AvailableCountries.Any(c =>
                        c.NameEn == country ||
                        c.NameAr == country ||
                        c.NameFr == country));
            }

            page = Math.Max(1, page);
            var allOpportunities = PageOf(query, page, out var totalCount);

            var countries = _context.Countries
                                    .OrderBy(c => c.NameAr)
                                    .Select(c => c.NameAr)
                                    .ToList();

            var model = new AllOpportunitiesViewModel
            {
                Opportunities = allOpportunities,
                SelectedCountry = country,
                SelectedTypes = type?.Where(t => Enum.TryParse<OpportunityType>(t, out _)).Select(t => Enum.Parse<OpportunityType>(t)).ToList() ?? new List<OpportunityType>(),
                Countries = countries,
                Page = page,
                PageSize = OpportunitiesPageSize,
                TotalCount = totalCount
            };

            ViewBag.SavedIds = await SavedIdsFor(allOpportunities);
            ViewBag.MatchScores = await MatchScoresFor(allOpportunities);
            ViewBag.SaveCounts = await _saved.SaveCountsAsync(allOpportunities.Select(o => o.Id));
            return View(model);
        }


        public IActionResult OrganizationsMap(string country = null, string sector = null, int page = 1)
        {
            int pageSize = 24;

            var registeredOrganizations = _context.Organizations
    .Where(o => !string.IsNullOrEmpty(o.LogoPath))
    .Select(o => new Organization
    {
        Id = o.Id,
        Name = o.Name,
        Country = o.Country,
        Sector = o.Sector,
        LogoPath = o.LogoPath,
        Location = o.Location,
        Website = o.Website,
        ContactName = o.ContactName,
        IsManual = false,
        CreatedAt = o.CreatedAt
    });

            var manualOrganizations = _context.ManualOrganizations
                .Where(o => !string.IsNullOrEmpty(o.LogoPath))
                .Select(o => new Organization
                {
                    Id = o.Id,
                    Name = o.OrganizationName,
                    Country = o.Country,
                    Sector = "Manual Organization",
                    LogoPath = o.LogoPath,
                    Location = o.Location,
                    Website = o.Website,
                    ContactName = o.ContactPersonName,
                    IsManual = true,
                    CreatedAt = o.CreatedAt,
                });

            var query = registeredOrganizations
                .Concat(manualOrganizations)
                .AsQueryable();

            if (!string.IsNullOrEmpty(country))
                query = query.Where(o => o.Country == country);

            if (!string.IsNullOrEmpty(sector))
            {
                query = query.AsEnumerable()
                    .Where(o =>
                    {
                        if (string.IsNullOrWhiteSpace(o.Sector))
                            return false;

                        var s1 = o.Sector.Trim().ToLower();
                        var s2 = sector.Trim().ToLower();

                        s1 = s1
                            .Replace("ة", "ه")
                            .Replace("أ", "ا")
                            .Replace("إ", "ا")
                            .Replace("آ", "ا")
                            .Replace("ى", "ي");

                        s2 = s2
                            .Replace("ة", "ه")
                            .Replace("أ", "ا")
                            .Replace("إ", "ا")
                            .Replace("آ", "ا")
                            .Replace("ى", "ي");

                        s1 = new string(s1.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
                        s2 = new string(s2.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());

                        s1 = string.Join(" ", s1.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                        s2 = string.Join(" ", s2.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                        return s1 == s2;
                    })
                    .AsQueryable();
            }

            int totalItems = query.Count();

            var organizations = query
.OrderByDescending(o => o.CreatedAt)
.Skip((page - 1) * pageSize)
     .Take(pageSize)
     .ToList();
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.SelectedCountry = country;
            ViewBag.SelectedSector = sector;

            ViewBag.Countries = _context.Organizations
    .Where(o => !string.IsNullOrWhiteSpace(o.Country))
    .AsEnumerable()
    .GroupBy(o => o.Country.Trim().ToLower())
    .Select(g => g.First().Country.Trim())
    .OrderBy(c => c)
    .ToList();


            ViewBag.Sectors = _context.Organizations
    .Where(o => !string.IsNullOrWhiteSpace(o.Sector))
    .AsEnumerable()
    .Where(o =>
        !string.IsNullOrWhiteSpace(o.Sector) &&
        !o.Sector.Contains("@") &&
        o.Sector.All(c => char.IsLetter(c) || char.IsWhiteSpace(c))
    )
    .GroupBy(o =>
    {
        var s = o.Sector.Trim().ToLower();

        s = s
            .Replace("ة", "ه")
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا")
            .Replace("ى", "ي");

        s = new string(s.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());

        return string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    })
    .Select(g => g.First().Sector.Trim())
    .OrderBy(s => s)
    .ToList();

            return View(organizations);
        }


        public IActionResult DetailsOrganization(int id)
        {
            var org = _context.Organizations.FirstOrDefault(o => o.Id == id);

            if (org == null)
                return NotFound();

            return View(org);
        }

        public IActionResult ManualOrganizationDetails(int id)
        {
            var org = _context.ManualOrganizations
                .FirstOrDefault(o => o.Id == id);

            if (org == null)
                return NotFound();

            return View(org);
        }

    }
}
