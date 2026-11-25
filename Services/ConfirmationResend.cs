using System.Text.Json;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>حالة حملة إعادة إرسال روابط التأكيد كما تُعرض في لوحة المدير.</summary>
    public sealed record ResendProgress(bool Enabled, int Sent, int Pending, int Failed, DateTime Since, DateTime? StartedAt, DateTime? LastRunAt);

    public interface IConfirmationResend
    {
        Task<ResendProgress> ProgressAsync(CancellationToken ct = default);
        Task StartAsync(DateTime since, CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);

        /// <summary>يرسل حتى max رسالة ويعيد عدد ما أُرسل فعلًا (0 = لا أحد متبقٍّ أو الحملة متوقفة).</summary>
        Task<int> SendBatchAsync(int max, CancellationToken ct = default);
    }

    /// <summary>
    /// إعادة إرسال رابط تأكيد البريد لمن سجّل أثناء عطل البريد ولم تصله رسالته (نوفمبر 2026).
    /// الإرسال على دفعات صغيرة (ConfirmationResendHostedService) لا دفعة واحدة: مئات الرسائل فجأة من عنوان
    /// قليل الإرسال تُرجَّح أن تُصنَّف مزعجة أو يُقيَّد الحساب. من أُرسل إليه يُسجَّل في App_Data/confirmation-resend.json
    /// (مثل مفاتيح VAPID) فلا تتكرر الرسالة ولو أُعيد تشغيل التطبيق.
    /// </summary>
    public sealed class ConfirmationResend : IConfirmationResend
    {
        private sealed class State
        {
            public bool Enabled { get; set; }
            public DateTime Since { get; set; }
            public DateTime? StartedAt { get; set; }
            public DateTime? LastRunAt { get; set; }
            public int Failed { get; set; }
            public List<string> SentUserIds { get; set; } = new();
        }

        private readonly string _path;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ConfirmationResend> _log;
        private readonly string _baseUrl;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private State _state;

        public ConfirmationResend(IWebHostEnvironment env, IServiceScopeFactory scopes, IConfiguration config, ILogger<ConfirmationResend> log)
            : this(Path.Combine(env.ContentRootPath, "App_Data", "confirmation-resend.json"), scopes, config, log) { }

        public ConfirmationResend(string path, IServiceScopeFactory scopes, IConfiguration config, ILogger<ConfirmationResend> log)
        {
            _path = path;
            _scopes = scopes;
            _log = log;
            _baseUrl = (config["BaseUrl"] ?? "https://www.foraskhadra.com/").TrimEnd('/');
            _state = Load(path);
        }

        private static State Load(string path)
        {
            try
            {
                if (File.Exists(path))
                    return JsonSerializer.Deserialize<State>(File.ReadAllText(path)) ?? new State();
            }
            catch (Exception)
            {
                // ملف تالف: نبدأ من جديد بدل أن يمنع التطبيق من الإقلاع (الحملة تُعاد من اللوحة)
            }
            return new State();
        }

        private void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_state));
        }

        public async Task<ResendProgress> ProgressAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                var pending = _state.StartedAt == null ? 0 : await CountPendingAsync(ct);
                return new ResendProgress(_state.Enabled, _state.SentUserIds.Count, pending, _state.Failed, _state.Since, _state.StartedAt, _state.LastRunAt);
            }
            finally { _gate.Release(); }
        }

        public async Task StartAsync(DateTime since, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                _state.Enabled = true;
                _state.Since = since;
                _state.StartedAt ??= DateTime.Now;
                Save();
                _log.LogInformation("Confirmation resend started (since {Since}, already sent {Sent})", since, _state.SentUserIds.Count);
            }
            finally { _gate.Release(); }
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                _state.Enabled = false;
                Save();
                _log.LogInformation("Confirmation resend stopped ({Sent} sent)", _state.SentUserIds.Count);
            }
            finally { _gate.Release(); }
        }

        public async Task<int> SendBatchAsync(int max, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (!_state.Enabled || max <= 0) return 0;

                using var scope = _scopes.CreateScope();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var emails = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
                var batch = await PendingQuery(scope).OrderBy(u => u.CreatedAt).Take(max).ToListAsync(ct);

                var sent = 0;
                foreach (var user in batch)
                {
                    try
                    {
                        var token = await users.GenerateEmailConfirmationTokenAsync(user);
                        var lang = user.Language is "en" or "fr" ? user.Language : "ar";
                        var link = $"{_baseUrl}{CultureRouting.Prefix(lang)}/Account/ConfirmEmail" +
                                   $"?userId={Uri.EscapeDataString(user.Id)}&code={Uri.EscapeDataString(TokenCodec.Encode(token))}";
                        var mail = AccountEmails.ConfirmEmailResent(lang, link, user.FirstName);
                        emails.Enqueue(user.Email!, mail.Subject, mail.Html);
                        _state.SentUserIds.Add(user.Id);
                        sent++;
                    }
                    catch (Exception ex)
                    {
                        _state.Failed++;
                        _log.LogError(ex, "Confirmation resend failed for {UserId}", user.Id);
                    }
                }

                _state.LastRunAt = DateTime.Now;
                // لا أحد متبقٍّ: توقف الحملة نفسها حتى لا تبقى مفتوحة بلا عمل
                if (sent == 0 || !await PendingQuery(scope).AnyAsync(ct))
                {
                    _state.Enabled = false;
                    _log.LogInformation("Confirmation resend finished ({Sent} sent, {Failed} failed)", _state.SentUserIds.Count, _state.Failed);
                }
                Save();
                return sent;
            }
            finally { _gate.Release(); }
        }

        private async Task<int> CountPendingAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            return await PendingQuery(scope).CountAsync(ct);
        }

        /// <summary>غير مؤكَّدين، سجّلوا منذ التاريخ المحدد، لم نرسل لهم في هذه الحملة، ولهم بريد، وليسوا منظمات.</summary>
        private IQueryable<ApplicationUser> PendingQuery(IServiceScope scope)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sent = _state.SentUserIds;
            var orgRoleIds = db.Roles.Where(r => r.Name == "Organization").Select(r => r.Id);
            return db.Users.AsNoTracking()
                .Where(u => !u.EmailConfirmed
                            && u.CreatedAt >= _state.Since
                            && u.Email != null
                            && !sent.Contains(u.Id)
                            && !db.UserRoles.Any(ur => ur.UserId == u.Id && orgRoleIds.Contains(ur.RoleId)));
        }
    }

    /// <summary>يسحب الحملة ببطء: دفعة صغيرة كل دقيقة (ConfirmationResend:PerMinute، الافتراضي 1) فلا تُرسل مئات الرسائل دفعة واحدة.</summary>
    public sealed class ConfirmationResendHostedService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
        private readonly IConfirmationResend _resend;
        private readonly int _perMinute;

        public ConfirmationResendHostedService(IConfirmationResend resend, IConfiguration config)
        {
            _resend = resend;
            _perMinute = Math.Clamp(config.GetValue("ConfirmationResend:PerMinute", 1), 1, 20);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { await _resend.SendBatchAsync(_perMinute, stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception) { /* الخطأ مسجَّل داخل الخدمة؛ لا نوقف الحلقة */ }
            }
        }
    }
}
