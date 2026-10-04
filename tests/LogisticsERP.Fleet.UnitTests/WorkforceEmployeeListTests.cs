using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class WorkforceEmployeeListTests
{
    [Fact]
    public async Task EmployeeListIncludesExternalRidersAndDedicatedListRemainsAvailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var employee = CreatePerson("1234567890", "Employee", isEmployee: true, EmployeeRelationshipType.SponsoredInternal);
        var sponsoredRider = CreatePerson("2234567890", "Sponsored rider", isEmployee: false, EmployeeRelationshipType.SponsoredInternal);
        var outsideRider = CreatePerson("3234567890", "Outside rider", isEmployee: false, EmployeeRelationshipType.OutsideRider);
        dbContext.Employees.AddRange(employee, sponsoredRider, outsideRider);
        dbContext.RiderProfiles.AddRange(
            new RiderProfile { EmployeeId = sponsoredRider.Id },
            new RiderProfile { EmployeeId = outsideRider.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        await using var identityDbContext = CreateIdentityContext();
        var service = new WorkforceService(dbContext, identityDbContext, new TestCurrentUser());

        var employeeResult = await service.GetEmployeesAsync(cancellationToken);
        var externalRiderResult = await service.GetExternalRidersAsync(cancellationToken);

        Assert.True(employeeResult.IsSuccess);
        Assert.True(externalRiderResult.IsSuccess);
        var employees = employeeResult.Value!;
        var externalRiders = externalRiderResult.Value!;
        Assert.Equal(3, employees.Count);
        Assert.Contains(employees, item => item.Id == employee.Id);
        Assert.Contains(employees, item => item.Id == sponsoredRider.Id);
        var outsideItem = Assert.Single(employees, item => item.Id == outsideRider.Id);
        Assert.False(outsideItem.IsEmployee);
        Assert.Equal(nameof(EmployeeRelationshipType.OutsideRider), outsideItem.EngagementType);
        Assert.NotNull(outsideItem.RiderDetails);
        Assert.Equal(outsideRider.Id, Assert.Single(externalRiders).EmployeeId);
        Assert.Equal([outsideRider.Id], employees.Select(item => item.Id)
            .Intersect(externalRiders.Select(item => item.EmployeeId)));
    }

    [Fact]
    public async Task EmployeeListIncludesOnlyDistinctArabicNamesOfCurrentNonDeletedLicenses()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        await using var identityDbContext = CreateIdentityContext();
        var rider = CreatePerson("1234567890", "Rider", false, EmployeeRelationshipType.SponsoredInternal);
        var outsideRider = CreatePerson("2234567890", "Outside rider", false, EmployeeRelationshipType.OutsideRider);
        var employee = CreatePerson("3234567890", "Employee without licenses", true, EmployeeRelationshipType.SponsoredInternal);
        var privateCategory = new DriverLicenseCategory { Code = "PRIVATE", NameAr = "خصوصي", NameEn = "Private" };
        var motorcycleCategory = new DriverLicenseCategory { Code = "MOTORCYCLE", NameAr = "دراجة نارية", NameEn = "Motorcycle" };
        var historicalCategory = new DriverLicenseCategory { Code = "OLD", NameAr = "رخصة سابقة", NameEn = "Historical" };
        var deletedCategory = new DriverLicenseCategory { Code = "DELETED", NameAr = "رخصة محذوفة", NameEn = "Deleted", IsDeleted = true };
        dbContext.AddRange(rider, outsideRider, employee, privateCategory, motorcycleCategory, historicalCategory, deletedCategory,
            new RiderProfile { EmployeeId = rider.Id }, new RiderProfile { EmployeeId = outsideRider.Id },
            new EmployeeDriverLicense { EmployeeId = rider.Id, DriverLicenseCategoryId = privateCategory.Id, IsCurrent = true, LicenseStatus = DriverLicenseStatus.Active },
            new EmployeeDriverLicense { EmployeeId = rider.Id, DriverLicenseCategoryId = privateCategory.Id, IsCurrent = true, LicenseStatus = DriverLicenseStatus.Expired },
            new EmployeeDriverLicense { EmployeeId = rider.Id, DriverLicenseCategoryId = motorcycleCategory.Id, IsCurrent = true, LicenseStatus = DriverLicenseStatus.Expired },
            new EmployeeDriverLicense { EmployeeId = rider.Id, DriverLicenseCategoryId = historicalCategory.Id, IsCurrent = false },
            new EmployeeDriverLicense { EmployeeId = rider.Id, DriverLicenseCategoryId = historicalCategory.Id, IsCurrent = true, IsDeleted = true },
            new EmployeeDriverLicense { EmployeeId = rider.Id, DriverLicenseCategoryId = deletedCategory.Id, IsCurrent = true },
            new EmployeeDriverLicense { EmployeeId = outsideRider.Id, DriverLicenseCategoryId = privateCategory.Id, IsCurrent = true });
        await dbContext.SaveChangesAsync(ct);
        var service = new WorkforceService(dbContext, identityDbContext, new TestCurrentUser());

        var result = await service.GetEmployeesAsync(ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        var riderItem = Assert.Single(result.Value!, item => item.Id == rider.Id);
        Assert.Equal(2, riderItem.LicenseNamesAr.Count);
        Assert.Contains("خصوصي", riderItem.LicenseNamesAr);
        Assert.Contains("دراجة نارية", riderItem.LicenseNamesAr);
        Assert.Equal(["خصوصي"], Assert.Single(result.Value!, item => item.Id == outsideRider.Id).LicenseNamesAr);
        Assert.Empty(Assert.Single(result.Value!, item => item.Id == employee.Id).LicenseNamesAr);
        var json = System.Text.Json.JsonSerializer.SerializeToElement(riderItem, System.Text.Json.JsonSerializerOptions.Web);
        var names = json.GetProperty("licenseNamesAr").EnumerateArray().ToArray();
        Assert.Equal(2, names.Length);
        Assert.All(names, name => Assert.Equal(System.Text.Json.JsonValueKind.String, name.ValueKind));
    }

    [Fact]
    public async Task ExternalRiderDeletionArchivesProfileAndRetainsHistory()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        await using var identity = CreateIdentityContext();
        var employee = CreatePerson("1234567890", "External rider", false, EmployeeRelationshipType.OutsideRider);
        employee.RowVersion = [1, 2, 3];
        var rider = new RiderProfile { EmployeeId = employee.Id };
        db.AddRange(employee, rider);
        await db.SaveChangesAsync(ct);
        var service = new WorkforceService(db, identity, new TestCurrentUser());

        var result = await service.ArchiveExternalRiderAsync(employee.Id, new("Record no longer needed", "AQID"), ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Empty((await service.GetExternalRidersAsync(ct)).Value!);
        Assert.True((await service.GetExternalRiderAsync(employee.Id, ct)).IsFailure);
        var archived = await db.Employees.IgnoreQueryFilters().SingleAsync(ct);
        Assert.True(archived.IsDeleted);
        Assert.Equal(EmployeeStatus.Archived, archived.Status);
        Assert.Equal("Record no longer needed", archived.DeletionReason);
        Assert.Single(await db.RiderProfiles.ToArrayAsync(ct));
        Assert.Single(await db.EmployeeWorkHistory.ToArrayAsync(ct));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExternalRiderDeletionCannotArchiveOtherWorkforceRecords(bool isEmployee)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        await using var identity = CreateIdentityContext();
        var employee = CreatePerson("1234567890", "Other person", isEmployee, EmployeeRelationshipType.SponsoredInternal);
        employee.RowVersion = [1, 2, 3];
        db.AddRange(employee, new RiderProfile { EmployeeId = employee.Id });
        await db.SaveChangesAsync(ct);
        var service = new WorkforceService(db, identity, new TestCurrentUser());

        var result = await service.ArchiveExternalRiderAsync(employee.Id, new("Delete", "AQID"), ct);

        Assert.True(result.IsFailure);
        Assert.Equal(HrErrors.NotFound.Code, result.Error.Code);
        Assert.False(employee.IsDeleted);
    }

    [Theory]
    [InlineData(false, "STALE")]
    [InlineData(true, "AQID")]
    public async Task ExternalRiderDeletionRequiresCurrentVersionAndNoActiveVehicleAssignment(bool hasAssignment, string rowVersion)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        await using var identity = CreateIdentityContext();
        var employee = CreatePerson("1234567890", "External rider", false, EmployeeRelationshipType.OutsideRider);
        employee.RowVersion = [1, 2, 3];
        var rider = new RiderProfile { EmployeeId = employee.Id };
        db.AddRange(employee, rider);
        if (hasAssignment) db.RiderVehicleAssignments.Add(new LogisticsERP.Domain.Entities.Fleet.RiderVehicleAssignment
        {
            RiderProfileId = rider.Id, VehicleId = Guid.NewGuid(), StartedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync(ct);
        var service = new WorkforceService(db, identity, new TestCurrentUser());

        var result = await service.ArchiveExternalRiderAsync(employee.Id, new("Delete", rowVersion), ct);

        Assert.True(result.IsFailure);
        Assert.Equal(hasAssignment ? HrErrors.Conflict.Code : HrErrors.ConcurrencyConflict.Code, result.Error.Code);
        Assert.False(employee.IsDeleted);
    }

    private static Employee CreatePerson(
        string iqamaNo,
        string name,
        bool isEmployee,
        EmployeeRelationshipType engagementType) => new()
    {
        IqamaNo = iqamaNo,
        FullNameAr = name,
        IsEmployee = isEmployee,
        EngagementType = engagementType,
        Status = EmployeeStatus.Terminated
    };

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .ConfigureWarnings(options => options.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options,
        TimeProvider.System);

    private static IdentityDbContext CreateIdentityContext() => new(
        new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => null;
    }
}
