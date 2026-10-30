using Foras_Khadra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Foras_Khadra.Helpers
{
    /// <summary>صفحتا الدخول (Account/Login و Organization/Login): من سجّل دخوله فعلًا يُحوَّل إلى صفحته بدل النموذج.
    /// يعمل قبل فحص رمز CSRF (Order أقل من 1000): على الخادم البطيء قد يصل طلب دخول ثانٍ (نقرة ثانية على الزر)
    /// بعد أن نجح الأول، فيحمل كوكي الدخول مع رمز صُنع لزائر مجهول ويرفضه الفحص بصفحة "Status Code: 400"
    /// مع أن الدخول تم — هنا يُحوَّل إلى صفحته بدلها.</summary>
    public class RedirectSignedInAttribute : Attribute, IAsyncAuthorizationFilter, IOrderedFilter
    {
        public int Order => -1;

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var principal = context.HttpContext.User;
            if (principal.Identity?.IsAuthenticated != true) return;

            // كوكي لحساب حُذف: نترك النموذج يعمل (وإلا دار التحويل بين Login و User/Dashboard)
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            if (await userManager.GetUserAsync(principal) == null) return;

            // نفس وجهات ما بعد الدخول في AccountController و OrganizationController
            context.Result = principal.IsInRole("Admin") ? new RedirectToActionResult("Dashboard", "Admin", null)
                : principal.IsInRole("Organization") ? new RedirectToActionResult("Index", "Home", null)
                : new RedirectToActionResult("Dashboard", "User", null);
        }
    }
}
