using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Foras_Khadra.Tests.Integration
{
    /// <summary>الموقع مع مرسِل بريد يلتقط الرسائل (بدل الوهمي الذي يرميها) لاختبار الروابط داخل البريد.</summary>
    public class MailCaptureSiteFactory : SiteFactory
    {
        public CapturingEmailSender Mail { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Mail);
            });
        }
    }

    public sealed class CapturingEmailSender : IEmailSender
    {
        private readonly ConcurrentQueue<(string To, string Subject, string Html)> _sent = new();

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _sent.Enqueue((email, subject, htmlMessage));
            return Task.CompletedTask;
        }

        /// <summary>الطابور يرسل في الخلفية: ننتظر حتى 10 ثوانٍ رسالةً إلى هذا العنوان.</summary>
        public async Task<(string To, string Subject, string Html)> WaitForAsync(string to)
        {
            for (var i = 0; i < 100; i++)
            {
                var hit = _sent.FirstOrDefault(m => string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase));
                if (hit.To != null) return hit;
                await Task.Delay(100);
            }
            throw new TimeoutException("no email to " + to);
        }
    }

    /// <summary>الموقع ومفاتيح Google مضبوطة (قيم اختبار): يظهر الزر ويتحوّل الطلب إلى Google.</summary>
    public class GoogleSiteFactory : SiteFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Authentication:Google:ClientId", "integration-test.apps.googleusercontent.com");
            builder.UseSetting("Authentication:Google:ClientSecret", "integration-test-secret");
        }
    }

    public class AccountRecoveryTests : IClassFixture<MailCaptureSiteFactory>
    {
        private readonly MailCaptureSiteFactory _site;
        public AccountRecoveryTests(MailCaptureSiteFactory site) => _site = site;

        private static string Email(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.local";

        private static string HiddenValue(string html, string name)
        {
            var m = Regex.Match(html, $"name=\"{name}\"[^>]*value=\"([^\"]*)\"|value=\"([^\"]*)\"[^>]*name=\"{name}\"");
            Assert.True(m.Success, name + " not found");
            return WebUtility.HtmlDecode(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
        }

        /// <summary>
        /// الشكوى الأصلية: «نسيت كلمة المرور» لا يصل رمزها. هنا الرحلة كاملة: الطلب ← البريد ← الرابط ← كلمة مرور جديدة ← الدخول بها.
        /// الرابط يحمل رمزًا base64url (لا + ولا / ولا =)، والاستعادة تؤكّد البريد غير المؤكَّد.
        /// </summary>
        [Fact]
        public async Task ForgotPasswordEmailsAWorkingResetLinkThatAlsoConfirmsTheEmail()
        {
            var email = Email("reset");
            var user = await _site.CreateUserAsync(email, "User", confirmed: false);
            var client = _site.Client();

            var formToken = await SiteFactory.AntiforgeryAsync(client, "/Account/ForgotPassword");
            var request = await client.PostAsync("/Account/ForgotPassword", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email, ["__RequestVerificationToken"] = formToken
            }));
            Assert.Equal(HttpStatusCode.OK, request.StatusCode);

            var mail = await _site.Mail.WaitForAsync(email);
            var href = Regex.Match(mail.Html, "href='([^']*ResetPassword[^']*)'");
            Assert.True(href.Success, "reset link missing from the email");
            var link = new Uri(WebUtility.HtmlDecode(href.Groups[1].Value));
            var tokenInLink = Uri.UnescapeDataString(Regex.Match(link.Query, "token=([^&]+)").Groups[1].Value);
            Assert.DoesNotContain('+', tokenInLink);
            Assert.DoesNotContain('/', tokenInLink);

            var page = await client.GetAsync(link.PathAndQuery);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync();

            const string newPassword = "New#Pass2026";
            var reset = await client.PostAsync("/Account/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = HiddenValue(html, "Email"),
                ["Token"] = HiddenValue(html, "Token"),
                ["Password"] = newPassword,
                ["ConfirmPassword"] = newPassword,
                ["__RequestVerificationToken"] = HiddenValue(html, "__RequestVerificationToken")
            }));
            Assert.Equal(HttpStatusCode.Redirect, reset.StatusCode);
            Assert.Contains("ResetPasswordConfirmation", reset.Headers.Location!.ToString());

            Assert.True(_site.InDb(db => db.Users.Single(u => u.Id == user.Id).EmailConfirmed));
            var login = await SiteFactory.LoginAsync(_site.Client(), email, newPassword);
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            Assert.DoesNotContain("/Account/Login", login.Headers.Location!.ToString());
        }

        [Fact]
        public async Task ResetWithATamperedTokenFailsWithAMessage()
        {
            var email = Email("reset-bad");
            await _site.CreateUserAsync(email, "User");
            var client = _site.Client();
            var formToken = await SiteFactory.AntiforgeryAsync(client, "/Account/ResetPassword?email=" + Uri.EscapeDataString(email) + "&token=abc");
            var reset = await client.PostAsync("/Account/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email, ["Token"] = "%%%", ["Password"] = "New#Pass2026", ["ConfirmPassword"] = "New#Pass2026",
                ["__RequestVerificationToken"] = formToken
            }));
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode); // الصفحة نفسها برسالة، لا تحويل إلى «تم»
        }

        /// <summary>بلا مفاتيح Google (الوضع الحالي في الإنتاج): لا زر، وطلب الدخول بـ Google يعود إلى صفحة الدخول.</summary>
        [Fact]
        public async Task GoogleButtonIsHiddenAndExternalLoginIsRefusedWithoutKeys()
        {
            var client = _site.Client();
            var html = await (await client.GetAsync("/Account/Login")).Content.ReadAsStringAsync();
            Assert.DoesNotContain("gsi-btn", html);

            var token = await SiteFactory.AntiforgeryAsync(client, "/Account/Login");
            var res = await client.PostAsync("/Account/ExternalLogin", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["provider"] = "Google", ["__RequestVerificationToken"] = token
            }));
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("/Account/Login", res.Headers.Location!.ToString());
        }
    }

    /// <summary>
    /// نموذج «تواصل معنا» كما ترسله الصفحة: الهاتف بصيغة E.164 مع ‎+‎ وبلا حقل CountryCode.
    /// كان هذا يُرفض دائمًا (CountryCode إلزامي، والهاتف يمنع ‎+‎) فلا تصل أي رسالة، والصفحة تعرض «تم الإرسال».
    /// </summary>
    public class ContactFormTests : IClassFixture<SiteFactory>
    {
        private readonly SiteFactory _site;
        public ContactFormTests(SiteFactory site) => _site = site;

        private async Task<string> PostAsync(IDictionary<string, string> fields)
        {
            var client = _site.Client();
            var token = await SiteFactory.AntiforgeryAsync(client, "/contact");
            fields["__RequestVerificationToken"] = token;
            var res = await client.PostAsync("/contact", new FormUrlEncodedContent(fields));
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            return await res.Content.ReadAsStringAsync();
        }

        [Theory]
        [InlineData("+962790000000")] // ما ترسله الصفحة فعلًا
        [InlineData("")]              // بلا هاتف: الحقل اختياري
        public async Task MessageFromThePageReachesTheMailService(string phone)
        {
            var before = _site.Contacts.Sent.Count;
            var body = await PostAsync(new Dictionary<string, string>
            {
                ["FirstName"] = "Sara", ["LastName"] = "Ahmad", ["Email"] = "sara@test.local",
                ["Phone"] = phone, ["MessageContent"] = "رسالة اختبار"
            });
            Assert.Contains("\"success\":true", body);
            Assert.Equal(before + 1, _site.Contacts.Sent.Count);
            var sent = _site.Contacts.Sent[^1];
            Assert.Equal("sara@test.local", sent.Email);
            if (phone.Length > 0) Assert.Equal("+962790000000", sent.Phone);
        }

        [Fact]
        public async Task MissingNameOrMessageIsRefusedAndNothingIsSent()
        {
            var before = _site.Contacts.Sent.Count;
            var body = await PostAsync(new Dictionary<string, string> { ["Email"] = "sara@test.local" });
            Assert.Contains("\"success\":false", body);
            Assert.Equal(before, _site.Contacts.Sent.Count);
        }

        [Fact]
        public async Task HoneypotSubmissionIsRefused()
        {
            var before = _site.Contacts.Sent.Count;
            var body = await PostAsync(new Dictionary<string, string>
            {
                ["FirstName"] = "Bot", ["LastName"] = "Bot", ["Email"] = "bot@test.local",
                ["MessageContent"] = "spam", ["Honeypot"] = "filled"
            });
            Assert.Contains("\"success\":false", body);
            Assert.Equal(before, _site.Contacts.Sent.Count);
        }
    }

    public class GoogleSignInTests : IClassFixture<GoogleSiteFactory>
    {
        private readonly GoogleSiteFactory _site;
        public GoogleSignInTests(GoogleSiteFactory site) => _site = site;

        [Theory]
        [InlineData("/Account/Login")]
        [InlineData("/en/Account/Login")]
        [InlineData("/Account/RegisterUser")]
        public async Task GoogleButtonShowsOnSignInPages(string path)
        {
            var html = await (await _site.Client().GetAsync(path)).Content.ReadAsStringAsync();
            Assert.Contains("gsi-btn", html);
            Assert.Contains("ExternalLogin", html);
        }

        [Fact]
        public async Task GoogleButtonOnCompassRegisterReturnsToMyCompass()
        {
            var client = _site.Client();
            var token = await SiteFactory.AntiforgeryAsync(client, "/compass");
            await client.PostAsync("/compass", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["A.Residence"] = "JO", ["A.Nationality"] = "JO", ["A.Goals"] = "Scholarships", ["__RequestVerificationToken"] = token
            }));
            var html = await (await client.GetAsync("/compass/register")).Content.ReadAsStringAsync();
            Assert.Contains("gsi-btn", html);
            Assert.Matches("name=\"returnUrl\" value=\"/compass/me\"", html);
        }

        [Fact]
        public async Task ExternalLoginRedirectsToGoogleWithTheSignInCallback()
        {
            var client = _site.Client();
            var token = await SiteFactory.AntiforgeryAsync(client, "/Account/Login");
            var res = await client.PostAsync("/Account/ExternalLogin", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["provider"] = "Google", ["returnUrl"] = "/compass/me", ["__RequestVerificationToken"] = token
            }));
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            var location = res.Headers.Location!.ToString();
            Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth", location);
            Assert.Contains("client_id=integration-test.apps.googleusercontent.com", location);
            Assert.Contains(Uri.EscapeDataString("https://localhost/signin-google"), location);
        }

        [Fact]
        public async Task CallbackWithoutAGoogleSessionReturnsToLoginWithAMessage()
        {
            var client = _site.Client();
            var res = await client.GetAsync("/Account/ExternalLoginCallback?remoteError=1");
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            var login = await client.GetAsync(res.Headers.Location!.ToString());
            Assert.Contains("gsi-error", await login.Content.ReadAsStringAsync());
        }
    }
}
