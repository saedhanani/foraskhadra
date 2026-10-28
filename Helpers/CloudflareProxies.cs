using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// نطاقات عناوين Cloudflare (https://www.cloudflare.com/ips). الموقع خلف Cloudflare CDN، فعنوان الاتصال
    /// الذي يراه الخادم هو عنوان Cloudflare لا الزائر؛ عنوان الزائر الحقيقي يأتي في الترويسة CF-Connecting-IP.
    /// لا تُصدَّق هذه الترويسة إلا إن جاء الاتصال من أحد هذه النطاقات، وإلا أمكن لأي أحد انتحال عنوان وتجاوز حدود الطلبات.
    /// </summary>
    public static class CloudflareProxies
    {
        public const string ClientIpHeader = "CF-Connecting-IP";

        // آخر تحديث للقائمة: 2026-10-28
        private static readonly string[] Ranges =
        {
            "173.245.48.0/20", "103.21.244.0/22", "103.22.200.0/22", "103.31.4.0/22", "141.101.64.0/18",
            "108.162.192.0/18", "190.93.240.0/20", "188.114.96.0/20", "197.234.240.0/22", "198.41.128.0/17",
            "162.158.0.0/15", "104.16.0.0/13", "104.24.0.0/14", "172.64.0.0/13", "131.0.72.0/22",
            "2400:cb00::/32", "2606:4700::/32", "2803:f800::/32", "2405:b500::/32", "2405:8100::/32",
            "2a06:98c0::/29", "2c0f:f248::/32",
        };

        /// <summary>يضبط ForwardedHeadersOptions بحيث يُقرأ عنوان الزائر والبروتوكول من Cloudflare فقط.</summary>
        public static void Configure(ForwardedHeadersOptions o)
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.ForwardedForHeaderName = ClientIpHeader;
            o.ForwardLimit = 1;
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
            foreach (var r in Ranges)
            {
                var parts = r.Split('/');
                o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse(parts[0]), int.Parse(parts[1])));
            }
        }
    }
}
