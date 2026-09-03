using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehicleReturnConditionApiSurfaceTests
{
    [Fact]
    public void ReturnContractExposesConditionalProblemFields()
    {
        Assert.Equal(typeof(VehicleConditionReportRequest),
            typeof(ReturnVehicleRequest).GetProperty(nameof(ReturnVehicleRequest.ConditionReport))!.PropertyType);
        Assert.Equal(typeof(VehicleIssueCategory),
            typeof(VehicleConditionReportRequest).GetProperty(nameof(VehicleConditionReportRequest.Category))!.PropertyType);
        Assert.Equal(typeof(VehicleIssueSeverity),
            typeof(VehicleConditionReportRequest).GetProperty(nameof(VehicleConditionReportRequest.Severity))!.PropertyType);
        Assert.Equal(typeof(bool),
            typeof(VehicleConditionReportRequest).GetProperty(nameof(VehicleConditionReportRequest.IsRiderResponsible))!.PropertyType);
        Assert.Equal(typeof(decimal),
            typeof(VehicleConditionReportRequest).GetProperty(nameof(VehicleConditionReportRequest.EstimatedRepairCost))!.PropertyType);
    }

    [Fact]
    public void MultipartReturnAcceptsEvidenceFiles()
    {
        var action = typeof(VehicleAssignmentsController).GetMethod(nameof(VehicleAssignmentsController.ReturnWithConditionReport));

        Assert.NotNull(action);
        Assert.Equal("return-with-condition-report", Assert.Single(action!.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>()).Template);
        Assert.Contains("multipart/form-data", Assert.Single(action.GetCustomAttributes(typeof(ConsumesAttribute), true).Cast<ConsumesAttribute>()).ContentTypes);
        Assert.Equal(typeof(List<IFormFile>), typeof(VehicleReturnMultipartForm).GetProperty(nameof(VehicleReturnMultipartForm.EvidenceFiles))!.PropertyType);
    }

    [Fact]
    public void IssueApiExposesEvidenceListingAndDownload()
    {
        Assert.NotNull(typeof(VehicleIssuesController).GetMethod(nameof(VehicleIssuesController.GetEvidence)));
        Assert.NotNull(typeof(VehicleIssuesController).GetMethod(nameof(VehicleIssuesController.DownloadEvidence)));
        Assert.NotNull(typeof(VehicleIssueSummaryResponse).GetProperty(nameof(VehicleIssueSummaryResponse.RelatedAssignmentId)));
        Assert.Equal(typeof(VehicleIssueRiderResponse),
            Nullable.GetUnderlyingType(typeof(VehicleIssueSummaryResponse).GetProperty(nameof(VehicleIssueSummaryResponse.Rider))!.PropertyType)
            ?? typeof(VehicleIssueSummaryResponse).GetProperty(nameof(VehicleIssueSummaryResponse.Rider))!.PropertyType);
        Assert.Equal(typeof(Guid),
            typeof(VehicleIssueRiderResponse).GetProperty(nameof(VehicleIssueRiderResponse.RiderProfileId))!.PropertyType);
        Assert.Equal(typeof(Guid),
            typeof(VehicleIssueRiderResponse).GetProperty(nameof(VehicleIssueRiderResponse.EmployeeId))!.PropertyType);
        Assert.Equal(typeof(string),
            typeof(VehicleIssueRiderResponse).GetProperty(nameof(VehicleIssueRiderResponse.RiderName))!.PropertyType);
        Assert.Equal(typeof(RealRiderResponse),
            Nullable.GetUnderlyingType(typeof(VehicleIssueRiderResponse).GetProperty(nameof(VehicleIssueRiderResponse.RealRider))!.PropertyType)
            ?? typeof(VehicleIssueRiderResponse).GetProperty(nameof(VehicleIssueRiderResponse.RealRider))!.PropertyType);
        Assert.NotNull(typeof(VehicleIssueSummaryResponse).GetProperty(nameof(VehicleIssueSummaryResponse.IsRiderResponsible)));
        Assert.NotNull(typeof(VehicleIssueSummaryResponse).GetProperty(nameof(VehicleIssueSummaryResponse.EstimatedRepairCost)));
    }

    [Fact]
    public void SwitchAcceptsTheOldVehicleConditionReportAndEvidenceFiles()
    {
        Assert.Equal(typeof(VehicleConditionReportRequest),
            typeof(SwitchVehicleRequest).GetProperty(nameof(SwitchVehicleRequest.ConditionReport))!.PropertyType);
        Assert.Equal(typeof(List<IFormFile>),
            typeof(VehicleSwitchMultipartForm).GetProperty(nameof(VehicleSwitchMultipartForm.EvidenceFiles))!.PropertyType);
    }
}
