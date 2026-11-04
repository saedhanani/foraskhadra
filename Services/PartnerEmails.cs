using System.Net;

namespace Foras_Khadra.Services
{
    /// <summary>رسائل بوابة المنظمات (استلام الطلب، نشر الفرصة، الاعتذار) بثلاث لغات على قالب AccountEmails.</summary>
    public static class PartnerEmails
    {
        public static AccountEmails.Message Received(string lang, string orgName, int reach, string pageLink) => AccountEmails.Build(lang,
            T(lang, "استلمنا طلب نشر فرصتكم — فرص خضراء", "We received your publishing request — Foras Khadra", "Nous avons reçu votre demande de publication — Foras Khadra"),
            T(lang, $"مرحبًا {Org(orgName)}،", $"Hello {Org(orgName)},", $"Bonjour {Org(orgName)},"),
            T(lang, $"وصلنا رابط فرصتكم. سيكتبها فريقنا بثلاث لغات مع شروط الأهلية، وتُنشر عادةً خلال 48 ساعة بعد المراجعة، ثم نرسل لكم رابطها. تقديريًا تصل إلى نحو {reach:N0} عضو مؤهَّل من المسجَّلين لدينا.",
                    $"We received your opportunity link. Our team writes it in three languages with the eligibility criteria; it is usually published within 48 hours after review, and we will send you the link. It should reach about {reach:N0} eligible registered members.",
                    $"Nous avons reçu le lien de votre opportunité. Notre équipe la rédige en trois langues avec les critères d'éligibilité ; elle est généralement publiée sous 48 heures après validation, puis nous vous envoyons le lien. Elle devrait atteindre environ {reach:N0} membres inscrits éligibles."),
            T(lang, "بوابة المنظمات", "Organizations gateway", "Portail des organisations"), pageLink,
            T(lang, "إن لم ترسلوا هذا الطلب فتجاهلوا الرسالة.", "If you did not send this request, ignore this message.", "Si vous n'avez pas envoyé cette demande, ignorez ce message."));

        public static AccountEmails.Message Published(string lang, string orgName, string title, string link) => AccountEmails.Build(lang,
            T(lang, "نُشرت فرصتكم على فرص خضراء", "Your opportunity is live on Foras Khadra", "Votre opportunité est en ligne sur Foras Khadra"),
            T(lang, $"مرحبًا {Org(orgName)}،", $"Hello {Org(orgName)},", $"Bonjour {Org(orgName)},"),
            T(lang, $"نُشرت «{WebUtility.HtmlEncode(title)}» بثلاث لغات، وأُرسلت إشعارات للأعضاء الذين تطابق اهتماماتهم. يمكنكم مشاركة الرابط في قنواتكم.",
                    $"“{WebUtility.HtmlEncode(title)}” is published in three languages, and members whose interests match were notified. Feel free to share the link on your channels.",
                    $"« {WebUtility.HtmlEncode(title)} » est publiée en trois langues, et les membres dont les intérêts correspondent ont été notifiés. N'hésitez pas à partager le lien."),
            T(lang, "عرض الفرصة", "View the opportunity", "Voir l'opportunité"), link,
            T(lang, "لنشر فرصة أخرى، أرسلوا رابطها من بوابة المنظمات في أي وقت.", "To publish another opportunity, send its link from the organizations gateway any time.", "Pour publier une autre opportunité, envoyez son lien depuis le portail des organisations."));

        public static AccountEmails.Message Rejected(string lang, string orgName, string? note, string pageLink) => AccountEmails.Build(lang,
            T(lang, "بخصوص طلب نشر فرصتكم — فرص خضراء", "About your publishing request — Foras Khadra", "À propos de votre demande de publication — Foras Khadra"),
            T(lang, $"مرحبًا {Org(orgName)}،", $"Hello {Org(orgName)},", $"Bonjour {Org(orgName)},"),
            T(lang, "راجعنا الرابط الذي أرسلتموه ولم نتمكن من نشره هذه المرة." + Note(note, lang),
                    "We reviewed the link you sent and could not publish it this time." + Note(note, lang),
                    "Nous avons examiné le lien envoyé et n'avons pas pu le publier cette fois." + Note(note, lang)),
            T(lang, "إرسال فرصة أخرى", "Send another opportunity", "Envoyer une autre opportunité"), pageLink,
            T(lang, "نرحّب بأي فرصة مفتوحة لشباب الوطن العربي في البيئة والمناخ والتنمية.", "We welcome any open opportunity for youth in the Arab world in environment, climate and development.", "Nous accueillons toute opportunité ouverte aux jeunes du monde arabe dans l'environnement, le climat et le développement."));

        private static string T(string lang, string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;
        private static string Org(string name) => WebUtility.HtmlEncode(name.Trim());
        private static string Note(string? note, string lang) =>
            string.IsNullOrWhiteSpace(note) ? "" : " " + T(lang, "السبب: ", "Reason: ", "Motif : ") + WebUtility.HtmlEncode(note.Trim());
    }
}
