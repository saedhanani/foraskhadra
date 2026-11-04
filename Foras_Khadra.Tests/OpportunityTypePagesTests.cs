using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class OpportunityTypePagesTests
    {
        [Fact]
        public void EveryType_HasAPageWithTextsInThreeLanguages()
        {
            foreach (var type in Enum.GetValues<OpportunityType>())
            {
                var page = OpportunityTypePages.For(type);
                Assert.Equal(type, page.Type);
                Assert.Matches("^[a-z]+$", page.Slug);
                foreach (var lang in new[] { "ar", "en", "fr" })
                {
                    Assert.False(string.IsNullOrWhiteSpace(page.Title(lang)));
                    Assert.True(page.Intro(lang).Length > 150, $"{page.Slug}/{lang} intro too short");
                    Assert.InRange(page.Meta(lang).Length, 40, 170);
                }
            }
            Assert.Equal(Enum.GetValues<OpportunityType>().Length, OpportunityTypePages.All.Select(p => p.Slug).Distinct().Count());
        }

        [Theory]
        [InlineData("jobs", OpportunityType.Jobs)]
        [InlineData("JOBS", OpportunityType.Jobs)]
        [InlineData("internships", OpportunityType.Internships)]
        [InlineData("trainings", OpportunityType.Internships)]   // الاسم القديم
        [InlineData("Scholarships", OpportunityType.Scholarships)]
        public void BySlug_IsForgiving(string slug, OpportunityType expected) =>
            Assert.Equal(expected, OpportunityTypePages.BySlug(slug)!.Type);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("grants")]
        [InlineData("jobs/1")]
        public void BySlug_Unknown_IsNull(string? slug) => Assert.Null(OpportunityTypePages.BySlug(slug));
    }
}
