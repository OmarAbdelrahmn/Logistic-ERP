using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Company;

public static class CompanyErrors
{
    public static readonly OperationError InvalidRequest = new(
        "company.invalid_request", "طلب ملف الشركة غير صالح.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "company.not_found", "لم يتم العثور على ملف الشركة.", ErrorType.NotFound);
    public static readonly OperationError ConcurrencyConflict = new(
        "company.concurrency_conflict", "تغير ملف الشركة بعد تحميله.", ErrorType.Conflict);
    public static readonly OperationError CurrentUserUnavailable = new(
        "company.current_user_unavailable", "تعذر تحديد هوية المستخدم المصادق عليه.", ErrorType.Unauthorized);
}
