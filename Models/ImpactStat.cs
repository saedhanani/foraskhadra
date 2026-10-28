using System.ComponentModel.DataAnnotations;

namespace Foras_Khadra.Models
{
    /// <summary>رقم من أرقام الأثر في بانر صفحة "نشاطاتنا" (120+ فعالية، 22 دولة...). يُحرَّر من لوحة التحكم.</summary>
    public class ImpactStat
    {
        public int Id { get; set; }

        [Required] public string Value { get; set; } = string.Empty;
        [Required] public string LabelAr { get; set; } = string.Empty;
        [Required] public string LabelEn { get; set; } = string.Empty;
        [Required] public string LabelFr { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Value) && string.IsNullOrWhiteSpace(LabelAr) && string.IsNullOrWhiteSpace(LabelEn) && string.IsNullOrWhiteSpace(LabelFr);
    }
}
