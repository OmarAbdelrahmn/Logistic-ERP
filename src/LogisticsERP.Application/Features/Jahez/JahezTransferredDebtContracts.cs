using System.Text.Json.Serialization;
using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezSettlementAmounts(decimal DashboardDebt, decimal Commission, decimal Total)
{
    public static JahezSettlementAmounts? From(JahezOperation? operation) => operation is null ? null
        : new(operation.Net, operation.Commission, operation.Remaining);
}

public sealed record JahezRiderTransferredDebtSummary(Guid RiderProfileId, JahezAccountPerson? UserRider,
    int UnpaidDebtCount, decimal OutstandingAmount, JahezSettlementAmounts? Amounts)
{
    public decimal PaidAmount { get; init; }
    public decimal? TotalCommission { get; init; }
}

public sealed record JahezRiderDebtPartialRepayment(decimal Amount, JahezSettlementAmounts ExpectedAmounts,
    JahezMoneyFund Fund, string Reference, string Reason);
public sealed record JahezRiderDebtPaymentAllocation(Guid DebtId, Guid MovementId,
    decimal DashboardDebtPaid, decimal CommissionPaid, decimal RemainingAmount, bool IsPaid);
public sealed record JahezRiderDebtPartialRepaymentResponse(Guid RiderProfileId, decimal PaidAmount,
    decimal DashboardDebtPaid, decimal CommissionPaid, JahezSettlementAmounts RemainingAmounts,
    IReadOnlyList<JahezRiderDebtPaymentAllocation> Allocations);
public sealed record JahezRiderDebtCommissionUpdate([property: JsonRequired] decimal Commission, [property: JsonRequired] decimal ExpectedCommission,
    JahezSettlementAmounts ExpectedAmounts, string Reason);
public sealed record JahezRiderDebtCommissionUpdateResponse(Guid RiderProfileId, decimal PreviousCommission,
    decimal Commission, JahezSettlementAmounts RemainingAmounts, IReadOnlyList<Guid> AdjustedDebtIds);

public sealed record JahezRiderDebtRepayment(decimal ExpectedAmount, JahezMoneyFund Fund, string Reference, string Reason);
public sealed record JahezRiderDebtRepaymentResponse(Guid RiderProfileId, decimal PaidAmount,
    IReadOnlyList<Guid> PaidDebtIds, decimal RemainingAmount);

public sealed record JahezTransferredDebtResponse(Guid Id, Guid OperationId, Guid RiderProfileId,
    decimal Amount, DateTimeOffset? PaidAtUtc, Guid? PaidByUserId,
    DateTimeOffset CreatedAtUtc, Guid? CreatedByUserId, DateTimeOffset? UpdatedAtUtc, Guid? UpdatedByUserId,
    byte[] RowVersion, bool IsDeleted, DateTimeOffset? DeletedAtUtc, Guid? DeletedByUserId, string? DeletionReason,
    JahezAccountPerson? UserRider, JahezTransferredDebtAssignment? Assignment,
    JahezTransferredDebtSourceOperation? SourceOperation)
{
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount => Amount - PaidAmount;
    public JahezSettlementAmounts? Amounts { get; init; }
}

public sealed record JahezTransferredDebtAssignment(Guid AssignmentId, Guid HandoverId, Guid AccountId,
    string? ExternalAccountId, Guid RiderProfileId, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc,
    DateOnly? EffectiveFrom, DateOnly? EffectiveTo, string? Status, string? StartReason, string? EndReason);

public sealed record JahezTransferredDebtSourceOperation(Guid Id, JahezOperationMode Mode,
    DateOnly ThroughDate, DateTimeOffset EffectiveAtUtc, decimal Net, decimal Commission,
    decimal Remaining, string Reason, Guid? ApprovedRequestId, DateTimeOffset? ExecutedAtUtc);
