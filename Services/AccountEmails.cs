using System.Net;

namespace Foras_Khadra.Services
{
    /// <summary>نصوص رسائل الحساب (تأكيد البريد، تأكيد تغييره) بثلاث لغات — نفس قالب رسالة الاستعادة.</summary>
    public static class AccountEmails
    {
        public sealed record Message(string Subject, string Html);

        public static Message ConfirmEmail(string lang, string link, string? firstName = null) => Build(lang,
            T(lang, "تأكيد بريدك في فرص خضراء", "Confirm your email — Foras Khadra", "Confirmez votre e-mail — Foras Khadra"),
            T(lang, $"مرحبًا {Name(firstName, lang)}،", $"Hello {Name(firstName, lang)},", $"Bonjour {Name(firstName, lang)},"),
            T(lang, "أهلًا بك في فرص خضراء. اضغط الزر لتأكيد بريدك؛ التأكيد مطلوب قبل التقديم على الفرص (تستطيع تصفحها وحفظها قبل ذلك).",
                    "Welcome to Foras Khadra. Click the button to confirm your email; confirmation is required before applying to opportunities (you can browse and save them meanwhile).",
                    "Bienvenue sur Foras Khadra. Cliquez sur le bouton pour confirmer votre e-mail ; la confirmation est requise avant de postuler (vous pouvez déjà parcourir et enregistrer les opportunités)."),
            T(lang, "تأكيد البريد", "Confirm email", "Confirmer l'e-mail"), link,
            T(lang, "إن لم تنشئ هذا الحساب فتجاهل هذه الرسالة.", "If you did not create this account, ignore this message.", "Si vous n'avez pas créé ce compte, ignorez ce message."));

        public static Message ChangeEmail(string lang, string link, string newEmail) => Build(lang,
            T(lang, "تأكيد بريدك الجديد في فرص خضراء", "Confirm your new email — Foras Khadra", "Confirmez votre nouvel e-mail — Foras Khadra"),
            T(lang, "مرحبًا،", "Hello,", "Bonjour,"),
            T(lang, $"طُلب تغيير بريد الدخول إلى {WebUtility.HtmlEncode(newEmail)}. اضغط الزر لتأكيد العنوان الجديد؛ يبقى بريدك الحالي حتى تؤكده.",
                    $"A request was made to change your sign-in email to {WebUtility.HtmlEncode(newEmail)}. Click the button to confirm the new address; your current email stays until you do.",
                    $"Une demande de changement d'e-mail de connexion vers {WebUtility.HtmlEncode(newEmail)} a été faite. Cliquez sur le bouton pour confirmer la nouvelle adresse ; l'actuelle reste active jusque-là."),
            T(lang, "تأكيد البريد الجديد", "Confirm new email", "Confirmer le nouvel e-mail"), link,
            T(lang, "إن لم تطلب هذا التغيير فتجاهل الرسالة ولن يتغير شيء.", "If you did not request this change, ignore this message and nothing will change.", "Si vous n'êtes pas à l'origine de cette demande, ignorez ce message : rien ne changera."));

        /// <summary>تسجيل ببريد مسجَّل أصلًا: لا نكشف ذلك لمن في المتصفح، بل نخبر صاحب البريد.</summary>
        public static Message AlreadyRegistered(string lang, string loginLink, string resetLink) => Build(lang,
            T(lang, "لديك حساب في فرص خضراء بالفعل", "You already have a Foras Khadra account", "Vous avez déjà un compte Foras Khadra"),
            T(lang, "مرحبًا،", "Hello,", "Bonjour,"),
            T(lang, "حاول أحدهم (ربما أنت) إنشاء حساب بهذا البريد، وهو مسجَّل عندنا من قبل. سجّل الدخول بكلمة مرورك، أو أعد تعيينها من رابط «نسيت كلمة المرور»: " + WebUtility.HtmlEncode(resetLink),
                    "Someone (probably you) tried to create an account with this email, which is already registered with us. Sign in with your password, or reset it here: " + WebUtility.HtmlEncode(resetLink),
                    "Quelqu'un (probablement vous) a tenté de créer un compte avec cet e-mail, déjà enregistré chez nous. Connectez-vous avec votre mot de passe, ou réinitialisez-le ici : " + WebUtility.HtmlEncode(resetLink)),
            T(lang, "تسجيل الدخول", "Sign in", "Se connecter"), loginLink,
            T(lang, "إن لم تكن أنت فتجاهل هذه الرسالة؛ لم يتغير شيء في حسابك.", "If this wasn't you, ignore this message; nothing changed in your account.", "Si ce n'était pas vous, ignorez ce message ; rien n'a changé dans votre compte."));

        private static string T(string lang, string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;

        private static string Name(string? first, string lang) =>
            string.IsNullOrWhiteSpace(first) ? T(lang, "بك", "there", "à vous") : WebUtility.HtmlEncode(first.Trim());

        internal static Message Build(string lang, string subject, string greeting, string body, string button, string link, string footer)
        {
            var dir = lang == "ar" ? "rtl" : "ltr";
            var html = $@"<!DOCTYPE html>
<html lang='{lang}' dir='{dir}'>
<head><meta charset='UTF-8'><title>{WebUtility.HtmlEncode(subject)}</title></head>
<body style='font-family: Tahoma, Arial, sans-serif; background-color: #f4f6f8; padding: 20px;'>
  <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 8px;'>
    <h2 style='text-align: center; color: #1B6B23;'>Foras Khadra · فرص خضراء</h2>
    <p>{greeting}</p>
    <p>{body}</p>
    <div style='text-align: center; margin: 30px 0;'>
      <a href='{WebUtility.HtmlEncode(link)}' style='background-color: #1B6B23; color: #ffffff; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: bold;'>{button}</a>
    </div>
    <p style='font-size: 13px; color: #555;'>{footer}</p>
    <p style='font-size: 12px; color: #777; word-break: break-all;'>{WebUtility.HtmlEncode(link)}</p>
  </div>
</body>
</html>";
            return new Message(subject, html);
        }
    }
}
