using System.ComponentModel.DataAnnotations;
using Foras_Khadra.Services;

namespace Foras_Khadra.Models
{
    /// <summary>نموذج طلب النشر في /for-organizations. رسائل التحقق بثلاث لغات في PartnersController لا هنا.</summary>
    public class PublishRequestForm
    {
        [StringLength(200)] public string? OrganizationName { get; set; }
        [StringLength(120)] public string? ContactName { get; set; }
        [StringLength(256)] public string? ContactEmail { get; set; }
        [StringLength(500)] public string? Website { get; set; }
        [StringLength(1000)] public string? SourceUrl { get; set; }
        [StringLength(2000)] public string? Notes { get; set; }
        /// <summary>اسم عضو OpportunityType أو فارغ.</summary>
        public string? Type { get; set; }
        /// <summary>ISO-2 أو XA أو XW أو فارغ.</summary>
        public string? Country { get; set; }
        /// <summary>فخ الروبوتات: حقل مخفي يجب أن يبقى فارغًا.</summary>
        public string? Fax { get; set; }
    }

    public sealed record CountryOption(string Iso, string Name);

    public class PartnersPageVM
    {
        public PublishRequestForm Form { get; set; } = new();
        public List<CountryOption> Countries { get; set; } = new();
        public SiteNumbers Numbers { get; set; } = new(0, 0, 0);
        /// <summary>التقدير الأولي المعروض قبل أي اختيار (كل الأعضاء).</summary>
        public int Estimate { get; set; }
    }
}
