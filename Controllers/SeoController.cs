using System.Text;
using System.Xml.Linq;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// sitemap.xml و robots.txt لمحركات البحث. الصفحات الإدارية وصفحات الدخول مستثناة من الفهرسة.
    /// </summary>
    public class SeoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        [Route("sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> Sitemap()
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";
            var urls = new List<XElement>();

            // كل صفحة بثلاث لغات: <url> لكل لغة، وفي كل واحدة xhtml:link لكل البدائل + x-default (العربية) كما يطلب Google
            void Add(string path, DateTime? lastModified, string changeFrequency, string priority)
            {
                foreach (var culture in CultureRouting.Supported)
                {
                    var url = new XElement(ns + "url", new XElement(ns + "loc", BaseUrl + CultureRouting.WithCulture(path, culture)));
                    if (lastModified.HasValue)
                        url.Add(new XElement(ns + "lastmod", lastModified.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
                    url.Add(new XElement(ns + "changefreq", changeFrequency), new XElement(ns + "priority", priority));
                    foreach (var alt in CultureRouting.Supported)
                        url.Add(Alternate(alt, BaseUrl + CultureRouting.WithCulture(path, alt)));
                    url.Add(Alternate("x-default", BaseUrl + CultureRouting.WithCulture(path, CultureRouting.Default)));
                    urls.Add(url);
                }
            }

            XElement Alternate(string hreflang, string href) =>
                new(xhtml + "link", new XAttribute("rel", "alternate"), new XAttribute("hreflang", hreflang), new XAttribute("href", href));

            // الصفحات الثابتة
            Add("/", null, "daily", "1.0");
            Add("/Home/AllOpportunities", null, "daily", "0.9");
            foreach (var typePage in new[] { "Jobs", "Trainings", "Scholarships", "Fellowships", "Volunteering", "Conferences", "Competitions" })
                Add($"/Home/{typePage}", null, "daily", "0.8");
            Add("/Home/Articles", null, "weekly", "0.7");
            Add("/Home/News", null, "weekly", "0.7");
            Add("/Home/OrganizationsMap", null, "weekly", "0.6");
            Add("/for-organizations", null, "monthly", "0.7");
            Add("/compass", null, "weekly", "0.9");
            Add("/Home/AboutUs", null, "monthly", "0.4");
            Add("/contact/Index", null, "monthly", "0.4");
            foreach (var policyPage in new[] { "PolicyPrivacy", "UsagePolicy", "TermsAndConditions" })
                Add($"/Home/{policyPage}", null, "yearly", "0.2");

            // الفرص المنشورة (المسودات القادمة من منصة الفريق لا تُفهرس)
            var opportunities = await _context.Opportunities
                .AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published)
                .OrderByDescending(o => o.PublishDate)
                .Select(o => new { o.Id, o.TitleAr, o.PublishDate })
                .ToListAsync();

            foreach (var o in opportunities)
                Add(Url.Action("Details", "Opportunity", new { id = o.Id, slug = SlugHelper.Make(o.TitleAr) })!, o.PublishDate, "weekly", "0.8");

            var articles = await _context.Articles
                .AsNoTracking()
                .OrderByDescending(a => a.PublishDate)
                .Select(a => new { a.Id, a.TitleAr, a.PublishDate })
                .ToListAsync();

            foreach (var a in articles)
                Add(Url.Action("Details", "Articles", new { id = a.Id, slug = SlugHelper.Make(a.TitleAr) })!, a.PublishDate, "monthly", "0.6");

            var activities = await _context.Activities
                .AsNoTracking()
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.StartDate)
                .Select(a => new { a.Id, a.TitleAr, a.StartDate })
                .ToListAsync();

            foreach (var a in activities)
                Add(Url.Action("Details", "Activities", new { id = a.Id, slug = SlugHelper.Make(a.TitleAr) })!, a.StartDate, "monthly", "0.6");

            var programs = await _context.FlagshipPrograms
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderBy(p => p.SortOrder)
                .Select(p => p.Slug)
                .ToListAsync();

            foreach (var slug in programs)
                Add($"/Programs/{slug}", null, "monthly", "0.8");

            var document = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "urlset", new XAttribute(XNamespace.Xmlns + "xhtml", xhtml), urls));
            return Content(document.Declaration + document.ToString(), "application/xml", Encoding.UTF8);
        }

        [Route("robots.txt")]
        [ResponseCache(Duration = 86400)]
        public IActionResult Robots()
        {
            var privatePaths = new[]
            {
                "/Admin/", "/Account/", "/Organization/", "/Team/", "/User/", "/Language/", "/api/",
                "/Opportunity/Index", "/Opportunity/Create", "/Opportunity/Edit", "/Opportunity/OrgOpportunities",
                "/Articles/Index", "/Articles/Create", "/Articles/Edit",
                "/Activities/Index", "/Activities/Create", "/Activities/Edit",
                "/Programs/Index", "/Programs/Create", "/Programs/Edit",
                "/ImpactStats/",
                "/compass/me", "/compass/result", "/compass/register", "/compass/documents"
            };
            var lines = new List<string> { "User-agent: *" };
            // المسارات نفسها بالعربية (بلا بادئة) وبـ/en و/fr
            foreach (var culture in CultureRouting.Supported)
                foreach (var p in privatePaths)
                    lines.Add("Disallow: " + CultureRouting.Prefix(culture) + p);
            lines.Add("");
            lines.Add($"Sitemap: {BaseUrl}/sitemap.xml");
            lines.Add("");
            var text = string.Join("\n", lines);
            return Content(text, "text/plain", Encoding.UTF8);
        }
    }
}
