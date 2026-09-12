using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Infrastructure;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PlatformServiceRegistrationTests
{
    [Fact]
    public void ReadServicesCanBeActivatedInProductionWithoutResolvingTheCredentialKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LogisticsDatabase"] =
                    "Server=(localdb)\\mssqllocaldb;Database=PlatformServiceRegistrationTests;Trusted_Connection=True;TrustServerCertificate=True"
            })
            .Build();
        var environment = new TestHostEnvironment(AppContext.BaseDirectory)
        {
            EnvironmentName = Environments.Production
        };
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISimplePlatformService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPlatformOperationsService>());
    }

    [Fact]
    public void CredentialProtectorResolvesItsKeyOnlyWhenEncryptionIsRequested()
    {
        var keyResolutionCount = 0;
        var protector = new PlatformCredentialProtector(() =>
        {
            keyResolutionCount++;
            return new byte[32];
        });

        Assert.Equal(0, keyResolutionCount);

        var protectedValue = protector.Protect("secret");

        Assert.Equal(1, keyResolutionCount);
        Assert.NotEmpty(protectedValue.Ciphertext);
        Assert.NotEmpty(protectedValue.Nonce);
        Assert.NotEmpty(protectedValue.AuthenticationTag);
    }
}
