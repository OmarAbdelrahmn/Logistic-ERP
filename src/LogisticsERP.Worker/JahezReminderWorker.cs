using LogisticsERP.Application.Features.Jahez;

namespace LogisticsERP.Worker;

internal sealed class JahezReminderWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<JahezReminderWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogFailure = LoggerMessage.Define(LogLevel.Error,
        new EventId(4401, nameof(JahezReminderWorker)), "Jahez settlement reminder scan failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IJahezReminderService>().RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { LogFailure(logger, e); }
            await Task.Delay(TimeSpan.FromHours(1), clock, stoppingToken);
        }
    }
}
