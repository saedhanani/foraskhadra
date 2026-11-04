using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>حالة طلب النشر القادم من بوابة المنظمات (/for-organizations).</summary>
    public enum PublishRequestStatus
    {
        /// <summary>وصل ولم تأخذه منصة الكتابة ولا المدير بعد.</summary>
        New = 0,
        /// <summary>أخذته منصة الكتابة (أو المدير) وتُكتب الفرصة.</summary>
        InProgress = 1,
        /// <summary>نُشرت الفرصة المرتبطة وأُبلغت المنظمة.</summary>
        Published = 2,
        /// <summary>ليس فرصة، أو منتهية، أو خارج نطاق المنصة — مع ملاحظة.</summary>
        Rejected = 3,
        Spam = 4
    }

    /// <summary>
    /// طلب نشر بلا حساب: المنظمة تترك اسمها وبريدها ورابط الفرصة، ومنصة الكتابة تجلبه من GET /api/team/requests
    /// وتكتب الفرصة بثلاث لغات، وعند نشرها يُغلق الطلب ويصل المنظمةَ رابطُها (Services/PublishRequestService).
    /// </summary>
    public class PublishRequest
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string OrganizationName { get; set; } = string.Empty;

        [StringLength(120)]
        public string? ContactName { get; set; }

        [Required, StringLength(256)]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Website { get; set; }

        /// <summary>رابط الفرصة في موقع الجهة (http/https فقط).</summary>
        [Required, StringLength(1000)]
        public string SourceUrl { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Notes { get; set; }

        /// <summary>نوع الفرصة كما اختارته المنظمة (تلميح للكاتب، لا قيد).</summary>
        public OpportunityType? TypeHint { get; set; }

        /// <summary>الجمهور المستهدف: رمز ISO-2، أو XA = الوطن العربي، أو XW = الجميع.</summary>
        [StringLength(2)]
        public string? CountryIso { get; set; }

        /// <summary>لغة واجهة مُرسل الطلب؛ بها تُرسل رسائل الاستلام والنشر.</summary>
        [StringLength(2)]
        public string Language { get; set; } = "ar";

        /// <summary>العدد التقديري للأعضاء المؤهَّلين كما عُرض للمنظمة لحظة الإرسال.</summary>
        public int EstimatedReach { get; set; }

        public PublishRequestStatus Status { get; set; } = PublishRequestStatus.New;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        /// <summary>متى أخذته منصة الكتابة أو المدير.</summary>
        public DateTime? TakenAt { get; set; }
        /// <summary>متى أُغلق (نُشر أو رُفض).</summary>
        public DateTime? ClosedAt { get; set; }

        /// <summary>الفرصة التي كُتبت من هذا الطلب (تُربط عند إنشائها من المنصة بـrequestId أو من المدير).</summary>
        public int? OpportunityId { get; set; }
        [ForeignKey(nameof(OpportunityId))]
        public Opportunity? Opportunity { get; set; }

        /// <summary>ملاحظة المدير أو سبب الرفض (يصل للمنظمة في رسالة الاعتذار).</summary>
        [StringLength(500)]
        public string? AdminNote { get; set; }
    }
}
