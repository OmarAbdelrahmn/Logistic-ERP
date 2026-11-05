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
    public Guid PlatformRiderAccountId { get; set; }
    public Guid UserId { get; set; }
    public string InputRevision { get; set; } = string.Empty;
    public string RequestJson { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
