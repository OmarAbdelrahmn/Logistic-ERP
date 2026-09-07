using System.Text;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Files;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using LogisticsERP.Domain.Common;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class AccidentWorkflowSqlTests
{
    // Opt-in: creates and drops only a uniquely named LocalDB test database; never uses application configuration.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FullWorkflowsPersistDocumentsTimersRefundsAndVehicleState(bool useSql)
    {
        Assert.SkipUnless(!useSql || OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("LOGISTICS_ACCIDENT_SQL_TESTS") == "1",
            "Set LOGISTICS_ACCIDENT_SQL_TESTS=1 on Windows with SQL LocalDB to run the isolated SQL integration test.");
        var database = $"LogisticsAccidentTest_{Guid.NewGuid():N}";
        var root = Path.Combine(Path.GetTempPath(), database);
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        if (useSql) optionsBuilder.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("__ApplicationMigrationsHistory", "migration"));
        // Seeded SQL-generated rowversion fields have no in-memory default generator.
        else optionsBuilder.UseInMemoryDatabase(database, x => x.EnableNullChecks(false)).AddInterceptors(new TestRowVersionInterceptor());
        var options = optionsBuilder.Options;
        await using var db = new ApplicationDbContext(options);
        var ct = TestContext.Current.CancellationToken;
        try
        {
            if (useSql)
            {
                await db.Database.MigrateAsync(ct);
                Assert.False(db.Database.HasPendingModelChanges());
            }
            else await db.Database.EnsureCreatedAsync(ct);
            var fixture = new Scenario(db, root);
            await fixture.SeedCatalogAsync();

            // 100% minor: requires multiple photos and a damage promissory note; no insurance fee.
            var minor = await fixture.CreateAsync(VehicleAccidentSeverity.Minor);
            await fixture.AssessAsync(minor, 100);
            var stale = (await fixture.GetAsync(minor)).RowVersion;
            var invalid = await fixture.ActAsync(minor, AccidentWorkflowAction.StartLocalRepair, amount: 700);
            Assert.True(invalid.IsFailure);
            await fixture.UploadAsync(minor, VehicleAccidentEvidenceType.DamagePhoto);
            await fixture.UploadAsync(minor, VehicleAccidentEvidenceType.DamagePhoto);
            var note = await fixture.UploadAsync(minor, VehicleAccidentEvidenceType.DamagePromissoryNote);
            await fixture.SuccessAsync(minor, AccidentWorkflowAction.StartLocalRepair, note, 700);
            var staleResult = await fixture.Service.ExecuteWorkflowAsync(minor,
                new(AccidentWorkflowAction.FollowUp, stale, "stale command", fixture.ActionAt), ct);
            Assert.True(staleResult.IsFailure);
            var done = await fixture.UploadAsync(minor, VehicleAccidentEvidenceType.RepairCompletion);
            await fixture.SuccessAsync(minor, AccidentWorkflowAction.CompleteLocalRepair, done);
            var minorState = await fixture.GetAsync(minor);
            Assert.Equal(AccidentCaseStage.Completed, minorState.Stage);
            Assert.Null(minorState.Fault.OpeningFeeAmount);
            var minorAccident = await db.VehicleAccidents.SingleAsync(x => x.Id == minor, ct);
            Assert.Equal(VehicleOperationalStatus.Available, (await db.Vehicles.SingleAsync(x => x.Id == minorAccident.VehicleId, ct)).CurrentOperationalStatus);

            // 100% serious: fee is mandatory; requested Repair may receive Compensation instead.
            var compensation = await fixture.CreateAsync(VehicleAccidentSeverity.Serious);
            await fixture.AssessAsync(compensation, 100);
            Assert.True((await fixture.ActAsync(compensation, AccidentWorkflowAction.OpenClaim, claimType: AccidentClaimType.Repair)).IsFailure);
            var fee = await fixture.UploadAsync(compensation, VehicleAccidentEvidenceType.ClaimOpeningFeeReceipt);
            Assert.True((await fixture.ActAsync(compensation, AccidentWorkflowAction.OpenClaim, fee, 2499, claimType: AccidentClaimType.Repair)).IsFailure);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.OpenClaim, fee, 2500, claimType: AccidentClaimType.Repair);
            var submitted = await fixture.UploadAsync(compensation, VehicleAccidentEvidenceType.ClaimSubmissionReport);
            Assert.True((await fixture.ActAsync(compensation, AccidentWorkflowAction.SubmitClaim, submitted, reference: "CL-1")).IsFailure);
            await fixture.SeedSourceDocumentsAsync(compensation);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.SubmitClaim, submitted, reference: "CL-1");
            var source = await fixture.Service.DownloadSourceDocumentAsync(compensation, "iqama", ct);
            Assert.True(source.IsSuccess); await source.Value!.Content.DisposeAsync();
            var offer = await fixture.UploadAsync(compensation, VehicleAccidentEvidenceType.AssessmentReceipt);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.ReceiveCompensationOffer, offer, 20000);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.SubmitToInsurance);
            var pending = await fixture.GetAsync(compensation);
            Assert.Equal(pending.Settlement.InsuranceSubmittedAtUtc!.Value.AddDays(15), pending.DeadlineAtUtc);
            var reject = await fixture.UploadAsync(compensation, VehicleAccidentEvidenceType.InsuranceDecision);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.RejectInsurance, reject);
            fixture.ActionAt = fixture.ActionAt.AddDays(1);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.SubmitToInsurance);
            Assert.NotEqual(pending.DeadlineAtUtc, (await fixture.GetAsync(compensation)).DeadlineAtUtc);
            var approved = await fixture.UploadAsync(compensation, VehicleAccidentEvidenceType.PaymentReceipt);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.ApproveInsurance, approved);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.SubmitToSupplier, submitted);
            pending = await fixture.GetAsync(compensation);
            Assert.Equal(pending.Settlement.SupplierSubmittedAtUtc!.Value.AddDays(10), pending.DeadlineAtUtc);
            Assert.True(pending.IsOverdue);
            var transfer = await fixture.UploadAsync(compensation, VehicleAccidentEvidenceType.TransferReceipt);
            Assert.True((await fixture.ActAsync(compensation, AccidentWorkflowAction.ConfirmTransfer, transfer, 1000)).IsFailure);
            await fixture.SuccessAsync(compensation, AccidentWorkflowAction.ConfirmTransfer, transfer, 20000);
            Assert.Equal(AccidentClaimType.Repair, (await fixture.GetAsync(compensation)).Claim.RequestedType);
            Assert.Equal(AccidentClaimOutcome.Compensation, (await fixture.GetAsync(compensation)).Claim.Outcome);
            await fixture.RefundAsync(compensation);

            // Shared fault enters claim directly; repair completion releases the accident hold.
            var repair = await fixture.CreateAsync(VehicleAccidentSeverity.Serious);
            await fixture.AssessAsync(repair, 75);
            await fixture.OpenAndSubmitAsync(repair);
            var direction = await fixture.UploadAsync(repair, VehicleAccidentEvidenceType.RepairDirection);
            await fixture.SuccessAsync(repair, AccidentWorkflowAction.ReceiveRepairDirection, direction, location: "Riyadh workshop", contact: "0500000000");
            await fixture.SuccessAsync(repair, AccidentWorkflowAction.StartRepair);
            await fixture.SuccessAsync(repair, AccidentWorkflowAction.RepairProgress);
            done = await fixture.UploadAsync(repair, VehicleAccidentEvidenceType.RepairCompletion);
            await fixture.SuccessAsync(repair, AccidentWorkflowAction.CompleteRepair, done);
            Assert.Equal(fixture.ActionAt, (await fixture.GetAsync(repair)).IncidentEndedAtUtc);

            // No rider fault: reinspection, total-loss collection, valuation and supplier payment.
            var loss = await fixture.CreateAsync(VehicleAccidentSeverity.Critical);
            await fixture.AssessAsync(loss, 0);
            await fixture.OpenAndSubmitAsync(loss);
            var assessment = await fixture.UploadAsync(loss, VehicleAccidentEvidenceType.AssessmentReceipt);
            await fixture.SuccessAsync(loss, AccidentWorkflowAction.ProposeTotalLoss, assessment);
            await fixture.SuccessAsync(loss, AccidentWorkflowAction.RequestReinspection, location: "Inspection center", appointment: fixture.ActionAt);
            var confirmation = await fixture.UploadAsync(loss, VehicleAccidentEvidenceType.TotalLossConfirmation);
            await fixture.SuccessAsync(loss, AccidentWorkflowAction.ConfirmTotalLoss, confirmation);
            var collection = await fixture.UploadAsync(loss, VehicleAccidentEvidenceType.VehicleCollectionReceipt);
            await fixture.SuccessAsync(loss, AccidentWorkflowAction.RecordVehicleCollection, collection);
            var incidentEnd = (await fixture.GetAsync(loss)).IncidentEndedAtUtc;
            fixture.ActionAt = fixture.ActionAt.AddDays(1);
            var valuation = await fixture.UploadAsync(loss, VehicleAccidentEvidenceType.ValuationReceipt);
            await fixture.SuccessAsync(loss, AccidentWorkflowAction.RecordValuation, valuation, 30000);
            submitted = await fixture.UploadAsync(loss, VehicleAccidentEvidenceType.ClaimSubmissionReport);
            await fixture.SuccessAsync(loss, AccidentWorkflowAction.SubmitToSupplier, submitted);
            transfer = await fixture.UploadAsync(loss, VehicleAccidentEvidenceType.TransferReceipt);
            await fixture.SuccessAsync(loss, AccidentWorkflowAction.ConfirmTransfer, transfer, 30000);
            Assert.Equal(incidentEnd, (await fixture.GetAsync(loss)).IncidentEndedAtUtc);
            var lossAccident = await db.VehicleAccidents.SingleAsync(x => x.Id == loss, ct);
            var lossVehicle = await db.Vehicles.SingleAsync(x => x.Id == lossAccident.VehicleId, ct);
            Assert.Equal(VehicleOperationalStatus.Decommissioned, lossVehicle.CurrentOperationalStatus);
            Assert.Null(lossVehicle.CurrentAssignmentId);
            Assert.False(await db.RiderVehicleAssignments.AnyAsync(x => x.VehicleId == lossVehicle.Id && x.EndedAtUtc == null, ct));

            // Unbounded towing records, ownership checks and real SQL rowversion conflicts.
            for (var i = 0; i < 7; i++) await fixture.UploadAsync(loss, VehicleAccidentEvidenceType.TowingReceipt);
            Assert.Equal(7, (await fixture.GetAsync(loss)).Attachments.Count(x => x.EvidenceType == VehicleAccidentEvidenceType.TowingReceipt));
            Assert.True((await fixture.Service.DownloadEvidenceAsync(loss, note, ct)).IsFailure);
            await using var competing = new ApplicationDbContext(options);
            var other = await competing.VehicleAccidentCases.SingleAsync(x => x.VehicleAccidentId == loss, ct);
            await fixture.SuccessAsync(loss, AccidentWorkflowAction.FollowUp);
            other.DamageAssessment = "Concurrent modification";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => competing.SaveChangesAsync(ct));
            Assert.True(fixture.Notifications.Queued > 20);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync(CancellationToken.None);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private sealed class Scenario(ApplicationDbContext db, string root)
    {
        private readonly TestUser user = new();
        private readonly PrivateFileStorage storage = new(new TestHostEnvironment(root));
        private readonly TestTime time = new();
        public readonly TestNotifications Notifications = new();
        private VehicleSupplier supplier = null!;
        private VehicleModel model = null!;
        public DateTimeOffset ActionAt { get; set; } = DateTimeOffset.Parse("2026-08-05T10:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture);
        public VehicleAccidentService Service => new(db, new FleetServiceSupport(user, new PermitAll(), time), storage, new DummyPdf(), Notifications);

        public async Task SeedCatalogAsync()
        {
            var manufacturer = new VehicleManufacturer { Code = "TEST", NameAr = "اختبار", NameEn = "Test" };
            model = new VehicleModel { VehicleManufacturerId = manufacturer.Id, Code = "TEST", NameAr = "اختبار", NameEn = "Test" };
            supplier = new VehicleSupplier { Code = "TEST", NameAr = "شركة شراء", NameEn = "Supplier" };
            db.AddRange(manufacturer, model, supplier);
            foreach (var (id, code) in new[] { (DocumentType.ResidencyPermitId, "TEST-IQAMA"), (DocumentType.DriverLicenseId, "TEST-LICENSE") })
                if (!await db.DocumentTypes.AnyAsync(x => x.Id == id)) db.DocumentTypes.Add(new DocumentType { Id = id, Code = code, NameAr = code, NameEn = code });
            await db.SaveChangesAsync();
        }

        public async Task<Guid> CreateAsync(VehicleAccidentSeverity severity)
        {
            var employee = new Employee { FullNameAr = "سائق اختبار" };
            var rider = new RiderProfile { EmployeeId = employee.Id };
            var asset = Guid.NewGuid().ToString("N");
            var vehicle = new Vehicle { AssetNumber = asset, NormalizedAssetNumber = asset, VehicleManufacturerId = model.VehicleManufacturerId, VehicleModelId = model.Id, PurchasedFromSupplierId = supplier.Id };
            var assignment = new RiderVehicleAssignment { VehicleId = vehicle.Id, RiderProfileId = rider.Id, StartedAtUtc = ActionAt.AddDays(-10), AssignmentReason = "Test", AssignedByUserId = user.UserId!.Value };
            vehicle.CurrentAssignmentId = assignment.Id;
            db.AddRange(employee, rider, vehicle, assignment);
            await db.SaveChangesAsync();
            var result = await Service.CreateAsync(new(vehicle.Id, rider.Id, DateTimeOffset.Parse("2026-08-01T10:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture), "Riyadh", null, null,
                "TRAFFIC-" + asset, null, severity, false, false, null, null, "Damage", null, "Accident narrative"), asset);
            Assert.True(result.IsSuccess, result.Error.Description);
            return result.Value!.Summary.Id;
        }

        public async Task<AccidentWorkflowResponse> GetAsync(Guid id)
        {
            var result = await Service.GetWorkflowAsync(id); Assert.True(result.IsSuccess, result.Error.Description); return result.Value!;
        }

        public async Task<Result<AccidentWorkflowResponse>> ActAsync(Guid id, AccidentWorkflowAction action, Guid? attachment = null, decimal? amount = null,
            AccidentClaimType? claimType = null, string? reference = null, string? location = null, string? contact = null, DateTimeOffset? appointment = null)
        {
            var current = await GetAsync(id);
            return await Service.ExecuteWorkflowAsync(id, new(action, current.RowVersion, "Test action notes", ActionAt,
                AttachmentId: attachment, Amount: amount, ClaimType: claimType, Reference: reference, Location: location, Contact: contact, AppointmentAtUtc: appointment));
        }

        public async Task SuccessAsync(Guid id, AccidentWorkflowAction action, Guid? attachment = null, decimal? amount = null,
            AccidentClaimType? claimType = null, string? reference = null, string? location = null, string? contact = null, DateTimeOffset? appointment = null)
        {
            var result = await ActAsync(id, action, attachment, amount, claimType, reference, location, contact, appointment);
            Assert.True(result.IsSuccess, $"{action}: {result.Error.Description}");
        }

        public async Task<Guid> UploadAsync(Guid id, VehicleAccidentEvidenceType type)
        {
            var photo = type == VehicleAccidentEvidenceType.DamagePhoto;
            byte[] bytes = photo ? [0xFF, 0xD8, 0xFF, 0xE0, 0x01] : Encoding.ASCII.GetBytes("%PDF-1.4 test document");
            using var stream = new MemoryStream(bytes);
            var metadata = type == VehicleAccidentEvidenceType.TowingReceipt ? new AccidentAttachmentMetadata("To workshop", "Accident site", "Workshop", ActionAt, 150) : new();
            var result = await Service.UploadWorkflowAttachmentAsync(id, type, metadata, new(stream, photo ? "photo.jpg" : "receipt.pdf", photo ? "image/jpeg" : "application/pdf", bytes.Length));
            Assert.True(result.IsSuccess, result.Error.Description); return result.Value!.Id;
        }

        public async Task AssessAsync(Guid id, decimal rider)
        {
            var attachment = await UploadAsync(id, VehicleAccidentEvidenceType.NajmReport);
            var result = await Service.ExecuteWorkflowAsync(id, new(AccidentWorkflowAction.AssessFault, (await GetAsync(id)).RowVersion, "Najm fault assessment", ActionAt,
                attachment, RiderFaultPercentage: rider, OtherParties: [new("Other driver", 100 - rider)]));
            Assert.True(result.IsSuccess, result.Error.Description);
        }

        public async Task OpenAndSubmitAsync(Guid id)
        {
            await SuccessAsync(id, AccidentWorkflowAction.OpenClaim, claimType: AccidentClaimType.Compensation);
            await SeedSourceDocumentsAsync(id);
            var submission = await UploadAsync(id, VehicleAccidentEvidenceType.ClaimSubmissionReport);
            await SuccessAsync(id, AccidentWorkflowAction.SubmitClaim, submission, reference: "CLAIM-" + id.ToString("N"));
        }

        public async Task SeedSourceDocumentsAsync(Guid id)
        {
            var accident = await db.VehicleAccidents.SingleAsync(x => x.Id == id);
            foreach (var typeId in new[] { DocumentType.ResidencyPermitId, DocumentType.DriverLicenseId })
            {
                var document = new EmployeeDocument { EmployeeId = accident.EmployeeId, DocumentTypeId = typeId };
                db.Add(document); await db.SaveChangesAsync();
                var bytes = Encoding.ASCII.GetBytes("%PDF-1.4 source"); using var stream = new MemoryStream(bytes);
                var stored = (await storage.StoreAsync($"test/{Guid.NewGuid():N}", new(stream, "source.pdf", "application/pdf", bytes.Length), 1000)).Value!;
                var version = new EmployeeDocumentVersion { EmployeeDocumentId = document.Id, VersionNumber = 1, StoragePath = stored.StoragePath,
                    OriginalFileName = stored.OriginalFileName, StoredFileName = stored.StoredFileName, ContentType = stored.ContentType, FileSizeBytes = stored.Length,
                    Sha256Checksum = stored.Sha256Checksum, UploadedAtUtc = ActionAt, UploadedByUserId = user.UserId!.Value };
                db.Add(version); await db.SaveChangesAsync(); document.CurrentVersionId = version.Id; await db.SaveChangesAsync();
            }
            var attachment = new VehicleAttachment { VehicleId = accident.VehicleId, Kind = VehicleFileKind.Istimara, DisplayName = "Registration" };
            db.Add(attachment); await db.SaveChangesAsync();
            var registrationBytes = Encoding.ASCII.GetBytes("%PDF-1.4 registration");
            using var registrationStream = new MemoryStream(registrationBytes);
            var registrationFile = (await storage.StoreAsync($"test/{Guid.NewGuid():N}", new(registrationStream, "registration.pdf", "application/pdf", registrationBytes.Length), 1000)).Value!;
            var registration = new VehicleAttachmentVersion { VehicleAttachmentId = attachment.Id, VersionNumber = 1, OriginalFileName = registrationFile.OriginalFileName, StoredFileName = registrationFile.StoredFileName,
                StoragePath = registrationFile.StoragePath, ContentType = registrationFile.ContentType, FileSizeBytes = registrationFile.Length, Sha256Checksum = registrationFile.Sha256Checksum, UploadedAtUtc = ActionAt, UploadedByUserId = user.UserId!.Value };
            db.Add(registration); await db.SaveChangesAsync(); attachment.CurrentVersionId = registration.Id; await db.SaveChangesAsync();
        }

        public async Task RefundAsync(Guid id)
        {
            var receipt = await UploadAsync(id, VehicleAccidentEvidenceType.InstallmentReceipt);
            var state = await GetAsync(id);
            var result = await Service.AddInstallmentAsync(id, new(state.RowVersion, new(2026, 8, 1), new(2026, 8, 31), new(2026, 8, 2), 1000, 200, receipt, "Partial period eligible amount"));
            Assert.True(result.IsSuccess, result.Error.Description);
            var form = await UploadAsync(id, VehicleAccidentEvidenceType.InstallmentRefundRequest);
            Assert.True((await ActAsync(id, AccidentWorkflowAction.SubmitInstallmentRefund, form, 1000, reference: "REF-1")).IsFailure);
            await SuccessAsync(id, AccidentWorkflowAction.SubmitInstallmentRefund, form, 200, reference: "REF-1");
            var payment = await UploadAsync(id, VehicleAccidentEvidenceType.InstallmentRefundReceipt);
            await SuccessAsync(id, AccidentWorkflowAction.ReceiveInstallmentRefund, payment, 200);
            Assert.Equal(AccidentRefundStatus.Received, (await GetAsync(id)).Refund.Status);
        }
    }

    private sealed class TestUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid(); public Guid? SessionId => null;
        public long? AuthorizationVersion => 1; public string? CorrelationId => "accident-test";
    }
    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    private sealed class TestTime : TimeProvider { public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-09-06T10:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture); }
    private sealed class DummyPdf : IAccidentPdfGenerator { public byte[] Generate(AccidentPdfSnapshot snapshot) => Encoding.ASCII.GetBytes("%PDF-1.4 generated"); }
    private sealed class TestNotifications : IAccidentNotificationService
    {
        public int Queued { get; private set; }
        public Task QueueAsync(Guid accidentId, Guid vehicleId, string accidentNumber, string eventKey, string description, CancellationToken cancellationToken = default) { Queued++; return Task.CompletedTask; }
        public Task RunDueNotificationsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    // In-memory tests simulate SQL rowversions; only the opt-in SQL variant verifies database enforcement.
    private sealed class TestRowVersionInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries<AuditableEntity>().Where(x => x.State is EntityState.Added or EntityState.Modified))
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            return ValueTask.FromResult(result);
        }
    }
}
