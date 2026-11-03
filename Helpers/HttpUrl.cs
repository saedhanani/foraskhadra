using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// روابط المستخدمين (رابط التقديم، موقع المنظمة، المصدر): يُقبل http/https فقط.
    /// رابط بلا scheme مثل "example.org/page" يُكمَّل بـ https://، أما javascript: أو data: أو أي scheme آخر فيُرفض،
    /// لأن href بـ javascript: ينفّذ كودًا في أصل الموقع عند النقر (CSP لا يمنعه).
    /// </summary>
    public static class HttpUrl
    {
        private static readonly Regex SchemePrefix = new(@"^[a-zA-Z][a-zA-Z0-9+.\-]*:", RegexOptions.Compiled);

        /// <summary>الرابط بعد التنظيف، أو null إن لم يكن رابط http/https صالحًا.</summary>
        public static string? Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var value = raw.Trim();
            if (value.Any(char.IsControl)) return null;

            // "www.daad.de/..." → "https://www.daad.de/..."؛ لكن "javascript:..." له scheme صريح فلا يُكمَّل بل يُرفض أدناه
            if (!SchemePrefix.IsMatch(value) && !value.StartsWith("//"))
                value = "https://" + value;

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
            if (string.IsNullOrEmpty(uri.Host) || (!uri.Host.Contains('.') && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)))
                return null;
            return value;
        }

        public static bool IsValid(string? raw) => Normalize(raw) != null;
    }

    /// <summary>سمة تحقق لحقول الروابط: فارغ = صالح (اجمعها مع [Required] حيث يلزم).</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class HttpUrlAttribute : ValidationAttribute
    {
        public HttpUrlAttribute() : base("الرابط غير صالح: يجب أن يكون عنوان موقع يبدأ بـ https:// أو http://") { }

        public override bool IsValid(object? value) =>
            value is null || (value is string s && (string.IsNullOrWhiteSpace(s) || HttpUrl.IsValid(s)));
    }
}
