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

        var problem = new ProblemDetails
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
        };
        if (result.Error.Field is not null)
        {
            problem.Extensions["field"] = result.Error.Field;
        }

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}
