using Foras_Khadra.Services;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Foras_Khadra.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender;
        private readonly IStringLocalizer<AccountController> _localizer;
        private readonly Foras_Khadra.Services.IReferralService _referrals;
        private readonly Foras_Khadra.Services.IEmailQueue _emails;

        /// <summary>كعكة رمز الإحالة (5-3): تُضبط من ?ref=CODE على صفحة التسجيل وتُستهلك عند إنشاء الحساب.</summary>
        private const string ReferralCookie = "fk_ref";

        public AccountController(
            Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager, IStringLocalizer<AccountController> localizer,
            Foras_Khadra.Services.IReferralService referrals,
            Foras_Khadra.Services.IEmailQueue emails)
        {
            _signInManager = signInManager;
            _emails = emails;
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
            _referrals = referrals;
        }

        // ===== GET: RegisterUser =====
        [HttpGet]
        public IActionResult RegisterUser(string? @ref, string? returnUrl)
        {
            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);
            // رابط دعوة صديق: نحفظ الرمز 30 يومًا حتى لو أكمل التسجيل لاحقًا
            if (!string.IsNullOrWhiteSpace(@ref) && @ref.Length <= 12)
                Response.Cookies.Append(ReferralCookie, @ref.Trim().ToUpperInvariant(),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), IsEssential = true, HttpOnly = true, SameSite = SameSiteMode.Lax });

            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var model = new RegisterViewModel
            {
                Countries = GetCountries(culture),
                Nationalities = GetCountries(culture),
                AvailableInterests = GetAvailableInterests(culture)
            };
            return View(model);
        }

        // ===== POST: RegisterUser =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RegisterUser(RegisterViewModel model, string? returnUrl, [FromServices] IAnalytics analytics)
        {
            returnUrl = SafeReturnUrl(returnUrl);
            ViewData["ReturnUrl"] = returnUrl;
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            model.Countries = GetCountries(culture);
            model.Nationalities = GetCountries(culture);
            model.AvailableInterests = GetAvailableInterests(culture);

            if (!ModelState.IsValid) return View(model);

            if (model.Interests == null || !model.Interests.Any())
            {
                ModelState.AddModelError("Interests", "اختر اهتمام واحد على الأقل");
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // لا نكشف وجود الحساب لمن في المتصفح: الصفحة نفسها، ورسالة لصاحب البريد (دخول/استعادة)
                var exists = Foras_Khadra.Services.AccountEmails.AlreadyRegistered(Lang,
                    Url.Action("Login", "Account", null, Request.Scheme)!, Url.Action("ForgotPassword", "Account", null, Request.Scheme)!);
                _emails.Enqueue(model.Email, exists.Subject, exists.Html);
                TempData["RegisteredEmail"] = model.Email;
                return RedirectToAction("RegisterConfirmation", new { returnUrl });
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                FullName = $"{model.FirstName} {model.LastName}".Trim(),
                Country = model.Country,
                Nationality = model.Nationality,
                Interests = model.Interests,
                Role = UserRole.User,
                CreatedAt = System.DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "User");

            // 5-3 ربط الحساب الجديد بمن دعاه
            if (Request.Cookies.TryGetValue(ReferralCookie, out var refCode))
            {
                await _referrals.ApplyAsync(user, refCode);
                Response.Cookies.Delete(ReferralCookie);
            }

            await SendConfirmationEmailAsync(user, returnUrl);
            // دخول فوري: يتصفح ويحفظ قبل التأكيد؛ التأكيد يلزم عند «قدّم الآن» فقط
            await _signInManager.SignInAsync(user, isPersistent: true);
            analytics.Event("register.done");
            return RedirectToAction("RegisterConfirmation", new { returnUrl });
        }

        [HttpGet]
        public async Task<IActionResult> RegisterConfirmation(string? returnUrl)
        {
            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);
            var user = await _userManager.GetUserAsync(User);
            ViewData["Email"] = user?.Email ?? TempData["RegisteredEmail"] as string;
            return View();
        }

        // ===== تأكيد البريد (المرحلة 0.4): التسجيل يرسل الرابط ويُدخل المستخدم؛ رابط «قدّم الآن» (/go/) يتطلب التأكيد =====
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? code, string? returnUrl, [FromServices] IAnalytics analytics)
        {
            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);
            var token = TokenCodec.Decode(code);
            var user = string.IsNullOrEmpty(userId) ? null : await _userManager.FindByIdAsync(userId);
            if (user == null || token == null)
                return ConfirmView(false, T("الرابط غير صالح.", "This link is not valid.", "Ce lien n'est pas valide."));
            if (user.EmailConfirmed)
                return ConfirmView(true, T("بريدك مؤكَّد من قبل.", "Your email was already confirmed.", "Votre e-mail était déjà confirmé."));
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return ConfirmView(false, T("انتهت صلاحية الرابط أو استُعمل من قبل. أعد الإرسال من حسابك.",
                    "The link expired or was already used. Resend it from your account.",
                    "Le lien a expiré ou a déjà été utilisé. Renvoyez-le depuis votre compte."));
            // دخول تلقائي على الجهاز الذي فُتح فيه الرابط: الرابط يثبت ملكية البريد ولا يعمل إلا مرة (الحساب المؤكَّد يُرفض أعلاه).
            // حساب آخر مفتوح في المتصفح لا يُستبدل.
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == user.Id) await _signInManager.RefreshSignInAsync(user);
            else if (currentUserId == null) await _signInManager.SignInAsync(user, isPersistent: true);
            analytics.Event("email.confirmed");
            return ConfirmView(true, T("شكرًا! بريدك مؤكَّد وروابط التقديم مفتوحة لك الآن.",
                "Thank you! Your email is confirmed and apply links are now open to you.",
                "Merci ! Votre e-mail est confirmé et les liens de candidature vous sont ouverts."));
        }

        /// <summary>تأكيد تغيير بريد الدخول من ProfileSettings — البريد القديم يبقى حتى هنا.</summary>
        [HttpGet]
        public async Task<IActionResult> ConfirmEmailChange(string? userId, string? email, string? code)
        {
            var token = TokenCodec.Decode(code);
            var user = string.IsNullOrEmpty(userId) ? null : await _userManager.FindByIdAsync(userId);
            if (user == null || token == null || string.IsNullOrWhiteSpace(email))
                return ConfirmView(false, T("الرابط غير صالح.", "This link is not valid.", "Ce lien n'est pas valide."));
            var result = await _userManager.ChangeEmailAsync(user, email, token);
            if (result.Succeeded) result = await _userManager.SetUserNameAsync(user, email);
            if (!result.Succeeded)
                return ConfirmView(false, T("انتهت صلاحية الرابط أو البريد مستخدم من قبل.",
                    "The link expired or the email is already in use.",
                    "Le lien a expiré ou l'e-mail est déjà utilisé."));
            if (_userManager.GetUserId(User) == user.Id) await _signInManager.RefreshSignInAsync(user);
            return ConfirmView(true, T("تم تغيير بريد الدخول إلى " + email, "Your sign-in email is now " + email, "Votre e-mail de connexion est maintenant " + email));
        }

        /// <summary>صفحة «أكّد بريدك أولًا» — يصل إليها من /go/ أو من إنشاء فرصة لمنظمة غير مؤكَّدة.</summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ConfirmRequired(string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            if (user.EmailConfirmed) return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
            ViewData["Email"] = user.Email;
            ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResendConfirmation(string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            if (!user.EmailConfirmed) await SendConfirmationEmailAsync(user, returnUrl);
            TempData["ConfirmationSent"] = T("أُعيد إرسال رابط التأكيد إلى " + user.Email, "Confirmation link resent to " + user.Email, "Lien de confirmation renvoyé à " + user.Email);
            return RedirectToAction("ConfirmRequired", new { returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
        }

        private async Task SendConfirmationEmailAsync(ApplicationUser user, string? returnUrl = null)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            // returnUrl في رابط البريد: بعد التأكيد يعود إلى الفرصة التي كان يحاول التقديم عليها
            var link = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = TokenCodec.Encode(code), returnUrl = SafeReturnUrl(returnUrl) }, Request.Scheme)!;
            var mail = Foras_Khadra.Services.AccountEmails.ConfirmEmail(Lang, link, user.FirstName);
            _emails.Enqueue(user.Email!, mail.Subject, mail.Html);
        }

        private IActionResult ConfirmView(bool success, string message)
        {
            ViewData["Success"] = success;
            ViewData["Message"] = message;
            return View("ConfirmEmail");
        }

        private static string Lang => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private static string T(string ar, string en, string fr) => Lang == "en" ? en : Lang == "fr" ? fr : ar;

        /// <summary>returnUrl محلي فقط؛ غير ذلك يُهمل (لا تحويل خارج الموقع).</summary>
        private string? SafeReturnUrl(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        /// <summary>التطبيق المثبّت (كوكي fk_app يضعه _Layout في وضع standalone): الدخول دائم حتى لا يُطلب عند كل فتح.</summary>
        private bool IsInstalledApp => Request.Cookies.ContainsKey("fk_app");


        // ===== GET: Login =====
        [HttpGet]
        [RedirectSignedIn]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);
            return View(new LoginViewModel { RememberMe = true });
        }

        // ===== POST: Login =====
        [HttpPost]
        [RedirectSignedIn]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, [FromServices] IAnalytics analytics)
        {
            returnUrl = SafeReturnUrl(returnUrl);
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
                return View(model);
            }

            // كلمة المرور قبل الدور: لا نكشف نوع الحساب لمن لا يعرف كلمة المرور

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "تم قفل الحساب مؤقتًا بسبب محاولات دخول فاشلة متكررة، حاول مجددًا بعد 15 دقيقة");
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
                return View(model);
            }

            if (await _userManager.IsInRoleAsync(user, "Organization"))
            {
                TempData["OrgLoginAlert"] = _localizer["OrgLoginAlert"].Value;
                TempData["OrgLoginRedirectText"] = _localizer["OrgLoginRedirectText"].Value;
                TempData["OrgLoginRedirect"] = Url.Action("Login", "Organization", new { returnUrl });
                return RedirectToAction("Login", new { returnUrl });
            }
            await _signInManager.SignInAsync(user, isPersistent: model.RememberMe || IsInstalledApp);
            analytics.Event("login.done");
            // جاء من /go أو من نافذة "الدخول مطلوب": نعيده إلى ما كان يفعله بدل لوحة البداية
            if (returnUrl != null) return LocalRedirect(returnUrl);
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else
            {
                // بعد الدخول يرى المستخدم "صفحتي": المقترحة حسب اهتماماته ومحفوظاته
                return RedirectToAction("Dashboard", "User");
            }
        }

        // ===== Logout =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ===== مساعد لجلب الدول والجنسية =====
        private List<string> GetCountries(string culture = null)
        {
            culture ??= CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            var countries = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(c =>
                {
                    try
                    {
                        var region = new RegionInfo(c.Name);
                        // إذا أردنا التصفية حسب الرمز
                        if (region.TwoLetterISORegionName == "IL") // إسرائيل فقط
                            return null;

                        return culture == "ar" ? region.NativeName : region.EnglishName;
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            return countries;
        }


        // ===== مساعد لجلب الاهتمامات حسب اللغة =====
        private List<InterestItem> GetAvailableInterests(string culture)
        {
            return new List<InterestItem>
            {
                new InterestItem { Key = "competitions", DisplayName = culture == "ar" ? "المسابقات" : culture == "fr" ? "Concours" : "Competitions" },
                new InterestItem { Key = "conferences", DisplayName = culture == "ar" ? "المؤتمرات" : culture == "fr" ? "Conférences" : "Conferences" },
                new InterestItem { Key = "volunteer_opportunities", DisplayName = culture == "ar" ? "فرص التطوع" : culture == "fr" ? "Opportunités de bénévolat" : "Volunteer Opportunities" },
                new InterestItem { Key = "jobs", DisplayName = culture == "ar" ? "الوظائف" : culture == "fr" ? "Emplois" : "Jobs" },
                new InterestItem { Key = "grants", DisplayName = culture == "ar" ? "المنح" : culture == "fr" ? "Bourses" : "Scholarships" },
                new InterestItem { Key = "fellowships", DisplayName = culture == "ar" ? "الزمالات" : culture == "fr" ? "Bourses de recherche" : "Fellowships" },
                new InterestItem { Key = "training_opportunities", DisplayName = culture == "ar" ? "فرص التدريب" : culture == "fr" ? "Opportunités de formation" : "Training Opportunities" }
            };
        }


        // ==== Forgot Password ====
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
                return View();

            //  نجيب المستخدم
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return View("ForgotPasswordConfirmation");
            // مهم: ما نفضح إذا الإيميل موجود أو لا

            //  توليد Token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            //  توليد رابط إعادة التعيين
            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { email = user.Email, token = token },
                Request.Scheme
            );

            string emailBody = $@"
<!DOCTYPE html>
<html lang='{CultureInfo.CurrentCulture.TwoLetterISOLanguageName}' dir='{(CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ar" ? "rtl" : "ltr")}'>
<head>
    <meta charset='UTF-8'>
    <title>{_localizer["ResetPasswordSubject"]}</title>
</head>
<body style='font-family: Tahoma, Arial, sans-serif; background-color: #f4f6f8; padding: 20px;'>
    <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 8px;'>

        <h2 style='text-align: center; color: #2c3e50;'>Foras Khadra</h2>

        <p>{_localizer["ResetPasswordGreeting"]}</p>

        <p>{_localizer["ResetPasswordBody"]}</p>

        <div style='text-align: center; margin: 30px 0;'>
            <a href='{resetLink}'
               style='background-color: #28a745; color: #ffffff; padding: 12px 30px;
                      text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: bold;'>
                {_localizer["ResetPasswordButton"]}
            </a>
        </div>

        <p>{_localizer["ResetPasswordIgnore"]}</p>

        <p>{_localizer["ResetPasswordRegards"]}</p>

        <hr style='margin-top: 30px;' />

        <p style='font-size: 12px; color: #777; text-align: center;'>
            {_localizer["ResetPasswordFooter"]}
        </p>
    </div>
</body>
</html>
";

            //  إرسال الإيميل
            string emailSubject = $"{_localizer["SiteName"]} - {_localizer["ResetPasswordSubject"]}";

            _emails.Enqueue(user.Email!, emailSubject, emailBody); // في الخلفية: لا يكشف زمن الرد وجود الحساب ولا يفشل الطلب بتعطّل SMTP

            //  صفحة تأكيد الإرسال
            return View("ForgotPasswordConfirmation");
        }


        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToAction("ForgotPassword");

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction("ResetPasswordConfirmation"); // لا نكشف إذا الإيميل موجود

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                // كلمة المرور نجحت → Redirect للـ Confirmation
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // إذا فشل، اعرض كل الأخطاء (Token منتهي أو PasswordPolicy)
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProfileSettings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Country = user.Country,
                Nationality = user.Nationality,
                Interests = user.Interests,
                Countries = GetCountries(culture),
                Nationalities = GetCountries(culture),
                AvailableInterests = GetAvailableInterests(culture)
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfileSettings(EditProfileViewModel model)
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            model.Countries = GetCountries(culture);
            model.Nationalities = GetCountries(culture);
            model.AvailableInterests = GetAvailableInterests(culture);

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.FullName = $"{model.FirstName} {model.LastName}";
            user.Country = model.Country;
            user.Nationality = model.Nationality;
            user.Interests = model.Interests;

            var emailChangeRequested = !string.Equals(user.Email, model.Email?.Trim(), StringComparison.OrdinalIgnoreCase);
            if (emailChangeRequested)
            {
                // تغيير بريد الدخول: كلمة المرور الحالية + رابط تأكيد إلى العنوان الجديد؛ البريد الحالي يبقى حتى التأكيد
                if (string.IsNullOrEmpty(model.CurrentPassword) || !await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", T("أدخل كلمة المرور الحالية لتغيير البريد", "Enter your current password to change the email", "Saisissez votre mot de passe actuel pour changer l'e-mail"));
                    model.Email = user.Email!;
                    return View(model);
                }
                var newEmail = model.Email!.Trim();
                if (await _userManager.FindByEmailAsync(newEmail) != null)
                {
                    ModelState.AddModelError("Email", _localizer["EmailAlreadyUsed"]);
                    model.Email = user.Email!;
                    return View(model);
                }
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                var link = Url.Action("ConfirmEmailChange", "Account", new { userId = user.Id, email = newEmail, code = TokenCodec.Encode(code) }, Request.Scheme)!;
                var mail = Foras_Khadra.Services.AccountEmails.ChangeEmail(Lang, link, newEmail);
                _emails.Enqueue(newEmail, mail.Subject, mail.Html);
            }
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                foreach (var e in update.Errors) ModelState.AddModelError("", e.Description);
                model.Email = user.Email!;
                return View(model);
            }
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = emailChangeRequested
                ? T("حُفظت بياناتك. أرسلنا رابط تأكيد إلى البريد الجديد؛ يبقى بريدك الحالي حتى تؤكده.", "Saved. We sent a confirmation link to the new email; your current email stays until you confirm it.", "Enregistré. Un lien de confirmation a été envoyé au nouvel e-mail ; l'actuel reste actif jusqu'à confirmation.")
                : _localizer["ProfileUpdatedSuccess"].Value;
            return RedirectToAction("ProfileSettings");
        }


    }
}