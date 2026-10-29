using System.Text.Encodings.Web;
using System.Text.Json;
using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>حمولة إشعار الدفع كما يقرؤها wwwroot/sw.js (حدث push).</summary>
    public record PushMessage(string Title, string Body, string Url, string Tag, string Lang)
    {
        // العربية كما هي بلا ترميز \u: الحمولة أصغر بكثير (حدّ خدمات الدفع نحو 4 كيلوبايت)، ولا تُدرج في HTML
        private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public string ToJson() => JsonSerializer.Serialize(new
        {
            title = Title,
            body = Body,
            url = Url,
            tag = Tag,
            lang = Lang,
            dir = Lang == "ar" ? "rtl" : "ltr"
        }, Options);
    }

    /// <summary>نص إشعار الدفع لكل نوع إشعار — نفس معاني قائمة الجرس (Views/Shared/_NotificationMenu.cshtml) بصياغة قصيرة تناسب شاشة القفل.</summary>
    public static class NotificationText
    {
        public static PushMessage Welcome(string lang)
        {
            string T(string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;
            return new PushMessage(
                T("فرص خضراء", "Foras Khadra", "Foras Khadra"),
                T("تم تفعيل التنبيهات ✓ سنذكّرك قبل انتهاء مواعيد فرصك المحفوظة.",
                  "Notifications are on ✓ We'll remind you before your saved deadlines close.",
                  "Notifications activées ✓ Nous vous rappellerons vos échéances enregistrées."),
                "/Opportunity/Saved", "welcome", lang);
        }

        /// <summary>null إن لم يكن للإشعار ما يكفي من البيانات (فرصة محذوفة مثلًا).</summary>
        public static PushMessage? For(Notification n, string lang, DateTime? today = null)
        {
            string T(string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;
            var opp = n.Opportunity;
            var oppTitle = opp == null ? null : lang switch { "en" => opp.TitleEn, "fr" => opp.TitleFr, _ => opp.TitleAr };
            var oppUrl = opp == null ? null : $"/Opportunity/Details/{opp.Id}/{Uri.EscapeDataString(SlugHelper.Make(oppTitle))}";
            var tag = $"n{n.Id}";

            switch (n.Kind)
            {
                case NotificationKind.SavedDeadlineSoon when opp != null:
                    return new PushMessage(
                        "⏰ " + DeadlineInfo.For(opp, lang, today).Label,
                        T($"فرصتك المحفوظة «{oppTitle}» يقترب موعدها النهائي.",
                          $"Your saved opportunity “{oppTitle}” closes soon.",
                          $"Votre opportunité « {oppTitle} » se termine bientôt."),
                        oppUrl!, $"opp{opp.Id}", lang);

                case NotificationKind.SavedDeadlineLastDay when opp != null:
                    return new PushMessage(
                        "🔔 " + DeadlineInfo.For(opp, lang, today).Label,
                        T($"آخر فرصة للتقديم على «{oppTitle}» — لا تفوّتها.",
                          $"Last chance to apply for “{oppTitle}”.",
                          $"Dernière chance pour « {oppTitle} »."),
                        oppUrl!, $"opp{opp.Id}", lang);

                case NotificationKind.CycleReopened when opp != null:
                    return new PushMessage(
                        T("فُتحت دورة جديدة", "A new cycle is open", "Nouvelle session ouverte"),
                        oppTitle!, oppUrl!, tag, lang);

                case NotificationKind.InterestMatches:
                    return new PushMessage(
                        T("فرص جديدة لك", "New opportunities for you", "Nouvelles opportunités pour vous"),
                        T($"أُضيفت {n.Count} فرص جديدة تطابق اهتماماتك.",
                          $"{n.Count} new opportunities match your interests.",
                          $"{n.Count} nouvelles opportunités correspondent à vos intérêts."),
                        "/Home/AllOpportunities", tag, lang);

                case NotificationKind.SavedSearchMatches:
                    return new PushMessage(
                        T($"نتائج جديدة لبحثك «{n.Label}»", $"New results for “{n.Label}”", $"Nouveaux résultats pour « {n.Label} »"),
                        T($"{n.Count} فرص جديدة في بحثك المحفوظ.", $"{n.Count} new matches for your saved search.", $"{n.Count} nouveaux résultats."),
                        "/Home/AllOpportunities" + (string.IsNullOrEmpty(n.LinkParams) ? "" : "?" + n.LinkParams), tag, lang);

                case NotificationKind.ProgramOpened when !string.IsNullOrEmpty(n.LinkParams):
                    return new PushMessage(
                        T("باب التقديم مفتوح", "Applications are open", "Candidatures ouvertes"),
                        n.Label ?? "", "/Programs/" + Uri.EscapeDataString(n.LinkParams), tag, lang);

                case NotificationKind.WeeklySummary:
                    return new PushMessage(
                        T("ملخص أسبوعك جاهز", "Your weekly summary is ready", "Votre résumé hebdomadaire est prêt"),
                        n.Count > 0
                            ? T($"{n.Count} فرص جديدة في اهتماماتك هذا الأسبوع.", $"{n.Count} new opportunities in your interests this week.", $"{n.Count} nouvelles opportunités cette semaine.")
                            : T("مواعيد تقترب من فرصك المحفوظة.", "Saved deadlines are approaching.", "Des échéances approchent."),
                        "/User/Weekly", tag, lang);

                default:
                    return null;
            }
        }
    }
}
