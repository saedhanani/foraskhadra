using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>
    /// «تابع هذه المنظمة» (المرحلة 3.2): صف واحد لكل (عضو، منظمة). عند نشر المنظمة فرصة جديدة يصل المتابع إشعار
    /// OrganizationPublished (وإشعار دفع إن كان مفعَّلًا). المنظمة ترى عدد متابعيها في تقرير الوصول.
    /// </summary>
    public class OrganizationFollow
    {
        public int Id { get; set; }

        [Required, StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int OrganizationId { get; set; }
        [ForeignKey("OrganizationId")]
        public Organization? Organization { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
