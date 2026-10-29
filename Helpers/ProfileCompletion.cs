using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>اكتمال الملف الشخصي للفرد (البند 4-4): خمسة عناصر متساوية الوزن، والناقص منها يُعرض كدعوة في "صفحتي".</summary>
    public record ProfileCompletionResult(int Percent, IReadOnlyList<string> MissingKeys)
    {
        public bool IsComplete => Percent >= 100;
        /// <summary>أول عنصر ناقص — هو ما ندعو المستخدم لإكماله الآن.</summary>
        public string? NextKey => MissingKeys.FirstOrDefault();
    }

    public static class ProfileCompletion
    {
        /// <summary>ترتيب العرض = ترتيب الأهمية للترشيح: الجنسية أولًا لأنها تفتح الترشيح بالدول.</summary>
        private static readonly (string key, Func<ApplicationUser, bool> has)[] Items =
        {
            ("nationality", u => !string.IsNullOrWhiteSpace(u.Nationality)),
            ("interests", u => u.Interests.Count > 0),
            ("country", u => !string.IsNullOrWhiteSpace(u.Country)),
            ("first_name", u => !string.IsNullOrWhiteSpace(u.FirstName)),
            ("last_name", u => !string.IsNullOrWhiteSpace(u.LastName))
        };

        public static ProfileCompletionResult Compute(ApplicationUser user)
        {
            var missing = Items.Where(i => !i.has(user)).Select(i => i.key).ToList();
            var percent = (Items.Length - missing.Count) * 100 / Items.Length;
            return new ProfileCompletionResult(percent, missing);
        }

        /// <summary>دعوة الإكمال بلغة الواجهة — تشرح الفائدة لا الحقل فقط.</summary>
        public static string Prompt(string key, string lang) => key switch
        {
            "nationality" => lang == "en" ? "Add your nationality so we only suggest opportunities that accept it"
                : lang == "fr" ? "Ajoutez votre nationalité pour ne voir que les opportunités qui l'acceptent"
                : "أضف جنسيتك لنرشّح لك فرصًا تقبلها",
            "interests" => lang == "en" ? "Pick your interests to get personal picks and alerts"
                : lang == "fr" ? "Choisissez vos intérêts pour des suggestions et alertes personnalisées"
                : "اختر اهتماماتك لتصلك ترشيحات وتنبيهات تخصك",
            "country" => lang == "en" ? "Add your country of residence for local opportunities"
                : lang == "fr" ? "Ajoutez votre pays de résidence pour les opportunités locales"
                : "أضف بلد إقامتك لنعرض لك الفرص المحلية",
            "first_name" => lang == "en" ? "Add your first name" : lang == "fr" ? "Ajoutez votre prénom" : "أضف اسمك الأول",
            "last_name" => lang == "en" ? "Add your last name" : lang == "fr" ? "Ajoutez votre nom" : "أضف اسم عائلتك",
            _ => key
        };
    }
}
