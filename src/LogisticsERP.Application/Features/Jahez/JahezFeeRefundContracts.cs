using LogisticsERP.Application.Features.Hr;

namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezFeeRefundPreviewRequest(Guid HandoverId, bool WaiveCommission);
public sealed record JahezFeeRefundCreate(Guid HandoverId, bool WaiveCommission, string Reference, string Reason,
    PlatformAccountDayUsageInput? DayUsage = null);
public sealed record JahezFeeRefundPreview(Guid HandoverId, DateTimeOffset ExpiresAtUtc, decimal GrossFeeRefund,
    decimal CancelledUnpaidFee, decimal CalculatedCommission, bool WaiveCommission, decimal DeductedCommission,
    decimal RefundAmount, string CalculationJson);
