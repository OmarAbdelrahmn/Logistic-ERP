using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleTakeWithoutFilesTests
{
    [Fact]
    public async Task PromissoryNotesCanBeAttachedToCurrentAssignmentAfterTakingWithoutFiles()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AssignmentPromissoryUpload_{Guid.NewGuid():N}", x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new TestRowVersionInterceptor())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var employee = new Employee { FullNameAr = "Rider", IsEmployee = false, Status = EmployeeStatus.Active };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var vehicle = new Vehicle
        {
            AssetNumber = "PROMISSORY-UPLOAD", NormalizedAssetNumber = "PROMISSORYUPLOAD",
            SerialNumber = "PROMISSORY-SERIAL", NormalizedSerialNumber = "PROMISSORYSERIAL",
            ChassisNumber = "PROMISSORY-CHASSIS", NormalizedChassisNumber = "PROMISSORYCHASSIS",
            PlateNumberAr = "أ ب ج 1234", PlateNumberEn = "ABC 1234",
            SponsorId = Guid.CreateVersion7(), OperatingCityId = Guid.CreateVersion7(),
            RegistrationType = VehicleRegistrationType.Private,
            CurrentOperationalStatus = VehicleOperationalStatus.Available
        };
        db.AddRange(employee, rider, vehicle);
        await db.SaveChangesAsync(cancellationToken);
        var storage = new TestFileStorage();
        var service = new FleetService(db, new FleetServiceSupport(new TestCurrentUser(), new PermitAll(), TimeProvider.System), storage);
        var taken = await service.TakeAsync(new TakeVehicleRequest(rider.Id, true, null, vehicle.Id,
            DateTimeOffset.UtcNow, 0, VehicleCondition.Good, null, "PERM-123", null, null), [], "take-before-upload", cancellationToken);
        Assert.True(taken.IsSuccess, taken.Error.Description);
        var assignmentId = taken.Value!.Id;
        var detail = await service.GetAssignmentAsync(assignmentId, cancellationToken);
        Assert.True(detail.IsSuccess, detail.Error.Description);
        Assert.Empty(detail.Value!.PromissoryFileVersionIds);

        using var fileStream = new MemoryStream([1, 2, 3]);
        var uploads = new[] { new PrivateFileUpload(fileStream, "note.pdf", "application/pdf", 3) };
        var uploaded = await service.AttachPromissoryFilesAsync(assignmentId, detail.Value.RowVersion, uploads, "upload-note-1", cancellationToken);
        Assert.True(uploaded.IsSuccess, uploaded.Error.Description);
        Assert.Single(uploaded.Value!.PromissoryFileVersionIds);
        Assert.Single(await db.RiderVehicleAssignmentPromissoryFiles.ToArrayAsync(cancellationToken));
        Assert.Single(await db.RiderPromissoryFiles.Where(x => x.CurrentVersionId != null).ToArrayAsync(cancellationToken));

        var replay = await service.AttachPromissoryFilesAsync(assignmentId, detail.Value.RowVersion, uploads, "upload-note-1", cancellationToken);
        Assert.True(replay.IsSuccess, replay.Error.Description);
        Assert.Single(await db.RiderPromissoryFiles.ToArrayAsync(cancellationToken));

        var noFiles = await service.AttachPromissoryFilesAsync(assignmentId, uploaded.Value.RowVersion, [], "upload-empty", cancellationToken);
        Assert.Equal(FleetErrors.PromissoryFilesRequired.Code, noFiles.Error.Code);
        var stale = await service.AttachPromissoryFilesAsync(assignmentId, detail.Value.RowVersion, uploads, "upload-stale", cancellationToken);
        Assert.Equal(FleetErrors.ConcurrencyConflict.Code, stale.Error.Code);

        using var secondStream = new MemoryStream([4]);
        using var thirdStream = new MemoryStream([5]);
        var twoMore = await service.AttachPromissoryFilesAsync(assignmentId, uploaded.Value.RowVersion,
            [new PrivateFileUpload(secondStream, "second.pdf", "application/pdf", 1),
             new PrivateFileUpload(thirdStream, "third.pdf", "application/pdf", 1)],
            "upload-notes-2-and-3", cancellationToken);
        Assert.True(twoMore.IsSuccess, twoMore.Error.Description);
        Assert.Equal(3, twoMore.Value!.PromissoryFileVersionIds.Count);
        var overLimit = await service.AttachPromissoryFilesAsync(assignmentId, twoMore.Value.RowVersion, uploads, "upload-note-4", cancellationToken);
        Assert.Equal(FleetErrors.FileLimit.Code, overLimit.Error.Code);

        var assignment = await db.RiderVehicleAssignments.SingleAsync(x => x.Id == assignmentId, cancellationToken);
        assignment.EndedAtUtc = DateTimeOffset.UtcNow;
        vehicle.CurrentAssignmentId = null;
        await db.SaveChangesAsync(cancellationToken);
        var ended = await service.AttachPromissoryFilesAsync(assignmentId, twoMore.Value.RowVersion, uploads, "upload-ended", cancellationToken);
        Assert.Equal(FleetErrors.AssignmentNotActive.Code, ended.Error.Code);
        Assert.Equal(3, await db.RiderPromissoryFiles.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task TakeAndSwitchSucceedWithoutExistingOrUploadedPromissoryFiles()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehicleTakeWithoutFiles_{Guid.NewGuid():N}", x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new TestRowVersionInterceptor())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var employee = new Employee { FullNameAr = "Test rider", IsEmployee = false, Status = EmployeeStatus.Active };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var vehicle = new Vehicle
        {
            AssetNumber = "TAKE-NO-FILES",
            NormalizedAssetNumber = "TAKENOFFILES",
            SerialNumber = "TAKE-SERIAL",
            NormalizedSerialNumber = "TAKESERIAL",
            ChassisNumber = "TAKE-CHASSIS",
            NormalizedChassisNumber = "TAKECHASSIS",
            PlateNumberAr = "أ ب ج 1234",
            PlateNumberEn = "ABC 1234",
            SponsorId = Guid.CreateVersion7(),
            OperatingCityId = Guid.CreateVersion7(),
            RegistrationType = VehicleRegistrationType.Private,
            CurrentOperationalStatus = VehicleOperationalStatus.Available
        };
        var nextVehicle = new Vehicle
        {
            AssetNumber = "SWITCH-NO-FILES",
            NormalizedAssetNumber = "SWITCHNOFILES",
            SerialNumber = "SWITCH-SERIAL",
            NormalizedSerialNumber = "SWITCHSERIAL",
            ChassisNumber = "SWITCH-CHASSIS",
            NormalizedChassisNumber = "SWITCHCHASSIS",
            PlateNumberAr = "أ ب ج 5678",
            PlateNumberEn = "ABC 5678",
            SponsorId = vehicle.SponsorId,
            OperatingCityId = vehicle.OperatingCityId,
            RegistrationType = VehicleRegistrationType.Private,
            CurrentOperationalStatus = VehicleOperationalStatus.Available
        };
        db.AddRange(employee, rider, vehicle, nextVehicle);
        await db.SaveChangesAsync(cancellationToken);
        var service = new FleetService(db,
            new FleetServiceSupport(new TestCurrentUser(), new PermitAll(), TimeProvider.System),
            new UnusedFileStorage());
        var request = new TakeVehicleRequest(rider.Id, true, null, vehicle.Id,
            DateTimeOffset.UtcNow, 0, VehicleCondition.Good, null, "PERM-123", null, null);

        var result = await service.TakeAsync(request, [], "take-without-files", cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Empty(result.Value!.PromissoryFileVersionIds);
        Assert.Equal(RiderVehicleAssignmentStatus.Active, result.Value.Status);
        Assert.Equal(VehicleOperationalStatus.Assigned, vehicle.CurrentOperationalStatus);
        Assert.Empty(await db.RiderVehicleAssignmentPromissoryFiles.ToArrayAsync(cancellationToken));

        var replay = await service.TakeAsync(request, [], "take-without-files", cancellationToken);
        Assert.True(replay.IsSuccess, replay.Error.Description);
        Assert.Equal(result.Value.Id, replay.Value!.Id);

        var switchRequest = new SwitchVehicleRequest(result.Value.Id, nextVehicle.Id,
            request.StartedAtUtc.AddMinutes(5), 0, 0, VehicleCondition.Good,
            VehicleCondition.Good, null, null, "PERM-124", "Vehicle switch", result.Value.RowVersion);
        var switched = await service.SwitchAsync(switchRequest, [], [], "switch-without-files", cancellationToken);
        Assert.True(switched.IsSuccess, switched.Error.Description);
        Assert.Empty(switched.Value!.PromissoryFileVersionIds);
        Assert.Equal(nextVehicle.Id, switched.Value.VehicleId);
        Assert.Empty(await db.RiderVehicleAssignmentPromissoryFiles.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task CompleteHistoriesIncludeReturnsEvidenceArchivedAccidentsOilAndEquipment()
    {
        var ct = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CompleteHistory_{Guid.NewGuid():N}", x => x.EnableNullChecks(false))
            .AddInterceptors(new TestRowVersionInterceptor()).Options;
        await using var db = new ApplicationDbContext(options);
        var now = DateTimeOffset.Parse("2026-09-06T10:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture);
        var actor = Guid.NewGuid();
        var employee = new Employee { FullNameAr = "History Rider", IsEmployee = false, Status = EmployeeStatus.Active };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var vehicle = new Vehicle
        {
            AssetNumber = "HISTORY-CAR", NormalizedAssetNumber = "HISTORYCAR",
            VehicleManufacturerId = Guid.NewGuid(), VehicleModelId = Guid.NewGuid()
        };
        var assignment = new RiderVehicleAssignment
        {
            RiderProfileId = rider.Id, VehicleId = vehicle.Id, OperationId = Guid.NewGuid(),
            StartedAtUtc = now.AddDays(-3), EndedAtUtc = now.AddDays(-1),
            AssignedByUserId = actor, EndedByUserId = actor
        };
        var issue = new VehicleIssue
        {
            IssueNumber = "RET-1", VehicleId = vehicle.Id,
            RelatedAssignmentId = assignment.Id, ReportedAtUtc = now.AddDays(-1),
            ReportedByUserId = actor, Description = "Damage at return"
        };
        var evidence = new VehicleIssueEvidence
        {
            VehicleIssueId = issue.Id, OriginalFileName = "return.jpg", StoredFileName = "stored.jpg",
            ContentType = "image/jpeg", FileSizeBytes = 100, StoragePath = "private/return.jpg",
            UploadedAtUtc = now.AddDays(-1), UploadedByUserId = actor
        };
        var accident = new VehicleAccident
        {
            AccidentNumber = "OLD-ACC-1", VehicleId = vehicle.Id, RiderProfileId = rider.Id,
            EmployeeId = employee.Id, RiderVehicleAssignmentId = assignment.Id,
            VehicleIssueId = issue.Id, OccurredAtUtc = now.AddDays(-2),
            ReportedAtUtc = now.AddDays(-2), ReportedByUserId = actor,
            LocationDescription = "Road", DamageDescription = "Damage", IsDeleted = true
        };
        var oilItem = new InventoryItem
        {
            Sku = "HISTORY-OIL", NormalizedSku = "HISTORY-OIL", NameAr = "زيت", NameEn = "Oil"
        };
        var oilUsage = new MaintenanceMaterialUsage
        {
            InventoryItemId = oilItem.Id, InventoryLocationId = Guid.NewGuid(),
            VehicleId = vehicle.Id, RiderProfileId = rider.Id, RiderVehicleAssignmentId = assignment.Id,
            Quantity = 4, TotalCost = 40, UsedAtUtc = now.AddDays(-2), UsedByUserId = actor
        };
        var oilChange = new OilChangeOperation
        {
            VehicleId = vehicle.Id, PerformedAtUtc = now.AddDays(-2), OdometerAtChange = 5000,
            OilInventoryItemId = oilItem.Id, OilMaterialUsageId = oilUsage.Id,
            OilQuantityLiters = 4, OilCost = 40, TotalCost = 40, PerformedByUserId = actor
        };
        var equipment = new InventoryItem
        {
            Sku = "HISTORY-HELMET", NormalizedSku = "HISTORY-HELMET",
            NameAr = "خوذة", NameEn = "Helmet"
        };
        var equipmentIssue = new RiderInventoryIssue
        {
            IssueNumber = "EQ-1", RiderProfileId = rider.Id, IssuedAtUtc = now.AddDays(-3),
            IssuedByUserId = actor, IssuedFromLocationId = Guid.NewGuid(),
            RelatedAssignmentId = assignment.Id
        };
        db.AddRange(employee, rider, vehicle, assignment, issue, evidence, accident,
            oilItem, oilUsage, oilChange, equipment, equipmentIssue,
            new RiderInventoryIssueLine
            {
                RiderInventoryIssueId = equipmentIssue.Id, InventoryItemId = equipment.Id,
                Quantity = 1, ReturnedQuantity = 1, ExpectedReturn = true
            });
        await db.SaveChangesAsync(ct);
        var service = new FleetService(db,
            new FleetServiceSupport(new TestCurrentUser(), new PermitAll(), TimeProvider.System),
            new UnusedFileStorage());

        var vehicleHistory = await service.GetCompleteVehicleHistoryAsync(vehicle.Id, ct);
        var riderHistory = await service.GetCompleteRiderHistoryAsync(rider.Id, ct);

        Assert.True(vehicleHistory.IsSuccess, vehicleHistory.Error.Description);
        Assert.True(riderHistory.IsSuccess, riderHistory.Error.Description);
        foreach (var timeline in new[] { vehicleHistory.Value!, riderHistory.Value! })
        {
            Assert.Equal(timeline.Events.Count, timeline.TotalEvents);
            Assert.Contains(timeline.Events, x => x.Category == "assignment" && x.Action == "handed_over");
            Assert.Contains(timeline.Events, x => x.Category == "assignment" && x.Action == "returned_or_switched");
            Assert.Contains(timeline.Events, x => x.Category == "issue" && x.Files.Any(f => f.FileName == "return.jpg"));
            Assert.Contains(timeline.Events, x => x.Category == "accident" && x.EntityId == accident.Id);
            Assert.Contains(timeline.Events, x => x.Category == "oil_change" && x.Action == "direct");
            Assert.DoesNotContain("private/return.jpg", System.Text.Json.JsonSerializer.Serialize(timeline));
        }
        Assert.Contains(riderHistory.Value!.Events, x => x.Category == "rider_equipment" && x.Action == "issued");
        Assert.Contains(riderHistory.Value.Events, x => x.Category == "rider_equipment" && x.Action == "return_balance_recorded");
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.CreateVersion7();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }

    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }

    private sealed class UnusedFileStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) { }
    }

    private sealed class TestFileStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new StoredPrivateFile($"{relativeDirectory}/{file.OriginalFileName}", file.OriginalFileName,
                file.Length, $"{file.OriginalFileName}-{file.Length}", file.ContentType, file.OriginalFileName)));
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) { }
    }

    private sealed class TestRowVersionInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries<AuditableEntity>()
                         .Where(x => x.State is EntityState.Added or EntityState.Modified))
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            return ValueTask.FromResult(result);
        }
    }
}
