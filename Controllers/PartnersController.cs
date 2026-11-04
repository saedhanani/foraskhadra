using System.Globalization;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// بوابة المنظمات /for-organizations: الوعد ("انشر مجانًا خلال 48 ساعة بثلاث لغات وتصل إلى المؤهَّلين فقط")،
    /// وطلب نشر بلا حساب (اسم الجهة، البريد، رابط الفرصة) يصبح PublishRequest تجلبه منصة الكتابة من GET /api/team/requests.
    /// </summary>
    public class PartnersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPublishRequestService _requests;
        private readonly IAudienceEstimator _audience;
        private readonly IAnalytics _analytics;

        public PartnersController(ApplicationDbContext db, IPublishRequestService requests, IAudienceEstimator audience, IAnalytics analytics)
        {
            _db = db;
            _requests = requests;
            _audience = audience;
            _analytics = analytics;
        }

        private static string Lang => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private static string T(string ar, string en, string fr) => Lang == "en" ? en : Lang == "fr" ? fr : ar;

        [HttpGet("/{culture:culture}/for-organizations")]
        [HttpGet("/for-organizations", Order = 1)]
        public async Task<IActionResult> Index() => View(await PageAsync(new PublishRequestForm()));

        [HttpPost("/{culture:culture}/for-organizations")]
        [HttpPost("/for-organizations", Order = 1)]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Index([Bind(Prefix = "Form")] PublishRequestForm form)
        {
            // فخ الروبوتات: نتظاهر بالنجاح حتى لا يتعلم المرسل الآلي
            if (!string.IsNullOrEmpty(form.Fax)) return RedirectToAction(nameof(Index), new { sent = 1 });

            var orgName = (form.OrganizationName ?? "").Trim();
            var email = (form.ContactEmail ?? "").Trim();
            var sourceUrl = HttpUrl.Normalize(form.SourceUrl);
            var website = string.IsNullOrWhiteSpace(form.Website) ? null : HttpUrl.Normalize(form.Website);

            if (orgName.Length < 2) ModelState.AddModelError("Form.OrganizationName", T("اكتب اسم الجهة.", "Enter the organization name.", "Indiquez le nom de l'organisation."));
            if (!IsEmail(email)) ModelState.AddModelError("Form.ContactEmail", T("اكتب بريدًا صحيحًا لنرسل إليه رابط الفرصة بعد نشرها.", "Enter a valid email; we send the published link there.", "Indiquez un e-mail valide ; nous y enverrons le lien publié."));
            if (sourceUrl == null) ModelState.AddModelError("Form.SourceUrl", T("اكتب رابط الفرصة كاملًا (يبدأ بـ https://).", "Enter the full opportunity link (starting with https://).", "Indiquez le lien complet de l'opportunité (commençant par https://)."));
            if (!string.IsNullOrWhiteSpace(form.Website) && website == null) ModelState.AddModelError("Form.Website", T("رابط الموقع غير صحيح.", "The website link is not valid.", "Le lien du site n'est pas valide."));

            var type = ParseType(form.Type);
            var countryIso = await ValidCountryAsync(form.Country);
            if (!ModelState.IsValid) return View(await PageAsync(form));

            var estimate = await _audience.EstimateAsync(type, countryIso);
            await _requests.CreateAsync(new PublishRequest
            {
                OrganizationName = orgName,
                ContactName = Trim(form.ContactName, 120),
                ContactEmail = email,
                Website = website,
                SourceUrl = sourceUrl!,
                Notes = Trim(form.Notes, 2000),
                TypeHint = type,
                CountryIso = countryIso,
                Language = Lang,
                EstimatedReach = estimate
            });

            _analytics.Event("publish_request.sent");
            TempData["RequestSentTo"] = email;
            TempData["RequestReach"] = estimate;
            return RedirectToAction(nameof(Index), new { sent = 1 });
        }

        /// <summary>"ستصل فرصتك إلى ~N عضو مؤهَّل" — يُحدَّث في النموذج مع تغيير النوع أو الجمهور.</summary>
        [HttpGet("/{culture:culture}/for-organizations/estimate")]
        [HttpGet("/for-organizations/estimate", Order = 1)]
        [EnableRateLimiting("api")]
        public async Task<IActionResult> Estimate(string? type, string? country)
        {
            var members = await _audience.EstimateAsync(ParseType(type), await ValidCountryAsync(country));
            return Json(new { members });
        }

        private async Task<PartnersPageVM> PageAsync(PublishRequestForm form)
        {
            var lang = Lang;
            var countries = await _db.Countries.AsNoTracking()
                .Where(c => c.IsoCode != null && c.IsoCode != FacetParser.ArabWorldCode && c.IsoCode != "XW")
                .ToListAsync();
            var options = new List<CountryOption>
            {
                new(FacetParser.ArabWorldCode, T("كل الوطن العربي", "The whole Arab world", "Tout le monde arabe"))
            };
            options.AddRange(countries
                .Where(c => AudienceEstimator.ArabIso.Contains(c.IsoCode!))
                .Select(c => new CountryOption(c.IsoCode!.ToUpperInvariant(), lang == "en" ? c.NameEn : lang == "fr" ? c.NameFr : c.NameAr))
                .OrderBy(o => o.Name, StringComparer.Create(CultureInfo.CurrentUICulture, true)));
            options.Add(new("XW", T("الجميع (داخل الوطن العربي وخارجه)", "Everyone (inside and outside the Arab world)", "Tout le monde (monde arabe et au-delà)")));

            return new PartnersPageVM
            {
                Form = form,
                Countries = options,
                Numbers = await _audience.NumbersAsync(),
                Estimate = await _audience.EstimateAsync(null, null)
            };
        }

        private static OpportunityType? ParseType(string? type) =>
            !string.IsNullOrWhiteSpace(type) && Enum.TryParse<OpportunityType>(type.Trim(), true, out var t) ? t : null;

        /// <summary>رمز معروف في جدول الدول (أو XA/XW)، وإلا null.</summary>
        private async Task<string?> ValidCountryAsync(string? iso)
        {
            if (string.IsNullOrWhiteSpace(iso)) return null;
            iso = iso.Trim().ToUpperInvariant();
            if (iso.Length != 2) return null;
            if (iso == FacetParser.ArabWorldCode || iso == "XW") return iso;
            return await _db.Countries.AsNoTracking().AnyAsync(c => c.IsoCode == iso) ? iso : null;
        }

        private static bool IsEmail(string value) =>
            value.Length is >= 5 and <= 256 && System.Net.Mail.MailAddress.TryCreate(value, out var m) && m.Address == value && value.Contains('.');

        private static string? Trim(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > max ? value[..max] : value;
        }
    }
}
