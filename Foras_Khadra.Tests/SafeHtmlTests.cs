using Foras_Khadra.Helpers;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class SafeHtmlTests
    {
        [Fact]
        public void Clean_RemovesScriptsEventHandlersAndJavascriptLinks()
        {
            var dirty = "<p>hi</p><script>alert(1)</script>" +
                        "<a href=\"javascript:alert(1)\" onclick=\"x()\">bad</a>" +
                        "<img src=x onerror=\"alert(1)\">" +
                        "<iframe src=\"https://evil\"></iframe>";

            var clean = SafeHtml.Clean(dirty);

            Assert.DoesNotContain("<script", clean, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onclick", clean, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onerror", clean, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", clean, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<iframe", clean, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<p>hi</p>", clean);
        }

        [Fact]
        public void Clean_KeepsQuillFormattingAndSafeLinks()
        {
            var html = "<p class=\"ql-align-center\"><strong>b</strong> <em>i</em></p>" +
                       "<ul><li>x</li></ul><a href=\"https://example.com\" target=\"_blank\">l</a>";

            var clean = SafeHtml.Clean(html);

            Assert.Contains("ql-align-center", clean);
            Assert.Contains("<strong>b</strong>", clean);
            Assert.Contains("<li>x</li>", clean);
            Assert.Contains("https://example.com", clean);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Clean_EmptyInputReturnsEmpty(string? input)
        {
            Assert.Equal("", SafeHtml.Clean(input));
        }
    }
}
