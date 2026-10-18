using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Chefz;

public enum ChefzAccountType { Freelancer = 1, FullTime = 2 }

public sealed class ChefzAccountDetails : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public string ReportIdNumber { get; set; } = string.Empty;
    public ChefzAccountType AccountType { get; set; }
    public string VehicleType { get; set; } = "Car";
    public string DashboardStatus { get; set; } = "active";
}

public sealed class ChefzPerformanceSettings : AuditableEntity
{
    public static readonly Guid SingletonId = Guid.Parse("019d18d5-62e1-7000-8000-000000000001");
    public int OverallAOrders { get; set; } = 80;
    public int OverallBOrders { get; set; } = 60;
    public int OverallCOrders { get; set; } = 40;
    public int DailyAOrders { get; set; } = 7;
    public int DailyBOrders { get; set; } = 5;
    public int DailyCOrders { get; set; } = 3;
    public decimal AAcceptanceRate { get; set; } = 80;
    public decimal BAcceptanceRate { get; set; } = 70;
    public decimal CAcceptanceRate { get; set; } = 60;
    public int ChangeAfterNoOrderDays { get; set; } = 2;
    public bool FollowUpOnZeroOrdersWithMissedOffers { get; set; } = true;
}

// Manually maintained sponsor statistics, matching the supplied application statistics sheet.
public sealed class ChefzSponsorStatistics : AuditableEntity
{
    public Guid SponsorId { get; set; }
    public int SponsorshipCount { get; set; }
    public int Working { get; set; }
    public int NotWorking { get; set; }
    public int Blocked { get; set; }
    public int PermanentlyBlocked { get; set; }
    public int UpdateBlocked { get; set; }
    public int Uploaded { get; set; }
    public int PrivateLicense { get; set; }
    public int PendingUpload { get; set; }
    public int NoLicense { get; set; }
    public int ManagementSuspended { get; set; }
    public int Motorcycle { get; set; }
    public int WorkingOutsideSponsorship { get; set; }
    public int? WorkingWithoutPerformance { get; set; }
}

public sealed class ChefzWalletSettlement : HistoryEntity
{
    public string Source { get; set; } = "Wallet";
    public Guid? ImportId { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public string RiderName { get; set; } = string.Empty;
    public int SourceRows { get; set; }
    public decimal DeliveryFees { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Tips { get; set; }
    public decimal Compensation { get; set; }
    public decimal Bonuses { get; set; }
    public decimal Deductions { get; set; }
    public Guid PlatformRiderAccountId { get; set; }
    public Guid RiderClientAssignmentId { get; set; }
    public Guid RiderProfileId { get; set; }
    public ChefzAccountType AccountType { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly ChargedFrom { get; set; }
    public DateOnly ChargedTo { get; set; }
    public int AssignedDays { get; set; }
    public decimal StartWalletAmount { get; set; }
    public decimal EndWalletAmount { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal DailyFee { get; set; }
    public decimal CompanyAmount { get; set; }
    public decimal RiderAmount { get; set; }
}

public sealed class ChefzOrderSettlementImport : HistoryEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int SourceRows { get; set; }
}

public sealed class ChefzPerformanceImport : HistoryEntity
{
    public DateOnly ReportDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
}

public sealed class ChefzDailyPerformance : HistoryEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid RiderClientAssignmentId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid ImportId { get; set; }
    public DateOnly ReportDate { get; set; }
    public string ReportIdNumber { get; set; } = string.Empty;
    public int SourceRowNumber { get; set; }
    public int TotalValidOffers { get; set; }
    public int AcceptedOffers { get; set; }
    public int DeclinedOffers { get; set; }
    public int MissedOffers { get; set; }
    // Percentage points: 66.67 means 66.67%, not 0.6667.
    public decimal AcceptanceRate { get; set; }
}
