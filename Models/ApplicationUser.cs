using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    public enum UserRole
    {
        Guest,
        User,
        Organization,
        Admin
    }

    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Language { get; set; } = "en";
        public DateTime CreatedAt { get; set; }

        // فقط للمنظمات
        public string? ContactEmail { get; set; }
        public string? ContactPersonName { get; set; }
        public string? OrganizationWebsite { get; set; }
        public string? OrganizationName { get; set; }
        public string? Sector { get; set; }
        public string? Country { get; set; }
        public string? Nationality { get; set; }

        // ===== الاهتمامات للفرد =====
        public string? InterestsJson { get; set; }

        /// <summary>آخر وقت وُلِّد له إشعار "فرص جديدة في اهتماماتك" — يضبطه NotificationSweeper.</summary>
        public DateTime? LastInterestNotifiedAt { get; set; }

        // ===== "جديد منذ آخر زيارة" (البند 4-1) =====
        /// <summary>آخر فتح لـ"صفحتي"؛ يُحدَّث عند كل فتح.</summary>
        public DateTime? LastSeenAt { get; set; }
        /// <summary>بداية الزيارة السابقة (قبل فجوة تزيد على 30 دقيقة) — الفرص المنشورة بعدها "جديدة".</summary>
        public DateTime? PreviousVisitAt { get; set; }

        // ===== سفراء فرص خضراء (البند 5-3) =====
        /// <summary>رمز الإحالة الشخصي (يُولَّد عند أول فتح لصفحتي)؛ رابط الدعوة = /Account/RegisterUser?ref=CODE.</summary>
        [MaxLength(12)]
        public string? ReferralCode { get; set; }
        /// <summary>من دعاه إلى الموقع (بلا قيد مرجعي حتى لا يتعقّد حذف المستخدمين).</summary>
        [MaxLength(450)]
        public string? ReferredByUserId { get; set; }
        public DateTime? ReferredAt { get; set; }

        [NotMapped]
        public List<string> Interests
        {
            get => string.IsNullOrEmpty(InterestsJson) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(InterestsJson)!;
            set => InterestsJson = JsonSerializer.Serialize(value);
        }
    }
}