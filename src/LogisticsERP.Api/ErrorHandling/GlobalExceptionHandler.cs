using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        var (status, title, detail, type, errorCode) = exception switch
        {
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "The record was changed by another operation.",
                "Reload the latest data and retry the operation.",
                "https://httpstatuses.io/409",
                $"{system}.concurrency_conflict"),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "The request is invalid.",
                "Check the request path, query parameters, and body, then try again.",
                "https://httpstatuses.io/400",
                $"{system}.invalid_request"),
            _ => (
                StatusCodes.Status500InternalServerError,
                $"An unexpected error occurred in the {ToDisplayName(system)} system.",
                "The request could not be completed. Contact support with the correlationId so the server log can be located.",
                "https://httpstatuses.io/500",
                $"{system}.unexpected_error")
        };

        LogUnhandledException(logger, status, httpContext.TraceIdentifier, exception);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["errorCode"] = errorCode,
                ["system"] = system,
                ["correlationId"] = httpContext.TraceIdentifier
            }
        }, cancellationToken);

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

    private static string ToDisplayName(string system) => system.Replace('_', ' ');
}
