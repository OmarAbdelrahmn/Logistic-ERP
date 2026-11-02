using System.Data.Common;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Maintenance;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class OilChangeWorkflowTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 10, 0, 0, TimeSpan.Zero);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PreviewRollsBackAndFilterEditThenCancellationRestoreStockExactlyOnce()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var edit = f.Edit(detail) with { OilFilterChanged = true, OilFilterInventoryItemId = f.FilterId };
        var before = await f.Db.StockBalances.AsNoTracking().Where(x => x.InventoryItemId == f.OilId).Select(x => x.QuantityOnHand).SingleAsync(Ct);
        var preview = await f.Service.EditOilChangeAsync(f.OperationId, edit, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        Assert.Equal(4m, preview.Value!.Effects.OilQuantityLiters);
        Assert.Equal(before, await f.Db.StockBalances.AsNoTracking().Where(x => x.InventoryItemId == f.OilId).Select(x => x.QuantityOnHand).SingleAsync(Ct));
        Assert.Empty(await f.Db.OilChangeRevisions.ToArrayAsync(Ct));
        edit = edit with { ExpectedEffectsHash = preview.Value.EffectsHash };
        var saved = await f.Service.EditOilChangeAsync(f.OperationId, edit, "add-filter", false, Ct);
        Assert.True(saved.IsSuccess, saved.Error.Description);
        Assert.Equal(26m, await f.OilStock());
        Assert.Equal(4m, await f.FilterStock());
        var replay = await f.Service.EditOilChangeAsync(f.OperationId, edit, "add-filter", false, Ct);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(saved.Value), System.Text.Json.JsonSerializer.Serialize(replay.Value));
        var cancel = new OilChangeCancelRequest("duplicate", saved.Value!.RowVersion);
        var cancelPreview = await f.Service.CancelOilChangeAsync(f.OperationId, cancel, "", true, Ct);
        Assert.True(cancelPreview.IsSuccess, cancelPreview.Error.Description);
        cancel = cancel with { ExpectedEffectsHash = cancelPreview.Value!.EffectsHash };
        var cancelled = await f.Service.CancelOilChangeAsync(f.OperationId, cancel, "cancel", false, Ct);
        Assert.True(cancelled.IsSuccess, cancelled.Error.Description);
        Assert.Equal(30m, await f.OilStock()); Assert.Equal(5m, await f.FilterStock());
        Assert.Equal(50m, cancelled.Value!.Effects.VehicleMileageKm);
        var repeated = await f.Service.CancelOilChangeAsync(f.OperationId, cancel, "cancel", false, Ct);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(cancelled.Value), System.Text.Json.JsonSerializer.Serialize(repeated.Value));
        var secondKey = await f.Service.CancelOilChangeAsync(f.OperationId, cancel, "other-cancel", false, Ct);
        Assert.Equal(OilChangeWorkflowErrors.Cancelled.Code, secondKey.Error.Code);
        Assert.Equal(30m, await f.OilStock());
        Assert.Equal(0m, await f.Db.VehicleExpenses.SumAsync(x => x.TotalAmount, Ct));
        Assert.Equal(2, await f.Db.OilChangeRevisions.CountAsync(Ct));
        Assert.Single(await f.Db.VehicleOdometerReadings.IgnoreQueryFilters().Where(x => x.SourceEntityId == f.OperationId && x.IsVoided).ToArrayAsync(Ct));
    }

    [Fact]
    public async Task DateAndOdometerEditPreservesStockPostsCurrentMonthAndRetainsOriginalEvidence()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var request = f.Edit(detail) with { PerformedAtUtc = new(2026, 9, 30, 22, 0, 0, TimeSpan.Zero), OdometerAtChange = 80 };
        var movements = await f.Db.StockMovements.CountAsync(Ct);
        var preview = await f.Service.EditOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var saved = await f.Service.EditOilChangeAsync(f.OperationId, request with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "date", false, Ct);
        Assert.True(saved.IsSuccess, saved.Error.Description);
        Assert.Equal(movements, await f.Db.StockMovements.CountAsync(Ct));
        Assert.Equal(26.5m, await f.OilStock());
        var expenses = await f.Db.VehicleExpenses.AsNoTracking().ToArrayAsync(Ct);
        Assert.Equal(35m, expenses.Sum(x => x.TotalAmount));
        Assert.All(expenses.Where(x => x.SourceEntityType == nameof(OilChangeRevision)), x => Assert.Equal(new DateOnly(2026, 10, 31), x.OccurredOn));
        var revised = await f.Detail();
        Assert.Equal(request.PerformedAtUtc, revised.Operation.PerformedAtUtc);
        Assert.Equal(request.PerformedAtUtc, revised.Materials[0].EffectiveServiceAtUtc);
        Assert.NotEqual(request.PerformedAtUtc, revised.Materials[0].UsedAtUtc);
        Assert.Single(await f.Db.VehicleOdometerReadings.IgnoreQueryFilters().Where(x => x.SourceEntityId == f.OperationId && x.IsVoided).ToArrayAsync(Ct));
        Assert.Equal(80m, saved.Value!.Effects.VehicleMileageKm);
    }

    [Fact]
    public async Task StalePreviewPermissionChangesAndIdempotencyMismatchAreRejected()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var request = f.Edit(detail);
        var badVersion = await f.Service.EditOilChangeAsync(f.OperationId, request with { RowVersion = "AAAA" }, "", true, Ct);
        Assert.Equal(MaintenanceErrors.ConcurrencyConflict.Code, badVersion.Error.Code);
        f.Permissions.Denied = PermissionKeys.Maintenance.OilDateEdit;
        var denied = await f.Service.EditOilChangeAsync(f.OperationId, request with { PerformedAtUtc = request.PerformedAtUtc.AddDays(-1) }, "", true, Ct);
        Assert.Equal(MaintenanceErrors.Forbidden.Code, denied.Error.Code);
        f.Permissions.Denied = null;
        var preview = await f.Service.EditOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var changed = await f.Service.EditOilChangeAsync(f.OperationId,
            request with { OtherCost = 10, ExpectedEffectsHash = preview.Value!.EffectsHash }, "changed", false, Ct);
        Assert.Equal(OilChangeWorkflowErrors.PreviewChanged.Code, changed.Error.Code);
        Assert.Empty(await f.Db.OilChangeRevisions.ToArrayAsync(Ct));
        var saved = await f.Service.EditOilChangeAsync(f.OperationId,
            request with { ExpectedEffectsHash = preview.Value.EffectsHash }, "same-key", false, Ct);
        Assert.True(saved.IsSuccess, saved.Error.Description);
        var mismatch = await f.Service.EditOilChangeAsync(f.OperationId,
            request with { OtherCost = 1, ExpectedEffectsHash = preview.Value.EffectsHash }, "same-key", false, Ct);
        Assert.Equal(MaintenanceErrors.OilChangeIdempotencyConflict.Code, mismatch.Error.Code);
        Assert.Equal(26.5m, await f.OilStock());
    }

    [Fact]
    public async Task LateAuditFailureRollsBackStockCostsStatusAndRevision()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var request = new OilChangeCancelRequest("wrong entry", detail.Operation.RowVersion!);
        var preview = await f.Service.CancelOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        f.Fault.Enabled = true;
        var result = await f.Service.CancelOilChangeAsync(f.OperationId,
            request with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "fault", false, Ct);
        Assert.True(result.IsFailure);
        Assert.Equal(26.5m, await f.OilStock());
        Assert.Equal(35m, await f.Db.VehicleExpenses.SumAsync(x => x.TotalAmount, Ct));
        Assert.False((await f.Detail()).Operation.IsCancelled);
        Assert.Empty(await f.Db.OilChangeRevisions.ToArrayAsync(Ct));
        Assert.Empty(await f.Db.MaintenanceMaterialUsages.Where(x => x.ReversalOfUsageId != null).ToArrayAsync(Ct));
    }

    [Fact]
    public async Task ReturnedBarrelCancellationRecordsWasteWithoutIncreasingStock()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var barrel = await f.Db.OilBarrels.SingleAsync(Ct); barrel.Status = OilBarrelStatus.Returned;
        await f.Db.SaveChangesAsync(Ct);
        var result = await f.Service.CancelOilChangeAsync(f.OperationId,
            new("cancel", detail.Operation.RowVersion!), "", true, Ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(3.5m, result.Value!.Effects.ReturnedAsWasteLiters);
        Assert.Equal(26.5m, await f.OilStock());
        Assert.Empty(await f.Db.OilChangeRevisions.ToArrayAsync(Ct));
        var committed = await f.Service.CancelOilChangeAsync(f.OperationId,
            new("cancel", detail.Operation.RowVersion!, ExpectedEffectsHash: result.Value.EffectsHash), "closed", false, Ct);
        Assert.True(committed.IsSuccess, committed.Error.Description);
        Assert.Equal(26.5m, await f.OilStock());
        var waste = await f.Db.OilBarrelLosses.SingleAsync(Ct);
        Assert.Equal(OilBarrelMissingReason.Wasted, waste.MissingReason);
        Assert.Equal(3.5m, waste.QuantityLiters);
        Assert.Equal(committed.Value!.RevisionId, waste.CorrectionRevisionId);
        Assert.Equal(OilBarrelStatus.Returned, (await f.Db.OilBarrels.AsNoTracking().SingleAsync(Ct)).Status);
    }

    [Fact]
    public async Task RemovingFilterRestoresItAndUsesTheCorrectCarOilQuantity()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var add = f.Edit(detail) with { OilFilterChanged = true, OilFilterInventoryItemId = f.FilterId };
        var preview = await f.Service.EditOilChangeAsync(f.OperationId, add, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var added = await f.Service.EditOilChangeAsync(f.OperationId, add with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "add", false, Ct);
        Assert.True(added.IsSuccess, added.Error.Description);
        var remove = f.Edit(await f.Detail());
        var rp = await f.Service.EditOilChangeAsync(f.OperationId, remove, "", true, Ct);
        Assert.True(rp.IsSuccess, rp.Error.Description);
        var result = await f.Service.EditOilChangeAsync(f.OperationId, remove with { ExpectedEffectsHash = rp.Value!.EffectsHash }, "remove", false, Ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(3.5m, result.Value!.Effects.OilQuantityLiters);
        Assert.Equal(26.5m, await f.OilStock()); Assert.Equal(5m, await f.FilterStock());
        Assert.Equal(35m, await f.Db.VehicleExpenses.SumAsync(x => x.TotalAmount, Ct));
    }

    [Fact]
    public async Task CorrectedDateUsesHistoricalRiderAndAuditPreservesPreviousAttribution()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var employee = new LogisticsERP.Domain.Entities.Workforce.Employee { FullNameAr = "Historical rider" };
        var rider = new LogisticsERP.Domain.Entities.Workforce.RiderProfile { EmployeeId = employee.Id };
        var correctedDate = Now.AddDays(-5);
        f.Db.AddRange(employee, rider, new RiderVehicleAssignment { RiderProfileId = rider.Id,
            VehicleId = detail.Operation.VehicleId!.Value, OperationId = Guid.NewGuid(),
            StartedAtUtc = correctedDate.AddDays(-1), EndedAtUtc = correctedDate.AddDays(1),
            Status = RiderVehicleAssignmentStatus.Completed });
        await f.Db.SaveChangesAsync(Ct);
        var request = f.Edit(detail) with { PerformedAtUtc = correctedDate };
        var preview = await f.Service.EditOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var result = await f.Service.EditOilChangeAsync(f.OperationId,
            request with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "rider-date", false, Ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(rider.Id, result.Value!.Effects.RiderProfileId);
        var revision = await f.Db.OilChangeRevisions.SingleAsync(Ct);
        Assert.Contains("\"RiderProfileId\":null", revision.BeforeJson, StringComparison.Ordinal);
        Assert.Contains(rider.Id.ToString(), revision.AfterJson, StringComparison.OrdinalIgnoreCase);
        var currentExpense = await f.Db.VehicleExpenses.AsNoTracking().SingleAsync(x => x.SourceEntityId == revision.Id && !x.ReversalOfExpenseId.HasValue, Ct);
        Assert.Equal(rider.Id, currentExpense.RiderProfileId);
    }

    [Fact]
    public async Task CancellingOneDuplicateLeavesOtherOperationAndItsMileageActive()
    {
        await using var f = await Fixture.Create();
        var first = await f.Detail();
        var car = await f.Db.Vehicles.SingleAsync(x => x.Id == first.Operation.VehicleId, Ct);
        var second = await f.Service.CompleteDirectOilChangeAsync(car.Id,
            new(Now.AddDays(-1), 120, f.LocationId, f.OilId, null, false, null, 3.5m, 0, "second", Convert.ToBase64String(car.RowVersion)), "second", Ct);
        Assert.True(second.IsSuccess, second.Error.Description);
        var request = new OilChangeCancelRequest("duplicate selected", first.Operation.RowVersion!);
        var preview = await f.Service.CancelOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var result = await f.Service.CancelOilChangeAsync(f.OperationId,
            request with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "first-only", false, Ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(120m, result.Value!.Effects.VehicleMileageKm);
        Assert.Equal(26.5m, await f.OilStock());
        Assert.Single(await f.Db.OilChangeOperations.Where(x => !x.IsCancelled).ToArrayAsync(Ct));
        Assert.Equal(35m, await f.Db.VehicleExpenses.SumAsync(x => x.TotalAmount, Ct));
    }

    [Fact]
    public async Task CancellingEarlierOilReadingRecalculatesFollowingGpsMileage()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        f.Db.VehicleDailyDistances.Add(new VehicleDailyDistance { VehicleId = detail.Operation.VehicleId!.Value,
            WorkDate = new DateOnly(2026, 10, 30), GpsDistanceKm = 20, AppliedDistanceKm = 20,
            AppliedSource = VehicleDailyDistanceSource.Gps, EffectiveOdometerAfterKm = 120 });
        var vehicle = await f.Db.Vehicles.SingleAsync(x => x.Id == detail.Operation.VehicleId, Ct);
        vehicle.CurrentOdometer = 120; vehicle.TrackedDistanceKm = 120;
        await f.Db.SaveChangesAsync(Ct);
        var request = new OilChangeCancelRequest("wrong earlier reading", detail.Operation.RowVersion!);
        var preview = await f.Service.CancelOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var saved = await f.Service.CancelOilChangeAsync(f.OperationId,
            request with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "gps-following", false, Ct);
        Assert.True(saved.IsSuccess, saved.Error.Description);
        Assert.Equal(70m, saved.Value!.Effects.VehicleMileageKm);
        Assert.Equal(70m, (await f.Db.VehicleDailyDistances.SingleAsync(Ct)).EffectiveOdometerAfterKm);
    }

    [Fact]
    public async Task ExternalWorkOrderCancellationReversesCostsAndPreservesCustomerPayment()
    {
        await using var f = await Fixture.Create(external: true);
        var detail = await f.Detail();
        var workId = detail.Operation.MaintenanceWorkOrderId!.Value;
        f.Db.ExternalCustomerPayments.Add(new ExternalCustomerPayment { MaintenanceWorkOrderId = workId,
            Amount = 10, PaidAtUtc = Now, PaymentMethod = WorkshopPaymentMethod.Cash, RecordedByUserId = Guid.NewGuid() });
        await f.Db.SaveChangesAsync(Ct);
        var request = new OilChangeCancelRequest("external cancellation", detail.Operation.RowVersion!, detail.WorkOrderRowVersion);
        var preview = await f.Service.CancelOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var result = await f.Service.CancelOilChangeAsync(f.OperationId,
            request with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "external", false, Ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(0m, await f.Db.ExternalMaintenanceFinancialEntries.SumAsync(x => x.TotalAmount, Ct));
        Assert.Equal(10m, await f.Db.ExternalCustomerPayments.SumAsync(x => x.Amount, Ct));
        var work = await f.Db.MaintenanceWorkOrders.SingleAsync(x => x.Id == workId, Ct);
        Assert.Equal(MaintenanceWorkOrderStatus.Completed, work.Status);
        Assert.Equal(0m, work.ActualTotalCost);
        Assert.Equal(30m, await f.OilStock());
    }

    [Fact]
    public async Task CompanyWorkOrderEditKeepsItsIdentityAndCancellationUpdatesTotals()
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var siteId = await f.Db.InventoryLocations.Where(x => x.Id == f.LocationId).Select(x => x.MaintenanceLocationId).SingleAsync(Ct);
        var work = new MaintenanceWorkOrder { WorkOrderNumber = "WAREHOUSE-WF", VehicleId = detail.Operation.VehicleId,
            ServiceSubjectType = MaintenanceServiceSubjectType.CompanyVehicle, MaintenanceLocationId = siteId,
            MaintenanceType = MaintenanceType.OilChange, Status = MaintenanceWorkOrderStatus.Completed,
            ActualMaterialCost = 35, ActualTotalCost = 35, OpenedAtUtc = Now.AddDays(-3), CompletedAtUtc = detail.Operation.PerformedAtUtc };
        f.Db.Add(work); await f.Db.SaveChangesAsync(Ct);
        await f.Db.OilChangeOperations.Where(x => x.Id == f.OperationId).ExecuteUpdateAsync(s => s.SetProperty(x => x.MaintenanceWorkOrderId, work.Id), Ct);
        await f.Db.MaintenanceMaterialUsages.Where(x => x.Id == detail.Operation.OilMaterialUsageId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaintenanceWorkOrderId, work.Id), Ct);
        f.Db.ChangeTracker.Clear(); detail = await f.Detail();
        var edit = f.Edit(detail) with { OilFilterChanged = true, OilFilterInventoryItemId = f.FilterId };
        var preview = await f.Service.EditOilChangeAsync(f.OperationId, edit, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var saved = await f.Service.EditOilChangeAsync(f.OperationId,
            edit with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "work-edit", false, Ct);
        Assert.True(saved.IsSuccess, saved.Error.Description);
        Assert.Equal(60m, (await f.Db.MaintenanceWorkOrders.AsNoTracking().SingleAsync(x => x.Id == work.Id, Ct)).ActualTotalCost);
        var updated = await f.Detail();
        var cancel = new OilChangeCancelRequest("cancel", updated.Operation.RowVersion!, updated.WorkOrderRowVersion);
        var cp = await f.Service.CancelOilChangeAsync(f.OperationId, cancel, "", true, Ct);
        Assert.True(cp.IsSuccess, cp.Error.Description);
        var result = await f.Service.CancelOilChangeAsync(f.OperationId, cancel with { ExpectedEffectsHash = cp.Value!.EffectsHash }, "work-cancel", false, Ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(0m, (await f.Db.MaintenanceWorkOrders.AsNoTracking().SingleAsync(x => x.Id == work.Id, Ct)).ActualTotalCost);
        Assert.Equal(1, await f.Db.OilChangeOperations.CountAsync(x => x.MaintenanceWorkOrderId == work.Id, Ct));
    }

    [Fact]
    public async Task StockLostAfterPreviewRollsBackTheEarlierOilRestoration()
    {
        await using var f = await Fixture.Create();
        var edit = f.Edit(await f.Detail()) with { OilFilterChanged = true, OilFilterInventoryItemId = f.FilterId };
        var preview = await f.Service.EditOilChangeAsync(f.OperationId, edit, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var balance = await f.Db.StockBalances.SingleAsync(x => x.InventoryItemId == f.FilterId, Ct);
        var layer = await f.Db.StockCostLayers.SingleAsync(x => x.InventoryItemId == f.FilterId, Ct);
        balance.QuantityOnHand = 0; layer.RemainingQuantity = 0; await f.Db.SaveChangesAsync(Ct);
        var result = await f.Service.EditOilChangeAsync(f.OperationId,
            edit with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "stock-race", false, Ct);
        Assert.Equal(MaintenanceErrors.InsufficientStock.Code, result.Error.Code);
        Assert.Equal(26.5m, await f.OilStock());
        Assert.Empty(await f.Db.OilChangeRevisions.ToArrayAsync(Ct));
        Assert.False((await f.Detail()).Operation.OilFilterChanged);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DepletedBarrelReturnsBecomeWasteEvenWithAnotherOpenBarrel(bool otherOpen)
    {
        await using var f = await Fixture.Create();
        var detail = await f.Detail();
        var barrel = await f.Db.OilBarrels.SingleAsync(Ct);
        var layer = await f.Db.StockCostLayers.SingleAsync(x => x.Id == barrel.StockCostLayerId, Ct);
        var balance = await f.Db.StockBalances.SingleAsync(x => x.InventoryItemId == f.OilId, Ct);
        barrel.Status = OilBarrelStatus.Depleted; barrel.RemainingLiters = 0; barrel.RecordedMissingLiters = 26.5m;
        barrel.DepletedAtUtc = Now.AddDays(-1); layer.RemainingQuantity = 0; balance.QuantityOnHand = 0;
        if (otherOpen)
        {
            var nextLayer = new StockCostLayer { InventoryItemId = f.OilId, InventoryLocationId = f.LocationId,
                OriginalQuantity = 30, RemainingQuantity = 30, UnitCost = 10, OriginalTotalCost = 300,
                BaseUnitOfMeasure = InventoryUnitOfMeasure.Liter, ReceivedAtUtc = Now.AddDays(-1), OriginalSequence = 2 };
            f.Db.Add(nextLayer); f.Db.OilBarrels.Add(new OilBarrel { BarrelNumber = "WF-NEXT", InventoryItemId = f.OilId,
                InventoryLocationId = f.LocationId, StockCostLayerId = nextLayer.Id, PackageSequence = 2,
                NominalCapacityLiters = 30, RemainingLiters = 30, UnitCostPerLiter = 10, MaximumAllowedLossLiters = 0.6m,
                Status = OilBarrelStatus.Open, OpenedAtUtc = Now.AddDays(-1), AllowedVehicleType = VehicleType.Car });
            balance.QuantityOnHand = 30;
        }
        await f.Db.SaveChangesAsync(Ct);
        var request = new OilChangeCancelRequest("depleted barrel", detail.Operation.RowVersion!);
        var preview = await f.Service.CancelOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var result = await f.Service.CancelOilChangeAsync(f.OperationId,
            request with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "closed-depleted", false, Ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(otherOpen ? 30m : 0m, await f.OilStock());
        Assert.Equal(3.5m, result.Value!.Effects.ReturnedAsWasteLiters);
        Assert.Equal(OilBarrelStatus.Depleted, (await f.Db.OilBarrels.AsNoTracking().SingleAsync(x => x.Id == barrel.Id, Ct)).Status);
        Assert.Equal(30m, (await f.Db.OilBarrels.AsNoTracking().SingleAsync(x => x.Id == barrel.Id, Ct)).RecordedMissingLiters);
    }

    [Fact]
    public async Task SameWorkflowRunsOnIsolatedSqlServerWhenConfigured()
    {
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LOGISTICS_OIL_WORKFLOW_SQL")),
            "Set LOGISTICS_OIL_WORKFLOW_SQL to a dedicated local/test SQL Server connection; never production.");
        await using var f = await Fixture.Create(sql: true);
        var detail = await f.Detail();
        var request = new OilChangeCancelRequest("SQL cancellation", detail.Operation.RowVersion!);
        var preview = await f.Service.CancelOilChangeAsync(f.OperationId, request, "", true, Ct);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        Assert.Equal(26.5m, await f.OilStock());
        var result = await f.Service.CancelOilChangeAsync(f.OperationId,
            request with { ExpectedEffectsHash = preview.Value!.EffectsHash }, "sql", false, Ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(30m, await f.OilStock());
        Assert.Equal(0m, await f.Db.VehicleExpenses.SumAsync(x => x.TotalAmount, Ct));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required MaintenanceService Service { get; init; }
        public required Permissions Permissions { get; init; }
        public required AuditFault Fault { get; init; }
        public DbConnection? Connection { get; init; }
        public required Guid OperationId { get; init; }
        public required Guid OilId { get; init; }
        public required Guid FilterId { get; init; }
        public required Guid LocationId { get; init; }
        public bool Sql { get; init; }

        public static async Task<Fixture> Create(bool sql = false, bool external = false)
        {
            var user = new User(); var clock = new Clock(); var permissions = new Permissions(); var fault = new AuditFault();
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            DbConnection? connection = null;
            if (sql)
            {
                var cs = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("LOGISTICS_OIL_WORKFLOW_SQL"))
                { InitialCatalog = "OilWorkflow_" + Guid.NewGuid().ToString("N") };
                builder.UseSqlServer(cs.ConnectionString);
            }
            else
            {
                connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(Ct);
                builder.UseSqlite(connection).ReplaceService<IModelCustomizer, OilWorkflowTestModelCustomizer>();
            }
            builder.AddInterceptors(new ApplicationPersistenceInterceptor(user, clock), fault);
            if (!sql) builder.AddInterceptors(new SimulatedVersions());
            var db = new ApplicationDbContext(builder.Options);
            await db.Database.EnsureCreatedAsync(Ct);
            var location = await db.InventoryLocations.SingleAsync(x => x.Id == InventoryLocation.JeddahWarehouseInventoryId, Ct);
            var site = await db.MaintenanceLocations.SingleAsync(x => x.Id == location.MaintenanceLocationId, Ct);
            site.AllowsCompanyVehicles = true; site.AllowsExternalVehicles = true; site.InventoryEnabled = true;
            var maker = new VehicleManufacturer { Code = "WF", NameAr = "WF", NameEn = "WF" };
            var model = new VehicleModel { Code = "WF", NameAr = "WF", NameEn = "WF", VehicleManufacturerId = maker.Id, VehicleType = VehicleType.Car };
            var car = new Vehicle { AssetNumber = "WF", NormalizedAssetNumber = "WF", VehicleManufacturerId = maker.Id,
                VehicleModelId = model.Id, VehicleType = VehicleType.Car, CurrentOdometer = 50, TrackedDistanceKm = 50 };
            var oil = new InventoryItem { Sku = "WF-OIL", NormalizedSku = "WFOIL", NameAr = "Oil", NameEn = "Oil",
                ItemType = InventoryItemType.Oil, BaseUnitOfMeasure = InventoryUnitOfMeasure.Liter, PurchaseUnitOfMeasure = InventoryUnitOfMeasure.Liter };
            var filter = new InventoryItem { Sku = "WF-FILTER", NormalizedSku = "WFFILTER", NameAr = "Filter", NameEn = "Filter",
                ItemType = InventoryItemType.SparePart, BaseUnitOfMeasure = InventoryUnitOfMeasure.Piece, PurchaseUnitOfMeasure = InventoryUnitOfMeasure.Piece };
            db.AddRange(maker, model, car, oil, filter);
            db.VehicleOdometerReadings.Add(new VehicleOdometerReading { VehicleId = car.Id, Reading = 50,
                SourceType = VehicleOdometerSourceType.Manual, SourceEntityId = car.Id, RecordedAtUtc = Now.AddMonths(-2) });
            var oilLayer = new StockCostLayer { InventoryItemId = oil.Id, InventoryLocationId = location.Id,
                OriginalQuantity = 30, RemainingQuantity = 30, UnitCost = 10, OriginalTotalCost = 300,
                BaseUnitOfMeasure = InventoryUnitOfMeasure.Liter, ReceivedAtUtc = Now.AddMonths(-2), OriginalSequence = 1 };
            var filterLayer = new StockCostLayer { InventoryItemId = filter.Id, InventoryLocationId = location.Id,
                OriginalQuantity = 5, RemainingQuantity = 5, UnitCost = 20, OriginalTotalCost = 100,
                BaseUnitOfMeasure = InventoryUnitOfMeasure.Piece, ReceivedAtUtc = Now.AddMonths(-2), OriginalSequence = 2 };
            db.AddRange(oilLayer, filterLayer);
            db.OilBarrels.Add(new OilBarrel { BarrelNumber = "WF", InventoryItemId = oil.Id, InventoryLocationId = location.Id,
                StockCostLayerId = oilLayer.Id, PackageSequence = 1, NominalCapacityLiters = 30, RemainingLiters = 30,
                UnitCostPerLiter = 10, MaximumAllowedLossLiters = 0.6m, Status = OilBarrelStatus.Open,
                AllowedVehicleType = VehicleType.Car, OpenedAtUtc = Now.AddMonths(-2) });
            db.StockBalances.AddRange(new StockBalance { InventoryItemId = oil.Id, InventoryLocationId = location.Id,
                QuantityOnHand = 30, ReportingAverageUnitCost = 10 }, new StockBalance { InventoryItemId = filter.Id,
                InventoryLocationId = location.Id, QuantityOnHand = 5, ReportingAverageUnitCost = 20 });
            await db.SaveChangesAsync(Ct);
            var service = new MaintenanceService(db, user, clock, new Files(), permissions);
            Result<OilChangeResponse> created;
            if (external)
            {
                var work = new MaintenanceWorkOrder { WorkOrderNumber = "EXT-WF", MaintenanceLocationId = site.Id,
                    ServiceSubjectType = MaintenanceServiceSubjectType.ExternalVehicle, MaintenanceType = MaintenanceType.OilChange,
                    OpenedAtUtc = Now.AddDays(-3), Status = MaintenanceWorkOrderStatus.Open };
                db.Add(work); db.ExternalVehicleSnapshots.Add(new ExternalVehicleSnapshot
                    { MaintenanceWorkOrderId = work.Id, VehicleType = VehicleType.Car, PlateOrReference = "EXT-WF" });
                await db.SaveChangesAsync(Ct);
                created = await service.CompleteOilChangeAsync(work.Id, new(Now.AddDays(-2), 100,
                    location.Id, oil.Id, null, false, null, 3.5m, 5, 10, "external", Convert.ToBase64String(work.RowVersion)), Ct);
            }
            else created = await service.CompleteDirectOilChangeAsync(car.Id,
                new(Now.AddDays(-2), 100, location.Id, oil.Id, null, false, null, 3.5m, 0, "original", Convert.ToBase64String(car.RowVersion)), "create", Ct);
            Assert.True(created.IsSuccess, created.Error.Description);
            db.ChangeTracker.Clear();
            return new Fixture { Db = db, Service = service, Permissions = permissions, Fault = fault,
                Connection = connection, Sql = sql, OperationId = created.Value!.Id, OilId = oil.Id, FilterId = filter.Id, LocationId = location.Id };
        }
        public async Task<OilChangeWorkflowDetails> Detail()
        { var r = await Service.GetOilChangeDetailsAsync(OperationId, Ct); Assert.True(r.IsSuccess, r.Error.Description); return r.Value!; }
        public OilChangeEditRequest Edit(OilChangeWorkflowDetails details) => new(details.Operation.PerformedAtUtc,
            details.Operation.OdometerAtChange, LocationId, OilId, 3.5m, false, null, null, 0, 0,
            "corrected", "correction", details.Operation.RowVersion!, details.WorkOrderRowVersion);
        public Task<decimal> OilStock() => Db.StockBalances.AsNoTracking().Where(x => x.InventoryItemId == OilId).Select(x => x.QuantityOnHand).SingleAsync(Ct);
        public Task<decimal> FilterStock() => Db.StockBalances.AsNoTracking().Where(x => x.InventoryItemId == FilterId).Select(x => x.QuantityOnHand).SingleAsync(Ct);
        public async ValueTask DisposeAsync()
        { if (Sql) await Db.Database.EnsureDeletedAsync(Ct); await Db.DisposeAsync(); if (Connection is not null) await Connection.DisposeAsync(); }
    }

    private sealed class User : ICurrentUser
    { public Guid? UserId { get; } = Guid.NewGuid(); public Guid? SessionId => null; public long? AuthorizationVersion => 1; public string? CorrelationId => "oil-workflow-test"; }
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    public sealed class Permissions : IPermissionChecker
    {
        public string? Denied { get; set; }
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null,
            CancellationToken cancellationToken = default) => Task.FromResult(permissionKey != Denied);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    public sealed class AuditFault : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<OilChangeRevision>()
                .Any(x => x.State == EntityState.Modified && x.Entity.ResultJson.Length > 0))
            { Enabled = false; throw new DbUpdateException("Injected late failure"); }
            return ValueTask.FromResult(result);
        }
    }
    private sealed class SimulatedVersions : SaveChangesInterceptor
    {
        private long nextSequence;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified))
            {
                if (entry.Metadata.FindProperty("RowVersion") is not null) entry.Property("RowVersion").CurrentValue = Guid.NewGuid().ToByteArray();
                if (entry.Entity is LogisticsERP.Domain.Entities.System.AuditEntry audit && entry.State == EntityState.Added)
                    audit.Sequence = ++nextSequence;
            }
            return ValueTask.FromResult(result);
        }
    }
    private sealed class Files : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) { }
    }
}

// SQLite checks transaction orchestration and rollback without a running SQL Server.
// SQL Server rowversion, constraints and locking are verified by the opt-in SQL test.
public sealed class OilWorkflowTestModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        foreach (var sequence in modelBuilder.Model.GetSequences().ToArray())
            modelBuilder.Model.RemoveSequence(sequence.Name, sequence.Schema);
        var tables = modelBuilder.Model.GetEntityTypes().Select(x => (Entity: x, Table: x.GetTableName(), Schema: x.GetSchema())).ToArray();
        foreach (var (entity, table, schema) in tables)
        {
            if (table is not null) { entity.SetTableName((schema ?? "app") + "_" + table); entity.SetSchema(null); }
            foreach (var constraint in entity.GetCheckConstraints().ToArray()) entity.RemoveCheckConstraint(constraint.Name!);
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnType(null); property.SetCollation(null);
                property.SetDefaultValueSql(null);
                if (entity.ClrType == typeof(LogisticsERP.Domain.Entities.System.AuditEntry) && property.Name == "Sequence")
                    property.ValueGenerated = ValueGenerated.Never;
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTimeOffset))
                    property.SetValueConverter(new ValueConverter<DateTimeOffset, long>(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero)));
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(decimal))
                    property.SetValueConverter(new ValueConverter<decimal, double>(x => (double)x, x => (decimal)x));
                if (property.Name == "RowVersion")
                { property.IsNullable = true; property.SetDefaultValue(Array.Empty<byte>()); property.ValueGenerated = ValueGenerated.Never; property.SetBeforeSaveBehavior(PropertySaveBehavior.Save); property.SetAfterSaveBehavior(PropertySaveBehavior.Save); }
            }
        }
    }
}
