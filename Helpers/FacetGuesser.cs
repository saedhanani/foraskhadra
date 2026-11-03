using System.Text.RegularExpressions;
using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// اقتراح الحقول المهيكلة للفرص القديمة من كلمات العنوان والنصوص (صفحة التعبئة السريعة Admin/Facets).
    /// اقتراح فقط — يؤكده المدير بنقرة؛ لا يُحفظ شيء تلقائيًا.
    /// </summary>
    public static class FacetGuesser
    {
        public sealed record Suggestion(FundingType? Funding, DegreeLevels Degrees, LanguageRequirement? Language,
            bool Remote, bool Online, bool WomenOnly, bool Fee, GreenThemes Themes);

        private static readonly (Regex Pattern, FundingType Value)[] Funding =
        {
            (Rx(@"ممو+لة? بالكامل|ممو+لة? كليا|fully[- ]funded|entièrement financ|full scholarship|منحة كاملة"), FundingType.FullyFunded),
            (Rx(@"ممو+لة? جزئيا|تمويل جزئي|partial(ly)?[- ]funded|partiellement financ"), FundingType.PartiallyFunded),
            (Rx(@"\bpaid\b|براتب|salary|salaire|rémunér|مدفوع"), FundingType.Paid),
        };

        private static readonly (Regex Pattern, DegreeLevels Value)[] Degrees =
        {
            (Rx(@"ثانوي|high[- ]school|lycée|secondaire"), DegreeLevels.HighSchool),
            (Rx(@"بكالوريوس|bachelor|licence|undergraduate|بكالوريس"), DegreeLevels.Bachelor),
            (Rx(@"ماجستير|ماستر|master|msc|mba"), DegreeLevels.Master),
            (Rx(@"دكتوراه|phd|ph\.d|doctora"), DegreeLevels.PhD),
            (Rx(@"ما بعد الدكتوراه|post-?doc"), DegreeLevels.Postdoc),
        };

        private static readonly (Regex Pattern, GreenThemes Value)[] Themes =
        {
            (Rx(@"مناخ|climat"), GreenThemes.Climate),
            (Rx(@"طاقة|energy|énergie|solar|شمسية|renewable|متجددة"), GreenThemes.Energy),
            (Rx(@"مياه|\bwater\b|\beau\b"), GreenThemes.Water),
            (Rx(@"تنوع حيوي|biodivers|wildlife"), GreenThemes.Biodiversity),
            (Rx(@"اقتصاد دائري|circular|recycl|تدوير"), GreenThemes.CircularEconomy),
            (Rx(@"زراع|agricult|food system|غذائي"), GreenThemes.Agriculture),
            (Rx(@"مدن مستدامة|sustainable cit|urban|حضري"), GreenThemes.SustainableCities),
            (Rx(@"سياسات|policy|politique|governance|حوكمة"), GreenThemes.Policy),
        };

        private static readonly Regex Ielts = Rx(@"ielts|آيلتس|ايلتس");
        private static readonly Regex Toefl = Rx(@"toefl|توفل|تويفل");
        private static readonly Regex Delf = Rx(@"delf|dalf|tcf|niveau b2|niveau c1");
        private static readonly Regex NoTest = Rx(@"بدون (آيلتس|ايلتس|توفل|شهادة لغة)|no ielts|without ielts|sans ielts");
        private static readonly Regex Remote = Rx(@"عن بعد|عن بُعد|remote|à distance|télétravail|work from home");
        private static readonly Regex Online = Rx(@"أونلاين|اونلاين|عبر الإنترنت|online|en ligne|virtual|افتراضي");
        private static readonly Regex Women = Rx(@"للنساء|للفتيات|للسيدات|women[- ]only|for women|femmes uniquement|pour les femmes");
        private static readonly Regex Fee = Rx(@"رسوم (تقديم|تسجيل)|application fee|registration fee|frais (de candidature|d'inscription)");

        public static Suggestion Suggest(Opportunity o)
        {
            var title = Join(o.TitleAr, o.TitleEn, o.TitleFr);
            var body = Join(o.EligibilityCriteriaAr, o.EligibilityCriteriaEn, o.BenefitsAr, o.BenefitsEn, o.DescriptionAr, o.DescriptionEn);
            var all = title + "\n" + body;

            FundingType? funding = null;
            foreach (var (p, v) in Funding) if (p.IsMatch(all)) { funding = v; break; }
            if (funding == null && o.Type is OpportunityType.Jobs or OpportunityType.Internships && Rx(@"راتب|salary|stipend|مكافأة").IsMatch(all))
                funding = FundingType.Paid;

            var degrees = DegreeLevels.None;
            foreach (var (p, v) in Degrees) if (p.IsMatch(all)) degrees |= v;

            LanguageRequirement? language = null;
            if (NoTest.IsMatch(all)) language = LanguageRequirement.None;
            else
            {
                var ielts = Ielts.IsMatch(all); var toefl = Toefl.IsMatch(all);
                if (ielts && toefl) language = LanguageRequirement.Either;
                else if (ielts) language = LanguageRequirement.IELTS;
                else if (toefl) language = LanguageRequirement.TOEFL;
                else if (Delf.IsMatch(all)) language = LanguageRequirement.French;
            }

            var themes = GreenThemes.None;
            foreach (var (p, v) in Themes) if (p.IsMatch(all)) themes |= v;

            return new Suggestion(funding, degrees, language, Remote.IsMatch(all), Online.IsMatch(all), Women.IsMatch(title + "\n" + (o.EligibilityCriteriaAr ?? "")), Fee.IsMatch(all), themes);
        }

        private static string Join(params string?[] parts) => string.Join("\n", parts.Where(p => !string.IsNullOrEmpty(p)));

        private static Regex Rx(string p) => new(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
