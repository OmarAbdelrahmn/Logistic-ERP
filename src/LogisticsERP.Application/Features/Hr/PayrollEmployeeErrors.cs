using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public static class PayrollEmployeeErrors
{
    public static readonly OperationError InvalidRequest = new(
        "payroll_employee.invalid_request",
        "The payroll employee request contains invalid or incomplete data.",
        ErrorType.Validation);

    public static readonly OperationError InvalidNationalId = new(
        "payroll_employee.invalid_national_id",
        "The national ID must contain exactly 10 digits.",
        ErrorType.Validation,
        "nationalId");

    public static readonly OperationError InvalidIban = new(
        "payroll_employee.invalid_iban",
        "The personal IBAN must be a valid 24-character Saudi IBAN.",
        ErrorType.Validation,
        "personalIban");

    public static readonly OperationError NotFound = new(
        "payroll_employee.not_found",
        "The requested payroll employee was not found.",
        ErrorType.NotFound,
        "id");

    public static readonly OperationError SponsorNotFound = new(
        "payroll_employee.sponsor_not_found",
        "The selected sponsor was not found or is not active.",
        ErrorType.Validation,
        "sponsorId");

    public static readonly OperationError DuplicateNumber = new(
        "payroll_employee.duplicate_number",
        "Another payroll employee already uses this number.",
        ErrorType.Conflict,
        "number");

    public static readonly OperationError DuplicateNationalId = new(
        "payroll_employee.duplicate_national_id",
        "Another payroll employee already uses this national ID.",
        ErrorType.Conflict,
        "nationalId");

    public static readonly OperationError DuplicateIban = new(
        "payroll_employee.duplicate_iban",
        "Another payroll employee already uses this personal IBAN.",
        ErrorType.Conflict,
        "personalIban");

    public static readonly OperationError ConcurrencyConflict = new(
        "payroll_employee.concurrency_conflict",
        "The payroll employee changed after it was loaded. Reload it and retry.",
        ErrorType.Conflict);

    public static readonly OperationError PersistenceConflict = new(
        "payroll_employee.persistence_conflict",
        "The payroll employee could not be saved because its unique data conflicts with another record.",
        ErrorType.Conflict);
}
