using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hunger;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HungerQualityServiceTests
{
    [Fact]
    public async Task LateImportDuplicateAndLatestSnapshotPreserveAccountMonth()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await f.Import(1, 20, "C"); await f.Import(3, 72, "A");
        var late = await f.Import(2, 45, "B"); Assert.Equal(1, late.AdjustedNextRecords);
        var duplicate = await f.Import(2, 99, "Z"); Assert.Equal(1, duplicate.SkippedRows); Assert.Equal(0, duplicate.ImportedRows);
        var report = Value(await f.Service.ReportAsync(new(2026, 11, 1), f.Account.Id, null, 1, 100, ct));
        Assert.Equal(new long[] { 20, 25, 27 }, report.Records.Select(r => r.Daily.CompletedOrders));
        Assert.Equal(72, report.Totals.CompletedOrders); Assert.Equal("A", Assert.Single(report.LatestSnapshots).Segment);
        Assert.Single(await f.Db.Set<HungerQualityRevision>().ToArrayAsync(ct));
    }
    [Fact]
    public async Task EarlierMonthHasNoEffectOnNextMonthAndValidRowsSurviveRowErrors()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var october = Value(await f.Service.PreviewAsync("quality.xlsx", File(100, "C"), new(2026, 10, 31), ct));
        Value(await f.Service.ConfirmAsync(october.PreviewId, ct));
        var preview = Value(await f.Service.PreviewAsync("quality.xlsx", File(20, "B", "missing"), new(2026, 11, 1), ct));
        var result = Value(await f.Service.ConfirmAsync(preview.PreviewId, ct));
        Assert.Equal(2, result.ProcessedRows); Assert.Equal(1, result.ImportedRows); Assert.Equal(1, result.RejectedRows);
        var report = Value(await f.Service.ReportAsync(new(2026, 11, 1), null, null, 1, 100, ct));
        Assert.Equal(20, report.Totals.CompletedOrders);
    }
    [Fact]
    public async Task ChangedBaselineRequiresNewPreviewAndReplayDoesNotDuplicate()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var preview = Value(await f.Service.PreviewAsync("quality.xlsx", File(45, "B"), new(2026, 11, 2), ct));
        await f.Import(1, 20, "C");
        var stale = await f.Service.ConfirmAsync(preview.PreviewId, ct); Assert.True(stale.IsFailure);
        var fresh = Value(await f.Service.PreviewAsync("quality.xlsx", File(45, "B"), new(2026, 11, 2), ct));
        var once = Value(await f.Service.ConfirmAsync(fresh.PreviewId, ct));
        var twice = Value(await f.Service.ConfirmAsync(fresh.PreviewId, ct));
        Assert.Equal(once.ImportId, twice.ImportId); Assert.Equal(2, await f.Db.Set<HungerQualitySnapshot>().CountAsync(ct));
    }
    [Fact]
    public async Task PermissionLossPreventsConfirmAndReport()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var preview = Value(await f.Service.PreviewAsync("quality.xlsx", File(20, "A"), new(2026, 11, 1), ct));
        f.Checker.Allowed = false;
        Assert.True((await f.Service.ConfirmAsync(preview.PreviewId, ct)).IsFailure);
        Assert.True((await f.Service.ReportAsync(new(2026, 11, 1), null, null, 1, 100, ct)).IsFailure);
        Assert.Empty(await f.Db.Set<HungerQualitySnapshot>().ToArrayAsync(ct));
    }
    [Fact]
    public async Task SqlServerLateImportAndUniqueKeyAreAtomic()
    {
        var server = Environment.GetEnvironmentVariable("HUNGER_SQL_TEST_SERVER");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(server), "Set HUNGER_SQL_TEST_SERVER to an isolated local test server.");
        Assert.DoesNotContain("databaseasp", server, StringComparison.OrdinalIgnoreCase);
        await using var f = await Fixture.Create(server); var ct = TestContext.Current.CancellationToken;
        await f.Import(1, 20, "C"); await f.Import(3, 72, "A"); await f.Import(2, 45, "B");
        var report = Value(await f.Service.ReportAsync(new(2026, 11, 1), null, null, 1, 100, ct));
        Assert.Equal(72, report.Totals.CompletedOrders); Assert.Equal(new long[] { 20, 25, 27 }, report.Records.Select(r => r.Daily.CompletedOrders));
        var duplicate = await f.Import(2, 45, "B"); Assert.Equal(1, duplicate.SkippedRows);
    }
    private static T Value<T>(Result<T> result) { Assert.True(result.IsSuccess, result.Error.Description); return result.Value!; }
    private static byte[] File(int orders, string segment, string? extra = null)
    {
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Quality");
        for (var i = 0; i < HungerQualityParser.Headers.Length; i++) sheet.Cell(1, i + 1).Value = HungerQualityParser.Headers[i];
        sheet.Cell(2, 1).Value = "test-account";
        for (var i = 2; i <= 7; i++) sheet.Cell(2, i).Value = orders;
        sheet.Cell(2, 8).Value = segment;
        for (var i = 9; i <= 11; i++) sheet.Cell(2, i).Value = 0.95;
        if (extra is not null) { sheet.Row(2).CopyTo(sheet.Row(3)); sheet.Cell(3, 1).Value = extra; }
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }
    private sealed class User : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid(); public Guid? SessionId => null;
        public long? AuthorizationVersion => 1; public string? CorrelationId => null;
    }
    private sealed class Checker : IPermissionChecker
    {
        public bool Allowed { get; set; } = true;
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(Allowed);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    private sealed class Clock : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero); }
    private sealed class Fixture : IAsyncDisposable
    {
        internal required ApplicationDbContext Db { get; init; }
        internal required HungerQualityService Service { get; init; }
        internal required PlatformRiderAccount Account { get; init; }
        internal required Checker Checker { get; init; }
        private bool Sql { get; init; }
        internal static async Task<Fixture> Create(string? server = null)
        {
            var ct = TestContext.Current.CancellationToken; var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (server is null) options.UseInMemoryDatabase($"hunger-{Guid.NewGuid():N}", m => m.EnableNullChecks(false));
            else options.UseSqlServer($"Server={server};Database=HungerApiTest_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True");
            var db = new ApplicationDbContext(options.Options); if (server is not null) await db.Database.EnsureCreatedAsync(ct);
            var platform = new ClientPlatform { Code = "HUNGER", NameAr = "Test", NameEn = "Test" };
            var city = new GlobalCity { Code = "HSTEST", NameAr = "Test", NameEn = "Test" };
            var operating = new OperatingCity { GlobalCityId = city.Id, EnabledFrom = new(2026, 1, 1) };
            var account = new PlatformRiderAccount { ClientPlatformId = platform.Id, OperatingCityId = operating.Id, Code = "HS_TEST", ExternalAccountId = "test-account" };
            db.AddRange(platform, city, operating, account); await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
            var checker = new Checker(); var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Features:Hunger"] = "true" }).Build();
            return new() { Db = db, Account = account, Checker = checker, Service = new(db, new User(), checker, new Clock(), config), Sql = server is not null };
        }
        internal async Task<HungerImportResult> Import(int day, int orders, string segment)
        {
            var ct = TestContext.Current.CancellationToken;
            var preview = Value(await Service.PreviewAsync("quality.xlsx", File(orders, segment), new(2026, 11, day), ct));
            return Value(await Service.ConfirmAsync(preview.PreviewId, ct));
        }
        public async ValueTask DisposeAsync()
        {
            if (Sql) await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
        }
    }
}
