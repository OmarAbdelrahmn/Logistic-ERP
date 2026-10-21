using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public Task<Result<TreasuryExport>> ExportTreasuryReportAsync(string type, string format, TreasuryFilter filter,
        Guid? handoverId = null, CancellationToken ct = default) =>
        ReadAsync(JahezTreasuryPermissions.ReportsExport, async () =>
        {
            await AuthorizeAsync(JahezTreasuryPermissions.ReportsRead, ct);
            Require(format is "xlsx" or "pdf", JahezErrors.Invalid("اختر xlsx أو pdf."));
            Require(treasuryRenderer is not null, JahezErrors.Conflict("محرك تصدير التقارير غير مهيأ."));
            var report = await BuildTreasuryReport(type, filter, handoverId, true, ct);
            ct.ThrowIfCancellationRequested();
            return treasuryRenderer!.Render(report, format);
        }, ct);
}
