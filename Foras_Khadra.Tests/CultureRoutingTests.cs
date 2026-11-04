using Foras_Khadra.Helpers;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class CultureRoutingTests
    {
        [Theory]
        [InlineData("/en", "en")]
        [InlineData("/en/", "en")]
        [InlineData("/fr/Home/AboutUs", "fr")]
        [InlineData("/EN/Home", "en")]
        [InlineData("/", null)]
        [InlineData("/Home/AboutUs", null)]
        [InlineData("/english", null)]      // مقطع يبدأ بـen لكنه ليس لغة
        [InlineData("/ar/Home", null)]      // العربية لا تُكتب كبادئة
        [InlineData("", null)]
        public void FromPath_ReadsOnlyWholeSegment(string path, string? expected) =>
            Assert.Equal(expected, CultureRouting.FromPath(path));

        [Theory]
        [InlineData("/en/Home/AboutUs", "/Home/AboutUs")]
        [InlineData("/en", "/")]
        [InlineData("/en/", "/")]
        [InlineData("/Home/AboutUs", "/Home/AboutUs")]
        [InlineData("", "/")]
        public void StripPrefix(string path, string expected) =>
            Assert.Equal(expected, CultureRouting.StripPrefix(path));

        [Theory]
        [InlineData("/Home/AboutUs", "en", "/en/Home/AboutUs")]
        [InlineData("/en/Home/AboutUs", "fr", "/fr/Home/AboutUs")]
        [InlineData("/fr/Home/AboutUs?page=2", "ar", "/Home/AboutUs?page=2")]
        [InlineData("/", "en", "/en")]
        [InlineData("/en", "ar", "/")]
        [InlineData("/?source=pwa", "fr", "/fr?source=pwa")]
        [InlineData("/Home/AboutUs", "xx", "/Home/AboutUs")]   // لغة غير معروفة → العربية
        public void WithCulture_SwapsPrefixAndKeepsQuery(string pathAndQuery, string culture, string expected) =>
            Assert.Equal(expected, CultureRouting.WithCulture(pathAndQuery, culture));

        [Theory]
        [InlineData("ar", "")]
        [InlineData("en", "/en")]
        [InlineData("FR", "/fr")]
        [InlineData(null, "")]
        public void Prefix(string? culture, string expected) =>
            Assert.Equal(expected, CultureRouting.Prefix(culture));

        [Theory]
        [InlineData("en", true)]
        [InlineData("fr", true)]
        [InlineData("ar", false)]   // يفشل عمدًا حتى تخرج روابط العربية من المسار بلا بادئة
        [InlineData("Home", false)]
        public void Constraint_AcceptsOnlyPrefixedCultures(string value, bool expected)
        {
            var constraint = new CultureRouteConstraint();
            var values = new RouteValueDictionary { ["culture"] = value };
            Assert.Equal(expected, constraint.Match(null, null, "culture", values, RouteDirection.IncomingRequest));
        }

        [Theory]
        [InlineData("/", true)]
        [InlineData("/en/Home/AboutUs", true)]
        [InlineData("/Opportunity/Details/12/some-slug", true)]
        [InlineData("/api/v1/opportunities", false)]
        [InlineData("/go/12", false)]
        [InlineData("/sitemap.xml", false)]
        [InlineData("/css/site.css", false)]
        public void IsHtmlPage(string path, bool expected) =>
            Assert.Equal(expected, CultureRouting.IsHtmlPage(path));
    }
}
