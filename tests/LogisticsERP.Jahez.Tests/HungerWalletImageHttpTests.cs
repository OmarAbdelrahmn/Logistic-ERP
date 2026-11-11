using System.Net;
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

public sealed class HungerWalletImageHttpTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task MultipartReviewBindsOptionalImageAndNestedAssignment(bool attachImage, bool legacyDictionaryNotation)
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        var service = DispatchProxy.Create<IHungerFinanceService, CaptureReview>();
        var capture = (CaptureReview)service;
        builder.Services.AddSingleton(service);
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerQualityService, CaptureReview>());
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerAccountsService, CaptureReview>());
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerOperationsService, CaptureReview>());
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerReportingService, CaptureReview>());
        builder.Services.AddSingleton(DispatchProxy.Create<IHungerSettlementService, CaptureReview>());
        builder.Services.AddControllers().AddApplicationPart(typeof(HungerController).Assembly);
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
        builder.Services.AddAuthorizationBuilder().AddPolicy(AuthenticationPolicies.PermissionPrefix + PermissionKeys.Hunger.WalletReview,
            policy => policy.RequireAuthenticatedUser());
        await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync(ct);
        using var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        var rider = Guid.NewGuid();
        var previousAssignment = Guid.NewGuid();
        var unrecordedAssignment = Guid.NewGuid();
        var usageVersionKey = "assignment.previousDayUsages[0].usageRowVersions" + (legacyDictionaryNotation ? $".{previousAssignment}" : $"[{previousAssignment}]");
        using var form = new MultipartFormDataContent();
        foreach (var (key, value) in new Dictionary<string, string>
        {
            ["action"] = "Deliver", ["riderProfileId"] = rider.ToString(), ["supervisorEmployeeId"] = Guid.NewGuid().ToString(),
            ["businessDate"] = "2026-11-08", ["mobileBalance"] = "0.00", ["closingBalance"] = "0", ["provenAdjustments"] = "0",
            ["assignment.actualRiderProfileId"] = rider.ToString(), ["assignment.effectiveFrom"] = "2026-11-08",
            ["assignment.wasBackdated"] = "false", ["assignment.reason"] = "Delivery",
            ["assignment.previousDayUsages[0].assignmentIds[0]"] = previousAssignment.ToString(),
            ["assignment.previousDayUsages[0].assignmentIds[1]"] = unrecordedAssignment.ToString(),
            ["assignment.previousDayUsages[0].figures.ordersCount"] = "50",
            ["assignment.previousDayUsages[0].rowVersion"] = "AQ==",
            [usageVersionKey] = "AQ==",
            ["assignment.previousDayUsages[0].usageRowVersions" + (legacyDictionaryNotation ? $".{unrecordedAssignment}" : $"[{unrecordedAssignment}]")] = ""
        }) form.Add(new StringContent(value), key);
        if (attachImage)
        {
            var image = new ByteArrayContent([137, 80, 78, 71]);
            image.Headers.ContentType = new("image/png"); form.Add(image, "walletEvidence", "wallet.png");
        }
        using var response = await client.PostAsync($"/api/hunger/accounts/{Guid.NewGuid()}/wallet-review", form, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(rider, capture.Request!.Assignment!.ActualRiderProfileId);
        Assert.Equal(new DateOnly(2026, 11, 8), capture.Request.Assignment.EffectiveFrom);
        Assert.Equal(0, capture.Request.ClosingBalance);
        var previous = Assert.Single(capture.Request.Assignment.PreviousDayUsages!);
        Assert.Equal(50, previous.Figures.OrdersCount);
        Assert.Equal("AQ==", previous.RowVersion);
        Assert.NotNull(previous.UsageRowVersions);
        Assert.Equal("AQ==", previous.UsageRowVersions[previousAssignment]);
        Assert.True(previous.UsageRowVersions.ContainsKey(unrecordedAssignment));
        Assert.Null(previous.UsageRowVersions[unrecordedAssignment]);
        Assert.Equal(attachImage ? "wallet.png" : null, capture.FileName);
        using var invalidForm = new MultipartFormDataContent();
        foreach (var part in form)
        {
            var clone = new ByteArrayContent(await part.ReadAsByteArrayAsync(ct));
            foreach (var header in part.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            invalidForm.Add(clone);
        }
        invalidForm.Add(new StringContent("Ag=="), usageVersionKey);
        using var invalid = await client.PostAsync($"/api/hunger/accounts/{Guid.NewGuid()}/wallet-review", invalidForm, ct);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(1, capture.Calls);
        await app.StopAsync(ct);
    }

    public class CaptureReview : DispatchProxy
    {
        public int Calls { get; private set; }
        public HungerWalletReviewRequest? Request { get; private set; }
        public string? FileName { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(IHungerFinanceService.ReviewWalletWithImageAsync), targetMethod!.Name);
            Calls++;
            Request = (HungerWalletReviewRequest)args![1]!;
            FileName = ((PrivateFileUpload?)args[2])?.OriginalFileName;
            return Task.FromResult(Result.Success(new HungerWalletReviewResponse(Guid.NewGuid(), 0, 0, 0, 0, DateTimeOffset.UtcNow, 0, true)));
        }
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test")], "test")), "test")));
    }
}
