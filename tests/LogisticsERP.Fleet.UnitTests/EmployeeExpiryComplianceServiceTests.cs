using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class EmployeeExpiryComplianceServiceTests
{
    private static readonly DateOnly CheckDate = new(2026, 9, 20);

    [Fact]
    public async Task GetExpiriesReturnsMissingItemForRiderRequiredDocumentThatHasNotBeenUploaded()
    {
        var databaseName = Guid.NewGuid().ToString();
        var applicationOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        await using var applicationDbContext = new ApplicationDbContext(applicationOptions);
        await using var identityDbContext = new IdentityDbContext(identityOptions);

        var rider = new Employee
        {
            FullNameAr = "Rider",
            Status = EmployeeStatus.Active,
            EngagementType = EmployeeRelationshipType.SponsoredInternal
        };
        var riderProfile = new RiderProfile { EmployeeId = rider.Id };
        var documentType = new DocumentType
        {
            Code = "RIDER_REQUIRED_DOCUMENT",
            NameAr = "وثيقة مطلوبة للرايدر",
            NameEn = "Required rider document",
            AppliesToRiderProfile = true,
            Status = CatalogStatus.Active
        };
        applicationDbContext.AddRange(rider, riderProfile, documentType, new DocumentRequirement
        {
            DocumentTypeId = documentType.Id,
            AppliesToRiderProfile = true,
            IsRequired = true,
            EffectiveFrom = CheckDate.AddDays(-1),
            Status = CatalogStatus.Active
        });
        await applicationDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new EmployeeExpiryComplianceService(
            applicationDbContext,
            identityDbContext,
            new AllowAllPermissionChecker(),
            TimeProvider.System);

        var result = await service.GetExpiriesAsync(new EmployeeExpiryComplianceQuery(
            CheckDate, rider.Id, null, null, null, null, null, null), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(rider.Id, item.EmployeeId);
        Assert.Equal(riderProfile.Id, item.RiderProfileId);
        Assert.Equal(documentType.Id, item.SourceId);
        Assert.Equal(documentType.Code, item.CategoryCode);
        Assert.Equal("Missing", item.SourceStatus);
        Assert.Null(item.ExpiryDate);
        Assert.Null(item.EmployeeDocumentId);
        Assert.Equal(EmployeeExpiryComplianceDueStatus.Missing, item.DueStatus);
    }

    private sealed class AllowAllPermissionChecker : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public void InvalidateUser(Guid userId, long authorizationVersion)
        {
        }
    }
}
