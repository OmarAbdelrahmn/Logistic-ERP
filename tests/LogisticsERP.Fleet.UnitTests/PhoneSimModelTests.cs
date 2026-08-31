using LogisticsERP.Domain.Entities.Telecom;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PhoneSimModelTests
{
    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PhoneSimModelTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    [Fact]
    public void SimIdentifiersAreUniqueForNonDeletedInventory()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(PhoneSimCard))!;

        var phoneIndex = Assert.Single(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(PhoneSimCard.NormalizedPhoneNumber)]));
        var iccidIndex = Assert.Single(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(PhoneSimCard.NormalizedIccid)]));

        Assert.True(phoneIndex.IsUnique);
        Assert.Equal("[IsDeleted] = 0", phoneIndex.GetFilter());
        Assert.True(iccidIndex.IsUnique);
        Assert.Equal("[NormalizedIccid] IS NOT NULL AND [IsDeleted] = 0", iccidIndex.GetFilter());
        Assert.Equal(typeof(Employee), Assert.Single(entity.GetForeignKeys()).PrincipalEntityType.ClrType);

        var rowVersion = entity.FindProperty(nameof(PhoneSimCard.RowVersion))!;
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void OnlyOneOpenRiderAssignmentIsAllowedPerSim()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RiderPhoneSimAssignment))!;
        var activeIndex = Assert.Single(entity.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(RiderPhoneSimAssignment.PhoneSimCardId)]));

        Assert.Equal("[EffectiveTo] IS NULL", activeIndex.GetFilter());
        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_RiderPhoneSimAssignments_EffectiveRange");
        Assert.Contains(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(PhoneSimCard));
        Assert.Contains(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(RiderProfile));
        Assert.DoesNotContain(entity.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(RiderPhoneSimAssignment.RiderProfileId)]));
    }

    [Fact]
    public void ResponsibilityChangesAreAppendOnlyHistoryWithEmployeeReferences()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(PhoneSimResponsibilityChange))!;

        Assert.Equal(2, entity.GetForeignKeys().Count(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Employee)));
        Assert.Contains(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(PhoneSimCard));
        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_PhoneSimResponsibilityChanges_ChangedResponsibleEmployee");
    }
}
