using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class JahezCashboxHandoverConfigurations : IEntityTypeConfiguration<JahezCashboxHandover>,
    IEntityTypeConfiguration<JahezCashboxHandoverMovement>
{
    public void Configure(EntityTypeBuilder<JahezCashboxHandover> b)
    {
        b.ConfigureAuditable("CashboxHandovers", "jahez_v2");
        b.Property(x => x.CashboxAmount).HasPrecision(18, 2);
        b.Property(x => x.PetroAppAmount).HasPrecision(18, 2);
        b.Property(x => x.BankAmount).HasPrecision(18, 2);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.Property(x => x.ConfirmationReason).HasMaxLength(2000);
        b.Property(x => x.CancellationReason).HasMaxLength(2000);
        // The company-wide journal has one pending reconciliation at a time.
        b.HasIndex(x => x.Status).IsUnique().HasFilter("[Status] = 1");
        b.HasIndex(x => new { x.BusinessDate, x.Id });
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_JahezCashboxHandover_Count", "[MovementCount] > 0");
            t.HasCheckConstraint("CK_JahezCashboxHandover_State",
                "[IsDeleted] = 0 AND (([Status] = 1 AND [AccountantUserId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [CancelledAtUtc] IS NULL) "
                + "OR ([Status] = 2 AND [AccountantUserId] IS NOT NULL AND [AccountantUserId] <> [RequestedByUserId] AND [ConfirmedAtUtc] IS NOT NULL AND [CancelledAtUtc] IS NULL) "
                + "OR ([Status] = 3 AND [AccountantUserId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [CancelledAtUtc] IS NOT NULL))");
        });
    }

    public void Configure(EntityTypeBuilder<JahezCashboxHandoverMovement> b)
    {
        b.ConfigureHistory("CashboxHandoverMovements", "jahez_v2");
        b.HasOne<JahezCashboxHandover>().WithMany().HasForeignKey(x => x.CashboxHandoverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezMoneyMovement>().WithMany().HasForeignKey(x => x.MoneyMovementId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CashboxHandoverId, x.MoneyMovementId }).IsUnique();
        b.HasIndex(x => x.MoneyMovementId);
    }
}
