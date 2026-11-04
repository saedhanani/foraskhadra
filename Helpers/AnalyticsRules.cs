using System.Text.RegularExpressions;

namespace Foras_Khadra.Helpers
{
    /// <summary>قواعد التحليلات الداخلية (بلا كوكيز): أي طلب يُعدّ، وكيف يُطبَّع المسار، ومن أين جاءت الزيارة، ومن هو زاحف.</summary>
    public static class AnalyticsRules
    {
        private static readonly Regex Bot = new(
            @"bot|crawl|spider|slurp|facebookexternalhit|whatsapp|telegram|skype|preview|lighthouse|headless|python|curl|wget|java/|go-http|okhttp|monitor|uptime|scan|fetch|httpclient|pingdom|validator",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TypeValue = new("^[a-z]{3,20}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>المسارات (بعد إزالة بادئة اللغة) التي لا تُعدّ: الإدارة والحسابات والواجهات والملفات. صفحتا الدخول والتسجيل تُعدّان (قمع التسجيل).</summary>
        private static readonly string[] SkippedPrefixes =
        {
            "/admin", "/api", "/go/", "/organization", "/user", "/team", "/impactstats", "/language", "/tracker",
            "/savedsearch", "/push", "/opportunity/index", "/opportunity/create", "/opportunity/edit", "/opportunity/delete",
            "/opportunity/orgopportunities", "/opportunity/toggle", "/articles/index", "/articles/create", "/articles/edit",
            "/activities/index", "/activities/create", "/activities/edit", "/programs/index", "/programs/create", "/programs/edit",
            "/health", "/sitemap", "/robots", "/offline",
            "/compass/me", "/compass/result", "/compass/register", "/compass/documents"
        };
        private static readonly string[] TrackedAccountPages = { "/account/login", "/account/registeruser" };

        public static bool IsBot(string? userAgent) => string.IsNullOrWhiteSpace(userAgent) || Bot.IsMatch(userAgent);

        /// <param name="barePath">المسار بلا بادئة اللغة.</param>
        public static bool ShouldTrack(string? barePath)
        {
            var p = (barePath ?? "/").ToLowerInvariant();
            if (p.StartsWith("/account")) return TrackedAccountPages.Any(t => p == t || p.StartsWith(t + "/"));
            if (SkippedPrefixes.Any(s => p.StartsWith(s))) return false;
            var lastSlash = p.LastIndexOf('/');
            return p.IndexOf('.', lastSlash < 0 ? 0 : lastSlash) < 0; // ملفات (css/js/صور) لا تُعدّ
        }

        /// <summary>
        /// يطبّع المسار للتجميع: بلا بادئة لغة، بلا slug بعد رقم التفاصيل، بلا استعلام (عدا type في كل الفرص)، أحرف صغيرة.
        /// يعيد رقم الفرصة إن كانت صفحة تفاصيل فرصة (لتعبئة OpportunityDailyStats.Views).
        /// </summary>
        public static (string Path, int? OpportunityId) Normalize(string? path, IQueryCollection? query)
        {
            var bare = CultureRouting.StripPrefix(path).TrimEnd('/');
            if (bare.Length == 0) bare = "/";
            var segments = bare.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int? opportunityId = null;
            if (segments.Length >= 3 && segments[1].Equals("Details", StringComparison.OrdinalIgnoreCase) && int.TryParse(segments[2], out var id)
                && (segments[0].Equals("Opportunity", StringComparison.OrdinalIgnoreCase) || segments[0].Equals("Articles", StringComparison.OrdinalIgnoreCase) || segments[0].Equals("Activities", StringComparison.OrdinalIgnoreCase)))
            {
                if (segments[0].Equals("Opportunity", StringComparison.OrdinalIgnoreCase)) opportunityId = id;
                bare = "/" + segments[0] + "/Details/" + id;
            }
            var result = bare.ToLowerInvariant();
            if (result == "/home/allopportunities" && query != null && query.TryGetValue("type", out var type) && type.Count == 1 && TypeValue.IsMatch(type.ToString()))
                result += "?type=" + type.ToString().ToLowerInvariant();
            if (result.Length > 200) result = result[..200];
            return (result, opportunityId);
        }

        /// <summary>مصدر الزيارة: utm:…، أو pwa (start_url التطبيق)، أو مضيف الإحالة الخارجي، أو direct؛ "internal" للتنقل داخل الموقع.</summary>
        public static string SourceOf(HttpRequest request)
        {
            if (request.Query.TryGetValue("utm_source", out var utm) && !string.IsNullOrWhiteSpace(utm))
                return Clean("utm:" + utm.ToString());
            if (request.Query["source"] == "pwa") return "pwa";
            var referer = request.Headers.Referer.ToString();
            if (string.IsNullOrEmpty(referer) || !Uri.TryCreate(referer, UriKind.Absolute, out var uri)) return "direct";
            var host = StripWww(uri.Host);
            return host == StripWww(request.Host.Host) ? "internal" : Clean(host);
        }

        /// <summary>عبارة البحث كما تُحفظ في Label: أحرف صغيرة، فراغات مفردة، 200 حرفًا.</summary>
        public static string CleanLabel(string? label)
        {
            var s = Regex.Replace((label ?? "").Trim().ToLowerInvariant(), @"\s+", " ");
            return s.Length > 200 ? s[..200] : s;
        }

        private static string StripWww(string host)
        {
            host = host.ToLowerInvariant();
            return host.StartsWith("www.") ? host[4..] : host;
        }

        private static string Clean(string value)
        {
            value = value.Trim().ToLowerInvariant();
            return value.Length > 100 ? value[..100] : value;
        }
    }
}
