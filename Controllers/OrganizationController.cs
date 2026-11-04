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
                ModelState.AddModelError(nameof(model.ContactEmail),
                    RegisterOrganizationResources.EmailAlreadyUsed);

                // تمرير رقم الخطوة الحالية
                ViewData["CurrentStep"] = 4; // الخطوة الخاصة بالمعلومات الأساسية + البريد
                return View(model);
            }


            // تحقق من قوة كلمة المرور
            if (string.IsNullOrWhiteSpace(model.Password) ||
                model.Password.Length < 8 ||
                !model.Password.Any(char.IsUpper) ||
                !model.Password.Any(char.IsLower) ||
                !model.Password.Any(char.IsDigit) ||
                !model.Password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                ModelState.AddModelError("Password", "كلمة المرور يجب أن تحتوي على 8 أحرف على الأقل، حرف كبير، حرف صغير، رقم ورمز خاص");
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "كلمة المرور وتأكيدها غير متطابقين");
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
        public IActionResult Login() => View();

        [HttpPost]
        [RedirectSignedIn]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(OrganizationLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
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
                ModelState.AddModelError("", "تم قفل الحساب مؤقتًا بسبب محاولات دخول فاشلة متكررة، حاول مجددًا بعد 15 دقيقة");
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
                return View(model);
            }

            if (isNormalUser)
            {
                TempData["UserLoginAlert"] = _localizer["UserLoginAlert"].Value;
                TempData["UserLoginRedirectText"] = _localizer["UserLoginRedirectText"].Value;
                TempData["UserLoginRedirect"] = Url.Action("Login", "Account");
                return RedirectToAction("Login");
            }
            await _signInManager.SignInAsync(user, isPersistent: false);
            // توجيه الادمن للوحة الإدارة والمنظمة للوحة التحكم الخاصة بها
            if (isAdmin)
                return RedirectToAction("Dashboard", "Admin");
            else
                return RedirectToAction("Index", "Home");
        }

        public IActionResult Dashboard() => View();

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
