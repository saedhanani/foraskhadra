using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class SearchIndexerTests
    {
        [Theory]
        [InlineData("منحة ماجستير", "منحه ماجستير")]                 // ة → ه
        [InlineData("أحمد إبراهيم آدم", "احمد ابراهيم ادم")]        // الهمزات → ا
        [InlineData("مستشفى", "مستشفي")]                           // ى → ي
        [InlineData("مُحَمَّد", "محمد")]                            // التشكيل يُحذف
        [InlineData("فـــرص", "فرص")]                               // التطويل يُحذف
        [InlineData("<p>Hello&nbsp;<b>World</b>!</p>", "hello world")]
        [InlineData("  Stage,  à   Paris (2026) ", "stage à paris 2026")]
        public void Normalize_UnifiesArabicLettersAndStripsNoise(string input, string expected)
        {
            Assert.Equal(expected, SearchIndexer.Normalize(input));
        }

        [Fact]
        public void Normalize_EmptyInputGivesEmpty()
        {
            Assert.Equal("", SearchIndexer.Normalize(null));
            Assert.Equal("", SearchIndexer.Normalize("   "));
        }

        [Fact]
        public void Terms_AreNormalizedDistinctAndLongerThanOneChar()
        {
            var terms = SearchIndexer.Terms("منحة  المنحة و منحة a 2026");

            Assert.Equal(new[] { "منحه", "المنحه", "2026" }, terms);
        }

        [Fact]
        public void Terms_LimitedToTen()
        {
            var query = string.Join(" ", Enumerable.Range(10, 20).Select(i => i.ToString()));

            Assert.Equal(10, SearchIndexer.Terms(query).Count);
        }

        [Fact]
        public void Build_CoversAllLanguagesAndSections()
        {
            var o = new Opportunity
            {
                TitleAr = "منحة", TitleEn = "Scholarship", TitleFr = "Bourse",
                DescriptionAr = "<p>وصف</p>", DescriptionEn = "desc", DescriptionFr = "descr",
                DetailsAr = "تفاصيل", DetailsEn = "details", DetailsFr = "détails",
                EligibilityCriteriaAr = "شروط", EligibilityCriteriaEn = "criteria", EligibilityCriteriaFr = "critères",
                BenefitsAr = "مزايا", BenefitsEn = "benefits", BenefitsFr = "avantages",
                PublishedBy = "UNESCO"
            };

            var text = SearchIndexer.Build(o);

            foreach (var expected in new[] { "منحه", "scholarship", "bourse", "وصف", "détails", "critères", "avantages", "unesco" })
                Assert.Contains(expected, text);
            Assert.DoesNotContain("<p>", text);
        }
    }
}
