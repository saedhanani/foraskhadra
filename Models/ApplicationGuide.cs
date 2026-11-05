using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foras_Khadra.Models
{
    /// <summary>أقسام الدليل بلغة واحدة: نصوص بفقرات (سطر فارغ بين الفقرات)، وقائمة مهام تُحمَّل في المتابعة.</summary>
    public sealed class GuideSections
    {
        public string Conditions { get; set; } = string.Empty;
        public string Timeline { get; set; } = string.Empty;
        public string Letter { get; set; } = string.Empty;
        public string Mistakes { get; set; } = string.Empty;
        public string Tips { get; set; } = string.Empty;
        public List<string> Checklist { get; set; } = new();

        public bool IsEmpty => string.IsNullOrWhiteSpace(Conditions) && string.IsNullOrWhiteSpace(Timeline) && string.IsNullOrWhiteSpace(Letter)
            && string.IsNullOrWhiteSpace(Mistakes) && string.IsNullOrWhiteSpace(Tips) && Checklist.Count == 0;
    }

    /// <summary>
    /// دليل تقديم لبرنامج متكرر (المرحلة 4.1): تشيفنينغ، DAAD، إيراسموس…؛ مرتبط بـOpportunity.ProgramKey فيظهر على كل فرصة
    /// من البرنامج وفي «قريبًا» في البوصلة. له رابط ثابت /guides/{slug} بثلاث لغات، وقائمة مهام تُحمَّل في متابعة العضو.
    /// </summary>
    public class ApplicationGuide
    {
        public int Id { get; set; }

        /// <summary>يطابق Opportunity.ProgramKey (أحرف لاتينية صغيرة وشرطات).</summary>
        [Required, StringLength(60)]
        public string ProgramKey { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string Slug { get; set; } = string.Empty;

        [Required, StringLength(200)] public string TitleAr { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleEn { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleFr { get; set; } = string.Empty;

        /// <summary>سطر أو سطران: ما البرنامج ولمن.</summary>
        [StringLength(400)] public string? SummaryAr { get; set; }
        [StringLength(400)] public string? SummaryEn { get; set; }
        [StringLength(400)] public string? SummaryFr { get; set; }

        /// <summary>{"ar": GuideSections, "en": …, "fr": …}</summary>
        public string SectionsJson { get; set; } = "{}";

        [StringLength(500)] public string? OfficialUrl { get; set; }

        /// <summary>الشهر الذي يفتح فيه التقديم عادةً (1–12) — للجدول الزمني و«قريبًا».</summary>
        public int? OpensMonth { get; set; }
        /// <summary>الشهر الذي يغلق فيه عادةً (1–12).</summary>
        public int? ClosesMonth { get; set; }

        public bool IsPublished { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public string Title(string lang) => lang == "en" ? TitleEn : lang == "fr" ? TitleFr : TitleAr;
        public string? Summary(string lang) => lang == "en" ? SummaryEn : lang == "fr" ? SummaryFr : SummaryAr;

        /// <summary>أقسام اللغة المطلوبة؛ وإن كانت فارغة (لم تُترجم بعد) تعود العربية بدل صفحة خاوية.</summary>
        public GuideSections Sections(string lang)
        {
            var all = All();
            if (all.TryGetValue(lang, out var s) && !s.IsEmpty) return s;
            return all.TryGetValue("ar", out var ar) ? ar : s ?? new GuideSections();
        }

        public Dictionary<string, GuideSections> All()
        {
            if (string.IsNullOrWhiteSpace(SectionsJson)) return new Dictionary<string, GuideSections>();
            try { return JsonSerializer.Deserialize<Dictionary<string, GuideSections>>(SectionsJson, Json) ?? new(); }
            catch (JsonException) { return new Dictionary<string, GuideSections>(); }
        }

        public void SetSections(GuideSections ar, GuideSections en, GuideSections fr) =>
            SectionsJson = JsonSerializer.Serialize(new Dictionary<string, GuideSections> { ["ar"] = ar, ["en"] = en, ["fr"] = fr }, Json);

        /// <summary>نص المدير: كل سطر مهمة.</summary>
        public static List<string> ParseChecklist(string? text) =>
            (text ?? string.Empty).Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(20).ToList();

        /// <summary>الأشهر بأسماء اللغة: «يفتح عادةً في سبتمبر ويغلق في نوفمبر».</summary>
        public string? Season(string lang)
        {
            if (OpensMonth is null && ClosesMonth is null) return null;
            var culture = System.Globalization.CultureInfo.GetCultureInfo(lang == "en" ? "en" : lang == "fr" ? "fr" : "ar");
            string M(int m) => culture.DateTimeFormat.GetMonthName(Math.Clamp(m, 1, 12));
            if (OpensMonth is int o && ClosesMonth is int c)
                return lang == "en" ? $"Usually opens in {M(o)} and closes in {M(c)}" : lang == "fr" ? $"Ouvre généralement en {M(o)} et ferme en {M(c)}" : $"يفتح عادةً في {M(o)} ويغلق في {M(c)}";
            if (OpensMonth is int o2) return lang == "en" ? $"Usually opens in {M(o2)}" : lang == "fr" ? $"Ouvre généralement en {M(o2)}" : $"يفتح عادةً في {M(o2)}";
            return lang == "en" ? $"Usually closes in {M(ClosesMonth!.Value)}" : lang == "fr" ? $"Ferme généralement en {M(ClosesMonth!.Value)}" : $"يغلق عادةً في {M(ClosesMonth!.Value)}";
        }
    }
}
