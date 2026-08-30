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
    public void InitialPermitUsesRiyadhDateAndOneYearMinusOneDay()
    {
        var timestamp = new DateTimeOffset(2026, 8, 26, 22, 30, 0, TimeSpan.Zero);

        var start = FleetBusinessRules.RiyadhDate(timestamp);
        var end = FleetBusinessRules.PermitEnd(start);
        Assert.Equal(new DateOnly(2026, 8, 27), start);
        Assert.Equal(new DateOnly(2027, 8, 26), end);
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
