using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Hunger;

public sealed class HungerFinanceConfiguration : AuditableEntity
{
    public Guid ClientPlatformId { get; set; }
    public Guid SupervisorEmployeeId { get; set; }
    public Guid SponsorId { get; set; }
    public Guid CustodySubAccountId { get; set; }
    public Guid CollectionsSubAccountId { get; set; }
    public Guid WalletSubAccountId { get; set; }
    public Guid RiderClearingSubAccountId { get; set; }
    public string EvidenceReference { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class HungerFinancialEvent : HistoryEntity
{
    public Guid ClientPlatformId { get; set; }
    public Guid SupervisorEmployeeId { get; set; }
    public Guid? PlatformRiderAccountId { get; set; }
    public Guid? RiderProfileId { get; set; }
    public Guid? RelatedEventId { get; set; }
    public Guid? FromSubAccountId { get; set; }
    public Guid? ToSubAccountId { get; set; }
    public string Status { get; set; } = "Posted";
    public decimal? SourceWalletBalance { get; set; }
    public string? DestinationType { get; set; }
    public Guid? DestinationOwnerId { get; set; }
    public string? TransferReference { get; set; }
    public DateOnly? TransferDate { get; set; }
    public string? CompletionKey { get; set; }
    public string? CompletionHash { get; set; }
    public string? ArrivalEvidence { get; set; }
    public DateOnly? CompletionDate { get; set; }
    public string Kind { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RiderBalanceChange { get; set; }
    public DateOnly BusinessDate { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
    public string DetailsJson { get; set; } = "{}";
    // Accountant seal: NotRequired | PendingSeal | Sealed | Rejected | Cancelled.
    public string ApprovalStatus { get; set; } = "NotRequired";
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    // A replacement for a rejected row. It carries no postings until it is sealed.
    public Guid? SupersedesEventId { get; set; }
    // Custody | Collections | Wallet | RiderClearing | OwnerCollections.
    public string? Role { get; set; }
    // Stc | Cashbox | Bank | OwnerVault.
    public string? DestinationChannel { get; set; }
    public Guid? ExceptionApprovalId { get; set; }
    // Account codes chosen by the accountant at seal. Stored verbatim; the ERP never interprets them.
    public string? SealAccountsJson { get; set; }
}

/// <summary>Server-issued screenshot. Clients reference it by id and never supply a storage link.</summary>
public sealed class HungerEvidenceFile : AuditableEntity
{
    public Guid ClientPlatformId { get; set; }
    public Guid PlatformRiderAccountId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    // WalletBefore | WalletAfter | Wallet | Arrival.
    public string Purpose { get; set; } = string.Empty;
    public DateOnly ObservedOn { get; set; }
    public Guid? FinancialEventId { get; set; }
}

public sealed class HungerExceptionApproval : AuditableEntity
{
    public Guid ClientPlatformId { get; set; }
    public Guid? PlatformRiderAccountId { get; set; }
    public Guid? SupervisorEmployeeId { get; set; }
    // NonZeroHandover | CustodyIdentityMismatch.
    public string Kind { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    // Requested | Approved | Rejected | Consumed.
    public string Status { get; set; } = "Requested";
    public Guid RequestedByUserId { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
    public string? DecisionNote { get; set; }
    public Guid? ConsumedByEventId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
}

public sealed class HungerRemittanceLine : AuditableEntity
{
    public Guid RemittanceEventId { get; set; }
    public Guid SourceEventId { get; set; }
    public decimal Amount { get; set; }
    public decimal ConfirmedAmount { get; set; }
}

public sealed class HungerWalletReview : AuditableEntity
{
    public string Status { get; set; } = "Pending";
    public Guid? SwapId { get; set; }
    public Guid PlatformRiderAccountId { get; set; }
    public Guid UserId { get; set; }
    public string InputRevision { get; set; } = string.Empty;
    public string RequestJson { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class HungerReceiptAllocation : HistoryEntity
{
    public Guid ConfirmationEventId { get; set; }
    public Guid RemittanceLineId { get; set; }
    public decimal Amount { get; set; }
}
