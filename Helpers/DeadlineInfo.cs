using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>العدّ التنازلي لموعد فرصة بلغة الواجهة ("باقي 5 أيام"، "ينتهي غدًا"...) مع صنف CSS للونه
    /// (dl-today / dl-soon / dl-ok / dl-open / dl-expired في wwwroot/css/my-opps.css).</summary>
    public record DeadlineInfo(int? DaysLeft, bool IsExpired, string Label, string CssClass)
    {
        /// <summary>مفتوحة وينتهي موعدها خلال 7 أيام — نفس نافذة شارة IsDeadlineSoon وتذكير الموعد.</summary>
        public bool IsUrgent => DaysLeft is >= 0 and <= 7;

        public static DeadlineInfo For(Opportunity opportunity, string lang, DateTime? today = null)
        {
            string T(string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;

            if (opportunity.DeadlineType != DeadlineType.SpecificDate || !opportunity.EndDate.HasValue)
            {
                var label = opportunity.DeadlineType == DeadlineType.UntilFull
                    ? T("حتى اكتمال العدد", "Until full", "Jusqu'à complet")
                    : T("موعد مفتوح", "Open deadline", "Date ouverte");
                return new DeadlineInfo(null, false, label, "dl-open");
            }

            var days = (opportunity.EndDate.Value.Date - (today ?? DateTime.Now).Date).Days;
            return days switch
            {
                < 0 => new DeadlineInfo(days, true, T("انتهى الموعد", "Closed", "Clôturée"), "dl-expired"),
                0 => new DeadlineInfo(0, false, T("آخر يوم اليوم!", "Last day today!", "Dernier jour !"), "dl-today"),
                1 => new DeadlineInfo(1, false, T("ينتهي غدًا", "Ends tomorrow", "Se termine demain"), "dl-today"),
                2 => new DeadlineInfo(2, false, T("باقي يومان", "2 days left", "2 jours restants"), "dl-soon"),
                <= 7 => new DeadlineInfo(days, false, T($"باقي {days} أيام", $"{days} days left", $"{days} jours restants"), "dl-soon"),
                <= 10 => new DeadlineInfo(days, false, T($"باقي {days} أيام", $"{days} days left", $"{days} jours restants"), "dl-ok"),
                _ => new DeadlineInfo(days, false, T($"باقي {days} يومًا", $"{days} days left", $"{days} jours restants"), "dl-ok")
            };
        }
    }
}
