using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>نوع الإشعار — النص يُصاغ بلغة الواجهة وقت العرض، لا وقت الإنشاء.</summary>
    public enum NotificationKind
    {
        /// <summary>فرصة محفوظة يقترب موعدها النهائي (خلال 7 أيام).</summary>
        SavedDeadlineSoon = 1,
        /// <summary>فرص جديدة تطابق اهتمامات المستخدم.</summary>
        InterestMatches = 2,
        /// <summary>فرص جديدة تطابق بحثًا محفوظًا.</summary>
        SavedSearchMatches = 3
    }

    /// <summary>إشعار داخل الموقع — يولّده NotificationSweeper ويظهر في جرس الهيدر.</summary>
    public class Notification
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public NotificationKind Kind { get; set; }

        /// <summary>الفرصة المعنية (لإشعارات الموعد النهائي).</summary>
        public int? OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        /// <summary>عدد الفرص الجديدة (لإشعارات الاهتمامات والبحث المحفوظ).</summary>
        public int Count { get; set; }

        /// <summary>نص البحث المحفوظ كما كتبه المستخدم — للعرض فقط.</summary>
        public string? Label { get; set; }

        /// <summary>معاملات رابط الوجهة (مثل search=منح&amp;country=مصر) تُلحق بصفحة كل الفرص.</summary>
        public string? LinkParams { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReadAt { get; set; }
    }
}
