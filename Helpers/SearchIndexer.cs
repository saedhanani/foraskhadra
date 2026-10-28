using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// بحث نصي كامل بلا اعتماد على Full-Text Search في SQL Server (غير متاح على الاستضافة المشتركة):
    /// نص الفرصة كله (العناوين والأوصاف والتفاصيل والشروط والمزايا بالثلاث لغات) يُطبَّع ويُخزَّن في
    /// Opportunity.SearchText عند الحفظ (ApplicationDbContext)، وكلمات البحث تُطبَّع بالطريقة نفسها
    /// ثم يُشترط ورودها كلها (LIKE لكل كلمة).
    /// التطبيع: حروف صغيرة، إزالة HTML والتشكيل والتطويل، توحيد الهمزات (أإآ→ا) والتاء المربوطة (ة→ه)
    /// والألف المقصورة (ى→ي)، وكل ما ليس حرفًا أو رقمًا يصبح مسافة.
    /// </summary>
    public static class SearchIndexer
    {
        private static readonly Regex HtmlTag = new("<.*?>", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var plain = WebUtility.HtmlDecode(HtmlTag.Replace(text, " ")).ToLowerInvariant();
            var sb = new StringBuilder(plain.Length);

            foreach (var ch in plain)
            {
                switch (ch)
                {
                    case 'أ': case 'إ': case 'آ': sb.Append('ا'); break;
                    case 'ة': sb.Append('ه'); break;
                    case 'ى': sb.Append('ي'); break;
                    case 'ـ': break;                              // تطويل
                    case >= 'ً' and <= 'ْ': break;           // تشكيل
                    default:
                        sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
                        break;
                }
            }

            return Whitespace.Replace(sb.ToString(), " ").Trim();
        }

        /// <summary>كلمات البحث المطبّعة، بلا تكرار، وبلا كلمات من حرف واحد.</summary>
        public static List<string> Terms(string? query) =>
            Normalize(query)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 1)
                .Distinct()
                .Take(10)
                .ToList();

        public static string Build(Opportunity o) =>
            Normalize(string.Join(" ", new[]
            {
                o.TitleAr, o.TitleEn, o.TitleFr,
                o.DescriptionAr, o.DescriptionEn, o.DescriptionFr,
                o.DetailsAr, o.DetailsEn, o.DetailsFr,
                o.EligibilityCriteriaAr, o.EligibilityCriteriaEn, o.EligibilityCriteriaFr,
                o.BenefitsAr, o.BenefitsEn, o.BenefitsFr,
                o.PublishedBy
            }));
    }
}
