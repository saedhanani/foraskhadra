using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>بند في "رفيق التقديم" لفرصة محفوظة (البند 2-2): إمّا بند افتراضي بمفتاح يُترجم وقت العرض، أو بند كتبه المستخدم بنصه.</summary>
    public class ChecklistItem
    {
        public int Id { get; set; }

        public int SavedOpportunityId { get; set; }
        [ForeignKey("SavedOpportunityId")]
        public SavedOpportunity? SavedOpportunity { get; set; }

        /// <summary>مفتاح البند الافتراضي (انظر Helpers/ChecklistDefaults)، أو null للبند المخصص.</summary>
        [MaxLength(40)]
        public string? Key { get; set; }

        /// <summary>نص البند المخصص كما كتبه المستخدم، أو null للبند الافتراضي.</summary>
        [MaxLength(120)]
        public string? Text { get; set; }

        public bool Done { get; set; }
        public int SortOrder { get; set; }
    }
}
