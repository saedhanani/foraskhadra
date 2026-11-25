using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Foras_Khadra.Tests.Integration
{
    /// <summary>
    /// إعادة إرسال روابط التأكيد لمن سجّل أثناء عطل البريد: تُرسل مرة واحدة لكل عضو، على دفعات،
    /// ولا تمس المؤكَّدين ولا المنظمات ولا من سجّل قبل التاريخ المحدد.
    /// </summary>
    public class ConfirmationResendTests : IClassFixture<MailCaptureSiteFactory>
    {
        private readonly MailCaptureSiteFactory _site;
        public ConfirmationResendTests(MailCaptureSiteFactory site) => _site = site;

        private static string Email(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.local";
        private IConfirmationResend Resend => _site.Services.GetRequiredService<IConfirmationResend>();

        private async Task<ApplicationUser> UserAsync(string prefix, string role, bool confirmed, DateTime createdAt)
        {
            var user = await _site.CreateUserAsync(Email(prefix), role, confirmed);
            using var scope = _site.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Foras_Khadra.Data.ApplicationDbContext>();
            var row = db.Users.Single(u => u.Id == user.Id);
            row.CreatedAt = createdAt;
            db.SaveChanges();
            return user;
        }

        [Fact]
        public async Task SendsOncePerUnconfirmedMemberAndSkipsEveryoneElse()
        {
            var since = new DateTime(2026, 10, 28);
            var target = await UserAsync("resend-target", "User", confirmed: false, createdAt: since.AddDays(3));
            var confirmed = await UserAsync("resend-confirmed", "User", confirmed: true, createdAt: since.AddDays(3));
            var tooOld = await UserAsync("resend-old", "User", confirmed: false, createdAt: since.AddDays(-5));
            var org = await UserAsync("resend-org", "Organization", confirmed: false, createdAt: since.AddDays(3));

            await Resend.StartAsync(since);
            var progress = await Resend.ProgressAsync();
            Assert.True(progress.Enabled);
            Assert.True(progress.Pending >= 1);

            // دفعة واحدة: رسالة واحدة فقط مهما كان المتبقّي
            Assert.Equal(1, await Resend.SendBatchAsync(1));

            var mail = await _site.Mail.WaitForAsync(target.Email!);
            Assert.Contains("/Account/ConfirmEmail?userId=", mail.Html);
            Assert.Contains("لم تصلك رسالة التأكيد", mail.Html);

            // لا تتكرر الرسالة لنفس العضو مهما أُعيد تشغيل الدفعات
            await Resend.SendBatchAsync(20);
            var after = await Resend.ProgressAsync();
            Assert.Equal(0, after.Pending);

            foreach (var skipped in new[] { confirmed, tooOld, org })
                Assert.DoesNotContain(_site.Mail.All, m => string.Equals(m.To, skipped.Email, StringComparison.OrdinalIgnoreCase));

            Assert.Single(_site.Mail.All, m => string.Equals(m.To, target.Email, StringComparison.OrdinalIgnoreCase));
            Assert.False(after.Enabled); // تتوقف وحدها عندما لا يبقى أحد
        }

        [Fact]
        public async Task AdminPageShowsTheCampaignAndIsClosedToOthers()
        {
            var anonymous = await _site.Client().GetAsync("/Admin/ResendConfirmations");
            Assert.Equal(System.Net.HttpStatusCode.Redirect, anonymous.StatusCode);
            Assert.Contains("/Account/Login", anonymous.Headers.Location!.ToString());

            var client = _site.Client();
            var admin = await _site.CreateUserAsync(Email("resend-admin"), "Admin");
            await SiteFactory.LoginAsync(client, admin.Email!, SiteFactory.Password);

            var page = await client.GetAsync("/Admin/ResendConfirmations");
            Assert.Equal(System.Net.HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync();
            // Razor يرمّز العربية إلى &#x...; فلا نبحث عنها نصًا؛ نتحقق من عناصر الصفحة
            Assert.Contains("StartResendConfirmations", html);
            Assert.Contains("name=\"since\"", html);
        }

        [Fact]
        public async Task StoppedCampaignSendsNothing()
        {
            await UserAsync("resend-stopped", "User", confirmed: false, createdAt: new DateTime(2026, 11, 1));
            await Resend.StopAsync();
            Assert.Equal(0, await Resend.SendBatchAsync(5));
        }
    }
}
