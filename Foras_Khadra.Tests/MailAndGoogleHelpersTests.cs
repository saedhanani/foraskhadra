using System.Security.Claims;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using MailKit.Security;
using MimeKit;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class MailAndGoogleHelpersTests
    {
        [Fact]
        public void HtmlToText_KeepsLinksReadableAndDropsMarkup()
        {
            var text = EmailSender.HtmlToText(
                "<html><head><title>x</title><style>p{color:red}</style></head><body><p>مرحبًا</p>" +
                "<a href='https://x.test/reset?a=1&amp;b=2' style='color:#fff'>إعادة التعيين</a><br/>شكرًا</body></html>");
            Assert.Contains("مرحبًا", text);
            Assert.Contains("إعادة التعيين: https://x.test/reset?a=1&b=2", text);
            Assert.Contains("شكرًا", text);
            Assert.DoesNotContain("<", text);
            Assert.DoesNotContain("color", text);
        }

        [Fact]
        public void BuildMessage_HasHtmlAndTextPartsAndASingleLineSubject()
        {
            var settings = new MailSettings { Mail = "sender@test.local", DisplayName = "Foras Khadra", Host = "smtp.test.local", Port = 587, Password = "x" };
            var message = EmailSender.BuildMessage(settings, " user@test.local ", "سطر\r\nثانٍ", "<p>Hi <a href='https://x.test'>link</a></p>");
            Assert.Equal("user@test.local", ((MailboxAddress)message.To.Single()).Address);
            Assert.Equal("Foras Khadra", ((MailboxAddress)message.From.Single()).Name);
            Assert.DoesNotContain("\n", message.Subject);
            Assert.Contains("<a href='https://x.test'>", message.HtmlBody);
            Assert.Contains("link: https://x.test", message.TextBody);
        }

        [Fact]
        public void ContactForm_GoesToInfoByDefaultAndRepliesToTheVisitor()
        {
            var contact = new Contact { FirstName = "Sara", LastName = "A", Email = "sara@test.local", Phone = "+962790000000", MessageContent = "<b>hi</b>\nthere" };
            var settings = new MailSettings { Mail = "sender@test.local", DisplayName = "Foras Khadra" };

            var message = Foras_Khadra.Services.MailService.BuildContactMessage(settings, contact);
            Assert.Equal("info@foraskhadra.com", ((MailboxAddress)message.To.Single()).Address);
            Assert.Equal("sara@test.local", ((MailboxAddress)message.ReplyTo.Single()).Address);
            Assert.Contains("&lt;b&gt;hi&lt;/b&gt;<br />there", message.HtmlBody);

            settings.ContactTo = "  team@test.local ";
            Assert.Equal("team@test.local", ((MailboxAddress)Foras_Khadra.Services.MailService.BuildContactMessage(settings, contact).To.Single()).Address);
        }

        [Theory]
        [InlineData(587, SecureSocketOptions.StartTls)]
        [InlineData(25, SecureSocketOptions.StartTls)]
        [InlineData(465, SecureSocketOptions.SslOnConnect)]
        public void SocketOptions_NeverSendThePasswordInPlainText(int port, SecureSocketOptions expected) =>
            Assert.Equal(expected, EmailSender.SocketOptionsFor(port));

        private static ClaimsPrincipal Google(params (string Type, string Value)[] claims) =>
            new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "Google"));

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("false", false)]
        [InlineData(null, false)]
        public void EmailCountsAsVerifiedOnlyWhenGoogleSaysSo(string? value, bool expected)
        {
            var principal = value == null ? Google((ClaimTypes.Email, "a@gmail.com")) : Google((ClaimTypes.Email, "a@gmail.com"), (GoogleSignIn.EmailVerifiedClaim, value));
            Assert.Equal(expected, GoogleSignIn.IsEmailVerified(principal));
        }

        [Fact]
        public void Names_PreferGivenAndFamilyName()
        {
            var p = Google((ClaimTypes.GivenName, "سارة"), (ClaimTypes.Surname, "أحمد"), (ClaimTypes.Name, "سارة أحمد"));
            Assert.Equal(("سارة", "أحمد"), GoogleSignIn.Names(p, "s@gmail.com"));
        }

        [Fact]
        public void Names_FallBackToFullNameThenEmail()
        {
            Assert.Equal(("Omar", "Al Khatib"), GoogleSignIn.Names(Google((ClaimTypes.Name, "Omar Al Khatib")), "o@gmail.com"));
            Assert.Equal(("omar.k", ""), GoogleSignIn.Names(Google(), "omar.k@gmail.com"));
            Assert.Null(GoogleSignIn.Email(Google((ClaimTypes.Email, "  "))));
        }
    }
}
