using System.ComponentModel.DataAnnotations;

namespace Foras_Khadra.Models
{
    /// <summary>نموذج تحرير دليل التقديم في اللوحة (4.1): الأقسام نصوص بفقرات، وقائمة المهام سطر لكل مهمة.</summary>
    public class GuideForm
    {
        public int Id { get; set; }
        [Required, StringLength(60), RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "مفتاح البرنامج: أحرف لاتينية صغيرة وأرقام وشرطات (daad-epos)")]
        public string ProgramKey { get; set; } = string.Empty;
        [Required, StringLength(80), RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "الرابط: أحرف لاتينية صغيرة وأرقام وشرطات")]
        public string Slug { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleAr { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleEn { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TitleFr { get; set; } = string.Empty;
        [StringLength(400)] public string? SummaryAr { get; set; }
        [StringLength(400)] public string? SummaryEn { get; set; }
        [StringLength(400)] public string? SummaryFr { get; set; }
        [StringLength(500)] public string? OfficialUrl { get; set; }
        [Range(1, 12)] public int? OpensMonth { get; set; }
        [Range(1, 12)] public int? ClosesMonth { get; set; }
        public bool IsPublished { get; set; }
        public int SortOrder { get; set; }

        public string? ConditionsAr { get; set; } public string? ConditionsEn { get; set; } public string? ConditionsFr { get; set; }
        public string? TimelineAr { get; set; } public string? TimelineEn { get; set; } public string? TimelineFr { get; set; }
        public string? LetterAr { get; set; } public string? LetterEn { get; set; } public string? LetterFr { get; set; }
        public string? MistakesAr { get; set; } public string? MistakesEn { get; set; } public string? MistakesFr { get; set; }
        public string? TipsAr { get; set; } public string? TipsEn { get; set; } public string? TipsFr { get; set; }
        public string? ChecklistAr { get; set; } public string? ChecklistEn { get; set; } public string? ChecklistFr { get; set; }

        public static GuideForm From(ApplicationGuide g)
        {
            var all = g.All();
            GuideSections S(string l) => all.TryGetValue(l, out var s) ? s : new GuideSections();
            var ar = S("ar"); var en = S("en"); var fr = S("fr");
            return new GuideForm
            {
                Id = g.Id, ProgramKey = g.ProgramKey, Slug = g.Slug,
                TitleAr = g.TitleAr, TitleEn = g.TitleEn, TitleFr = g.TitleFr,
                SummaryAr = g.SummaryAr, SummaryEn = g.SummaryEn, SummaryFr = g.SummaryFr,
                OfficialUrl = g.OfficialUrl, OpensMonth = g.OpensMonth, ClosesMonth = g.ClosesMonth, IsPublished = g.IsPublished, SortOrder = g.SortOrder,
                ConditionsAr = ar.Conditions, ConditionsEn = en.Conditions, ConditionsFr = fr.Conditions,
                TimelineAr = ar.Timeline, TimelineEn = en.Timeline, TimelineFr = fr.Timeline,
                LetterAr = ar.Letter, LetterEn = en.Letter, LetterFr = fr.Letter,
                MistakesAr = ar.Mistakes, MistakesEn = en.Mistakes, MistakesFr = fr.Mistakes,
                TipsAr = ar.Tips, TipsEn = en.Tips, TipsFr = fr.Tips,
                ChecklistAr = string.Join("\n", ar.Checklist), ChecklistEn = string.Join("\n", en.Checklist), ChecklistFr = string.Join("\n", fr.Checklist)
            };
        }

        public void ApplyTo(ApplicationGuide g)
        {
            g.ProgramKey = ProgramKey.Trim().ToLowerInvariant();
            g.Slug = Slug.Trim().ToLowerInvariant();
            g.TitleAr = TitleAr.Trim(); g.TitleEn = TitleEn.Trim(); g.TitleFr = TitleFr.Trim();
            g.SummaryAr = Clean(SummaryAr); g.SummaryEn = Clean(SummaryEn); g.SummaryFr = Clean(SummaryFr);
            g.OfficialUrl = Clean(OfficialUrl);
            g.OpensMonth = OpensMonth; g.ClosesMonth = ClosesMonth;
            g.IsPublished = IsPublished; g.SortOrder = SortOrder;
            g.UpdatedAt = DateTime.Now;
            GuideSections Sec(string? c, string? t, string? l, string? m, string? tips, string? check) => new()
            {
                Conditions = Clean(c) ?? "", Timeline = Clean(t) ?? "", Letter = Clean(l) ?? "", Mistakes = Clean(m) ?? "", Tips = Clean(tips) ?? "",
                Checklist = ApplicationGuide.ParseChecklist(check)
            };
            g.SetSections(Sec(ConditionsAr, TimelineAr, LetterAr, MistakesAr, TipsAr, ChecklistAr),
                          Sec(ConditionsEn, TimelineEn, LetterEn, MistakesEn, TipsEn, ChecklistEn),
                          Sec(ConditionsFr, TimelineFr, LetterFr, MistakesFr, TipsFr, ChecklistFr));
        }

        private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Replace("\r\n", "\n").Trim();
    }
}
