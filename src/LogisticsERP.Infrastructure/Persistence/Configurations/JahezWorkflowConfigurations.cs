using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class JahezWorkflowConfigurations : IEntityTypeConfiguration<JahezOperation>,
    IEntityTypeConfiguration<JahezMoneyMovement>, IEntityTypeConfiguration<JahezTransferredDebt>
{
    public void Configure(EntityTypeBuilder<JahezOperation> b)
    {
        b.ConfigureAuditable("Operations", "jahez_v2");
        b.Property(x => x.Net).HasPrecision(18, 2);
        b.Property(x => x.Commission).HasPrecision(18, 2);
        b.Property(x => x.CalculatedCommission).HasPrecision(18, 2);
        b.Property(x => x.Remaining).HasPrecision(18, 2);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.Property(x => x.PaymentReference).HasMaxLength(500);
        b.HasOne<JahezAccountSession>().WithMany().HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezApprovalRequest>().WithMany().HasForeignKey(x => x.ApprovedRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.HandoverId, x.CreatedAtUtc });
        // Only one unexecuted operation per handover; previews do not reserve a period.
        b.HasIndex(x => x.HandoverId).IsUnique().HasFilter("[ExecutedAtUtc] IS NULL AND [IsDeleted] = 0");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_JahezOperation_Amounts", "[Commission] >= 0 AND [CalculatedCommission] >= 0 AND [Remaining] = [Net] + [Commission]");
            t.HasCheckConstraint("CK_JahezOperation_State", "[Mode] BETWEEN 1 AND 3 AND [Status] BETWEEN 1 AND 3 AND [PaymentStatus] BETWEEN 1 AND 4 AND (([Status] = 3 AND [ExecutedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [ExecutedAtUtc] IS NULL))");
            t.HasCheckConstraint("CK_JahezOperation_CommissionStart", "[CommissionStartDate] IS NULL OR [CommissionStartDate] <= [ThroughDate]");
        });
    }
    public void Configure(EntityTypeBuilder<JahezMoneyMovement> b)
    {
        b.ConfigureHistory("MoneyMovements", "jahez_v2");
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Reference).HasMaxLength(500);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<JahezAccountSession>().WithMany().HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezOperation>().WithMany().HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezTransferredDebt>().WithMany().HasForeignKey(x => x.TransferredDebtId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OperationId).IsUnique().HasFilter("[OperationId] IS NOT NULL");
        b.HasIndex(x => x.TransferredDebtId).IsUnique().HasFilter("[TransferredDebtId] IS NOT NULL");
        b.HasIndex(x => new { x.Fund, x.OccurredAtUtc });
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_JahezMoney_Amount", "[Amount] > 0 OR ([Amount] = 0 AND [Direction] = 0 AND [Purpose] = 1 AND [OperationId] IS NOT NULL)");
            t.HasCheckConstraint("CK_JahezMoney_Routing", "([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] IN (1, 2)))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2) OR ([Direction] = 0 AND [Amount] = 0 AND [Purpose] = 1 AND [OperationId] IS NOT NULL AND [Fund] IN (1, 2, 3))");
        });
    }
    public void Configure(EntityTypeBuilder<JahezTransferredDebt> b)
    {
        b.ConfigureAuditable("TransferredDebts", "jahez_v2");
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasOne<JahezOperation>().WithMany().HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OperationId).IsUnique();
        b.HasIndex(x => new { x.RiderProfileId, x.PaidAtUtc });
        b.ToTable(t => t.HasCheckConstraint("CK_JahezTransferredDebt_Amount", "[Amount] > 0"));
    }
}
