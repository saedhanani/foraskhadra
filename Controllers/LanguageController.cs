using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Foras_Khadra.Controllers
{
    public class LanguageController : Controller
    {
        private static readonly string[] Supported = { "ar", "en", "fr" };

        public IActionResult Change(string? culture, string? returnUrl)
        {
            // ثقافة غير معروفة → العربية؛ رابط خارجي → الرئيسية (LocalRedirect كان يرمي 500)
            culture = Supported.Contains((culture ?? "").Trim().ToLowerInvariant()) ? culture!.Trim().ToLowerInvariant() : "ar";
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true
                });
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
        }
    }
}
