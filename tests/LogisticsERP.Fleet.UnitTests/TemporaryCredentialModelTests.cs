using LogisticsERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class TemporaryCredentialModelTests
{
    [Fact]
    public void CredentialHashIsNotGloballyUnique()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LogisticsERP_ModelTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var dbContext = new IdentityDbContext(options);
        var entityType = dbContext.Model.FindEntityType(typeof(TemporaryCredential));

        Assert.NotNull(entityType);
        Assert.DoesNotContain(entityType!.GetIndexes(), index =>
            index.Properties.Count == 1
            && index.Properties[0].Name == nameof(TemporaryCredential.CredentialHash)
            && index.IsUnique);
    }
}
