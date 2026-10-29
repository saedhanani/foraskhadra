using Foras_Khadra.Services;

namespace Foras_Khadra.Helpers
{
    /// <summary>شارة إنجاز (البند 4-2): تُحسب من الأرقام الموجودة ولا تُخزَّن.</summary>
    public record Achievement(string Key, string Icon, bool Earned, int Progress, int Target)
    {
        public int Percent => Target == 0 ? 100 : Math.Min(100, Progress * 100 / Target);
    }

    public static class Achievements
    {
        /// <summary>الشارات بترتيب العرض: من الأسهل إلى الأصعب، فيرى المستخدم دائمًا الشارة القادمة في متناوله.</summary>
        public static List<Achievement> Compute(TrackerStats stats, int profilePercent) => new()
        {
            new("first_save", "bi-bookmark-heart", stats.Saved >= 1, stats.Saved, 1),
            new("profile_complete", "bi-person-check", profilePercent >= 100, profilePercent, 100),
            new("first_apply", "bi-send-check", stats.Applied >= 1, stats.Applied, 1),
            new("active_seeker", "bi-compass", stats.Saved >= 5, stats.Saved, 5),
            new("persistent", "bi-fire", stats.Applied >= 5, stats.Applied, 5),
            new("accepted", "bi-trophy", stats.Accepted >= 1, stats.Accepted, 1)
        };

        public static string Title(string key, string lang) => key switch
        {
            "first_save" => lang == "en" ? "First save" : lang == "fr" ? "Premier favori" : "أول فرصة محفوظة",
            "profile_complete" => lang == "en" ? "Complete profile" : lang == "fr" ? "Profil complet" : "ملف مكتمل",
            "first_apply" => lang == "en" ? "First application" : lang == "fr" ? "Première candidature" : "أول تقديم",
            "active_seeker" => lang == "en" ? "Active seeker" : lang == "fr" ? "Chercheur actif" : "باحث نشط",
            "persistent" => lang == "en" ? "Persistent" : lang == "fr" ? "Persévérant" : "مثابر",
            "accepted" => lang == "en" ? "Accepted!" : lang == "fr" ? "Accepté !" : "قُبلت!",
            _ => key
        };

        /// <summary>كيف تُكسب الشارة — يظهر على الشارات غير المكتسبة.</summary>
        public static string Hint(string key, string lang) => key switch
        {
            "first_save" => lang == "en" ? "Save any opportunity" : lang == "fr" ? "Enregistrez une opportunité" : "احفظ أي فرصة",
            "profile_complete" => lang == "en" ? "Fill in your profile 100%" : lang == "fr" ? "Complétez votre profil à 100 %" : "أكمل ملفك 100%",
            "first_apply" => lang == "en" ? "Mark a saved opportunity as applied" : lang == "fr" ? "Marquez une opportunité comme postulée" : "علّم فرصة محفوظة بـ«قدّمت»",
            "active_seeker" => lang == "en" ? "Save 5 opportunities" : lang == "fr" ? "Enregistrez 5 opportunités" : "احفظ 5 فرص",
            "persistent" => lang == "en" ? "Apply to 5 opportunities" : lang == "fr" ? "Postulez à 5 opportunités" : "قدّم على 5 فرص",
            "accepted" => lang == "en" ? "Mark an application as accepted" : lang == "fr" ? "Marquez une candidature comme acceptée" : "علّم تقديمًا بـ«قُبلت»",
            _ => key
        };
    }
}
