using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Infrastructure.Fleet;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FleetBusinessRulesTests
{
    [Fact]
    public void CarAllowsTwoPlatformAccountsPerPlatformAndCity()
    {
        Assert.Equal(2, VehiclePlatformAccountAssignmentPolicy.GetMaximumAccounts(VehicleType.Car));
        Assert.False(VehiclePlatformAccountAssignmentPolicy.IsCapacityExceeded(VehicleType.Car, 2));
        Assert.True(VehiclePlatformAccountAssignmentPolicy.IsCapacityExceeded(VehicleType.Car, 3));
    }

    [Fact]
    public void MotorcycleAllowsThreePlatformAccountsPerPlatformAndCity()
    {
        Assert.Equal(3, VehiclePlatformAccountAssignmentPolicy.GetMaximumAccounts(VehicleType.Motorcycle));
        Assert.False(VehiclePlatformAccountAssignmentPolicy.IsCapacityExceeded(VehicleType.Motorcycle, 3));
        Assert.True(VehiclePlatformAccountAssignmentPolicy.IsCapacityExceeded(VehicleType.Motorcycle, 4));
    }

    [Theory]
    [InlineData(VehicleType.Van)]
    [InlineData(VehicleType.Truck)]
    [InlineData(VehicleType.Other)]
    public void UnspecifiedVehicleTypesHaveNoConfiguredCapacity(VehicleType vehicleType) =>
        Assert.Null(VehiclePlatformAccountAssignmentPolicy.GetMaximumAccounts(vehicleType));

    [Fact]
    public void SponsorCompatibilityAllowsOwnerAndContractualKeetaLessee()
    {
        var ownerSponsorId = Guid.CreateVersion7();
        var lesseeSponsorId = Guid.CreateVersion7();

        Assert.True(VehiclePlatformAccountAssignmentPolicy.IsSponsorCompatible(
            ownerSponsorId,
            ownerSponsorId,
            hasApplicableLeaseAgreement: false));
        Assert.True(VehiclePlatformAccountAssignmentPolicy.IsSponsorCompatible(
            ownerSponsorId,
            lesseeSponsorId,
            hasApplicableLeaseAgreement: true));
        Assert.False(VehiclePlatformAccountAssignmentPolicy.IsSponsorCompatible(
            ownerSponsorId,
            lesseeSponsorId,
            hasApplicableLeaseAgreement: false));
        Assert.False(VehiclePlatformAccountAssignmentPolicy.IsSponsorCompatible(
            null,
            lesseeSponsorId,
            hasApplicableLeaseAgreement: true));
    }

    [Fact]
    public void VehicleComplianceContractExposesPermitEndDate()
    {
        Assert.NotNull(typeof(VehicleSummaryResponse).GetProperty(nameof(VehicleSummaryResponse.PermitEndDate)));
    }

    [Fact]
    public void VehicleSummaryExposesActualRiderDetails()
    {
        Assert.NotNull(typeof(VehicleSummaryResponse).GetProperty(nameof(VehicleSummaryResponse.IsRealRider)));
        Assert.NotNull(typeof(VehicleSummaryResponse).GetProperty(nameof(VehicleSummaryResponse.RealRider)));
    }

    [Fact]
    public void VehicleAssetNumberDoesNotContainTheDate()
    {
        var number = FleetServiceSupport.NewVehicleAssetNumber(Guid.Parse("01a04223-0000-7000-8000-000000000000"));

        Assert.Equal("VEH-01A04223", number);
    }

    [Fact]
    public void VehicleIssueNumberDoesNotContainTheDate()
    {
        var number = FleetServiceSupport.NewIssueNumber(Guid.Parse("01a04223-0000-7000-8000-1234567890ab"));

        Assert.Equal("ISS-80001234567890AB", number);
        Assert.DoesNotContain("20260903", number);
    }

    [Fact]
    public void InitialPermitUsesRiyadhDateAndOneYearMinusOneDay()
    {
        var timestamp = new DateTimeOffset(2026, 8, 26, 22, 30, 0, TimeSpan.Zero);

        var start = FleetBusinessRules.RiyadhDate(timestamp);
        var end = FleetBusinessRules.PermitEnd(start);
        Assert.Equal(new DateOnly(2026, 8, 27), start);
        Assert.Equal(new DateOnly(2027, 8, 26), end);
    }

    [Fact]
    public void GoodReturnRejectsAConditionReportAndEvidence()
    {
        Assert.False(FleetBusinessRules.RequiresReturnConditionReport(VehicleCondition.Good));
        Assert.True(FleetBusinessRules.IsValidReturnConditionReport(VehicleCondition.Good, false, null, null, null, null, 0));
        Assert.False(FleetBusinessRules.IsValidReturnConditionReport(VehicleCondition.Good, true, VehicleIssueCategory.Damage, VehicleIssueSeverity.Low, "Scratch", 100m, 1));
    }

    [Theory]
    [InlineData(VehicleCondition.Unknown)]
    [InlineData(VehicleCondition.Fair)]
    [InlineData(VehicleCondition.Damaged)]
    [InlineData(VehicleCondition.Unsafe)]
    public void NonGoodReturnRequiresCompleteReportAndOneOrTwoEvidenceFiles(VehicleCondition condition)
    {
        Assert.True(FleetBusinessRules.RequiresReturnConditionReport(condition));
        Assert.True(FleetBusinessRules.IsValidReturnConditionReport(condition, true, VehicleIssueCategory.Damage, VehicleIssueSeverity.High, "Visible damage", 350.50m, 1));
        Assert.True(FleetBusinessRules.IsValidReturnConditionReport(condition, true, VehicleIssueCategory.Damage, VehicleIssueSeverity.High, "Visible damage", 350.50m, 2));
        Assert.False(FleetBusinessRules.IsValidReturnConditionReport(condition, false, null, null, null, null, 1));
        Assert.False(FleetBusinessRules.IsValidReturnConditionReport(condition, true, null, VehicleIssueSeverity.High, "Visible damage", 350.50m, 1));
        Assert.False(FleetBusinessRules.IsValidReturnConditionReport(condition, true, VehicleIssueCategory.Damage, null, "Visible damage", 350.50m, 1));
        Assert.False(FleetBusinessRules.IsValidReturnConditionReport(condition, true, VehicleIssueCategory.Damage, VehicleIssueSeverity.High, "Visible damage", 350.50m, 0));
        Assert.False(FleetBusinessRules.IsValidReturnConditionReport(condition, true, VehicleIssueCategory.Damage, VehicleIssueSeverity.High, "Visible damage", 350.50m, 3));
        Assert.False(FleetBusinessRules.IsValidReturnConditionReport(condition, true, VehicleIssueCategory.Damage, VehicleIssueSeverity.High, "Visible damage", -1m, 1));
    }

    [Fact]
    public void OwnedVehicleNeedsSupplierButLeasedVehicleDoesNot()
    {
        var vehicle = CompleteVehicle();
        vehicle.OwnershipType = VehicleOwnershipType.Owned;
        vehicle.PurchasedFromSupplierId = null;

        Assert.False(FleetBusinessRules.IsCoreIdentityReady(vehicle));

        vehicle.PurchasedFromSupplierId = Guid.CreateVersion7();
        Assert.True(FleetBusinessRules.IsCoreIdentityReady(vehicle));

        vehicle.OwnershipType = VehicleOwnershipType.Leased;
        vehicle.PurchasedFromSupplierId = null;
        Assert.True(FleetBusinessRules.IsCoreIdentityReady(vehicle));
    }

    [Fact]
    public void PublicTransportRequiresOperationCardButFilesRemainWarnings()
    {
        var (photos, documents) = FleetBusinessRules.MissingFiles(
            VehicleRegistrationType.PublicTransport,
            [VehicleFileKind.Istimara, VehicleFileKind.FrontImage]);

        Assert.Equal([VehicleFileKind.RearImage, VehicleFileKind.LeftImage, VehicleFileKind.RightImage], photos);
        Assert.Equal([VehicleFileKind.OperationCard], documents);
        Assert.True(FleetBusinessRules.IsCoreIdentityReady(CompleteVehicle()));
    }

    [Fact]
    public void PrivateTransportDoesNotRequireOperationCard()
    {
        var (_, documents) = FleetBusinessRules.MissingFiles(
            VehicleRegistrationType.PrivateTransport,
            [VehicleFileKind.Istimara]);

        Assert.Empty(documents);
    }

    private static Vehicle CompleteVehicle() => new()
    {
        SerialNumber = "SER-1",
        NormalizedSerialNumber = "SER1",
        ChassisNumber = "CHS-1",
        NormalizedChassisNumber = "CHS1",
        PlateNumberAr = "أ ب ج 1234",
        PlateNumberEn = "ABC 1234",
        SponsorId = Guid.CreateVersion7(),
        OperatingCityId = Guid.CreateVersion7(),
        RegistrationType = VehicleRegistrationType.PrivateTransport,
        OwnershipType = VehicleOwnershipType.Leased
    };
}
