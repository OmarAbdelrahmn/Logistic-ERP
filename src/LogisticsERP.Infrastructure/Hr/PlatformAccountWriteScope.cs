using System.Data;
using System.Data.Common;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LogisticsERP.Infrastructure.Hr;

// Session locks cover the reads as well as SaveChanges and work inside existing Jahez transactions.
// Every writer takes these in account-id order; nested scopes on one connection are supported by SQL Server.
internal sealed class PlatformAccountWriteScope : IAsyncDisposable
{
    private readonly ApplicationDbContext db;
    private readonly List<string> resources = [];
    private bool opened;
    private PlatformAccountWriteScope(ApplicationDbContext context) => db = context;

    public static async Task<PlatformAccountWriteScope> AcquireAsync(ApplicationDbContext db,
        IEnumerable<Guid> accountIds, CancellationToken ct)
    {
        var scope = new PlatformAccountWriteScope(db);
        if (!db.Database.IsSqlServer()) return scope;
        try
        {
            scope.opened = db.Database.GetDbConnection().State != ConnectionState.Open;
            if (scope.opened) await db.Database.OpenConnectionAsync(ct);
            foreach (var id in accountIds.Distinct().Order())
            {
                var resource = $"platform-account:{id:D}";
                var result = await scope.Command("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=15000; SELECT @r;", resource, ct);
                if (Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) < 0)
                    throw new DbUpdateConcurrencyException("The account is being updated; retry the operation.");
                scope.resources.Add(resource);
            }
            return scope;
        }
        catch { await scope.DisposeAsync(); throw; }
    }

    private async Task<object?> Command(string sql, string resource, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        var parameter = command.CreateParameter(); parameter.ParameterName = "@resource"; parameter.Value = resource;
        command.Parameters.Add(parameter);
        return await command.ExecuteScalarAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var resource in resources.AsEnumerable().Reverse())
            {
                if (db.Database.GetDbConnection().State != ConnectionState.Open) break;
                await Command("EXEC sys.sp_releaseapplock @Resource=@resource, @LockOwner='Session';", resource, CancellationToken.None);
            }
        }
        finally { if (opened) await db.Database.CloseConnectionAsync(); }
    }
}
