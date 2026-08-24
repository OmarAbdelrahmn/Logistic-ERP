using LogisticsERP.Application.Common.Results;

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
