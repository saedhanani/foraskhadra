using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// اللغة من الرابط لا من الكوكي: العربية بلا بادئة (/Home/AboutUs)، والإنجليزية والفرنسية ببادئة (/en/Home/AboutUs، /fr/...).
    /// لكل لغة رابط ثابت يفهرسه Google، والكوكي يُستعمل لشيء واحد: تحويل الزائر العائد من الرئيسية "/" إلى لغته (302).
    /// </summary>
    public static class CultureRouting
    {
        public const string Default = "ar";
        public static readonly string[] Supported = { "ar", "en", "fr" };
        public static readonly string[] Prefixed = { "en", "fr" };

        /// <summary>اسم قيد المسار المسجَّل في RouteOptions: {culture:culture}.</summary>
        public const string ConstraintName = "culture";

        public static bool IsSupported(string? culture) =>
            culture != null && Supported.Contains(culture.Trim(), StringComparer.OrdinalIgnoreCase);

        /// <summary>لغة معروفة بأحرف صغيرة، وإلا العربية.</summary>
        public static string Normalize(string? culture) =>
            IsSupported(culture) ? culture!.Trim().ToLowerInvariant() : Default;

        /// <summary>"en" أو "fr" إن بدأ المسار بها كمقطع كامل ("/en"، "/en/x")، وإلا null (العربية أو مسار آخر مثل /english).</summary>
        public static string? FromPath(string? path)
        {
            if (string.IsNullOrEmpty(path) || path.Length < 3 || path[0] != '/') return null;
            if (path.Length > 3 && path[3] != '/') return null;
            var segment = path.Substring(1, 2).ToLowerInvariant();
            return Prefixed.Contains(segment) ? segment : null;
        }

        /// <summary>بادئة اللغة في الروابط: "" للعربية، "/en" و"/fr" لغيرها.</summary>
        public static string Prefix(string? culture)
        {
            culture = Normalize(culture);
            return culture == Default ? string.Empty : "/" + culture;
        }

        /// <summary>يزيل بادئة اللغة إن وُجدت: "/en/x" → "/x"، "/en" → "/"، "/x" → "/x".</summary>
        public static string StripPrefix(string? path)
        {
            if (string.IsNullOrEmpty(path)) return "/";
            if (FromPath(path) == null) return path;
            var rest = path.Substring(3);
            return rest.Length == 0 ? "/" : rest;
        }

        /// <summary>الرابط نفسه بلغة أخرى، مع الاستعلام: ("/en/Home/AboutUs?x=1", "fr") → "/fr/Home/AboutUs?x=1"؛ (…, "ar") → "/Home/AboutUs?x=1".</summary>
        public static string WithCulture(string? pathAndQuery, string? culture)
        {
            pathAndQuery ??= "/";
            var q = pathAndQuery.IndexOf('?');
            var path = q < 0 ? pathAndQuery : pathAndQuery.Substring(0, q);
            var query = q < 0 ? string.Empty : pathAndQuery.Substring(q);
            var bare = StripPrefix(path);
            var prefix = Prefix(culture);
            var result = prefix.Length == 0 ? bare : bare == "/" ? prefix : prefix + bare;
            return result + query;
        }

        /// <summary>لغة الكوكي (.AspNetCore.Culture) إن كانت مدعومة.</summary>
        public static string? FromCookie(HttpRequest request)
        {
            var raw = request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
            if (string.IsNullOrEmpty(raw)) return null;
            var parsed = CookieRequestCultureProvider.ParseCookieValue(raw);
            var value = parsed?.Cultures.FirstOrDefault().Value;
            return IsSupported(value) ? value!.ToLowerInvariant() : null;
        }

        public static void WriteCookie(HttpResponse response, string culture) =>
            response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(Normalize(culture))),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });

        /// <summary>صفحة يراها إنسان (لا API ولا بوابة /go ولا ملفات) — فقط هذه تُحدِّث كوكي اللغة.</summary>
        public static bool IsHtmlPage(PathString path)
        {
            var p = StripPrefix(path.Value);
            if (p.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) || p.StartsWith("/go/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("/Language/", StringComparison.OrdinalIgnoreCase)) return false;
            if (p.Equals("/health", StringComparison.OrdinalIgnoreCase) || p.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase) || p.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase)) return false;
            var lastSlash = p.LastIndexOf('/');
            return p.IndexOf('.', lastSlash < 0 ? 0 : lastSlash) < 0;
        }
    }

    /// <summary>قيد {culture:culture}: يقبل en وfr فقط؛ "ar" يفشل عمدًا حتى تُولَّد روابط العربية بلا بادئة.</summary>
    public sealed class CultureRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection) =>
            values.TryGetValue(routeKey, out var value)
            && value is string s
            && CultureRouting.Prefixed.Contains(s, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>مزوّد اللغة الوحيد: من بادئة المسار. في إعادة تنفيذ صفحة 404 يقرأ المسار الأصلي حتى تظهر الصفحة بلغة الرابط.</summary>
    public sealed class PathRequestCultureProvider : RequestCultureProvider
    {
        public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            var path = httpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath ?? httpContext.Request.Path.Value;
            var culture = CultureRouting.FromPath(path) ?? CultureRouting.Default;
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture));
        }
    }

    public static class CultureRedirectExtensions
    {
        /// <summary>
        /// ثلاث مهام قبل تحديد اللغة: (1) الروابط القديمة ?culture=xx → 301 إلى المسار باللغة؛
        /// (2) الرئيسية "/" لزائر كوكيه en/fr → 302 إلى /en أو /fr (الزاحف بلا كوكي فيرى العربية)؛
        /// (3) تذكُّر آخر لغة شوهدت بها صفحة في الكوكي.
        /// </summary>
        public static IApplicationBuilder UseCultureRedirects(this IApplicationBuilder app) => app.Use(async (context, next) =>
        {
            var request = context.Request;
            var reExecuting = context.Features.Get<IStatusCodeReExecuteFeature>() != null; // صفحة 404: اللغة من المسار الأصلي، ولا كوكي ولا تحويل
            if (!reExecuting && (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)))
            {
                if (request.Query.TryGetValue("culture", out var fromQuery)
                    && CultureRouting.IsSupported(fromQuery)
                    && !request.Path.StartsWithSegments("/Language"))
                {
                    var rest = QueryString.Create(request.Query
                        .Where(kv => !string.Equals(kv.Key, "culture", StringComparison.OrdinalIgnoreCase))
                        .SelectMany(kv => kv.Value.Select(v => KeyValuePair.Create(kv.Key, v))));
                    context.Response.Redirect(CultureRouting.WithCulture(request.Path + rest, fromQuery!), permanent: true);
                    return;
                }

                var inPath = CultureRouting.FromPath(request.Path.Value);
                var inCookie = CultureRouting.FromCookie(request);
                var isRoot = !request.Path.HasValue || request.Path == "/";
                if (inPath == null && isRoot && inCookie != null && inCookie != CultureRouting.Default)
                {
                    context.Response.Redirect(CultureRouting.WithCulture("/" + request.QueryString, inCookie));
                    return;
                }

                var current = inPath ?? CultureRouting.Default;
                if (inCookie != current && CultureRouting.IsHtmlPage(request.Path))
                    CultureRouting.WriteCookie(context.Response, current);
            }

            await next();
        });
    }
}
