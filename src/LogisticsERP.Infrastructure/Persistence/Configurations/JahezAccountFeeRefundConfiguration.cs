using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class JahezAccountFeeRefundConfiguration : IEntityTypeConfiguration<JahezAccountFeeRefund>
{
    public void Configure(EntityTypeBuilder<JahezAccountFeeRefund> b)
    {
        b.ConfigureHistory("AccountFeeRefunds", "jahez_v2");
        b.Property(x => x.GrossFeeRefund).HasPrecision(18, 2);
        b.Property(x => x.CancelledUnpaidFee).HasPrecision(18, 2);
        b.Property(x => x.CalculatedCommission).HasPrecision(18, 2);
        b.Property(x => x.DeductedCommission).HasPrecision(18, 2);
        b.Property(x => x.RefundAmount).HasPrecision(18, 2);
        b.Property(x => x.Reference).HasMaxLength(500);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<JahezAccountSession>().WithMany().HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezAccountFee>().WithMany().HasForeignKey(x => x.AccountFeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezMoneyMovement>().WithMany().HasForeignKey(x => x.MoneyMovementId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.HandoverId).IsUnique();
        b.HasIndex(x => x.AccountFeeId).IsUnique();
        b.HasIndex(x => x.MoneyMovementId).IsUnique();
        b.HasIndex(x => x.OccurredAtUtc);
        b.ToTable(t => t.HasCheckConstraint("CK_JahezFeeRefund_Amounts",
            "[GrossFeeRefund] > 0 AND [CancelledUnpaidFee] >= 0 AND [CalculatedCommission] >= 0 AND [DeductedCommission] >= 0 "
            + "AND [RefundAmount] > 0 AND [RefundAmount] = [GrossFeeRefund] - [DeductedCommission] "
            + "AND (([WaiveCommission] = 1 AND [DeductedCommission] = 0) OR ([WaiveCommission] = 0 AND [DeductedCommission] = [CalculatedCommission]))"));
    }
}
