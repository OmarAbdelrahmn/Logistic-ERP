using LogisticsERP.Application.Features.Hr;

namespace LogisticsERP.Worker;

internal sealed class EmployeeExpiryNotificationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<EmployeeExpiryNotificationWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogScanFailure =
        LoggerMessage.Define(LogLevel.Error, new EventId(4201, nameof(EmployeeExpiryNotificationWorker)), "Employee expiry notification scan failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IEmployeeExpiryComplianceService>().RunDueNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { LogScanFailure(logger, exception); }

            var now = timeProvider.GetUtcNow();
            var riyadhNow = now.ToOffset(TimeSpan.FromHours(3));
            var nextRiyadh = new DateTimeOffset(riyadhNow.Year, riyadhNow.Month, riyadhNow.Day, 1, 0, 0, TimeSpan.FromHours(3)).AddDays(1);
            await Task.Delay(nextRiyadh.ToUniversalTime() - now, stoppingToken);
        }
    }
}
