using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Telecom;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class HungerQualityServiceTests
{
    private static DateTimeOffset At(int day) => new(2026, 11, day, 0, 0, 0, TimeSpan.FromHours(3));

    private static async Task<RiderProfile> Assign(Fixture f)
    {
        var employee = new Employee { FullNameAr = "Owner", FullNameEn = "Owner English", IqamaNo = "1234567890", PrimaryPhone = "0501111111" };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        f.Account.RegisteredEmployeeId = employee.Id;
        f.Db.Update(f.Account);
        f.Db.AddRange(employee, rider, new RiderClientAssignment { PlatformRiderAccountId = f.Account.Id,
            RiderProfileId = rider.Id, EffectiveFrom = new(2026, 11, 1), Status = RiderAssignmentStatus.Active },
            new PlatformAccountBusinessDetails { PlatformRiderAccountId = f.Account.Id, PhoneNumber = "0501234567",
                RegistryIdentifier = "Registry", SettlementModel = "Monthly", RegisteredVehicleType = "Car",
                RegisteredVehiclePlate = "REG", RegisteredVehicleSerial = "SERIAL", SimOwnership = "ImportedOnly" });
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return rider;
    }

    private static async Task<HungerAccountItem> Item(Fixture f) => Assert.Single(Value(await f.Service.AccountsAsync(null, 1, 100, TestContext.Current.CancellationToken)).Items);

    [Fact]
    public async Task HungerAccountSharedContextUsesHungerReadAndRejectsPermissionLoss()
    {
        await using var f = await Fixture.Create(); await Assign(f);
        f.Checker.OnlyPermission = LogisticsERP.Application.Authorization.PermissionKeys.Hunger.Read;
        Assert.Equal("None", (await Item(f)).ActualVehicleCustodyStatus);
        f.Checker.Allowed = false;
        Assert.True((await f.Service.AccountsAsync(null, 1, 10, TestContext.Current.CancellationToken)).IsFailure);
    }

    [Fact]
    public async Task HungerAccountSeveralCompanyVehiclesKeepEveryPeriodAndMixedIdentity()
    {
        await using var f = await Fixture.Create(); var rider = await Assign(f);
        var a = new Vehicle { PlateNumberAr = "FIRST", VehicleType = VehicleType.Car };
        var b = new Vehicle { PlateNumberAr = "SECOND", VehicleType = VehicleType.Motorcycle };
        f.Db.AddRange(a, b, new RiderVehicleAssignment { RiderProfileId = rider.Id, VehicleId = a.Id, StartedAtUtc = At(1), EndedAtUtc = At(2) },
            new RiderVehicleAssignment { RiderProfileId = rider.Id, VehicleId = b.Id, StartedAtUtc = At(2) });
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = await Item(f); Assert.Equal("Mixed", item.ActualVehicleCustodyStatus);
        Assert.Equal("Mixed", item.ActualVehiclePlate); Assert.Equal("Mixed", item.ActualVehicleType);
        Assert.Equal(2, item.ActualVehiclePeriods.Count);
        var match = Value(await f.Service.AccountsFilteredAsync(new HungerAccountFilter { ActualVehiclePlate = "Mixed", ActualVehicleType = "Mixed" }, 1, 10, TestContext.Current.CancellationToken));
        Assert.Equal(1, match.TotalCount);
    }

    [Fact]
    public async Task HungerAccountVehiclePeriodsIncludeEndedCustodyAndVerifiedGaps()
    {
        await using var f = await Fixture.Create(); var rider = await Assign(f);
        var car = new Vehicle { VehicleType = VehicleType.Car, PlateNumberAr = "ACTUAL", OwnershipType = VehicleOwnershipType.Owned };
        f.Db.AddRange(car, new RiderVehicleAssignment { RiderProfileId = rider.Id, VehicleId = car.Id,
            StartedAtUtc = At(1), EndedAtUtc = At(2), Status = RiderVehicleAssignmentStatus.Completed });
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = await Item(f);
        Assert.Equal("Mixed", item.ActualVehicleOwnership);
        Assert.Equal("Mixed", item.ActualVehicleCustodyStatus);
        Assert.Equal("REG", item.RegisteredVehiclePlate);
        Assert.Collection(item.ActualVehiclePeriods, p => { Assert.Equal("Company", p.Ownership); Assert.Equal(At(2), p.ThroughUtc); },
            p => { Assert.Equal("Private", p.Ownership); Assert.Equal(At(2), p.FromUtc); });
    }

    [Fact]
    public async Task HungerAccountNoCustodyIsNoneButMissingAssignmentAndOverlapsAreUnknown()
    {
        await using var f = await Fixture.Create();
        Assert.Equal("Unknown", (await Item(f)).ActualVehicleCustodyStatus);
        var rider = await Assign(f);
        var none = await Item(f); Assert.Equal("None", none.ActualVehicleCustodyStatus); Assert.Equal("Private", none.ActualVehicleOwnership);
        var car = new Vehicle { PlateNumberAr = "A" };
        f.Db.AddRange(car, new RiderVehicleAssignment { RiderProfileId = rider.Id, VehicleId = car.Id, StartedAtUtc = At(1) },
            new RiderVehicleAssignment { RiderProfileId = rider.Id, VehicleId = car.Id, StartedAtUtc = At(2) });
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var conflict = await Item(f); Assert.Equal("Unknown", conflict.ActualVehicleCustodyStatus); Assert.Equal("Unknown", conflict.ActualVehicleOwnership);
    }

    [Fact]
    public async Task HungerAccountSubstituteMatchesVerifiedIdentityRatherThanOfficialCustodian()
    {
        await using var f = await Fixture.Create(); var rider = await Assign(f);
        var vehicle = new Vehicle { PlateNumberAr = "SUBSTITUTE", OwnershipType = VehicleOwnershipType.Owned };
        var custody = new RiderVehicleAssignment { VehicleId = vehicle.Id, RiderProfileId = Guid.NewGuid(), IsRealRider = false, StartedAtUtc = At(1) };
        f.Db.AddRange(vehicle, custody, new RealRider { RiderVehicleAssignmentId = custody.Id, IqamaNo = "1234567890" });
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = await Item(f); Assert.Equal("SUBSTITUTE", item.ActualVehiclePlate);
        Assert.Equal(rider.Id, Assert.Single(item.ActualVehiclePeriods).RiderProfileId);
        Assert.Equal("Company", item.ActualVehicleOwnership);
    }

    [Fact]
    public async Task HungerAccountSimHistorySeparatesOwnershipHolderAndResponsibleEmployeeFromImport()
    {
        await using var f = await Fixture.Create(); var rider = await Assign(f);
        var responsible = Guid.NewGuid();
        f.Db.Add(new Employee { Id = responsible, FullNameAr = "SIM responsible", IqamaNo = "2222222222" });
        var sim = new PhoneSimCard { NormalizedPhoneNumber = "+966501234567", ResponsibleEmployeeId = responsible, Ownership = "Private", OwnershipEffectiveFrom = new(2026, 11, 3) };
        f.Db.AddRange(sim, new PhoneSimOwnershipRevision { PhoneSimCardId = sim.Id, Ownership = "Company", EffectiveFrom = new(2026, 11, 1), EvidenceReference = "purchase" },
            new PhoneSimOwnershipRevision { PhoneSimCardId = sim.Id, Ownership = "Private", EffectiveFrom = new(2026, 11, 3), EvidenceReference = "transfer" },
            new RiderPhoneSimAssignment { PhoneSimCardId = sim.Id, RiderProfileId = rider.Id, EffectiveFrom = new(2026, 11, 1) });
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = await Item(f); Assert.Equal("Mixed", item.SimOwnership); Assert.Equal("ImportedOnly", item.SourceSimOwnership);
        Assert.Equal(responsible, item.Sim!.ResponsibleEmployeeId); Assert.Equal(rider.Id, item.Sim.HolderRiderProfileId);
        Assert.Equal("SIM responsible", item.SimResponsibleEmployee?.NameAr);
        Assert.Equal("Owner", item.SimHolder?.NameAr);
        Assert.NotEqual(item.SimResponsibleEmployee?.EmployeeId, item.SimHolder?.EmployeeId);
        Assert.Equal(2, item.Sim.OwnershipPeriods.Count);
        var firstDay = Assert.Single(Value(await f.Service.AccountsFilteredAsync(new HungerAccountFilter { From = new(2026, 11, 1), Through = new(2026, 11, 1) }, 1, 10, TestContext.Current.CancellationToken)).Items);
        Assert.Equal("Company", firstDay.SimOwnership);
    }

    [Fact]
    public async Task HungerAccountFiltersAreAndedBeforeCountAndPagination()
    {
        await using var f = await Fixture.Create(); await Assign(f);
        var matching = new HungerAccountFilter { OwnerName = "own", OwnerIqama = "1234", OwnerPhone = "0501", OwnerNameEn = "English",
            PhoneNumber = "1234567", OperatingCityNameAr = "Test", RegistryIdentifier = "reg", SettlementModel = "month",
            RegisteredVehicleType = "Car", RegisteredVehiclePlate = "REG", RegisteredVehicleSerial = "SERIAL",
            ActualVehicleOwnership = "Private", ActualVehicleCustodyStatus = "None", SimOwnership = "Unknown", SourceSimOwnership = "ImportedOnly",
            AssignmentStatus = "Assigned", OperationalDisposition = "Normal" };
        var page = Value(await f.Service.AccountsFilteredAsync(matching, 2, 1, TestContext.Current.CancellationToken)); Assert.Equal(1, page.TotalCount); Assert.Empty(page.Items);
        var mismatch = Value(await f.Service.AccountsFilteredAsync(matching with { RegisteredVehicleSerial = "missing" }, 1, 10, TestContext.Current.CancellationToken)); Assert.Equal(0, mismatch.TotalCount);
        Assert.True((await f.Service.AccountsFilteredAsync(matching with { From = new(2026, 12, 1) }, 1, 10, TestContext.Current.CancellationToken)).IsFailure);
    }
}

