using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezService
{
    public Task<Result<JahezCashboxBalance>> GetCashboxAsync(CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, async () =>
        {
            var balances = await (from e in db.Set<JahezCashboxEntry>().AsNoTracking()
                join h in db.Set<JahezCashboxHandover>() on e.CashboxHandoverId equals h.Id into handovers
                from h in handovers.DefaultIfEmpty()
                where h == null || h.Status != JahezCashboxHandoverStatus.Approved
                group e by new { e.Section, Reserved = e.CashboxHandoverId != null } into g
                select new { g.Key.Section, g.Key.Reserved, Amount = g.Sum(e => e.Amount) }).ToArrayAsync(ct);
            var fees = balances.Where(x => x.Section == JahezCashboxSection.AccountFees).Sum(x => x.Amount);
            var settlements = balances.Where(x => x.Section == JahezCashboxSection.Settlements).Sum(x => x.Amount);
            var reservedFees = balances.Where(x => x.Section == JahezCashboxSection.AccountFees && x.Reserved).Sum(x => x.Amount);
            var reservedSettlements = balances.Where(x => x.Section == JahezCashboxSection.Settlements && x.Reserved).Sum(x => x.Amount);
            return new JahezCashboxBalance(fees, settlements, reservedFees, reservedSettlements, fees - reservedFees, settlements - reservedSettlements);
        }, ct);

    public Task<Result<JahezCashboxHandover>> SubmitCashboxAsync(string key, JahezCashboxCreateRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "submit-cashbox", request, PermissionKeys.Jahez.CashboxSubmit, async () =>
        {
            Reason(request.Reason);
            Require(request.BusinessDate <= Today, JahezErrors.Invalid("تاريخ التسليم لا يكون في المستقبل."));
            var through = JahezRules.StartOfDay(request.BusinessDate.AddDays(1)).ToUniversalTime();
            var entries = await db.Set<JahezCashboxEntry>().Where(x => x.CashboxHandoverId == null && x.ReceivedAtUtc < through).ToArrayAsync(ct);
            Require(entries.Length > 0, JahezErrors.Conflict("لا توجد مبالغ متاحة للتسليم حتى هذا التاريخ."));
            var entity = new JahezCashboxHandover { BusinessDate = request.BusinessDate, Status = JahezCashboxHandoverStatus.Pending,
                FeeAmount = entries.Where(x => x.Section == JahezCashboxSection.AccountFees).Sum(x => x.Amount),
                SettlementAmount = entries.Where(x => x.Section == JahezCashboxSection.Settlements).Sum(x => x.Amount),
                RequestedByUserId = Actor, Reason = request.Reason };
            db.Add(entity);
            foreach (var entry in entries) entry.CashboxHandoverId = entity.Id;
            return entity;
        }, ct);

    public Task<Result<JahezCashboxHandover>> ConfirmCashboxAsync(string key, Guid id, JahezAccountantConfirmRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "confirm-cashbox", new { id, request }, PermissionKeys.Jahez.CashboxConfirm, async () =>
        {
            Reason(request.Reason);
            var entity = await db.Set<JahezCashboxHandover>().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(entity is not null, JahezErrors.NotFound);
            Require(entity!.Status == JahezCashboxHandoverStatus.Pending && entity.RequestedByUserId != Actor, JahezErrors.Conflict("المحاسب يجب أن يختلف عن مقدم طلب معلق."));
            Require(request.FeeAmount == entity.FeeAmount && request.SettlementAmount == entity.SettlementAmount,
                JahezErrors.Conflict("المبلغ المستلم لا يطابق كل قسم على حدة."));
            entity.AccountantFeeAmount = request.FeeAmount;
            entity.AccountantSettlementAmount = request.SettlementAmount;
            entity.AccountantUserId = Actor;
            entity.ConfirmedAtUtc = Now;
            entity.ConfirmationReason = request.Reason;
            entity.Status = JahezCashboxHandoverStatus.AccountantConfirmed;
            return entity;
        }, ct);

    public Task<Result<JahezCashboxHandover>> DecideCashboxAsync(string key, Guid id, JahezDecisionRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "decide-cashbox", new { id, request }, PermissionKeys.Jahez.CashboxApprove, async () =>
        {
            Reason(request.Reason);
            var entity = await db.Set<JahezCashboxHandover>().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(entity is not null, JahezErrors.NotFound);
            Require(entity!.RequestedByUserId != Actor && entity.AccountantUserId != Actor, JahezErrors.Forbidden);
            Require(entity.Status is JahezCashboxHandoverStatus.Pending or JahezCashboxHandoverStatus.AccountantConfirmed,
                JahezErrors.Conflict("تم البت في التسليم بالفعل."));
            var entries = await db.Set<JahezCashboxEntry>().Where(x => x.CashboxHandoverId == id).ToArrayAsync(ct);
            if (request.Approve)
            {
                Require(entity.Status == JahezCashboxHandoverStatus.AccountantConfirmed && entity.AccountantFeeAmount == entity.FeeAmount
                    && entity.AccountantSettlementAmount == entity.SettlementAmount, JahezErrors.Conflict("تأكيد المحاسب المطابق مطلوب قبل الاعتماد."));
                Require(entries.Where(x => x.Section == JahezCashboxSection.AccountFees).Sum(x => x.Amount) == entity.FeeAmount
                    && entries.Where(x => x.Section == JahezCashboxSection.Settlements).Sum(x => x.Amount) == entity.SettlementAmount,
                    JahezErrors.Conflict("حركات التسليم تغيرت؛ لا يمكن الاعتماد."));
                entity.Status = JahezCashboxHandoverStatus.Approved;
            }
            else
            {
                entity.Status = JahezCashboxHandoverStatus.Rejected;
                foreach (var entry in entries) entry.CashboxHandoverId = null;
            }
            entity.ApprovedByUserId = Actor;
            entity.DecidedAtUtc = Now;
            entity.DecisionReason = request.Reason;
            return entity;
        }, ct);

    public Task<Result<JahezPage<JahezCashboxHandover>>> GetCashboxHandoversAsync(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, async () =>
        {
            Page(page, pageSize);
            var rows = await db.Set<JahezCashboxHandover>().AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezCashboxHandover>(rows, page, pageSize);
        }, ct);
}
