using Foras_Khadra.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

namespace Foras_Khadra.Services
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;

        public MailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendEmailAsync(Contact contact) => await SendAsync(BuildContactMessage(_mailSettings, contact));

        /// <summary>رسالة «تواصل معنا» إلى صندوق الموقع (MailSettings:ContactTo، افتراضيًا info@foraskhadra.com)؛ الرد يذهب إلى الزائر.</summary>
        public static MimeMessage BuildContactMessage(MailSettings settings, Contact contact)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(settings.DisplayName, settings.Mail));
            email.To.Add(MailboxAddress.Parse(settings.ContactInbox));
            email.ReplyTo.Add(MailboxAddress.Parse(contact.Email));
            email.Subject = $"New Contact Message from {contact.FirstName} {contact.LastName}".Replace("\r", " ").Replace("\n", " ");

            var builder = new BodyBuilder
            {
                HtmlBody = $@"
                    <p><strong>From:</strong> {System.Net.WebUtility.HtmlEncode(contact.FirstName + " " + contact.LastName)} &lt;{System.Net.WebUtility.HtmlEncode(contact.Email)}&gt;</p>
                    <p><strong>Phone:</strong> {System.Net.WebUtility.HtmlEncode(contact.Phone)}</p>
                    <p><strong>Message:</strong><br />{System.Net.WebUtility.HtmlEncode(contact.MessageContent ?? "").Replace("\n", "<br />")}</p>"
            };
            email.Body = builder.ToMessageBody();
            email.MessageId = MimeUtils.GenerateMessageId();
            return email;
        }

        private async Task SendAsync(MimeMessage email)
        {
            using var smtp = new SmtpClient { Timeout = 20000 };
            await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, EmailSender.SocketOptionsFor(_mailSettings.Port));
            await smtp.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
