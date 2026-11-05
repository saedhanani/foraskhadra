using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    public enum QuestionStatus
    {
        /// <summary>سؤال عضو بانتظار الجواب/المراجعة؛ لا يظهر للعموم.</summary>
        Pending = 0,
        /// <summary>منشور على صفحة الفرصة (وفي FAQPage إن كان له جواب).</summary>
        Published = 1,
        Hidden = 2
    }

    /// <summary>
    /// أسئلة وأجوبة على فرصة (المرحلة 4.6): منسَّقة من المدير (IsCurated، بثلاث لغات) أو من الأعضاء (بلغة واحدة) تُنشر بعد
    /// المراجعة. المنشور ذو الجواب يُصدَّر كـFAQPage على صفحة الفرصة.
    /// </summary>
    public class OpportunityQuestion
    {
        public const int MaxQuestion = 300;
        public const int MaxAnswer = 1500;

        public int Id { get; set; }

        public int OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        [Required, StringLength(MaxQuestion)] public string QuestionAr { get; set; } = string.Empty;
        [StringLength(MaxQuestion)] public string? QuestionEn { get; set; }
        [StringLength(MaxQuestion)] public string? QuestionFr { get; set; }

        [StringLength(MaxAnswer)] public string? AnswerAr { get; set; }
        [StringLength(MaxAnswer)] public string? AnswerEn { get; set; }
        [StringLength(MaxAnswer)] public string? AnswerFr { get; set; }

        /// <summary>لغة سؤال العضو (ar/en/fr)؛ للمنسَّق تُملأ الحقول الثلاثة.</summary>
        [StringLength(2)] public string Lang { get; set; } = "ar";

        /// <summary>العضو السائل (null للمنسَّق من المدير).</summary>
        [StringLength(450)] public string? UserId { get; set; }
        [ForeignKey("UserId")] public ApplicationUser? User { get; set; }

        public bool IsCurated { get; set; }
        public QuestionStatus Status { get; set; } = QuestionStatus.Pending;
        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? AnsweredAt { get; set; }
        [StringLength(450)] public string? AnsweredByUserId { get; set; }

        public string Question(string lang) => lang == "en" && !string.IsNullOrWhiteSpace(QuestionEn) ? QuestionEn! : lang == "fr" && !string.IsNullOrWhiteSpace(QuestionFr) ? QuestionFr! : QuestionAr;
        public string? Answer(string lang) => lang == "en" && !string.IsNullOrWhiteSpace(AnswerEn) ? AnswerEn : lang == "fr" && !string.IsNullOrWhiteSpace(AnswerFr) ? AnswerFr : AnswerAr;
        public bool HasAnswer => !string.IsNullOrWhiteSpace(AnswerAr) || !string.IsNullOrWhiteSpace(AnswerEn) || !string.IsNullOrWhiteSpace(AnswerFr);
    }
}
