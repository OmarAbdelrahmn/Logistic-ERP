using System.Reflection;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class SimplePlatformQueryTests
{
    [Fact]
    public void AccountListProjectionCanBeTranslatedToSqlServer()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer()
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new SimplePlatformService(context, new TestCurrentUser(), TimeProvider.System, null!);
        var method = typeof(SimplePlatformService).GetMethod(
            "CreateAccountProjectionQuery",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var query = Assert.IsAssignableFrom<IQueryable>(method!.Invoke(
            service,
            [context.PlatformRiderAccounts.AsNoTracking(), false]));

        var sql = query.ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PlatformRiderAccounts", sql, StringComparison.Ordinal);
        Assert.Contains("ClientPlatforms", sql, StringComparison.Ordinal);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => null;
    }
}
