using Foras_Khadra.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Foras_Khadra.Models
{
    public class OpportunityEditVM
    {
        public int Id { get; set; }

        [Required]
        public string? TitleAr { get; set; }
        [Required]
        public string? TitleEn { get; set; }
        [Required]
        public string? TitleFr { get; set; }

        public IFormFile? Image { get; set; } // اختياري في التعديل
        public string? ImagePath { get; set; } // موجود مسبقاً

        public string? PublishedBy { get; set; }
        public OpportunityType? Type { get; set; }

        [Required]
        public string? DescriptionAr { get; set; }
        [Required]
        public string? DescriptionEn { get; set; }
        [Required]
        public string? DescriptionFr { get; set; }

        [Required]
        public string? DetailsAr { get; set; }
        [Required]
        public string? DetailsEn { get; set; }
        [Required]
        public string? DetailsFr { get; set; }

        public List<int>? AvailableCountryIds { get; set; } = new List<int>();
        [Required]
        public string? EligibilityCriteriaAr { get; set; }
        [Required]
        public string? EligibilityCriteriaEn { get; set; }
        [Required]
        public string? EligibilityCriteriaFr { get; set; }

        [Required]
        public string? BenefitsAr { get; set; }
        [Required]
        public string? BenefitsEn { get; set; }
        [Required]
        public string? BenefitsFr { get; set; }

        [Required]
        [HttpUrl]
        public string? ApplyLink { get; set; }


        public DeadlineType DeadlineType { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public IEnumerable<SelectListItem>? CountriesSelectList { get; set; } = new List<SelectListItem>();

        /// <summary>حقول الأهلية المهيكلة (Views/Shared/_FacetFields.cshtml بالبادئة Facets).</summary>
        public FacetFormVM Facets { get; set; } = new();

        /// <summary>مسودة من منصة الفريق: تظهر في الصفحة أزرار «حفظ كمسودة» و«حفظ ونشر».</summary>
        public bool IsDraft { get; set; }

        /// <summary>يأتي من زر «حفظ ونشر»: يحوّل المسودة إلى منشورة بعد الحفظ.</summary>
        public bool Publish { get; set; }
    }
}