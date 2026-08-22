using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LogisticsERP.Fleet.UnitTests;

internal sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "LogisticsERP.Fleet.UnitTests";
    public string ContentRootPath { get; set; } = contentRootPath;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
