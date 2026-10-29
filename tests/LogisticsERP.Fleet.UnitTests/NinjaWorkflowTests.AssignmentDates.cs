using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    private static readonly DateOnly CorrectionDay = new(2026, 10, 12);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AssignmentDateCorrectionSupportsCurrentAndEndedAssignmentsWithOnlyItsOwnPermission(bool sql)
    {
        Assert.SkipWhen(sql && Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER") is null,
            "Set NINJA_SQL_TEST_SERVER to run isolated SQL Server date-correction checks.");
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(sql);
        var assignment = await AssignmentForDateCorrection(f, false);
        var accountStatus = f.Account.Status; var credentials = await f.Db.PlatformAccountCredentialVersions.CountAsync(ct);
        f.Checker.Granted = [PermissionKeys.Ninja.AssignmentDatesUpdate];
        var request = new NinjaAssignmentDatesRequest(CorrectionDay.AddDays(-1), null, "Correct current start", assignment.RowVersion);
        var changed = Value(await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, request, ct));
        Assert.Equal(request.EffectiveFrom, changed.EffectiveFrom); Assert.Null(changed.EffectiveTo);
        Assert.Equal("Active", changed.Status); Assert.Equal(f.Account.Id, changed.AccountId);
        Assert.Equal(accountStatus, f.Account.Status); Assert.Equal(credentials, await f.Db.PlatformAccountCredentialVersions.CountAsync(ct));
        Assert.Equal(2, await f.Db.RiderAssignmentEvents.CountAsync(ct));
        var audit = await f.Db.RiderAssignmentEvents.SingleAsync(e => e.Reason == request.Reason, ct);
        Assert.Equal(f.User.UserId, audit.ActorUserId); Assert.Equal(audit.FromStatus, audit.ToStatus);
        using (var snapshot = JsonDocument.Parse(audit.ChangeSnapshotJson!))
        {
            Assert.Equal("NinjaAssignmentDatesCorrected", snapshot.RootElement.GetProperty("Action").GetString());
            Assert.Equal("2026-10-12", snapshot.RootElement.GetProperty("PreviousEffectiveFrom").GetString());
            Assert.Equal("2026-10-11", snapshot.RootElement.GetProperty("EffectiveFrom").GetString());
        }
        if (sql)
        {
            Assert.NotEqual(assignment.RowVersion, changed.RowVersion);
            await using var other = f.OtherContext();
            Assert.Equal("ninja.conflict", (await f.CreateService(other).UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, request, ct)).Error.Code);
        }
        Value(await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, request with { RowVersion = changed.RowVersion }, ct));
        Assert.Equal(2, await f.Db.RiderAssignmentEvents.CountAsync(ct)); // No-op is idempotent.

        f.Checker.Granted = null;
        var ended = Value(await f.Service.ReturnAsync(f.Account.Id,
            new(CorrectionDay.AddDays(3), "0502222222", "returned", "Returned", changed.RowVersion), ct)).Assignment;
        var endedBy = ended.EndedByUserId; var endReason = ended.EndReason;
        f.Checker.Granted = [PermissionKeys.Ninja.AssignmentDatesUpdate];
        var corrected = Value(await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id,
            new(CorrectionDay.AddDays(-2), CorrectionDay.AddDays(4), "Correct ended period", ended.RowVersion), ct));
        Assert.Equal("Ended", corrected.Status); Assert.Equal(CorrectionDay.AddDays(4), corrected.EffectiveTo);
        var saved = await f.Db.RiderClientAssignments.AsNoTracking().SingleAsync(ct);
        Assert.Equal(endedBy, saved.EndedByUserId); Assert.Equal(endReason, saved.EndReason);
        Assert.Equal(PlatformRiderAccountStatus.Available, f.Account.Status);
    }

    [Fact]
    public async Task AssignmentDateCorrectionRejectsOtherNinjaAndSharedPermissionsAndMismatchedAccounts()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(); var assignment = await AssignmentForDateCorrection(f, false);
        var request = new NinjaAssignmentDatesRequest(CorrectionDay.AddDays(-1), null, "Correct", assignment.RowVersion);
        f.Checker.Granted = PermissionKeys.All.Where(k => k != PermissionKeys.Ninja.AssignmentDatesUpdate).ToHashSet(StringComparer.Ordinal);
        Assert.Equal("ninja.forbidden", (await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, request, ct)).Error.Code);
        Assert.Single(f.Db.RiderAssignmentEvents);
        f.Checker.Granted = [PermissionKeys.Ninja.AssignmentDatesUpdate];
        Assert.Equal("ninja.assignment_not_found", (await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, Guid.NewGuid(), request, ct)).Error.Code);
        var second = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, Code = "SECOND", ExternalAccountId = "second" };
        var platform = new ClientPlatform { Code = "OTHER", NameAr = "Other" };
        var other = new PlatformRiderAccount { ClientPlatformId = platform.Id, Code = "OTHER", ExternalAccountId = "other" };
        f.Db.AddRange(second, platform, other); await f.Db.SaveChangesAsync(ct);
        Assert.Equal("ninja.assignment_not_found", (await f.Service.UpdateAssignmentDatesAsync(second.Id, assignment.Id, request, ct)).Error.Code);
        Assert.Equal("ninja.not_found", (await f.Service.UpdateAssignmentDatesAsync(other.Id, assignment.Id, request, ct)).Error.Code);
        Assert.Equal("ninja.not_found", (await f.Service.UpdateAssignmentDatesAsync(Guid.NewGuid(), assignment.Id, request, ct)).Error.Code);
        Assert.Equal(CorrectionDay, (await f.Db.RiderClientAssignments.SingleAsync(ct)).EffectiveFrom);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AssignmentDateCorrectionRejectsInvalidDatesStatusChangesAndStaleTokensWithoutStagingWrites(bool ended)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(); var assignment = await AssignmentForDateCorrection(f, ended);
        f.Checker.Granted = [PermissionKeys.Ninja.AssignmentDatesUpdate];
        var valid = new NinjaAssignmentDatesRequest(CorrectionDay.AddDays(-1), assignment.EffectiveTo, "Correct dates", assignment.RowVersion);
        var invalid = new[]
        {
            valid with { EffectiveFrom = default }, valid with { EffectiveFrom = CorrectionDay.AddDays(6) },
            valid with { EffectiveTo = CorrectionDay.AddDays(6) }, valid with { Reason = " " },
            valid with { Reason = new string('x', 1001) },
            valid with { EffectiveTo = ended ? null : CorrectionDay },
            valid with { EffectiveTo = CorrectionDay.AddDays(-2) }
        };
        foreach (var request in invalid)
            Assert.Equal("ninja.invalid", (await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, request, ct)).Error.Code);
        foreach (var token in new[] { "", "not-base64", "AQI=" })
            Assert.Equal("ninja.conflict", (await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, valid with { RowVersion = token }, ct)).Error.Code);
        var saved = await f.Db.RiderClientAssignments.SingleAsync(ct);
        foreach (var status in new[] { RiderAssignmentStatus.Cancelled, RiderAssignmentStatus.Planned })
        {
            saved.Status = status; await f.Db.SaveChangesAsync(ct);
            Assert.Equal("ninja.invalid", (await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id, valid, ct)).Error.Code);
        }
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal(assignment.EffectiveFrom, saved.EffectiveFrom); Assert.Equal(assignment.EffectiveTo, saved.EffectiveTo);
        Assert.DoesNotContain(f.Db.RiderAssignmentEvents, e => e.Reason == valid.Reason);
    }

    [Theory]
    [InlineData("performance")]
    [InlineData("deductions")]
    [InlineData("performance-source")]
    [InlineData("deduction-source")]
    [InlineData("usage")]
    [InlineData("responsibility")]
    public async Task AssignmentDateCorrectionProtectsRecordedFiguresSourcesAndReviewedResponsibility(string evidence)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(); var assignment = await AssignmentForDateCorrection(f, true);
        switch (evidence)
        {
            case "performance":
                Assert.Empty(Value(await f.Service.ImportAsync(false, CorrectionDay, "p.xlsx", Sheet(false, "272578", 10, 5), ct)).Issues);
                break;
            case "deductions":
                Assert.Empty(Value(await f.Service.ImportAsync(true, CorrectionDay, "d.xlsx", Sheet(true, "272578", 100, -5), ct)).Issues);
                break;
            case "performance-source":
                f.Db.NinjaDailyPerformanceSources.Add(new() { PlatformRiderAccountId = f.Account.Id, ReportDate = CorrectionDay.AddDays(-1), TotalOrders = 10 });
                break;
            case "deduction-source":
                f.Db.NinjaDailyDeductionSources.Add(new() { PlatformRiderAccountId = f.Account.Id, ReportDate = CorrectionDay.AddDays(-1), TotalDeductions = -5 });
                break;
            case "usage":
                (await f.Db.PlatformAccountDayUsages.SingleAsync(u => u.BusinessDate == CorrectionDay, ct)).OrdersCount = 0;
                break;
            case "responsibility":
                var revision = new NinjaResponsibilityRevision { PlatformRiderAccountId = f.Account.Id, Month = new(2026, 10, 1), Revision = 1, Reason = "Reviewed" };
                f.Db.AddRange(revision, new NinjaResponsibilityLine { RevisionId = revision.Id, RiderClientAssignmentId = assignment.Id, BusinessDate = CorrectionDay, Weight = 1 });
                break;
        }
        await f.Db.SaveChangesAsync(ct);
        f.Checker.Granted = [PermissionKeys.Ninja.AssignmentDatesUpdate];
        var requestedStart = evidence.EndsWith("-source", StringComparison.Ordinal) ? CorrectionDay.AddDays(-1) : CorrectionDay.AddDays(1);
        var result = await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id,
            new(requestedStart, assignment.EffectiveTo, "Correct dates", assignment.RowVersion), ct);
        Assert.Equal("ninja.assignment_dates_conflict", result.Error.Code);
        await f.Db.SaveChangesAsync(ct); // A rejected operation cannot leak pending mutations into a later save.
        var saved = await f.Db.RiderClientAssignments.AsNoTracking().SingleAsync(ct);
        Assert.Equal(assignment.EffectiveFrom, saved.EffectiveFrom); Assert.Equal(assignment.EffectiveTo, saved.EffectiveTo);
        Assert.DoesNotContain(f.Db.RiderAssignmentEvents, e => e.Reason == "Correct dates");
    }

    [Fact]
    public async Task AssignmentDateCorrectionAllowsSafeNarrowingAndRetiresOnlyEmptyExcludedMarkers()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(); var assignment = await AssignmentForDateCorrection(f, true);
        Assert.Empty(Value(await f.Service.ImportAsync(false, CorrectionDay.AddDays(1), "p.xlsx", Sheet(false, "272578", 10, 5), ct)).Issues);
        var original = await f.Db.NinjaDailyPerformance.AsNoTracking().SingleAsync(ct);
        f.Checker.Granted = [PermissionKeys.Ninja.AssignmentDatesUpdate];
        Value(await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, assignment.Id,
            new(CorrectionDay.AddDays(1), CorrectionDay.AddDays(2), "Correct empty boundaries", assignment.RowVersion), ct));
        var markers = await f.Db.PlatformAccountDayUsages.IgnoreQueryFilters().ToArrayAsync(ct);
        Assert.Equal(2, markers.Count(u => u.IsDeleted));
        Assert.All(markers.Where(u => u.IsDeleted), u => Assert.Null(u.Sequence));
        Assert.Equal(2, markers.Count(u => !u.IsDeleted));
        var performance = await f.Db.NinjaDailyPerformance.AsNoTracking().SingleAsync(ct);
        Assert.Equal(original.Id, performance.Id); Assert.Equal(original.TotalOrders, performance.TotalOrders);
        Assert.Equal(original.RiderClientAssignmentId, performance.RiderClientAssignmentId);
    }

    [Fact]
    public async Task AssignmentDateCorrectionRejectsLongOverlapButKeepsValidSharedBoundaryDays()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(); var ended = await AssignmentForDateCorrection(f, true);
        var current = Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, CorrectionDay.AddDays(4), "0503333333",
            WasBackdated: true, BackdatedReason: "History", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct)).Assignment;
        f.Checker.Granted = [PermissionKeys.Ninja.AssignmentDatesUpdate];
        Assert.Equal("ninja.assignment_dates_conflict", (await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, ended.Id,
            new(ended.EffectiveFrom, CorrectionDay.AddDays(5), "Overlap", ended.RowVersion), ct)).Error.Code);
        Value(await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, ended.Id,
            new(ended.EffectiveFrom, CorrectionDay.AddDays(4), "Share boundary", ended.RowVersion), ct));
        var ordered = Value(await new PlatformAccountDayAllocationService(f.Db).Ordered(f.Account.Id, CorrectionDay.AddDays(4), ct));
        Assert.Equal(new[] { ended.Id, current.Id }, ordered.Select(a => a.Id));
        Assert.Equal("ninja.assignment_dates_conflict", (await f.Service.UpdateAssignmentDatesAsync(f.Account.Id, current.Id,
            new(CorrectionDay.AddDays(3), null, "Overlap current", current.RowVersion), ct)).Error.Code);
        Assert.Equal(PlatformRiderAccountStatus.Assigned, f.Account.Status);
    }

    private static async Task<SimplePlatformAssignmentResponse> AssignmentForDateCorrection(Fixture f, bool ended)
    {
        var ct = TestContext.Current.CancellationToken;
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id,
            new(f.Rider.Id, CorrectionDay, "0501111111", "first", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct)).Assignment;
        f.Clock.Now = f.Clock.Now.AddDays(5);
        return ended ? Value(await f.Service.ReturnAsync(f.Account.Id,
            new(CorrectionDay.AddDays(3), "0502222222", "returned", "Returned", assigned.RowVersion), ct)).Assignment : assigned;
    }
}
