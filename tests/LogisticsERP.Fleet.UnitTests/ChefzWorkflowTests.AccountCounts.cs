using System.Reflection;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Chefz;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Theory]
    [InlineData("CHEFZ")]
    [InlineData("THECHEFZ")]
    [InlineData("CHEFSZ")]
    [InlineData("SHIFTZ")]
    public async Task AccountListReturnsCompleteSponsorAndStatusCountsRegardlessOfPage(string platformCode)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        (await f.Db.ClientPlatforms.SingleAsync(ct)).Code = platformCode;
        var firstSponsor = new Sponsor { RegistryNameAr = "الكفيل الأول", RegistryNameEn = "First", EmployerIdentityNumber = "7000000001" };
        var secondSponsor = new Sponsor { RegistryNameAr = "الكفيل الثاني", RegistryNameEn = "Second", EmployerIdentityNumber = "7000000002" };
        f.Db.AddRange(firstSponsor, secondSponsor);
        f.Account.SponsorId = firstSponsor.Id;
        f.Account.Status = PlatformRiderAccountStatus.Assigned;
        PlatformRiderAccount Add(string id, Guid? sponsorId, PlatformRiderAccountStatus status)
        {
            var account = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, SponsorId = sponsorId,
                OperatingCityId = f.Account.OperatingCityId, Code = id, ExternalAccountId = id, Status = status, RowVersion = [1] };
            f.Db.Add(account);
            return account;
        }
        RiderClientAssignment Link(PlatformRiderAccount account, DateOnly? to = null, bool deleted = false)
        {
            var assignment = new RiderClientAssignment { PlatformRiderAccountId = account.Id, RiderProfileId = f.First.RiderProfileId,
                ClientContractId = f.First.ClientContractId, EffectiveFrom = new(2026, 10, 1), EffectiveTo = to,
                Status = to is null ? RiderAssignmentStatus.Active : RiderAssignmentStatus.Ended, IsDeleted = deleted };
            f.Db.Add(assignment);
            return assignment;
        }
        var blocked = Add("B", firstSponsor.Id, PlatformRiderAccountStatus.Suspended);
        Link(blocked);
        Link(blocked); // Count accounts once even if legacy data contains duplicate open links.
        Add("C", firstSponsor.Id, PlatformRiderAccountStatus.Available);
        Add("D", secondSponsor.Id, PlatformRiderAccountStatus.Retired);
        Link(Add("E", secondSponsor.Id, PlatformRiderAccountStatus.Archived));
        Add("F", null, PlatformRiderAccountStatus.Available);
        Link(Add("G", null, PlatformRiderAccountStatus.Suspended), deleted: true);
        Link(Add("H", secondSponsor.Id, PlatformRiderAccountStatus.Assigned), new(2026, 10, 10));
        var deleted = Add("deleted", firstSponsor.Id, PlatformRiderAccountStatus.Archived);
        deleted.IsDeleted = true;
        Link(deleted);
        var other = new ClientPlatform { Code = "OTHER", NameAr = "آخر", NameEn = "Other" };
        f.Db.Add(other);
        var otherAccount = Add("other", firstSponsor.Id, PlatformRiderAccountStatus.Assigned);
        otherAccount.ClientPlatformId = other.Id;
        Link(otherAccount);
        await f.Db.SaveChangesAsync(ct);

        var response = Value(await f.Service.GetAccountsAsync(1, 1, ct));
        Assert.Single(response.Items);
        Assert.Equal(1, response.Page);
        Assert.Equal(1, response.PageSize);
        Assert.Equal(8, response.TotalCount);
        Assert.Equal(new ChefzAccountCounts(8, 3, 5, 2, 2, 2, 1, 1), response.Summary);
        Assert.Equal(3, response.Sponsors.Count);
        var first = Assert.Single(response.Sponsors, s => s.SponsorId == firstSponsor.Id);
        Assert.Equal("الكفيل الأول", first.SponsorNameAr);
        Assert.Equal("First", first.SponsorNameEn);
        Assert.Equal(new ChefzAccountCounts(3, 2, 1, 1, 1, 1, 0, 0), first.Counts);
        Assert.Equal(new ChefzAccountCounts(3, 1, 2, 0, 1, 0, 1, 1), Assert.Single(response.Sponsors, s => s.SponsorId == secondSponsor.Id).Counts);
        var outside = Assert.Single(response.Sponsors, s => s.SponsorId is null);
        Assert.Equal("خارج الكفالة", outside.SponsorNameAr);
        Assert.Equal(new ChefzAccountCounts(2, 0, 2, 1, 0, 1, 0, 0), outside.Counts);
        Assert.Equal(response.TotalCount, response.Sponsors.Sum(s => s.Counts.Total));
        Assert.Equal(response.Summary.Taken, response.Sponsors.Sum(s => s.Counts.Taken));

        var emptyPage = Value(await f.Service.GetAccountsAsync(100, 1, ct));
        Assert.Empty(emptyPage.Items);
        Assert.Equal(response.Summary, emptyPage.Summary);
        Assert.Equal(response.Sponsors, emptyPage.Sponsors);
        Value(await f.Service.SetAccountStatusAsync(blocked.Id, new("Archived", "AQ=="), ct));
        var changed = Value(await f.Service.GetAccountsAsync(1, 1, ct));
        Assert.Equal(new ChefzAccountCounts(8, 3, 5, 2, 2, 1, 1, 2), changed.Summary);
    }

    [Fact]
    public async Task EmptyAccountListHasZeroCountsAndStillValidatesAccessAndPaging()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        f.Account.IsDeleted = true;
        await f.Db.SaveChangesAsync(ct);
        var result = Value(await f.Service.GetAccountsAsync(1, 50, ct));
        Assert.Empty(result.Items);
        Assert.Empty(result.Sponsors);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(new ChefzAccountCounts(0, 0, 0, 0, 0, 0, 0, 0), result.Summary);
        Assert.Equal("Chefz.Invalid", (await f.Service.GetAccountsAsync(0, 50, ct)).Error.Code);
        Assert.Equal("Chefz.Invalid", (await f.Service.GetAccountsAsync(1, 101, ct)).Error.Code);
        f.Checker.Allow = false;
        Assert.Equal("Chefz.Forbidden", (await f.Service.GetAccountsAsync(1, 50, ct)).Error.Code);
    }

    [Fact]
    public void AccountCountsAggregateAndDeduplicateInSqlServer()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer().Options);
        var service = new ChefzService(db, new CurrentUser(), new Checker(), new Clock(), null!);
        var method = typeof(ChefzService).GetMethod("AccountCountsQuery", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var query = Assert.IsAssignableFrom<IQueryable>(method.Invoke(service, null));
        var sql = query.ToQueryString();
        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEFT JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT(*)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EffectiveTo", sql, StringComparison.Ordinal);
    }
}
