using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Infrastructure;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var ct = CancellationToken.None;
string Option(string key) => args.SkipWhile(a => a != key).Skip(1).FirstOrDefault() ?? throw new ArgumentException($"Missing {key}.");
var configPath = Path.GetFullPath(Option("--config"));
var workbook = args.Contains("--verify-accounts", StringComparer.Ordinal) ? null : Path.GetFullPath(Option("--workbook"));
var actorId = Guid.Parse(Option("--actor-user-id"));
var apply = args.Contains("--apply", StringComparer.Ordinal);
var config = new ConfigurationBuilder().AddJsonFile(configPath).AddEnvironmentVariables()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["Features:Hunger"] = "true" }).Build();
var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(config.GetConnectionString("LogisticsDatabase"));
if (connection.DataSource != "db67927.public.databaseasp.net" || connection.InitialCatalog != "db67927")
    throw new InvalidOperationException("Unexpected database target; this runner is restricted to the authorized hosted ERP.");
var actor = new ImportActor();
var host = Host.CreateApplicationBuilder(); host.Environment.EnvironmentName = Environments.Production;
var services = host.Services; services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning)); services.AddSingleton<IConfiguration>(config);
services.AddSingleton<ICurrentUser>(actor); services.AddInfrastructure(config);
await using var provider = services.BuildServiceProvider(); await using var scope = provider.CreateAsyncScope();
var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
var user = await identity.Users.AsNoTracking().SingleAsync(u => u.Id == actorId, ct);
actor.Id = user.Id; actor.Version = user.AuthorizationVersion;
var service = scope.ServiceProvider.GetRequiredService<IHungerAccountsService>();
if (args.Contains("--verify-accounts", StringComparer.Ordinal))
{
    var verified = await HungerAccountVerification.ReadAllAsync(service, ct);
    if (verified.IsFailure) throw new InvalidOperationException(verified.Error.Description);
    var rows = verified.Value!;
    Console.WriteLine(JsonSerializer.Serialize(new { Verified = true, TotalCount = rows.Count,
        ReturnedRows = rows.Count, RowsWithOwner = rows.Count(i => i.Owner is not null),
        RowsWithSourceRider = rows.Count(i => i.SourceActualRider is not null),
        SourceReportedUsed = rows.Count(i => i.MonthlyUsageStatus == "Used" && i.MonthlyUsageSource == "ImportedSource"),
        Dispositions = rows.GroupBy(i => i.OperationalDisposition).ToDictionary(g => g.Key, g => g.Count()) }));
    return;
}
var preview = await service.PreviewAccountsAsync(Path.GetFileName(workbook!), await File.ReadAllBytesAsync(workbook!, ct), ct);
if (preview.IsFailure) throw new InvalidOperationException(preview.Error.Description);
Console.WriteLine(JsonSerializer.Serialize(new { Mode = apply ? "Apply" : "Preview", preview.Value!.PreviewId,
    preview.Value.CanConfirm, Rows = preview.Value.Rows.Count, Rejected = preview.Value.Rows.Count(r => r.Error is not null),
    MatchedActualUsers = preview.Value.Rows.Count(r => r.ActualRiderProfileId.HasValue), PreviewRows = preview.Value.Rows }));
if (!apply) return;
var decisions = JsonSerializer.Deserialize<HungerAccountImportConfirmation>(await File.ReadAllTextAsync(Option("--resolutions"), ct), new JsonSerializerOptions(JsonSerializerDefaults.Web))
    ?? throw new ArgumentException("A reviewed resolutions file is required.");
var result = await service.ConfirmAccountsAsync(preview.Value.PreviewId, decisions, ct);
if (result.IsFailure) throw new InvalidOperationException(result.Error.Description);
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
db.ChangeTracker.Clear();
var stored = await db.Set<HungerImportPreview>().AsNoTracking().SingleAsync(p => p.Id == result.Value!.ImportId, ct);
if (stored.ResultJson is null) throw new InvalidOperationException("Missing durable import receipt.");
var actual = await db.Set<LogisticsERP.Domain.Entities.Clients.PlatformAccountBusinessDetails>().AsNoTracking()
    .CountAsync(d => d.SourceReference != null && d.SourceReference.StartsWith("Import:" + stored.Id.ToString() + "/"), ct);
if (actual != result.Value!.ImportedAccounts) throw new InvalidOperationException("Post-import account count mismatch.");
Console.WriteLine(JsonSerializer.Serialize(new { result.Value.ImportId, result.Value.ImportedAccounts,
    result.Value.MatchedActualUsers, result.Value.PendingAssignments, VerifiedSourceRecords = actual }));

internal sealed class ImportActor : ICurrentUser
{
    internal Guid Id { get; set; }
    internal long Version { get; set; }
    public Guid? UserId => Id;
    public Guid? SessionId => null;
    public long? AuthorizationVersion => Version;
    public string? CorrelationId => "hunger-workbook-import-20261104";
}
