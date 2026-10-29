using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;
using static System.Net.Mime.MediaTypeNames;

namespace Foras_Khadra.Controllers
{
    [Authorize]
    public class OpportunityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageUploadService _images;
        private readonly ISavedOpportunityService _saved;
        private readonly IApplicationTracker _tracker;

        public OpportunityController(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, IImageUploadService images, ISavedOpportunityService saved, IApplicationTracker tracker)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _images = images;
            _saved = saved;
            _tracker = tracker;
        }

        // ================= INDEX =================
        // قائمة الإدارة (تشمل المسودات) — للأدمن فقط
        [Authorize(Roles = "Admin")]
        public IActionResult Index(bool availableOnly)
        {
            var query = _context.Opportunities.AsQueryable();

// اضفنا فلتره للفعاليات المفتوحه وفعاليات لها وقت محدد (غير منتهيه) 
           if (availableOnly)
            {
                query = query.Where(o => o.EndDate == null || o.EndDate >= DateTime.Now);
            }

            // تجميع الفرص مع استبعاد الفرص المجهوله لتجنب انهيار الواجهه 
            var opportunities = query.ToList();
            var grouped = opportunities 
.Where(o => o.Type.HasValue)
.GroupBy(o => o.Type.Value)
.ToDictionary(g => g.Key, g => g.ToList());

            return View(grouped);
        }

        // ================= CREATE =================
[HttpGet]
[Authorize(Roles = "Admin,Organization")]
public async Task<IActionResult> Create()
        {
            var model = new OpportunityCreateVM();
            var countries = await _context.Countries.ToListAsync();

            // احصل على لغة الموقع الحالية
            var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            model.CountriesSelectList = countries.Select(c => new SelectListItem
            {
                Text = culture switch
                {
                    "en" => c.NameEn,
                    "fr" => c.NameFr,
                    _ => c.NameAr
                },
                Value = c.Id.ToString(),
                Selected = model.AvailableCountryIds?.Contains(c.Id) ?? false
            }).ToList();


            var currentUser = await _userManager.GetUserAsync(User);

            if (await _userManager.IsInRoleAsync(currentUser, "Organization"))
            {
                var org = await _context.Organizations.FirstOrDefaultAsync(o => o.UserId == currentUser.Id);
                if (org != null) model.PublishedBy = org.Name;
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                model.PublishedBy = "Admin";
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Organization")]
        public async Task<IActionResult> Create(OpportunityCreateVM model)
        {
           
            if (model.DeadlineType == DeadlineType.SpecificDate && model.EndDate == null)
            {
                ModelState.AddModelError("EndDate", "End date is required when selecting specific date.");
            }
            if (model.Image != null && _images.Validate(model.Image) is string imageError)
            {
                ModelState.AddModelError("Image", imageError);
            }
            if (!ModelState.IsValid)
            {
                // إعادة ملء قائمة الدول حسب لغة الموقع
                var countries = await _context.Countries.ToListAsync();
                var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                model.CountriesSelectList = countries.Select(c => new SelectListItem
                {
                    Text = culture switch
                    {
                        "en" => c.NameEn,
                        "fr" => c.NameFr,
                        _ => c.NameAr
                    },
                    Value = c.Id.ToString(),
                    Selected = model.AvailableCountryIds?.Contains(c.Id) ?? false
                }).ToList();

                return View(model);
            }

            string imagePath = null;
            if (model.Image != null)
            {
                imagePath = await _images.SaveAsync(model.Image, "uploads/opportunities");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.UserId == currentUser.Id);

            var opportunity = new Opportunity
            {
                TitleAr = model.TitleAr,
                TitleEn = model.TitleEn,
                TitleFr = model.TitleFr,
                // تعقيم HTML القادم من المحرر قبل التخزين
                DescriptionAr = SafeHtml.Clean(model.DescriptionAr),
                DescriptionEn = SafeHtml.Clean(model.DescriptionEn),
                DescriptionFr = SafeHtml.Clean(model.DescriptionFr),
                DetailsAr = SafeHtml.Clean(model.DetailsAr),
                DetailsEn = SafeHtml.Clean(model.DetailsEn),
                DetailsFr = SafeHtml.Clean(model.DetailsFr),
                AvailableCountries = model.AvailableCountryIds != null
                    ? await _context.Countries
                        .Where(c => model.AvailableCountryIds.Contains(c.Id))
                        .ToListAsync()
                    : new List<Country>(),
                EligibilityCriteriaAr = SafeHtml.Clean(model.EligibilityCriteriaAr),
                EligibilityCriteriaEn = SafeHtml.Clean(model.EligibilityCriteriaEn),
                EligibilityCriteriaFr = SafeHtml.Clean(model.EligibilityCriteriaFr),
                BenefitsAr = SafeHtml.Clean(model.BenefitsAr),
                BenefitsEn = SafeHtml.Clean(model.BenefitsEn),
                BenefitsFr = SafeHtml.Clean(model.BenefitsFr),
                ApplyLink = model.ApplyLink,
                ImagePath = imagePath,
                DeadlineType = model.DeadlineType,

                EndDate = model.DeadlineType == DeadlineType.SpecificDate
        ? model.EndDate
        : null,

                PublishedBy = org != null ? org.Name : "Admin",
                Type = model.Type.Value,
                CreatedByUserId = currentUser.Id,
                IsPublishedByAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin"),
                IsPublishedByOrganization = await _userManager.IsInRoleAsync(currentUser, "Organization")
            };

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            return RedirectToAction(await _userManager.IsInRoleAsync(currentUser, "Admin") ? "Index" : "OrgOpportunities");
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Organization")]
        public async Task<IActionResult> Edit(OpportunityEditVM model)
        {
            if (model.Image != null && _images.Validate(model.Image) is string imageError)
            {
                ModelState.AddModelError("Image", imageError);
            }

            // أولاً تحقق من صحة الموديل
            if (!ModelState.IsValid)
            {
                var countries = await _context.Countries.ToListAsync();
                var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                model.CountriesSelectList = countries.Select(c => new SelectListItem
                {
                    Text = culture switch
                    {
                        "en" => c.NameEn,
                        "fr" => c.NameFr,
                        _ => c.NameAr
                    },
                    Value = c.Id.ToString(),
                    Selected = model.AvailableCountryIds?.Contains(c.Id) ?? false
                }).ToList();

                return View(model);
            }

            // جلب المستخدم الحالي
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.UserId == currentUser.Id);

            // جلب الفرصة الحالية بدل إنشاء واحدة جديدة
            var opportunity = await _context.Opportunities
                .Include(o => o.AvailableCountries)
                .FirstOrDefaultAsync(o => o.Id == model.Id);

            if (opportunity == null) return NotFound();

            if (opportunity.CreatedByUserId != currentUser.Id && !await _userManager.IsInRoleAsync(currentUser, "Admin"))
                return Forbid();

            // تحديث الحقول
            opportunity.TitleAr = model.TitleAr;
            opportunity.TitleEn = model.TitleEn;
            opportunity.TitleFr = model.TitleFr;
            // تعقيم HTML القادم من المحرر قبل التخزين
            opportunity.DescriptionAr = SafeHtml.Clean(model.DescriptionAr);
            opportunity.DescriptionEn = SafeHtml.Clean(model.DescriptionEn);
            opportunity.DescriptionFr = SafeHtml.Clean(model.DescriptionFr);
            opportunity.DetailsAr = SafeHtml.Clean(model.DetailsAr);
            opportunity.DetailsEn = SafeHtml.Clean(model.DetailsEn);
            opportunity.DetailsFr = SafeHtml.Clean(model.DetailsFr);
            opportunity.EligibilityCriteriaAr = SafeHtml.Clean(model.EligibilityCriteriaAr);
            opportunity.EligibilityCriteriaEn = SafeHtml.Clean(model.EligibilityCriteriaEn);
            opportunity.EligibilityCriteriaFr = SafeHtml.Clean(model.EligibilityCriteriaFr);
            opportunity.BenefitsAr = SafeHtml.Clean(model.BenefitsAr);
            opportunity.BenefitsEn = SafeHtml.Clean(model.BenefitsEn);
            opportunity.BenefitsFr = SafeHtml.Clean(model.BenefitsFr);
            opportunity.ApplyLink = model.ApplyLink;
            opportunity.Type = model.Type ?? opportunity.Type;
            opportunity.DeadlineType = model.DeadlineType;

            opportunity.EndDate = model.DeadlineType == DeadlineType.SpecificDate
                ? model.EndDate
                : null;
            // تحديث الصورة فقط إذا تم رفعها
            if (model.Image != null)
            {
                opportunity.ImagePath = await _images.SaveAsync(model.Image, "uploads/opportunities");
            }

            // تحديث الدول المتاحة
            if (model.AvailableCountryIds != null)
            {
                opportunity.AvailableCountries = await _context.Countries
                    .Where(c => model.AvailableCountryIds.Contains(c.Id))
                    .ToListAsync();
            }
            else
            {
                opportunity.AvailableCountries = new List<Country>();
            }

            // تحديث من نشر
            opportunity.PublishedBy = org != null ? org.Name : "Admin";
            opportunity.IsPublishedByAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            opportunity.IsPublishedByOrganization = await _userManager.IsInRoleAsync(currentUser, "Organization");

            var existingRequest = await _context.ReelsRequests
    .FirstOrDefaultAsync(r => r.OpportunityId == opportunity.Id);

            if (existingRequest != null && existingRequest.Status == ReelsRequestStatus.Rejected)
            {
                existingRequest.SetStatus(ReelsRequestStatus.Pending);
                existingRequest.RejectionReason = null;
                existingRequest.RequestDate = DateTime.Now; // ترجع Pending تلقائياً
            }
            // حفظ التغييرات
            _context.Opportunities.Update(opportunity);
            await _context.SaveChangesAsync();

            // إعادة التوجيه حسب الدور
            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            return RedirectToAction(isAdmin ? "Index" : "OrgOpportunities");
        }


        // ================= EDIT GET =================
        [Authorize(Roles = "Admin,Organization")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var opp = await _context.Opportunities
                .Include(o => o.AvailableCountries)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (opp == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            if (opp.CreatedByUserId != currentUser.Id && !await _userManager.IsInRoleAsync(currentUser, "Admin"))
                return Forbid();

            var countries = await _context.Countries.ToListAsync();
            var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            var model = new OpportunityEditVM
            {
                Id = opp.Id,
                TitleAr = opp.TitleAr,
                TitleEn = opp.TitleEn,
                TitleFr = opp.TitleFr,
                PublishedBy = opp.PublishedBy,
                Type = opp.Type,
                DescriptionAr = opp.DescriptionAr,
                DescriptionEn = opp.DescriptionEn,
                DescriptionFr = opp.DescriptionFr,
                DetailsAr = opp.DetailsAr,
                DetailsEn = opp.DetailsEn,
                DetailsFr = opp.DetailsFr,
                DeadlineType = opp.DeadlineType ?? DeadlineType.NotSpecified,
                AvailableCountryIds = opp.AvailableCountries?.Select(c => c.Id).ToList() ?? new List<int>(),
                CountriesSelectList = countries.Select(c => new SelectListItem
                {
                    Text = culture switch
                    {
                        "en" => c.NameEn,
                        "fr" => c.NameFr,
                        _ => c.NameAr
                    },
                    Value = c.Id.ToString(),
                    Selected = opp.AvailableCountries?.Any(ac => ac.Id == c.Id) ?? false
                }).ToList(),
                BenefitsAr = opp.BenefitsAr,
                BenefitsEn = opp.BenefitsEn,
                BenefitsFr = opp.BenefitsFr,
                EligibilityCriteriaAr = opp.EligibilityCriteriaAr,
                EligibilityCriteriaEn = opp.EligibilityCriteriaEn,
                EligibilityCriteriaFr = opp.EligibilityCriteriaFr,
                ApplyLink = opp.ApplyLink,
                ImagePath = opp.ImagePath
            };

            return View(model);
        }




        // ================= DELETE =================
        // POST فقط: الحذف عبر GET يُستغل بمجرد رابط/صورة في صفحة خارجية
        [HttpPost]
        [Authorize(Roles = "Admin,Organization")]
        public async Task<IActionResult> Delete(int id)
        {
            var opp = await _context.Opportunities.FindAsync(id);
            if (opp == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            if (opp.CreatedByUserId != currentUser.Id && !await _userManager.IsInRoleAsync(currentUser, "Admin"))
                return Forbid();

            _context.Opportunities.Remove(opp);
            await _context.SaveChangesAsync();

            return RedirectToAction(await _userManager.IsInRoleAsync(currentUser, "Admin") ? "Index" : "OrgOpportunities");
        }

        // ================= DETAILS =================
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var opp = await _context.Opportunities
                .Include(o => o.AvailableCountries)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (opp == null) return NotFound();
            // المسودات القادمة من منصة الفريق لا تظهر للزوار قبل اعتمادها
            if (opp.Status == OpportunityStatus.Draft && !(User.Identity?.IsAuthenticated == true && User.IsInRole("Admin")))
                return NotFound();

            var creator = await _userManager.FindByIdAsync(opp.CreatedByUserId);
            opp.IsPublishedByAdmin = creator != null && await _userManager.IsInRoleAsync(creator, "Admin");

            var userId = _userManager.GetUserId(User);
            ViewBag.IsSaved = userId != null && await _saved.IsSavedAsync(userId, opp.Id);

            // "تابعت مؤخرًا" للمسجّلين (المنظمات والإدارة تتصفح لأغراض أخرى)
            if (userId != null && opp.Status == OpportunityStatus.Published && !User.IsInRole("Organization") && !User.IsInRole("Admin"))
                await _tracker.RecordViewAsync(userId, opp.Id);

            return View(opp);
        }

        /// <summary>ملف تقويم (.ics) ليوم الموعد النهائي مع تنبيه قبله بثلاثة أيام — زر "ذكّرني بالموعد".</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Calendar(int id)
        {
            var opp = await _context.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            if (opp == null || opp.Status != OpportunityStatus.Published
                || opp.DeadlineType != Foras_Khadra.Models.DeadlineType.SpecificDate || !opp.EndDate.HasValue)
                return NotFound();

            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var title = lang switch { "en" => opp.TitleEn, "fr" => opp.TitleFr, _ => opp.TitleAr };
            var prefix = lang == "en" ? "Deadline: " : lang == "fr" ? "Date limite : " : "الموعد النهائي: ";
            var url = Url.Action("Details", "Opportunity", new { id = opp.Id, slug = SlugHelper.Make(title) }, Request.Scheme) ?? "";

            var ics = CalendarHelper.BuildDeadlineEvent(opp.Id, prefix + title, opp.EndDate.Value, url);
            return File(System.Text.Encoding.UTF8.GetBytes(ics), "text/calendar; charset=utf-8", $"foras-khadra-{opp.Id}.ics");
        }

        // ================= SAVED OPPORTUNITIES =================
        /// <summary>يحفظ الفرصة أو يلغي حفظها (زر الإشارة المرجعية). JSON: { saved: true/false }.</summary>
        [HttpPost]
        public async Task<IActionResult> ToggleSave(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var saved = await _saved.ToggleAsync(userId, id);
            return Json(new { saved });
        }

        /// <summary>قائمة الفرص التي حفظها المستخدم.</summary>
        [HttpGet]
        public async Task<IActionResult> Saved()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var opportunities = await _saved.ListAsync(userId);
            foreach (var opp in opportunities)
                opp.IsExpired = opp.EndDate.HasValue && opp.EndDate.Value < DateTime.Now;

            ViewBag.TrackerCards = await _tracker.CardsAsync(userId, opportunities.Select(o => o.Id));
            return View(opportunities);
        }

        // ================= FILTER BY TYPE =================
        [Route("Opportunities/{type}")]
        public IActionResult ByType(OpportunityType type)
        {
            var data = _context.Opportunities
                .Where(x => x.Status == OpportunityStatus.Published && x.Type == type)
                .ToList();

            ViewBag.Type = type;
            return View(data);
        }

        // ================= ORG OPPORTUNITIES =================
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> OrgOpportunities()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.UserId == user.Id);
            var opportunities = await _context.Opportunities
    .Where(o => o.CreatedByUserId == user.Id)
    .Include(o => o.AvailableCountries)
    .AsNoTracking()
    .ToListAsync();

            // طلبات الريلز للمنظمة كلها باستعلام واحد بدل استعلام لكل فرصة
            var requestsByOpportunity = org == null
                ? new Dictionary<int, ReelsRequest>()
                : (await _context.ReelsRequests
                        .Where(r => r.OrganizationId == org.Id)
                        .AsNoTracking()
                        .ToListAsync())
                    .GroupBy(r => r.OpportunityId)
                    .ToDictionary(g => g.Key, g => g.First());

            var model = new OrgOpportunityPageVM
            {
                OrganizationName = org?.Name,
                Opportunities = opportunities.Select(o =>
                {
                    requestsByOpportunity.TryGetValue(o.Id, out var request);

                    return new OrgOpportunityVM
                    {
                        Id = o.Id,
                        TitleAr = o.TitleAr,
                        TitleEn = o.TitleEn,
                        TitleFr = o.TitleFr,
                        DescriptionAr = o.DescriptionAr,
                        DescriptionEn = o.DescriptionEn,
                        DescriptionFr = o.DescriptionFr,
                        AvailableCountries = o.AvailableCountries?.ToList() ?? new List<Country>(),
                        Type = o.Type,
                        PublishDate = o.PublishDate,
                        ImagePath = o.ImagePath,
                        HasRequestedReels = request != null,
                        IsReelsCompleted = request?.IsCompleted ?? false,
                        IsReelsRejected = request?.IsRejected ?? false,
                        RejectionReason = request?.RejectionReason ?? "",
                        IsReelsInProgress = request?.IsInProgress ?? false
                    };
                }).ToList()
            };

            return View(model);
        }

        // ================= TOGGLE REELS REQUEST =================
        [HttpPost]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> ToggleReelsRequest(int id, [FromBody] ReelsRequestDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return BadRequest();

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.UserId == user.Id);
            if (org == null) return BadRequest();

            var request = await _context.ReelsRequests
                .FirstOrDefaultAsync(r => r.OpportunityId == id && r.OrganizationId == org.Id);

            if (dto.RequestReels)
            {
                if (request == null)
                {
                    request = new ReelsRequest
                    {
                        OpportunityId = id,
                        OrganizationId = org.Id,
                        RequestDate = DateTime.Now,
                        IsCompleted = false
                    };
                    _context.ReelsRequests.Add(request);
                }
            }
            else
            {
                if (request != null)
                {
                    _context.ReelsRequests.Remove(request);
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        public class ReelsRequestDto
        {
            public bool RequestReels { get; set; }
        }


    }
}
