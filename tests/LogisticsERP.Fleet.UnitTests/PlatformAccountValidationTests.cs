using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PlatformAccountValidationTests
{
    public static TheoryData<SimplePlatformAccountUpsertRequest, string> InvalidRequests
    {
        get
        {
            var valid = ValidRequest();
            return new()
            {
                { valid with { PlatformId = Guid.Empty }, "platformId" },
                { valid with { OperatingCityId = Guid.Empty }, "operatingCityId" },
                { valid with { SponsorId = Guid.Empty }, "sponsorId" },
                { valid with { OwnerRiderProfileId = Guid.Empty }, "ownerRiderProfileId" },
                { valid with { Code = " " }, "code" },
                { valid with { ExternalAccountId = " " }, "externalAccountId" },
                { valid with { PaymentModel = "Unknown" }, "paymentModel" },
                { valid with { Status = "Unknown" }, "status" },
                { valid with { Status = "Assigned" }, "status" },
                { valid with { Status = "Archived" }, "status" },
                { valid with { StartDate = new(2026, 10, 2), EndDate = new(2026, 10, 1) }, "endDate" },
                { valid with { Code = new string('a', 33) }, "code" },
                { valid with { ExternalAccountId = new string('a', 151) }, "externalAccountId" },
                { valid with { UserName = new string('a', 151) }, "userName" },
                { valid with { StatusReason = new string('a', 501) }, "statusReason" },
                { valid with { Notes = new string('a', 4001) }, "notes" }
            };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task InvalidCreationIdentifiesTheInputAndDoesNotSave(
        SimplePlatformAccountUpsertRequest request, string field)
    {
        await using var db = CreateContext();
        var result = await Service(db).CreateAccountAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(field, result.Error.Field);
        Assert.StartsWith("platform.account_", result.Error.Code, StringComparison.Ordinal);
        var errors = Assert.IsType<Dictionary<string, string[]>>(result.Error.Details!["errors"]);
        Assert.Equal(result.Error.Description, Assert.Single(errors[field]));
        Assert.NotEqual(HrErrors.InvalidRequest.Description, result.Error.Description);
        Assert.Empty(db.PlatformRiderAccounts.Local);
    }

    [Theory]
    [InlineData("platformId")]
    [InlineData("operatingCityId")]
    [InlineData("sponsorId")]
    [InlineData("ownerRiderProfileId")]
    public async Task UnknownReferencesIdentifyTheSelection(string field)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var request = SeedReferences(db);
        await db.SaveChangesAsync(ct);
        var unknown = Guid.NewGuid();
        request = field switch
        {
            "platformId" => request with { PlatformId = unknown },
            "operatingCityId" => request with { OperatingCityId = unknown },
            "sponsorId" => request with { SponsorId = unknown },
            _ => request with { OwnerRiderProfileId = unknown }
        };

        var result = await Service(db).CreateAccountAsync(request, ct);

        Assert.Equal("platform.account_reference_unavailable", result.Error.Code);
        Assert.Equal(field, result.Error.Field);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Empty(db.PlatformRiderAccounts.Local);
    }

    [Fact]
    public async Task UnsupportedPaymentModelExplainsThePlatformRestriction()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var request = SeedReferences(db);
        Assert.Single(db.ClientPlatforms.Local).SupportedPaymentModels = SupportedPlatformPaymentModels.Salary;
        await db.SaveChangesAsync(ct);

        var result = await Service(db).CreateAccountAsync(request, ct);

        Assert.Equal(PlatformAccountErrors.UnsupportedPaymentModel, result.Error);
        Assert.Empty(db.PlatformRiderAccounts.Local);
    }

    [Theory]
    [InlineData("code", "platform.account_code_duplicate")]
    [InlineData("externalAccountId", "platform.account_external_id_duplicate")]
    [InlineData("ownerRiderProfileId", "platform.account_owner_duplicate")]
    public async Task DuplicateCreationExplainsWhichRuleConflicts(string field, string code)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var request = SeedReferences(db);
        db.PlatformRiderAccounts.Add(new PlatformRiderAccount
        {
            Code = field == "code" ? request.Code : "OTHER",
            ExternalAccountId = field == "externalAccountId" ? request.ExternalAccountId : "OTHER",
            ClientPlatformId = request.PlatformId,
            RegisteredEmployeeId = Assert.Single(db.RiderProfiles.Local).EmployeeId,
            OperatingCityId = request.OperatingCityId,
            SponsorId = request.SponsorId
        });
        await db.SaveChangesAsync(ct);

        var result = await Service(db).CreateAccountAsync(request, ct);

        Assert.Equal(code, result.Error.Code);
        Assert.Equal(field, result.Error.Field);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Single(db.PlatformRiderAccounts.Local);
    }

    [Fact]
    public async Task ValidCreationNeedsOnlyTheExistingAccountSponsor()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var request = SeedReferences(db);
        await db.SaveChangesAsync(ct);

        var result = await Service(db).CreateAccountAsync(request, ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(request.SponsorId, result.Value!.SponsorId);
        Assert.Equal("Available", result.Value.Status);
        Assert.Single(db.PlatformRiderAccounts.Local);
    }

    private static SimplePlatformAccountUpsertRequest SeedReferences(ApplicationDbContext db)
    {
        var platform = new ClientPlatform();
        var globalCity = new GlobalCity();
        var city = new OperatingCity { GlobalCityId = globalCity.Id };
        var sponsor = new Sponsor();
        var employee = new Employee { IsEmployee = false };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        db.AddRange(platform, globalCity, city, sponsor, employee, rider);
        return ValidRequest() with
        {
            PlatformId = platform.Id, OperatingCityId = city.Id, SponsorId = sponsor.Id,
            OwnerRiderProfileId = rider.Id
        };
    }

    private static SimplePlatformAccountUpsertRequest ValidRequest() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ACCOUNT-1", "EXT-1", null,
        "PayPerOrder", "Available", null, null, null, null, null, null, null);

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"PlatformAccountValidation_{Guid.NewGuid():N}", options => options.EnableNullChecks(false))
        .Options);

    private static SimplePlatformService Service(ApplicationDbContext db) => new(db, new TestCurrentUser(), TimeProvider.System, null!);

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => null;
    }
}
