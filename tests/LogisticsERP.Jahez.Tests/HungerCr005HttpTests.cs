using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
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

// HTTP binding of the CR-005 routes whose shape changed: optional execute body and the two rider-swap content types.
public sealed class HungerCr005HttpTests
{
    [Fact]
    public async Task ExecuteAcceptsAnEmptyBodyAndAnEvidenceBody()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = await Host.Start(ct);
        var id = Guid.NewGuid(); var before = Guid.NewGuid(); var after = Guid.NewGuid();

        var empty = await host.Client.PostAsync($"api/hunger/finance/requests/{id}/execute", null, ct);
        Assert.Equal(HttpStatusCode.NotFound, empty.StatusCode);
        Assert.Equal(nameof(IHungerFinanceService.ExecuteMoneyRequestAsync), host.Capture.Method);
        Assert.Equal(id, host.Capture.Args[0]); Assert.Null(host.Capture.Args[1]);

        var withBody = await host.Client.PostAsJsonAsync($"api/hunger/finance/requests/{id}/execute", new { evidenceBeforeId = before, evidenceAfterId = after }, ct);
        Assert.Equal(HttpStatusCode.NotFound, withBody.StatusCode);
        Assert.Equal(new HungerMoneyExecution(before, after), host.Capture.Args[1]);
        Assert.Equal(2, host.Capture.Calls);
    }

    [Fact]
    public async Task RiderSwapReviewRoutesByContentTypeAndParsesTheJsonField()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = await Host.Start(ct);
        var account = Guid.NewGuid(); var oldRider = Guid.NewGuid(); var newRider = Guid.NewGuid(); var approval = Guid.NewGuid();
        var body = new
        {
            @return = new { action = "Return", riderProfileId = oldRider, supervisorEmployeeId = Guid.NewGuid(), businessDate = "2026-11-08",
                mobileBalance = "-40.50", closingBalance = (decimal?)null, provenAdjustments = 0, walletEvidence = "" },
            handover = new { actualRiderProfileId = newRider, effectiveFrom = "2026-11-08", reason = "swap", wasBackdated = false },
            handoverExceptionApprovalId = approval
        };
        var url = $"api/hunger/accounts/{account}/rider-swap/review";

        var json = await host.Client.PostAsJsonAsync(url, body, ct);
        Assert.Equal(HttpStatusCode.NotFound, json.StatusCode);
        Assert.Equal(nameof(IHungerFinanceService.ReviewRiderSwapAsync), host.Capture.Method);

        using var form = new MultipartFormDataContent { { new StringContent(System.Text.Json.JsonSerializer.Serialize(body)), "request" } };
        var image = new ByteArrayContent([137, 80, 78, 71]); image.Headers.ContentType = new("image/png");
        form.Add(image, "walletEvidence", "wallet.png");
        var multipart = await host.Client.PostAsync(url, form, ct);
        Assert.Equal(HttpStatusCode.NotFound, multipart.StatusCode);
        Assert.Equal(nameof(IHungerFinanceService.ReviewRiderSwapWithImageAsync), host.Capture.Method);
        Assert.Equal(account, host.Capture.Args[0]);
        var parsed = Assert.IsType<HungerRiderSwapRequest>(host.Capture.Args[1]);
        Assert.Equal(oldRider, parsed.Return.RiderProfileId); Assert.Equal(-40.50m, parsed.Return.MobileBalance);
        Assert.Equal(newRider, parsed.Handover.ActualRiderProfileId); Assert.Equal(approval, parsed.HandoverExceptionApprovalId);
        Assert.Equal("wallet.png", Assert.IsType<PrivateFileUpload>(host.Capture.Args[2]).OriginalFileName);

        using var broken = new MultipartFormDataContent { { new StringContent("{not json"), "request" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync(url, broken, ct)).StatusCode);
        Assert.Equal(2, host.Capture.Calls);
    }

    // Records the call and answers "not found", which is enough to prove routing and binding.
    public class Capture : DispatchProxy
    {
        public int Calls { get; private set; }
        public string? Method { get; private set; }
        public object?[] Args { get; private set; } = [];
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls++; Method = targetMethod!.Name; Args = args ?? [];
            var value = targetMethod.ReturnType.GetGenericArguments()[0].GetGenericArguments()[0];
            var failure = typeof(Result).GetMethods().Single(m => m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true })
                .MakeGenericMethod(value).Invoke(null, [HungerErrors.NotFound])!;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(failure.GetType()).Invoke(null, [failure]);
        }
    }

    private sealed class Host(WebApplication app, HttpClient client, Capture capture) : IAsyncDisposable
    {
        internal HttpClient Client { get; } = client;
        internal Capture Capture { get; } = capture;
        internal static async Task<Host> Start(CancellationToken ct)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
            var service = DispatchProxy.Create<IHungerFinanceService, Capture>();
            builder.Services.AddSingleton(service);
            builder.Services.AddSingleton(DispatchProxy.Create<IHungerQualityService, Capture>());
            builder.Services.AddSingleton(DispatchProxy.Create<IHungerAccountsService, Capture>());
            builder.Services.AddSingleton(DispatchProxy.Create<IHungerOperationsService, Capture>());
            builder.Services.AddSingleton(DispatchProxy.Create<IHungerReportingService, Capture>());
            builder.Services.AddSingleton(DispatchProxy.Create<IHungerSettlementService, Capture>());
            builder.Services.AddControllers().AddApplicationPart(typeof(HungerController).Assembly);
            builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
            var policies = builder.Services.AddAuthorizationBuilder();
            foreach (var name in new[] { PermissionKeys.Hunger.WalletReview, PermissionKeys.Hunger.FundingExecute + "|" + PermissionKeys.Hunger.TopUpExecute })
                policies.AddPolicy(AuthenticationPolicies.PermissionPrefix + name, policy => policy.RequireAuthenticatedUser());
            var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
            await app.StartAsync(ct);
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new(app, new HttpClient { BaseAddress = new Uri(address) }, (Capture)service);
        }
        public async ValueTask DisposeAsync() { Client.Dispose(); await app.StopAsync(); await app.DisposeAsync(); }
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test")], "test")), "test")));
    }
}
