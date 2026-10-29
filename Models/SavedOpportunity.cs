using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>فرصة حفظها مستخدم في قائمته. (UserId, OpportunityId) فريد.</summary>
    public class SavedOpportunity
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.Now;

        // ===== تتبّع التقديم (المرحلة 2) =====
        /// <summary>حالة التقديم كما يحدّدها المستخدم. تغييرها إلى "قدّمت" فما بعد يُكمل بنود رفيق التقديم.</summary>
        public ApplicationStatus Status { get; set; } = ApplicationStatus.NotApplied;
        public DateTime? StatusChangedAt { get; set; }

        /// <summary>بنود رفيق التقديم لهذه الفرصة.</summary>
        public ICollection<ChecklistItem> Checklist { get; set; } = new List<ChecklistItem>();
    }
}
