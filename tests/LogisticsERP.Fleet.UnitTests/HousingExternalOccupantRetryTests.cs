using System.Data;
using System.Data.Common;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HousingExternalOccupantRetryTests
{
    [Fact]
    public async Task SqlServerRejectsCapacityWriteInsideAnUnwrappedTransaction()
    {
        var commands = new FullRoomCommands(Guid.NewGuid());
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=housing-retry-diagnostic;Trusted_Connection=True",
                sql => sql.EnableRetryOnFailure())
            .AddInterceptors(new NoConnection(), new TestTransactionInterceptor(), commands).Options);
        var ct = TestContext.Current.CancellationToken;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.HousingRooms
            .Where(item => item.CurrentOccupancy < item.Capacity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CurrentOccupancy, item => item.CurrentOccupancy + 1), ct));

        Assert.Contains("does not support user-initiated transactions", exception.Message);
        Assert.Equal(0, commands.CapacityChecks);
    }

    [Fact]
    public async Task SqlServerRetryStrategyAllowsTransactionalCapacityCheckForExternalResident()
    {
        var roomId = Guid.NewGuid();
        var commands = new FullRoomCommands(roomId);
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=housing-retry-diagnostic;Trusted_Connection=True",
                sql => sql.EnableRetryOnFailure())
            .AddInterceptors(new NoConnection(), new TestTransactionInterceptor(), commands).Options);
        var service = new HousingService(db, new User());

        var result = await service.UpsertExternalOccupantAsync(null, roomId, new("مينا محسن", null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(HrErrors.CapacityExceeded, result.Error);
        Assert.Equal(1, commands.CapacityChecks);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private sealed class NoConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class TestTransactionInterceptor : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult<DbTransaction>.SuppressWithResult(new TestTransaction(connection)));
    }

    private sealed class TestTransaction(DbConnection connection) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        protected override DbConnection DbConnection => connection;
        public override void Commit() { }
        public override void Rollback() { }
    }

    private sealed class FullRoomCommands(Guid roomId) : DbCommandInterceptor
    {
        public int CapacityChecks { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Status", typeof(int));
            table.Rows.Add(roomId, (int)HousingStatus.Active);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            // Commands are suppressed; real persistence/rollback is covered by the opt-in LocalDB tests.
            Assert.NotNull(ExecutionStrategy.Current);
            Assert.Contains("UPDATE", command.CommandText);
            CapacityChecks++;
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
        }
    }

    private sealed class User : ICurrentUser
    {
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => "housing-retry-diagnostic";
    }
}
