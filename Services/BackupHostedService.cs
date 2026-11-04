namespace Foras_Khadra.Services
{
    /// <summary>
    /// يشغّل BackupExporter مرة يوميًا بعد الساعة Backup:Hour (افتراضيًا 3 فجرًا بتوقيت الخادم) إن لم تُكتب نسخة لهذا اليوم.
    /// يفحص كل 30 دقيقة؛ Backup:Enabled=false يعطّله.
    /// </summary>
    public class BackupHostedService : BackgroundService
    {
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan Tick = TimeSpan.FromMinutes(30);

        private readonly IServiceScopeFactory _scopes;
        private readonly IConfiguration _cfg;
        private readonly ILogger<BackupHostedService> _log;

        public BackupHostedService(IServiceScopeFactory scopes, IConfiguration cfg, ILogger<BackupHostedService> log)
        {
            _scopes = scopes;
            _cfg = cfg;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_cfg.GetValue<bool?>("Backup:Enabled") == false) return;
            var hour = Math.Clamp(_cfg.GetValue<int?>("Backup:Hour") ?? 3, 0, 23);

            try { await Task.Delay(InitialDelay, stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var exporter = scope.ServiceProvider.GetRequiredService<IBackupExporter>();
                    var now = DateTime.Now;
                    if (now.Hour >= hour && !exporter.HasBackupFor(DateOnly.FromDateTime(now)))
                        await exporter.ExportAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Daily backup failed");
                }

                try { await Task.Delay(Tick, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
