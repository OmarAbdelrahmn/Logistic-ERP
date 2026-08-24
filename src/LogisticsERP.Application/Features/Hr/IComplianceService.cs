using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public interface IComplianceService
{
    Task<Result<IReadOnlyList<DriverLicenseResponse>>> GetDriverLicensesAsync(Guid? employeeId, CancellationToken cancellationToken = default);
    Task<Result<DriverLicenseResponse>> UpsertDriverLicenseAsync(Guid employeeId, Guid? id, DriverLicenseUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RiderCardResponse>>> GetRiderCardsAsync(Guid riderProfileId, CancellationToken cancellationToken = default);
    Task<Result<RiderCardResponse>> UpsertRiderCardAsync(Guid riderProfileId, Guid? id, RiderCardUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HealthCardResponse>>> GetHealthCardsAsync(Guid riderProfileId, CancellationToken cancellationToken = default);
    Task<Result<HealthCardResponse>> UpsertHealthCardAsync(Guid riderProfileId, Guid? id, HealthCardUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PromissoryNoteResponse>>> GetPromissoryNotesAsync(Guid? employeeId, CancellationToken cancellationToken = default);
    Task<Result<PromissoryNoteResponse>> UpsertPromissoryNoteAsync(Guid employeeId, Guid? id, PromissoryNoteUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InsuranceCompanyResponse>>> GetInsuranceCompaniesAsync(CancellationToken cancellationToken = default);
    Task<Result<InsuranceCompanyResponse>> UpsertInsuranceCompanyAsync(Guid? id, InsuranceCompanyUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InsurancePlanResponse>>> GetInsurancePlansAsync(Guid insuranceCompanyId, CancellationToken cancellationToken = default);
    Task<Result<InsurancePlanResponse>> UpsertInsurancePlanAsync(Guid insuranceCompanyId, Guid? id, InsurancePlanUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MedicalInsurancePolicyResponse>>> GetMedicalInsurancePoliciesAsync(Guid? employeeId, CancellationToken cancellationToken = default);
    Task<Result<MedicalInsurancePolicyResponse>> UpsertMedicalInsurancePolicyAsync(Guid employeeId, Guid? id, MedicalInsurancePolicyUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(string resource, Guid id, ArchiveRequest request, CancellationToken cancellationToken = default);
}
