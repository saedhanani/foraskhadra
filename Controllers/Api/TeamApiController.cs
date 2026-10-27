using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers.Api
{
    /// <summary>
    /// واجهة منصة الفريق (team.foraskhadra.com). المصادقة بمفتاح مشترك في الترويسة X-Team-Key
    /// يُضبط في الإعدادات: "TeamApi": { "Key": "...", "UserEmail": "admin@..." }.
    /// </summary>
    [ApiController]
    [Route("api/team")]
    [AllowAnonymous]
    [TeamApiKey]
    [IgnoreAntiforgeryToken] // المصادقة بالمفتاح لا بالكوكي، فلا ينطبق عليها CSRF
    [EnableRateLimiting("api")]
    public class TeamApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IConfiguration _cfg;

        public TeamApiController(ApplicationDbContext db, UserManager<ApplicationUser> users, IConfiguration cfg)
        {
            _db = db;
            _users = users;
            _cfg = cfg;
        }

        private string BaseUrl => (_cfg["BaseUrl"] ?? "https://www.foraskhadra.com/").TrimEnd('/');

        // ------------------------------------------------------------------ helpers

        /// <summary>تطبيع الرابط للمقارنة: دون بروتوكول/www/utm/شرطة أخيرة.</summary>
        public static string NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u)) return url.Trim().ToLowerInvariant();
            var host = Regex.Replace(u.Host.ToLowerInvariant(), "^www\\.", "");
            var path = u.AbsolutePath.TrimEnd('/');
            var q = u.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(kv => !Regex.IsMatch(kv, "^(utm_|fbclid|gclid|igsh|ref=|source=|si=)", RegexOptions.IgnoreCase))
                .OrderBy(kv => kv, StringComparer.Ordinal);
            var qs = string.Join("&", q);
            return host + path + (qs.Length > 0 ? "?" + qs : "");
        }

        private static OpportunityType? ParseType(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return null;
            return Enum.TryParse<OpportunityType>(t, true, out var v) ? v : null;
        }

        private object Shape(Opportunity o) => new
        {
            id = o.Id,
            status = o.Status.ToString().ToLowerInvariant(),
            titleAr = o.TitleAr,
            titleEn = o.TitleEn,
            type = o.Type?.ToString(),
            endDate = o.EndDate?.ToString("yyyy-MM-dd"),
            applyLink = o.ApplyLink,
            hasImage = !string.IsNullOrEmpty(o.ImagePath),
            pipeId = o.PipeId,
            aiGenerated = o.AiGenerated,
            publishDate = o.PublishDate.ToString("yyyy-MM-dd"),
            detailsUrl = $"{BaseUrl}/Opportunity/Details/{o.Id}",
            editUrl = $"{BaseUrl}/Opportunity/Edit/{o.Id}",
        };

        // ------------------------------------------------------------------ endpoints

        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { ok = true, site = BaseUrl, time = DateTime.UtcNow });

        /// <summary>قائمة الدول كما هي في قاعدة الموقع، لتختار منها منصة الفريق.</summary>
        [HttpGet("countries")]
        public async Task<IActionResult> Countries()
        {
            var list = await _db.Countries.OrderBy(c => c.NameAr).Select(c => new { id = c.Id, nameAr = c.NameAr, nameEn = c.NameEn }).ToListAsync();
            return Ok(list);
        }

        /// <summary>
        /// كشف التكرار: بالرابط (ApplyLink أو SourceUrl بعد التطبيع) أو بكلمات من العنوان.
        /// يعيد المنشور والمسودات معًا؛ الفرص المنتهية منذ أكثر من سنة تُستثنى.
        /// </summary>
        [HttpGet("opportunities/search")]
        public async Task<IActionResult> Search([FromQuery] string? url, [FromQuery] string? q, [FromQuery] int limit = 10)
        {
            limit = Math.Clamp(limit, 1, 50);
            var cutoff = DateTime.Now.AddYears(-1);
            var baseQ = _db.Opportunities.Where(o => o.EndDate == null || o.EndDate >= cutoff);
            var found = new List<Opportunity>();

            var norm = NormalizeUrl(url);
            if (norm.Length > 0)
            {
                // المقارنة بالمضيف أولًا في SQL ثم التطبيع الكامل في الذاكرة
                var host = norm.Split('/')[0].Split('?')[0];
                var cands = await baseQ.Where(o => o.ApplyLink.Contains(host) || (o.SourceUrl != null && o.SourceUrl.Contains(host))).ToListAsync();
                found.AddRange(cands.Where(o => NormalizeUrl(o.ApplyLink) == norm || NormalizeUrl(o.SourceUrl) == norm));
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var words = Regex.Split(q.Trim(), "\\s+").Where(w => w.Length >= 4).Take(5).ToList();
                if (words.Count > 0)
                {
                    var titleQ = baseQ;
                    foreach (var w in words)
                    {
                        var ww = w;
                        titleQ = titleQ.Where(o => o.TitleAr.Contains(ww) || o.TitleEn.Contains(ww));
                    }
                    found.AddRange(await titleQ.OrderByDescending(o => o.Id).Take(limit).ToListAsync());
                }
            }
            var items = found.GroupBy(o => o.Id).Select(g => g.First()).OrderByDescending(o => o.Id).Take(limit).Select(Shape);
            return Ok(new { items });
        }

        /// <summary>أمثلة أسلوب: آخر الفرص المنشورة بنصها العربي، لتغذية التوليد الآلي.</summary>
        [HttpGet("opportunities/examples")]
        public async Task<IActionResult> Examples([FromQuery] int n = 12, [FromQuery] string? type = null)
        {
            n = Math.Clamp(n, 1, 40);
            var t = ParseType(type);
            var q = _db.Opportunities.Where(o => o.Status == OpportunityStatus.Published && !o.AiGenerated);
            if (t != null) q = q.Where(o => o.Type == t);
            var rows = await q.OrderByDescending(o => o.PublishDate).Take(n)
                .Select(o => new { o.Id, o.TitleAr, o.TitleEn, type = o.Type.ToString(), o.DescriptionAr, o.DetailsAr, o.EligibilityCriteriaAr, o.BenefitsAr, o.DescriptionEn, o.DetailsEn })
                .ToListAsync();
            return Ok(new { items = rows });
        }

        [HttpGet("opportunities/{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var o = await _db.Opportunities.Include(x => x.AvailableCountries).FirstOrDefaultAsync(x => x.Id == id);
            return o == null ? NotFound(new { error = "not_found" }) : Ok(Shape(o));
        }

        public class CreateDto
        {
            public string TitleAr { get; set; } = "";
            public string TitleEn { get; set; } = "";
            public string TitleFr { get; set; } = "";
            public string DescriptionAr { get; set; } = "";
            public string DescriptionEn { get; set; } = "";
            public string DescriptionFr { get; set; } = "";
            public string DetailsAr { get; set; } = "";
            public string DetailsEn { get; set; } = "";
            public string DetailsFr { get; set; } = "";
            public string EligibilityCriteriaAr { get; set; } = "";
            public string EligibilityCriteriaEn { get; set; } = "";
            public string EligibilityCriteriaFr { get; set; } = "";
            public string BenefitsAr { get; set; } = "";
            public string BenefitsEn { get; set; } = "";
            public string BenefitsFr { get; set; } = "";
            /// <summary>اسم النوع كما في OpportunityType: Competitions, Conferences, Volunteering, Fellowships, Internships, Jobs, Scholarships</summary>
            public string Type { get; set; } = "";
            /// <summary>specific | untilFull | notSpecified</summary>
            public string DeadlineType { get; set; } = "specific";
            public string? EndDate { get; set; }
            public string ApplyLink { get; set; } = "";
            public string? SourceUrl { get; set; }
            public int? PipeId { get; set; }
            public List<int>? CountryIds { get; set; }
            public bool AiGenerated { get; set; } = true;
            /// <summary>true = ينشر مباشرة (يتطلب صورة)، الافتراضي مسودة.</summary>
            public bool Publish { get; set; } = false;
        }

        /// <summary>إنشاء فرصة (مسودة افتراضيًا) من منصة الفريق.</summary>
        [HttpPost("opportunities")]
        public async Task<IActionResult> Create([FromBody] CreateDto d)
        {
            string Need(string v, string name) => string.IsNullOrWhiteSpace(v) ? name : "";
            var missing = new[] { Need(d.TitleAr, "titleAr"), Need(d.TitleEn, "titleEn"), Need(d.DescriptionAr, "descriptionAr"), Need(d.DetailsAr, "detailsAr"), Need(d.EligibilityCriteriaAr, "eligibilityCriteriaAr"), Need(d.BenefitsAr, "benefitsAr"), Need(d.ApplyLink, "applyLink") }
                .Where(x => x.Length > 0).ToList();
            if (missing.Count > 0) return BadRequest(new { error = "missing", fields = missing });
            var type = ParseType(d.Type);
            if (type == null) return BadRequest(new { error = "bad_type", allowed = Enum.GetNames(typeof(OpportunityType)) });
            if (!Uri.TryCreate(d.ApplyLink, UriKind.Absolute, out _)) return BadRequest(new { error = "bad_link" });

            if (d.PipeId != null)
            {
                var existing = await _db.Opportunities.FirstOrDefaultAsync(o => o.PipeId == d.PipeId);
                if (existing != null) return Conflict(new { error = "exists", item = Shape(existing) });
            }

            var user = await ApiUserAsync();
            if (user == null) return StatusCode(500, new { error = "no_api_user", hint = "اضبط TeamApi:UserEmail لمستخدم بدور Admin" });

            var dt = (d.DeadlineType ?? "specific").ToLowerInvariant() switch
            {
                "untilfull" => Models.DeadlineType.UntilFull,
                "notspecified" => Models.DeadlineType.NotSpecified,
                _ => Models.DeadlineType.SpecificDate,
            };
            DateTime? end = null;
            if (dt == Models.DeadlineType.SpecificDate)
            {
                if (!DateTime.TryParse(d.EndDate, out var e)) return BadRequest(new { error = "bad_end_date" });
                end = e.Date;
            }
            // تعقيم HTML المولّد آليًا قبل التخزين (يُعرض لاحقًا بـ Html.Raw)
            d.DescriptionAr = SafeHtml.Clean(d.DescriptionAr); d.DescriptionEn = SafeHtml.Clean(d.DescriptionEn); d.DescriptionFr = SafeHtml.Clean(d.DescriptionFr);
            d.DetailsAr = SafeHtml.Clean(d.DetailsAr); d.DetailsEn = SafeHtml.Clean(d.DetailsEn); d.DetailsFr = SafeHtml.Clean(d.DetailsFr);
            d.EligibilityCriteriaAr = SafeHtml.Clean(d.EligibilityCriteriaAr); d.EligibilityCriteriaEn = SafeHtml.Clean(d.EligibilityCriteriaEn); d.EligibilityCriteriaFr = SafeHtml.Clean(d.EligibilityCriteriaFr);
            d.BenefitsAr = SafeHtml.Clean(d.BenefitsAr); d.BenefitsEn = SafeHtml.Clean(d.BenefitsEn); d.BenefitsFr = SafeHtml.Clean(d.BenefitsFr);

            // الفرنسية والإنجليزية: إن غابت، تُملأ بالإنجليزية/العربية حتى لا يفشل العرض
            string Or(string v, string fb) => string.IsNullOrWhiteSpace(v) ? fb : v;
            var countries = d.CountryIds is { Count: > 0 }
                ? await _db.Countries.Where(c => d.CountryIds.Contains(c.Id)).ToListAsync()
                : new List<Country>();

            var o = new Opportunity
            {
                TitleAr = d.TitleAr.Trim(), TitleEn = d.TitleEn.Trim(), TitleFr = Or(d.TitleFr, d.TitleEn).Trim(),
                DescriptionAr = d.DescriptionAr, DescriptionEn = Or(d.DescriptionEn, d.DescriptionAr), DescriptionFr = Or(d.DescriptionFr, Or(d.DescriptionEn, d.DescriptionAr)),
                DetailsAr = d.DetailsAr, DetailsEn = Or(d.DetailsEn, d.DetailsAr), DetailsFr = Or(d.DetailsFr, Or(d.DetailsEn, d.DetailsAr)),
                EligibilityCriteriaAr = d.EligibilityCriteriaAr, EligibilityCriteriaEn = Or(d.EligibilityCriteriaEn, d.EligibilityCriteriaAr), EligibilityCriteriaFr = Or(d.EligibilityCriteriaFr, Or(d.EligibilityCriteriaEn, d.EligibilityCriteriaAr)),
                BenefitsAr = d.BenefitsAr, BenefitsEn = Or(d.BenefitsEn, d.BenefitsAr), BenefitsFr = Or(d.BenefitsFr, Or(d.BenefitsEn, d.BenefitsAr)),
                Type = type, DeadlineType = dt, EndDate = end, ApplyLink = d.ApplyLink.Trim(),
                AvailableCountries = countries,
                ImagePath = "",
                PublishedBy = "Admin",
                CreatedByUserId = user.Id,
                PublishDate = DateTime.Now,
                Status = OpportunityStatus.Draft,
                SourceUrl = d.SourceUrl,
                PipeId = d.PipeId,
                AiGenerated = d.AiGenerated,
            };
            if (d.Publish)
            {
                return BadRequest(new { error = "no_image", hint = "النشر المباشر يتطلب صورة؛ أنشئ مسودة ثم ارفع الصورة وانشر." });
            }
            _db.Opportunities.Add(o);
            await _db.SaveChangesAsync();
            return Ok(Shape(o));
        }

        /// <summary>اعتماد مسودة ونشرها. يتطلب صورة مرفوعة من صفحة التعديل.</summary>
        [HttpPost("opportunities/{id:int}/publish")]
        public async Task<IActionResult> Publish(int id)
        {
            var o = await _db.Opportunities.FirstOrDefaultAsync(x => x.Id == id);
            if (o == null) return NotFound(new { error = "not_found" });
            if (string.IsNullOrEmpty(o.ImagePath)) return BadRequest(new { error = "no_image", editUrl = $"{BaseUrl}/Opportunity/Edit/{o.Id}" });
            if (o.DeadlineType == Models.DeadlineType.SpecificDate && o.EndDate != null && o.EndDate.Value.Date < DateTime.Now.Date)
                return BadRequest(new { error = "expired" });
            if (o.Status != OpportunityStatus.Published)
            {
                o.Status = OpportunityStatus.Published;
                o.PublishDate = DateTime.Now;
                await _db.SaveChangesAsync();
            }
            return Ok(Shape(o));
        }

        /// <summary>إلغاء مسودة لم تُنشر (عند رفض الفرصة في المنصة). المنشور لا يُحذف من هنا.</summary>
        [HttpPost("opportunities/{id:int}/discard")]
        public async Task<IActionResult> Discard(int id)
        {
            var o = await _db.Opportunities.FirstOrDefaultAsync(x => x.Id == id);
            if (o == null) return Ok(new { ok = true, gone = true });
            if (o.Status == OpportunityStatus.Published) return Conflict(new { error = "published" });
            _db.Opportunities.Remove(o);
            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        private async Task<ApplicationUser?> ApiUserAsync()
        {
            var email = _cfg["TeamApi:UserEmail"];
            if (!string.IsNullOrWhiteSpace(email))
            {
                var u = await _users.FindByEmailAsync(email);
                if (u != null) return u;
            }
            var admins = await _users.GetUsersInRoleAsync("Admin");
            return admins.OrderBy(a => a.CreatedAt).FirstOrDefault();
        }
    }

    /// <summary>يرفض كل طلب لا يحمل X-Team-Key مطابقًا لـ TeamApi:Key (مقارنة ثابتة الزمن).</summary>
    public class TeamApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var cfg = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expected = cfg["TeamApi:Key"] ?? "";
            var given = ctx.HttpContext.Request.Headers["X-Team-Key"].FirstOrDefault() ?? "";
            if (expected.Length < 24 || given.Length == 0 || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(given)))
            {
                ctx.Result = new UnauthorizedObjectResult(new { error = "unauthorized" });
                return;
            }
            await next();
        }
    }
}
