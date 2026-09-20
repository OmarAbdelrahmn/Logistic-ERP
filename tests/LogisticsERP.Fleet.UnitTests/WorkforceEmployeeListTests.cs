using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class WorkforceEmployeeListTests
{
    [Fact]
    public async Task EmployeeAndExternalRiderListsDoNotOverlap()
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
        var service = new WorkforceService(dbContext, new TestCurrentUser());

        var employeeResult = await service.GetEmployeesAsync(cancellationToken);
        var externalRiderResult = await service.GetExternalRidersAsync(cancellationToken);

        Assert.True(employeeResult.IsSuccess);
        Assert.True(externalRiderResult.IsSuccess);
        var employees = employeeResult.Value!;
        var externalRiders = externalRiderResult.Value!;
        Assert.Equal(2, employees.Count);
        Assert.Contains(employees, item => item.Id == employee.Id);
        Assert.Contains(employees, item => item.Id == sponsoredRider.Id);
        Assert.DoesNotContain(employees, item => item.Id == outsideRider.Id);
        Assert.Equal(outsideRider.Id, Assert.Single(externalRiders).EmployeeId);
        Assert.Empty(employees.Select(item => item.Id)
            .Intersect(externalRiders.Select(item => item.EmployeeId)));
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

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => null;
    }
}
