using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.ErrorHandling;

internal static class PlatformAccountValidationProblem
{
    public static BadRequestObjectResult Create(ActionContext context)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var entry in context.ModelState.Where(entry => entry.Value?.Errors.Count > 0))
        {
            var name = entry.Key.Split('.').Last();
            var property = typeof(SimplePlatformAccountUpsertRequest).GetProperties()
                .FirstOrDefault(property => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (property is null)
                continue;

            var field = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var detail = type == typeof(Guid)
                ? $"حقل {field} يجب أن يحتوي على معرّف صالح بصيغة UUID."
                : type == typeof(DateOnly)
                    ? $"حقل {field} يجب أن يحتوي على تاريخ صالح بصيغة yyyy-MM-dd أو null إذا لم يوجد تاريخ."
                    : field is "code" or "externalAccountId" or "paymentModel" or "status"
                        ? $"حقل {field} مطلوب ويجب أن يحتوي على قيمة نصية صالحة."
                        : $"حقل {field} يجب أن يحتوي على قيمة نصية صالحة أو null للحقل الاختياري.";
            errors[field] = [detail];
        }

        if (errors.Count == 0)
            errors["body"] = ["محتوى الطلب غير صالح. أرسل بيانات الحساب بصيغة JSON صحيحة وبأنواع الحقول المطلوبة."];

        var first = errors.First();
        var problem = ApiProblemDetails.Create(context.HttpContext, StatusCodes.Status400BadRequest,
            first.Value[0], "platform.account_invalid_field");
        problem.Extensions["field"] = first.Key;
        problem.Extensions["errors"] = errors;
        return new BadRequestObjectResult(problem);
    }
}
