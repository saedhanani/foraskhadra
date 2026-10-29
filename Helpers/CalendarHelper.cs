using System.Text;

namespace Foras_Khadra.Helpers
{
    /// <summary>يولّد ملف iCalendar ليوم الموعد النهائي (البند 2-4 "ذكّرني بالموعد") بتنبيه قبله بثلاثة أيام.</summary>
    public static class CalendarHelper
    {
        public static string BuildDeadlineEvent(int opportunityId, string title, DateTime deadline, string url, DateTime? now = null)
        {
            var stamp = (now ?? DateTime.UtcNow).ToString("yyyyMMdd'T'HHmmss'Z'");
            var day = deadline.Date;

            var sb = new StringBuilder();
            sb.Append("BEGIN:VCALENDAR\r\n");
            sb.Append("VERSION:2.0\r\n");
            sb.Append("PRODID:-//Foras Khadra//Opportunity Deadline//AR\r\n");
            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append(Fold($"UID:opportunity-{opportunityId}@foraskhadra.com"));
            sb.Append($"DTSTAMP:{stamp}\r\n");
            sb.Append($"DTSTART;VALUE=DATE:{day:yyyyMMdd}\r\n");
            sb.Append($"DTEND;VALUE=DATE:{day.AddDays(1):yyyyMMdd}\r\n");
            sb.Append(Fold("SUMMARY:" + Escape(title)));
            sb.Append(Fold("DESCRIPTION:" + Escape(url)));
            sb.Append(Fold("URL:" + Escape(url)));
            sb.Append("BEGIN:VALARM\r\n");
            sb.Append("TRIGGER:-P3D\r\n");
            sb.Append("ACTION:DISPLAY\r\n");
            sb.Append(Fold("DESCRIPTION:" + Escape(title)));
            sb.Append("END:VALARM\r\n");
            sb.Append("END:VEVENT\r\n");
            sb.Append("END:VCALENDAR\r\n");
            return sb.ToString();
        }

        /// <summary>هروب النص حسب RFC 5545: الفاصلة والفاصلة المنقوطة والشرطة المائلة وسطر جديد.</summary>
        public static string Escape(string text) =>
            text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\\n").Replace("\n", "\\n");

        /// <summary>طيّ السطور الأطول من 75 بايتًا (المسافة في بداية السطر التالي هي الاستمرار).</summary>
        public static string Fold(string line)
        {
            var bytes = Encoding.UTF8.GetBytes(line);
            if (bytes.Length <= 75) return line + "\r\n";

            var sb = new StringBuilder();
            var current = new StringBuilder();
            var currentBytes = 0;
            var limit = 75;
            foreach (var ch in line)
            {
                var chBytes = Encoding.UTF8.GetByteCount(ch.ToString());
                if (currentBytes + chBytes > limit)
                {
                    sb.Append(current).Append("\r\n ");
                    current.Clear();
                    currentBytes = 0;
                    limit = 74; // السطر التالي يبدأ بمسافة
                }
                current.Append(ch);
                currentBytes += chBytes;
            }
            sb.Append(current).Append("\r\n");
            return sb.ToString();
        }
    }
}
