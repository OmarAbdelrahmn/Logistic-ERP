using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.ErrorHandling;

internal static class ApiProblemDetails
{
    public static BadRequestObjectResult BadRequest(HttpContext httpContext, string detail) =>
        new(Create(httpContext, StatusCodes.Status400BadRequest, detail));

    public static ProblemDetails Create(
        HttpContext httpContext,
        int status,
        string? detail = null,
        string? errorCode = null)
    {
        if (FleetProblemDetails.Matches(httpContext.Request.Path))
        {
            return FleetProblemDetails.Create(httpContext, status, detail, errorCode);
        }

        var (title, defaultDetail, defaultCode) = status switch
        {
            StatusCodes.Status400BadRequest => ("طلب غير صالح", "تحقق من بيانات الطلب وحاول مرة أخرى.", "api.invalid_request"),
            StatusCodes.Status401Unauthorized => ("يلزم تسجيل الدخول", "سجل الدخول ثم حاول مرة أخرى.", "api.unauthorized"),
            StatusCodes.Status403Forbidden => ("غير مصرح لك", "ليس لديك صلاحية لتنفيذ هذه العملية.", "api.forbidden"),
            StatusCodes.Status404NotFound => ("العنوان غير موجود", "تحقق من عنوان الطلب وحاول مرة أخرى.", "api.not_found"),
            StatusCodes.Status405MethodNotAllowed => ("طريقة الطلب غير مدعومة", "تحقق من طريقة الطلب وحاول مرة أخرى.", "api.method_not_allowed"),
            StatusCodes.Status409Conflict => ("تعارض في البيانات", "تغيرت البيانات. حدّث الصفحة ثم حاول مرة أخرى.", "api.conflict"),
            StatusCodes.Status413PayloadTooLarge => ("الطلب كبير جدًا", "حجم الطلب أو الملف يتجاوز الحد المسموح. اختر ملفًا أصغر.", "api.request_too_large"),
            StatusCodes.Status415UnsupportedMediaType => ("نوع الطلب غير مدعوم", "تحقق من نوع المحتوى المرسل وحاول مرة أخرى.", "api.unsupported_media_type"),
            StatusCodes.Status429TooManyRequests => ("طلبات كثيرة", "انتظر قليلًا ثم حاول مرة أخرى.", "api.too_many_requests"),
            _ => ("تعذر إكمال الطلب", "حدث خطأ غير متوقع. تواصل مع الدعم واذكر رقم التتبع.", "api.unexpected_error")
        };

        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail ?? defaultDetail,
            Type = $"https://httpstatuses.io/{status}",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["errorCode"] = errorCode ?? defaultCode,
                ["correlationId"] = httpContext.TraceIdentifier
            }
        };
    }
}
