using System.ComponentModel.DataAnnotations;

namespace Foras_Khadra.Models
{
    /// <summary>مجال الدراسة/العمل (قائمة ثابتة ~25 مجالًا مبنية على ISCED-F مبسَّط). علاقة كثير-إلى-كثير مع الفرص.</summary>
    public class Field
    {
        public int Id { get; set; }

        /// <summary>المفتاح الثابت الذي تستعمله منصة الكتابة: environment, engineering, ... , any</summary>
        [Required, StringLength(40)]
        public string Key { get; set; } = string.Empty;

        [Required] public string NameAr { get; set; } = string.Empty;
        [Required] public string NameEn { get; set; } = string.Empty;
        [Required] public string NameFr { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

        /// <summary>مفتاح المجال "كل المجالات".</summary>
        public const string AnyKey = "any";
    }
}
