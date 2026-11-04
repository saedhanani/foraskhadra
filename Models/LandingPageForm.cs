using System.ComponentModel.DataAnnotations;
using Foras_Khadra.Helpers;

namespace Foras_Khadra.Models
{
    /// <summary>نموذج تحرير صفحة نية في لوحة المدير (/Admin/LandingPageEdit).</summary>
    public class LandingPageForm
    {
        public int Id { get; set; }

        [Required, StringLength(80), RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "أحرف لاتينية صغيرة وأرقام وشرطات فقط")]
        public string Slug { get; set; } = string.Empty;

        [Required, StringLength(200)] public string TitleAr { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleEn { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleFr { get; set; } = string.Empty;

        public string? IntroAr { get; set; }
        public string? IntroEn { get; set; }
        public string? IntroFr { get; set; }

        /// <summary>سؤال في سطر ثم جوابه في السطر التالي؛ الأسئلة مفصولة بسطر فارغ.</summary>
        public string? FaqAr { get; set; }
        public string? FaqEn { get; set; }
        public string? FaqFr { get; set; }

        public bool IsPublished { get; set; } = true;
        public int SortOrder { get; set; }

        // الفلتر المحفوظ (نفس معاملات صفحة الفرص)
        public string? Search { get; set; }
        public List<string> Types { get; set; } = new();
        public string? Country { get; set; }
        public string? Funding { get; set; }
        public string? Degree { get; set; }
        public string? Field { get; set; }
        public string? Theme { get; set; }
        public string? Host { get; set; }
        public bool Remote { get; set; }
        public bool NoIelts { get; set; }
        public bool Women { get; set; }
        public int? Within { get; set; }

        public OpportunityQuery ToQuery() => new()
        {
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            Type = Types.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
            Country = string.IsNullOrWhiteSpace(Country) ? null : Country.Trim(),
            Funding = string.IsNullOrWhiteSpace(Funding) ? null : Funding,
            Degree = string.IsNullOrWhiteSpace(Degree) ? null : Degree,
            Field = string.IsNullOrWhiteSpace(Field) ? new List<string>() : new List<string> { Field },
            Theme = string.IsNullOrWhiteSpace(Theme) ? null : Theme,
            Host = string.IsNullOrWhiteSpace(Host) ? null : Host,
            Remote = Remote,
            NoIelts = NoIelts,
            Women = Women,
            Within = Within
        };

        public static LandingPageForm From(LandingPage p)
        {
            var q = p.Query();
            return new LandingPageForm
            {
                Id = p.Id, Slug = p.Slug, TitleAr = p.TitleAr, TitleEn = p.TitleEn, TitleFr = p.TitleFr,
                IntroAr = p.IntroAr, IntroEn = p.IntroEn, IntroFr = p.IntroFr,
                FaqAr = LandingPage.FaqToText(p.Faq("ar")), FaqEn = LandingPage.FaqToText(p.Faq("en")), FaqFr = LandingPage.FaqToText(p.Faq("fr")),
                IsPublished = p.IsPublished, SortOrder = p.SortOrder,
                Search = q.Search, Types = q.Type, Country = q.Country, Funding = q.Funding, Degree = q.Degree, Field = q.Field.FirstOrDefault(),
                Theme = q.Theme, Host = q.Host, Remote = q.Remote, NoIelts = q.NoIelts, Women = q.Women, Within = q.Within
            };
        }

        public void ApplyTo(LandingPage p)
        {
            p.Slug = Slug.Trim().ToLowerInvariant();
            p.TitleAr = TitleAr.Trim(); p.TitleEn = TitleEn.Trim(); p.TitleFr = TitleFr.Trim();
            p.IntroAr = Clean(IntroAr); p.IntroEn = Clean(IntroEn); p.IntroFr = Clean(IntroFr);
            p.SetFaq(LandingPage.ParseFaqText(FaqAr), LandingPage.ParseFaqText(FaqEn), LandingPage.ParseFaqText(FaqFr));
            p.IsPublished = IsPublished;
            p.SortOrder = SortOrder;
            p.SetQuery(ToQuery());
            p.UpdatedAt = DateTime.Now;
        }

        private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Replace("\r\n", "\n").Trim();
    }
}
