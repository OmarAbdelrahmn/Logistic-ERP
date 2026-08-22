using LogisticsERP.Application.Features.Fleet;

namespace LogisticsERP.Worker;

internal sealed class FleetExpiryNotificationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<FleetExpiryNotificationWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogScanFailure =
        LoggerMessage.Define(LogLevel.Error, new EventId(4101, nameof(FleetExpiryNotificationWorker)), "Fleet compliance notification scan failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IFleetComplianceNotificationService>().RunDueNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                LogScanFailure(logger, exception);
            }

            var now = timeProvider.GetUtcNow();
            var riyadhNow = now.ToOffset(TimeSpan.FromHours(3));
            var nextRiyadh = new DateTimeOffset(riyadhNow.Year, riyadhNow.Month, riyadhNow.Day, 1, 0, 0, TimeSpan.FromHours(3)).AddDays(1);
            await Task.Delay(nextRiyadh.ToUniversalTime() - now, stoppingToken);
        }
    }
}
