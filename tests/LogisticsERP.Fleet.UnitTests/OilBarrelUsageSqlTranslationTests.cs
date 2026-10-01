using System.Data;
using System.Data.Common;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Infrastructure.Maintenance;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class OilBarrelUsageSqlTranslationTests
{
    [Fact]
    public async Task ActualReportCompilesSqlServerAggregatesAndPagination()
    {
        var recorder = new Recorder();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=diagnostic;Trusted_Connection=True")
            .AddInterceptors(new NoConnection(), recorder).Options);
        var service = new MaintenanceService(db, new User(), TimeProvider.System, new Files());
        var result = await service.GetOilBarrelUsageAsync(Guid.NewGuid(), 1, 50, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, recorder.Commands.Count);
        Assert.Contains("GROUP BY", recorder.Commands[3]);
        Assert.Contains("GROUP BY", recorder.Commands[4]);
        Assert.Contains("OFFSET", recorder.Commands[4]);
    }
    private sealed class NoConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }
    private sealed class Recorder : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            var table = new DataTable();
            if (Commands.Count == 1)
            {
                var barrel = new OilBarrel { BarrelNumber = "TRANSLATION" };
                var values = new List<object>();
                var select = command.CommandText[..command.CommandText.IndexOf("FROM", StringComparison.Ordinal)];
                foreach (var column in select.Split(','))
                {
                    var name = column[(column.LastIndexOf('[') + 1)..column.LastIndexOf(']')];
                    var property = typeof(OilBarrel).GetProperty(name)!;
                    table.Columns.Add(name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                    values.Add(property.GetValue(barrel) ?? DBNull.Value);
                }
                table.Rows.Add(values.ToArray());
            }
            else if (Commands.Count < 5)
            {
                table.Columns.Add("Value", Commands.Count == 4 ? typeof(int) : typeof(decimal));
                table.Rows.Add(0);
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
    private sealed class User : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }
    private sealed class Files : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) { }
    }
}
