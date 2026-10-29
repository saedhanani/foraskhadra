using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    /// <summary>"سجّل اهتمامك" ببرنامج رائد (البند 5-5): يُنبَّه المستخدم عندما يفعّل الأدمن "باب التقديم مفتوح". (UserId, ProgramId) فريد.</summary>
    public class ProgramInterest
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int ProgramId { get; set; }
        [ForeignKey("ProgramId")]
        public FlagshipProgram? Program { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>وقت تنبيه الفتح؛ يُصفَّر عندما يُغلق التقديم ثم يُفتح مجددًا (دورة جديدة).</summary>
        public DateTime? NotifiedAt { get; set; }
    }
}
