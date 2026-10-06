using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HousingExternalOccupantSqlTests
{
    [Fact]
    public async Task ExternalResidentCanBeCreatedRenamedMovedAndRemovedWithSqlRetriesEnabled()
    {
        await using var fixture = await Fixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var service = fixture.Service;
        var created = await service.UpsertExternalOccupantAsync(null, fixture.RoomId, new("  مينا محسن  ", null, null), ct);
        Assert.True(created.IsSuccess, created.Error.Description);
        Assert.Equal("مينا محسن", created.Value!.Name);
        Assert.NotEmpty(created.Value.RowVersion);
        Assert.Equal(1, await fixture.OccupancyAsync(fixture.RoomId));

        var renamed = await service.UpsertExternalOccupantAsync(created.Value.Id, fixture.RoomId,
            new("مينا محسن علي", null, created.Value.RowVersion), ct);
        Assert.True(renamed.IsSuccess, renamed.Error.Description);
        Assert.Equal(1, await fixture.OccupancyAsync(fixture.RoomId));
        var stale = await service.UpsertExternalOccupantAsync(created.Value.Id, fixture.RoomId,
            new("stale", null, created.Value.RowVersion), ct);
        Assert.Equal(HrErrors.ConcurrencyConflict, stale.Error);

        var moved = await service.UpsertExternalOccupantAsync(created.Value.Id, fixture.RoomId,
            new(renamed.Value!.Name, fixture.OtherRoomId, renamed.Value.RowVersion), ct);
        Assert.True(moved.IsSuccess, moved.Error.Description);
        Assert.Equal(0, await fixture.OccupancyAsync(fixture.RoomId));
        Assert.Equal(1, await fixture.OccupancyAsync(fixture.OtherRoomId));
        var full = await service.UpsertExternalOccupantAsync(null, fixture.OtherRoomId, new("full room", null, null), ct);
        Assert.Equal(HrErrors.CapacityExceeded, full.Error);
        Assert.Equal(1, await fixture.Db.HousingExternalOccupants.CountAsync(ct));

        var removed = await service.DeleteExternalOccupantAsync(created.Value.Id, ct);
        Assert.True(removed.IsSuccess, removed.Error.Description);
        Assert.Equal(0, await fixture.OccupancyAsync(fixture.OtherRoomId));
        Assert.Empty(await fixture.Db.HousingExternalOccupants.AsNoTracking().ToArrayAsync(ct));
        var stored = await fixture.Db.HousingExternalOccupants.IgnoreQueryFilters().AsNoTracking().SingleAsync(ct);
        Assert.True(stored.IsDeleted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedSaveRollsBackCapacityAndTransientFailureReplaysWholeOperation(bool transient)
    {
        var failure = new FailNextSave(transient);
        await using var fixture = await Fixture.CreateAsync(failure);
        var ct = TestContext.Current.CancellationToken;
        failure.Armed = true;
        if (transient)
        {
            var result = await fixture.Service.UpsertExternalOccupantAsync(null, fixture.RoomId, new("retry resident", null, null), ct);
            Assert.True(result.IsSuccess, result.Error.Description);
            Assert.Equal(1, await fixture.OccupancyAsync(fixture.RoomId));
            Assert.Equal(1, await fixture.Db.HousingExternalOccupants.CountAsync(ct));
        }
        else
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Service.UpsertExternalOccupantAsync(
                null, fixture.RoomId, new("failed resident", null, null), ct));
            Assert.Equal(0, await fixture.OccupancyAsync(fixture.RoomId));
            Assert.Equal(0, await fixture.Db.HousingExternalOccupants.CountAsync(ct));
            await fixture.Db.SaveChangesAsync(ct);
            Assert.Equal(0, await fixture.Db.HousingExternalOccupants.CountAsync(ct));
        }
        Assert.False(failure.Armed);
    }

    private sealed class Fixture(ApplicationDbContext db, Guid roomId, Guid otherRoomId) : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; } = db;
        public HousingService Service { get; } = new(db, new User());
        public Guid RoomId { get; } = roomId;
        public Guid OtherRoomId { get; } = otherRoomId;

        public static async Task<Fixture> CreateAsync(SaveChangesInterceptor? interceptor = null)
        {
            Assert.SkipUnless(OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("LOGISTICS_HOUSING_WRITE_SQL_TESTS") == "1",
                "Set LOGISTICS_HOUSING_WRITE_SQL_TESTS=1 on Windows with SQL LocalDB to run isolated housing write tests.");
            // Only a unique LocalDB test database is created/dropped; application configuration is never used.
            var database = $"LogisticsHousingWriteTest_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=True;TrustServerCertificate=True",
                    sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null));
            if (interceptor is not null) options.AddInterceptors(interceptor);
            var db = new ApplicationDbContext(options.Options);
            try
            {
                var ct = TestContext.Current.CancellationToken;
                await db.Database.EnsureCreatedAsync(ct);
                var city = new GlobalCity { Code = "TEST", NameAr = "اختبار", NameEn = "Test" };
                var housing = new LogisticsERP.Domain.Entities.Housing.Housing
                {
                    CityId = city.Id, Code = "TEST", NameAr = "اختبار", NameEn = "Test", Status = HousingStatus.Active
                };
                var floor = new HousingFloor { HousingId = housing.Id, Name = "1" };
                var room = new HousingRoom { HousingId = housing.Id, FloorId = floor.Id, Name = "3", Capacity = 3 };
                var other = new HousingRoom { HousingId = housing.Id, FloorId = floor.Id, Name = "4", Capacity = 1 };
                db.AddRange(city, housing, floor, room, other);
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
                return new Fixture(db, room.Id, other.Id);
            }
            catch
            {
                await db.Database.EnsureDeletedAsync(CancellationToken.None);
                await db.DisposeAsync();
                throw;
            }
        }

        public Task<int> OccupancyAsync(Guid id) => Db.HousingRooms.AsNoTracking()
            .Where(item => item.Id == id).Select(item => item.CurrentOccupancy).SingleAsync(TestContext.Current.CancellationToken);

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync(CancellationToken.None);
            await Db.DisposeAsync();
        }
    }

    private sealed class FailNextSave(bool transient) : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!Armed) return ValueTask.FromResult(result);
            Armed = false;
            if (transient) throw new TimeoutException("Injected transient save failure.");
            throw new DbUpdateException("Injected permanent save failure.");
        }
    }

    private sealed class User : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => "housing-write-sql-tests";
    }
}
