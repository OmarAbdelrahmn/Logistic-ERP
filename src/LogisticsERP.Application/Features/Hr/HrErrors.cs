using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Hr;

public static class HrErrors
{
    public static OperationError Required(string field) => new(
        $"housing.{field}_required", $"The '{field}' field is required.", ErrorType.Validation, field);

    public static OperationError Invalid(string field, string requirement) => new(
        $"housing.invalid_{field}", $"The '{field}' field is invalid. {requirement}", ErrorType.Validation, field);

    public static readonly OperationError CityNotFound = new(
        "housing.city_not_found", "The selected 'cityId' does not match an existing city.", ErrorType.Validation, "cityId");

    public static readonly OperationError HousingNotFound = new(
        "housing.not_found", "The requested housing record was not found.", ErrorType.NotFound, "id");

    public static readonly OperationError HousingNotActive = new(
        "housing.not_active", "Residents can only be assigned to active housing.", ErrorType.Conflict, "id");

    public static readonly OperationError EmployeeNotFound = new(
        "housing.employee_not_found", "The selected 'employeeId' does not match an existing employee.", ErrorType.NotFound, "employeeId");

    public static readonly OperationError ResidencePeriodNotFound = new(
        "housing.residence_period_not_found", "The requested residence period was not found.", ErrorType.NotFound, "periodId");

    public static readonly OperationError SupervisorPeriodNotFound = new(
        "housing.supervisor_period_not_found", "The requested supervisor period was not found.", ErrorType.NotFound, "periodId");

    public static readonly OperationError InvalidRequest = new(
        "hr.invalid_request", "The HR request contains invalid or incomplete data.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "hr.not_found", "The requested HR record was not found.", ErrorType.NotFound);
    public static readonly OperationError Duplicate = new(
        "hr.duplicate", "A record with the same unique value already exists.", ErrorType.Conflict);
    public static readonly OperationError Conflict = new(
        "hr.conflict", "The operation conflicts with the current record state.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "hr.concurrency_conflict", "The record changed after it was loaded. Reload it and retry.", ErrorType.Conflict);
    public static readonly OperationError UnsupportedPlatformPaymentModel = new(
        "platform.payment_model_not_supported", "The selected platform does not support this account payment model.", ErrorType.Conflict, "paymentModel");
    public static readonly OperationError PlatformPaymentModelsInUse = new(
        "platform.payment_models_in_use", "A payment model cannot be disabled while non-archived accounts still use it.", ErrorType.Conflict, "supportedPaymentModels");
    public static readonly OperationError RiderAccountLimitReached = new(
        "platform.rider_account_limit_reached", "A rider can have at most two active platform accounts.", ErrorType.Conflict, "actualRiderProfileId");
    public static readonly OperationError RiderSalaryAccountLimitReached = new(
        "platform.rider_salary_account_limit_reached", "A rider can have at most one active salary platform account.", ErrorType.Conflict, "actualRiderProfileId");
    public static OperationError RiderProfileNotFound(string field, Guid riderProfileId) => new(
        "platform.rider_profile_not_found",
        "The supplied rider profile was not found. Send the riderProfileId from the employee response, not the employee, account, or assignment ID.",
        ErrorType.NotFound,
        field,
        new Dictionary<string, object?> { ["riderProfileId"] = riderProfileId });
    public static OperationError RiderProfileUnavailable(string field, Guid riderProfileId) => new(
        "platform.rider_profile_unavailable",
        "The supplied rider profile belongs to an inactive or ineligible rider and cannot receive a platform account.",
        ErrorType.Conflict,
        field,
        new Dictionary<string, object?> { ["riderProfileId"] = riderProfileId });
    public static OperationError PlatformAccountNotFound(Guid accountId) => new(
        "platform.account_not_found",
        "The platform account in the assignment URL was not found.",
        ErrorType.NotFound,
        "id",
        new Dictionary<string, object?> { ["accountId"] = accountId });
    public static OperationError PlatformAccountUnavailable(Guid accountId, PlatformRiderAccountStatus status) => new(
        "platform.account_unavailable",
        "The platform account is not available for assignment. Release its current assignment before assigning it again.",
        ErrorType.Conflict,
        "id",
        new Dictionary<string, object?> { ["accountId"] = accountId, ["accountStatus"] = status.ToString() });
    public static OperationError PlatformAccountOwnerNotFound(Guid accountId, Guid ownerEmployeeId) => new(
        "platform.account_owner_not_found",
        "The platform account's registered owner is missing or is not an eligible rider. Correct the account owner before assigning the account.",
        ErrorType.Conflict,
        "ownerRiderProfileId",
        new Dictionary<string, object?> { ["accountId"] = accountId, ["ownerEmployeeId"] = ownerEmployeeId });
    public static readonly OperationError CurrentUserUnavailable = new(
        "hr.current_user_unavailable", "The authenticated user could not be resolved.", ErrorType.Unauthorized);
    public static readonly OperationError InvalidFile = new(
        "documents.invalid_file",
        "The file is empty, exceeds the 10 MB limit, has an unsupported format, or its content does not match its file type. Allowed formats: PDF, JPG/JPEG, PNG, WEBP, GIF, and BMP.",
        ErrorType.Validation);
    public static readonly OperationError InvalidDocumentMetadata = new(
        "documents.invalid_metadata",
        "The document metadata is incomplete or invalid. Residency permits require a document number, issue date, and expiry date. The expiry date must be on or after the issue date.",
        ErrorType.Validation);
    public static readonly OperationError FileMissing = new(
        "documents.file_missing", "The stored file could not be found.", ErrorType.NotFound);
    public static readonly OperationError CapacityExceeded = new(
        "housing.capacity_exceeded", "The housing capacity would be exceeded. Supply an approved override reason.", ErrorType.Conflict);
}
