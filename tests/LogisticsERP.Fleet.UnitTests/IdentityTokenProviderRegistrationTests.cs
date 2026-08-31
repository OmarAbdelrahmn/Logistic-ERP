using LogisticsERP.Infrastructure;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class IdentityTokenProviderRegistrationTests
{
    [Fact]
    public async Task PasswordResetTokenCanBeGenerated()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LogisticsDatabase"] = "Server=(localdb)\\mssqllocaldb;Database=LogisticsERP_TokenProviderTest;Trusted_Connection=True;TrustServerCertificate=True"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}
