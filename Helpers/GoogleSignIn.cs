using System.Security.Claims;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// الدخول بحساب Google: الإعدادات وقراءة بيانات الحساب من المطالبات (Claims).
    /// المفاتيح في الإعدادات: Authentication:Google:ClientId و Authentication:Google:ClientSecret.
    /// </summary>
    public static class GoogleSignIn
    {
        public const string Scheme = "Google";
        public const string EmailVerifiedClaim = "urn:google:email_verified";

        public static bool IsConfigured(IConfiguration config) =>
            !string.IsNullOrWhiteSpace(config["Authentication:Google:ClientId"])
            && !string.IsNullOrWhiteSpace(config["Authentication:Google:ClientSecret"]);

        public static string? Email(ClaimsPrincipal principal)
        {
            var email = principal.FindFirstValue(ClaimTypes.Email)?.Trim();
            return string.IsNullOrEmpty(email) ? null : email;
        }

        /// <summary>
        /// Google يرسل email_verified=true لبريد Gmail ولأي بريد تحقق منه. غياب المطالبة = غير مُتحقَّق
        /// (لا نُنشئ حسابًا ولا نربطه ببريد قد لا يملكه صاحب حساب Google).
        /// </summary>
        public static bool IsEmailVerified(ClaimsPrincipal principal) =>
            string.Equals(principal.FindFirstValue(EmailVerifiedClaim), "true", StringComparison.OrdinalIgnoreCase);

        /// <summary>الاسم الأول والأخير من Google، وإلا من الاسم الكامل، وإلا من البريد.</summary>
        public static (string First, string Last) Names(ClaimsPrincipal principal, string email)
        {
            var first = principal.FindFirstValue(ClaimTypes.GivenName)?.Trim() ?? "";
            var last = principal.FindFirstValue(ClaimTypes.Surname)?.Trim() ?? "";
            if (first.Length == 0)
            {
                var full = principal.FindFirstValue(ClaimTypes.Name)?.Trim() ?? "";
                var space = full.IndexOf(' ');
                (first, last) = full.Length == 0 ? ("", last)
                    : space < 0 ? (full, last)
                    : (full[..space], last.Length > 0 ? last : full[(space + 1)..].Trim());
            }
            if (first.Length == 0) first = email.Split('@')[0];
            return (Trim(first, 50), Trim(last, 50));
        }

        private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
    }
}
