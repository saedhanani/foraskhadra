using Ganss.Xss;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// تعقيم HTML القادم من محرر Quill (المنظمات، الأدمن، منصة الفريق) قبل تخزينه أو عرضه بـ Html.Raw.
    /// يزيل السكربتات ومعالجات الأحداث وروابط javascript: ويُبقي التنسيق العادي (فقرات، قوائم، روابط، صور...).
    /// </summary>
    public static class SafeHtml
    {
        private static readonly HtmlSanitizer Sanitizer = Create();

        private static HtmlSanitizer Create()
        {
            var s = new HtmlSanitizer();
            s.AllowedAttributes.Add("class"); // محاذاة Quill: ql-align-*, ql-indent-*
            s.AllowedSchemes.Add("mailto");
            s.AllowedSchemes.Add("tel");
            return s;
        }

        /// <summary>يعيد نسخة معقّمة من النص؛ النص الفارغ يعود فارغًا.</summary>
        public static string Clean(string? html)
            => string.IsNullOrWhiteSpace(html) ? "" : Sanitizer.Sanitize(html);

        /// <summary>بديل آمن لـ Html.Raw للمحتوى الذي كتبه المستخدمون.</summary>
        public static IHtmlContent SanitizedRaw(this IHtmlHelper html, string? content)
            => new HtmlString(Clean(content));
    }
}
