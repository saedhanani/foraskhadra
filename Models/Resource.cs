using System.ComponentModel.DataAnnotations;

namespace Foras_Khadra.Models
{
    public enum ResourceCategory
    {
        CvTemplate = 1,
        MotivationLetter = 2,
        Guide = 3,
        Other = 4
    }

    /// <summary>مورد في "خزانة الأعضاء" (البند 5-4): ملف PDF مرفوع أو رابط خارجي، للمسجّلين فقط. يديره الأدمن.</summary>
    public class Resource
    {
        public int Id { get; set; }

        [Required] public string TitleAr { get; set; } = string.Empty;
        [Required] public string TitleEn { get; set; } = string.Empty;
        [Required] public string TitleFr { get; set; } = string.Empty;

        public string? DescriptionAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionFr { get; set; }

        public ResourceCategory Category { get; set; } = ResourceCategory.Guide;

        /// <summary>مسار ملف PDF تحت /uploads/resources، أو null إن كان المورد رابطًا.</summary>
        public string? FilePath { get; set; }
        /// <summary>رابط خارجي (مثل Google Drive)، أو null إن كان ملفًا.</summary>
        [MaxLength(500)]
        public string? ExternalUrl { get; set; }

        public int SortOrder { get; set; } = 1;
        public bool IsPublished { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public static class ResourceCategoryExtensions
    {
        public static string Label(this ResourceCategory c, string lang) => c switch
        {
            ResourceCategory.CvTemplate => lang == "en" ? "CV templates" : lang == "fr" ? "Modèles de CV" : "قوالب سيرة ذاتية",
            ResourceCategory.MotivationLetter => lang == "en" ? "Motivation letters" : lang == "fr" ? "Lettres de motivation" : "خطابات الدوافع",
            ResourceCategory.Guide => lang == "en" ? "Guides" : lang == "fr" ? "Guides" : "أدلة وإرشادات",
            _ => lang == "en" ? "Other" : lang == "fr" ? "Autres" : "أخرى"
        };

        public static string Icon(this ResourceCategory c) => c switch
        {
            ResourceCategory.CvTemplate => "bi-file-earmark-person",
            ResourceCategory.MotivationLetter => "bi-envelope-paper",
            ResourceCategory.Guide => "bi-signpost-2",
            _ => "bi-folder2-open"
        };
    }
}
