using System.ComponentModel.DataAnnotations;
using Foras_Khadra.Services;

namespace Foras_Khadra.Models
{
    /// <summary>صفحة الأسئلة الثلاث (/compass): للزائر، وللعضو بلا ملف، ولتعديل الملف.</summary>
    public class CompassIndexVM
    {
        public CompassAnswers A { get; set; } = new();
        public List<CountryOption> ArabCountries { get; set; } = new();
        public List<CountryOption> HostOptions { get; set; } = new();
        public List<Field> Fields { get; set; } = new();
        public bool IsMember { get; set; }
        public bool IsEdit { get; set; }
    }

    /// <summary>نتيجة الزائر (/compass/result): العدد، ثلاث بطاقات، والباقي خلف بوابة التسجيل.</summary>
    public class CompassResultVM
    {
        public CompassResult Result { get; set; } = new();
        public CompassAnswers Answers { get; set; } = new();
        public const int VisibleCards = 3;
    }

    /// <summary>«بوصلتي» للعضو (/compass/me).</summary>
    public class CompassMeVM
    {
        public CompassResult Result { get; set; } = new();
        public CompassAnswers Answers { get; set; } = new();
        public string FirstName { get; set; } = string.Empty;
        public HashSet<int> SavedIds { get; set; } = new();
        public string ReferralCode { get; set; } = string.Empty;
        public string ShareUrl { get; set; } = string.Empty;
    }

    /// <summary>تسجيل مختصر من البوصلة: بريد وكلمة مرور فقط؛ الاسم لاحقًا.</summary>
    public class CompassRegisterVM
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
