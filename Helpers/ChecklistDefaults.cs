using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>البنود الافتراضية لرفيق التقديم حسب نوع الفرصة، وترجمتها وقت العرض.
    /// المفاتيح تُخزَّن في ChecklistItem.Key؛ غيّر النصوص هنا دون لمس القاعدة.</summary>
    public static class ChecklistDefaults
    {
        private static readonly Dictionary<string, (string ar, string en, string fr)> Labels = new()
        {
            ["passport"] = ("جواز سفر ساري", "Valid passport", "Passeport valide"),
            ["transcript"] = ("كشف العلامات", "Academic transcript", "Relevé de notes"),
            ["motivation_letter"] = ("خطاب الدوافع", "Motivation letter", "Lettre de motivation"),
            ["recommendation_letters"] = ("خطابا توصية", "Two recommendation letters", "Deux lettres de recommandation"),
            ["cv"] = ("السيرة الذاتية", "CV / résumé", "CV"),
            ["language_certificate"] = ("شهادة لغة", "Language certificate", "Certificat de langue"),
            ["research_proposal"] = ("مقترح البحث", "Research proposal", "Projet de recherche"),
            ["cover_letter"] = ("خطاب التغطية", "Cover letter", "Lettre d'accompagnement"),
            ["portfolio"] = ("نماذج من الأعمال", "Portfolio / work samples", "Portfolio"),
            ["enrollment_proof"] = ("إثبات القيد الجامعي", "Proof of enrollment", "Attestation d'inscription"),
            ["team"] = ("تشكيل الفريق", "Form the team", "Constituer l'équipe"),
            ["idea_summary"] = ("ملخص الفكرة", "Idea summary", "Résumé de l'idée"),
            ["pitch_deck"] = ("العرض التقديمي", "Pitch deck", "Présentation"),
            ["abstract"] = ("ملخص المشاركة", "Abstract", "Résumé de la contribution"),
            ["visa"] = ("التأشيرة / إذن السفر", "Visa / travel permit", "Visa / autorisation de voyage"),
            ["availability"] = ("تحديد أوقات التفرّغ", "Confirm availability", "Confirmer vos disponibilités")
        };

        /// <summary>مفاتيح البنود الافتراضية لنوع الفرصة، بترتيب العرض.</summary>
        public static IReadOnlyList<string> For(OpportunityType? type) => type switch
        {
            OpportunityType.Scholarships => new[] { "passport", "transcript", "motivation_letter", "recommendation_letters", "cv", "language_certificate" },
            OpportunityType.Fellowships => new[] { "cv", "motivation_letter", "recommendation_letters", "research_proposal" },
            OpportunityType.Jobs => new[] { "cv", "cover_letter", "portfolio" },
            OpportunityType.Internships => new[] { "cv", "cover_letter", "enrollment_proof" },
            OpportunityType.Competitions => new[] { "team", "idea_summary", "pitch_deck" },
            OpportunityType.Conferences => new[] { "abstract", "passport", "visa" },
            OpportunityType.Volunteering => new[] { "cv", "motivation_letter", "availability" },
            _ => new[] { "cv", "motivation_letter" }
        };

        /// <summary>تسمية البند بلغة الواجهة؛ المفتاح غير المعروف يُعرض كما هو.</summary>
        public static string Label(string key, string lang) =>
            Labels.TryGetValue(key, out var l) ? (lang == "en" ? l.en : lang == "fr" ? l.fr : l.ar) : key;
    }
}
