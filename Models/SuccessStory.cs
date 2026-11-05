using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    public enum StoryStatus { Pending = 0, Approved = 1, Rejected = 2 }

    public enum StoryDisplayName
    {
        /// <summary>الاسم الأول فقط.</summary>
        FirstName = 0,
        FullName = 1,
        Anonymous = 2
    }

    /// <summary>
    /// قصة نجاح (المرحلة 4.2): يكتبها العضو من متابعته حين تصير حالة طلبه «مقبول»، بموافقة صريحة على النشر العام،
    /// ولا تظهر في /impact إلا بعد اعتماد المدير.
    /// </summary>
    public class SuccessStory
    {
        public const int MaxStory = 1500;

        public int Id { get; set; }

        [Required, StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int? OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public Opportunity? Opportunity { get; set; }

        /// <summary>اسم الفرصة/البرنامج كما كتبه العضو إن لم تكن من الموقع.</summary>
        [StringLength(200)]
        public string? ProgramName { get; set; }

        [Required, StringLength(MaxStory)]
        public string Story { get; set; } = string.Empty;

        /// <summary>نصيحة قصيرة للمتقدمين بعده (اختياري).</summary>
        [StringLength(300)]
        public string? Advice { get; set; }

        public StoryDisplayName DisplayName { get; set; }

        /// <summary>اسم العرض المجمَّد وقت الاعتماد (حتى لا يتغير إن غيّر العضو اسمه).</summary>
        [StringLength(120)]
        public string? ShownName { get; set; }

        [StringLength(2)] public string? CountryIso { get; set; }

        /// <summary>موافقة صريحة على النشر العام باسم العرض المختار — بدونها لا يُحفظ الطلب.</summary>
        public bool ConsentPublic { get; set; }

        [StringLength(2)] public string Lang { get; set; } = "ar";

        public StoryStatus Status { get; set; } = StoryStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
        [StringLength(450)] public string? ReviewedByUserId { get; set; }
        /// <summary>يظهر في الصفحة بترتيب تنازلي.</summary>
        public bool Featured { get; set; }
    }
}
