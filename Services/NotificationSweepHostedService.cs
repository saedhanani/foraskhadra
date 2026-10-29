using Foras_Khadra.Services;

namespace Foras_Khadra.Services
{
    /// <summary>يشغّل NotificationSweeper دوريًا (بعد دقيقة من الإقلاع ثم كل 6 ساعات)،
    /// ويرسل ما لم يُرسل من إشعارات الدفع كل 20 دقيقة (PushDispatcher) — فالمؤجَّل ليلًا يخرج صباحًا دون انتظار المسح التالي.</summary>
    public class NotificationSweepHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationSweepHostedService> _logger;

        private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(6);
        private static readonly TimeSpan Tick = TimeSpan.FromMinutes(20);

        public NotificationSweepHostedService(IServiceScopeFactory scopeFactory, ILogger<NotificationSweepHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(InitialDelay, stoppingToken); }
            catch (OperationCanceledException) { return; }

            var lastSweep = DateTime.MinValue;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    if (DateTime.UtcNow - lastSweep >= SweepInterval)
                    {
                        lastSweep = DateTime.UtcNow;
                        var sweeper = scope.ServiceProvider.GetRequiredService<INotificationSweeper>();
                        var created = await sweeper.SweepAsync(stoppingToken);
                        if (created > 0)
                            _logger.LogInformation("Notification sweep created {Count} notifications", created);
                    }

                    var dispatcher = scope.ServiceProvider.GetRequiredService<IPushDispatcher>();
                    var pushed = await dispatcher.DispatchPendingAsync(stoppingToken);
                    if (pushed > 0)
                        _logger.LogInformation("Sent {Count} push notifications", pushed);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification sweep failed");
                }

                try { await Task.Delay(Tick, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
