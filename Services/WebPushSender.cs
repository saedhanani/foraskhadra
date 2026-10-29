using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Foras_Khadra.Services
{
    public enum PushResult
    {
        Sent,
        /// <summary>الاشتراك انتهى أو أُلغي من المتصفح (404/410) — يُحذف.</summary>
        Gone,
        Failed
    }

    /// <summary>خدمات الدفع المعروفة فقط: الخادم يرسل طلبًا إلى رابط الاشتراك، فلا يُقبل أي رابط آخر (حماية من SSRF).</summary>
    public static class PushEndpoints
    {
        private static readonly string[] Hosts =
        {
            "fcm.googleapis.com",          // Chrome / Android / Edge على Android / Samsung / Opera
            "android.googleapis.com",
            "push.services.mozilla.com",   // Firefox
            "notify.windows.com",          // Edge على Windows
            "push.apple.com"               // Safari على iPhone/iPad/Mac (web.push.apple.com)
        };

        public static bool IsAllowed(string? endpoint) =>
            Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.IsDefaultPort
            && Hosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase)
                              || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));

        /// <summary>مفتاح المتصفح 65 بايت يبدأ بـ 0x04، وسرّ المصادقة 16 بايت.</summary>
        public static bool KeysAreValid(string? p256dh, string? auth)
        {
            try
            {
                var key = WebEncoders.Base64UrlDecode(p256dh ?? "");
                var secret = WebEncoders.Base64UrlDecode(auth ?? "");
                return key.Length == 65 && key[0] == 0x04 && secret.Length == 16;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public interface IWebPushSender
    {
        Task<PushResult> SendAsync(PushSubscription subscription, string payloadJson, CancellationToken ct = default);
    }

    /// <summary>Web Push بلا مكتبات خارجية: تشفير الحمولة (RFC 8291، aes128gcm) وتوقيع VAPID (RFC 8292) بتشفير .NET المدمج،
    /// ثم POST إلى خدمة الدفع الخاصة بمتصفح المشترك.</summary>
    public class WebPushSender : IWebPushSender
    {
        public const string HttpClientName = "webpush";
        private const int RecordSize = 4096;

        private readonly IHttpClientFactory _http;
        private readonly IVapidKeyStore _keys;
        private readonly ILogger<WebPushSender> _logger;

        public WebPushSender(IHttpClientFactory http, IVapidKeyStore keys, ILogger<WebPushSender> logger)
        {
            _http = http;
            _keys = keys;
            _logger = logger;
        }

        public async Task<PushResult> SendAsync(PushSubscription subscription, string payloadJson, CancellationToken ct = default)
        {
            if (!PushEndpoints.IsAllowed(subscription.Endpoint) || !PushEndpoints.KeysAreValid(subscription.P256dh, subscription.Auth))
                return PushResult.Gone;

            var body = Encrypt(Encoding.UTF8.GetBytes(payloadJson),
                WebEncoders.Base64UrlDecode(subscription.P256dh),
                WebEncoders.Base64UrlDecode(subscription.Auth));

            var endpoint = new Uri(subscription.Endpoint);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new ByteArrayContent(body) };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            request.Content.Headers.ContentEncoding.Add("aes128gcm");
            request.Headers.TryAddWithoutValidation("TTL", "86400");
            request.Headers.TryAddWithoutValidation("Urgency", "normal");
            request.Headers.TryAddWithoutValidation("Authorization",
                $"vapid t={_keys.CreateJwt(endpoint.GetLeftPart(UriPartial.Authority))}, k={_keys.PublicKey}");

            try
            {
                using var response = await _http.CreateClient(HttpClientName).SendAsync(request, ct);
                if (response.IsSuccessStatusCode) return PushResult.Sent;
                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone) return PushResult.Gone;

                var detail = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Web push to {Host} failed: {Status} {Detail}",
                    endpoint.Host, (int)response.StatusCode, detail.Length > 300 ? detail[..300] : detail);
                return PushResult.Failed;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Web push to {Host} failed", endpoint.Host);
                return PushResult.Failed;
            }
        }

        /// <summary>RFC 8291: يشفّر الحمولة لمتصفح واحد بمفتاحه العام (uaPublic) وسرّه (authSecret).
        /// الناتج: ترويسة aes128gcm (salt ‖ rs ‖ idlen ‖ مفتاح الخادم المؤقت) ثم النص المشفّر.</summary>
        public static byte[] Encrypt(byte[] plaintext, byte[] uaPublic, byte[] authSecret)
        {
            using var serverKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            return Encrypt(plaintext, uaPublic, authSecret, serverKey, RandomNumberGenerator.GetBytes(16));
        }

        /// <summary>بمفتاح خادم مؤقت وملح محدّدين — للتحقق من مثال RFC 8291 (الملحق A) في الاختبارات.</summary>
        public static byte[] Encrypt(byte[] plaintext, byte[] uaPublic, byte[] authSecret, ECDiffieHellman serverKey, byte[] salt)
        {
            if (uaPublic.Length != 65 || uaPublic[0] != 0x04) throw new ArgumentException("Invalid p256dh key", nameof(uaPublic));
            if (plaintext.Length > RecordSize - 17) throw new ArgumentException("Payload too large", nameof(plaintext));

            var serverParams = serverKey.ExportParameters(false);
            byte[] asPublic = [0x04, .. serverParams.Q.X!, .. serverParams.Q.Y!];

            using var browserKey = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = uaPublic[1..33], Y = uaPublic[33..65] }
            });

            var (cek, nonce) = DeriveKeys(serverKey, browserKey.PublicKey, authSecret, salt, uaPublic, asPublic);

            // سجل واحد أخير: الحمولة ثم الفاصل 0x02 بلا حشو
            var padded = new byte[plaintext.Length + 1];
            plaintext.CopyTo(padded, 0);
            padded[^1] = 0x02;

            var cipher = new byte[padded.Length];
            var tag = new byte[16];
            using (var aes = new AesGcm(cek, 16))
                aes.Encrypt(nonce, padded, cipher, tag);

            var header = new byte[16 + 4 + 1 + asPublic.Length];
            salt.CopyTo(header, 0);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16), RecordSize);
            header[20] = (byte)asPublic.Length;
            asPublic.CopyTo(header, 21);

            return [.. header, .. cipher, .. tag];
        }

        /// <summary>اشتقاق مفتاح المحتوى والـ nonce (RFC 8291 §3.4). مشترك بين التشفير وفك التشفير (في الاختبارات).</summary>
        public static (byte[] Cek, byte[] Nonce) DeriveKeys(ECDiffieHellman ownKey, ECDiffieHellmanPublicKey otherKey,
            byte[] authSecret, byte[] salt, byte[] uaPublic, byte[] asPublic)
        {
            // PRK_key = HMAC-SHA-256(auth_secret, ecdh_secret)
            var prkKey = ownKey.DeriveKeyFromHmac(otherKey, HashAlgorithmName.SHA256, authSecret);
            byte[] keyInfo = [.. "WebPush: info\0"u8, .. uaPublic, .. asPublic];
            var ikm = HKDF.Expand(HashAlgorithmName.SHA256, prkKey, 32, keyInfo);
            var prk = HKDF.Extract(HashAlgorithmName.SHA256, ikm, salt);
            var cek = HKDF.Expand(HashAlgorithmName.SHA256, prk, 16, "Content-Encoding: aes128gcm\0"u8.ToArray());
            var nonce = HKDF.Expand(HashAlgorithmName.SHA256, prk, 12, "Content-Encoding: nonce\0"u8.ToArray());
            return (cek, nonce);
        }
    }
}
