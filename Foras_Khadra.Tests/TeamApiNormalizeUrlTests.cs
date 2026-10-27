using Foras_Khadra.Controllers.Api;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class TeamApiNormalizeUrlTests
    {
        [Theory]
        [InlineData("https://www.Example.com/apply/", "example.com/apply")]
        [InlineData("http://example.com/apply?utm_source=x&fbclid=y", "example.com/apply")]
        [InlineData("https://example.com/apply?b=2&a=1", "example.com/apply?a=1&b=2")]
        [InlineData("https://example.com/apply?a=1&utm_medium=m", "example.com/apply?a=1")]
        public void NormalizeUrl_StripsSchemeWwwTrackingAndOrdersQuery(string input, string expected)
        {
            Assert.Equal(expected, TeamApiController.NormalizeUrl(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void NormalizeUrl_EmptyReturnsEmpty(string? input)
        {
            Assert.Equal("", TeamApiController.NormalizeUrl(input));
        }

        [Fact]
        public void NormalizeUrl_SameOpportunityDifferentLinksMatch()
        {
            var a = TeamApiController.NormalizeUrl("https://www.site.org/jobs/123?utm_campaign=fb");
            var b = TeamApiController.NormalizeUrl("http://site.org/jobs/123/");
            Assert.Equal(a, b);
        }
    }
}
