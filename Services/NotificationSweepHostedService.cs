using Foras_Khadra.Services;

namespace Foras_Khadra.Services
{
    /// <summary>يشغّل NotificationSweeper دوريًا (بعد دقيقة من الإقلاع ثم كل 6 ساعات).</summary>
    public class NotificationSweepHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationSweepHostedService> _logger;

        private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

        public NotificationSweepHostedService(IServiceScopeFactory scopeFactory, ILogger<NotificationSweepHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(InitialDelay, stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sweeper = scope.ServiceProvider.GetRequiredService<INotificationSweeper>();
                    var created = await sweeper.SweepAsync(stoppingToken);
                    if (created > 0)
                        _logger.LogInformation("Notification sweep created {Count} notifications", created);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification sweep failed");
                }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
