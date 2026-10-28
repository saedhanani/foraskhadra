using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers.Api
{
    /// <summary>
    /// الواجهة العامة للقراءة فقط (بلا مفتاح): الفرص المنشورة والمقالات بالثلاث لغات.
    /// GET /api/v1/opportunities?type=Jobs&amp;country=فلسطين&amp;q=منحة&amp;page=1&amp;pageSize=20
    /// GET /api/v1/opportunities/{id}
    /// GET /api/v1/articles?page=1&amp;pageSize=20 · GET /api/v1/articles/{id}
    /// الحد: 120 طلبًا في الدقيقة لكل عنوان IP (سياسة "api"). الردود قابلة للتخزين 60 ثانية.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableCors("public-api")]
    [EnableRateLimiting("api")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [Produces("application/json")]
    public class PublicApiController : ControllerBase
    {
        public const int MaxPageSize = 50;

        private readonly ApplicationDbContext _db;

        public PublicApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        private string Absolute(string? path) =>
            string.IsNullOrEmpty(path) ? "" : path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : BaseUrl + path;

        private static object Localized(string? ar, string? en, string? fr) => new { ar, en, fr };

        private object Summary(Opportunity o) => new
        {
            id = o.Id,
            title = Localized(o.TitleAr, o.TitleEn, o.TitleFr),
            description = Localized(o.DescriptionAr, o.DescriptionEn, o.DescriptionFr),
            type = o.Type?.ToString(),
            publishDate = o.PublishDate,
            deadlineType = o.DeadlineType?.ToString(),
            endDate = o.EndDate,
            countries = (o.AvailableCountries ?? new List<Country>()).Select(c => Localized(c.NameAr, c.NameEn, c.NameFr)),
            image = Absolute(o.ImagePath),
            url = BaseUrl + Url.Action("Details", "Opportunity", new { id = o.Id, slug = SlugHelper.Make(o.TitleAr) }),
            applyLink = o.ApplyLink,
            publishedBy = string.Equals(o.PublishedBy, "admin", StringComparison.OrdinalIgnoreCase) ? "فرص خضراء" : o.PublishedBy
        };

        [HttpGet("opportunities")]
        public async Task<IActionResult> Opportunities(string? type, string? country, string? q, int page = 1, int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _db.Opportunities
                .AsNoTracking()
                .Include(o => o.AvailableCountries)
                .Where(o => o.Status == OpportunityStatus.Published);

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!Enum.TryParse<OpportunityType>(type, true, out var parsedType))
                    return BadRequest(new { error = "unknown type", allowed = Enum.GetNames<OpportunityType>() });
                query = query.Where(o => o.Type == parsedType);
            }

            if (!string.IsNullOrWhiteSpace(country))
                query = query.Where(o => o.AvailableCountries.Any(c => c.NameAr == country || c.NameEn == country || c.NameFr == country));

            foreach (var term in SearchIndexer.Terms(q))
            {
                var pattern = $"%{term}%";
                query = query.Where(o => EF.Functions.Like(o.SearchText, pattern));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { page, pageSize, total, items = items.Select(Summary) });
        }

        [HttpGet("opportunities/{id:int}")]
        public async Task<IActionResult> Opportunity(int id)
        {
            var o = await _db.Opportunities
                .AsNoTracking()
                .Include(x => x.AvailableCountries)
                .FirstOrDefaultAsync(x => x.Id == id && x.Status == OpportunityStatus.Published);
            if (o == null) return NotFound();

            return Ok(new
            {
                summary = Summary(o),
                details = Localized(o.DetailsAr, o.DetailsEn, o.DetailsFr),
                eligibility = Localized(o.EligibilityCriteriaAr, o.EligibilityCriteriaEn, o.EligibilityCriteriaFr),
                benefits = Localized(o.BenefitsAr, o.BenefitsEn, o.BenefitsFr)
            });
        }

        private object ArticleSummary(Article a) => new
        {
            id = a.Id,
            title = Localized(a.TitleAr, a.TitleEn, a.TitleFr),
            author = a.Author,
            publishDate = a.PublishDate,
            image = Absolute(a.ImagePath),
            url = BaseUrl + Url.Action("Details", "Articles", new { id = a.Id, slug = SlugHelper.Make(a.TitleAr) })
        };

        [HttpGet("articles")]
        public async Task<IActionResult> Articles(int page = 1, int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _db.Articles.AsNoTracking();
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { page, pageSize, total, items = items.Select(ArticleSummary) });
        }

        [HttpGet("articles/{id:int}")]
        public async Task<IActionResult> Article(int id)
        {
            var a = await _db.Articles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            return Ok(new
            {
                summary = ArticleSummary(a),
                content = Localized(a.ContentAr, a.ContentEn, a.ContentFr)
            });
        }
    }
}
