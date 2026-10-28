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
    }
}
