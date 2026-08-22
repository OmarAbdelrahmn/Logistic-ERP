using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public interface IWorkforceService
{
    Task<Result<IReadOnlyList<EmployeeListItemResponse>>> GetEmployeesAsync(CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> GetEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveEmployeeAsync(Guid employeeId, ArchiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> ChangeStatusAsync(Guid employeeId, ChangeEmployeeStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> ChangeRelationshipAsync(Guid employeeId, ChangeEmployeeRelationshipRequest request, CancellationToken cancellationToken = default);
    Task<Result<SponsoredInternalDetailsResponse>> UpdateSponsoredDetailsAsync(Guid employeeId, SponsoredInternalDetailsRequest request, CancellationToken cancellationToken = default);
    Task<Result<OutsideRiderDetailsResponse>> UpdateOutsideRiderDetailsAsync(Guid employeeId, OutsideRiderDetailsRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDetailsResponse>> AssignOperationalWorkAsync(Guid employeeId, AssignOperationalWorkRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RiderDetailsResponse>>> GetRidersAsync(bool? outsideOnly, CancellationToken cancellationToken = default);
    Task<Result<RiderDetailsResponse>> CreateRiderProfileAsync(Guid employeeId, CreateRiderProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<RiderDetailsResponse>> UpdateRiderProfileAsync(Guid riderProfileId, UpdateRiderProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SponsorResponse>>> GetSponsorsAsync(CancellationToken cancellationToken = default);
    Task<Result<SponsorResponse>> GetSponsorAsync(Guid sponsorId, CancellationToken cancellationToken = default);
    Task<Result<SponsorResponse>> UpsertSponsorAsync(Guid? sponsorId, SponsorUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveSponsorAsync(Guid sponsorId, ArchiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SponsorshipPeriodResponse>>> GetSponsorshipHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SponsorshipPeriodResponse>>> ChangeSponsorshipAsync(Guid employeeId, ChangeSponsorshipRequest request, CancellationToken cancellationToken = default);
}
