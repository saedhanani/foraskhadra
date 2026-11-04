using Foras_Khadra.Helpers;
using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Foras_Khadra.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.IO;
namespace Foras_Khadra.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUploadService _images;
        private readonly IOpportunityReviewService _review;
        private readonly IPublishRequestService _requests;

        public AdminController(ApplicationDbContext context, IImageUploadService images, IOpportunityReviewService review, IPublishRequestService requests)
        {
            _context = context;
            _images = images;
            _review = review;
            _requests = requests;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalReelsRequests = await _context.ReelsRequests.CountAsync();
            var pendingReelsRequests = await _context.ReelsRequests.CountAsync(r => r.IsInProgress);
            var completedReelsRequests = await _context.ReelsRequests.CountAsync(r => r.IsCompleted);
            var rejectedReelsRequests = await _context.ReelsRequests.CountAsync(r => r.IsRejected);

            var model = new AdminDashboardViewModel
            {
                TotalOrganizations = await _context.Organizations.CountAsync(),
                TotalUsers = await _context.TeamMember.CountAsync(),
                TotalRegisteredUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),
                TotalOpportunities = await _context.Opportunities.CountAsync(),
                TotalArticles = await _context.Articles.CountAsync(),
                TotalReelsRequests = totalReelsRequests,
                PendingReelsRequests = pendingReelsRequests,
                CompletedReelsRequests = completedReelsRequests,
                RejectReelsRequests = rejectedReelsRequests,
                TotalManualOrganizations = await _context.ManualOrganizations.CountAsync(),
                PendingReviewCount = await _review.PendingCountAsync(),
                OpenPublishRequests = await _requests.OpenCountAsync()
            };

            return View(model);
        }

        // ===== مراجعة فرص المنظمات قبل النشر (Services/OpportunityReviewService) =====
        [HttpGet]
        public async Task<IActionResult> Review(string? status = null)
        {
            var wanted = status == "rejected" ? OpportunityStatus.Rejected : OpportunityStatus.PendingReview;
            var list = await _context.Opportunities.AsNoTracking()
                .Where(o => o.Status == wanted)
                .OrderBy(o => o.PublishDate)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveOpportunity(int id, bool verifyOrganization = false)
        {
            TempData["ReviewMessage"] = await _review.ApproveAsync(id, verifyOrganization)
                ? (verifyOrganization ? "نُشرت الفرصة ووُثِّقت المنظمة؛ فرصها القادمة تُنشر مباشرة." : "نُشرت الفرصة.")
                : "تعذّر اعتماد هذه الفرصة.";
            return RedirectToAction(nameof(Review));
        }

        [HttpPost]
        public async Task<IActionResult> RejectOpportunity(int id, string? note)
        {
            TempData["ReviewMessage"] = await _review.RejectAsync(id, note)
                ? "رُفضت الفرصة؛ السبب يظهر للمنظمة في قائمة فرصها."
                : "تعذّر رفض هذه الفرصة.";
            return RedirectToAction(nameof(Review));
        }

        // ===== التحليلات الداخلية بلا كوكيز (Services/Analytics) =====
        [HttpGet]
        public async Task<IActionResult> Analytics(int days = 7)
        {
            days = days switch { 30 => 30, 90 => 90, _ => 7 };
            var to = DateOnly.FromDateTime(DateTime.Now);
            var from = to.AddDays(-(days - 1));
            var previousFrom = from.AddDays(-days);

            var views = await _context.PageViewDailyStats.AsNoTracking().Where(r => r.Day >= from && r.Day <= to).ToListAsync();
            var sources = await _context.TrafficSourceDailyStats.AsNoTracking().Where(r => r.Day >= from && r.Day <= to).ToListAsync();
            var events = await _context.SiteEventDailyStats.AsNoTracking().Where(r => r.Day >= from && r.Day <= to).ToListAsync();
            var oppStats = await _context.OpportunityDailyStats.AsNoTracking().Where(r => r.Day >= from && r.Day <= to).ToListAsync();
            var previous = await _context.PageViewDailyStats.AsNoTracking().Where(r => r.Day >= previousFrom && r.Day < from).SumAsync(r => (int?)r.Views) ?? 0;

            var eventTotals = events.Where(e => e.Label == null).GroupBy(e => e.Event).ToDictionary(g => g.Key, g => g.Sum(e => e.Count));
            eventTotals["register.view"] = views.Where(v => v.Path == "/account/registeruser").Sum(v => v.Views);
            eventTotals["search.zero"] = events.Where(e => e.Event == "search.zero").Sum(e => e.Count);

            var topOpportunities = oppStats.GroupBy(s => s.OpportunityId)
                .Select(g => new { Id = g.Key, Views = g.Sum(s => s.Views), Clicks = g.Sum(s => s.ApplyClicks) })
                .OrderByDescending(x => x.Views).ThenByDescending(x => x.Clicks).Take(10).ToList();
            var ids = topOpportunities.Select(x => x.Id).ToList();
            var titles = await _context.Opportunities.AsNoTracking().Where(o => ids.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.TitleAr);

            var vm = new AnalyticsVM
            {
                Days = days, From = from, To = to,
                TotalViews = views.Sum(v => v.Views),
                PreviousTotalViews = previous,
                PerDay = Enumerable.Range(0, days).Select(i => from.AddDays(i)).Select(d => (d, views.Where(v => v.Day == d).Sum(v => v.Views))).ToList(),
                ByCulture = views.GroupBy(v => v.Culture).ToDictionary(g => g.Key, g => g.Sum(v => v.Views)),
                TopPages = views.GroupBy(v => v.Path).Select(g => (g.Key, g.Sum(v => v.Views))).OrderByDescending(x => x.Item2).Take(25).ToList(),
                TopSources = sources.GroupBy(s => s.Source).Select(g => (g.Key, g.Sum(s => s.Views))).OrderByDescending(x => x.Item2).Take(20).ToList(),
                Events = eventTotals,
                ZeroResultSearches = events.Where(e => e.Event == "search.zero" && e.Label != null).GroupBy(e => e.Label!)
                    .Select(g => (g.Key, g.Sum(e => e.Count))).OrderByDescending(x => x.Item2).Take(30).ToList(),
                TopOpportunities = topOpportunities.Select(x => (x.Id, titles.TryGetValue(x.Id, out var t) ? t : $"#{x.Id}", x.Views, x.Clicks)).ToList(),
                ApplyClicks = oppStats.Sum(s => s.ApplyClicks)
            };
            return View(vm);
        }

        // ===== صفحات النية (المرحلة 2.3) =====
        [HttpGet]
        public async Task<IActionResult> LandingPages()
        {
            var countries = await _context.Countries.AsNoTracking().ToListAsync();
            var pages = await _context.LandingPages.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Slug).ToListAsync();
            var rows = new List<(LandingPage Page, int OpenCount)>();
            foreach (var p in pages) rows.Add((p, await LandingPage.OpenCountAsync(_context, p, countries, DateTime.Now)));
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> LandingPageEdit(int? id)
        {
            var form = new LandingPageForm { SortOrder = await _context.LandingPages.CountAsync() };
            if (id is > 0)
            {
                var page = await _context.LandingPages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (page == null) return NotFound();
                form = LandingPageForm.From(page);
            }
            await FillLandingOptionsAsync();
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LandingPageEdit(LandingPageForm form)
        {
            var slug = (form.Slug ?? "").Trim().ToLowerInvariant();
            if (OpportunityTypePages.BySlug(slug) != null) ModelState.AddModelError(nameof(form.Slug), "هذا الاسم محجوز لصفحة نوع.");
            if (await _context.LandingPages.AnyAsync(p => p.Slug == slug && p.Id != form.Id)) ModelState.AddModelError(nameof(form.Slug), "الرابط مستخدم في صفحة أخرى.");
            if (!ModelState.IsValid)
            {
                await FillLandingOptionsAsync();
                return View(form);
            }
            var page = form.Id > 0 ? await _context.LandingPages.FirstOrDefaultAsync(p => p.Id == form.Id) : null;
            if (page == null)
            {
                page = new LandingPage();
                _context.LandingPages.Add(page);
            }
            form.ApplyTo(page);
            await _context.SaveChangesAsync();
            TempData["LandingMessage"] = $"حُفظت الصفحة /opportunities/{page.Slug}.";
            return RedirectToAction(nameof(LandingPages));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LandingPageDelete(int id)
        {
            var page = await _context.LandingPages.FirstOrDefaultAsync(p => p.Id == id);
            if (page != null)
            {
                _context.LandingPages.Remove(page);
                await _context.SaveChangesAsync();
                TempData["LandingMessage"] = "حُذفت الصفحة.";
            }
            return RedirectToAction(nameof(LandingPages));
        }

        /// <summary>الدفعة الأولى (Data/LandingPageSeed): يضيف الناقص بالـslug فقط ولا يمسّ الموجود.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LandingPagesSeed()
        {
            var countries = await _context.Countries.AsNoTracking().ToListAsync();
            var existing = (await _context.LandingPages.Select(p => p.Slug).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var added = 0;
            foreach (var page in LandingPageSeed.All(countries))
            {
                if (existing.Contains(page.Slug)) continue;
                _context.LandingPages.Add(page);
                added++;
            }
            await _context.SaveChangesAsync();
            TempData["LandingMessage"] = added == 0 ? "لا جديد: كل صفحات الدفعة الأولى موجودة." : $"أُضيفت {added} صفحة.";
            return RedirectToAction(nameof(LandingPages));
        }

        private async Task FillLandingOptionsAsync()
        {
            ViewBag.Fields = await _context.Fields.AsNoTracking().Where(f => f.Key != Field.AnyKey).OrderBy(f => f.SortOrder).ToListAsync();
            ViewBag.Countries = (await _context.Countries.AsNoTracking().ToListAsync())
                .Where(c => c.IsoCode != null && CompassMatcher.ArabIso.Contains(c.IsoCode))
                .Select(c => new CountryOption(c.IsoCode!.ToUpperInvariant(), c.NameAr)).OrderBy(o => o.Name).ToList();
            var hostIso = await _context.Opportunities.AsNoTracking().Where(o => o.HostCountryIso != null && o.HostCountryIso != "").Select(o => o.HostCountryIso!).Distinct().ToListAsync();
            ViewBag.Hosts = hostIso.Select(iso => new CountryOption(iso.ToUpperInvariant(), CountryHelper.NameForIso(iso, "ar") ?? iso)).OrderBy(o => o.Name).ToList();
        }

        // ===== طلبات النشر من بوابة المنظمات (Services/PublishRequestService) =====
        [HttpGet]
        public async Task<IActionResult> PublishRequests(string? status = null)
        {
            var closed = status == "closed";
            var list = await _context.PublishRequests.AsNoTracking()
                .Where(r => closed
                    ? r.Status == PublishRequestStatus.Published || r.Status == PublishRequestStatus.Rejected || r.Status == PublishRequestStatus.Spam
                    : r.Status == PublishRequestStatus.New || r.Status == PublishRequestStatus.InProgress)
                .OrderByDescending(r => r.CreatedAt)
                .Take(200)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkPublishRequest(int id, int opportunityId)
        {
            TempData["RequestMessage"] = await _requests.LinkOpportunityAsync(id, opportunityId)
                ? "رُبط الطلب بالفرصة؛ إن كانت منشورة فقد أُغلق وأُرسل رابطها للمنظمة، وإلا يُغلق عند نشرها."
                : "تعذّر الربط: تحقق من رقم الفرصة وحالة الطلب.";
            return RedirectToAction(nameof(PublishRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPublishRequest(int id, string? note, bool spam = false)
        {
            TempData["RequestMessage"] = await _requests.RejectAsync(id, note, spam)
                ? (spam ? "عُلِّم الطلب كمزعج." : "رُفض الطلب وأُرسل السبب للمنظمة.")
                : "تعذّر رفض هذا الطلب.";
            return RedirectToAction(nameof(PublishRequests));
        }

        [HttpPost]
        public async Task<IActionResult> SetOrganizationVerified(int id, bool verified)
        {
            await _review.SetOrganizationVerifiedAsync(id, verified);
            return RedirectToAction(nameof(OrganizationDetails), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> SetOrganizationSuspended(int id, bool suspended)
        {
            await _review.SetOrganizationSuspendedAsync(id, suspended);
            return RedirectToAction(nameof(OrganizationDetails), new { id });
        }

        // ===== تعبئة بيانات الأهلية للفرص القديمة (Helpers/FacetGuesser يقترح، المدير يؤكد) =====
        private const int FacetsPageSize = 10;

        [HttpGet]
        public async Task<IActionResult> Facets(int page = 1)
        {
            page = Math.Clamp(page, 1, 10000);
            var q = _context.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published && o.FundingType == FundingType.Unknown
                            && (o.EndDate == null || o.EndDate >= DateTime.Now.Date));
            ViewData["Remaining"] = await q.CountAsync();
            ViewData["Page"] = page;
            ViewBag.FieldOptions = await _context.Fields.OrderBy(f => f.SortOrder).ToListAsync();
            var list = await q.Include(o => o.Fields).OrderByDescending(o => o.PublishDate)
                .Skip((page - 1) * FacetsPageSize).Take(FacetsPageSize).ToListAsync();
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFacets(int id, FacetFormVM facets, int page = 1)
        {
            var opp = await _context.Opportunities.Include(o => o.Fields).FirstOrDefaultAsync(o => o.Id == id);
            if (opp == null) return NotFound();
            var error = facets.ApplyTo(opp, await _context.Fields.ToListAsync());
            if (error != null) { TempData["FacetMessage"] = "لم يُحفظ: " + error; return RedirectToAction(nameof(Facets), new { page }); }
            opp.LastCheckedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["FacetMessage"] = $"حُفظت بيانات «{opp.TitleAr}».";
            return RedirectToAction(nameof(Facets), new { page });
        }

        // ===== النسخ الاحتياطي اليومي (Services/BackupExporter) =====
        [HttpGet]
        public IActionResult Backups([FromServices] IBackupExporter backups)
        {
            ViewData["Folder"] = backups.Folder;
            return View(backups.List());
        }

        [HttpPost]
        public async Task<IActionResult> RunBackup([FromServices] IBackupExporter backups)
        {
            var result = await backups.ExportAsync(HttpContext.RequestAborted);
            TempData["BackupMessage"] = $"كُتبت النسخة {result.FileName} ({result.Bytes / 1024} KB، {result.Counts.Count} جدولًا).";
            return RedirectToAction(nameof(Backups));
        }

        [HttpGet]
        public IActionResult DownloadBackup(string name, [FromServices] IBackupExporter backups)
        {
            if (!BackupExporter.IsValidFileName(name)) return NotFound();
            var path = Path.Combine(backups.Folder, name);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, "application/gzip", name);
        }

        // ===== تحسين الصور المخزّنة (تصغير وإعادة ترميز الصور المرفوعة سابقًا) =====
        [HttpGet]
        public IActionResult OptimizeImages()
        {
            var report = TempData["ImageOptimizeReport"] is string json
                ? System.Text.Json.JsonSerializer.Deserialize<StoredImageReport>(json)
                : null;
            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> OptimizeImages([FromServices] StoredImageOptimizer optimizer)
        {
            // مهلة أقصر من حد الاستضافة للطلب الواحد؛ الصفحة تطلب إعادة التشغيل إن لم تكتمل الجولة
            var report = await optimizer.RunAsync(TimeSpan.FromSeconds(45), HttpContext.RequestAborted);
            TempData["ImageOptimizeReport"] = System.Text.Json.JsonSerializer.Serialize(report);
            return RedirectToAction(nameof(OptimizeImages));
        }

        public IActionResult ReelsRequestsDashboard(string status = null)
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var requests = _context.ReelsRequests
                .Include(r => r.Organization)
                .Include(r => r.Opportunity)
                .Select(r => new ReelsRequestAdminVM
                {
                    Id = r.Id,
                    OpportunityId = r.OpportunityId,
                    OpportunityTitle = lang == "en" ? r.Opportunity.TitleEn :
                               lang == "fr" ? r.Opportunity.TitleFr :
                               r.Opportunity.TitleAr,
                    OpportunityType = r.Opportunity.Type,
                    OrganizationName = r.Organization.Name,
                    RequestDate = r.RequestDate,
                    IsCompleted = r.IsCompleted,
                    IsRejected = r.IsRejected,
                    IsInProgress = r.IsInProgress,

                    RejectionReason = r.RejectionReason
                })
                .ToList();

            if (!string.IsNullOrEmpty(status))
            {
                status = status.ToLower();
                if (status == "pending")
                    requests = requests.Where(r => r.IsInProgress).ToList(); // فقط الطلبات التي حددها الأدمن
                else if (status == "completed")
                    requests = requests.Where(r => r.IsCompleted).ToList();
                else if (status == "rejected")
                    requests = requests.Where(r => r.IsRejected).ToList();
            }

            return View(requests);
        }


        [HttpPost]
        public async Task<IActionResult> ToggleReelsRequest(int id, string newStatus, string? rejectionReason)
        {
            var request = await _context.ReelsRequests.FindAsync(id);
            if (request == null) return NotFound();

            // تعديل الحالة حسب اختيار الادمن ("pending" في القائمة تعني "جاري العمل عليها")
            request.SetStatus(newStatus switch
            {
                "completed" => ReelsRequestStatus.Completed,
                "rejected" => ReelsRequestStatus.Rejected,
                "pending" => ReelsRequestStatus.InProgress,
                _ => ReelsRequestStatus.Pending
            });

            // حفظ سبب الرفض إذا تم رفض الطلب
            request.RejectionReason = request.Status == ReelsRequestStatus.Rejected ? rejectionReason : null;

            await _context.SaveChangesAsync();
            return Ok();
        }


        public async Task<IActionResult> OrganizationsList()
        {
            var organizations = await _context.Organizations.ToListAsync();
            return View(organizations);
        }

        public async Task<IActionResult> OrganizationDetails(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null) return NotFound();
            return View(org);
        }

        [HttpGet]
        public async Task<IActionResult> EditOrganization(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null) return NotFound();

            return View(org);
        }

        [HttpPost]
        public async Task<IActionResult> EditOrganization(int id, Organization formModel, IFormFile logoFile)
        {
            var org = await _context.Organizations.FindAsync(id);

            if (org == null)
                return NotFound();

            // رفع اللوجو
            if (logoFile != null && logoFile.Length > 0)
            {
                var error = _images.Validate(logoFile);
                if (error != null)
                {
                    ModelState.AddModelError("logoFile", error);
                    return View(org);
                }
                org.LogoPath = await _images.SaveAsync(logoFile, "logos");
            }

            // تحديث البيانات من formModel (مش model)
            org.Name = formModel.Name ?? org.Name;
            org.Sector = formModel.Sector ?? org.Sector;
            org.Country = formModel.Country ?? org.Country;
            org.ContactName = formModel.ContactName ?? org.ContactName;
            org.ContactEmail = formModel.ContactEmail ?? org.ContactEmail;
            org.PhoneNumber = formModel.PhoneNumber ?? org.PhoneNumber;
            org.Location = formModel.Location ?? org.Location;
            org.Website = HttpUrl.Normalize(formModel.Website) ?? org.Website;

            await _context.SaveChangesAsync();

            return RedirectToAction("OrganizationsList");
        }


        //--------------------------//

        public async Task<IActionResult> ManualOrganizationsList()
        {
            var organizations = await _context.ManualOrganizations
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(organizations);
        }

        [HttpGet]
        public IActionResult AddManualOrganization()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddManualOrganization(ManualOrganization model, IFormFile logoFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (logoFile != null && logoFile.Length > 0)
            {
                var error = _images.Validate(logoFile);
                if (error != null)
                {
                    ModelState.AddModelError("logoFile", error);
                    return View(model);
                }
                model.LogoPath = await _images.SaveAsync(logoFile, "logos");
            }

            _context.ManualOrganizations.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManualOrganizationsList");
        }

        [HttpGet]
        public async Task<IActionResult> EditManualOrganization(int id)
        {
            var org = await _context.ManualOrganizations.FindAsync(id);

            if (org == null)
                return NotFound();

            return View(org);
        }

        [HttpPost]
        public async Task<IActionResult> EditManualOrganization(ManualOrganization model, IFormFile logoFile)
        {
            var org = await _context.ManualOrganizations.FindAsync(model.Id);

            if (org == null)
                return NotFound();

            if (logoFile != null && logoFile.Length > 0)
            {
                var error = _images.Validate(logoFile);
                if (error != null)
                {
                    ModelState.AddModelError("logoFile", error);
                    return View(model);
                }
                org.LogoPath = await _images.SaveAsync(logoFile, "logos");
            }

            org.OrganizationName = model.OrganizationName;
            org.ContactPersonName = model.ContactPersonName;
            org.Details = model.Details;
            org.PhoneNumber = model.PhoneNumber;
            org.Email = model.Email;
            org.Website = HttpUrl.Normalize(model.Website);
            org.Location = model.Location;
            org.Country = model.Country;
            org.Sector = model.Sector;
            org.IsActive = model.IsActive;


            await _context.SaveChangesAsync();

            return RedirectToAction("ManualOrganizationsList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteManualOrganization(int id)
        {
            var org = await _context.ManualOrganizations.FindAsync(id);

            if (org == null)
                return NotFound();

            _context.ManualOrganizations.Remove(org);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManualOrganizationsList));
        }


        public async Task<IActionResult> ManualOrganizationDetails(int id)
        {
            var org = await _context.ManualOrganizations.FindAsync(id);

            if (org == null)
                return NotFound();

            return View(org);
        }




    }



}
