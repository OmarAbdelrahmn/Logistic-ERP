using System.Text.Json;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Chefz;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Theory]
    [InlineData("CHEFZ", false)]
    [InlineData("THECHEFZ", true)]
    [InlineData("CHEFSZ", false)]
    [InlineData("SHIFTZ", true)]
    public async Task AssignmentStartDateUpdatesOnlyTheDateAndRecordsHistory(string code, bool ended)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        (await f.Db.ClientPlatforms.SingleAsync(ct)).Code = code;
        var assignment = ended ? f.First : f.Second;
        assignment.RowVersion = [1];
        assignment.StartReason = "Original handover";
        assignment.OperationalAgreementNotes = "Original notes";
        await f.Db.SaveChangesAsync(ct);
        var before = JsonSerializer.SerializeToElement(assignment);
        var newDate = ended ? new DateOnly(2026, 9, 29) : new DateOnly(2026, 10, 18);

        var response = Value(await f.Service.UpdateAssignmentStartDateAsync(assignment.Id, new(newDate, "AQ=="), ct));
        Assert.Equal(newDate, response.EffectiveFrom);
        Assert.Equal(assignment.Id, response.AssignmentId);
        Assert.Equal(f.Account.Id, response.AccountId);
        Assert.Equal(assignment.RiderProfileId, response.RiderProfileId);
        var persisted = await f.Db.RiderClientAssignments.AsNoTracking().SingleAsync(a => a.Id == assignment.Id, ct);
        var after = JsonSerializer.SerializeToElement(persisted);
        foreach (var field in before.EnumerateObject().Where(p => p.Name is not ("EffectiveFrom" or "UpdatedAtUtc" or "UpdatedByUserId")))
            Assert.Equal(field.Value.GetRawText(), after.GetProperty(field.Name).GetRawText());
        var history = Assert.Single(await f.Db.RiderAssignmentEvents.ToArrayAsync(ct));
        Assert.Equal(assignment.Id, history.RiderClientAssignmentId);
        Assert.Equal(assignment.Status, history.FromStatus);
        Assert.Equal(assignment.Status, history.ToStatus);
        Assert.Contains("PreviousEffectiveFrom", history.ChangeSnapshotJson!, StringComparison.Ordinal);
        Assert.Contains(await f.Db.AuditEntries.AsNoTracking().ToArrayAsync(ct), a => a.EntityId == assignment.Id
            && a.Action == "Updated" && a.ActorUserId is not null);
        // Repeating an unchanged date does not append another history event.
        Value(await f.Service.UpdateAssignmentStartDateAsync(assignment.Id, new(newDate, "AQ=="), ct));
        Assert.Equal(1, await f.Db.RiderAssignmentEvents.CountAsync(ct));
    }

    [Fact]
    public async Task AssignmentStartDateRejectsInvalidRangesAndOverlapsWithoutChanges()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        f.First.RowVersion = [1]; f.Second.RowVersion = [1];
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal("Chefz.Invalid", (await f.Service.UpdateAssignmentStartDateAsync(f.First.Id, new(default, "AQ=="), ct)).Error.Code);
        Assert.Equal("Chefz.Invalid", (await f.Service.UpdateAssignmentStartDateAsync(f.First.Id, new(new(2026, 10, 16), "AQ=="), ct)).Error.Code);
        // FullTime accounts retain exclusive inclusive calendar days.
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("FullTime"), ct));
        Assert.Equal("Chefz.Conflict", (await f.Service.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 15), "AQ=="), ct)).Error.Code);
        f.Second.Status = RiderAssignmentStatus.Cancelled;
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal("Chefz.Conflict", (await f.Service.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 17), "AQ=="), ct)).Error.Code);
        Assert.Equal(new DateOnly(2026, 10, 1), f.First.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 10, 16), f.Second.EffectiveFrom);
        Assert.Empty(await f.Db.RiderAssignmentEvents.ToArrayAsync(ct));
        Assert.Equal(0, await f.Db.SaveChangesAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AssignmentStartDateCannotExcludeRecordedPerformanceOrSettlement(bool settlement)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        f.Second.RowVersion = [1];
        if (settlement)
            f.Db.ChefzWalletSettlements.Add(new ChefzWalletSettlement { PlatformRiderAccountId = f.Account.Id,
                RiderClientAssignmentId = f.Second.Id, RiderProfileId = f.Second.RiderProfileId,
                ChargedFrom = new(2026, 10, 16), ChargedTo = new(2026, 10, 31) });
        else
            f.Db.ChefzDailyPerformance.Add(new ChefzDailyPerformance { PlatformRiderAccountId = f.Account.Id,
                RiderClientAssignmentId = f.Second.Id, RiderProfileId = f.Second.RiderProfileId, ReportDate = new(2026, 10, 16) });
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal("Chefz.Conflict", (await f.Service.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 17), "AQ=="), ct)).Error.Code);
        Assert.Equal(new DateOnly(2026, 10, 16), f.Second.EffectiveFrom);
        Assert.Empty(await f.Db.RiderAssignmentEvents.ToArrayAsync(ct));
        Assert.Equal(0, await f.Db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task AssignmentStartDateEnforcesPermissionChefzScopeAndAssignmentRowVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        f.Second.RowVersion = [1];
        await f.Db.SaveChangesAsync(ct);
        f.Checker.Allow = false;
        Assert.Equal("Chefz.Forbidden", (await f.Service.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 17), "AQ=="), ct)).Error.Code);
        f.Checker.Allow = true;
        foreach (var token in new[] { "Ag==", "bad", "", null })
            Assert.Equal("Chefz.Conflict", (await f.Service.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 17), token!), ct)).Error.Code);
        Assert.Equal("Chefz.NotFound", (await f.Service.UpdateAssignmentStartDateAsync(Guid.NewGuid(), new(new(2026, 10, 17), "AQ=="), ct)).Error.Code);
        (await f.Db.ClientPlatforms.SingleAsync(ct)).Code = "OTHER";
        // Keep a Chefz platform for permission resolution, but the requested assignment belongs to another platform.
        f.Db.ClientPlatforms.Add(new() { Code = "CHEFZ", NameAr = "شيفز", NameEn = "Chefz" });
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal("Chefz.NotFound", (await f.Service.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 17), "AQ=="), ct)).Error.Code);
        Assert.Equal(new DateOnly(2026, 10, 16), f.Second.EffectiveFrom);
        Assert.Empty(await f.Db.RiderAssignmentEvents.ToArrayAsync(ct));
    }

    [Fact]
    public async Task AssignmentStartDateSqlConcurrencyConflictKeepsWinnerAndRollsBackHistory()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1", "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(true);
        await using var competing = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(f.Db.Database.GetConnectionString()).Options);
        var staleAssignment = await competing.RiderClientAssignments.SingleAsync(a => a.Id == f.Second.Id, ct);
        var staleVersion = Convert.ToBase64String(staleAssignment.RowVersion);
        var winner = Value(await f.Service.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 17), staleVersion), ct));
        Assert.NotEqual(staleVersion, winner.RowVersion);
        var staleService = new ChefzService(competing, new CurrentUser(), new Checker(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        var result = await staleService.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 18), staleVersion), ct);
        Assert.Equal("Chefz.Conflict", result.Error.Code);
        Assert.Empty(competing.ChangeTracker.Entries());
        Assert.Equal(0, await competing.SaveChangesAsync(ct));
        Assert.Equal(new DateOnly(2026, 10, 17), (await competing.RiderClientAssignments.AsNoTracking().SingleAsync(a => a.Id == f.Second.Id, ct)).EffectiveFrom);
        Assert.Single(await competing.RiderAssignmentEvents.AsNoTracking().ToArrayAsync(ct));
    }
}
