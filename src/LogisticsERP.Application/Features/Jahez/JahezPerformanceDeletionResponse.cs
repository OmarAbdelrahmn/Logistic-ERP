namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezPerformanceDeletionResponse(DateOnly ReportDate, Guid? SponsorId, int PerformanceRowsDeleted);
