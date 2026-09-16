using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HousingReadSqlDiagnosticTests
{
    [Fact]
    public async Task ExistingHousingAndRoomsCanBeReadFromSqlServer()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("LOGISTICS_HOUSING_SQL_TESTS") == "1",
            "Set LOGISTICS_HOUSING_SQL_TESTS=1 and ConnectionStrings__LogisticsDatabase to run the read-only SQL diagnostic.");
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LogisticsDatabase");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var db = new ApplicationDbContext(options);
        var housingId = await db.Housing.AsNoTracking().Select(item => item.Id)
            .FirstAsync(TestContext.Current.CancellationToken);
        var service = new HousingService(db, new DiagnosticCurrentUser());

        var housing = await service.GetAsync(housingId, TestContext.Current.CancellationToken);
        var rooms = await service.GetRoomsAsync(housingId, TestContext.Current.CancellationToken);

        Assert.True(housing.IsSuccess, housing.Error.Description);
        Assert.True(rooms.IsSuccess, rooms.Error.Description);
        Assert.Equal(rooms.Value!.Count, housing.Value!.Rooms!.Count);
    }

    private sealed class DiagnosticCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => "housing-read-sql-diagnostic";
    }
}
