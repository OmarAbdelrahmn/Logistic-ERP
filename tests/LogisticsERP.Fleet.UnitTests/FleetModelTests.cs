using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FleetModelTests
{
    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=FleetModelTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    [Fact]
    public void DailyDistanceLedgerIsUniquePerVehicleAndDateAndPreservesGpsDecimals()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var distance = model.FindEntityType(typeof(VehicleDailyDistance))!;
        var vehicle = model.FindEntityType(typeof(Vehicle))!;

        var dailyIndex = Assert.Single(distance.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(VehicleDailyDistance.VehicleId), nameof(VehicleDailyDistance.WorkDate)]));
        Assert.Equal("[IsDeleted] = 0", dailyIndex.GetFilter());
        Assert.Equal(18, distance.FindProperty(nameof(VehicleDailyDistance.GpsDistanceKm))!.GetPrecision());
        Assert.Equal(2, distance.FindProperty(nameof(VehicleDailyDistance.GpsDistanceKm))!.GetScale());
        Assert.Equal(18, vehicle.FindProperty(nameof(Vehicle.TrackedDistanceKm))!.GetPrecision());
        Assert.Contains(distance.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_VehicleDailyDistances_ManualOdometer");
    }

    [Fact]
    public void AssignmentIndexesGuaranteeOneActiveVehiclePerRiderAndVehicle()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(RiderVehicleAssignment))!;
        var indexes = entity.GetIndexes().ToArray();

        var riderIndex = Assert.Single(indexes, index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(RiderVehicleAssignment.RiderProfileId)]));
        var vehicleIndex = Assert.Single(indexes, index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(RiderVehicleAssignment.VehicleId)]));
        Assert.True(riderIndex.IsUnique);
        Assert.True(vehicleIndex.IsUnique);
        Assert.Equal("[EndedAtUtc] IS NULL AND [IsDeleted] = 0", riderIndex.GetFilter());
        Assert.Equal("[EndedAtUtc] IS NULL AND [IsDeleted] = 0", vehicleIndex.GetFilter());
    }

    [Fact]
    public void RealRiderIsOptionalOneToOneAssignmentDetailsWithValidatedIqama()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RealRider))!;
        var foreignKey = Assert.Single(entity.GetForeignKeys());

        Assert.True(foreignKey.IsUnique);
        Assert.Equal(typeof(RiderVehicleAssignment), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(
            [nameof(RealRider.RiderVehicleAssignmentId)],
            foreignKey.Properties.Select(property => property.Name));
        Assert.Equal(200, entity.FindProperty(nameof(RealRider.Name))!.GetMaxLength());
        Assert.Equal(10, entity.FindProperty(nameof(RealRider.IqamaNo))!.GetMaxLength());
        Assert.Contains(entity.GetCheckConstraints(), constraint => constraint.Name == "CK_RealRiders_IqamaNo");
    }

    [Fact]
    public void PlatformAccountVehicleAssignmentsAreIndependentAndNeverDatabaseRejectedForDuplicates()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(VehiclePlatformAccountAssignment))!;
        var pairIndex = Assert.Single(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [
                    nameof(VehiclePlatformAccountAssignment.VehicleId),
                    nameof(VehiclePlatformAccountAssignment.PlatformRiderAccountId)
                ]));

        Assert.False(pairIndex.IsUnique);
        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_VehiclePlatformAccountAssignments_AlwaysApproved"
            && constraint.Sql.Contains("[ApprovalStatus] = 1", StringComparison.Ordinal));
        Assert.Contains(entity.GetForeignKeys(), foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Vehicle));
        Assert.Contains(entity.GetForeignKeys(), foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(PlatformRiderAccount));
        Assert.Null(entity.FindProperty("RiderProfileId"));
        Assert.DoesNotContain(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(RiderProfile));
    }

    [Fact]
    public void PlatformAccountSwitchesAllowOnlyOnePendingRequestPerSourceAssignment()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(VehiclePlatformAccountSwitch))!;
        var pendingIndex = Assert.Single(entity.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(VehiclePlatformAccountSwitch.SourceAssignmentId)]));

        Assert.Equal("[Status] = 1 AND [IsDeleted] = 0", pendingIndex.GetFilter());
        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_VehiclePlatformAccountSwitches_Acceptance");
        Assert.Equal(
            2,
            entity.GetForeignKeys().Count(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(VehiclePlatformAccountAssignment)));
        Assert.Equal(
            2,
            entity.GetForeignKeys().Count(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(Vehicle)));
    }

    [Fact]
    public void SponsorVehicleLeaseAgreementKeepsPartiesAndVehiclesNormalized()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var agreement = model.FindEntityType(typeof(SponsorVehicleLeaseAgreement))!;
        var relation = model.FindEntityType(typeof(SponsorVehicleLeaseAgreementVehicle))!;

        Assert.Equal(
            2,
            agreement.GetForeignKeys().Count(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(Sponsor)));
        Assert.Contains(agreement.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_SponsorVehicleLeaseAgreements_DifferentSponsors");
        Assert.Contains(agreement.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_SponsorVehicleLeaseAgreements_EffectiveRange");
        var uniqueVehicle = Assert.Single(relation.GetIndexes(), index => index.IsUnique);
        Assert.Equal(
            [
                nameof(SponsorVehicleLeaseAgreementVehicle.SponsorVehicleLeaseAgreementId),
                nameof(SponsorVehicleLeaseAgreementVehicle.VehicleId)
            ],
            uniqueVehicle.Properties.Select(property => property.Name));
    }

    [Fact]
    public void StatusTimelineGuaranteesOneOpenPeriodPerVehicle()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(VehicleOperationalStatusPeriod))!;
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.IsUnique && candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(VehicleOperationalStatusPeriod.VehicleId)]));

        Assert.Equal("[EffectiveToUtc] IS NULL AND [IsDeleted] = 0", index.GetFilter());
    }

    [Fact]
    public void VehicleIdentifiersAndConcurrencyAreDatabaseProtected()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Vehicle))!;

        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedAssetNumber));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedSerialNumber));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedChassisNumber));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedPlateNumberAr));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedPlateNumberEn));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.Vin));

        var rowVersion = entity.FindProperty(nameof(Vehicle.RowVersion))!;
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void SaudiRegistrationTypesHaveStableMoiOrder()
    {
        Assert.Equal(
            [
                VehicleRegistrationType.Private,
                VehicleRegistrationType.PrivateTransport,
                VehicleRegistrationType.SmallBus,
                VehicleRegistrationType.Taxi,
                VehicleRegistrationType.PublicTransport,
                VehicleRegistrationType.PublicBus,
                VehicleRegistrationType.Motorcycle,
                VehicleRegistrationType.PublicWorks
            ],
            Enum.GetValues<VehicleRegistrationType>());
        Assert.Equal(Enumerable.Range(1, 8), Enum.GetValues<VehicleRegistrationType>().Select(value => (int)value));
    }

    [Fact]
    public void VehicleRegistrationTypeIsDatabaseConstrainedToEightValues()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Vehicle))!;

        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_Vehicles_RegistrationType"
            && constraint.Sql.Contains("BETWEEN 1 AND 8", StringComparison.Ordinal));
    }

    [Fact]
    public void FixedVehicleFileSlotsAllowOnlyOneCurrentSlotPerVehicle()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(VehicleAttachment))!;
        var index = Assert.Single(entity.GetIndexes(), candidate => candidate.IsUnique
            && candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(VehicleAttachment.VehicleId), nameof(VehicleAttachment.Kind)]));

        Assert.Equal("[Kind] <> 99 AND [IsDeleted] = 0", index.GetFilter());
    }

    [Fact]
    public void ReturnIssueStoresResponsibilityCostAndPrivateEvidence()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var issue = model.FindEntityType(typeof(VehicleIssue))!;
        var evidence = model.FindEntityType(typeof(VehicleIssueEvidence))!;

        Assert.True(issue.FindProperty(nameof(VehicleIssue.IsRiderResponsible))!.IsNullable);
        Assert.Equal(18, issue.FindProperty(nameof(VehicleIssue.EstimatedRepairCost))!.GetPrecision());
        Assert.Equal(2, issue.FindProperty(nameof(VehicleIssue.EstimatedRepairCost))!.GetScale());
        Assert.Contains(issue.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_VehicleIssues_EstimatedRepairCost");
        Assert.Contains(evidence.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(VehicleIssue));
        Assert.Equal(1000, evidence.FindProperty(nameof(VehicleIssueEvidence.StoragePath))!.GetMaxLength());
        Assert.Contains(evidence.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_VehicleIssueEvidenceFiles_Size");
    }

    [Fact]
    public void OperationCardsKeepOneCurrentRecordPerVehicle()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(VehicleOperationCard))!;
        var index = Assert.Single(entity.GetIndexes(), candidate => candidate.IsUnique
            && candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(VehicleOperationCard.VehicleId)]));

        Assert.Equal("[IsCurrent] = 1 AND [IsDeleted] = 0", index.GetFilter());
        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_VehicleOperationCards_DateRange"
            && constraint.Sql.Contains("[ExpiryDate] >= [IssueDate]", StringComparison.Ordinal));
    }

    [Fact]
    public void SupplierCommercialAndTaxNumbersUseFilteredUniqueIndexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(VehicleSupplier))!;

        foreach (var propertyName in new[] { nameof(VehicleSupplier.CommercialRegistrationNumber), nameof(VehicleSupplier.TaxNumber) })
        {
            var index = Assert.Single(entity.GetIndexes(), candidate => candidate.Properties.Select(property => property.Name).SequenceEqual([propertyName]));
            Assert.True(index.IsUnique);
            Assert.Contains("IS NOT NULL", index.GetFilter(), StringComparison.Ordinal);
            Assert.Contains("[IsDeleted] = 0", index.GetFilter(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RiderPromissoryVersionsAreLinkedToAssignmentsForAudit()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(RiderVehicleAssignmentPromissoryFile))!;
        var index = Assert.Single(entity.GetIndexes(), candidate => candidate.IsUnique);

        Assert.Equal(
            [nameof(RiderVehicleAssignmentPromissoryFile.RiderVehicleAssignmentId), nameof(RiderVehicleAssignmentPromissoryFile.RiderPromissoryFileVersionId)],
            index.Properties.Select(property => property.Name));
    }

    [Fact]
    public void FleetLocationIsRemovedFromTheModel()
    {
        using var context = CreateContext();

        Assert.DoesNotContain(context.Model.GetEntityTypes(), entity => entity.ClrType.Name == "FleetLocation");
        Assert.Null(context.Model.FindEntityType(typeof(Vehicle))!.FindProperty("CurrentLocationId"));
    }

    [Fact]
    public void LegacyTemporaryVehicleOperationIsNotPartOfTheModel()
    {
        using var context = CreateContext();

        Assert.DoesNotContain(context.Model.GetEntityTypes(), entity =>
            entity.ClrType.Name.Contains("TempVehicleOperation", StringComparison.Ordinal));
    }
}
