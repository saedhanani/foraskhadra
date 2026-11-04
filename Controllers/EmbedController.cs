using System.Globalization;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Foras_Khadra.Controllers
{
    /// <summary>عنصر في الودجت المضمَّن — ما تحتاجه البطاقة فقط.</summary>
    public sealed record EmbedItem(int Id, string Title, OpportunityType? Type, DeadlineType? DeadlineType, DateTime? EndDate, string? Image, string? HostCountryIso, bool IsRemote);

    /// <summary>
    /// أداة التضمين للجامعات والمنظمات (المرحلة 3.3): /embed صفحة المولّد (قصاصة HTML + شارة «نُشر على فرص خضراء»)،
    /// و/embed/opportunities الودجت نفسه داخل iframe (ترويسات الإطار تُستثنى لهذا المسار في Program.cs).
    /// كل الروابط تعود إلى الموقع بـutm_source=embed فتظهر في التحليلات كمصدر مستقل.
    /// </summary>
    [AllowAnonymous]
    public class EmbedController : Controller
    {
        public const int DefaultLimit = 6;
        public const int MaxLimit = 20;

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public EmbedController(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        [HttpGet("/{culture:culture}/embed")]
        [HttpGet("/embed", Order = 1)]
        public async Task<IActionResult> Index()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            ViewData["Title"] = lang == "en" ? "Embed opportunities on your website" : lang == "fr" ? "Intégrer les opportunités sur votre site" : "أداة التضمين: اعرض الفرص على موقعك";
            ViewData["Description"] = lang == "en"
                ? "A free widget for universities and organizations: open opportunities for youth in the Arab world on your own website, plus the “Published on Foras Khadra” badge."
                : lang == "fr"
                    ? "Un widget gratuit pour les universités et organisations : opportunités ouvertes aux jeunes du monde arabe sur votre site, et le badge « Publié sur Foras Khadra »."
                    : "ودجت مجاني للجامعات والمنظمات: الفرص المفتوحة لشباب الوطن العربي على موقعك، وشارة «نُشر على فرص خضراء».";

            var countries = await _db.Countries.AsNoTracking()
                .Where(c => c.IsoCode != null)
                .ToListAsync();
            ViewBag.Countries = countries
                .Where(c => CompassMatcher.ArabIso.Contains(c.IsoCode!))
                .Select(c => (Iso: c.IsoCode!.ToUpperInvariant(), Name: lang == "en" ? c.NameEn : lang == "fr" ? c.NameFr : c.NameAr))
                .OrderBy(c => c.Name)
                .ToList();
            return View();
        }

        [HttpGet("/{culture:culture}/embed/opportunities")]
        [HttpGet("/embed/opportunities", Order = 1)]
        public async Task<IActionResult> Opportunities([FromQuery] OpportunityQuery q, int limit = DefaultLimit, string? theme = null)
        {
            limit = Math.Clamp(limit, 1, MaxLimit);
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var cacheKey = "embed:" + lang + ":" + limit + ":" + Request.QueryString.Value;

            var items = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                var countries = await _db.Countries.AsNoTracking().ToListAsync();
                var parsed = OpportunityFilter.Parse(q, countries);
                var today = DateTime.Now.Date;
                var open = OpportunityFilter.Apply(_db.Opportunities.AsNoTracking(), parsed, today)
                    .Where(o => o.Status == OpportunityStatus.Published
                        && (o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today));
                return await OpportunityFilter.Sort(open, "newest", today)
                    .Take(limit)
                    .Select(o => new EmbedItem(
                        o.Id,
                        lang == "en" ? o.TitleEn : lang == "fr" ? o.TitleFr : o.TitleAr,
                        o.Type, o.DeadlineType, o.EndDate, o.ImagePath, o.HostCountryIso, o.IsRemote))
                    .ToListAsync();
            }) ?? new List<EmbedItem>();

            Response.Headers.CacheControl = "public,max-age=600";
            ViewBag.Theme = theme == "dark" ? "dark" : "light";
            return View(items);
        }
    }
}
