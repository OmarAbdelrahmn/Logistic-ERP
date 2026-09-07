using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Fleet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class VehicleAccidentCaseConfiguration : IEntityTypeConfiguration<VehicleAccidentCase>
{
    public void Configure(EntityTypeBuilder<VehicleAccidentCase> builder)
    {
        builder.ConfigureOperational("VehicleAccidentCases");
        builder.HasOne<VehicleAccident>().WithOne().HasForeignKey<VehicleAccidentCase>(x => x.VehicleAccidentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleSupplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.OtherPartiesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.RiderFaultPercentage).HasPrecision(5, 2);
        foreach (var property in typeof(VehicleAccidentCase).GetProperties().Where(x => x.PropertyType == typeof(decimal?) && x.Name != nameof(VehicleAccidentCase.RiderFaultPercentage)))
            builder.Property(property.Name).HasPrecision(18, 2);
        foreach (var property in typeof(VehicleAccidentCase).GetProperties().Where(x => x.Name.EndsWith("AttachmentId", StringComparison.Ordinal)))
            builder.HasOne<VehicleAccidentAttachment>().WithMany().HasForeignKey(property.Name).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocumentVersion>().WithMany().HasForeignKey(x => x.IqamaVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocumentVersion>().WithMany().HasForeignKey(x => x.LicenseVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleAttachmentVersion>().WithMany().HasForeignKey(x => x.RegistrationVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.ClaimNumber).HasMaxLength(150);
        builder.Property(x => x.RefundReference).HasMaxLength(150);
        builder.Property(x => x.DamageAssessment).HasMaxLength(4000);
        builder.Property(x => x.InsuranceRejectionReason).HasMaxLength(1000);
        builder.Property(x => x.RepairLocation).HasMaxLength(1000);
        builder.Property(x => x.RepairContact).HasMaxLength(300);
        builder.Property(x => x.ReinspectionLocation).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Stage, x.InsuranceDueAtUtc });
        builder.HasIndex(x => new { x.Stage, x.SupplierTransferDueAtUtc });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_AccidentCase_Fault", "[RiderFaultPercentage] IS NULL OR [RiderFaultPercentage] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_AccidentCase_Parties", "ISJSON([OtherPartiesJson]) = 1");
            t.HasCheckConstraint("CK_AccidentCase_OpeningFee", "[OpeningFeeAmount] IS NULL OR [OpeningFeeAmount] = 2500");
        });
    }
}

internal sealed class VehicleAccidentInstallmentConfiguration : IEntityTypeConfiguration<VehicleAccidentInstallment>
{
    public void Configure(EntityTypeBuilder<VehicleAccidentInstallment> builder)
    {
        builder.ConfigureOperational("VehicleAccidentInstallments");
        builder.HasOne<VehicleAccident>().WithMany().HasForeignKey(x => x.VehicleAccidentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleAccidentAttachment>().WithMany().HasForeignKey(x => x.ReceiptAttachmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.RefundEligibleAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => new { x.VehicleAccidentId, x.ReceiptAttachmentId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_AccidentInstallment_Dates", "[PeriodTo] >= [PeriodFrom]");
            t.HasCheckConstraint("CK_AccidentInstallment_Amount", "[Amount] > 0 AND [RefundEligibleAmount] > 0 AND [RefundEligibleAmount] <= [Amount]");
        });
    }
}
