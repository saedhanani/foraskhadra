using Microsoft.AspNetCore.Authorization;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// أين يُرسَل غير المسجَّل حين يطلب صفحة تتطلب دخولاً: صفحات المنظمات (Roles تحتوي Organization) إلى /Organization/Login،
    /// وكل ما عداها إلى /Account/Login — مع بادئة لغة الرابط وreturnUrl يعيده إلى ما كان يفتحه.
    /// </summary>
    public static class LoginRedirect
    {
        public const string IndividualLoginPath = "/Account/Login";
        public const string OrganizationLoginPath = "/Organization/Login";

        public static bool IsOrganizationOnly(IEnumerable<IAuthorizeData> authorizeData) =>
            authorizeData.Any(a => (a.Roles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains("Organization", StringComparer.OrdinalIgnoreCase));

        /// <summary>رابط صفحة الدخول المناسبة للطلب الحالي، مع returnUrl = المسار والاستعلام الأصليان.</summary>
        public static string BuildLoginUrl(string? requestPath, string? queryString, IEnumerable<IAuthorizeData> authorizeData)
        {
            var path = string.IsNullOrEmpty(requestPath) ? "/" : requestPath;
            var culture = CultureRouting.FromPath(path);
            var loginPath = CultureRouting.Prefix(culture) + (IsOrganizationOnly(authorizeData) ? OrganizationLoginPath : IndividualLoginPath);
            var returnUrl = path + (queryString ?? string.Empty);
            return loginPath + "?returnUrl=" + Uri.EscapeDataString(returnUrl);
        }
    }
}
