using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>
    /// إحصاء يومي مجمَّع لكل فرصة (بلا بيانات شخصية): نقرات «قدّم الآن» عبر بوابة /go/{id}، ولاحقًا المشاهدات.
    /// المفتاح المركّب (OpportunityId, Day). أساس تقرير الوصول للمنظمات.
    /// </summary>
    public class OpportunityDailyStat
    {
        public int OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        public DateOnly Day { get; set; }

        public int ApplyClicks { get; set; }

        public int Views { get; set; }
    }
}
