using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// يضيف قيمة المسار culture (en/fr) إلى كل رابط يُولَّد في صفحة غير عربية (Url.Action، asp-action، RedirectToAction).
    /// لماذا: التوجيه بالنهايات (endpoint routing) يتجاهل القيم المحيطة حين يختلف controller/action عن الصفحة الحالية إن كانا
    /// قيمتَين مطلوبتَين في المسار الهدف (مسارات Details ذات الـslug)، فكانت روابط الفرص من /en/Home/AllOpportunities تخرج بلا /en.
    /// العربية لا تُضاف لها قيمة، فتسقط إلى المسارات بلا بادئة كما هو مطلوب.
    /// </summary>
    public sealed class CultureUrlHelperFactory : IUrlHelperFactory
    {
        private readonly UrlHelperFactory _inner = new();

        public IUrlHelper GetUrlHelper(ActionContext context)
        {
            var items = context.HttpContext.Items;
            if (items.TryGetValue(typeof(CultureUrlHelper), out var cached) && cached is CultureUrlHelper existing)
                return existing;
            var helper = new CultureUrlHelper(_inner.GetUrlHelper(context));
            items[typeof(CultureUrlHelper)] = helper;
            return helper;
        }
    }

    public sealed class CultureUrlHelper : IUrlHelper
    {
        private readonly IUrlHelper _inner;

        public CultureUrlHelper(IUrlHelper inner) => _inner = inner;

        public ActionContext ActionContext => _inner.ActionContext;

        public string? Action(UrlActionContext actionContext)
        {
            actionContext.Values = WithCulture(actionContext.Values);
            return _inner.Action(actionContext);
        }

        public string? RouteUrl(UrlRouteContext routeContext)
        {
            routeContext.Values = WithCulture(routeContext.Values);
            return _inner.RouteUrl(routeContext);
        }

        public string? Content(string? contentPath) => _inner.Content(contentPath);
        public bool IsLocalUrl(string? url) => _inner.IsLocalUrl(url);
        public string? Link(string? routeName, object? values) => _inner.Link(routeName, WithCulture(values));

        private object? WithCulture(object? values)
        {
            var culture = _inner.ActionContext.RouteData.Values.TryGetValue("culture", out var fromRoute) && fromRoute is string s
                ? s
                : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (!CultureRouting.Prefixed.Contains(culture, StringComparer.OrdinalIgnoreCase)) return values;
            var dictionary = new RouteValueDictionary(values);
            if (!dictionary.ContainsKey("culture")) dictionary["culture"] = culture.ToLowerInvariant();
            return dictionary;
        }
    }
}
