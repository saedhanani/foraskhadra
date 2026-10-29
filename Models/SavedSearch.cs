using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>بحث حفظه المستخدم من صفحة كل الفرص — يفحصه NotificationSweeper دوريًا وينبّه بالجديد.</summary>
    public class SavedSearch
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        /// <summary>نص البحث كما كتبه المستخدم (قد يكون فارغًا إن حفظ فلاتر فقط).</summary>
        [MaxLength(200)]
        public string Query { get; set; } = string.Empty;

        /// <summary>أنواع الفرص المختارة مفصولة بفاصلة (قيم OpportunityType)، أو فارغ.</summary>
        [MaxLength(200)]
        public string TypesCsv { get; set; } = string.Empty;

        /// <summary>اسم الدولة المفلتر بها كما يمرَّر لصفحة كل الفرص، أو فارغ.</summary>
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>آخر وقت فحصه الماسح — الفرص الأحدث منه فقط تولّد إشعارًا.</summary>
        public DateTime LastRunAt { get; set; } = DateTime.Now;
    }
}
