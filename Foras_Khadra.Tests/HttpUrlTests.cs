using Foras_Khadra.Helpers;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class HttpUrlTests
    {
        [Theory]
        [InlineData("https://www.daad.de/go/en/stipa50076777", "https://www.daad.de/go/en/stipa50076777")]
        [InlineData("http://example.org/apply", "http://example.org/apply")]
        [InlineData("  https://example.org/apply  ", "https://example.org/apply")]
        [InlineData("www.daad.de/apply", "https://www.daad.de/apply")]
        [InlineData("example.org", "https://example.org")]
        [InlineData("HTTPS://EXAMPLE.ORG/x?y=1&z=2", "HTTPS://EXAMPLE.ORG/x?y=1&z=2")]
        [InlineData("https://منصة.مثال/فرصة", "https://منصة.مثال/فرصة")]
        public void Normalize_AcceptsHttpAndAddsMissingScheme(string input, string expected)
        {
            Assert.Equal(expected, HttpUrl.Normalize(input));
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("JavaScript:alert(document.cookie)")]
        [InlineData(" javascript:alert(1)")]
        [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
        [InlineData("vbscript:msgbox(1)")]
        [InlineData("ftp://example.org/file")]
        [InlineData("mailto:someone@example.org")]
        [InlineData("file:///C:/Windows")]
        [InlineData("//example.org/protocol-relative")]
        [InlineData("https://")]
        [InlineData("https://nodot")]
        [InlineData("just some words")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("https://example.org/\u0000")]
        public void Normalize_RejectsEverythingElse(string? input)
        {
            Assert.Null(HttpUrl.Normalize(input));
            Assert.False(HttpUrl.IsValid(input));
        }

        [Fact]
        public void Attribute_TreatsEmptyAsValid_AndRejectsScripts()
        {
            var attr = new HttpUrlAttribute();
            Assert.True(attr.IsValid(null));
            Assert.True(attr.IsValid(""));
            Assert.True(attr.IsValid("https://example.org"));
            Assert.True(attr.IsValid("example.org"));
            Assert.False(attr.IsValid("javascript:alert(1)"));
            Assert.False(attr.IsValid(12345));
        }
    }
}
