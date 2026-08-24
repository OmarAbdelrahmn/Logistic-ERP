using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public interface IWorkforceService
{
    Task<Result<IReadOnlyList<EmployeeListItemResponse>>> GetEmployeesAsync(CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> GetEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> CreateEmployeeAsync(EmployeeUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> UpdateEmployeeAsync(Guid employeeId, EmployeeUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveEmployeeAsync(Guid employeeId, ArchiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> ChangeStatusAsync(Guid employeeId, ChangeEmployeeStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> ChangeRoleAsync(Guid employeeId, ChangeEmployeeRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmployeeWorkHistoryResponse>>> GetWorkHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RiderDetailsResponse>>> GetRidersAsync(bool? outsideOnly, CancellationToken cancellationToken = default);
    Task<Result<RiderDetailsResponse>> UpdateRiderProfileAsync(Guid riderProfileId, RiderProfileUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SponsorResponse>>> GetSponsorsAsync(CancellationToken cancellationToken = default);
    Task<Result<SponsorResponse>> GetSponsorAsync(Guid sponsorId, CancellationToken cancellationToken = default);
    Task<Result<SponsorResponse>> UpsertSponsorAsync(Guid? sponsorId, SponsorUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveSponsorAsync(Guid sponsorId, ArchiveRequest request, CancellationToken cancellationToken = default);
}
