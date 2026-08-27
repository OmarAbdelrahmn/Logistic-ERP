using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class EmployeeExpiryComplianceModelTests
{
    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=EmployeeExpiryComplianceModelTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    [Theory]
    [InlineData(typeof(EmployeeDocument), "Status", "ExpiryDate", "EmployeeId")]
    [InlineData(typeof(EmployeeDriverLicense), "IsCurrent", "ExpiryDate", "EmployeeId")]
    [InlineData(typeof(RiderCard), "IsCurrent", "ExpiryDate", "RiderProfileId")]
    [InlineData(typeof(RiderHealthCard), "IsCurrent", "ExpiryDate", "RiderProfileId")]
    [InlineData(typeof(EmployeeMedicalInsurancePolicy), "IsCurrent", "EndDate", "EmployeeId")]
    public void ExpiryQueriesHaveFilteredIndexes(Type entityType, params string[] propertyNames)
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(entityType)!;
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        Assert.Equal("[IsDeleted] = 0", index.GetFilter());
    }
}
