using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, int, string, Exception?> LogUnhandledException =
        LoggerMessage.Define<int, string>(
            LogLevel.Error,
            new EventId(1000, nameof(GlobalExceptionHandler)),
            "Unhandled request exception. StatusCode: {StatusCode}, CorrelationId: {CorrelationId}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var system = ResolveSystem(httpContext.Request.Path);
        var (status, errorCode) = exception switch
        {
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                $"{system}.concurrency_conflict"),
            BadHttpRequestException badRequest when badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge => (
                StatusCodes.Status413PayloadTooLarge,
                $"{system}.request_too_large"),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                $"{system}.invalid_request"),
            _ => (
                StatusCodes.Status500InternalServerError,
                $"{system}.unexpected_error")
        };

        LogUnhandledException(logger, status, httpContext.TraceIdentifier, exception);

        httpContext.Response.StatusCode = status;
        var problem = ApiProblemDetails.Create(httpContext, status, errorCode: errorCode);
        problem.Extensions["system"] = system;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private static string ResolveSystem(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith("/api/platform-accounts", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/api/platforms", StringComparison.OrdinalIgnoreCase))
        {
            return "platform_accounts";
        }

        if (value.StartsWith("/api/riders/", StringComparison.OrdinalIgnoreCase)
            && value.Contains("/platform-history", StringComparison.OrdinalIgnoreCase))
        {
            return "platform_assignments";
        }

        var firstRouteSegment = value.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(segment => segment.Equals("api", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstRouteSegment)
            ? "system"
            : firstRouteSegment.Replace('-', '_').ToLowerInvariant();
    }

}
