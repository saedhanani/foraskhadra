using CountryData;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Resources.Views.Organization;
using Foras_Khadra.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Nager.Country;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Foras_Khadra.Controllers
{
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<OrganizationController> _localizer;

        public OrganizationController(
            ApplicationDbContext context,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender, IStringLocalizer<OrganizationController> localizer)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
        }

        private List<SelectListItem> GetCountries(string selectedCountry = null)
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            var countries = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(c =>
                {
                    try
                    {
                        var region = new RegionInfo(c.Name);
                        string name = culture switch
                        {
                            "ar" => region.NativeName,
                            "fr" => region.EnglishName,
                            _ => region.EnglishName
                        };
                        return new SelectListItem
                        {
                            Value = culture switch
                            {
                                "ar" => region.NativeName,
                                "fr" => region.EnglishName,
                                _ => region.EnglishName
                            },
                            Text = name,
                            Selected = region.TwoLetterISORegionName == selectedCountry
                        };
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(x => x != null)
                .DistinctBy(x => x.Value)
                .OrderBy(x => x.Text)
                .ToList();

            // هنا نفلتر إسرائيل
            countries = countries.Where(c => c.Value != "IL").ToList();

            return countries;
        }

        // ====== REGISTER ======
        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterOrganizationViewModel
            {
                Countries = GetCountries()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(RegisterOrganizationViewModel model, [FromServices] Foras_Khadra.Services.IEmailQueue emails)
        {
            model.Countries = GetCountries();

            if (!ModelState.IsValid)
                return View(model);

            // تحقق من أن الإيميل غير مستخدم
            var existingUser = await _userManager.FindByEmailAsync(model.ContactEmail);
            if (existingUser != null)
            {
                // لا نكشف وجود الحساب لمن في المتصفح: صفحة النجاح نفسها، ورسالة لصاحب البريد
                var exists = Foras_Khadra.Services.AccountEmails.AlreadyRegistered(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
                    Url.Action("Login", "Organization", null, Request.Scheme)!, Url.Action("ForgotPassword", "Organization", null, Request.Scheme)!);
                emails.Enqueue(model.ContactEmail, exists.Subject, exists.Html);
                return RedirectToAction("RegisterSuccess");
            }


            // تحقق من قوة كلمة المرور
            if (string.IsNullOrWhiteSpace(model.Password) ||
                model.Password.Length < 8 ||
                !model.Password.Any(char.IsUpper) ||
                !model.Password.Any(char.IsLower) ||
                !model.Password.Any(char.IsDigit) ||
                !model.Password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                ModelState.AddModelError("Password", _localizer["PasswordRules"].Value);
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", _localizer["PasswordMismatch"].Value);
                return View(model);
            }

            // إنشاء Identity User
            var identityUser = new ApplicationUser
            {
                UserName = model.ContactEmail,
                Email = model.ContactEmail
            };

            var createResult = await _userManager.CreateAsync(identityUser, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            // إضافة الدور Organization
            if (!await _userManager.IsInRoleAsync(identityUser, "Organization"))
            {
                await _userManager.AddToRoleAsync(identityUser, "Organization");
            }

            // رابط تأكيد البريد: إنشاء الفرص يتطلب بريدًا مؤكَّدًا (OpportunityController.Create)
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                var link = Url.Action("ConfirmEmail", "Account", new { userId = identityUser.Id, code = TokenCodec.Encode(code) }, Request.Scheme)!;
                var mail = Foras_Khadra.Services.AccountEmails.ConfirmEmail(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, link, model.ContactName);
                emails.Enqueue(identityUser.Email!, mail.Subject, mail.Html);
            }

            // إنشاء Organization بدون لمس الباسورد (Password يُخزن hashed في IdentityUser)
            var organization = new Organization
            {
                Name = model.Name,
                Sector = model.Sector,
                Country = model.Country,
                Location = model.Location ?? "",
                ContactName = model.ContactName,
                ContactEmail = model.ContactEmail,
                PhoneNumber = model.PhoneNumber,
                Website = HttpUrl.Normalize(model.Website),
                CreatedAt = DateTime.Now,
                UserId = identityUser.Id,
                Password = new PasswordHasher<Organization>().HashPassword(null, model.Password) // ← مهم

            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            return RedirectToAction("RegisterSuccess");
        }

        [HttpGet]
        public IActionResult RegisterSuccess() => View();

        // ====== LOGIN ======
        [HttpGet]
        [RedirectSignedIn]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            return View(new OrganizationLoginViewModel { RememberMe = true });
        }

        [HttpPost]
        [RedirectSignedIn]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(OrganizationLoginViewModel model, string? returnUrl)
        {
            returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", _localizer["InvalidCredentials"].Value);
                return View(model);
            }

            // التحقق من الدور
            var isOrganization = await _userManager.IsInRoleAsync(user, "Organization");
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isNormalUser = await _userManager.IsInRoleAsync(user, "User"); // المستخدم العادي


            // كلمة المرور قبل الدور: لا نكشف نوع الحساب لمن لا يعرف كلمة المرور
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", _localizer["AccountLockedOut"].Value);
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", _localizer["InvalidCredentials"].Value);
                return View(model);
            }

            if (isNormalUser)
            {
                TempData["UserLoginAlert"] = _localizer["UserLoginAlert"].Value;
                TempData["UserLoginRedirectText"] = _localizer["UserLoginRedirectText"].Value;
                TempData["UserLoginRedirect"] = Url.Action("Login", "Account", new { returnUrl });
                return RedirectToAction("Login", new { returnUrl });
            }
            // "تذكرني" كان يُعرض ولا يُقرأ؛ والتطبيق المثبّت (كوكي fk_app) دخوله دائم
            await _signInManager.SignInAsync(user, isPersistent: model.RememberMe || Request.Cookies.ContainsKey("fk_app"));
            if (returnUrl != null) return LocalRedirect(returnUrl);
            // توجيه الادمن للوحة الإدارة والمنظمة للوحة التحكم الخاصة بها
            if (isAdmin)
                return RedirectToAction("Dashboard", "Admin");
            else
                return RedirectToAction("Index", "Home");
        }

        public IActionResult Dashboard() => RedirectToAction("OrgOpportunities", "Opportunity"); // لوحة المنظمة هي قائمة فرصها

        // ===== تقرير الوصول (3.2): مشاهدات فرص المنظمة ونقرات «قدّم الآن» خلال 7 أو 30 يومًا؛ CSV، والـPDF بطباعة المتصفح =====
        [Authorize(Roles = "Organization")]
        [HttpGet]
        public async Task<IActionResult> Report([FromServices] Foras_Khadra.Services.IOrgReachReport reach, int days = 30, string? format = null)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var report = await reach.BuildAsync(userId, days, lang);
            if (report == null) return RedirectToAction("ProfileSetting");

            if (format == "csv")
            {
                string T(string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;
                var sb = new System.Text.StringBuilder();
                sb.Append('﻿');
                sb.AppendLine(string.Join(",", T("الفرصة", "Opportunity", "Opportunité"), T("الحالة", "Status", "Statut"), T("الموعد النهائي", "Deadline", "Date limite"),
                    T("المشاهدات", "Views", "Vues"), T("نقرات التقديم", "Apply clicks", "Clics"), T("نسبة النقر %", "Click rate %", "Taux de clic %"), T("محفوظة", "Saves", "Enregistrements")));
                static string Csv(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
                foreach (var r in report.Rows)
                    sb.AppendLine(string.Join(",", Csv(r.Title), r.Status, r.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", r.Views, r.Clicks, r.ClickRatePercent, r.Saves));
                sb.AppendLine();
                sb.AppendLine(string.Join(",", Csv(T("المجموع", "Total", "Total")), "", "", report.Views, report.Clicks, report.ClickRatePercent, report.Saves));
                var name = $"foraskhadra-reach-{report.From:yyyy-MM-dd}-{report.To:yyyy-MM-dd}.csv";
                return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", name);
            }

            return View(report);
        }

        /// <summary>«تابع هذه المنظمة» (3.2) — للأعضاء؛ يرد JSON لطلبات fetch وإلا يعود إلى الصفحة.</summary>
        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> ToggleFollow(int id, string? returnUrl, [FromServices] Foras_Khadra.Services.IOrganizationFollowService follows)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();
            var following = await follows.ToggleAsync(userId, id);
            if (Request.Headers.XRequestedWith == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
                return Json(new { following, count = await follows.FollowerCountAsync(id) });
            return LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("DetailsOrganization", "Home", new { id })!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ====== PROFILE SETTING ======
        [Authorize(Roles = "Organization")]
        [HttpGet]
        public async Task<IActionResult> ProfileSetting()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return RedirectToAction("Login");

            var organization = await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organization == null)
                return NotFound();

            var model = new OrganizationProfileViewModel
            {
                Name = organization.Name ?? "",
                Sector = organization.Sector ?? "",
                Country = organization.Country ?? "",
                ContactEmail = organization.ContactEmail ?? "",
                PhoneNumber = organization.PhoneNumber ?? "",
                Location = organization.Location ?? "",
                ContactName = organization.ContactName ?? "",
                Website = organization.Website ?? "",
                // ← هنا نمرر الدولة الحالية لتظهر محددة بشكل صحيح
                Countries = GetCountries(organization.Country)
            };

            return View(model);
        }

        [Authorize(Roles = "Organization")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfileSetting(OrganizationProfileViewModel model)
        {
            // لا نحتاج تمرير selectedCountry هنا، لأنه موجود في model.Country
            model.Countries = GetCountries(model.Country);
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organization == null)
                return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(organization.UserId);
            if (user == null)
                return RedirectToAction("Login");

            // بريد الدخول لا يتغير من هنا (كان يُغيَّر بلا كلمة مرور ولا تأكيد ويكسر فهرس الاسم الفريد)؛ المدير يعدّله من لوحة الإدارة

            // باقي البيانات
            organization.Name = model.Name ?? organization.Name;
            organization.Sector = model.Sector ?? organization.Sector;
            organization.Country = model.Country ?? organization.Country;
            organization.Location = model.Location ?? organization.Location;
            organization.ContactName = model.ContactName ?? organization.ContactName;
            organization.PhoneNumber = model.PhoneNumber ?? organization.PhoneNumber;
            organization.Website = HttpUrl.Normalize(model.Website) ?? organization.Website;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تحديث البيانات بنجاح!";
            return RedirectToAction("ProfileSetting");
        }


        // ====== FORGOT / RESET PASSWORD ======
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword(string email, [FromServices] Foras_Khadra.Services.IEmailQueue emails)
        {
            if (string.IsNullOrWhiteSpace(email))
                return View();

            // رمز Identity القياسي (مُوقَّع ومحدود الصلاحية) بدل الرمز النصي القديم في جدول المنظمات
            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null || !await _userManager.IsInRoleAsync(user, "Organization"))
                return View("ForgotPasswordConfirmation"); // لا نكشف وجود الحساب

            var organization = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.UserId == user.Id);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Organization",
                new { email = user.Email, token = TokenCodec.Encode(token) }, Request.Scheme);

            string emailBody = $@"
<!DOCTYPE html>
<html lang='{CultureInfo.CurrentCulture.TwoLetterISOLanguageName}' dir='{(CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ar" ? "rtl" : "ltr")}'>
<head>
    <meta charset='UTF-8'>
    <title>{_localizer["ResetPasswordSubject"]}</title>
</head>
<body style='font-family: Tahoma, Arial; background-color: #f4f6f8; padding:20px;'>
    <div style='max-width:600px;margin:auto;background:#fff;padding:30px;border-radius:8px;'>
        <h2 style='text-align:center;'>Foras Khadra</h2>
        <p>{_localizer["ResetPasswordGreeting"]}</p>
        <p>{string.Format(_localizer["ResetPasswordBodyOrg"], System.Net.WebUtility.HtmlEncode(organization?.Name ?? user.Email))}</p>
        <p style='text-align:center;margin:30px 0;'>
            <a href='{resetLink}'
               style='background:#28a745; color:#fff; padding:12px 30px;
                      text-decoration:none; border-radius:6px; font-weight:bold;'>
                {_localizer["ResetPasswordButton"]}
            </a>
        </p>
        <p>{_localizer["ResetPasswordIgnore"]}</p>
        <p>{_localizer["ResetPasswordRegards"]}</p>
    </div>
</body>
</html>";
            string emailSubject = $"{_localizer["SiteName"]} - {_localizer["ResetPasswordSubject"]}";
            emails.Enqueue(user.Email!, emailSubject, emailBody);
            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || TokenCodec.Decode(token) == null)
                return BadRequest("الرابط غير صالح أو منتهي الصلاحية.");
            return View(new OrganizationResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(OrganizationResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            var rawToken = TokenCodec.Decode(model.Token);
            if (user == null || rawToken == null || !await _userManager.IsInRoleAsync(user, "Organization"))
            {
                ModelState.AddModelError("", "الرابط غير صالح أو منتهي الصلاحية.");
                return View(model);
            }

            var resetResult = await _userManager.ResetPasswordAsync(user, rawToken, model.Password);
            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }
            return View("ResetPasswordConfirmation");
        }
    }
}
