using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record PayrollEmployeeResponse(
    Guid Id,
    int Number,
    Guid SponsorId,
    PayrollEmployeeSponsorResponse Sponsor,
    string Name,
    string NationalId,
    string Country,
    DateOnly JoiningDate,
    string PersonalIban,
    decimal Salary,
    string Status,
    string RowVersion);

public sealed record PayrollEmployeeSponsorResponse(
    Guid Id,
    string EmployerIdentityNumber,
    string RegistryNameAr,
    string? RegistryNameEn);

public sealed record CreatePayrollEmployeeRequest(
    int Number,
    Guid SponsorId,
    string Name,
    string NationalId,
    string Country,
    DateOnly JoiningDate,
    string PersonalIban,
    decimal Salary,
    string Status);

public sealed record UpdatePayrollEmployeeRequest(
    int Number,
    Guid SponsorId,
    string Name,
    string NationalId,
    string Country,
    DateOnly JoiningDate,
    string PersonalIban,
    decimal Salary,
    string Status,
    string RowVersion);

public interface IPayrollEmployeeService
{
    Task<Result<IReadOnlyList<PayrollEmployeeResponse>>> GetAllAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollEmployeeResponse>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollEmployeeResponse>> CreateAsync(
        CreatePayrollEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollEmployeeResponse>> UpdateAsync(
        Guid id,
        UpdatePayrollEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid id,
        string rowVersion,
        string? reason,
        CancellationToken cancellationToken = default);
}
