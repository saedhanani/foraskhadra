using System.Threading.Channels;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Foras_Khadra.Services
{
    /// <summary>
    /// إرسال البريد في الخلفية: الطلب لا ينتظر SMTP (فلا يكشف زمن الاستجابة وجود الحساب، ولا يعيد 500 عند تعطّل Gmail).
    /// الرسائل تُفقد عند إعادة تشغيل التطبيق — مقبول لرسائل التأكيد والاستعادة (يمكن إعادة طلبها).
    /// </summary>
    public interface IEmailQueue
    {
        void Enqueue(string to, string subject, string htmlBody);
    }

    public sealed record EmailJob(string To, string Subject, string HtmlBody, int Attempt = 0);

    public class EmailQueue : BackgroundService, IEmailQueue
    {
        public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(25);
        private const int MaxAttempts = 2;

        private readonly Channel<EmailJob> _channel = Channel.CreateBounded<EmailJob>(new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest });
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<EmailQueue> _log;

        public EmailQueue(IServiceScopeFactory scopes, ILogger<EmailQueue> log)
        {
            _scopes = scopes;
            _log = log;
        }

        public void Enqueue(string to, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(to)) return;
            _channel.Writer.TryWrite(new EmailJob(to.Trim(), subject, htmlBody));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
                await SendAsync(job, stoppingToken);
        }

        /// <summary>يرسل رسالة واحدة بمهلة ومحاولة ثانية؛ معزول للاختبار.</summary>
        public async Task<bool> SendAsync(EmailJob job, CancellationToken ct)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                var send = sender.SendEmailAsync(job.To, job.Subject, job.HtmlBody);
                var finished = await Task.WhenAny(send, Task.Delay(SendTimeout, ct));
                if (finished != send) throw new TimeoutException($"SMTP لم يرد خلال {SendTimeout.TotalSeconds} ثانية");
                await send; // يرفع الاستثناء إن فشل
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Email to {To} failed (attempt {Attempt}): {Subject}", job.To, job.Attempt + 1, job.Subject);
                if (job.Attempt + 1 < MaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                    return await SendAsync(job with { Attempt = job.Attempt + 1 }, ct);
                }
                return false;
            }
        }
    }
}
