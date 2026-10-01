using LogisticsERP.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.ErrorHandling;

internal static class ResultExtensions
{
    public static IActionResult ToProblem(this Result result, HttpContext httpContext, string? title = null)
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

        var problem = ApiProblemDetails.Create(httpContext, statusCode, result.Error.Description, result.Error.Code);
        if (title is not null)
        {
            problem.Title = title;
        }
        if (result.Error.Field is not null)
        {
            problem.Extensions["field"] = result.Error.Field;
        }
        if (result.Error.Details is not null)
        {
            foreach (var detail in result.Error.Details)
            {
                problem.Extensions[detail.Key] = detail.Value;
            }
        }

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}
