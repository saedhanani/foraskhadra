using System.Net;
using System.Text.RegularExpressions;
using Foras_Khadra.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

/// <summary>
/// بريد الموقع كله (تأكيد البريد، استعادة كلمة المرور، رسائل المنظمات والتنبيهات) عبر MailKit — كنموذج التواصل.
/// System.Net.Mail.SmtpClient كان يفشل مع Gmail على الاستضافة في كل رسالة منذ 28 أكتوبر 2026 على الأقل
/// ("Unable to read data from the transport connection: The connection was closed" بعد MAIL FROM)،
/// بينما نموذج التواصل بـ MailKit وبالحساب وكلمة المرور نفسيهما أرسل في الأيام نفسها بلا خطأ.
/// </summary>
public class EmailSender : IEmailSender
{
    private static readonly TimeSpan SmtpTimeout = TimeSpan.FromSeconds(20);
    private readonly MailSettings _settings;

    public EmailSender(IOptions<MailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("البريد الإلكتروني المستلم فارغ", nameof(email));
        if (string.IsNullOrWhiteSpace(_settings.Mail) || string.IsNullOrWhiteSpace(_settings.Host)
            || _settings.Port <= 0 || string.IsNullOrWhiteSpace(_settings.Password))
            throw new InvalidOperationException("MailSettings ناقص في الإعدادات (Mail / Host / Port / Password)");

        var message = BuildMessage(_settings, email, subject, htmlMessage);

        using var smtp = new SmtpClient { Timeout = (int)SmtpTimeout.TotalMilliseconds };
        await smtp.ConnectAsync(_settings.Host, _settings.Port, SocketOptionsFor(_settings.Port));
        await smtp.AuthenticateAsync(_settings.Mail, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    /// <summary>465 = TLS من البداية؛ غيره (587 في Gmail) = STARTTLS إلزامي (لا يرسل كلمة المرور بلا تشفير).</summary>
    public static SecureSocketOptions SocketOptionsFor(int port) =>
        port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

    /// <summary>رسالة HTML مع نسخة نصية بديلة (تقلّل احتمال ذهابها إلى الرسائل غير المرغوب فيها، وتُظهر الرابط في أي برنامج بريد).</summary>
    public static MimeMessage BuildMessage(MailSettings settings, string to, string subject, string html)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.DisplayName ?? "", settings.Mail));
        message.To.Add(MailboxAddress.Parse(to.Trim()));
        message.Subject = (subject ?? "").Replace("\r", " ").Replace("\n", " ");
        message.MessageId = MimeUtils.GenerateMessageId();
        message.Body = new BodyBuilder { HtmlBody = html, TextBody = HtmlToText(html) }.ToMessageBody();
        return message;
    }

    /// <summary>نص بسيط من HTML الرسائل: الروابط تصبح «العنوان: الرابط»، والفقرات أسطرًا.</summary>
    public static string HtmlToText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var text = Regex.Replace(html, @"<(head|style|script)\b[^>]*>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<a\b[^>]*?href\s*=\s*['""]([^'""]+)['""][^>]*>(.*?)</a>",
            m => $"{Regex.Replace(m.Groups[2].Value, "<[^>]+>", "").Trim()}: {m.Groups[1].Value}",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<br\s*/?>|</(p|div|h\d|li|tr)>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", "");
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\s*\n\s*", "\n");
        return text.Trim();
    }
}
