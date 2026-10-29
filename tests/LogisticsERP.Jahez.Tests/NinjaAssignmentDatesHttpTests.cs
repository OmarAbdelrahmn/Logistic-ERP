using System.Net;
using System.Net.Http.Json;
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

public sealed class NinjaAssignmentDatesHttpTests
{
    [Fact]
    public async Task DateEndpointRequiresOnlyItsDedicatedPermissionAndBindsAccountAssignmentAndDates()
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        var service = DispatchProxy.Create<INinjaService, CaptureDates>(); var capture = (CaptureDates)service;
        builder.Services.AddSingleton(service);
        builder.Services.AddControllers().AddApplicationPart(typeof(NinjaController).Assembly);
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
        builder.Services.AddAuthorizationBuilder().AddPolicy(AuthenticationPolicies.PermissionPrefix + PermissionKeys.Ninja.AssignmentDatesUpdate,
            policy => policy.RequireAuthenticatedUser().RequireClaim("test-permission", PermissionKeys.Ninja.AssignmentDatesUpdate));
        await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync(ct);
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var accountId = Guid.NewGuid(); var assignmentId = Guid.NewGuid();
        var path = $"/api/ninja/accounts/{accountId}/assignments/{assignmentId}/dates";
        var request = new NinjaAssignmentDatesRequest(new(2026, 10, 11), null, "Correct start date", "AQ==");
        foreach (var permission in new[] { PermissionKeys.Ninja.Read, PermissionKeys.Ninja.AccountsUpdate,
            PermissionKeys.Ninja.AssignmentsCreate, PermissionKeys.Ninja.AssignmentsDelete, PermissionKeys.Operations.PlatformAssignmentsUpdate })
        {
            client.DefaultRequestHeaders.Remove("X-Test-Permission"); client.DefaultRequestHeaders.Add("X-Test-Permission", permission);
            using var forbidden = await client.PutAsJsonAsync(path, request, ct);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }
        Assert.Equal(0, capture.Calls);
        client.DefaultRequestHeaders.Remove("X-Test-Permission");
        client.DefaultRequestHeaders.Add("X-Test-Permission", PermissionKeys.Ninja.AssignmentDatesUpdate);
        using (var response = await client.PutAsJsonAsync(path, request, ct))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<NinjaAssignmentDatesResponse>(ct);
            Assert.Equal(accountId, body!.AccountId); Assert.Equal(assignmentId, body.AssignmentId);
            Assert.Equal(request.EffectiveFrom, body.EffectiveFrom); Assert.Null(body.EffectiveTo);
        }
        Assert.Equal(accountId, capture.AccountId); Assert.Equal(assignmentId, capture.AssignmentId);
        Assert.Equal(request, capture.Request);
        var ended = request with { EffectiveTo = new(2026, 10, 15), Reason = "Correct ended dates" };
        using (var response = await client.PutAsJsonAsync(path, ended, ct)) Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ended, capture.Request);
        using (var malformed = new StringContent("{\"effectiveFrom\":\"invalid\",\"reason\":\"Correct\",\"rowVersion\":\"AQ==\"}", Encoding.UTF8, "application/json"))
        using (var response = await client.PutAsync(path, malformed, ct)) Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, capture.Calls);
        await app.StopAsync(ct);
    }

    public class CaptureDates : DispatchProxy
    {
        public int Calls { get; private set; }
        public Guid AccountId { get; private set; }
        public Guid AssignmentId { get; private set; }
        public NinjaAssignmentDatesRequest? Request { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(INinjaService.UpdateAssignmentDatesAsync))
                throw new InvalidOperationException("Unexpected Ninja operation.");
            Calls++; AccountId = (Guid)args![0]!; AssignmentId = (Guid)args[1]!;
            Request = (NinjaAssignmentDatesRequest)args[2]!;
            return Task.FromResult(Result.Success(new NinjaAssignmentDatesResponse(AccountId, AssignmentId,
                Request.EffectiveFrom, Request.EffectiveTo, Request.EffectiveTo is null ? "Active" : "Ended", "Ag==")));
        }
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
