using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>
    /// «عزّز للمؤهَّلين» (المرحلة 4.5): دفعة إشعارات مدفوعة من المدير لفرصة واحدة إلى الأعضاء الذين تنطبق عليهم شروطها
    /// (عبر CompassMatcher). سجلّ للمراجعة والفوترة: متى، كم عضوًا، ومن أطلقها.
    /// </summary>
    public class OpportunityBoost
    {
        public int Id { get; set; }

        public int OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        public int Recipients { get; set; }

        [StringLength(450)] public string? CreatedByUserId { get; set; }
        [StringLength(200)] public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
