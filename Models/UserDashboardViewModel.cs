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

        // ===== المرحلة 2: تتبّع التقديم =====
        /// <summary>حالة التقديم وتقدّم رفيق التقديم لكل فرصة محفوظة معروضة (المفتاح OpportunityId).</summary>
        public Dictionary<int, Foras_Khadra.Services.TrackerCard> Tracker { get; set; } = new();
        public int AppliedCount { get; set; }
        public int AcceptedCount { get; set; }

        /// <summary>آخر فرص فتحها المستخدم ("تابعت مؤخرًا").</summary>
        public List<Opportunity> Recent { get; set; } = new();

        // ===== المرحلة 3: ذكاء الترشيح =====
        /// <summary>نسبة التطابق لكل فرصة معروضة (المفتاح OpportunityId) — تقرؤها بطاقة _OpportunityCard عبر ViewData.</summary>
        public Dictionary<int, int> MatchScores { get; set; } = new();
        /// <summary>هل عرفنا جنسية/بلد المستخدم فرشّحنا على أساسها؟</summary>
        public bool MatchesNationality { get; set; }

        // ===== المرحلة 4: الموقع يعرفك =====
        /// <summary>بداية الزيارة السابقة — الفرص المنشورة بعدها تحمل شارة "جديد" (null في أول زيارة).</summary>
        public DateTime? NewSince { get; set; }
        public int NewSinceCount { get; set; }
        public Foras_Khadra.Helpers.ProfileCompletionResult Profile { get; set; } = new(0, Array.Empty<string>());
        public List<Foras_Khadra.Helpers.Achievement> Achievements { get; set; } = new();
        /// <summary>كم مستخدمًا حفظ كل فرصة معروضة (المفتاح OpportunityId).</summary>
        public Dictionary<int, int> SaveCounts { get; set; } = new();

        // ===== المرحلة 5: العادة والمجتمع =====
        public string ReferralCode { get; set; } = string.Empty;
        public int ReferralCount { get; set; }
        /// <summary>فرصة قُبل فيها المستخدم — لعرض زر "شارك إنجازك" (5-2).</summary>
        public Opportunity? LatestAccepted { get; set; }
    }
}
