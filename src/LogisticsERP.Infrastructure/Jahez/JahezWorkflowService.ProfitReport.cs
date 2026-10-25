using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public Task<Result<JahezProfitReport>> ProfitReportAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default) =>
        ReadAsync(JahezTreasuryPermissions.ReportsRead, async () =>
        {
            var today = Today;
            var from = fromDate ?? new DateOnly(today.Year, today.Month, 1);
            var to = toDate ?? today;
            Require(from > DateOnly.MinValue && to < DateOnly.MaxValue && from <= to,
                JahezErrors.Invalid("نطاق التاريخ غير صالح."));
            var fromUtc = JahezRules.StartOfDay(from).ToUniversalTime();
            var toExclusiveUtc = JahezRules.StartOfDay(to).ToUniversalTime().AddDays(1);

            // Count actual opening-fee receipts, including partial payments and handed-over funds.
            var fees = await db.JahezMoneyMovements.AsNoTracking()
                .Where(x => x.Purpose == JahezMoneyPurpose.AccountFee && x.Direction == JahezMoneyDirection.Receipt
                    && x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toExclusiveUtc)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

            // Refunds reverse the entire collected fee. Retained commission is profit in its own bucket.
            var refundedFees = await db.JahezAccountFeeRefunds.AsNoTracking()
                .Where(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toExclusiveUtc)
                .SumAsync(x => (decimal?)x.GrossFeeRefund, ct) ?? 0m;
            var refundCommission = await db.JahezAccountFeeRefunds.AsNoTracking()
                .Where(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toExclusiveUtc)
                .SumAsync(x => (decimal?)x.DeductedCommission, ct) ?? 0m;

            // Commission belongs to the executed settlement, even when netting creates no receipt
            // or transfers the amount to rider debt. Use the saved approved value exactly once.
            var commission = await db.JahezOperations.AsNoTracking()
                .Where(x => x.Status == JahezOperationStatus.Executed && x.Mode != JahezOperationMode.Withdraw
                    && x.ExecutedAtUtc >= fromUtc && x.ExecutedAtUtc < toExclusiveUtc)
                .SumAsync(x => (decimal?)x.Commission, ct) ?? 0m;

            return new JahezProfitReport(from, to, fees - refundedFees, commission + refundCommission);
        }, ct);
}
