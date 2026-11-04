using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>قواعد «بوصلة» الصافية: الاستبعاد الصريح (الدولة، المرحلة، العمر، للنساء، الهدف، التمويل، عن بُعد)، النقص الجزئي (اللغة)، والأسباب.</summary>
    public class CompassMatcherTests
    {
        private static readonly DateTime Today = new(2026, 11, 4);
        private static readonly Country World = new() { Id = 1, IsoCode = "XW", NameAr = "العالم", NameEn = "World", NameFr = "Monde" };
        private static readonly Country ArabWorld = new() { Id = 2, IsoCode = "XA", NameAr = "الوطن العربي", NameEn = "Arab world", NameFr = "Monde arabe" };
        private static readonly Country Jordan = new() { Id = 4, IsoCode = "JO", NameAr = "الأردن", NameEn = "Jordan", NameFr = "Jordanie" };

        private static Opportunity Opp(Action<Opportunity>? setup = null)
        {
            var o = new Opportunity
            {
                Id = 1, TitleAr = "منحة", TitleEn = "Grant", TitleFr = "Bourse", DescriptionAr = "", DescriptionEn = "", DescriptionFr = "",
                DetailsAr = "", DetailsEn = "", DetailsFr = "", EligibilityCriteriaAr = "", EligibilityCriteriaEn = "", EligibilityCriteriaFr = "",
                BenefitsAr = "", BenefitsEn = "", BenefitsFr = "", ApplyLink = "https://x.example", ImagePath = "", PublishedBy = "Admin", CreatedByUserId = "u",
                Type = OpportunityType.Scholarships, Status = OpportunityStatus.Published,
                DeadlineType = DeadlineType.SpecificDate, EndDate = Today.AddDays(30), PublishDate = Today.AddDays(-2),
                AvailableCountries = new List<Country>(), Fields = new List<Field>()
            };
            setup?.Invoke(o);
            return o;
        }

        private static CompassAnswers Me(Action<CompassAnswers>? setup = null)
        {
            var a = new CompassAnswers { Residence = "JO", Nationality = "JO", Goals = new() { "Scholarships" } };
            setup?.Invoke(a);
            return a;
        }

        private static CompassMatch? Eval(CompassAnswers a, Opportunity o, bool includeClosed = false) =>
            CompassMatcher.Evaluate(a.Normalized(), o, Today, "ar", includeClosed);

        [Fact]
        public void Goal_FiltersOtherTypes_AndExplains()
        {
            Assert.Null(Eval(Me(), Opp(o => o.Type = OpportunityType.Jobs)));
            var m = Eval(Me(), Opp());
            Assert.NotNull(m);
            Assert.Contains(m!.Reasons, r => r.StartsWith("هدفك"));
            Assert.NotNull(Eval(Me(a => a.Goals.Clear()), Opp(o => o.Type = OpportunityType.Jobs))); // بلا هدف = كل الأنواع
        }

        [Fact]
        public void Country_ArabWorld_SpecificCodes_World_AndUnknown()
        {
            Assert.NotNull(Eval(Me(), Opp(o => o.AvailableCountries.Add(ArabWorld))));
            Assert.Null(Eval(Me(a => { a.Residence = "ZZ"; a.Nationality = "ZZ"; }), Opp(o => o.AvailableCountries.Add(ArabWorld))));
            Assert.NotNull(Eval(Me(a => { a.Residence = "DE"; a.Nationality = "EG"; }), Opp(o => o.AvailableCountries.Add(ArabWorld)))); // الجنسية عربية

            Assert.NotNull(Eval(Me(), Opp(o => o.AvailableCountries.Add(Jordan))));
            Assert.Null(Eval(Me(a => { a.Residence = "EG"; a.Nationality = "EG"; }), Opp(o => o.AvailableCountries.Add(Jordan))));

            Assert.NotNull(Eval(Me(), Opp(o => o.AvailableCountries.Add(World))));
            Assert.NotNull(Eval(Me(a => { a.Residence = "ZZ"; a.Nationality = "ZZ"; }), Opp(o => o.AvailableCountries.Add(World))));

            Assert.NotNull(Eval(Me(a => { a.Residence = "DE"; a.Nationality = "DE"; }), Opp(o => o.EligibleCountriesIso = "DE,FR")));
            Assert.Null(Eval(Me(), Opp(o => o.EligibleCountriesIso = "DE,FR")));

            Assert.NotNull(Eval(Me(), Opp()));                                              // لا دول على الفرصة = لا نعرف
            Assert.NotNull(Eval(Me(a => { a.Residence = null; a.Nationality = null; }), Opp(o => o.AvailableCountries.Add(Jordan)))); // لا نعرف بلده
        }

        [Fact]
        public void Degree_Age_Women_AreHardRules()
        {
            Assert.Null(Eval(Me(a => a.Degree = "Bachelor"), Opp(o => o.DegreeLevels = DegreeLevels.Master | DegreeLevels.PhD)));
            Assert.NotNull(Eval(Me(a => a.Degree = "Master"), Opp(o => o.DegreeLevels = DegreeLevels.Master | DegreeLevels.PhD)));
            Assert.NotNull(Eval(Me(), Opp(o => o.DegreeLevels = DegreeLevels.Master)));            // لم يذكر مرحلته
            Assert.NotNull(Eval(Me(a => a.Degree = "Bachelor"), Opp(o => o.DegreeLevels = DegreeLevels.Any)));

            Assert.Null(Eval(Me(a => a.BirthYear = 1990), Opp(o => o.MaxAge = 30)));
            Assert.NotNull(Eval(Me(a => a.BirthYear = 2000), Opp(o => o.MaxAge = 30)));
            Assert.Null(Eval(Me(a => a.BirthYear = 2008), Opp(o => o.MinAge = 21)));

            Assert.Null(Eval(Me(a => a.Gender = "m"), Opp(o => o.ForWomenOnly = true)));
            var f = Eval(Me(a => a.Gender = "f"), Opp(o => o.ForWomenOnly = true));
            Assert.NotNull(f);
            Assert.Contains(f!.Reasons, r => r.Contains("للنساء"));
            Assert.NotNull(Eval(Me(), Opp(o => o.ForWomenOnly = true)));                             // لم يذكر
        }

        [Fact]
        public void Funding_And_Remote_Preferences_Filter()
        {
            Assert.Null(Eval(Me(a => a.FullyFundedOnly = true), Opp(o => o.FundingType = FundingType.PartiallyFunded)));
            Assert.NotNull(Eval(Me(a => a.FullyFundedOnly = true), Opp(o => o.FundingType = FundingType.Unknown)));
            var full = Eval(Me(a => a.FullyFundedOnly = true), Opp(o => o.FundingType = FundingType.FullyFunded));
            Assert.Contains(full!.Reasons, r => r.Contains("ممولة"));

            Assert.Null(Eval(Me(a => a.RemoteOnly = true), Opp()));
            Assert.NotNull(Eval(Me(a => a.RemoteOnly = true), Opp(o => o.IsOnline = true)));
        }

        [Fact]
        public void Language_IsASoftGap()
        {
            var noCert = Eval(Me(), Opp(o => { o.LanguageRequirement = LanguageRequirement.IELTS; o.LanguageScore = "6.5"; }));
            Assert.NotNull(noCert);
            Assert.False(noCert!.Eligible);
            Assert.Contains("IELTS", noCert.Gap);
            Assert.Contains("6.5", noCert.Gap);

            var withCert = Eval(Me(a => a.English.Add("IELTS")), Opp(o => o.LanguageRequirement = LanguageRequirement.IELTS));
            Assert.True(withCert!.Eligible);
            Assert.Contains(withCert.Reasons, r => r.Contains("IELTS"));

            Assert.True(Eval(Me(a => a.English.Add("toefl")), Opp(o => o.LanguageRequirement = LanguageRequirement.Either))!.Eligible);
            Assert.False(Eval(Me(), Opp(o => o.LanguageRequirement = LanguageRequirement.French))!.Eligible);
            Assert.True(Eval(Me(a => a.French = true), Opp(o => o.LanguageRequirement = LanguageRequirement.French))!.Eligible);
            Assert.Contains(Eval(Me(), Opp(o => o.LanguageRequirement = LanguageRequirement.None))!.Reasons, r => r.Contains("بلا شهادة"));
        }

        [Fact]
        public void Expired_OnlyWithIncludeClosed()
        {
            var old = Opp(o => o.EndDate = Today.AddDays(-5));
            Assert.Null(Eval(Me(), old));
            Assert.NotNull(Eval(Me(), old, includeClosed: true));
            Assert.NotNull(Eval(Me(), Opp(o => { o.DeadlineType = DeadlineType.UntilFull; o.EndDate = null; })));
        }

        [Fact]
        public void Reasons_AreAtMostThree_AndBetterMatchScoresHigher()
        {
            var field = new Field { Id = 1, Key = "energy", NameAr = "الطاقة", NameEn = "Energy", NameFr = "Énergie" };
            var strong = Opp(o =>
            {
                o.AvailableCountries.Add(Jordan); o.DegreeLevels = DegreeLevels.Master; o.FundingType = FundingType.FullyFunded;
                o.Fields.Add(field); o.LanguageRequirement = LanguageRequirement.None; o.HostCountryIso = "DE";
            });
            var weak = Opp(o => o.Id = 2);
            var me = Me(a => { a.Degree = "Master"; a.Fields.Add("energy"); a.HostCountries.Add("DE"); });

            var s = Eval(me, strong)!;
            var w = Eval(me, weak)!;
            Assert.Equal(3, s.Reasons.Count);
            Assert.True(s.Score > w.Score);
            Assert.Contains(s.Reasons, r => r.Contains("الطاقة"));
        }
    }
}
