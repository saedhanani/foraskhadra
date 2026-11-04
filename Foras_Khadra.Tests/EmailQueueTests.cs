using Foras_Khadra.Helpers;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class EmailQueueTests
    {
        private sealed class FakeSender : IEmailSender
        {
            public readonly List<(string To, string Subject)> Sent = new();
            public int FailFirst;
            public Task SendEmailAsync(string email, string subject, string htmlMessage)
            {
                if (FailFirst-- > 0) throw new InvalidOperationException("smtp down");
                Sent.Add((email, subject));
                return Task.CompletedTask;
            }
        }

        private static (EmailQueue queue, FakeSender sender) Build()
        {
            var sender = new FakeSender();
            var services = new ServiceCollection();
            services.AddSingleton<IEmailSender>(sender);
            var provider = services.BuildServiceProvider();
            return (new EmailQueue(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<EmailQueue>.Instance), sender);
        }

        [Fact]
        public async Task SendAsync_DeliversThroughTheRegisteredSender()
        {
            var (queue, sender) = Build();
            Assert.True(await queue.SendAsync(new EmailJob("a@test.local", "hi", "<p>x</p>"), CancellationToken.None));
            Assert.Single(sender.Sent);
            Assert.Equal("a@test.local", sender.Sent[0].To);
        }

        [Fact]
        public async Task SendAsync_RetriesOnceAfterFailure()
        {
            var (queue, sender) = Build();
            sender.FailFirst = 1;
            Assert.True(await queue.SendAsync(new EmailJob("a@test.local", "hi", "x"), CancellationToken.None));
            Assert.Single(sender.Sent);
        }

        [Fact]
        public async Task SendAsync_GivesUpAfterSecondFailure()
        {
            var (queue, sender) = Build();
            sender.FailFirst = 5;
            Assert.False(await queue.SendAsync(new EmailJob("a@test.local", "hi", "x"), CancellationToken.None));
            Assert.Empty(sender.Sent);
        }

        [Fact]
        public void Enqueue_IgnoresEmptyRecipient()
        {
            var (queue, _) = Build();
            queue.Enqueue("", "s", "b"); // لا استثناء
        }

        [Theory]
        [InlineData("CfDJ8Abc+/=xyz")]
        [InlineData("plain")]
        [InlineData("رمز عربي")]
        public void TokenCodec_RoundTrips(string token)
        {
            var encoded = TokenCodec.Encode(token);
            Assert.DoesNotContain('+', encoded);
            Assert.DoesNotContain('/', encoded);
            Assert.DoesNotContain('=', encoded);
            Assert.Equal(token, TokenCodec.Decode(encoded));
        }

        [Fact]
        public void TokenCodec_RejectsGarbage()
        {
            Assert.Null(TokenCodec.Decode(null));
            Assert.Null(TokenCodec.Decode(""));
            Assert.Null(TokenCodec.Decode("%%%not base64url%%%"));
        }

        [Fact]
        public void AccountEmails_AreTrilingualAndEncodeTheLink()
        {
            var ar = AccountEmails.ConfirmEmail("ar", "https://x.test/confirm?code=a&b=c", "أحمد");
            var en = AccountEmails.ConfirmEmail("en", "https://x.test/confirm", null);
            var fr = AccountEmails.ChangeEmail("fr", "https://x.test/change", "new@test.local");
            Assert.Contains("تأكيد", ar.Subject);
            Assert.Contains("أحمد", ar.Html);
            Assert.Contains("&amp;b=c", ar.Html);
            Assert.Contains("Confirm", en.Subject);
            Assert.Contains("nouvel e-mail", fr.Subject);
            Assert.Contains("new@test.local", fr.Html);
        }
    }
}
