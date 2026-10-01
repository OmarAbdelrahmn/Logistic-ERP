using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.ErrorHandling;

internal static class FleetProblemDetails
{
    public static bool Matches(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith("/api/vehicle", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.StartsWith("/api/import/vehicle", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/api/reports/fleet/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return value.StartsWith("/api/riders/", StringComparison.OrdinalIgnoreCase)
            && (value.Contains("/vehicle-timeline", StringComparison.OrdinalIgnoreCase)
                || value.Contains("/promissory-files", StringComparison.OrdinalIgnoreCase));
    }

    public static ProblemDetails Create(
        HttpContext httpContext,
        int status,
        string? detail = null,
        string? errorCode = null)
    {
        var (title, defaultDetail, defaultCode) = status switch
        {
            StatusCodes.Status400BadRequest => ("طلب غير صالح", "تحقق من بيانات الطلب وحاول مرة أخرى.", "fleet.invalid_request"),
            StatusCodes.Status401Unauthorized => ("يلزم تسجيل الدخول", "سجل الدخول ثم حاول مرة أخرى.", "fleet.unauthorized"),
            StatusCodes.Status403Forbidden => ("غير مصرح لك", "ليس لديك صلاحية لتنفيذ هذه العملية.", "fleet.forbidden"),
            StatusCodes.Status404NotFound => ("العنوان غير موجود", "تحقق من عنوان الطلب وحاول مرة أخرى.", "fleet.not_found"),
            StatusCodes.Status405MethodNotAllowed => ("طريقة الطلب غير مدعومة", "تحقق من طريقة الطلب وحاول مرة أخرى.", "fleet.method_not_allowed"),
            StatusCodes.Status409Conflict => ("تعارض في البيانات", "تغيرت البيانات. حدّث الصفحة ثم حاول مرة أخرى.", "fleet.conflict"),
            StatusCodes.Status413PayloadTooLarge => ("الطلب كبير جدًا", "حجم الطلب أو الملف يتجاوز الحد المسموح. اختر ملفًا أصغر.", "fleet.request_too_large"),
            StatusCodes.Status415UnsupportedMediaType => ("نوع الطلب غير مدعوم", "تحقق من نوع المحتوى المرسل وحاول مرة أخرى.", "fleet.unsupported_media_type"),
            StatusCodes.Status429TooManyRequests => ("طلبات كثيرة", "انتظر قليلًا ثم حاول مرة أخرى.", "fleet.too_many_requests"),
            _ => ("تعذر إكمال الطلب", "حدث خطأ غير متوقع. تواصل مع الدعم واذكر رقم التتبع.", "fleet.unexpected_error")
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
