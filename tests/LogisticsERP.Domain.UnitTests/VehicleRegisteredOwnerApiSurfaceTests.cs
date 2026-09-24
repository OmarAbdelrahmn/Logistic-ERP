using System.Text.Json;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehicleRegisteredOwnerApiSurfaceTests
{
    [Fact]
    public void PurchaseSupplierCanBeOmittedOrExplicitlyClearedInVehicleEditRequests()
    {
        var upsertWithoutSupplier = JsonSerializer.Deserialize<VehicleUpsertRequest>("{}", JsonSerializerOptions.Web)!;
        var upsertWithNullSupplier = JsonSerializer.Deserialize<VehicleUpsertRequest>("{\"purchasedFromSupplierId\":null}", JsonSerializerOptions.Web)!;
        var correctionWithoutSupplier = JsonSerializer.Deserialize<VehicleIdentityCorrectionRequest>("{}", JsonSerializerOptions.Web)!;
        var correctionWithNullSupplier = JsonSerializer.Deserialize<VehicleIdentityCorrectionRequest>("{\"purchasedFromSupplierId\":null}", JsonSerializerOptions.Web)!;

        Assert.False(upsertWithoutSupplier.IsPurchasedFromSupplierIdSpecified);
        Assert.True(upsertWithNullSupplier.IsPurchasedFromSupplierIdSpecified);
        Assert.False(correctionWithoutSupplier.IsPurchasedFromSupplierIdSpecified);
        Assert.True(correctionWithNullSupplier.IsPurchasedFromSupplierIdSpecified);
    }

    [Fact]
    public void VehicleContractsExposeAnOptionalRegisteredOwnerSupplier()
    {
        Assert.Equal(typeof(Guid?), typeof(VehicleUpsertRequest)
            .GetProperty(nameof(VehicleUpsertRequest.RegisteredOwnerSupplierId))!.PropertyType);
        Assert.Equal(typeof(Guid?), typeof(VehicleDetailResponse)
            .GetProperty(nameof(VehicleDetailResponse.RegisteredOwnerSupplierId))!.PropertyType);
        Assert.Equal(typeof(string), Nullable.GetUnderlyingType(typeof(VehicleDetailResponse)
            .GetProperty(nameof(VehicleDetailResponse.RegisteredOwnerSupplier))!.PropertyType)
            ?? typeof(VehicleDetailResponse).GetProperty(nameof(VehicleDetailResponse.RegisteredOwnerSupplier))!.PropertyType);
        Assert.Equal(typeof(string), Nullable.GetUnderlyingType(typeof(VehicleDetailResponse)
            .GetProperty(nameof(VehicleDetailResponse.RegisteredOwnerType))!.PropertyType)
            ?? typeof(VehicleDetailResponse).GetProperty(nameof(VehicleDetailResponse.RegisteredOwnerType))!.PropertyType);
        Assert.Equal(typeof(Guid?), typeof(Vehicle)
            .GetProperty(nameof(Vehicle.RegisteredOwnerSponsorId))!.PropertyType);
    }
}
