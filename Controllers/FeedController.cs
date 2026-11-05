using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// قنوات نملكها (المرحلة 4.3): خلاصات Atom للفرص المفتوحة — عامة، أو لكل نوع (/feed/jobs.xml)، أو لكل صفحة نية
    /// (/feed/egypt.xml)، أو بأي فلتر من OpportunityQuery (/feed/opportunities.xml?country=JO&funding=FullyFunded)،
    /// وبثلاث لغات عبر بادئة المسار (/en/feed/…). تُخزَّن 10 دقائق. الروابط تحمل utm_source=rss.
    /// </summary>
    [AllowAnonymous]
    public class FeedController : Controller
    {
        public const int MaxItems = 50;
        private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public FeedController(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        [HttpGet("/{culture:culture}/feed/opportunities.xml")]
        [HttpGet("/feed/opportunities.xml", Order = 1)]
        public Task<IActionResult> Opportunities([FromQuery] OpportunityQuery q) => BuildAsync(q, null, null);

        /// <summary>خلاصة نوع (jobs, scholarships…) أو صفحة نية (egypt, fully-funded…).</summary>
        [HttpGet("/{culture:culture}/feed/{slug}.xml")]
        [HttpGet("/feed/{slug}.xml", Order = 1)]
        public async Task<IActionResult> BySlug(string slug, [FromQuery] OpportunityQuery q)
        {
            var type = OpportunityTypePages.BySlug(slug);
            if (type != null)
            {
                q.Type = new List<string> { type.Type.ToString() };
                return await BuildAsync(q, type, null);
            }
            var landing = await _db.LandingPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
            if (landing == null) return NotFound();
            return await BuildAsync(OpportunityQuery.Merge(landing.Query(), q), null, landing);
        }

        private async Task<IActionResult> BuildAsync(OpportunityQuery q, TypePage? type, LandingPage? landing)
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            string T(string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;
            var site = $"{Request.Scheme}://{Request.Host}";
            var prefix = lang == "ar" ? "" : "/" + lang;
            var selfUrl = site + Request.Path + Request.QueryString;
            var cacheKey = "feed:" + lang + ":" + Request.Path + Request.QueryString;

            var xml = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                var countries = await _db.Countries.AsNoTracking().ToListAsync();
                var parsed = OpportunityFilter.Parse(q, countries);
                var today = DateTime.Now.Date;
                var open = OpportunityFilter.Apply(_db.Opportunities.AsNoTracking(), parsed, today)
                    .Where(o => o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today);
                var items = await OpportunityFilter.Sort(open, "newest", today)
                    .Take(MaxItems)
                    .Select(o => new
                    {
                        o.Id, o.TitleAr, o.TitleEn, o.TitleFr, o.DescriptionAr, o.DescriptionEn, o.DescriptionFr,
                        o.Type, o.PublishDate, o.UpdatedAt, o.EndDate, o.DeadlineType, o.ImagePath, o.PublishedBy, o.HostOrganizationName
                    })
                    .ToListAsync();

                var title = landing != null ? landing.Title(lang)
                    : type != null ? type.Title(lang)
                    : parsed.HasCriteria ? T("فرص مفلترة", "Filtered opportunities", "Opportunités filtrées")
                    : T("أحدث الفرص المفتوحة", "Latest open opportunities", "Dernières opportunités ouvertes");
                var siteName = T("فرص خضراء", "Foras Khadra", "Foras Khadra");
                var htmlUrl = site + prefix + (landing != null ? "/opportunities/" + landing.Slug : type != null ? "/opportunities/" + type.Slug : "/Home/AllOpportunities") + (Request.QueryString.HasValue && landing == null && type == null ? Request.QueryString.Value : "");
                var updated = items.Count > 0 ? items.Max(i => i.UpdatedAt ?? i.PublishDate) : DateTime.Now;

                var feed = new XElement(Atom + "feed",
                    new XAttribute(XNamespace.Xml + "lang", lang),
                    new XElement(Atom + "id", selfUrl), // معرّف فريد لكل خلاصة بما فيها الفلاتر
                    new XElement(Atom + "title", siteName + " — " + title),
                    new XElement(Atom + "subtitle", T("فرص مفتوحة لشباب الوطن العربي، تُحدَّث يوميًا", "Open opportunities for youth in the Arab world, updated daily", "Opportunités ouvertes aux jeunes du monde arabe, mises à jour chaque jour")),
                    new XElement(Atom + "link", new XAttribute("rel", "self"), new XAttribute("type", "application/atom+xml"), new XAttribute("href", selfUrl)),
                    new XElement(Atom + "link", new XAttribute("rel", "alternate"), new XAttribute("type", "text/html"), new XAttribute("href", htmlUrl)),
                    new XElement(Atom + "updated", updated.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture)),
                    new XElement(Atom + "author", new XElement(Atom + "name", siteName), new XElement(Atom + "uri", site + prefix + "/")),
                    new XElement(Atom + "icon", site + "/images/icon-192.png"));

                foreach (var o in items)
                {
                    var oTitle = lang == "en" ? o.TitleEn : lang == "fr" ? o.TitleFr : o.TitleAr;
                    var desc = Plain(lang == "en" ? o.DescriptionEn : lang == "fr" ? o.DescriptionFr : o.DescriptionAr);
                    if (desc.Length > 300) desc = desc[..297].TrimEnd() + "…";
                    var deadline = o.DeadlineType == DeadlineType.SpecificDate && o.EndDate.HasValue
                        ? T("الموعد النهائي: ", "Deadline: ", "Date limite : ") + o.EndDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : T("مفتوح حتى إشعار آخر", "Open until further notice", "Ouvert jusqu'à nouvel ordre");
                    var url = site + prefix + "/Opportunity/Details/" + o.Id + "/" + Uri.EscapeDataString(SlugHelper.Make(oTitle)) + "?utm_source=rss";
                    var item = new XElement(Atom + "entry",
                        new XElement(Atom + "id", $"tag:foraskhadra.com,2026:opportunity/{o.Id}"),
                        new XElement(Atom + "title", oTitle),
                        new XElement(Atom + "link", new XAttribute("rel", "alternate"), new XAttribute("type", "text/html"), new XAttribute("href", url)),
                        new XElement(Atom + "published", o.PublishDate.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture)),
                        new XElement(Atom + "updated", (o.UpdatedAt ?? o.PublishDate).ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture)),
                        new XElement(Atom + "summary", new XAttribute("type", "text"), deadline + " · " + desc));
                    if (o.Type.HasValue)
                        item.Add(new XElement(Atom + "category", new XAttribute("term", o.Type.Value.ToString()), new XAttribute("label", OpportunityTypeExtensions.GetDisplayName(o.Type.Value, lang))));
                    var org = !string.IsNullOrWhiteSpace(o.HostOrganizationName) ? o.HostOrganizationName : o.PublishedBy;
                    if (!string.IsNullOrWhiteSpace(org)) item.Add(new XElement(Atom + "author", new XElement(Atom + "name", org)));
                    if (!string.IsNullOrEmpty(o.ImagePath))
                        item.Add(new XElement(Atom + "link", new XAttribute("rel", "enclosure"), new XAttribute("type", MimeFor(o.ImagePath)), new XAttribute("href", o.ImagePath.StartsWith("http") ? o.ImagePath : site + o.ImagePath)));
                    feed.Add(item);
                }

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), feed);
                var sb = new StringBuilder();
                using (var w = new StringWriter(sb)) doc.Save(w);
                return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
            }) ?? "";

            Response.Headers.CacheControl = "public,max-age=600";
            return Content(xml, "application/atom+xml; charset=utf-8", Encoding.UTF8);
        }

        private static string Plain(string? html) =>
            Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html ?? "", "<.*?>", " ")), @"\s+", " ").Trim();

        private static string MimeFor(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", ".svg" => "image/svg+xml", _ => "image/jpeg" };
        }
    }
}
