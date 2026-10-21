using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Jahez;

public enum TreasuryLocation { Supervisor = 1, Central = 2, Bank = 3, PetroApp = 4 }
public enum TreasuryPurpose { Settlement = 1, AccountFee = 2, Combined = 3 }
public enum TreasuryDirections { InOnly = 1, OutOnly = 2, Dual = 3 }
public enum TreasuryEntryKind { Business = 1, Transfer = 2, Opening = 3, Adjustment = 4, Reversal = 5 }
public enum TreasurySource { Fee = 1, Operation = 2, Debt = 3, Movement = 4 }
public enum PetroEvidenceKind { Funding = 1, Statement = 2 }

public sealed class TreasurySubAccount : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Guid? SponsorId { get; set; }
    public TreasuryLocation Location { get; set; }
    public TreasuryPurpose Purpose { get; set; }
    public TreasuryDirections AllowedDirections { get; set; } = TreasuryDirections.Dual;
    public bool IsPosting { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

// Ownership is evidence, independent of later account or employee sponsor changes.
public sealed class TreasuryOwnership : HistoryEntity
{
    public TreasurySource Source { get; set; }
    public Guid SourceId { get; set; }
    public Guid PlatformAccountId { get; set; }
    public Guid SponsorId { get; set; }
    public string SponsorIdentifier { get; set; } = string.Empty;
    public string SponsorNameAr { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
}

public sealed class TreasuryPosting : HistoryEntity
{
    public Guid SubAccountId { get; set; }
    public Guid SponsorId { get; set; }
    public Guid? MoneyMovementId { get; set; }
    public Guid? CashboxHandoverId { get; set; }
    public Guid BatchId { get; set; }
    public int Revision { get; set; }
    public Guid? ReversesPostingId { get; set; }
    public TreasuryEntryKind Kind { get; set; }
    // Signed: incoming positive, outgoing negative. Original business purpose remains on MoneyMovement.
    public decimal Amount { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorNameAr { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TreasuryState : AuditableEntity
{
    public static readonly Guid SingletonId = Guid.Parse("019d0000-0000-7000-8000-000000000001");
    public bool IsActive { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public long NextHandoverNumber { get; set; } = 1;
}

// Finance verifies each posting account, including zero balances, before activation.
public sealed class TreasuryBalanceVerification : HistoryEntity
{
    public Guid SubAccountId { get; set; }
    public decimal VerifiedBalance { get; set; }
    public DateTimeOffset VerifiedAtUtc { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TreasuryHandoverLine : HistoryEntity
{
    public Guid HandoverId { get; set; }
    public Guid SubAccountId { get; set; }
    public Guid SponsorId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountNameAr { get; set; } = string.Empty;
    public string SponsorNameAr { get; set; } = string.Empty;
    public string SponsorIdentifier { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int MovementCount { get; set; }
}

public sealed class TreasuryHandoverPosting : HistoryEntity
{
    public Guid HandoverId { get; set; }
    public Guid PostingId { get; set; }
}

// Append-only replacement chains preserve the original statement/funding evidence.
public sealed class PetroAppEvidence : HistoryEntity
{
    public Guid SponsorId { get; set; }
    public PetroEvidenceKind Kind { get; set; }
    public DateOnly BusinessDate { get; set; }
    public decimal Amount { get; set; }
    public decimal AllocatedCollections { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid? ReplacesId { get; set; }
}
