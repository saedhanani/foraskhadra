using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Models
{
    public sealed record FaqItem(string Q, string A);

    /// <summary>
    /// صفحة نية (المرحلة 2.3): رابط ثابت /opportunities/{slug} بعنوان ومقدمة بشرية وأسئلة شائعة لكل لغة، وفلتر محفوظ
    /// يُطبَّق على الفرص. تُفهرس فقط إن كانت منشورة وبمقدمة وبخمس فرص مفتوحة على الأقل؛ وإلا noindex.
    /// </summary>
    public class LandingPage
    {
        public const int MinOpenToIndex = 5;
        public const int MinIntroWords = 50;

        public int Id { get; set; }

        /// <summary>أحرف لاتينية صغيرة وأرقام وشرطات؛ ثابت لكل اللغات.</summary>
        [Required, StringLength(80)]
        public string Slug { get; set; } = string.Empty;

        [Required, StringLength(200)] public string TitleAr { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleEn { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleFr { get; set; } = string.Empty;

        /// <summary>فقرات نصية مفصولة بسطر فارغ (لا HTML).</summary>
        public string? IntroAr { get; set; }
        public string? IntroEn { get; set; }
        public string? IntroFr { get; set; }

        /// <summary>{"ar":[{"q":"…","a":"…"}],"en":[…],"fr":[…]}</summary>
        public string? FaqJson { get; set; }

        /// <summary>OpportunityQuery بصيغة JSON (بلا page ولا sort).</summary>
        [Required]
        public string FilterJson { get; set; } = "{}";

        public bool IsPublished { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        public string Title(string lang) => lang == "en" ? TitleEn : lang == "fr" ? TitleFr : TitleAr;
        public string? Intro(string lang) => lang == "en" ? IntroEn : lang == "fr" ? IntroFr : IntroAr;

        /// <summary>وصف الصفحة: أول 155 حرفًا من المقدمة.</summary>
        public string Meta(string lang)
        {
            var text = (Intro(lang) ?? string.Empty).Replace('\n', ' ').Trim();
            return text.Length > 155 ? text[..152].TrimEnd() + "…" : text;
        }

        public bool HasIntro(string lang) =>
            (Intro(lang) ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length >= MinIntroWords;

        public bool IsIndexable(int openCount, string lang) => IsPublished && openCount >= MinOpenToIndex && HasIntro(lang);

        public OpportunityQuery Query()
        {
            OpportunityQuery q;
            try { q = JsonSerializer.Deserialize<OpportunityQuery>(string.IsNullOrWhiteSpace(FilterJson) ? "{}" : FilterJson, Json) ?? new OpportunityQuery(); }
            catch (JsonException) { q = new OpportunityQuery(); }
            q.Page = 1;
            return q;
        }

        /// <summary>يُحفظ الفلتر بلا page ولا sort (Page = 0 يُهمل عند الكتابة لأنه القيمة الافتراضية).</summary>
        public void SetQuery(OpportunityQuery q)
        {
            q.Page = 0;
            q.Sort = null;
            FilterJson = JsonSerializer.Serialize(q, Json);
        }

        public List<FaqItem> Faq(string lang)
        {
            if (string.IsNullOrWhiteSpace(FaqJson)) return new List<FaqItem>();
            try
            {
                var all = JsonSerializer.Deserialize<Dictionary<string, List<FaqItem>>>(FaqJson, Json);
                return all != null && all.TryGetValue(lang, out var items) ? items.Where(i => !string.IsNullOrWhiteSpace(i.Q) && !string.IsNullOrWhiteSpace(i.A)).ToList() : new List<FaqItem>();
            }
            catch (JsonException) { return new List<FaqItem>(); }
        }

        public void SetFaq(List<FaqItem> ar, List<FaqItem> en, List<FaqItem> fr) =>
            FaqJson = JsonSerializer.Serialize(new Dictionary<string, List<FaqItem>> { ["ar"] = ar, ["en"] = en, ["fr"] = fr }, Json);

        /// <summary>نص المدير: كل سؤال في سطر ثم جوابه في السطر التالي، والأسئلة مفصولة بسطر فارغ.</summary>
        public static List<FaqItem> ParseFaqText(string? text)
        {
            var items = new List<FaqItem>();
            if (string.IsNullOrWhiteSpace(text)) return items;
            foreach (var block in text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
            {
                var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (lines.Length >= 2) items.Add(new FaqItem(lines[0], string.Join(" ", lines.Skip(1))));
            }
            return items.Take(10).ToList();
        }

        public static string FaqToText(List<FaqItem> items) => string.Join("\n\n", items.Select(i => i.Q + "\n" + i.A));

        /// <summary>عدد الفرص المفتوحة التي يطابقها فلتر الصفحة — شرط الفهرسة وما يظهر في لوحة المدير وsitemap.</summary>
        public static Task<int> OpenCountAsync(ApplicationDbContext db, LandingPage page, IReadOnlyCollection<Country> countries, DateTime today, CancellationToken ct = default)
        {
            today = today.Date;
            var parsed = OpportunityFilter.Parse(page.Query(), countries);
            return OpportunityFilter.Apply(db.Opportunities.AsNoTracking(), parsed, today)
                .CountAsync(o => o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today, ct);
        }
    }
}
