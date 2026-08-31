using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class StaffDocumentApiSurfaceTests
{
    public static TheoryData<Type, string, string> ProtectedEndpoints => new()
    {
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.GetAll), PermissionKeys.Documents.Read },
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.Checklist), PermissionKeys.Documents.Read },
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.Upload), PermissionKeys.Documents.Upload },
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.UploadVersion), PermissionKeys.Documents.Upload },
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.UpdateMetadata), PermissionKeys.Documents.Upload },
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.Versions), PermissionKeys.Documents.Read },
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.Download), PermissionKeys.Documents.DownloadSensitive },
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.Preview), PermissionKeys.Documents.DownloadSensitive },
        { typeof(EmployeeDocumentsController), nameof(EmployeeDocumentsController.Archive), PermissionKeys.Documents.Upload },
        { typeof(RiderDocumentsController), nameof(RiderDocumentsController.Checklist), PermissionKeys.Documents.Read },
        { typeof(RiderDocumentsController), nameof(RiderDocumentsController.UploadCustom), PermissionKeys.Documents.Upload }
    };

    [Fact]
    public void EmployeeDocumentEndpointsAreNotAnonymous()
    {
        Assert.DoesNotContain(
            typeof(EmployeeDocumentsController).GetCustomAttributes(),
            attribute => attribute is AllowAnonymousAttribute);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public void StaffDocumentEndpointsRequireExpectedPermission(Type controllerType, string actionName, string permission)
    {
        var action = controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        Assert.Contains(action!.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ChecklistRoutesAreAvailableForEmployeesAndRiders()
    {
        var employeeAction = typeof(EmployeeDocumentsController).GetMethod(nameof(EmployeeDocumentsController.Checklist));
        var riderAction = typeof(RiderDocumentsController).GetMethod(nameof(RiderDocumentsController.Checklist));

        Assert.Equal("checklist", Assert.Single(employeeAction!.GetCustomAttributes<HttpGetAttribute>()).Template);
        Assert.Equal("checklist", Assert.Single(riderAction!.GetCustomAttributes<HttpGetAttribute>()).Template);
    }

    [Fact]
    public void RiderControllerSupportsCustomDocumentTypeUploads()
    {
        var action = typeof(RiderDocumentsController).GetMethod(nameof(RiderDocumentsController.UploadCustom));
        Assert.NotNull(action);
        var post = Assert.Single(action!.GetCustomAttributes<HttpPostAttribute>());

        Assert.Null(post.Template);
        Assert.Contains(action!.GetParameters(), parameter => parameter.ParameterType == typeof(EmployeeDocumentUploadForm));
    }

    [Fact]
    public void ChecklistContractExposesDefinitionRulesAssignmentAndDocuments()
    {
        var properties = typeof(EmployeeDocumentChecklistItemResponse)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(
            [
                "DocumentTypeId", "DocumentTypeCode", "DocumentTypeNameAr", "DocumentTypeNameEn",
                "RequiresNumber", "RequiresIssueDate", "RequiresExpiryDate", "RequiresFile", "IsRequired",
                "ReminderOffsetsDays", "FulfillmentStatus", "MissingFields", "Documents"
            ],
            properties);
    }

    [Fact]
    public void DocumentTypeRequestSupportsDynamicNumberExpiryFileAndAudienceRules()
    {
        var properties = typeof(DocumentTypeUpsertRequest).GetProperties().Select(property => property.Name).ToHashSet();

        Assert.Contains("RequiresNumber", properties);
        Assert.Contains("RequiresExpiryDate", properties);
        Assert.Contains("RequiresFile", properties);
        Assert.Contains("AppliesToSponsoredInternal", properties);
        Assert.Contains("AppliesToOutsideRider", properties);
        Assert.Contains("AppliesToRiderProfile", properties);
    }

    [Fact]
    public void RequirementRequestSupportsGlobalAndRiderAssignments()
    {
        var relationshipType = typeof(DocumentRequirementUpsertRequest).GetProperty("RelationshipType");
        var riderScope = typeof(DocumentRequirementUpsertRequest).GetProperty("AppliesToRiderProfile");

        Assert.NotNull(relationshipType);
        Assert.True(relationshipType!.PropertyType == typeof(string));
        Assert.NotNull(riderScope);
        Assert.True(riderScope!.PropertyType == typeof(bool));
    }
}
