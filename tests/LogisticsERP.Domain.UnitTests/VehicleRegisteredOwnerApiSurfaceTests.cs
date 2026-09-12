using LogisticsERP.Application.Features.Fleet;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehicleRegisteredOwnerApiSurfaceTests
{
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
    }
}
