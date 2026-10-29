using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed class NinjaEvaluationHttpTests
{
    [Fact]
    public async Task MultipartPreviewBindsOptionsAndRejectsReadOnlyAndEmptyFiles()
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        var imports = new CaptureImports();
        builder.Services.AddSingleton<INinjaPerformanceImportService>(imports);
        builder.Services.AddSingleton(DispatchProxy.Create<INinjaEvaluationService, UnexpectedEvaluationCalls>());
        builder.Services.AddControllers().AddApplicationPart(typeof(NinjaEvaluationController).Assembly);
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
        builder.Services.AddAuthorizationBuilder().AddPolicy(AuthenticationPolicies.PermissionPrefix + PermissionKeys.Ninja.PerformanceImport,
            policy => policy.RequireAuthenticatedUser().RequireClaim("test-permission", PermissionKeys.Ninja.PerformanceImport));
        await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync(ct);
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        client.DefaultRequestHeaders.Add("X-Test-Permission", PermissionKeys.Ninja.PerformanceImport);
        using (var form = Form("captain.csv", "Captain ID,Total Orders,Total Working Hours\n0001,15,08:30:00"))
        {
            form.Add(new StringContent("MultiDay"), "mode");
            form.Add(new StringContent("Duration"), "hoursUnit");
            form.Add(new StringContent("MM/dd/yyyy"), "dateFormat");
            using var response = await client.PostAsync("/api/ninja/performance/imports/preview", form, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("MultiDay", imports.Options!.Mode); Assert.Equal("Duration", imports.Options.HoursUnit);
            Assert.Equal("MM/dd/yyyy", imports.Options.DateFormat); Assert.Equal("captain.csv", imports.FileName);
        }
        using (var empty = Form("empty.csv", string.Empty))
        {
            using var response = await client.PostAsync("/api/ninja/performance/imports/preview", empty, ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        client.DefaultRequestHeaders.Remove("X-Test-Permission");
        using (var form = Form("captain.csv", "content"))
        {
            using var response = await client.PostAsync("/api/ninja/performance/imports/preview", form, ct);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        Assert.Equal(1, imports.Calls); await app.StopAsync(ct);
    }
    private static MultipartFormDataContent Form(string name, string value)
    {
        var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(value)), "file", name); return form;
    }
    private sealed class CaptureImports : INinjaPerformanceImportService
    {
        public int Calls { get; private set; }
        public NinjaPerformanceImportOptions? Options { get; private set; }
        public string? FileName { get; private set; }
        public Task<Result<NinjaPerformancePreviewResponse>> PreviewAsync(string fileName, byte[] content, NinjaPerformanceImportOptions options, CancellationToken ct = default)
        {
            Calls++; Options = options; FileName = fileName;
            return Task.FromResult(Result.Success(new NinjaPerformancePreviewResponse(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(30), "hash", "revision", true, [])));
        }
        public Task<Result<NinjaPerformanceConfirmation>> ConfirmAsync(Guid previewId, string fileName, byte[] content, CancellationToken ct = default) => throw new InvalidOperationException("Unexpected confirmation.");
        public Task<Result<NinjaEvaluationFile>> TemplateAsync(string mode, CancellationToken ct = default) => throw new InvalidOperationException("Unexpected template request.");
    }
    public class UnexpectedEvaluationCalls : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new InvalidOperationException("Evaluation must not run during an import preview.");
    }
    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = Request.Headers.TryGetValue("X-Test-Permission", out var permission)
                ? new[] { new Claim("test-permission", permission.ToString()) } : [];
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")), "test")));
        }
    }
}
