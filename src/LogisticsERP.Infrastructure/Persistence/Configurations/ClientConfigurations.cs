using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class ClientContractConfiguration : IEntityTypeConfiguration<ClientContract>
{
    public void Configure(EntityTypeBuilder<ClientContract> builder)
    {
        builder.ConfigureOperational("ClientContracts");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.DisplayNameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DisplayNameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ExternalBusinessAccountId).HasMaxLength(150);
        builder.Property(entity => entity.StatusReason).HasMaxLength(500);
        builder.Property(entity => entity.ContactName).HasMaxLength(200);
        builder.Property(entity => entity.ContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.ContactEmail).HasMaxLength(320);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<ClientPlatform>().WithMany().HasForeignKey(entity => entity.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClientPlatformId });
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => new { entity.ClientPlatformId, entity.Status });
        builder.HasIndex(entity => new { entity.ClientPlatformId, entity.ExternalBusinessAccountId })
            .IsUnique()
            .HasFilter("[ExternalBusinessAccountId] IS NOT NULL");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ClientContracts_DateRange",
            "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]"));
    }
}

internal sealed class PlatformRiderAccountConfiguration : IEntityTypeConfiguration<PlatformRiderAccount>
{
    public void Configure(EntityTypeBuilder<PlatformRiderAccount> builder)
    {
        builder.ConfigureOperational("PlatformRiderAccounts");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ExternalAccountId).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.UserName).HasMaxLength(150);
        builder.Property(entity => entity.StatusReason).HasMaxLength(500);
        builder.Property(entity => entity.OwnershipNotes).HasMaxLength(4000);
        builder.Property(entity => entity.OperationalNotes).HasMaxLength(4000);
        builder.HasOne<ClientPlatform>().WithMany().HasForeignKey(entity => entity.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.RegisteredEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperatingCity>().WithMany().HasForeignKey(entity => entity.OperatingCityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => new { entity.ClientPlatformId, entity.ExternalAccountId }).IsUnique();
        builder.HasIndex(entity => new { entity.RegisteredEmployeeId, entity.ClientPlatformId })
            .IsUnique()
            .HasFilter("[RegisteredEmployeeId] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(entity => new { entity.ClientPlatformId, entity.Status });
        builder.HasIndex(entity => entity.OperatingCityId);
        builder.ToTable(table => table.HasCheckConstraint("CK_PlatformRiderAccounts_DateRange", "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]"));
    }
}

internal sealed class PlatformAccountCredentialVersionConfiguration : IEntityTypeConfiguration<PlatformAccountCredentialVersion>
{
    public void Configure(EntityTypeBuilder<PlatformAccountCredentialVersion> builder)
    {
        builder.ConfigureHistory("PlatformAccountCredentialVersions");
        builder.Property(entity => entity.Ciphertext).IsRequired();
        builder.Property(entity => entity.Nonce).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.AuthenticationTag).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.RotationReason).HasMaxLength(1000).IsRequired();
        builder.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(entity => entity.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformAccountCredentialVersion>().WithMany().HasForeignKey(entity => entity.SupersededVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.PlatformRiderAccountId, entity.KeyVersion }).IsUnique();
        builder.HasIndex(entity => new { entity.PlatformRiderAccountId, entity.RotatedAtUtc });
    }
}

internal sealed class RiderClientAssignmentConfiguration : IEntityTypeConfiguration<RiderClientAssignment>
{
    public void Configure(EntityTypeBuilder<RiderClientAssignment> builder)
    {
        builder.ConfigureOperational("RiderClientAssignments");
        builder.Property(entity => entity.StartReason).HasMaxLength(1000);
        builder.Property(entity => entity.EndReason).HasMaxLength(1000);
        builder.Property(entity => entity.OperationalAgreementReference).HasMaxLength(200);
        builder.Property(entity => entity.OperationalAgreementNotes).HasMaxLength(4000);
        builder.Property(entity => entity.BackdatedReason).HasMaxLength(1000);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(entity => entity.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClientContract>().WithMany().HasForeignKey(entity => entity.ClientContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(entity => entity.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.RiderProfileId, entity.EffectiveFrom });
        builder.HasIndex(entity => new { entity.ClientContractId, entity.Status });
        builder.HasIndex(entity => entity.RiderProfileId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL AND [IsDeleted] = 0");
        builder.HasIndex(entity => entity.PlatformRiderAccountId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL AND [IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_RiderClientAssignments_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
            table.HasCheckConstraint("CK_RiderClientAssignments_BackdatedReason", "[WasBackdated] = 0 OR [BackdatedReason] IS NOT NULL");
        });
    }
}

internal sealed class PlatformAccountRegistrationConfiguration : IEntityTypeConfiguration<PlatformAccountRegistration>
{
    public void Configure(EntityTypeBuilder<PlatformAccountRegistration> builder)
    {
        builder.ConfigureOperational("PlatformAccountRegistrations");
        builder.Property(entity => entity.StatusReason).HasMaxLength(1000);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.RegisteredEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany()
            .HasForeignKey(entity => new { entity.RiderProfileId, entity.RegisteredEmployeeId })
            .HasPrincipalKey(entity => new { entity.Id, entity.EmployeeId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClientPlatform>().WithMany().HasForeignKey(entity => entity.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClientContract>().WithMany()
            .HasForeignKey(entity => new { entity.ClientContractId, entity.ClientPlatformId })
            .HasPrincipalKey(entity => new { entity.Id, entity.ClientPlatformId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sponsor>().WithMany().HasForeignKey(entity => entity.SponsorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperatingCity>().WithMany().HasForeignKey(entity => entity.OperatingCityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(entity => entity.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.RegisteredEmployeeId, entity.ClientPlatformId, entity.Status });
        builder.HasIndex(entity => new { entity.OperatingCityId, entity.SponsorId, entity.RegistrationType, entity.Status });
        builder.HasIndex(entity => entity.PlatformRiderAccountId)
            .IsUnique()
            .HasFilter("[PlatformRiderAccountId] IS NOT NULL AND [IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_PlatformAccountRegistrations_Registration",
                "([RegistrationType] = 1 AND [SponsorId] IS NOT NULL) OR ([RegistrationType] = 2 AND [SponsorId] IS NULL)");
            table.HasCheckConstraint(
                "CK_PlatformAccountRegistrations_ActivationRange",
                "[ActivatedAtUtc] IS NULL OR [RequestedAtUtc] IS NULL OR [ActivatedAtUtc] >= [RequestedAtUtc]");
        });
    }
}

internal sealed class RiderAssignmentEventConfiguration : IEntityTypeConfiguration<RiderAssignmentEvent>
{
    public void Configure(EntityTypeBuilder<RiderAssignmentEvent> builder)
    {
        builder.ConfigureHistory("RiderAssignmentEvents");
        builder.Property(entity => entity.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.ChangeSnapshotJson).HasColumnType("nvarchar(max)");
        builder.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(entity => entity.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.RiderClientAssignmentId, entity.OccurredAtUtc });
    }
}
