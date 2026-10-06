using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class HousingRoomApiSurfaceTests
{
    public static TheoryData<Type, string, Type, string, string> Endpoints => new()
    {
        { typeof(HousingController), nameof(HousingController.GetRooms), typeof(HttpGetAttribute), "{id:guid}/rooms", PermissionKeys.Operations.HousingRead },
        { typeof(HousingController), nameof(HousingController.CreateRoom), typeof(HttpPostAttribute), "{id:guid}/rooms", PermissionKeys.Operations.HousingCreate },
        { typeof(RoomsController), nameof(RoomsController.Get), typeof(HttpGetAttribute), "{id:guid}", PermissionKeys.Operations.HousingRead },
        { typeof(RoomsController), nameof(RoomsController.Update), typeof(HttpPutAttribute), "{id:guid}", PermissionKeys.Operations.HousingUpdate },
        { typeof(RoomsController), nameof(RoomsController.Archive), typeof(HttpDeleteAttribute), "{id:guid}", PermissionKeys.Operations.HousingDelete },
        { typeof(RoomsController), nameof(RoomsController.AssignEmployee), typeof(HttpPostAttribute), "{id:guid}/occupants/employees", PermissionKeys.Operations.HousingCreate },
        { typeof(RoomsController), nameof(RoomsController.AssignRider), typeof(HttpPostAttribute), "{id:guid}/occupants/riders", PermissionKeys.Operations.HousingCreate },
        { typeof(RoomsController), nameof(RoomsController.MoveOccupant), typeof(HttpPostAttribute), "occupants/{occupancyPeriodId:guid}/move", PermissionKeys.Operations.HousingUpdate },
        { typeof(RoomsController), nameof(RoomsController.RemoveOccupant), typeof(HttpPostAttribute), "occupants/{occupancyPeriodId:guid}/remove", PermissionKeys.Operations.HousingDelete },
        { typeof(HousingController), nameof(HousingController.GetFloors), typeof(HttpGetAttribute), "{id:guid}/floors", PermissionKeys.Operations.HousingRead },
        { typeof(HousingController), nameof(HousingController.CreateFloor), typeof(HttpPostAttribute), "{id:guid}/floors", PermissionKeys.Operations.HousingCreate },
        { typeof(HousingController), nameof(HousingController.UpdateFloor), typeof(HttpPutAttribute), "{id:guid}/floors/{floorId:guid}", PermissionKeys.Operations.HousingUpdate },
        { typeof(HousingController), nameof(HousingController.DeleteFloor), typeof(HttpDeleteAttribute), "floors/{floorId:guid}", PermissionKeys.Operations.HousingDelete },
        { typeof(HousingController), nameof(HousingController.CreateFloorEquipment), typeof(HttpPostAttribute), "floors/{floorId:guid}/equipment", PermissionKeys.Operations.HousingCreate },
        { typeof(RoomsController), nameof(RoomsController.CreateEquipment), typeof(HttpPostAttribute), "{id:guid}/equipment", PermissionKeys.Operations.HousingCreate },
        { typeof(RoomsController), nameof(RoomsController.CreateExternalOccupant), typeof(HttpPostAttribute), "{id:guid}/occupants/external", PermissionKeys.Operations.HousingCreate },
        { typeof(RoomsController), nameof(RoomsController.AssignByIqama), typeof(HttpPostAttribute), "{id:guid}/occupants/iqama", PermissionKeys.Operations.HousingCreate },
        { typeof(RoomsController), nameof(RoomsController.ResolvePendingOccupant), typeof(HttpPostAttribute), "occupants/pending/{pendingId:guid}/resolve", PermissionKeys.Operations.HousingUpdate },
        { typeof(RoomsController), nameof(RoomsController.DeletePendingOccupant), typeof(HttpDeleteAttribute), "occupants/pending/{pendingId:guid}", PermissionKeys.Operations.HousingDelete },
        { typeof(RoomsController), nameof(RoomsController.UpdateEquipment), typeof(HttpPutAttribute), "{id:guid}/equipment/{equipmentId:guid}", PermissionKeys.Operations.HousingUpdate },
        { typeof(HousingController), nameof(HousingController.UpdateFloorEquipment), typeof(HttpPutAttribute), "floors/{floorId:guid}/equipment/{equipmentId:guid}", PermissionKeys.Operations.HousingUpdate },
        { typeof(HousingController), nameof(HousingController.DeleteEquipment), typeof(HttpDeleteAttribute), "equipment/{equipmentId:guid}", PermissionKeys.Operations.HousingDelete },
        { typeof(RoomsController), nameof(RoomsController.UpdateExternalOccupant), typeof(HttpPutAttribute), "occupants/external/{occupantId:guid}", PermissionKeys.Operations.HousingUpdate },
        { typeof(RoomsController), nameof(RoomsController.DeleteExternalOccupant), typeof(HttpDeleteAttribute), "occupants/external/{occupantId:guid}", PermissionKeys.Operations.HousingDelete }
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointsUseExpectedVerbRouteAndPermission(
        Type controller,
        string methodName,
        Type verbType,
        string route,
        string permission)
    {
        var method = controller.GetMethod(methodName);
        Assert.NotNull(method);
        var verb = Assert.Single(method!.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(route, verb.Template);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ResidenceUsesRoomAsItsOnlyLocationRelationship()
    {
        Assert.Equal(typeof(Guid), typeof(HousingResidencePeriod).GetProperty(nameof(HousingResidencePeriod.RoomId))!.PropertyType);
        Assert.Null(typeof(HousingResidencePeriod).GetProperty("HousingId"));
        Assert.NotNull(typeof(HousingRoom).GetProperty(nameof(HousingRoom.HousingId)));
        Assert.Null(typeof(Housing).GetProperty("TotalCapacity"));
    }

    [Fact]
    public void DedicatedRiderAndEmployeeRequestsUseTheirOwnIdentifiers()
    {
        Assert.NotNull(typeof(AssignRoomEmployeeRequest).GetProperty(nameof(AssignRoomEmployeeRequest.EmployeeId)));
        Assert.NotNull(typeof(AssignRoomRiderRequest).GetProperty(nameof(AssignRoomRiderRequest.RiderProfileId)));
        Assert.NotNull(typeof(AssignHousingResidentRequest).GetProperty(nameof(AssignHousingResidentRequest.RoomId)));
    }

    [Fact]
    public void FloorEquipmentAndPendingOccupantsHaveDatabaseRelationships()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HousingModelTest;Trusted_Connection=True")
            .Options);
        var model = db.GetService<IDesignTimeModel>().Model;
        var room = model.FindEntityType(typeof(HousingRoom))!;
        var equipment = model.FindEntityType(typeof(HousingEquipment))!;
        var pending = model.FindEntityType(typeof(HousingPendingOccupant))!;

        Assert.Contains(room.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(HousingRoom.FloorId), nameof(HousingRoom.Name)]));
        Assert.Contains(equipment.GetCheckConstraints(), constraint => constraint.Name == "CK_HousingEquipment_Scope");
        Assert.Contains(pending.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(HousingPendingOccupant.IqamaNo)]));
    }
}
