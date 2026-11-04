using Foras_Khadra.Helpers;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class AnalyticsRulesTests
    {
        [Theory]
        [InlineData("/", null, "/", null)]
        [InlineData("/en", null, "/", null)]
        [InlineData("/fr/Home/AboutUs/", null, "/home/aboutus", null)]
        [InlineData("/Opportunity/Details/123/منحة-ماجستير", null, "/opportunity/details/123", 123)]
        [InlineData("/en/Opportunity/Details/5", null, "/opportunity/details/5", 5)]
        [InlineData("/Articles/Details/7/slug-here", null, "/articles/details/7", null)]
        [InlineData("/Programs/rcoy-mena", null, "/programs/rcoy-mena", null)]
        [InlineData("/Home/AllOpportunities", "Jobs", "/home/allopportunities?type=jobs", null)]
        [InlineData("/Home/AllOpportunities", "<script>", "/home/allopportunities", null)]
        public void Normalize_StripsCultureSlugAndQuery(string path, string? type, string expectedPath, int? expectedId)
        {
            var query = type == null ? null : new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["type"] = type });
            var (p, id) = AnalyticsRules.Normalize(path, query);
            Assert.Equal(expectedPath, p);
            Assert.Equal(expectedId, id);
        }

        [Theory]
        [InlineData("/", true)]
        [InlineData("/Home/AllOpportunities", true)]
        [InlineData("/Opportunity/Details/12/x", true)]
        [InlineData("/for-organizations", true)]
        [InlineData("/Account/Login", true)]
        [InlineData("/Account/RegisterUser", true)]
        [InlineData("/Account/ProfileSettings", false)]
        [InlineData("/Admin/Review", false)]
        [InlineData("/api/v1/opportunities", false)]
        [InlineData("/go/12", false)]
        [InlineData("/Opportunity/Edit/12", false)]
        [InlineData("/Opportunity/Index", false)]
        [InlineData("/css/site.css", false)]
        [InlineData("/sitemap.xml", false)]
        public void ShouldTrack_PublicHtmlPagesOnly(string path, bool expected) =>
            Assert.Equal(expected, AnalyticsRules.ShouldTrack(path));

        [Theory]
        [InlineData("Mozilla/5.0 (Linux; Android 13) Chrome/120 Mobile Safari/537.36", false)]
        [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", true)]
        [InlineData("Mozilla/5.0 (compatible; bingbot/2.0)", true)]
        [InlineData("facebookexternalhit/1.1", true)]
        [InlineData("curl/8.4.0", true)]
        [InlineData("", true)]
        [InlineData(null, true)]
        public void IsBot(string? ua, bool expected) => Assert.Equal(expected, AnalyticsRules.IsBot(ua));

        private static HttpRequest Request(string host, string? referer = null, string? query = null)
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Host = new HostString(host);
            if (referer != null) ctx.Request.Headers.Referer = referer;
            if (query != null) ctx.Request.QueryString = new QueryString(query);
            return ctx.Request;
        }

        [Fact]
        public void SourceOf_ClassifiesEntries()
        {
            Assert.Equal("direct", AnalyticsRules.SourceOf(Request("www.foraskhadra.com")));
            Assert.Equal("internal", AnalyticsRules.SourceOf(Request("www.foraskhadra.com", "https://foraskhadra.com/Home/AllOpportunities")));
            Assert.Equal("google.com", AnalyticsRules.SourceOf(Request("www.foraskhadra.com", "https://www.google.com/")));
            Assert.Equal("t.co", AnalyticsRules.SourceOf(Request("www.foraskhadra.com", "https://t.co/abc")));
            Assert.Equal("utm:newsletter", AnalyticsRules.SourceOf(Request("www.foraskhadra.com", "https://www.google.com/", "?utm_source=Newsletter")));
            Assert.Equal("pwa", AnalyticsRules.SourceOf(Request("www.foraskhadra.com", null, "?source=pwa")));
            Assert.Equal("direct", AnalyticsRules.SourceOf(Request("www.foraskhadra.com", "not a url")));
        }

        [Fact]
        public void CleanLabel_NormalizesSearchTerms() =>
            Assert.Equal("منح   ماجستير".Replace("   ", " "), AnalyticsRules.CleanLabel("  مِنح   ماجستير ".Replace("مِنح", "منح")));
    }
}
