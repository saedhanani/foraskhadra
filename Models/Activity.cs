using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>نشاط من نشاطات فرص خضراء (ورشة، ندوة، حملة...) يُعرض في صفحة "نشاطاتنا".</summary>
    public class Activity
    {
        public int Id { get; set; }

        [Required] public string TitleAr { get; set; } = string.Empty;
        [Required] public string TitleEn { get; set; } = string.Empty;
        [Required] public string TitleFr { get; set; } = string.Empty;

        /// <summary>HTML من المحرر، يُعقَّم عند الحفظ وعند العرض.</summary>
        [Required] public string DescriptionAr { get; set; } = string.Empty;
        [Required] public string DescriptionEn { get; set; } = string.Empty;
        [Required] public string DescriptionFr { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        /// <summary>اختياري لنشاط يمتد أيامًا.</summary>
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        /// <summary>المكان أو "عن بُعد".</summary>
        public string? Location { get; set; }

        /// <summary>رابط التسجيل أو الألبوم أو التغطية (اختياري).</summary>
        public string? ExternalLink { get; set; }

        public string? ImagePath { get; set; }

        /// <summary>غير المنشور يراه الأدمن فقط.</summary>
        public bool IsPublished { get; set; } = true;

        /// <summary>المشروع الرائد الذي يتبعه النشاط (اختياري) — يظهر في صفحة المشروع وكوسم على البطاقة.</summary>
        public int? ProgramId { get; set; }
        [ForeignKey("ProgramId")]
        public FlagshipProgram? Program { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>قادم ما لم ينقضِ آخر يوم فيه.</summary>
        [NotMapped]
        public bool IsUpcoming => (EndDate ?? StartDate).Date >= DateTime.Today;
    }
}
