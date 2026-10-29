namespace Foras_Khadra.Models
{
    /// <summary>حالة تقديم المستخدم على فرصة محفوظة (البند 2-1 من خطة العودة). يبدّلها المستخدم بنفسه.</summary>
    public enum ApplicationStatus
    {
        NotApplied = 0,
        Applied = 1,
        Pending = 2,
        Accepted = 3,
        Rejected = 4
    }

    public static class ApplicationStatusExtensions
    {
        public static string Label(this ApplicationStatus status, string lang) => status switch
        {
            ApplicationStatus.Applied => lang == "en" ? "Applied ✓" : lang == "fr" ? "Candidature envoyée ✓" : "قدّمت ✓",
            ApplicationStatus.Pending => lang == "en" ? "Awaiting reply" : lang == "fr" ? "En attente" : "قيد الانتظار",
            ApplicationStatus.Accepted => lang == "en" ? "Accepted 🎉" : lang == "fr" ? "Acceptée 🎉" : "قُبلت 🎉",
            ApplicationStatus.Rejected => lang == "en" ? "Rejected" : lang == "fr" ? "Refusée" : "رُفضت",
            _ => lang == "en" ? "Not applied yet" : lang == "fr" ? "Pas encore postulé" : "لم أقدّم بعد"
        };

        /// <summary>قدّم فعلًا (أي حالة بعد "لم أقدّم بعد") — يُكمل شريط رفيق التقديم تلقائيًا.</summary>
        public static bool HasApplied(this ApplicationStatus status) => status != ApplicationStatus.NotApplied;
    }
}
