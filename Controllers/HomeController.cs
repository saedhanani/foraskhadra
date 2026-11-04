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
        private readonly IApplicationTracker _tracker;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, ISavedOpportunityService saved,
            UserManager<ApplicationUser> userManager, IMatchService match, IReferralService referrals, IApplicationTracker tracker)
        {
            _logger = logger;
            _context = context;
            _saved = saved;
            _userManager = userManager;
            _match = match;
            _referrals = referrals;
            _tracker = tracker;
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

  public async Task<IActionResult> Index(bool filterByAvailable = false)
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

    // شريط "فرصي" أعلى الرئيسية للفرد المسجّل (نقطة البداية في تطبيق الهاتف)
    var userId = _userManager.GetUserId(User);
    if (userId != null && !User.IsInRole("Organization") && !User.IsInRole("Admin"))
    {
        var saved = await _saved.ListAsync(userId);
        var cards = await _tracker.CardsAsync(userId, saved.Select(o => o.Id));
        model.My = MyOpportunitiesViewModel.Build(saved, cards, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

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

        /// <summary>صفحة 404/403/… بالعلامة (UseStatusCodePagesWithReExecute)؛ رمز الحالة الأصلي يبقى في الرد.</summary>
        public IActionResult StatusCodePage(int code)
        {
            ViewData["Code"] = code;
            return View("StatusCodePage");
        }

        /// <summary>"حمّل التطبيق": خطوات تثبيت فرص خضراء على iPhone (من Safari) وAndroid والحاسوب، وتفعيل التنبيهات.
        /// رابط قصير للمشاركة في وسائل التواصل: foraskhadra.com/app</summary>
        [HttpGet("/{culture:culture}/app")]
        [HttpGet("/app", Order = 1)]
        public IActionResult App() => View();

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
            page = Math.Clamp(page, 1, 10000); // رقم سالب أو ضخم كان يصير OFFSET سالبًا → 500
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

        // ================= صفحات الأنواع والقائمة العامة (المرحلة 2.1 + 2.2) =================

        /// <summary>الروابط القديمة /Home/Jobs… → 301 إلى /opportunities/{slug} مع الدولة والصفحة.</summary>
        private IActionResult RedirectToType(OpportunityType type, string? country, int page) =>
            RedirectToActionPermanent(nameof(ByType), new { type = OpportunityTypePages.For(type).Slug, country = string.IsNullOrWhiteSpace(country) ? null : country, page = page > 1 ? page : (int?)null });

        public IActionResult Trainings(string? country = null, int page = 1) => RedirectToType(OpportunityType.Internships, country, page);
        public IActionResult Jobs(string? country = null, int page = 1) => RedirectToType(OpportunityType.Jobs, country, page);
        public IActionResult Competitions(string? country = null, int page = 1) => RedirectToType(OpportunityType.Competitions, country, page);
        public IActionResult Conferences(string? country = null, int page = 1) => RedirectToType(OpportunityType.Conferences, country, page);
        public IActionResult Volunteering(string? country = null, int page = 1) => RedirectToType(OpportunityType.Volunteering, country, page);
        public IActionResult Fellowships(string? country = null, int page = 1) => RedirectToType(OpportunityType.Fellowships, country, page);
        public IActionResult Scholarships(string? country = null, int page = 1) => RedirectToType(OpportunityType.Scholarships, country, page);

        /// <summary>صفحة نوع واحد /opportunities/{slug}: عنوان ومقدمة لكل لغة، canonical ذاتي لكل صفحة ترقيم، والفلاتر نفسها.</summary>
        [HttpGet("/{culture:culture}/opportunities/{type}")]
        [HttpGet("/opportunities/{type}", Order = 1)]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("search")]
        public async Task<IActionResult> ByType(string type, [FromQuery] OpportunityQuery q)
        {
            var page = OpportunityTypePages.BySlug(type);
            if (page == null)
            {
                // صفحة نية (المرحلة 2.3): فلتر محفوظ + مقدمة بشرية + أسئلة؛ فلاتر الطلب تُضاف فوقه (وتجعلها noindex)
                var slug = type.Trim().ToLowerInvariant();
                var landing = await _context.LandingPages.AsNoTracking().FirstOrDefaultAsync(l => l.Slug == slug && l.IsPublished);
                if (landing == null) return NotFound();
                var all = await CountriesAsync();
                var vm = await BuildListAsync(OpportunityQuery.Merge(landing.Query(), q), null, all);
                vm.Landing = landing;
                vm.ExtraFilter = OpportunityFilter.Parse(q, all);
                vm.LandingIndexable = landing.IsIndexable(await LandingPage.OpenCountAsync(_context, landing, all, DateTime.Now),
                    System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
                return View("AllOpportunities", vm);
            }
            if (!string.Equals(type, page.Slug, StringComparison.Ordinal))
                return RedirectToActionPermanent(nameof(ByType), new { type = page.Slug, page = q.Page > 1 ? q.Page : (int?)null }); // الأسماء القديمة والأحرف الكبيرة → الرابط القانوني
            q.Type = new List<string> { page.Type.ToString() };
            return View("AllOpportunities", await BuildListAsync(q, page));
        }

        // ================= صفحة كل الفرص العامة =================
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("search")]
        public async Task<IActionResult> AllOpportunities([FromQuery] OpportunityQuery q, [FromServices] IAnalytics analytics)
        {
            var countries = await CountriesAsync();
            var parsed = OpportunityFilter.Parse(q, countries);
            // نوع واحد بلا أي معيار آخر: صفحته الخاصة هي القانونية
            if (parsed.IsSingleTypeOnly)
                return RedirectToActionPermanent(nameof(ByType), new { type = OpportunityTypePages.For(parsed.Types[0]).Slug, page = q.Page > 1 ? q.Page : (int?)null });

            var vm = await BuildListAsync(q, null, countries, parsed);

            // تحليلات: عدد عمليات البحث، والبحث بصفر نتائج مع العبارة (ما يطلبه الزوار ولا نملكه)
            if (parsed.Search.Length > 0)
            {
                analytics.Event("search");
                if (vm.TotalCount == 0) analytics.Event("search.zero", parsed.Search);
            }
            return View(vm);
        }

        private Task<List<Country>> CountriesAsync() => _context.Countries.AsNoTracking().ToListAsync();

        private async Task<AllOpportunitiesViewModel> BuildListAsync(OpportunityQuery q, TypePage? typePage, List<Country>? countries = null, ParsedFilter? parsed = null)
        {
            countries ??= await CountriesAsync();
            parsed ??= OpportunityFilter.Parse(q, countries);
            var today = DateTime.Now.Date;

            var query = OpportunityFilter.Apply(_context.Opportunities.Include(o => o.AvailableCountries).AsNoTracking(), parsed, today);
            var totalCount = await query.CountAsync();
            var page = Math.Clamp(q.Page, 1, 10000); // رقم سالب أو ضخم كان يصير OFFSET سالبًا → 500
            var items = await OpportunityFilter.Sort(query, parsed.Sort, today)
                .Skip((page - 1) * OpportunitiesPageSize)
                .Take(OpportunitiesPageSize)
                .ToListAsync();
            foreach (var opp in items)
                opp.IsExpired = opp.EndDate.HasValue && opp.EndDate.Value < DateTime.Now;

            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            string Name(Country c) => lang == "en" ? c.NameEn : lang == "fr" ? c.NameFr : c.NameAr;
            var comparer = StringComparer.Create(System.Globalization.CultureInfo.CurrentUICulture, true);
            var hostIso = await _context.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published && o.HostCountryIso != null && o.HostCountryIso != "")
                .Select(o => o.HostCountryIso!).Distinct().ToListAsync();

            var model = new AllOpportunitiesViewModel
            {
                Opportunities = items,
                SelectedCountry = q.Country,
                SelectedTypes = parsed.Types,
                Countries = countries.OrderBy(c => c.NameAr).Select(c => c.NameAr).ToList(),
                Page = page,
                PageSize = OpportunitiesPageSize,
                TotalCount = totalCount,
                Query = q,
                Filter = parsed,
                TypePage = typePage,
                Fields = await _context.Fields.AsNoTracking().Where(f => f.Key != Field.AnyKey).OrderBy(f => f.SortOrder).ToListAsync(),
                CountryOptions = countries.Where(c => c.IsoCode != null && CompassMatcher.ArabIso.Contains(c.IsoCode))
                    .Select(c => new CountryOption(c.IsoCode!.ToUpperInvariant(), Name(c))).OrderBy(o => o.Name, comparer).ToList(),
                HostOptions = hostIso.Select(iso => new CountryOption(iso.ToUpperInvariant(), CountryHelper.NameForIso(iso, lang) ?? iso)).OrderBy(o => o.Name, comparer).ToList()
            };

            ViewBag.SavedIds = await SavedIdsFor(items);
            ViewBag.MatchScores = await MatchScoresFor(items);
            ViewBag.SaveCounts = await _saved.SaveCountsAsync(items.Select(o => o.Id));
            return model;
        }

        public IActionResult OrganizationsMap(string country = null, string sector = null, int page = 1)
        {
            int pageSize = 24;
            page = Math.Clamp(page, 1, 10000);

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


        public async Task<IActionResult> DetailsOrganization(int id, [FromServices] IOrganizationFollowService follows)
        {
            var org = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);

            if (org == null)
                return NotFound();

            // فرص المنظمة المنشورة (المفتوحة أولًا) وحالة المتابعة — المرحلة 3.2
            var today = DateTime.Now.Date;
            var opps = org.UserId == null
                ? new List<Opportunity>()
                : await _context.Opportunities.AsNoTracking()
                    .Where(o => o.CreatedByUserId == org.UserId && o.Status == OpportunityStatus.Published)
                    .OrderByDescending(o => o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today)
                    .ThenByDescending(o => o.PublishDate)
                    .Take(12)
                    .ToListAsync();
            var userId = _userManager.GetUserId(User);
            ViewBag.OrgOpportunities = opps;
            ViewBag.SavedIds = userId != null && opps.Count > 0 ? (await _saved.SavedIdsAsync(userId, opps.Select(o => o.Id))).ToHashSet() : new HashSet<int>();
            ViewBag.IsFollowing = userId != null && User.IsInRole("User") && await follows.IsFollowingAsync(userId, org.Id);
            ViewBag.FollowerCount = await follows.FollowerCountAsync(org.Id);

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
