using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// السفراء الجامعيون (المرحلة 4.4): مهام شهرية ثابتة بنقاط، ومستويات شارات تراكمية، وعتبة الشهادة.
    /// «invite_3» تُعتمد آليًا من الإحالات المسجَّلة في الشهر؛ البقية يسلّمها السفير بإثبات ويعتمدها المدير.
    /// </summary>
    public static class AmbassadorTasks
    {
        public const string InviteKey = "invite_3";
        public const int InviteTarget = 3;

        public static readonly IReadOnlyList<AmbassadorTaskDef> All = new[]
        {
            new AmbassadorTaskDef(InviteKey, 15,
                "ادعُ 3 زملاء يسجّلون برمزك", "Invite 3 classmates who sign up with your code", "Invitez 3 camarades qui s'inscrivent avec votre code",
                "تُحتسب آليًا من رمز الدعوة في لوحتك.", "Counted automatically from the referral code on your dashboard.", "Comptée automatiquement via le code de parrainage de votre tableau de bord.", false),
            new AmbassadorTaskDef("share_5", 10,
                "شارك 5 فرص في مجموعات جامعتك", "Share 5 opportunities in your university groups", "Partagez 5 opportunités dans les groupes de votre université",
                "اكتب روابط الفرص أو اسم المجموعة في الملاحظة.", "List the opportunity links or the group name in the note.", "Indiquez les liens ou le nom du groupe dans la note.", false),
            new AmbassadorTaskDef("campus_post", 20,
                "منشور أو ملصق في الحرم الجامعي", "A post or poster on campus", "Une publication ou une affiche sur le campus",
                "رابط المنشور أو صورة الملصق (رابط).", "Link to the post or a photo of the poster (URL).", "Lien de la publication ou photo de l'affiche (URL).", true),
            new AmbassadorTaskDef("session", 40,
                "جلسة تعريفية (حضورية أو عبر الإنترنت)", "An info session (in person or online)", "Une séance d'information (en personne ou en ligne)",
                "رابط التسجيل أو صورة من الجلسة وعدد الحضور.", "Registration link or a photo from the session and the attendance.", "Lien d'inscription ou photo de la séance et nombre de participants.", true),
            new AmbassadorTaskDef("org_intro", 25,
                "عرّفنا بمنظمة أو مكتب جامعي لينشر فرصه", "Introduce an organization or university office that will publish", "Présentez-nous une organisation ou un bureau universitaire qui publiera",
                "اسم الجهة وبريد التواصل، أو طلب النشر الذي أرسلوه.", "The organization's name and contact email, or the publish request they sent.", "Nom de l'organisation et e-mail de contact, ou la demande de publication envoyée.", false),
            new AmbassadorTaskDef("story", 15,
                "اجمع قصة نجاح من طالب قُبل", "Collect a success story from an accepted student", "Recueillez une histoire de réussite d'un étudiant admis",
                "اطلب منه إرسالها من صفحة «قصتي» أو اكتب اسمه وبريده.", "Ask them to submit it from the “My story” page, or note their name and email.", "Demandez-leur de l'envoyer depuis « Mon histoire », ou notez nom et e-mail.", false),
        };

        public static AmbassadorTaskDef? Find(string? key) => All.FirstOrDefault(t => t.Key == key);

        /// <summary>مستويات الشارة بالنقاط التراكمية المعتمدة.</summary>
        public static readonly (string Key, int MinPoints, string Ar, string En, string Fr)[] Levels =
        {
            ("gold", 150, "سفير ذهبي", "Gold ambassador", "Ambassadeur or"),
            ("silver", 60, "سفير فضي", "Silver ambassador", "Ambassadeur argent"),
            ("bronze", 20, "سفير برونزي", "Bronze ambassador", "Ambassadeur bronze"),
        };

        public const int CertificateMinPoints = 20;

        public static (string Key, string Title)? LevelFor(int points, string lang)
        {
            foreach (var l in Levels)
                if (points >= l.MinPoints) return (l.Key, lang == "en" ? l.En : lang == "fr" ? l.Fr : l.Ar);
            return null;
        }

        /// <summary>النقاط المتبقية للمستوى التالي (null عند الذهبي).</summary>
        public static (int Needed, string Title)? NextLevel(int points, string lang)
        {
            foreach (var l in Levels.Reverse())
                if (points < l.MinPoints) return (l.MinPoints - points, lang == "en" ? l.En : lang == "fr" ? l.Fr : l.Ar);
            return null;
        }
    }
}
