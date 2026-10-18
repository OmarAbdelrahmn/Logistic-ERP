using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private IQueryable<JahezMoneyMovement> OutstandingMoneyMovements() =>
        db.JahezMoneyMovements.AsNoTracking().Where(m => !db.JahezCashboxHandoverMovements.Any(link =>
            link.MoneyMovementId == m.Id && db.JahezCashboxHandovers.Any(h =>
                h.Id == link.CashboxHandoverId && h.Status == JahezCashboxHandoverStatus.Confirmed)));

    public Task<Result<JahezPage<JahezCashboxHandover>>> CashboxHandoversV2Async(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, () => WorkflowPage(db.JahezCashboxHandovers, page, pageSize, ct), ct);

    public Task<Result<JahezCashboxHandover>> CashboxHandoverV2Async(Guid id, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, async () =>
        {
            var h = await db.JahezCashboxHandovers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(h is not null, JahezErrors.NotFound);
            return h!;
        }, ct);

    public Task<Result<JahezCashboxHandover>> SubmitCashboxHandoverV2Async(string key, JahezCashboxHandoverCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.cashbox.submit", request, PermissionKeys.Jahez.CashboxSubmit, async () =>
        {
            Reason(request.Reason);
            Require(request.BusinessDate <= Today, JahezErrors.Invalid("تاريخ تسليم الأموال لا يمكن أن يكون في المستقبل."));
            Require(!await db.JahezCashboxHandovers.AnyAsync(x => x.Status == JahezCashboxHandoverStatus.Pending, ct),
                JahezErrors.Conflict("يوجد تسليم أموال معلق؛ أكده أو ألغِه أولاً."));
            var cutoff = request.BusinessDate == DateOnly.MinValue ? DateTimeOffset.MinValue.AddHours(21)
                : JahezRules.StartOfDay(request.BusinessDate).ToUniversalTime().AddDays(1);
            var movements = await OutstandingMoneyMovements().Where(x => x.OccurredAtUtc < cutoff)
                .Select(x => new { x.Id, x.Fund, x.Direction, x.Amount }).ToArrayAsync(ct);
            Require(movements.Length > 0, JahezErrors.Conflict("لا توجد حركات أموال غير مسلمة حتى هذا التاريخ."));
            decimal Net(JahezMoneyFund fund) => movements.Where(x => x.Fund == fund)
                .Sum(x => x.Direction == JahezMoneyDirection.Receipt ? x.Amount : -x.Amount);
            var h = new JahezCashboxHandover
            {
                BusinessDate = request.BusinessDate, RequestedByUserId = Actor, Reason = request.Reason,
                CashboxAmount = Net(JahezMoneyFund.Cashbox), PetroAppAmount = Net(JahezMoneyFund.PetroApp),
                BankAmount = Net(JahezMoneyFund.Bank), MovementCount = movements.Length
            };
            db.Add(h);
            db.AddRange(movements.Select(x => new JahezCashboxHandoverMovement { CashboxHandoverId = h.Id, MoneyMovementId = x.Id }));
            return h;
        }, ct);

    public Task<Result<JahezCashboxHandover>> ConfirmCashboxHandoverV2Async(string key, Guid id,
        JahezCashboxHandoverConfirm request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.cashbox.confirm", new { id, request }, PermissionKeys.Jahez.CashboxConfirm, async () =>
        {
            Reason(request.Reason);
            var h = await PendingCashboxHandover(id, request.RowVersion, ct);
            Require(h.RequestedByUserId != Actor, JahezErrors.Forbidden);
            Require(h.CashboxAmount == request.CashboxAmount && h.PetroAppAmount == request.PetroAppAmount && h.BankAmount == request.BankAmount,
                JahezErrors.Invalid("يجب مطابقة مبلغ كل جهة بشكل مستقل مع التسليم المسجل."));
            h.Status = JahezCashboxHandoverStatus.Confirmed;
            h.AccountantUserId = Actor; h.ConfirmedAtUtc = Now; h.ConfirmationReason = request.Reason;
            return h;
        }, ct);

    public Task<Result<JahezCashboxHandover>> CancelCashboxHandoverV2Async(string key, Guid id,
        JahezCashboxHandoverCancel request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.cashbox.cancel", new { id, request }, PermissionKeys.Jahez.CashboxSubmit, async () =>
        {
            Reason(request.Reason);
            var h = await PendingCashboxHandover(id, request.RowVersion, ct);
            Require(h.RequestedByUserId == Actor, JahezErrors.Forbidden);
            h.Status = JahezCashboxHandoverStatus.Cancelled;
            h.CancelledAtUtc = Now; h.CancellationReason = request.Reason;
            return h;
        }, ct);

    private async Task<JahezCashboxHandover> PendingCashboxHandover(Guid id, string rowVersion, CancellationToken ct)
    {
        var h = await db.JahezCashboxHandovers.SingleOrDefaultAsync(x => x.Id == id, ct);
        Require(h is not null, JahezErrors.NotFound);
        Require(h!.Status == JahezCashboxHandoverStatus.Pending, JahezErrors.Conflict("تسليم الأموال غير معلق."));
        Require(Convert.ToBase64String(h.RowVersion) == rowVersion, JahezErrors.Conflict("تغير تسليم الأموال؛ أعد تحميله."));
        return h;
    }
}
