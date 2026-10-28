using Foras_Khadra.Helpers;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class SlugHelperTests
    {
        [Theory]
        [InlineData("Master Scholarship 2026", "master-scholarship-2026")]
        [InlineData("  Hello,   World!  ", "hello-world")]
        [InlineData("منحة ماجستير في ألمانيا", "منحة-ماجستير-في-ألمانيا")]
        [InlineData("Stage à Paris (été)", "stage-à-paris-été")]
        public void Make_KeepsLettersAndDigits_CollapsesTheRestToSingleDashes(string input, string expected)
        {
            Assert.Equal(expected, SlugHelper.Make(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("!!! ---")]
        public void Make_ReturnsEmptyWhenNothingUsable(string? input)
        {
            Assert.Equal(string.Empty, SlugHelper.Make(input));
        }

        [Fact]
        public void Make_TruncatesLongTitlesWithoutTrailingDash()
        {
            var input = string.Join(" ", Enumerable.Repeat("word", 40));

            var slug = SlugHelper.Make(input, maxLength: 20);

            Assert.True(slug.Length <= 20);
            Assert.False(slug.EndsWith('-'));
            Assert.Equal("word-word-word-word", slug);
        }
    }
}
