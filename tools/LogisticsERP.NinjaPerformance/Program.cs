using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Ninja;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

if (args.Length != 4 || args[0] is not ("inspect" or "rehearse" or "apply" or "verify" or "evaluate"))
    throw new ArgumentException("Usage: inspect|rehearse|apply|verify|evaluate <source.xlsx> <row-mapping.json> <output-directory>");
var mode = args[0]; var sourcePath = Path.GetFullPath(args[1]); var mappingPath = Path.GetFullPath(args[2]); var output = Path.GetFullPath(args[3]);
Directory.CreateDirectory(output);
var originalBytes = await File.ReadAllBytesAsync(sourcePath); var originalHash = Convert.ToHexString(SHA256.HashData(originalBytes));
var parsed = NinjaPerformanceParser.Parse(Path.GetFileName(sourcePath), originalBytes,
    new("MultiDay", HoursUnit: "Duration", DateFormat: "MM/dd/yyyy"), new DateOnly(2026, 10, 26), CancellationToken.None);
if (parsed.Issues.Count != 0) throw new InvalidOperationException(JsonSerializer.Serialize(parsed.Issues));
var mapping = JsonSerializer.Deserialize<Mapping[]>(await File.ReadAllTextAsync(mappingPath), NinjaEvaluationService.Json)!;
var approved = mapping.Where(m => m.Status == "Matched").OrderBy(m => m.Row).ToArray();
if (parsed.Rows.Count != 3072 || approved.Length != 3032 || mapping.Length != 3072
    || originalHash != "A896FD10B2348F506D0E0661934DC32D65ABA0EB9C34813A54131250B3AFCE70")
    throw new InvalidOperationException("The approved source/selection changed.");
var selected = parsed.Rows.Where(r => approved.Any(m => m.Row == r.RowNumber)).OrderBy(r => r.RowNumber).ToArray();
foreach (var row in selected)
{
    var match = approved.Single(m => m.Row == row.RowNumber);
    if (match.CaptainId != row.CaptainId || match.Date != row.Date || match.Orders != row.Orders || decimal.Parse(match.Hours, CultureInfo.InvariantCulture) != row.Hours)
        throw new InvalidOperationException("Source rows no longer match the reviewed selection.");
}
var csv = new StringBuilder("Captain ID,Total Orders,Total Working Hours,Date,Original Source Row,Original Workbook SHA256\n");
foreach (var row in selected) csv.Append(CultureInfo.InvariantCulture, $"{row.CaptainId},{row.Orders},{row.Hours},{row.Date:yyyy-MM-dd},{row.RowNumber},{originalHash}\n");
var bytes = Encoding.UTF8.GetBytes(csv.ToString()); var hash = Convert.ToHexString(SHA256.HashData(bytes));
await File.WriteAllBytesAsync(Path.Combine(output, "approved-performance.csv"), bytes);
if (mode == "inspect")
{
    Console.WriteLine(JsonSerializer.Serialize(new { Mode = mode, Parsed = parsed.Rows.Count, Selected = selected.Length, Held = mapping.Length - selected.Length, SourceHash = originalHash, SelectionHash = hash }));
    return;
}
using var settings = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "src/LogisticsERP.Api/appsettings.json")));
var connectionString = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("LogisticsDatabase").GetString()!;
var connectionInfo = new SqlConnectionStringBuilder(connectionString);
if (connectionInfo.DataSource != "db67927.public.databaseasp.net" || connectionInfo.InitialCatalog != "db67927") throw new InvalidOperationException("Unexpected hosted target.");
Guid actor; long authorizationVersion;
await using (var connection = new SqlConnection(connectionString))
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    // Explicitly authorized maintenance uses the existing human actor for audit attribution;
    // it does not require or change an ERP role assignment (direct-grant users may have no role).
    command.CommandText = "SELECT u.Id,u.AuthorizationVersion FROM [identity].Users u WHERE u.UserName=N'Omar' AND u.IsDeleted=0 AND u.IsDevelopmentOnly=0";
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("The existing non-development Omar actor is required for audit attribution.");
    actor = reader.GetGuid(0); authorizationVersion = reader.GetInt64(1);
}
var actorContext = new ImportActor(actor, authorizationVersion, $"ninja-performance-20261026-{mode}");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
    .AddInterceptors(new ApplicationPersistenceInterceptor(actorContext, TimeProvider.System)).Options;
await using var db = new ApplicationDbContext(options);
var platform = await db.ClientPlatforms.AsNoTracking().Where(p => p.Code == "NINJA").Select(p => p.Id).SingleAsync();
if (mode == "evaluate")
{
    // Exercise legacy report materialization against the real SQL Server provider as
    // well as the evaluation read model. This branch only performs database reads.
    var readPermission = new EvaluationReadPermission(platform);
    var protector = new PlatformCredentialProtector(() => new byte[32]);
    var reports = new NinjaService(db, actorContext, readPermission, TimeProvider.System, protector,
        new SimplePlatformService(db, actorContext, TimeProvider.System, protector));
    var from = new DateOnly(2026, 10, 1); var to = new DateOnly(2026, 10, 26);
    var summary = await reports.GetPerformanceReportAsync(from, to, null, null);
    var daily = await reports.GetDailyReportAsync(from, to, null, null, 1, 50);
    var riders = await reports.GetRiderReportAsync(from, to, null, null, 1, 100);
    if (summary.IsFailure || daily.IsFailure || riders.IsFailure)
        throw new InvalidOperationException("Legacy Ninja report verification failed.");
    var shareOrders = await db.NinjaDailyPerformance.Where(p => p.ReportDate >= from && p.ReportDate <= to
        && db.PlatformRiderAccounts.IgnoreQueryFilters().Any(a => a.Id == p.PlatformRiderAccountId && a.ClientPlatformId == platform))
        .SumAsync(p => (long)p.TotalOrders);
    if (summary.Value!.Metrics.TotalOrders != shareOrders || daily.Value!.Items.Sum(d => d.Metrics.TotalOrders) != shareOrders)
        throw new InvalidOperationException("Legacy summary/daily report totals differ from the performance facts.");
    await File.WriteAllTextAsync(Path.Combine(output, "hosted-legacy-report-verification.json"),
        JsonSerializer.Serialize(new { Summary = summary.Value, DailyRows = daily.Value.Items.Count,
            RiderPageRows = riders.Value!.Items.Count, ShareOrders = shareOrders, DatabaseWrites = 0 }, NinjaEvaluationService.Json));
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Features:NinjaEvaluation"] = "true" }).Build();
    var evaluation = new NinjaEvaluationService(db, actorContext, new EvaluationReadPermission(platform), TimeProvider.System, config);
    var results = new Dictionary<string, object>();
    foreach (var view in new[] { "accounts", "riders" })
    {
        var result = await evaluation.EvaluateAsync(new() { Year = 2026, Month = 10, View = view, PageSize = 100 });
        if (result.IsFailure) throw new InvalidOperationException(result.Error.Code);
        results[view] = result.Value!.Summary;
        await File.WriteAllTextAsync(Path.Combine(output, $"hosted-{view}-report.json"), JsonSerializer.Serialize(result.Value, NinjaEvaluationService.Json));
    }
    var accounts = (NinjaEvaluationSummary)results["accounts"];
    var actualOrders = await db.Set<NinjaDailyPerformanceSource>().Where(s => s.ReportDate >= new DateOnly(2026, 10, 1) && s.ReportDate <= new DateOnly(2026, 10, 26)).SumAsync(s => (long)s.TotalOrders);
    if (accounts.ObservedOrders != actualOrders) throw new InvalidOperationException("Account report double-counted or omitted source orders.");
    await File.WriteAllTextAsync(Path.Combine(output, "hosted-evaluation-verification.json"), JsonSerializer.Serialize(results, NinjaEvaluationService.Json));
    Console.WriteLine(JsonSerializer.Serialize(new { Mode = mode, Accounts = accounts.TotalRows, ObservedOrders = accounts.ObservedOrders,
        SourceOrders = actualOrders, RiderRows = ((NinjaEvaluationSummary)results["riders"]).TotalRows, DatabaseWrites = 0 }));
    return;
}
var ids = await NinjaPerformanceImportEngine.AccountIds(db, [platform], selected, CancellationToken.None);
var rehearsalPath = Path.Combine(output, "rehearsal.json");
await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
{
    db.ChangeTracker.Clear();
    await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, ids, CancellationToken.None);
    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    var beforeSources = await db.Set<NinjaDailyPerformanceSource>().AsNoTracking().OrderBy(s => s.Id).ToArrayAsync();
    var beforeShares = await db.NinjaDailyPerformance.AsNoTracking().OrderBy(s => s.Id).ToArrayAsync();
    var beforeSourceHash = NinjaEvaluationService.Hash(JsonSerializer.Serialize(beforeSources, NinjaEvaluationService.Json));
    var beforeShareHash = NinjaEvaluationService.Hash(JsonSerializer.Serialize(beforeShares, NinjaEvaluationService.Json));
    var plan = await NinjaPerformanceImportEngine.Prepare(db, [platform], selected, "Ninja-October-2026-approved.csv", hash, CancellationToken.None);
    if (!plan.Valid) throw new InvalidOperationException(JsonSerializer.Serialize(plan.Rows.Where(r => r.Issues.Count > 0), NinjaEvaluationService.Json));
    var existing = plan.Results.Sum(r => r.SkippedRows);
    if (mode == "verify" && existing != 3032) throw new InvalidOperationException("Expected all approved rows to be saved identically.");
    if (mode == "apply")
    {
        var rehearsal = JsonSerializer.Deserialize<Rehearsal>(await File.ReadAllTextAsync(rehearsalPath), NinjaEvaluationService.Json)!;
        if (!rehearsal.Passed || rehearsal.SelectionHash != hash || rehearsal.SourceHash != originalHash || rehearsal.InputRevision != plan.InputRevision)
            throw new InvalidOperationException("A successful matching rehearsal is required.");
    }
    NinjaPerformanceConfirmation? confirmation = null;
    if (mode != "verify" && existing != 3032)
    {
        if (existing != 0 || plan.Sources.Count != 3032 || plan.Shares.Count != 3032) throw new InvalidOperationException("Unexpected partial import state.");
        foreach (var share in plan.Shares)
        {
            var map = approved.Single(m => m.Row == share.SourceRowNumber);
            if (map.Assignments.Length != 1 || map.AccountId != share.PlatformRiderAccountId || map.Assignments[0].Id != share.RiderClientAssignmentId || map.Assignments[0].RiderId != share.RiderProfileId)
                throw new InvalidOperationException("Historical assignment changed since review.");
        }
        var originalArtifact = await db.Set<NinjaPerformanceArtifact>().AsNoTracking().FirstOrDefaultAsync(a => a.ContentHash == originalHash && a.OriginalArtifactId == null);
        if (originalArtifact is null)
        {
            originalArtifact = new NinjaPerformanceArtifact { ContentHash = originalHash, Content = originalBytes, FileName = Path.GetFileName(sourcePath), Format = ".xlsx" };
            originalArtifact.ResultJson = JsonSerializer.Serialize(new NinjaPerformanceConfirmation(originalArtifact.Id, 0, 0, []), NinjaEvaluationService.Json);
            db.Add(originalArtifact);
        }
        foreach (var origin in plan.Provenance) origin.EvidenceReference = $"OriginalArtifact:{originalArtifact.Id:D}/Row:{plan.Sources.Single(s => s.Id == origin.SourceId).SourceRowNumber}";
        plan.Stage(db);
        confirmation = await NinjaPerformanceImportService.Artifact(db, plan, null, "Ninja-October-2026-approved.csv", bytes, CancellationToken.None);
        await db.SaveChangesAsync();
    }
    var dates = selected.Select(r => r.Date).Distinct().ToArray();
    var storedSources = await db.Set<NinjaDailyPerformanceSource>().AsNoTracking().Where(s => ids.Contains(s.PlatformRiderAccountId) && dates.Contains(s.ReportDate)).ToArrayAsync();
    var storedShares = await db.NinjaDailyPerformance.AsNoTracking().Where(s => ids.Contains(s.PlatformRiderAccountId) && dates.Contains(s.ReportDate)).ToArrayAsync();
    foreach (var row in selected)
    {
        var map = approved.Single(m => m.Row == row.RowNumber);
        var source = storedSources.Single(s => s.PlatformRiderAccountId == map.AccountId && s.ReportDate == row.Date);
        var fact = storedShares.Single(s => s.SourceId == source.Id);
        if (source.TotalOrders != row.Orders || source.TotalWorkingHours != row.Hours || fact.TotalOrders != row.Orders || fact.TotalWorkingHours != row.Hours
            || fact.RiderClientAssignmentId != map.Assignments[0].Id || fact.RiderProfileId != map.Assignments[0].RiderId || fact.SourceRowNumber != row.RowNumber)
            throw new InvalidOperationException("Saved values or historical attribution failed verification.");
    }
    var oldSourceIds = beforeSources.Select(s => s.Id).ToArray(); var oldShareIds = beforeShares.Select(s => s.Id).ToArray();
    var preservedSources = await db.Set<NinjaDailyPerformanceSource>().AsNoTracking().Where(s => oldSourceIds.Contains(s.Id)).OrderBy(s => s.Id).ToArrayAsync();
    var preservedShares = await db.NinjaDailyPerformance.AsNoTracking().Where(s => oldShareIds.Contains(s.Id)).OrderBy(s => s.Id).ToArrayAsync();
    if (beforeSourceHash != NinjaEvaluationService.Hash(JsonSerializer.Serialize(preservedSources, NinjaEvaluationService.Json))
        || beforeShareHash != NinjaEvaluationService.Hash(JsonSerializer.Serialize(preservedShares, NinjaEvaluationService.Json)))
        throw new InvalidOperationException("Existing performance changed.");
    var report = new Rehearsal(true, hash, originalHash, plan.InputRevision, mode, confirmation?.ImportedRows ?? 0,
        existing, 40, selected.Sum(r => (long)r.Orders), selected.Sum(r => r.Hours), DateTimeOffset.UtcNow);
    if (mode == "apply") await transaction.CommitAsync(); else await transaction.RollbackAsync();
    db.ChangeTracker.Clear();
    if (mode == "rehearse")
    {
        if (await db.Set<NinjaDailyPerformanceSource>().CountAsync() != beforeSources.Length || await db.NinjaDailyPerformance.CountAsync() != beforeShares.Length)
            throw new InvalidOperationException("Rehearsal rollback did not preserve row counts.");
        await File.WriteAllTextAsync(rehearsalPath, JsonSerializer.Serialize(report, new JsonSerializerOptions(NinjaEvaluationService.Json) { WriteIndented = true }));
    }
    else await File.WriteAllTextAsync(Path.Combine(output, mode + ".json"), JsonSerializer.Serialize(report, new JsonSerializerOptions(NinjaEvaluationService.Json) { WriteIndented = true }));
    Console.WriteLine(JsonSerializer.Serialize(report, NinjaEvaluationService.Json));
});

internal sealed record ImportActor(Guid ActorId, long Version, string RunId) : ICurrentUser
{
    public Guid? UserId => ActorId;
    public long? AuthorizationVersion => Version;
    public Guid? SessionId => null;
    public string? CorrelationId => RunId;
}
internal sealed record Mapping(int Row, string CaptainId, DateOnly Date, int Orders, string Hours,
    Guid? AccountId, string Status, AssignmentMapping[] Assignments);
internal sealed record AssignmentMapping(Guid Id, Guid RiderId);
internal sealed record Rehearsal(bool Passed, string SelectionHash, string SourceHash, string InputRevision,
    string Mode, int ImportedRows, int SkippedRows, int HeldRows, long Orders, decimal Hours, DateTimeOffset CompletedAtUtc);
internal sealed class EvaluationReadPermission(Guid platform) : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(permissionKey == PermissionKeys.Ninja.Read && scope?.TargetId == platform);
    public void InvalidateUser(Guid userId, long authorizationVersion) { }
}
