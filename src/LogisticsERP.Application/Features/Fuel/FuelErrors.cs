using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Fuel;

public static class FuelErrors
{
    public static readonly OperationError Forbidden = new(
        "fuel.forbidden", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Forbidden);

    public static readonly OperationError InvalidRequest = new(
        "fuel.invalid_request", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation);

    public static readonly OperationError InvalidProvider = new(
        "fuel.invalid_provider", "شركة الوقود غير مدعومة.", ErrorType.Validation, "provider");

    public static readonly OperationError InvalidCardNumber = new(
        "fuel.invalid_card_number", "رقم بطاقة الوقود غير صالح.", ErrorType.Validation, "cardNumber");

    public static readonly OperationError NotFound = new(
        "fuel.card_not_found", "لم يتم العثور على بطاقة الوقود.", ErrorType.NotFound, "id");

    public static readonly OperationError DuplicateCard = new(
        "fuel.duplicate_card", "رقم البطاقة مسجل مسبقًا لدى شركة الوقود نفسها.", ErrorType.Conflict, "cardNumber");

    public static readonly OperationError RiderNotFound = new(
        "fuel.rider_not_found", "لم يتم العثور على الرايدر.", ErrorType.NotFound, "riderProfileId");

    public static readonly OperationError RiderUnavailable = new(
        "fuel.rider_unavailable", "الرايدر غير متاح للإسناد.", ErrorType.Conflict, "riderProfileId");

    public static readonly OperationError ActiveAssignmentConflict = new(
        "fuel.active_assignment_conflict", "البطاقة مسندة حاليًا إلى رايدر.", ErrorType.Conflict);

    public static readonly OperationError MonthlyRiderConflict = new(
        "fuel.monthly_rider_conflict", "لا يمكن إسناد بطاقة الوقود إلى رايدرين مختلفين في الشهر نفسه.", ErrorType.Conflict);

    public static readonly OperationError AssignmentNotFound = new(
        "fuel.assignment_not_found", "لا يوجد إسناد حالي لهذه البطاقة.", ErrorType.NotFound);

    public static readonly OperationError InvalidDateRange = new(
        "fuel.invalid_date_range", "نطاق تاريخ الإسناد غير صالح.", ErrorType.Validation);

    public static readonly OperationError InvalidFile = new(
        "fuel.invalid_file", "ملف الوقود غير صالح أو لا يطابق تنسيق بترو أب أو سيارة أب.", ErrorType.Validation, "file");

    public static readonly OperationError MonthMismatch = new(
        "fuel.month_mismatch", "شهر التقرير لا يطابق الشهر المتوقع.", ErrorType.Validation, "expectedMonth");

    public static readonly OperationError CurrentUserUnavailable = new(
        "fuel.current_user_unavailable", "تعذر تحديد المستخدم الحالي.", ErrorType.Unauthorized);

    public static readonly OperationError ConcurrencyConflict = new(
        "fuel.concurrency_conflict", "تم تعديل السجل من مستخدم آخر؛ أعد تحميل البيانات.", ErrorType.Conflict);

    public static readonly OperationError PersistenceConflict = new(
        "fuel.persistence_conflict", "تعذر حفظ بيانات الوقود بسبب تعارض في البيانات.", ErrorType.Conflict);
}
