using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    public enum OpportunityReportReason
    {
        /// <summary>رابط التقديم لا يعمل.</summary>
        BrokenLink = 1,
        /// <summary>الفرصة منتهية رغم أنها تظهر مفتوحة.</summary>
        Expired = 2,
        /// <summary>الجهة أو وسيط يطلب مالًا.</summary>
        AsksForMoney = 3,
        /// <summary>معلومات خاطئة (الدول، الموعد، الشروط…).</summary>
        WrongInfo = 4,
        Other = 5
    }

    public enum OpportunityReportStatus
    {
        Open = 0,
        /// <summary>عولج (رابط صُحّح، فرصة أُغلقت…).</summary>
        Resolved = 1,
        /// <summary>بلاغ غير صحيح.</summary>
        Dismissed = 2
    }

    /// <summary>
    /// بلاغ زائر أو عضو عن مشكلة في فرصة (المرحلة 3.1). بلاغان مفتوحان من مُبلِّغَين مختلفَين يوقفان زر التقديم
    /// («التقديم موقوف مؤقتًا للمراجعة») حتى يغلق المدير البلاغات من /Admin/Reports.
    /// </summary>
    public class OpportunityReport
    {
        public const int HideThreshold = 2;
        public const int MaxDetails = 500;

        public int Id { get; set; }

        public int OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        public OpportunityReportReason Reason { get; set; }

        [StringLength(MaxDetails)]
        public string? Details { get; set; }

        /// <summary>العضو المُبلِّغ إن كان مسجَّلًا.</summary>
        [StringLength(450)]
        public string? UserId { get; set; }

        /// <summary>بريد اختياري للزائر إن أراد ردًا.</summary>
        [StringLength(256)]
        public string? Email { get; set; }

        /// <summary>هوية المُبلِّغ لمنع التكرار: "u:" + معرّف العضو، أو "g:" + بصمة IP والمتصفح للزائر.</summary>
        [Required, StringLength(64)]
        public string ReporterKey { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public OpportunityReportStatus Status { get; set; } = OpportunityReportStatus.Open;
        public DateTime? ResolvedAt { get; set; }

        [StringLength(450)]
        public string? ResolvedByUserId { get; set; }
    }
}
