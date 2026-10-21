using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezApprovalRequestResponse(Guid Id, Guid HandoverId, JahezApprovalKind Kind,
    JahezApprovalStatus Status, Guid RequestedByUserId, Guid? OperationId, Guid? TargetAccountId,
    decimal WaiverAmount, DateOnly? FromDate, DateOnly? ToDate, DateTimeOffset? EffectiveAtUtc,
    string Reason, JahezAdjustmentTarget? AdjustmentTarget, decimal? ProposedNet,
    decimal? ProposedCommission, decimal? DebtAdjustment,
    DateTimeOffset CreatedAtUtc, Guid? CreatedByUserId, DateTimeOffset? UpdatedAtUtc, Guid? UpdatedByUserId,
    byte[] RowVersion, bool IsDeleted, DateTimeOffset? DeletedAtUtc, Guid? DeletedByUserId, string? DeletionReason,
    JahezAccountPerson? UserRider, JahezApprovalAssignment? Assignment,
    JahezApprovalAccount? Account, JahezApprovalAccount? TargetAccount, JahezOperation? Operation)
{
    public JahezSettlementAmounts? Amounts => JahezSettlementAmounts.From(Operation);
}

public sealed record JahezApprovalAssignment(Guid AssignmentId, Guid HandoverId, Guid AccountId,
    Guid RiderProfileId, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc,
    DateOnly? EffectiveFrom, DateOnly? EffectiveTo, string? Status, string? StartReason, string? EndReason,
    DateOnly CommissionStartsOn, DateOnly? CommissionPostedThrough, DateTimeOffset? LastSettlementPaymentAtUtc);

public sealed record JahezApprovalAccount(Guid AccountId, string Code, string ExternalAccountId,
    string? UserName, string Status, string? StatusReason, Guid OperatingCityId, string? CityNameAr,
    Guid? SponsorId, string? SponsorNameAr);

public sealed record JahezWorkflowApprovalDetails(JahezApprovalRequestResponse Request,
    IReadOnlyList<JahezApprovalDecision> Decisions);
