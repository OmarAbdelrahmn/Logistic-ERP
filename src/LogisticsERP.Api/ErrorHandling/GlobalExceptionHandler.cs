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

        var (status, title, type) = exception switch
        {
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "The record was changed by another operation.",
                "https://httpstatuses.io/409"),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "The request is invalid.",
                "https://httpstatuses.io/400"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "https://httpstatuses.io/500")
        };

        LogUnhandledException(logger, status, httpContext.TraceIdentifier, exception);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["correlationId"] = httpContext.TraceIdentifier
            }
        }, cancellationToken);

        return true;
    }
}
