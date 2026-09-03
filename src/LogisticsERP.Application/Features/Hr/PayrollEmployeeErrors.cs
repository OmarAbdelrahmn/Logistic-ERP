using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public static class PayrollEmployeeErrors
{
    public static readonly OperationError InvalidRequest = new(
        "payroll_employee.invalid_request",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation);

    public static readonly OperationError InvalidNationalId = new(
        "payroll_employee.invalid_national_id",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation,
        "nationalId");

    public static readonly OperationError InvalidIban = new(
        "payroll_employee.invalid_iban",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation,
        "personalIban");

    public static readonly OperationError NotFound = new(
        "payroll_employee.not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        "id");

    public static readonly OperationError SponsorNotFound = new(
        "payroll_employee.sponsor_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation,
        "sponsorId");

    public static readonly OperationError DuplicateNumber = new(
        "payroll_employee.duplicate_number",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "number");

    public static readonly OperationError DuplicateNationalId = new(
        "payroll_employee.duplicate_national_id",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "nationalId");

    public static readonly OperationError DuplicateIban = new(
        "payroll_employee.duplicate_iban",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "personalIban");

    public static readonly OperationError ConcurrencyConflict = new(
        "payroll_employee.concurrency_conflict",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict);

    public static readonly OperationError PersistenceConflict = new(
        "payroll_employee.persistence_conflict",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict);
}
