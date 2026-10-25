using System.Text.Json;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Fact]
    public async Task JahezAccountsReturnTheLatestActiveLinkedVehicleTypeInListAndDetails()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var linked = await f.Account("01-vehicle-linked");
        var empty = await f.Account("02-without-vehicle");
        Vehicle AddVehicle(string code, VehicleType type, bool deleted = false)
        {
            var vehicle = new Vehicle { AssetNumber = code, NormalizedAssetNumber = code, VehicleType = type, IsDeleted = deleted };
            f.Db.Add(vehicle); return vehicle;
        }
        void Link(Vehicle vehicle, DateTimeOffset approvedAt, bool ended = false, bool deleted = false)
        {
            f.Db.Add(new VehiclePlatformAccountAssignment { VehicleId = vehicle.Id, PlatformRiderAccountId = linked.Id,
                AssignedAtUtc = At(1), ApprovedAtUtc = approvedAt,
                Status = ended ? VehiclePlatformAccountAssignmentStatus.Ended : VehiclePlatformAccountAssignmentStatus.Active,
                EndedAtUtc = ended ? At(6) : null, IsDeleted = deleted });
        }
        Link(AddVehicle("CAR", VehicleType.Car), At(2));
        Link(AddVehicle("VAN", VehicleType.Van), At(3));
        Link(AddVehicle("ENDED", VehicleType.Truck), At(4), ended: true);
        Link(AddVehicle("DELETED-LINK", VehicleType.Other), At(5), deleted: true);
        Link(AddVehicle("DELETED-VEHICLE", VehicleType.Motorcycle, deleted: true), At(6));
        // An account with only ended links has no current vehicle type.
        var oldVehicle = AddVehicle("OLD", VehicleType.Car);
        f.Db.Add(new VehiclePlatformAccountAssignment { VehicleId = oldVehicle.Id, PlatformRiderAccountId = empty.Id,
            AssignedAtUtc = At(1), ApprovedAtUtc = At(1), Status = VehiclePlatformAccountAssignmentStatus.Ended, EndedAtUtc = At(2) });
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();

        var accounts = Success(await f.Service.AccountsV2Async(1, 50, ct));
        Assert.Equal(2, accounts.TotalCount); Assert.Equal(2, accounts.Items.Count);
        var row = accounts.Items.Single(x => x.AccountId == linked.Id);
        Assert.Equal("Van", row.VehicleType);
        Assert.False(row.IsAssigned); Assert.Null(row.ActualRider);
        Assert.Null(accounts.Items.Single(x => x.AccountId == empty.Id).VehicleType);
        Assert.Equal("Van", Success(await f.Service.AccountDetailsV2Async(linked.Id, 1, 50, ct)).Account.VehicleType);
        Assert.Null(Success(await f.Service.AccountDetailsV2Async(empty.Id, 1, 50, ct)).Account.VehicleType);
        var firstPage = Success(await f.Service.AccountsV2Async(1, 1, ct));
        Assert.Single(firstPage.Items); Assert.Equal(2, firstPage.TotalCount); Assert.Equal("Van", firstPage.Items[0].VehicleType);
        using var json = JsonSerializer.SerializeToDocument(row, JsonSerializerOptions.Web);
        Assert.Equal("Van", json.RootElement.GetProperty("vehicleType").GetString());
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }
}
