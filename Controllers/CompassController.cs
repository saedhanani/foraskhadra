using System.Globalization;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// «بوصلة»: الزائر يجيب عن 8 أسئلة في دقيقتين (/compass) فيرى كم فرصة يستطيع التقديم عليها الآن (/compass/result)،
    /// ويسجّل ببريد وكلمة مرور (/compass/register) لتصير له صفحة حيّة (/compass/me): الآن / قريبًا / جاهزيتك / خطة الأسبوع.
    /// إجابات الزائر في كوكي fk_compass حتى التسجيل، ثم في EligibilityProfiles.
    /// </summary>
    public class CompassController : Controller
    {
        private const string ReferralCookie = "fk_ref";

        private readonly ApplicationDbContext _db;
        private readonly ICompassService _compass;
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly ISavedOpportunityService _saved;
        private readonly IReferralService _referrals;
        private readonly IEmailQueue _emails;
        private readonly IAnalytics _analytics;

        public CompassController(ApplicationDbContext db, ICompassService compass, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
            ISavedOpportunityService saved, IReferralService referrals, IEmailQueue emails, IAnalytics analytics)
        {
            _db = db;
            _compass = compass;
            _users = users;
            _signIn = signIn;
            _saved = saved;
            _referrals = referrals;
            _emails = emails;
            _analytics = analytics;
        }

        private static string Lang => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private static string T(string ar, string en, string fr) => Lang == "en" ? en : Lang == "fr" ? fr : ar;
        private bool IsIndividual => User.Identity?.IsAuthenticated == true && !User.IsInRole("Organization");

        // ---------------------------------------------------------------- الأسئلة

        [HttpGet("/{culture:culture}/compass")]
        [HttpGet("/compass", Order = 1)]
        public async Task<IActionResult> Index(string? @ref, bool edit = false)
        {
            if (!string.IsNullOrWhiteSpace(@ref) && @ref.Length <= 12)
                Response.Cookies.Append(ReferralCookie, @ref.Trim().ToUpperInvariant(),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), IsEssential = true, HttpOnly = true, SameSite = SameSiteMode.Lax });

            CompassAnswers answers;
            ApplicationUser? user = null;
            if (IsIndividual)
            {
                user = await _users.GetUserAsync(User);
                var profile = user == null ? null : await _compass.GetProfileAsync(user.Id);
                if (profile != null && !edit) return RedirectToAction(nameof(Me));
                answers = profile?.ToAnswers() ?? CompassCookie.Read(Request) ?? new CompassAnswers();
                if (user != null) Prefill(answers, user, await _db.Countries.AsNoTracking().ToListAsync());
            }
            else
            {
                answers = CompassCookie.Read(Request) ?? new CompassAnswers();
                if (!edit) _analytics.Event("compass.start");
            }
            return View(await PageAsync(answers, user != null, edit));
        }

        [HttpPost("/{culture:culture}/compass")]
        [HttpPost("/compass", Order = 1)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind(Prefix = "A")] CompassAnswers a)
        {
            a = a.Normalized();
            if (a.Goals.Count == 0)
            {
                ModelState.AddModelError("A.Goals", T("اختر هدفًا واحدًا على الأقل.", "Pick at least one goal.", "Choisissez au moins un objectif."));
                return View(await PageAsync(a, IsIndividual, true));
            }

            if (IsIndividual)
            {
                var user = await _users.GetUserAsync(User);
                if (user != null)
                {
                    await _compass.SaveAsync(user, a);
                    CompassCookie.Delete(Response);
                    return RedirectToAction(nameof(Me));
                }
            }
            CompassCookie.Write(Response, a);
            return RedirectToAction(nameof(Result));
        }

        // ---------------------------------------------------------------- نتيجة الزائر (بوابة التسجيل)

        [HttpGet("/{culture:culture}/compass/result")]
        [HttpGet("/compass/result", Order = 1)]
        public async Task<IActionResult> Result()
        {
            if (IsIndividual) return RedirectToAction(nameof(Me));
            var answers = CompassCookie.Read(Request);
            if (answers == null || answers.IsEmpty) return RedirectToAction(nameof(Index));
            _analytics.Event("compass.result");
            return View(new CompassResultVM { Result = await _compass.BuildAsync(answers, Lang), Answers = answers });
        }

        // ---------------------------------------------------------------- «بوصلتي»

        [Authorize]
        [HttpGet("/{culture:culture}/compass/me")]
        [HttpGet("/compass/me", Order = 1)]
        public async Task<IActionResult> Me()
        {
            if (User.IsInRole("Organization")) return RedirectToAction("OrgOpportunities", "Opportunity");
            var user = await _users.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var profile = await _compass.GetProfileAsync(user.Id);
            if (profile == null)
            {
                // سجّل أو دخل بعد أن أجاب كزائر: نستورد إجاباته ولا يضيع شيء
                var fromCookie = CompassCookie.Read(Request);
                if (fromCookie == null || fromCookie.IsEmpty) return RedirectToAction(nameof(Index));
                profile = await _compass.SaveAsync(user, fromCookie);
                CompassCookie.Delete(Response);
            }

            var answers = profile.ToAnswers();
            var result = await _compass.BuildAsync(answers, Lang);
            _analytics.Event("compass.me");
            var code = await _referrals.EnsureCodeAsync(user);
            // أدلة التقديم المنشورة (4.1): رابط الدليل تحت كل برنامج في «قريبًا»
            ViewBag.GuideSlugs = await _db.ApplicationGuides.AsNoTracking().Where(g => g.IsPublished).ToDictionaryAsync(g => g.ProgramKey, g => g.Slug);
            return View(new CompassMeVM
            {
                Result = result,
                Answers = answers,
                FirstName = string.IsNullOrWhiteSpace(user.FirstName) ? user.FullName : user.FirstName,
                SavedIds = await _saved.SavedIdsAsync(user.Id, result.Now.Select(m => m.Opportunity.Id)),
                ReferralCode = code,
                ShareUrl = $"{Request.Scheme}://{Request.Host}{CultureRouting.Prefix(Lang)}/compass?ref={code}"
            });
        }

        /// <summary>جاهزيتك: تعليم ورقة جاهزة أو إلغاؤها.</summary>
        [Authorize]
        [HttpPost("/{culture:culture}/compass/documents")]
        [HttpPost("/compass/documents", Order = 1)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Document(string key, bool have)
        {
            var userId = _users.GetUserId(User);
            if (userId != null) await _compass.SetDocumentAsync(userId, key, have);
            return Redirect(Url.Action(nameof(Me)) + "#readiness");
        }

        // ---------------------------------------------------------------- تسجيل مختصر

        [HttpGet("/{culture:culture}/compass/register")]
        [HttpGet("/compass/register", Order = 1)]
        public IActionResult Register()
        {
            if (IsIndividual) return RedirectToAction(nameof(Me));
            var answers = CompassCookie.Read(Request);
            if (answers == null || answers.IsEmpty) return RedirectToAction(nameof(Index));
            return View(new CompassRegisterVM());
        }

        [HttpPost("/{culture:culture}/compass/register")]
        [HttpPost("/compass/register", Order = 1)]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(CompassRegisterVM model)
        {
            if (IsIndividual) return RedirectToAction(nameof(Me));
            var answers = CompassCookie.Read(Request);
            if (answers == null || answers.IsEmpty) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim();
            var meUrl = Url.Action(nameof(Me))!;
            if (await _users.FindByEmailAsync(email) != null)
            {
                // لا نكشف وجود الحساب لمن في المتصفح؛ صاحب البريد يستلم رسالة (كما في التسجيل العادي)
                var exists = AccountEmails.AlreadyRegistered(Lang,
                    Url.Action("Login", "Account", new { returnUrl = meUrl }, Request.Scheme)!,
                    Url.Action("ForgotPassword", "Account", null, Request.Scheme)!);
                _emails.Enqueue(email, exists.Subject, exists.Html);
                TempData["CompassRegistered"] = email;
                return RedirectToAction("Login", "Account", new { returnUrl = meUrl });
            }

            var countries = await _db.Countries.AsNoTracking().ToListAsync();
            string? NameFor(string? iso) => iso == null ? null : countries.FirstOrDefault(c => string.Equals(c.IsoCode, iso, StringComparison.OrdinalIgnoreCase))?.NameAr ?? CountryHelper.NameForIso(iso, "ar");
            var firstName = NameFromEmail(email);
            var user = new ApplicationUser
            {
                UserName = email, Email = email,
                FirstName = firstName, LastName = string.Empty, FullName = firstName,
                Country = NameFor(answers.Residence) ?? string.Empty,
                Nationality = NameFor(answers.Nationality) ?? string.Empty,
                Interests = answers.GoalTypes().Select(t => InterestMap.ToType.FirstOrDefault(kv => kv.Value == t).Key).Where(k => k != null).Distinct().ToList(),
                Role = UserRole.User,
                CreatedAt = DateTime.Now
            };
            var created = await _users.CreateAsync(user, model.Password);
            if (!created.Succeeded)
            {
                foreach (var e in created.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }
            await _users.AddToRoleAsync(user, "User");

            if (Request.Cookies.TryGetValue(ReferralCookie, out var refCode))
            {
                await _referrals.ApplyAsync(user, refCode);
                Response.Cookies.Delete(ReferralCookie);
            }

            // تأكيد البريد يلزم عند «قدّم الآن» فقط؛ رابط البريد يعيده إلى بوصلته
            var token = await _users.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = TokenCodec.Encode(token), returnUrl = meUrl }, Request.Scheme)!;
            var mail = AccountEmails.ConfirmEmail(Lang, link, user.FirstName);
            _emails.Enqueue(email, mail.Subject, mail.Html);

            await _signIn.SignInAsync(user, isPersistent: true);
            await _compass.SaveAsync(user, answers);
            CompassCookie.Delete(Response);
            _analytics.Event("compass.register");
            _analytics.Event("register.done");
            return RedirectToAction(nameof(Me));
        }

        // ---------------------------------------------------------------- مساعدات

        private async Task<CompassIndexVM> PageAsync(CompassAnswers answers, bool isMember, bool isEdit)
        {
            var lang = Lang;
            var countries = await _db.Countries.AsNoTracking().Where(c => c.IsoCode != null).ToListAsync();
            string Name(Country c) => lang == "en" ? c.NameEn : lang == "fr" ? c.NameFr : c.NameAr;
            var comparer = StringComparer.Create(CultureInfo.CurrentUICulture, true);
            var arab = countries.Where(c => CompassMatcher.ArabIso.Contains(c.IsoCode!))
                .Select(c => new CountryOption(c.IsoCode!.ToUpperInvariant(), Name(c))).OrderBy(o => o.Name, comparer).ToList();

            // بلدان الاستضافة: الشائعة في الفرص المنشورة + ما اختاره سابقًا
            var hostIso = new[] { "DE", "GB", "US", "CA", "FR", "TR", "NL", "SE", "IT", "ES", "JP", "KR", "CN", "MY", "AU", "CH", "AT", "BE", "QA", "AE", "SA", "EG", "JO", "MA", "TN" }
                .Concat(answers.HostCountries).Distinct(StringComparer.OrdinalIgnoreCase);
            var hosts = hostIso.Select(iso => new CountryOption(iso.ToUpperInvariant(), CountryHelper.NameForIso(iso, lang) ?? iso)).OrderBy(o => o.Name, comparer).ToList();

            return new CompassIndexVM
            {
                A = answers,
                ArabCountries = arab,
                HostOptions = hosts,
                Fields = await _db.Fields.AsNoTracking().Where(f => f.Key != Field.AnyKey).OrderBy(f => f.SortOrder).ToListAsync(),
                IsMember = isMember,
                IsEdit = isEdit
            };
        }

        /// <summary>السؤال الذكي: لا نسأل العضو ما نعرفه — بلده وجنسيته واهتماماته من ملفه.</summary>
        private static void Prefill(CompassAnswers a, ApplicationUser user, List<Country> countries)
        {
            a.Residence ??= MatchScorer.ResolveCountry(user.Country, countries)?.IsoCode;
            a.Nationality ??= MatchScorer.ResolveCountry(user.Nationality, countries)?.IsoCode;
            if (a.Goals.Count == 0) a.Goals = InterestMap.TypesFor(user.Interests).Select(t => t.ToString()).ToList();
        }

        private static string NameFromEmail(string email)
        {
            var local = email.Split('@')[0];
            var word = new string(local.TakeWhile(ch => char.IsLetter(ch)).ToArray());
            return word.Length >= 2 ? char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant() : local;
        }
    }
}
