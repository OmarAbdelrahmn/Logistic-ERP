using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class MissingModelApiSurfaceTests
{
    public static TheoryData<Type, string, Type, string> ProtectedEndpoints => new()
    {
        { typeof(CompanyProfileController), nameof(CompanyProfileController.Update), typeof(HttpPutAttribute), PermissionKeys.Catalog.CompanyProfileManage },
        { typeof(TagsController), nameof(TagsController.ReplaceAssignments), typeof(HttpPutAttribute), PermissionKeys.Catalog.TagsManage },
        { typeof(PlatformOperationsController), nameof(PlatformOperationsController.RotateCredential), typeof(HttpPostAttribute), PermissionKeys.Operations.PlatformCredentialsRotate },
        { typeof(UsersController), nameof(UsersController.IssueTemporaryCredential), typeof(HttpPostAttribute), PermissionKeys.Security.UsersUpdate },
        { typeof(SupportAccessController), nameof(SupportAccessController.Resolve), typeof(HttpPostAttribute), PermissionKeys.Security.SupportAccessManage },
        { typeof(HrWorkflowsController), nameof(HrWorkflowsController.ResolveCancellation), typeof(HttpPostAttribute), PermissionKeys.Workflows.LeaveRequestsApprove },
        { typeof(LeaveDocumentsController), nameof(LeaveDocumentsController.Download), typeof(HttpGetAttribute), PermissionKeys.Documents.DownloadSensitive },
        { typeof(RiderDocumentsController), nameof(RiderDocumentsController.AjeerContract), typeof(HttpPostAttribute), PermissionKeys.Documents.Upload },
        { typeof(ExportsController), nameof(ExportsController.Create), typeof(HttpPostAttribute), PermissionKeys.Reporting.ExportsCreate },
        { typeof(AuditEntriesController), nameof(AuditEntriesController.Query), typeof(HttpGetAttribute), PermissionKeys.Security.AuditRead },
        { typeof(DatasetVersionsController), nameof(DatasetVersionsController.Get), typeof(HttpGetAttribute), PermissionKeys.Reporting.ReportsRead }
    };

    [Fact]
    public void NewPermissionKeysAreUniqueAndRegistered()
    {
        string[] expected =
        [
            PermissionKeys.Catalog.CompanyProfileRead,
            PermissionKeys.Catalog.CompanyProfileManage,
            PermissionKeys.Catalog.TagsRead,
            PermissionKeys.Catalog.TagsManage,
            PermissionKeys.Documents.CatalogManage,
            PermissionKeys.Operations.PlatformCredentialsRead,
            PermissionKeys.Operations.PlatformCredentialsRotate
        ];

        Assert.Equal(PermissionKeys.All.Count, PermissionKeys.All.Distinct(StringComparer.Ordinal).Count());
        Assert.All(expected, permission => Assert.Contains(permission, (IEnumerable<string>)PermissionKeys.All));
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public void SensitiveEndpointsUseExpectedVerbAndPermission(
        Type controllerType,
        string actionName,
        Type verbAttributeType,
        string permissionKey)
    {
        var action = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        Assert.Contains(action.GetCustomAttributes(), attribute => attribute.GetType() == verbAttributeType);

        var permissionAttributes = action.GetCustomAttributes<RequirePermissionAttribute>(true)
            .Concat(controllerType.GetCustomAttributes<RequirePermissionAttribute>(true));
        Assert.Contains(permissionAttributes, attribute =>
            attribute.Policy?.EndsWith(permissionKey, StringComparison.Ordinal) == true);
    }
}
