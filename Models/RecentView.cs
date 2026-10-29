using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>آخر فرصة فتحها المستخدم (البند 2-3 "تابعت مؤخرًا"). (UserId, OpportunityId) فريد ويُحدَّث وقته عند كل فتح.</summary>
    public class RecentView
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.Now;
    }
}
