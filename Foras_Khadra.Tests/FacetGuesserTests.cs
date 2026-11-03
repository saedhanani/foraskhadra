using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class FacetGuesserTests
    {
        private static Opportunity Opp(string titleAr, string titleEn = "", string eligibilityAr = "", OpportunityType? type = null) => new()
        {
            TitleAr = titleAr, TitleEn = titleEn, TitleFr = "",
            EligibilityCriteriaAr = eligibilityAr, EligibilityCriteriaEn = "", BenefitsAr = "", BenefitsEn = "",
            DescriptionAr = "", DescriptionEn = "", Type = type
        };

        [Fact]
        public void FullyFundedMasterWithIelts()
        {
            var s = FacetGuesser.Suggest(Opp("منحة DAAD للماجستير في ألمانيا ممولة بالكامل", "", "IELTS 6.0 أو TOEFL 80"));
            Assert.Equal(FundingType.FullyFunded, s.Funding);
            Assert.True(s.Degrees.HasFlag(DegreeLevels.Master));
            Assert.False(s.Degrees.HasFlag(DegreeLevels.PhD));
            Assert.Equal(LanguageRequirement.Either, s.Language);
            Assert.False(s.Remote);
        }

        [Fact]
        public void RemoteClimateJobIsPaid()
        {
            var s = FacetGuesser.Suggest(Opp("وظيفة عن بُعد: محلل سياسات المناخ", "Remote climate policy analyst (salary)", "", OpportunityType.Jobs));
            Assert.Equal(FundingType.Paid, s.Funding);
            Assert.True(s.Remote);
            Assert.True(s.Themes.HasFlag(GreenThemes.Climate));
            Assert.True(s.Themes.HasFlag(GreenThemes.Policy));
        }

        [Fact]
        public void NoIeltsAndWomenOnlyAndFee()
        {
            var s = FacetGuesser.Suggest(Opp("منحة للنساء بدون آيلتس", "", "رسوم تقديم 50 دولارًا"));
            Assert.Equal(LanguageRequirement.None, s.Language);
            Assert.True(s.WomenOnly);
            Assert.True(s.Fee);
        }

        [Fact]
        public void NothingRecognised_GivesNulls()
        {
            var s = FacetGuesser.Suggest(Opp("مؤتمر الشباب", "Youth conference"));
            Assert.Null(s.Funding);
            Assert.Equal(DegreeLevels.None, s.Degrees);
            Assert.Null(s.Language);
            Assert.Equal(GreenThemes.None, s.Themes);
            Assert.False(s.WomenOnly);
        }
    }
}
