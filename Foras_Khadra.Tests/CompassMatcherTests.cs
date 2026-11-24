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

        /// <summary>فرصة كل شروطها الأساسية معروفة: مفتوحة للعالم، كل المراحل، بلا شهادة لغة.</summary>
        private static Opportunity Known(Action<Opportunity>? setup = null) => Opp(o =>
        {
            o.AvailableCountries.Add(World);
            o.DegreeLevels = DegreeLevels.Any;
            o.LanguageRequirement = LanguageRequirement.None;
            setup?.Invoke(o);
        });

        [Fact]
        public void Confirmed_OnlyWhenEveryKnownRequirementIsChecked()
        {
            var confirmed = Eval(Me(), Known())!;
            Assert.True(confirmed.Confirmed);
            Assert.Empty(confirmed.Verify);

            // فرصة بلا بيانات أهلية: تناسب، لكنها ليست مؤكَّدة — ثلاثة شروط مجهولة (الدول، المرحلة، اللغة)
            var legacy = Eval(Me(), Opp())!;
            Assert.True(legacy.Eligible);
            Assert.False(legacy.Confirmed);
            Assert.Equal(3, legacy.Verify.Count);

            // شرط معروف في الفرصة والطالب لم يجب عنه
            Assert.False(Eval(Me(), Known(o => o.DegreeLevels = DegreeLevels.Master))!.Confirmed);
            Assert.True(Eval(Me(a => a.Degree = "Master"), Known(o => o.DegreeLevels = DegreeLevels.Master))!.Confirmed);
            Assert.False(Eval(Me(), Known(o => o.MaxAge = 30))!.Confirmed);
            Assert.True(Eval(Me(a => a.BirthYear = 2000), Known(o => o.MaxAge = 30))!.Confirmed);
            Assert.False(Eval(Me(), Known(o => o.ForWomenOnly = true))!.Confirmed);
            Assert.True(Eval(Me(a => a.Gender = "f"), Known(o => o.ForWomenOnly = true))!.Confirmed);
            Assert.False(Eval(Me(a => { a.Residence = null; a.Nationality = null; }), Known(o => { o.AvailableCountries.Clear(); o.AvailableCountries.Add(Jordan); }))!.Confirmed);

            // لغة أخرى أو تمويل مجهول مع «الممولة بالكامل فقط»
            Assert.False(Eval(Me(), Known(o => o.LanguageRequirement = LanguageRequirement.Other))!.Confirmed);
            Assert.False(Eval(Me(a => a.FullyFundedOnly = true), Known())!.Confirmed);
            Assert.True(Eval(Me(a => a.FullyFundedOnly = true), Known(o => o.FundingType = FundingType.FullyFunded))!.Confirmed);

            // النقص المعروف ليس "مؤكَّدة" ولا "تحتاج مراجعة"
            var gap = Eval(Me(), Known(o => o.LanguageRequirement = LanguageRequirement.IELTS))!;
            Assert.False(gap.Eligible);
            Assert.False(gap.Confirmed);
        }

        [Fact]
        public void LanguageScore_IsComparedWithTheRequiredOne()
        {
            Opportunity Ielts65() => Known(o => { o.LanguageRequirement = LanguageRequirement.IELTS; o.LanguageScore = "6.5"; });

            var low = Eval(Me(a => { a.English.Add("ielts"); a.EnglishScore = "6"; }), Ielts65())!;
            Assert.False(low.Eligible);
            Assert.Contains("6.5", low.Gap);

            Assert.True(Eval(Me(a => { a.English.Add("ielts"); a.EnglishScore = "7"; }), Ielts65())!.Confirmed);
            Assert.True(Eval(Me(a => { a.English.Add("ielts"); a.EnglishScore = "6,5"; }), Ielts65())!.Confirmed);

            // يملك الشهادة ولم يذكر درجته: لا نقص، لكنها تحتاج مراجعة
            var noScore = Eval(Me(a => a.English.Add("ielts")), Ielts65())!;
            Assert.True(noScore.Eligible);
            Assert.False(noScore.Confirmed);
            Assert.Contains(noScore.Verify, v => v.Contains("6.5"));

            // «أي منهما»: كل شهادة تُقارن بدرجتها في النص
            Opportunity Either() => Known(o => { o.LanguageRequirement = LanguageRequirement.Either; o.LanguageScore = "IELTS 6.5 / TOEFL 90"; });
            Assert.True(Eval(Me(a => { a.English.Add("toefl"); a.EnglishScore = "95"; }), Either())!.Confirmed);
            Assert.False(Eval(Me(a => { a.English.Add("toefl"); a.EnglishScore = "80"; }), Either())!.Eligible);

            // لا درجة مطلوبة في الفرصة: الشهادة تكفي
            Assert.True(Eval(Me(a => a.English.Add("ielts")), Known(o => o.LanguageRequirement = LanguageRequirement.IELTS))!.Confirmed);

            Assert.Equal(6.5m, CompassMatcher.ParseScore("IELTS 6.5 / TOEFL 90", ielts: true));
            Assert.Equal(90m, CompassMatcher.ParseScore("IELTS 6.5 / TOEFL 90", ielts: false));
            Assert.Null(CompassMatcher.ParseScore("B2", ielts: true));
        }

        [Fact]
        public void NoDeadline_OldOnesAreHidden_AndAgingOnesNeedChecking()
        {
            Opportunity Rolling(int publishedDaysAgo, string titleEn = "Grant") => Known(o =>
            {
                o.DeadlineType = DeadlineType.NotSpecified; o.EndDate = null; o.PublishDate = Today.AddDays(-publishedDaysAgo); o.TitleEn = titleEn;
            });

            Assert.True(Eval(Me(), Rolling(10))!.Confirmed);

            var aging = Eval(Me(), Rolling(CompassMatcher.VerifyOpenAfterDays + 5))!;
            Assert.True(aging.Eligible);
            Assert.False(aging.Confirmed);

            Assert.Null(Eval(Me(), Rolling(CompassMatcher.StaleAfterDays + 1)));
            Assert.Null(Eval(Me(), Rolling(10, "Climate Scholarship 2025")));          // سنة العنوان مضت
            Assert.NotNull(Eval(Me(), Rolling(10, "Climate Scholarship 2026-2027")));  // أحدث سنة في العنوان لم تمضِ

            // الموعد المحدد يُحترم مهما قَدُم النشر
            Assert.True(Eval(Me(), Known(o => { o.PublishDate = Today.AddDays(-400); o.TitleEn = "Grant 2025"; }))!.Confirmed);
        }

        [Fact]
        public void GraduationFarAhead_NeedsCheckingForScholarships()
        {
            Opportunity Masters(OpportunityType type) => Known(o => { o.Type = type; o.DegreeLevels = DegreeLevels.Bachelor; });

            var student = Eval(Me(a => { a.Degree = "Bachelor"; a.GraduationYear = Today.Year + 3; }), Masters(OpportunityType.Scholarships))!;
            Assert.True(student.Eligible);
            Assert.False(student.Confirmed);

            Assert.True(Eval(Me(a => { a.Degree = "Bachelor"; a.GraduationYear = Today.Year + 1; }), Masters(OpportunityType.Scholarships))!.Confirmed);
            Assert.True(Eval(Me(a => { a.Degree = "Bachelor"; a.GraduationYear = Today.Year + 3; a.Goals = new() { "Internships" }; }), Masters(OpportunityType.Internships))!.Confirmed);
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
