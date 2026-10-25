using LogisticsERP.Infrastructure;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PlatformCredentialConfigurationTests
{
    [Fact]
    public void CredentialProtectorUsesEnvironmentKeyOverJsonStyleConfiguration()
    {
        var prefix = $"LogisticsERP_Test_{Guid.NewGuid():N}_";
        var variable = prefix + "SensitiveData__EncryptionKey";
        var environmentKey = Enumerable.Repeat((byte)7, 32).ToArray();
        Environment.SetEnvironmentVariable(variable, Convert.ToBase64String(environmentKey));
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                EnvironmentName = Environments.Production,
                DisableDefaults = true
            });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LogisticsDatabase"] = "Server=invalid;Database=configuration_test;Trusted_Connection=True",
                ["SensitiveData:EncryptionKey"] = Convert.ToBase64String(new byte[32])
            }).AddEnvironmentVariables(prefix);
            builder.Services.AddInfrastructure(builder.Configuration);
            using var provider = builder.Services.BuildServiceProvider();
            var configured = provider.GetRequiredService<IPlatformCredentialProtector>();
            var expected = new PlatformCredentialProtector(() => environmentKey);
            var secret = expected.Protect("environment-key-password");

            Assert.Equal("environment-key-password", configured.Unprotect(secret));
        }
        finally { Environment.SetEnvironmentVariable(variable, null); }
    }
}
