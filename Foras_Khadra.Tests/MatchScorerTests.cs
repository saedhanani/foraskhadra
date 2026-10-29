using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>منطق المرحلة 3 الصافي: ربط الجنسية بدول الفرصة ونسبة التطابق.</summary>
    public class MatchScorerTests
    {
        private static readonly List<Country> Countries = new()
        {
            new Country { Id = 1, NameAr = "كل دول العالم", NameEn = "All World", NameFr = "Tous les pays du monde" },
            new Country { Id = 2, NameAr = "الوطن العربي ", NameEn = "The Arab World", NameFr = "Le Monde arabe" },
            new Country { Id = 3, NameAr = "فلسطين", NameEn = "Palestine", NameFr = "Palestine" },
            new Country { Id = 4, NameAr = "الأردن", NameEn = "Jordan", NameFr = "Jordanie" },
            new Country { Id = 8, NameAr = "السعودية", NameEn = "Saudi Arabia", NameFr = "Arabie saoudite" },
            new Country { Id = 13, NameAr = "عُمان", NameEn = "Oman", NameFr = "Oman" },
            new Country { Id = 15, NameAr = "مصر", NameEn = "Egypt", NameFr = "Égypte" },
            new Country { Id = 20, NameAr = "المغرب", NameEn = "Morocco", NameFr = "Maroc" },
            new Country { Id = 22, NameAr = "جيبوتي", NameEn = "Djibouti", NameFr = "Djibouti" }
        };

        private static Country C(int id) => Countries.Single(c => c.Id == id);

        private static Opportunity Opp(OpportunityType? type, params int[] countryIds) => new()
        {
            Id = 1, Type = type,
            AvailableCountries = countryIds.Select(C).ToList(),
            DeadlineType = DeadlineType.NotSpecified
        };

        // ===== ResolveCountry: أسماء التسجيل (RegionInfo) → دول الجدول =====

        [Theory]
        [InlineData("الأردن", 4)]
        [InlineData("Jordan", 4)]
        [InlineData("مصر", 15)]
        [InlineData("المملكة العربية السعودية", 8)]   // NativeName يحوي اسم الجدول
        [InlineData("Saudi Arabia", 8)]
        [InlineData("السلطة الفلسطينية", 3)]            // احتواء "فلسطين"
        [InlineData("Palestinian Authority", 3)]        // عبر الأسماء البديلة
        [InlineData("Palestinian Territories", 3)]
        [InlineData("عمان", 13)]                        // بلا ضمة
        [InlineData("المملكة المغربية", 20)]
        [InlineData("Yabuuti", 22)]                     // الاسم المحلي لجيبوتي
        public void ResolveCountry_MapsRegistrationNames(string name, int expectedId)
        {
            Assert.Equal(expectedId, MatchScorer.ResolveCountry(name, Countries)?.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Germany")]
        [InlineData("ألمانيا")]
        [InlineData("Romania")]   // تحوي "oman" حرفيًا — لا يجوز أن تُحسب عُمان
        public void ResolveCountry_UnknownOrNonArab_ReturnsNull(string? name)
        {
            Assert.Null(MatchScorer.ResolveCountry(name, Countries));
        }

        [Fact]
        public void ResolveCountry_NeverReturnsTheGroupRows()
        {
            Assert.Null(MatchScorer.ResolveCountry("كل دول العالم", Countries));
            Assert.Null(MatchScorer.ResolveCountry("The Arab World", Countries));
        }

        // ===== Eligible =====

        [Fact]
        public void Eligible_AllWorld_TrueEvenWithoutLocation()
        {
            var p = new MatchProfile(Array.Empty<OpportunityType>(), null, null);
            Assert.True(MatchScorer.Eligible(p, Opp(null, 1)));
        }

        [Fact]
        public void Eligible_UnknownWhenNoCountriesOrNoLocation()
        {
            var noLocation = new MatchProfile(Array.Empty<OpportunityType>(), null, null);
            Assert.Null(MatchScorer.Eligible(noLocation, Opp(null, 4)));
            Assert.Null(MatchScorer.Eligible(noLocation, Opp(null)));

            var jordanian = new MatchProfile(Array.Empty<OpportunityType>(), 4, null);
            Assert.Null(MatchScorer.Eligible(jordanian, Opp(null)));
        }

        [Fact]
        public void Eligible_ArabWorld_TrueForAnyResolvedArabUser()
        {
            var egyptian = new MatchProfile(Array.Empty<OpportunityType>(), 15, null);
            Assert.True(MatchScorer.Eligible(egyptian, Opp(null, 2)));
        }

        [Fact]
        public void Eligible_MatchesNationalityOrResidence_ElseFalse()
        {
            var p = new MatchProfile(Array.Empty<OpportunityType>(), 3, 4); // فلسطيني مقيم في الأردن
            Assert.True(MatchScorer.Eligible(p, Opp(null, 3)));
            Assert.True(MatchScorer.Eligible(p, Opp(null, 4)));
            Assert.False(MatchScorer.Eligible(p, Opp(null, 15, 8)));
        }

        [Fact]
        public void MatchService_Scores_DedupesByIdNotReference()
        {
            var service = new Foras_Khadra.Services.MatchService(null!, new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
            var p = new MatchProfile(Array.Empty<OpportunityType>(), null, null);
            var a = Opp(OpportunityType.Jobs, 4); var b = Opp(OpportunityType.Jobs, 4); // نفس المعرّف (1) بمرجعين
            var scores = service.Scores(p, new[] { a, b });
            Assert.Single(scores);
        }

        // ===== Score =====

        [Fact]
        public void Score_FullMatch_Is100()
        {
            var p = new MatchProfile(new[] { OpportunityType.Scholarships }, 4, null);
            Assert.Equal(100, MatchScorer.Score(p, Opp(OpportunityType.Scholarships, 4)));
        }

        [Fact]
        public void Score_OnlyCountsKnownCriteria()
        {
            // بلا اهتمامات وبلا موقع: يبقى معيار الانفتاح فقط (15/15 = 100)
            var blank = new MatchProfile(Array.Empty<OpportunityType>(), null, null);
            Assert.Equal(100, MatchScorer.Score(blank, Opp(OpportunityType.Jobs, 4)));

            // اهتمامات دون موقع، والنوع لا يطابق: 15 / (50+15)
            var interestsOnly = new MatchProfile(new[] { OpportunityType.Jobs }, null, null);
            Assert.Equal(23, MatchScorer.Score(interestsOnly, Opp(OpportunityType.Scholarships, 4)));
        }

        [Fact]
        public void Score_IneligibleCountry_LosesEligibilityWeight()
        {
            var p = new MatchProfile(new[] { OpportunityType.Jobs }, 4, null);
            // النوع يطابق (50) + مفتوحة (15) ولا تقبل جنسيته (0 من 35) = 65/100
            Assert.Equal(65, MatchScorer.Score(p, Opp(OpportunityType.Jobs, 15)));
        }

        [Fact]
        public void Score_DeadlineAffectsOpennessWeight()
        {
            var p = new MatchProfile(new[] { OpportunityType.Jobs }, 4, null);
            var today = new DateTime(2026, 10, 29);

            var soon = Opp(OpportunityType.Jobs, 4);
            soon.DeadlineType = DeadlineType.SpecificDate; soon.EndDate = today.AddDays(3);
            Assert.Equal(93, MatchScorer.Score(p, soon, today)); // 50+35+8

            var expired = Opp(OpportunityType.Jobs, 4);
            expired.DeadlineType = DeadlineType.SpecificDate; expired.EndDate = today.AddDays(-1);
            Assert.Equal(85, MatchScorer.Score(p, expired, today)); // 50+35+0
        }
    }
}
