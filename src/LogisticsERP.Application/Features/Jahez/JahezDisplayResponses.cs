using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Application.Features.Jahez;

// Display data is resolved after authorization and does not change stored financial evidence.
public abstract record JahezNamedResponse
{
    public JahezAccountDisplay? Account { get; init; }
    public Guid? OwnerRiderProfileId { get; init; }
    public Guid? OwnerEmployeeId { get; init; }
    public string? OwnerRiderNameAr { get; init; }
    public string? OwnerRiderNameEn { get; init; }
    public Guid? ActualRiderProfileId { get; init; }
    public Guid? ActualEmployeeId { get; init; }
    public string? ActualRiderNameAr { get; init; }
    public string? ActualRiderNameEn { get; init; }
    public string? CreatedByUserNameAr { get; init; }
    public string? CreatedByUserNameEn { get; init; }
    public string? UpdatedByUserNameAr { get; init; }
    public string? UpdatedByUserNameEn { get; init; }
    public string? DeletedByUserNameAr { get; init; }
    public string? DeletedByUserNameEn { get; init; }
    public string? RequestedByUserNameAr { get; init; }
    public string? RequestedByUserNameEn { get; init; }
    public string? CollectedByUserNameAr { get; init; }
    public string? CollectedByUserNameEn { get; init; }
    public string? ActorUserNameAr { get; init; }
    public string? ActorUserNameEn { get; init; }
    public string? UploadedByUserNameAr { get; init; }
    public string? UploadedByUserNameEn { get; init; }
    public string? AccountantUserNameAr { get; init; }
    public string? AccountantUserNameEn { get; init; }
    public string? ApprovedByUserNameAr { get; init; }
    public string? ApprovedByUserNameEn { get; init; }
    public JahezAccountDisplay? TargetAccount { get; init; }
}

public sealed record JahezAccountDisplay(Guid Id, string Code, string ExternalAccountId);
public sealed record JahezApprovalDetailsResponse(JahezApprovalRequestResponse Request,
    IReadOnlyList<JahezApprovalDecisionResponse> Decisions);

public interface IJahezResponseMapper
{
    Task<object?> MapAsync(object? value, CancellationToken ct = default);
}

public sealed record JahezAccountFeeResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public Guid? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeletedAtUtc { get; init; }
    public Guid? DeletedByUserId { get; init; }
    public string? DeletionReason { get; init; }
    public Guid HandoverId { get; init; }
    public decimal Amount { get; init; }
    public decimal WaivedAmount { get; init; }
    public Guid? ApprovalRequestId { get; init; }

    public JahezAccountFeeResponse(JahezAccountFee source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        UpdatedAtUtc = source.UpdatedAtUtc;
        UpdatedByUserId = source.UpdatedByUserId;
        RowVersion = source.RowVersion;
        IsDeleted = source.IsDeleted;
        DeletedAtUtc = source.DeletedAtUtc;
        DeletedByUserId = source.DeletedByUserId;
        DeletionReason = source.DeletionReason;
        HandoverId = source.HandoverId;
        Amount = source.Amount;
        WaivedAmount = source.WaivedAmount;
        ApprovalRequestId = source.ApprovalRequestId;
    }
}

public sealed record JahezApprovalRequestResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public Guid? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeletedAtUtc { get; init; }
    public Guid? DeletedByUserId { get; init; }
    public string? DeletionReason { get; init; }
    public Guid HandoverId { get; init; }
    public JahezApprovalKind Kind { get; init; }
    public JahezApprovalStatus Status { get; init; }
    public Guid RequestedByUserId { get; init; }
    public Guid? TargetAccountId { get; init; }
    public decimal WaiverAmount { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public DateTimeOffset? EffectiveAtUtc { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ExternalResetReference { get; init; }

    public JahezApprovalRequestResponse(JahezApprovalRequest source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        UpdatedAtUtc = source.UpdatedAtUtc;
        UpdatedByUserId = source.UpdatedByUserId;
        RowVersion = source.RowVersion;
        IsDeleted = source.IsDeleted;
        DeletedAtUtc = source.DeletedAtUtc;
        DeletedByUserId = source.DeletedByUserId;
        DeletionReason = source.DeletionReason;
        HandoverId = source.HandoverId;
        Kind = source.Kind;
        Status = source.Status;
        RequestedByUserId = source.RequestedByUserId;
        TargetAccountId = source.TargetAccountId;
        WaiverAmount = source.WaiverAmount;
        FromDate = source.FromDate;
        ToDate = source.ToDate;
        EffectiveAtUtc = source.EffectiveAtUtc;
        Reason = source.Reason;
        ExternalResetReference = source.ExternalResetReference;
    }
}

public sealed record JahezApprovalDecisionResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public Guid RequestId { get; init; }
    public Guid ActorUserId { get; init; }
    public JahezApprovalStatus Status { get; init; }
    public DateTimeOffset DecidedAtUtc { get; init; }
    public string Reason { get; init; } = string.Empty;

    public JahezApprovalDecisionResponse(JahezApprovalDecision source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        RequestId = source.RequestId;
        ActorUserId = source.ActorUserId;
        Status = source.Status;
        DecidedAtUtc = source.DecidedAtUtc;
        Reason = source.Reason;
    }
}

public sealed record JahezCommissionPolicyPeriodResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public Guid HandoverId { get; init; }
    public Guid ApprovalRequestId { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public decimal Rate { get; init; }

    public JahezCommissionPolicyPeriodResponse(JahezCommissionPolicyPeriod source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        HandoverId = source.HandoverId;
        ApprovalRequestId = source.ApprovalRequestId;
        FromDate = source.FromDate;
        ToDate = source.ToDate;
        Rate = source.Rate;
    }
}

public sealed record JahezEarningsStatementResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public Guid HandoverId { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public decimal TotalDeliveryPrice { get; init; }
    public decimal TotalPenalties { get; init; }
    public decimal TotalCashAmount { get; init; }
    public decimal TotalDriverDebit { get; init; }
    public decimal TotalServiceDeduction { get; init; }
    public decimal TotalDriverCredit { get; init; }
    public decimal TotalBonuses { get; init; }
    public decimal TotalTips { get; init; }
    public decimal TotalFreeOrders { get; init; }
    public Guid? SupersedesId { get; init; }
    public string Reason { get; init; } = string.Empty;

    public JahezEarningsStatementResponse(JahezEarningsStatement source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        HandoverId = source.HandoverId;
        FromDate = source.FromDate;
        ToDate = source.ToDate;
        TotalDeliveryPrice = source.TotalDeliveryPrice;
        TotalPenalties = source.TotalPenalties;
        TotalCashAmount = source.TotalCashAmount;
        TotalDriverDebit = source.TotalDriverDebit;
        TotalServiceDeduction = source.TotalServiceDeduction;
        TotalDriverCredit = source.TotalDriverCredit;
        TotalBonuses = source.TotalBonuses;
        TotalTips = source.TotalTips;
        TotalFreeOrders = source.TotalFreeOrders;
        SupersedesId = source.SupersedesId;
        Reason = source.Reason;
    }
}

public sealed record JahezImportBatchResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public Guid? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeletedAtUtc { get; init; }
    public Guid? DeletedByUserId { get; init; }
    public string? DeletionReason { get; init; }
    public JahezImportKind Kind { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public Guid UploadedByUserId { get; init; }
    public DateTimeOffset? CommittedAtUtc { get; init; }
    public Guid? ReplacesBatchId { get; init; }
    public string? CorrectionReason { get; init; }

    public JahezImportBatchResponse(JahezImportBatch source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        UpdatedAtUtc = source.UpdatedAtUtc;
        UpdatedByUserId = source.UpdatedByUserId;
        RowVersion = source.RowVersion;
        IsDeleted = source.IsDeleted;
        DeletedAtUtc = source.DeletedAtUtc;
        DeletedByUserId = source.DeletedByUserId;
        DeletionReason = source.DeletionReason;
        Kind = source.Kind;
        ContentHash = source.ContentHash;
        UploadedByUserId = source.UploadedByUserId;
        CommittedAtUtc = source.CommittedAtUtc;
        ReplacesBatchId = source.ReplacesBatchId;
        CorrectionReason = source.CorrectionReason;
    }
}

public sealed record JahezRiderSettlementResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public Guid HandoverId { get; init; }
    public DateOnly ThroughDate { get; init; }
    public DateTimeOffset RecordedAtUtc { get; init; }
    public Guid CollectedByUserId { get; init; }
    public decimal FeePayment { get; init; }
    public decimal DebtPayment { get; init; }
    public decimal CommissionPayment { get; init; }
    public bool CountsAsSettlement { get; init; }
    public string Reason { get; init; } = string.Empty;

    public JahezRiderSettlementResponse(JahezRiderSettlement source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        HandoverId = source.HandoverId;
        ThroughDate = source.ThroughDate;
        RecordedAtUtc = source.RecordedAtUtc;
        CollectedByUserId = source.CollectedByUserId;
        FeePayment = source.FeePayment;
        DebtPayment = source.DebtPayment;
        CommissionPayment = source.CommissionPayment;
        CountsAsSettlement = source.CountsAsSettlement;
        Reason = source.Reason;
    }
}

public sealed record JahezLedgerEntryResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public Guid HandoverId { get; init; }
    public JahezLedgerBucket Bucket { get; init; }
    public JahezLedgerKind Kind { get; init; }
    public decimal Amount { get; init; }
    public Guid SourceId { get; init; }
    public Guid? ReversesEntryId { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateOnly? FromDate { get; init; }
    public DateOnly? ThroughDate { get; init; }
    public string? CalculationJson { get; init; }

    public JahezLedgerEntryResponse(JahezLedgerEntry source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        HandoverId = source.HandoverId;
        Bucket = source.Bucket;
        Kind = source.Kind;
        Amount = source.Amount;
        SourceId = source.SourceId;
        ReversesEntryId = source.ReversesEntryId;
        OccurredAtUtc = source.OccurredAtUtc;
        Reason = source.Reason;
        FromDate = source.FromDate;
        ThroughDate = source.ThroughDate;
        CalculationJson = source.CalculationJson;
    }
}

public sealed record JahezCashboxEntryResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public Guid? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeletedAtUtc { get; init; }
    public Guid? DeletedByUserId { get; init; }
    public string? DeletionReason { get; init; }
    public Guid SettlementId { get; init; }
    public Guid HandoverId { get; init; }
    public JahezCashboxSection Section { get; init; }
    public decimal Amount { get; init; }
    public Guid CollectedByUserId { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
    public Guid? CashboxHandoverId { get; init; }

    public JahezCashboxEntryResponse(JahezCashboxEntry source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        UpdatedAtUtc = source.UpdatedAtUtc;
        UpdatedByUserId = source.UpdatedByUserId;
        RowVersion = source.RowVersion;
        IsDeleted = source.IsDeleted;
        DeletedAtUtc = source.DeletedAtUtc;
        DeletedByUserId = source.DeletedByUserId;
        DeletionReason = source.DeletionReason;
        SettlementId = source.SettlementId;
        HandoverId = source.HandoverId;
        Section = source.Section;
        Amount = source.Amount;
        CollectedByUserId = source.CollectedByUserId;
        ReceivedAtUtc = source.ReceivedAtUtc;
        CashboxHandoverId = source.CashboxHandoverId;
    }
}

public sealed record JahezCashboxHandoverResponse : JahezNamedResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public Guid? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeletedAtUtc { get; init; }
    public Guid? DeletedByUserId { get; init; }
    public string? DeletionReason { get; init; }
    public DateOnly BusinessDate { get; init; }
    public JahezCashboxHandoverStatus Status { get; init; }
    public decimal FeeAmount { get; init; }
    public decimal SettlementAmount { get; init; }
    public decimal? AccountantFeeAmount { get; init; }
    public decimal? AccountantSettlementAmount { get; init; }
    public Guid RequestedByUserId { get; init; }
    public Guid? AccountantUserId { get; init; }
    public Guid? ApprovedByUserId { get; init; }
    public DateTimeOffset? ConfirmedAtUtc { get; init; }
    public DateTimeOffset? DecidedAtUtc { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ConfirmationReason { get; init; }
    public string? DecisionReason { get; init; }

    public JahezCashboxHandoverResponse(JahezCashboxHandover source)
    {
        Id = source.Id;
        CreatedAtUtc = source.CreatedAtUtc;
        CreatedByUserId = source.CreatedByUserId;
        UpdatedAtUtc = source.UpdatedAtUtc;
        UpdatedByUserId = source.UpdatedByUserId;
        RowVersion = source.RowVersion;
        IsDeleted = source.IsDeleted;
        DeletedAtUtc = source.DeletedAtUtc;
        DeletedByUserId = source.DeletedByUserId;
        DeletionReason = source.DeletionReason;
        BusinessDate = source.BusinessDate;
        Status = source.Status;
        FeeAmount = source.FeeAmount;
        SettlementAmount = source.SettlementAmount;
        AccountantFeeAmount = source.AccountantFeeAmount;
        AccountantSettlementAmount = source.AccountantSettlementAmount;
        RequestedByUserId = source.RequestedByUserId;
        AccountantUserId = source.AccountantUserId;
        ApprovedByUserId = source.ApprovedByUserId;
        ConfirmedAtUtc = source.ConfirmedAtUtc;
        DecidedAtUtc = source.DecidedAtUtc;
        Reason = source.Reason;
        ConfirmationReason = source.ConfirmationReason;
        DecisionReason = source.DecisionReason;
    }
}
