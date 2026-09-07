using LogisticsERP.Application.Features.Fleet;

namespace LogisticsERP.Worker;

internal sealed class AccidentDeadlineNotificationWorker(IServiceScopeFactory scopeFactory,
    ILogger<AccidentDeadlineNotificationWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogFailure = LoggerMessage.Define(LogLevel.Error,
        new EventId(4103, nameof(AccidentDeadlineNotificationWorker)), "Accident deadline notification scan failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IAccidentNotificationService>().RunDueNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { LogFailure(logger, exception); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
