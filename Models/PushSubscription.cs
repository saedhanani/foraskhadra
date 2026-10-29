using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>اشتراك متصفح/جهاز في إشعارات الدفع (Web Push) لمستخدم مسجّل — يُنشأ من بطاقة "فعّل التنبيهات".
    /// للمستخدم أكثر من اشتراك (هاتف + حاسوب). يُحذف تلقائيًا حين ترد خدمة الدفع بأنه انتهى (404/410).</summary>
    public class PushSubscription
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        /// <summary>رابط خدمة الدفع الخاص بهذا المتصفح (Google/Apple/Mozilla/Microsoft) — انظر PushEndpoints.
        /// العمود اسمه PushUrl لا Endpoint: بوابة SQL Studio في الاستضافة ترفض أي استعلام فيه كلمة ENDPOINT (كلمة محجوزة لكائنات الخادم).</summary>
        [MaxLength(1000)]
        [Column("PushUrl")]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>المفتاح العام للمتصفح (P-256، base64url) — لتشفير الحمولة.</summary>
        [MaxLength(200)]
        public string P256dh { get; set; } = string.Empty;

        /// <summary>سرّ المصادقة للمتصفح (16 بايت، base64url).</summary>
        [MaxLength(100)]
        public string Auth { get; set; } = string.Empty;

        /// <summary>لغة الواجهة وقت الاشتراك — يُصاغ بها نص الإشعار.</summary>
        [MaxLength(5)]
        public string Lang { get; set; } = "ar";

        /// <summary>فرق التوقيت كما يعيده getTimezoneOffset() في المتصفح (دقائق، UTC − المحلي) — لتأجيل الإشعارات ليلًا.</summary>
        public int TzOffsetMinutes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastSentAt { get; set; }
    }
}
