using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Foras_Khadra.Models
{
    /// <summary>رقم بارز في صفحة المشروع: "500+" مع تسميته بالثلاث لغات.</summary>
    public class ProgramStat
    {
        public string Value { get; set; } = string.Empty;
        public string Ar { get; set; } = string.Empty;
        public string En { get; set; } = string.Empty;
        public string Fr { get; set; } = string.Empty;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Value) && string.IsNullOrWhiteSpace(Ar) && string.IsNullOrWhiteSpace(En) && string.IsNullOrWhiteSpace(Fr);
    }

    /// <summary>محطة في مسار المشروع: سنة (أو نسخة) ووصف قصير بالثلاث لغات.</summary>
    public class ProgramMilestone
    {
        public string Year { get; set; } = string.Empty;
        public string Ar { get; set; } = string.Empty;
        public string En { get; set; } = string.Empty;
        public string Fr { get; set; } = string.Empty;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Year) && string.IsNullOrWhiteSpace(Ar) && string.IsNullOrWhiteSpace(En) && string.IsNullOrWhiteSpace(Fr);
    }

    /// <summary>
    /// مشروع رائد (RCOY، قمة صنّاع المحتوى، برنامج بناء القدرات...): له صفحة خاصة /Programs/{slug}
    /// وبند في قائمة "نشاطاتنا" وبطاقة بارزة في أعلى صفحة النشاطات. النشاطات تُربط به عبر Activity.ProgramId.
    /// </summary>
    public class FlagshipProgram
    {
        public int Id { get; set; }

        /// <summary>مقطع الرابط: rcoy-mena، climate-voices...</summary>
        [Required] public string Slug { get; set; } = string.Empty;

        /// <summary>ترتيب الظهور في القائمة والصفحة (1 = الأول).</summary>
        public int SortOrder { get; set; } = 1;
        public bool IsPublished { get; set; } = true;

        /// <summary>الاسم القصير في القائمة المنسدلة.</summary>
        [Required] public string MenuTitleAr { get; set; } = string.Empty;
        [Required] public string MenuTitleEn { get; set; } = string.Empty;
        [Required] public string MenuTitleFr { get; set; } = string.Empty;

        [Required] public string TitleAr { get; set; } = string.Empty;
        [Required] public string TitleEn { get; set; } = string.Empty;
        [Required] public string TitleFr { get; set; } = string.Empty;

        /// <summary>سطر المكان والتاريخ والشركاء تحت العنوان.</summary>
        public string? SubtitleAr { get; set; }
        public string? SubtitleEn { get; set; }
        public string? SubtitleFr { get; set; }

        /// <summary>الرقم الأبرز ("500+") وتسميته.</summary>
        public string? HeadlineValue { get; set; }
        public string? HeadlineLabelAr { get; set; }
        public string? HeadlineLabelEn { get; set; }
        public string? HeadlineLabelFr { get; set; }

        /// <summary>التعريف بالمشروع (HTML من المحرر، يُعقَّم).</summary>
        [Required] public string IntroAr { get; set; } = string.Empty;
        [Required] public string IntroEn { get; set; } = string.Empty;
        [Required] public string IntroFr { get; set; } = string.Empty;

        /// <summary>عنوان قسم المخرجات ("مخرجات القمة"، "ما بعد التدريب"...) ومحتواه (HTML).</summary>
        public string? OutcomesHeadingAr { get; set; }
        public string? OutcomesHeadingEn { get; set; }
        public string? OutcomesHeadingFr { get; set; }
        public string? OutcomesAr { get; set; }
        public string? OutcomesEn { get; set; }
        public string? OutcomesFr { get; set; }

        /// <summary>الشركاء والممولون (سطر نصي).</summary>
        public string? PartnersAr { get; set; }
        public string? PartnersEn { get; set; }
        public string? PartnersFr { get; set; }

        public string? HeroImagePath { get; set; }
        public string? ExternalLink { get; set; }

        /// <summary>باب التقديم مفتوح الآن — تفعيله يُنبّه كل من سجّل اهتمامه (البند 5-5).</summary>
        public bool ApplicationsOpen { get; set; }

        public string StatsJson { get; set; } = "[]";
        public string TimelineJson { get; set; } = "[]";
        public string GalleryJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ===== القوائم المخزّنة كـ JSON: تُقرأ مرة وتُعاد كتابتها عند الضبط =====
        private List<ProgramStat>? _stats;
        private List<ProgramMilestone>? _timeline;
        private List<string>? _gallery;

        [NotMapped]
        public List<ProgramStat> Stats
        {
            get => _stats ??= Read<ProgramStat>(StatsJson);
            set { _stats = value; StatsJson = Write(value); }
        }

        [NotMapped]
        public List<ProgramMilestone> Timeline
        {
            get => _timeline ??= Read<ProgramMilestone>(TimelineJson);
            set { _timeline = value; TimelineJson = Write(value); }
        }

        [NotMapped]
        public List<string> Gallery
        {
            get => _gallery ??= Read<string>(GalleryJson);
            set { _gallery = value; GalleryJson = Write(value); }
        }

        private static List<T> Read<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<T>();
            try { return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>(); }
            catch (JsonException) { return new List<T>(); }
        }

        private static string Write<T>(List<T>? list) => JsonSerializer.Serialize(list ?? new List<T>());
    }
}
