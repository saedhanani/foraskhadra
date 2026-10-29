using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>"أعلمني حين تفتح الدورة القادمة" على فرصة منتهية (البند 3-3). (UserId, OpportunityId) فريد.
    /// يُنبَّه المستخدم مرة واحدة إذا أُعيد فتح الفرصة نفسها أو نُشرت فرصة جديدة بعنوان مطابق.</summary>
    public class CycleWatch
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>وقت إرسال التنبيه؛ null = ما زال ينتظر.</summary>
        public DateTime? NotifiedAt { get; set; }
    }
}
