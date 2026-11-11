using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Application.Features.Jahez;
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

public sealed class PlatformPerformanceDeletionHttpTests
{
    [Theory]
    [InlineData("/api/hunger/performance", PermissionKeys.Hunger.PerformanceDelete)]
    [InlineData("/api/chefz/performance", PermissionKeys.Chefz.PerformanceDelete)]
    [InlineData("/api/jahez/v2/performance", PermissionKeys.Jahez.PerformanceDelete)]
    public async Task DeleteDayRequiresItsPermissionAndBindsOptionalSponsorWithoutBroadeningMalformedScope(string route, string permission)
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerQualityService, Capture>());
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerAccountsService, Capture>());
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerFinanceService, Capture>());
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerReportingService, Capture>());
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerSettlementService, Capture>());
        var hunger = DispatchProxy.Create<IHungerOperationsService, Capture>();
        var chefz = DispatchProxy.Create<IChefzService, Capture>();
        var jahez = DispatchProxy.Create<IJahezWorkflowService, Capture>();
        builder.Services.AddSingleton(hunger); builder.Services.AddSingleton(chefz); builder.Services.AddSingleton(jahez);
        var capture = route.Contains("hunger", StringComparison.Ordinal) ? (Capture)hunger : route.Contains("chefz", StringComparison.Ordinal) ? (Capture)chefz : (Capture)jahez;
        builder.Services.AddControllers().AddApplicationPart(typeof(HungerController).Assembly);
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, Authentication>("test", _ => { });
        builder.Services.AddAuthorizationBuilder().AddPolicy(AuthenticationPolicies.PermissionPrefix + permission,
            policy => policy.RequireAuthenticatedUser().RequireClaim("test-permission", permission));
        await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync(ct);
        using var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        var path = route + "?reportDate=2026-11-01&reason=Wrong%20assignment";
        client.DefaultRequestHeaders.Add("X-Test-Permission", PermissionKeys.Ninja.PerformanceImport);
        using (var denied = await client.DeleteAsync(path, ct)) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(0, capture.Calls);
        client.DefaultRequestHeaders.Remove("X-Test-Permission"); client.DefaultRequestHeaders.Add("X-Test-Permission", permission);
        using (var all = await client.DeleteAsync(path, ct)) Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        Assert.Null(capture.Sponsor); Assert.Equal(new DateOnly(2026, 11, 1), capture.Day); Assert.Equal("Wrong assignment", capture.Reason);
        var sponsor = Guid.NewGuid();
        using (var scoped = await client.DeleteAsync(path + $"&sponsorId={sponsor}", ct)) Assert.Equal(HttpStatusCode.OK, scoped.StatusCode);
        Assert.Equal(sponsor, capture.Sponsor);
        foreach (var invalid in new[] { route + "?reason=Correction", route + "?reportDate=bad&reason=Correction", route + "?reportDate=2026-11-01", path + "&sponsorId=bad", path + "&sponsorId=" })
        {
            using var response = await client.DeleteAsync(invalid, ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        Assert.Equal(2, capture.Calls);
        await app.StopAsync(ct);
    }

    public class Capture : DispatchProxy
    {
        public int Calls { get; private set; }
        public DateOnly Day { get; private set; }
        public Guid? Sponsor { get; private set; }
        public string? Reason { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != "DeletePerformanceAsync") throw new InvalidOperationException("Unexpected operation.");
            Calls++; Day = (DateOnly)args![0]!; Sponsor = (Guid?)args[1]; Reason = (string)args[2]!;
            if (targetMethod.ReturnType == typeof(Task<Result<HungerPerformanceDeletionResponse>>))
                return Task.FromResult(Result.Success(new HungerPerformanceDeletionResponse(Day, Sponsor, 1, 0)));
            if (targetMethod.ReturnType == typeof(Task<Result<ChefzPerformanceDeletionResponse>>))
                return Task.FromResult(Result.Success(new ChefzPerformanceDeletionResponse(Day, 1, 1, 0) { SponsorId = Sponsor }));
            return Task.FromResult(Result.Success(new JahezPerformanceDeletionResponse(Day, Sponsor, 1)));
        }
    }
    private sealed class Authentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim("test-permission", Request.Headers["X-Test-Permission"].ToString()) };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")), "test")));
        }
    }
}
