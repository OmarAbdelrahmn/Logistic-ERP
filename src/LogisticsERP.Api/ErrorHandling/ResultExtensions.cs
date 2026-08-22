using LogisticsERP.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.ErrorHandling;

internal static class ResultExtensions
{
    public static IActionResult ToProblem(this Result result, HttpContext httpContext)
    {
        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = result.Error.Code,
            Detail = result.Error.Description,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["errorCode"] = result.Error.Code,
                ["correlationId"] = httpContext.TraceIdentifier
            }
        })
        {
            StatusCode = statusCode
        };
    }
}
