using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task PerformanceDeletionRequiresDedicatedPermissionValidDateSponsorAndReason()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        f.Checker.Granted = PermissionKeys.All.Where(p => p != PermissionKeys.Ninja.PerformanceDelete).ToHashSet();
        Assert.Equal("ninja.forbidden", (await f.Service.DeletePerformanceAsync(date, null, "Correction", ct)).Error.Code);
        f.Checker.Granted = [PermissionKeys.Ninja.PerformanceDelete];
        Assert.Equal("ninja.invalid", (await f.Service.DeletePerformanceAsync(default, null, "Correction", ct)).Error.Code);
        Assert.Equal("ninja.invalid", (await f.Service.DeletePerformanceAsync(date.AddDays(1), null, "Correction", ct)).Error.Code);
        Assert.Equal("ninja.invalid", (await f.Service.DeletePerformanceAsync(date, Guid.Empty, "Correction", ct)).Error.Code);
        Assert.Equal("ninja.invalid", (await f.Service.DeletePerformanceAsync(date, Guid.NewGuid(), "Correction", ct)).Error.Code);
        Assert.Equal("ninja.invalid", (await f.Service.DeletePerformanceAsync(date, null, " ", ct)).Error.Code);
        Assert.Equal("ninja.invalid", (await f.Service.DeletePerformanceAsync(date, null, new string('x', 1001), ct)).Error.Code);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PerformanceDeletionScopesDayAndSponsorPreservesEvidenceAndAllowsSameFileReimport(bool sponsorOnly, bool sql)
    {
        Assert.SkipWhen(sql && Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER") is null,
            "Set NINJA_SQL_TEST_SERVER for disposable SQL Server deletion checks.");
        await using var f = await Fixture.Create(sql, sqlite: !sql);
        var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        f.Account.SponsorId = Sponsor.AlBawabaCommercialEstablishmentId;
        await f.Db.SaveChangesAsync(ct);
        Value(await f.Service.AssignAsync(f.Account.Id,
            new(f.Rider.Id, date, "0501111111", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        var second = new PlatformRiderAccount
        {
            ClientPlatformId = f.Account.ClientPlatformId, RegisteredEmployeeId = f.Account.RegisteredEmployeeId,
            OperatingCityId = f.Account.OperatingCityId, SponsorId = Sponsor.ExpressGateId, Code = "NINJA-SECOND", ExternalAccountId = "272579"
        };
        f.Db.Add(second);
        await f.Db.SaveChangesAsync(ct);
        Value(await f.Service.AssignAsync(second.Id,
            new(f.OwnerRider.Id, date, "0502222222", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        var sheet = Sheet(false, "272578", 12, 8, "272579");
        Assert.Empty(Value(await f.Service.ImportAsync(false, date, "performance.xlsx", sheet, ct)).Issues);
        Assert.Empty(Value(await f.Service.ImportAsync(true, date, "deductions.xlsx", Sheet(true, "272578", 10, -2), ct)).Issues);
        f.Clock.Now = f.Clock.Now.AddDays(1);
        Assert.Empty(Value(await f.Service.ImportAsync(false, date.AddDays(1), "performance.xlsx", sheet, ct)).Issues);
        var imports = await f.Db.NinjaDailyImports.CountAsync(ct);
        var artifacts = await f.Db.Set<NinjaPerformanceArtifact>().CountAsync(ct);
        var expected = sponsorOnly ? 1 : 2;
        var deleted = Value(await f.Service.DeletePerformanceAsync(date,
            sponsorOnly ? f.Account.SponsorId : null, "Wrong assignments", ct));
        Assert.Equal(expected, deleted.PerformanceRowsDeleted);
        Assert.Equal(expected, deleted.SourcesDeleted);
        Assert.Equal(expected, deleted.ProvenanceRowsDeleted);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(2, await f.Db.NinjaDailyPerformance.CountAsync(p => p.ReportDate == date.AddDays(1), ct));
        Assert.Equal(2 - expected, await f.Db.NinjaDailyPerformance.CountAsync(p => p.ReportDate == date, ct));
        Assert.Equal(1, await f.Db.NinjaDailyDeductions.CountAsync(ct));
        Assert.Equal(1, await f.Db.NinjaDailyDeductionSources.CountAsync(ct));
        Assert.Equal(imports, await f.Db.NinjaDailyImports.CountAsync(ct));
        Assert.Equal(artifacts, await f.Db.Set<NinjaPerformanceArtifact>().CountAsync(ct));
        var audit = await f.Db.AuditEntries.SingleAsync(a => a.EntityType == "NinjaPerformanceDay", ct);
        Assert.Equal("Wrong assignments", audit.Reason);
        Assert.Equal(f.User.UserId, audit.ActorUserId);
        var repeated = Value(await f.Service.DeletePerformanceAsync(date,
            sponsorOnly ? Sponsor.AlBawabaCommercialEstablishmentId : null, "Retry", ct));
        Assert.Equal(0, repeated.PerformanceRowsDeleted);

        // Same file restores only deleted rows under the surviving immutable import batch.
        var reimported = Value(await f.Service.ImportAsync(false, date, "performance.xlsx", sheet, ct));
        Assert.Empty(reimported.Issues);
        Assert.Equal(expected, reimported.ImportedRows);
        Assert.Equal(2 - expected, reimported.SkippedRows);
        Assert.Equal(2, await f.Db.NinjaDailyPerformance.CountAsync(p => p.ReportDate == date, ct));
        Assert.Equal(2, await f.Db.Set<NinjaAllocationProvenance>().CountAsync(p =>
            f.Db.NinjaDailyPerformanceSources.Where(s => s.ReportDate == date).Select(s => s.Id).Contains(p.SourceId), ct));
        Assert.Equal(2, Value(await f.Service.ImportAsync(false, date, "performance.xlsx", sheet, ct)).SkippedRows);
        Assert.Equal(imports, await f.Db.NinjaDailyImports.CountAsync(ct));
    }

    [Fact]
    public async Task DeletingBlockingDayAllowsAssignmentDateCorrection()
    {
        await using var f = await Fixture.Create(sqlite: true);
        var ct = TestContext.Current.CancellationToken;
        var assignment = await AssignmentForDateCorrection(f, true);
        Assert.Empty(Value(await f.Service.ImportAsync(false, CorrectionDay, "p.xlsx", Sheet(false, "272578", 10, 8), ct)).Issues);
        var request = new LogisticsERP.Application.Features.Ninja.NinjaAssignmentDatesRequest(
            CorrectionDay.AddDays(1), assignment.EffectiveTo, "Correct attribution", assignment.RowVersion);
        Assert.Equal("ninja.assignment_dates_conflict",
            (await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, request, ct)).Error.Code);
        Value(await f.Service.DeletePerformanceAsync(CorrectionDay, null, "Remove blocking performance", ct));
        f.Db.ChangeTracker.Clear();
        var corrected = Value(await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, request, ct));
        Assert.Equal(CorrectionDay.AddDays(1), corrected.EffectiveFrom);
        var old = await f.Db.RiderClientAssignments.AsNoTracking().SingleAsync(a => a.Id == assignment.Id, ct);
        var replacement = new RiderClientAssignment
        {
            PlatformRiderAccountId = f.Account.Id, ClientContractId = old.ClientContractId,
            RiderProfileId = f.OwnerRider.Id, EffectiveFrom = CorrectionDay, EffectiveTo = CorrectionDay,
            Status = LogisticsERP.Domain.Enums.RiderAssignmentStatus.Ended
        };
        f.Db.Add(replacement);
        await f.Db.SaveChangesAsync(ct);
        Assert.Empty(Value(await f.Service.ImportAsync(false, CorrectionDay, "p.xlsx", Sheet(false, "272578", 10, 8), ct)).Issues);
        var restored = await f.Db.NinjaDailyPerformance.AsNoTracking().SingleAsync(ct);
        Assert.Equal(replacement.Id, restored.RiderClientAssignmentId);
        Assert.Equal(f.OwnerRider.Id, restored.RiderProfileId);
        Assert.Equal(10, restored.TotalOrders);
    }

    [Fact]
    public async Task PerformanceDeletionRollsBackWhenAuditCannotBeSaved()
    {
        var failure = new DeletionAuditFailure();
        await using var f = await Fixture.Create(sqlite: true, saveInterceptor: failure);
        var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        Value(await f.Service.AssignAsync(f.Account.Id,
            new(f.Rider.Id, date, "0501111111", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Assert.Empty(Value(await f.Service.ImportAsync(false, date, "p.xlsx", Sheet(false, "272578", 10, 8), ct)).Issues);
        failure.Enabled = true;
        Assert.Equal("ninja.conflict", (await f.Service.DeletePerformanceAsync(date, null, "Correction", ct)).Error.Code);
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal(1, await f.Db.NinjaDailyPerformance.CountAsync(ct));
        Assert.Equal(1, await f.Db.NinjaDailyPerformanceSources.CountAsync(ct));
        Assert.Equal(1, await f.Db.Set<NinjaAllocationProvenance>().CountAsync(ct));
        Assert.False(await f.Db.AuditEntries.AnyAsync(a => a.EntityType == "NinjaPerformanceDay", ct));
    }

    private sealed class DeletionAuditFailure : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<AuditEntry>()
                .Any(e => e.State == EntityState.Added && e.Entity.EntityType == "NinjaPerformanceDay"))
                throw new DbUpdateException("Injected audit failure.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class DeletionTestVersions : SaveChangesInterceptor
    {
        private long sequence;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
            {
                if (entry.Metadata.FindProperty("RowVersion") is not null)
                    entry.Property("RowVersion").CurrentValue = Guid.NewGuid().ToByteArray();
                if (entry.Entity is AuditEntry audit && entry.State == EntityState.Added) audit.Sequence = ++sequence;
            }
            return ValueTask.FromResult(result);
        }
    }
}
