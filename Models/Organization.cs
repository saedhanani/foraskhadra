using Foras_Khadra.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>
    /// درجة توثيق المنظمة المعروضة للزوار (المرحلة 3.1): None = غير موثَّقة، Registered = حساب مؤكد،
    /// Verified = وثّقها المدير (تنشر بلا مراجعة)، Partner = شريك. IsVerified يتبع الدرجة (≥ Verified).
    /// </summary>
    public enum OrganizationTier
    {
        None = 0,
        Registered = 1,
        Verified = 2,
        Partner = 3
    }

    public class Organization
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "ادخل اسم المنظمة")]
        public string Name { get; set; }

        [Required(ErrorMessage = "ادخل القطاع")]
        public string Sector { get; set; }

        [Required(ErrorMessage = "اختر الدولة")]
        public string Country { get; set; }


        [Required(ErrorMessage = "ادخل اسم جهة الاتصال")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "ادخل البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
        public string ContactEmail { get; set; }

        [HttpUrl]
        public string? Website { get; set; } // ← اختياري

        [Required(ErrorMessage = "ادخل رقم الهاتف")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 8 أحرف")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
    ErrorMessage = "كلمة المرور يجب أن تحتوي على حرف كبير، حرف صغير، رقم ورمز خاص")]
        public string? Password { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Location { get; internal set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? TokenExpiry { get; set; }

        [ForeignKey("UserId")]
        public string? UserId { get; set; }  //   nullable مؤقتا
        public ApplicationUser? User { get; set; }

        public string? LogoPath { get; set; }
        public bool IsManual { get; set; } = true;

        /// <summary>موثَّقة: تنشر فرصها مباشرة بلا مراجعة (Admin/Review «اعتماد وتوثيق»).</summary>
        public bool IsVerified { get; set; }
        /// <summary>موقوفة: كل فرصها المنشورة تُخفى (Suspended) حتى يُرفع الإيقاف.</summary>
        public bool IsSuspended { get; set; }

        /// <summary>درجة التوثيق المعروضة (شارة «منظمة موثَّقة» / «شريك») — يضبطها المدير؛ تُرفع تلقائيًا مع «اعتماد وتوثيق».</summary>
        public OrganizationTier VerificationTier { get; set; }

        [NotMapped]
        public bool ShowsVerifiedBadge => VerificationTier >= OrganizationTier.Verified && !IsSuspended;
    }
}
