using System.ComponentModel.DataAnnotations;

namespace Foras_Khadra.Models
{
    /// <summary>مشاهدات يومية مجمَّعة لكل صفحة (مسار مطبَّع بلا slug ولا استعلام) ولغة — بلا كوكيز ولا بيانات شخصية.</summary>
    public class PageViewDailyStat
    {
        public int Id { get; set; }
        public DateOnly Day { get; set; }
        [StringLength(200)] public string Path { get; set; } = "/";
        [StringLength(2)] public string Culture { get; set; } = "ar";
        public int Views { get; set; }
    }

    /// <summary>من أين جاءت الزيارات: مضيف الإحالة الخارجي، أو utm:…، أو pwa، أو direct. التنقل الداخلي لا يُعدّ.</summary>
    public class TrafficSourceDailyStat
    {
        public int Id { get; set; }
        public DateOnly Day { get; set; }
        [StringLength(100)] public string Source { get; set; } = "direct";
        public int Views { get; set; }
    }

    /// <summary>أحداث مسمّاة يومية (قمع التسجيل والتقديم، بحث بصفر نتائج مع العبارة في Label، طلبات المنظمات…).</summary>
    public class SiteEventDailyStat
    {
        public int Id { get; set; }
        public DateOnly Day { get; set; }
        [StringLength(60)] public string Event { get; set; } = string.Empty;
        [StringLength(200)] public string? Label { get; set; }
        public int Count { get; set; }
    }
}
