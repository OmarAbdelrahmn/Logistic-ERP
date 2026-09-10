using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.SystemServices;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class AccidentWorkflowTests
{
    [Fact]
    public void AccidentCanBeCreatedWithoutOptionalDescriptiveFields()
    {
        var now = DateTimeOffset.Parse("2026-09-08T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var request = new CreateVehicleAccidentRequest(
            "ACC-123", Guid.NewGuid(), Guid.NewGuid(), now.AddMinutes(-5),
            LocationDescription: null, Latitude: null, Longitude: null,
            PoliceReportNumber: "NAJM-123", InsuranceClaimNumber: null,
            Severity: VehicleAccidentSeverity.Minor, IsDrivable: true,
            HasInjuries: false, InjuryDetails: null, ThirdPartyDetails: null,
            DamageDescription: null, FaultAssessment: null, Narrative: null);

        Assert.True(VehicleAccidentService.IsValidCreateRequest(request, now));
    }

    [Fact]
    public void InjuryDetailsRemainRequiredWhenAccidentHasInjuries()
    {
        var now = DateTimeOffset.Parse("2026-09-08T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var request = new CreateVehicleAccidentRequest(
            "ACC-124", Guid.NewGuid(), Guid.NewGuid(), now.AddMinutes(-5),
            LocationDescription: null, Latitude: null, Longitude: null,
            PoliceReportNumber: "NAJM-123", InsuranceClaimNumber: null,
            Severity: VehicleAccidentSeverity.Moderate, IsDrivable: false,
            HasInjuries: true, InjuryDetails: null, ThirdPartyDetails: null,
            DamageDescription: null, FaultAssessment: null, Narrative: null);

        Assert.False(VehicleAccidentService.IsValidCreateRequest(request, now));
    }

    [Fact]
    public void UserEnteredAccidentNumberIsRequired()
    {
        var now = DateTimeOffset.Parse("2026-09-08T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var request = new CreateVehicleAccidentRequest(
            " ", Guid.NewGuid(), Guid.NewGuid(), now.AddMinutes(-5),
            LocationDescription: null, Latitude: null, Longitude: null,
            PoliceReportNumber: "NAJM-123", InsuranceClaimNumber: null,
            Severity: VehicleAccidentSeverity.Minor, IsDrivable: true,
            HasInjuries: false, InjuryDetails: null, ThirdPartyDetails: null,
            DamageDescription: null, FaultAssessment: null, Narrative: null);

        Assert.False(VehicleAccidentService.IsValidCreateRequest(request, now));
    }

    [Theory]
    [InlineData(100, 0, true)]
    [InlineData(75, 25, true)]
    [InlineData(0, 100, true)]
    [InlineData(101, -1, false)]
    [InlineData(75, 24, false)]
    [InlineData(-1, 101, false)]
    public void FaultSharesMustAccountForExactlyOneHundredPercent(decimal rider, decimal other, bool expected) =>
        Assert.Equal(expected, AccidentWorkflowRules.ValidFaultShares(rider, [other]));

    [Fact]
    public void MultiplePartiesAndDecimalSharesAreSupportedWithoutRoundingAwayErrors()
    {
        Assert.True(AccidentWorkflowRules.ValidFaultShares(33.33m, [33.33m, 33.34m]));
        Assert.False(AccidentWorkflowRules.ValidFaultShares(33.333m, [66.667m]));
    }

    [Theory]
    [InlineData(100, VehicleAccidentSeverity.Serious, true)]
    [InlineData(100, VehicleAccidentSeverity.Minor, false)]
    [InlineData(75, VehicleAccidentSeverity.Serious, false)]
    [InlineData(0, VehicleAccidentSeverity.Critical, false)]
    public void OnlyFullyResponsibleNonMinorClaimsRequireOpeningFee(decimal rider, VehicleAccidentSeverity severity, bool required) =>
        Assert.Equal(required, AccidentWorkflowRules.RequiresOpeningFee(rider, severity));

    [Theory]
    [InlineData(AccidentCaseStage.AwaitingNajm, AccidentWorkflowAction.OpenClaim)]
    [InlineData(AccidentCaseStage.ClaimDraft, AccidentWorkflowAction.ConfirmTransfer)]
    [InlineData(AccidentCaseStage.AwaitingAssessment, AccidentWorkflowAction.CompleteRepair)]
    [InlineData(AccidentCaseStage.TotalLossProposed, AccidentWorkflowAction.RecordVehicleCollection)]
    [InlineData(AccidentCaseStage.AwaitingInsurance, AccidentWorkflowAction.SubmitToSupplier)]
    [InlineData(AccidentCaseStage.InsuranceRejected, AccidentWorkflowAction.ApproveInsurance)]
    public void CannotSkipRequiredWorkflowStages(AccidentCaseStage stage, AccidentWorkflowAction action) =>
        Assert.Null(AccidentWorkflowRules.NextStage(stage, action));

    [Fact]
    public void ReinspectionCanConfirmTotalLossOrRedirectToRepair()
    {
        Assert.Equal(AccidentCaseStage.TotalLossConfirmed, AccidentWorkflowRules.NextStage(AccidentCaseStage.AwaitingReinspection, AccidentWorkflowAction.ConfirmTotalLoss));
        Assert.Equal(AccidentCaseStage.RepairDirected, AccidentWorkflowRules.NextStage(AccidentCaseStage.AwaitingReinspection, AccidentWorkflowAction.ReceiveRepairDirection));
    }

    [Fact]
    public void IncidentPeriodUsesInclusiveRiyadhCalendarDates()
    {
        var start = DateTimeOffset.Parse("2026-08-01T20:59:00Z", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(1, AccidentWorkflowRules.IncidentCalendarDays(start, start));
        Assert.Equal(2, AccidentWorkflowRules.IncidentCalendarDays(start, start.AddMinutes(2)));
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("binary/octet-stream")]
    [InlineData("application/x-pdf")]
    [InlineData("application/pdf; charset=binary")]
    public void NajmPdfContentTypeIsNormalizedBeforeSecureFileInspection(string suppliedContentType)
    {
        var upload = new PrivateFileUpload(Stream.Null, "najm-report.pdf", suppliedContentType, 1);

        var normalized = VehicleAccidentService.NormalizeEvidenceContentType(upload);

        Assert.Equal("application/pdf", normalized.ContentType);
    }

    [Theory]
    [InlineData("najm-report.jpg", "image/jpeg")]
    [InlineData("najm-report.jpeg", "image/jpeg")]
    [InlineData("najm-report.png", "image/png")]
    [InlineData("najm-report.webp", "image/webp")]
    public void NajmImageContentTypeIsNormalizedBeforeSecureFileInspection(string fileName, string expectedContentType)
    {
        var upload = new PrivateFileUpload(Stream.Null, fileName, "application/octet-stream", 1);

        var normalized = VehicleAccidentService.NormalizeEvidenceContentType(upload);

        Assert.Equal(expectedContentType, normalized.ContentType);
        Assert.True(VehicleAccidentService.IsAllowedEvidenceContentType(
            VehicleAccidentEvidenceType.NajmReport, normalized.ContentType));
    }

    [Fact]
    public void UnsupportedExtensionIsNotDisguisedAsPdf()
    {
        var upload = new PrivateFileUpload(Stream.Null, "najm-report.exe", "application/octet-stream", 1);

        var normalized = VehicleAccidentService.NormalizeEvidenceContentType(upload);

        Assert.Equal("application/octet-stream", normalized.ContentType);
    }

    [Theory]
    [InlineData(VehicleAccidentEvidenceType.NajmReport, "application/pdf", true)]
    [InlineData(VehicleAccidentEvidenceType.NajmReport, "image/jpeg", true)]
    [InlineData(VehicleAccidentEvidenceType.NajmReport, "image/png", true)]
    [InlineData(VehicleAccidentEvidenceType.NajmReport, "text/plain", false)]
    [InlineData(VehicleAccidentEvidenceType.DamagePhoto, "image/webp", true)]
    [InlineData(VehicleAccidentEvidenceType.DamagePhoto, "application/pdf", false)]
    [InlineData(VehicleAccidentEvidenceType.ClaimSubmissionReport, "image/jpeg", true)]
    [InlineData(VehicleAccidentEvidenceType.PaymentReceipt, "image/png", true)]
    [InlineData(VehicleAccidentEvidenceType.InsuranceDecision, "image/webp", true)]
    [InlineData(VehicleAccidentEvidenceType.TransferReceipt, "application/pdf", true)]
    [InlineData(VehicleAccidentEvidenceType.TransferReceipt, "text/plain", false)]
    public void EvidenceTypeOnlyAcceptsItsSupportedFileKinds(
        VehicleAccidentEvidenceType type, string contentType, bool expected)
    {
        Assert.Equal(expected, VehicleAccidentService.IsAllowedEvidenceContentType(type, contentType));
    }

    [Fact]
    public void NotificationCursorPreservesTimeAndTieBreakerAndAcceptsLegacyCursors()
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        Assert.True(NotificationService.TryParseCursor($"{now.UtcTicks}:{id:N}", out var before, out var parsed));
        Assert.Equal(now, before); Assert.Equal(id, parsed);
        Assert.True(NotificationService.TryParseCursor(now.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture), out _, out var legacy));
        Assert.Null(legacy);
        Assert.False(NotificationService.TryParseCursor("9223372036854775807", out _, out _));
        Assert.False(NotificationService.TryParseCursor($"{now.UtcTicks}:invalid", out _, out _));
    }
}
