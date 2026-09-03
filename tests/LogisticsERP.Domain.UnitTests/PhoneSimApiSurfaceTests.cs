using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Telecom;
using LogisticsERP.Domain.Entities.Telecom;
using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class PhoneSimApiSurfaceTests
{
    public static TheoryData<string, Type, string?, string> Endpoints => new()
    {
        { nameof(PhoneSimsController.GetAll), typeof(HttpGetAttribute), null, PermissionKeys.Operations.PhoneSimsRead },
        { nameof(PhoneSimsController.Get), typeof(HttpGetAttribute), "{id:guid}", PermissionKeys.Operations.PhoneSimsRead },
        { nameof(PhoneSimsController.DownloadReceiptForm), typeof(HttpGetAttribute), "{id:guid}/receipt-form", PermissionKeys.Operations.PhoneSimsRead },
        { nameof(PhoneSimsController.Create), typeof(HttpPostAttribute), null, PermissionKeys.Operations.PhoneSimsManage },
        { nameof(PhoneSimsController.Update), typeof(HttpPutAttribute), "{id:guid}", PermissionKeys.Operations.PhoneSimsManage },
        { nameof(PhoneSimsController.ChangeResponsibleEmployee), typeof(HttpPatchAttribute), "{id:guid}/responsible-employee", PermissionKeys.Operations.PhoneSimsManage },
        { nameof(PhoneSimsController.ChangeStatus), typeof(HttpPatchAttribute), "{id:guid}/status", PermissionKeys.Operations.PhoneSimsManage },
        { nameof(PhoneSimsController.Archive), typeof(HttpPatchAttribute), "{id:guid}/archive", PermissionKeys.Operations.PhoneSimsManage },
        { nameof(PhoneSimsController.GetResponsibilityHistory), typeof(HttpGetAttribute), "{id:guid}/responsibility-history", PermissionKeys.Operations.PhoneSimsRead },
        { nameof(PhoneSimsController.GetAssignments), typeof(HttpGetAttribute), "{id:guid}/assignments", PermissionKeys.Operations.PhoneSimsRead },
        { nameof(PhoneSimsController.Assign), typeof(HttpPostAttribute), "{id:guid}/assignments", PermissionKeys.Operations.PhoneSimsManage },
        { nameof(PhoneSimsController.CloseAssignment), typeof(HttpPostAttribute), "{id:guid}/assignments/{assignmentId:guid}/close", PermissionKeys.Operations.PhoneSimsManage }
    };

    [Fact]
    public void ControllerUsesPhoneSimRoute()
    {
        var route = Assert.Single(typeof(PhoneSimsController).GetCustomAttributes(typeof(RouteAttribute), true));
        Assert.Equal("api/phone-sims", Assert.IsType<RouteAttribute>(route).Template);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointsUseExpectedVerbRouteAndPermission(
        string methodName,
        Type verbType,
        string? routeTemplate,
        string permission)
    {
        var method = typeof(PhoneSimsController).GetMethod(methodName);
        Assert.NotNull(method);
        var verb = Assert.Single(method!.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(routeTemplate, verb.Template);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ContractsExposeConcurrencyAndDistinctResponsibleAndRiderIdentities()
    {
        Assert.NotNull(typeof(PhoneSimResponse).GetProperty(nameof(PhoneSimResponse.RowVersion)));
        Assert.NotNull(typeof(UpdatePhoneSimRequest).GetProperty(nameof(UpdatePhoneSimRequest.RowVersion)));
        Assert.NotNull(typeof(AssignPhoneSimRequest).GetProperty(nameof(AssignPhoneSimRequest.RowVersion)));
        Assert.NotNull(typeof(ClosePhoneSimAssignmentRequest).GetProperty(nameof(ClosePhoneSimAssignmentRequest.RowVersion)));
        Assert.NotNull(typeof(PhoneSimResponse).GetProperty(nameof(PhoneSimResponse.ResponsibleEmployeeId)));
        Assert.NotNull(typeof(PhoneSimResponse).GetProperty(nameof(PhoneSimResponse.CurrentRider)));
        Assert.NotNull(typeof(PhoneSimCurrentRiderResponse).GetProperty(nameof(PhoneSimCurrentRiderResponse.RiderProfileId)));
        Assert.NotNull(typeof(PhoneSimCurrentRiderResponse).GetProperty(nameof(PhoneSimCurrentRiderResponse.EmployeeId)));
        Assert.NotNull(typeof(PhoneSimResponse).GetProperty(nameof(PhoneSimResponse.ReceiptForm)));
        Assert.NotNull(typeof(CreatePhoneSimForm).GetProperty(nameof(CreatePhoneSimForm.ReceiptForm)));
    }

    [Fact]
    public void CreateAcceptsMultipartFormData()
    {
        var method = typeof(PhoneSimsController).GetMethod(nameof(PhoneSimsController.Create));
        Assert.NotNull(method);

        var formParameter = Assert.Single(method!.GetParameters(), parameter =>
            parameter.ParameterType == typeof(CreatePhoneSimForm));
        Assert.NotNull(formParameter.GetCustomAttribute<FromFormAttribute>());

        var consumes = Assert.Single(method.GetCustomAttributes<ConsumesAttribute>());
        Assert.Contains("multipart/form-data", consumes.ContentTypes);
    }

    [Fact]
    public void DomainSeparatesCurrentResponsibilityFromRiderAssignmentHistory()
    {
        Assert.Equal(typeof(Guid), typeof(PhoneSimCard).GetProperty(nameof(PhoneSimCard.ResponsibleEmployeeId))!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(RiderPhoneSimAssignment).GetProperty(nameof(RiderPhoneSimAssignment.RiderProfileId))!.PropertyType);
        Assert.Equal(typeof(Guid?), typeof(PhoneSimResponsibilityChange).GetProperty(nameof(PhoneSimResponsibilityChange.PreviousResponsibleEmployeeId))!.PropertyType);
        Assert.Equal(PhoneSimStatus.Available, new PhoneSimCard().Status);
    }

    [Fact]
    public void PhoneSimPermissionsAreRegistered()
    {
        Assert.Contains(PermissionKeys.Operations.PhoneSimsRead, (IEnumerable<string>)PermissionKeys.All);
        Assert.Contains(PermissionKeys.Operations.PhoneSimsManage, (IEnumerable<string>)PermissionKeys.All);
    }
}
