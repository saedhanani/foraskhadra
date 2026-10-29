using System.Collections.Generic;

namespace Foras_Khadra.Models
{
    /// <summary>بيانات صفحة "صفحتي" للمستخدم الفرد: المقترحة حسب الاهتمامات + المحفوظة + أحدث الفرص.</summary>
    public class UserDashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;

        /// <summary>أسماء اهتمامات المستخدم بلغة الواجهة (فارغة إن لم يختر اهتمامات).</summary>
        public List<string> InterestNames { get; set; } = new();

        /// <summary>فرص محفوظة ينتهي موعدها خلال 7 أيام — تُعرض في شريط تنبيه أعلى الصفحة.</summary>
        public List<Opportunity> ExpiringSaved { get; set; } = new();

        /// <summary>فرص مفتوحة تطابق اهتمامات المستخدم (بلا المحفوظة منها).</summary>
        public List<Opportunity> Recommended { get; set; } = new();

        /// <summary>أحدث المحفوظات (حتى 6؛ العدد الكامل في SavedCount).</summary>
        public List<Opportunity> Saved { get; set; } = new();
        public int SavedCount { get; set; }

        /// <summary>أحدث الفرص المنشورة بعد استبعاد المقترحة والمحفوظة.</summary>
        public List<Opportunity> Latest { get; set; } = new();

        /// <summary>معرّفات كل محفوظات المستخدم — لحالة زر الحفظ على البطاقات.</summary>
        public HashSet<int> SavedIds { get; set; } = new();

        /// <summary>بحوث المستخدم المحفوظة — تُدار من "صفحتي" ويفحصها ماسح الإشعارات.</summary>
        public List<SavedSearch> SavedSearches { get; set; } = new();
    }
}
