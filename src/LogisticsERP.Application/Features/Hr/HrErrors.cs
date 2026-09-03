using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Hr;

public static class HrErrors
{
    public static OperationError Required(string field) => new(
        $"housing.{field}_required", $"تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation, field);

    public static OperationError Invalid(string field, string requirement) => new(
        $"housing.invalid_{field}", $"تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation, field);

    public static readonly OperationError CityNotFound = new(
        "housing.city_not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation, "cityId");

    public static readonly OperationError HousingNotFound = new(
        "housing.not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound, "id");

    public static readonly OperationError HousingNotActive = new(
        "housing.not_active", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict, "id");

    public static readonly OperationError EmployeeNotFound = new(
        "housing.employee_not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound, "employeeId");

    public static readonly OperationError ResidencePeriodNotFound = new(
        "housing.residence_period_not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound, "periodId");

    public static readonly OperationError SupervisorPeriodNotFound = new(
        "housing.supervisor_period_not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound, "periodId");

    public static readonly OperationError InvalidRequest = new(
        "hr.invalid_request", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "hr.not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
    public static readonly OperationError Duplicate = new(
        "hr.duplicate", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError Conflict = new(
        "hr.conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "hr.concurrency_conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError UnsupportedPlatformPaymentModel = new(
        "platform.payment_model_not_supported", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict, "paymentModel");
    public static readonly OperationError PlatformPaymentModelsInUse = new(
        "platform.payment_models_in_use", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict, "supportedPaymentModels");
    public static readonly OperationError RiderAccountLimitReached = new(
        "platform.rider_account_limit_reached", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict, "actualRiderProfileId");
    public static readonly OperationError RiderSalaryAccountLimitReached = new(
        "platform.rider_salary_account_limit_reached", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict, "actualRiderProfileId");
    public static OperationError RiderProfileNotFound(string field, Guid riderProfileId) => new(
        "platform.rider_profile_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        field,
        new Dictionary<string, object?> { ["riderProfileId"] = riderProfileId });
    public static OperationError RiderProfileUnavailable(string field, Guid riderProfileId) => new(
        "platform.rider_profile_unavailable",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        field,
        new Dictionary<string, object?> { ["riderProfileId"] = riderProfileId });
    public static OperationError PlatformAccountNotFound(Guid accountId) => new(
        "platform.account_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        "id",
        new Dictionary<string, object?> { ["accountId"] = accountId });
    public static OperationError PlatformAccountUnavailable(Guid accountId, PlatformRiderAccountStatus status) => new(
        "platform.account_unavailable",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "id",
        new Dictionary<string, object?> { ["accountId"] = accountId, ["accountStatus"] = status.ToString() });
    public static OperationError PlatformAccountOwnerNotFound(Guid accountId, Guid ownerEmployeeId) => new(
        "platform.account_owner_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "ownerRiderProfileId",
        new Dictionary<string, object?> { ["accountId"] = accountId, ["ownerEmployeeId"] = ownerEmployeeId });
    public static readonly OperationError CurrentUserUnavailable = new(
        "hr.current_user_unavailable", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Unauthorized);
    public static readonly OperationError InvalidFile = new(
        "documents.invalid_file",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation);
    public static readonly OperationError InvalidDocumentMetadata = new(
        "documents.invalid_metadata",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation);
    public static readonly OperationError FileMissing = new(
        "documents.file_missing", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
    public static readonly OperationError CapacityExceeded = new(
        "housing.capacity_exceeded", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
}
