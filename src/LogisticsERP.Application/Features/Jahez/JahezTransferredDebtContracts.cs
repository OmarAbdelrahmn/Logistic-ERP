using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezSettlementAmounts(decimal DashboardDebt, decimal Commission, decimal Total)
{
    public static JahezSettlementAmounts? From(JahezOperation? operation) => operation is null ? null
        : new(operation.Net, operation.Commission, operation.Remaining);
}

public sealed record JahezRiderTransferredDebtSummary(Guid RiderProfileId, JahezAccountPerson? UserRider,
    int UnpaidDebtCount, decimal OutstandingAmount, JahezSettlementAmounts? Amounts);

public sealed record JahezRiderDebtRepayment(decimal ExpectedAmount, JahezMoneyFund Fund, string Reference, string Reason);
public sealed record JahezRiderDebtRepaymentResponse(Guid RiderProfileId, decimal PaidAmount,
    IReadOnlyList<Guid> PaidDebtIds, decimal RemainingAmount);

public sealed record JahezTransferredDebtResponse(Guid Id, Guid OperationId, Guid RiderProfileId,
    decimal Amount, DateTimeOffset? PaidAtUtc, Guid? PaidByUserId,
    DateTimeOffset CreatedAtUtc, Guid? CreatedByUserId, DateTimeOffset? UpdatedAtUtc, Guid? UpdatedByUserId,
    byte[] RowVersion, bool IsDeleted, DateTimeOffset? DeletedAtUtc, Guid? DeletedByUserId, string? DeletionReason,
    JahezAccountPerson? UserRider, JahezTransferredDebtAssignment? Assignment,
    JahezTransferredDebtSourceOperation? SourceOperation);

public sealed record JahezTransferredDebtAssignment(Guid AssignmentId, Guid HandoverId, Guid AccountId,
    string? ExternalAccountId, Guid RiderProfileId, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc,
    DateOnly? EffectiveFrom, DateOnly? EffectiveTo, string? Status, string? StartReason, string? EndReason);

public sealed record JahezTransferredDebtSourceOperation(Guid Id, JahezOperationMode Mode,
    DateOnly ThroughDate, DateTimeOffset EffectiveAtUtc, decimal Net, decimal Commission,
    decimal Remaining, string Reason, Guid? ApprovedRequestId, DateTimeOffset? ExecutedAtUtc);
