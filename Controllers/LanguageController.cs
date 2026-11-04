using Foras_Khadra.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// للروابط القديمة فقط (Language/Change?culture=en&amp;returnUrl=…): يحفظ اللغة في الكوكي ويحوّل إلى الرابط نفسه ببادئة اللغة.
    /// المبدِّل في الصفحات لم يعد يمرّ من هنا؛ روابطه مباشرة (/en/…، /fr/…، وبلا بادئة للعربية).
    /// </summary>
    public class LanguageController : Controller
    {
        public IActionResult Change(string? culture, string? returnUrl)
        {
            var target = CultureRouting.Normalize(culture);
            CultureRouting.WriteCookie(Response, target);
            var path = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
            return LocalRedirect(CultureRouting.WithCulture(path, target));
        }
    }
}
