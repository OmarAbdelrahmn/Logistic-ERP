using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class AuthenticationSessionModelTests
{
    [Fact]
    public void UserSessionsAllowOnlyOneUnrevokedSessionPerUser()
    {
        using var context = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=AuthenticationSessionModelTests;Trusted_Connection=True;TrustServerCertificate=True")
                .Options);
        var entity = context.Model.FindEntityType(typeof(UserSession))!;
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == "UX_UserSessions_OneOpenSessionPerUser");

        Assert.True(index.IsUnique);
        Assert.Equal([nameof(UserSession.UserId)], index.Properties.Select(property => property.Name));
        Assert.Equal("[RevokedAtUtc] IS NULL AND [IsDeleted] = 0", index.GetFilter());
    }

    [Fact]
    public void AuthenticationDefaultsDisableConcurrentSessionsAndValidationCaching()
    {
        var options = new AuthenticationOptions();

        Assert.Equal(1, options.MaxActiveSessions);
        Assert.Equal(0, options.SessionValidationCacheSeconds);
    }
}
