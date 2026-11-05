using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// /.well-known/assetlinks.json (المرحلة 4.7): يربط تطبيق Android (TWA من PWABuilder) بالموقع حتى يفتح بلا شريط عنوان.
    /// الملفات الثابتة لا تخدم المجلدات التي تبدأ بنقطة، لذا يُولَّد من الإعدادات:
    /// Android:PackageName (مثل com.foraskhadra.app) وAndroid:Sha256Fingerprints (قائمة بصمات مفتاح التوقيع من Play Console).
    /// بلا إعدادات يعيد مصفوفة فارغة (صالحة) فلا يكسر شيئًا.
    /// </summary>
    [AllowAnonymous]
    public class WellKnownController : Controller
    {
        private readonly IConfiguration _config;

        public WellKnownController(IConfiguration config) => _config = config;

        [HttpGet("/.well-known/assetlinks.json")]
        [ResponseCache(Duration = 3600)]
        public IActionResult AssetLinks()
        {
            var package = _config["Android:PackageName"];
            var fingerprints = _config.GetSection("Android:Sha256Fingerprints").Get<string[]>() ?? Array.Empty<string>();
            fingerprints = fingerprints.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim().ToUpperInvariant()).ToArray();

            var statements = string.IsNullOrWhiteSpace(package) || fingerprints.Length == 0
                ? Array.Empty<object>()
                : new object[]
                {
                    new
                    {
                        relation = new[] { "delegate_permission/common.handle_all_urls" },
                        target = new { @namespace = "android_app", package_name = package.Trim(), sha256_cert_fingerprints = fingerprints }
                    }
                };
            return Json(statements);
        }
    }
}
