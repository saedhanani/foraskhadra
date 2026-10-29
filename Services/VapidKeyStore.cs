using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Foras_Khadra.Services
{
    public interface IVapidKeyStore
    {
        /// <summary>المفتاح العام (P-256 غير مضغوط، base64url) — يُعطى للمتصفح عند الاشتراك ويُرسل مع كل إشعار.</summary>
        string PublicKey { get; }

        /// <summary>رمز JWT موقّع (ES256) لخدمة الدفع صاحبة الرابط (RFC 8292).</summary>
        string CreateJwt(string audience);
    }

    /// <summary>مفاتيح VAPID لإشعارات الدفع: تُولَّد مرة واحدة وتُحفظ في App_Data/push-vapid.json (مثل مفاتيح حماية البيانات).
    /// لا تحذف هذا الملف من الخادم: تغيير المفتاح يُبطل كل اشتراكات الإشعارات القائمة.</summary>
    public sealed class VapidKeyStore : IVapidKeyStore, IDisposable
    {
        /// <summary>جهة الاتصال التي تطلبها خدمات الدفع (بريد الدعم الظاهر في تذييل الموقع).</summary>
        public const string Subject = "mailto:suppforaskhadra@gmail.com";

        private readonly ECDsa _key;
        private readonly object _signLock = new();

        public string PublicKey { get; }

        public VapidKeyStore(IWebHostEnvironment env)
            : this(Path.Combine(env.ContentRootPath, "App_Data", "push-vapid.json")) { }

        public VapidKeyStore(string path)
        {
            if (File.Exists(path))
            {
                var stored = JsonSerializer.Deserialize<StoredKeys>(File.ReadAllText(path))
                             ?? throw new InvalidOperationException("push-vapid.json is empty");
                var pub = WebEncoders.Base64UrlDecode(stored.PublicKey);
                _key = ECDsa.Create(new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    D = WebEncoders.Base64UrlDecode(stored.PrivateKey),
                    Q = new ECPoint { X = pub[1..33], Y = pub[33..65] }
                });
                PublicKey = stored.PublicKey;
            }
            else
            {
                _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var p = _key.ExportParameters(true);
                PublicKey = WebEncoders.Base64UrlEncode([0x04, .. p.Q.X!, .. p.Q.Y!]);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(new StoredKeys(PublicKey, WebEncoders.Base64UrlEncode(p.D!))));
            }
        }

        public string CreateJwt(string audience)
        {
            var header = WebEncoders.Base64UrlEncode("{\"typ\":\"JWT\",\"alg\":\"ES256\"}"u8.ToArray());
            var claims = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
            {
                aud = audience,
                exp = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds(),
                sub = Subject
            }));
            var unsigned = header + "." + claims;

            byte[] signature;
            lock (_signLock)
            {
                // الصيغة الافتراضية في .NET هي IEEE P1363 (r||s، 64 بايت) وهي المطلوبة في JWT
                signature = _key.SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256);
            }
            return unsigned + "." + WebEncoders.Base64UrlEncode(signature);
        }

        public void Dispose() => _key.Dispose();

        private record StoredKeys(string PublicKey, string PrivateKey);
    }
}
