using System.Linq.Expressions;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class JahezConfigurations : IEntityTypeConfiguration<JahezAccountHandover>,
    IEntityTypeConfiguration<JahezAccountFee>,
    IEntityTypeConfiguration<JahezApprovalRequest>,
    IEntityTypeConfiguration<JahezApprovalDecision>,
    IEntityTypeConfiguration<JahezCommissionPolicyPeriod>,
    IEntityTypeConfiguration<JahezEarningsStatement>,
    IEntityTypeConfiguration<JahezImportBatch>,
    IEntityTypeConfiguration<JahezImportFile>,
    IEntityTypeConfiguration<JahezImportRow>,
    IEntityTypeConfiguration<JahezTransaction>,
    IEntityTypeConfiguration<JahezDailyDispatch>,
    IEntityTypeConfiguration<JahezRiderSettlement>,
    IEntityTypeConfiguration<JahezLedgerEntry>,
    IEntityTypeConfiguration<JahezCashboxEntry>,
    IEntityTypeConfiguration<JahezCashboxHandover>,
    IEntityTypeConfiguration<JahezReminderState>,
    IEntityTypeConfiguration<JahezCommandReceipt>
{
    public void Configure(EntityTypeBuilder<JahezAccountHandover> b)
    {
        b.ConfigureAuditable("JahezAccountHandover", "jahez");
        Values(b);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.RiderClientAssignmentId).IsUnique();
        b.HasIndex(x => x.PlatformRiderAccountId).IsUnique().HasFilter("[EndedAtUtc] IS NULL AND [IsDeleted] = 0");
        b.HasIndex(x => new { x.RiderProfileId, x.StartedAtUtc });
        b.ToTable(t => t.HasCheckConstraint("CK_JahezHandover_Range", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]"));
    }

    public void Configure(EntityTypeBuilder<JahezAccountFee> b)
    {
        b.ConfigureAuditable("JahezAccountFee", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasOne<JahezApprovalRequest>().WithMany().HasForeignKey(x => x.ApprovalRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.HandoverId).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_JahezFee_Amount", "[Amount] >= 0 AND [WaivedAmount] >= 0 AND [WaivedAmount] <= [Amount]"));
    }

    public void Configure(EntityTypeBuilder<JahezApprovalRequest> b)
    {
        b.ConfigureAuditable("JahezApprovalRequest", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.TargetAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }

    public void Configure(EntityTypeBuilder<JahezApprovalDecision> b)
    {
        b.ConfigureHistory("JahezApprovalDecision", "jahez");
        Values(b);
        b.HasOne<JahezApprovalRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.RequestId).IsUnique();
    }

    public void Configure(EntityTypeBuilder<JahezCommissionPolicyPeriod> b)
    {
        b.ConfigureHistory("JahezCommissionPolicyPeriod", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasOne<JahezApprovalRequest>().WithMany().HasForeignKey(x => x.ApprovalRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ApprovalRequestId).IsUnique();
        b.HasIndex(x => new { x.HandoverId, x.FromDate, x.ToDate });
        b.ToTable(t => t.HasCheckConstraint("CK_JahezPolicy_Range", "[ToDate] >= [FromDate] AND [Rate] = 0.15"));
    }

    public void Configure(EntityTypeBuilder<JahezEarningsStatement> b)
    {
        b.ConfigureHistory("JahezEarningsStatement", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasOne<JahezEarningsStatement>().WithMany().HasForeignKey(x => x.SupersedesId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.SupersedesId).IsUnique().HasFilter("[SupersedesId] IS NOT NULL");
        b.HasIndex(x => new { x.HandoverId, x.FromDate, x.ToDate });
        b.ToTable(t => t.HasCheckConstraint("CK_JahezEarnings_Range", "[ToDate] >= [FromDate]"));
    }

    public void Configure(EntityTypeBuilder<JahezImportBatch> b)
    {
        b.ConfigureAuditable("JahezImportBatch", "jahez");
        Values(b);
        b.Property(x => x.ContentHash).HasMaxLength(64);
        b.HasIndex(x => new { x.Kind, x.ContentHash }).IsUnique();
        b.HasOne<JahezImportBatch>().WithMany().HasForeignKey(x => x.ReplacesBatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ReplacesBatchId).IsUnique().HasFilter("[ReplacesBatchId] IS NOT NULL AND [CommittedAtUtc] IS NOT NULL");
    }

    public void Configure(EntityTypeBuilder<JahezImportFile> b)
    {
        b.ConfigureHistory("JahezImportFile", "jahez");
        Values(b);
        b.HasOne<JahezImportBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.FileName).HasMaxLength(260);
        b.Property(x => x.ContentHash).HasMaxLength(64);
        b.HasIndex(x => new { x.BatchId, x.ContentHash }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<JahezImportRow> b)
    {
        b.ConfigureHistory("JahezImportRow", "jahez");
        Values(b);
        b.HasOne<JahezImportFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.DriverId).HasMaxLength(150);
        b.HasIndex(x => new { x.FileId, x.RowNumber }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<JahezTransaction> b)
    {
        b.ConfigureHistory("JahezTransaction", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasOne<JahezImportBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezImportRow>().WithMany().HasForeignKey(x => x.ImportRowId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ImportRowId).IsUnique();
        b.HasIndex(x => new { x.HandoverId, x.OccurredAtUtc });
    }

    public void Configure(EntityTypeBuilder<JahezDailyDispatch> b)
    {
        b.ConfigureHistory("JahezDailyDispatch", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasOne<JahezImportBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezImportRow>().WithMany().HasForeignKey(x => x.ImportRowId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImportRowId, x.HandoverId }).IsUnique();
        b.HasIndex(x => new { x.HandoverId, x.Date });
        b.ToTable(t => t.HasCheckConstraint("CK_JahezDispatch_Count", "[Count] >= 0"));
    }

    public void Configure(EntityTypeBuilder<JahezRiderSettlement> b)
    {
        b.ConfigureHistory("JahezRiderSettlement", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasIndex(x => new { x.HandoverId, x.RecordedAtUtc });
        b.ToTable(t => t.HasCheckConstraint("CK_JahezSettlement_Payments", "[FeePayment] >= 0 AND [DebtPayment] >= 0 AND [CommissionPayment] >= 0"));
    }

    public void Configure(EntityTypeBuilder<JahezLedgerEntry> b)
    {
        b.ConfigureHistory("JahezLedgerEntry", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasOne<JahezLedgerEntry>().WithMany().HasForeignKey(x => x.ReversesEntryId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ReversesEntryId).IsUnique().HasFilter("[ReversesEntryId] IS NOT NULL");
        b.HasIndex(x => new { x.HandoverId, x.Bucket, x.OccurredAtUtc });
        b.HasIndex(x => new { x.SourceId, x.Bucket, x.Kind }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<JahezCashboxEntry> b)
    {
        b.ConfigureAuditable("JahezCashboxEntry", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasOne<JahezRiderSettlement>().WithMany().HasForeignKey(x => x.SettlementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezCashboxHandover>().WithMany().HasForeignKey(x => x.CashboxHandoverId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SettlementId, x.Section }).IsUnique();
        b.HasIndex(x => new { x.CashboxHandoverId, x.ReceivedAtUtc });
        b.ToTable(t => t.HasCheckConstraint("CK_JahezCashbox_Positive", "[Amount] > 0"));
    }

    public void Configure(EntityTypeBuilder<JahezCashboxHandover> b)
    {
        b.ConfigureAuditable("JahezCashboxHandover", "jahez");
        Values(b);
        b.HasIndex(x => new { x.Status, x.BusinessDate });
        b.ToTable(t => t.HasCheckConstraint("CK_JahezCashboxHandover_Amounts", "[FeeAmount] >= 0 AND [SettlementAmount] >= 0"));
    }

    public void Configure(EntityTypeBuilder<JahezReminderState> b)
    {
        b.ConfigureAuditable("JahezReminderState", "jahez");
        Values(b);
        Handover(b, x => x.HandoverId);
        b.HasIndex(x => new { x.HandoverId, x.AnchorAtUtc }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<JahezCommandReceipt> b)
    {
        b.ConfigureHistory("JahezCommandReceipt", "jahez");
        Values(b);
        b.Property(x => x.CommandKey).HasMaxLength(150);
        b.Property(x => x.Operation).HasMaxLength(100);
        b.Property(x => x.PayloadHash).HasMaxLength(64);
        b.HasIndex(x => new { x.ActorUserId, x.Operation, x.CommandKey }).IsUnique();
    }

    private static void Handover<T>(EntityTypeBuilder<T> b, Expression<Func<T, object?>> foreignKey) where T : Entity =>
        b.HasOne<JahezAccountHandover>().WithMany().HasForeignKey(foreignKey).OnDelete(DeleteBehavior.Restrict);

    private static void Values<T>(EntityTypeBuilder<T> b) where T : Entity
    {
        foreach (var property in b.Metadata.GetProperties().ToArray())
        {
            if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                b.Property(property.Name).HasPrecision(19, 6);
            if (property.ClrType == typeof(string) && property.Name.EndsWith("Reason", StringComparison.Ordinal))
                b.Property(property.Name).HasMaxLength(2000);
        }
    }
}
