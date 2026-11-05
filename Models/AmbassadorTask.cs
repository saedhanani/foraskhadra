using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    public enum TaskSubmissionStatus { Submitted = 0, Approved = 1, Rejected = 2 }

    /// <summary>تعريف مهمة شهرية للسفراء الجامعيين (المرحلة 4.4) — القائمة ثابتة في Helpers/AmbassadorTasks.</summary>
    public sealed record AmbassadorTaskDef(string Key, int Points, string TitleAr, string TitleEn, string TitleFr, string HintAr, string HintEn, string HintFr, bool NeedsProof)
    {
        public string Title(string lang) => lang == "en" ? TitleEn : lang == "fr" ? TitleFr : TitleAr;
        public string Hint(string lang) => lang == "en" ? HintEn : lang == "fr" ? HintFr : HintAr;
    }

    /// <summary>
    /// تسليم مهمة من سفير في شهر معيّن (صف واحد لكل عضو × مهمة × شهر). المهام التي تحتاج إثباتًا (رابط صورة/منشور)
    /// ينتظر المدير اعتمادها؛ الآلية (الإحالات) تُعتمد تلقائيًا من العدّادات.
    /// </summary>
    public class AmbassadorTaskSubmission
    {
        public int Id { get; set; }

        [Required, StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required, StringLength(40)]
        public string TaskKey { get; set; } = string.Empty;

        /// <summary>yyyy-MM</summary>
        [Required, StringLength(7)]
        public string Month { get; set; } = string.Empty;

        [StringLength(500)] public string? Note { get; set; }
        [StringLength(500)] public string? ProofUrl { get; set; }

        public TaskSubmissionStatus Status { get; set; } = TaskSubmissionStatus.Submitted;
        public int PointsAwarded { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
        [StringLength(450)] public string? ReviewedByUserId { get; set; }

        public static string MonthKey(DateTime d) => d.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
    }
}
