using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private IQueryable<TreasuryPosting> OutstandingCustodyPostings() =>
        from p in db.Set<TreasuryPosting>().AsNoTracking()
        join a in db.Set<TreasurySubAccount>() on p.SubAccountId equals a.Id
        where a.Location == TreasuryLocation.Supervisor && p.CashboxHandoverId == null
            && !db.Set<TreasuryHandoverPosting>().Any(l => l.PostingId == p.Id && db.JahezCashboxHandovers.Any(h =>
                h.Id == l.HandoverId && h.Status == JahezCashboxHandoverStatus.Confirmed))
        select p;

    private async Task<List<TreasuryHandoverLine>> BuildHandoverLines(IReadOnlyList<JahezMoneyMovement> movements, Guid id, CancellationToken ct)
    {
        var lines = new List<TreasuryHandoverLine>();
        var ids = movements.Select(x => x.Id).ToArray();
        var owners = await db.Set<TreasuryOwnership>().AsNoTracking().Where(x => x.Source == TreasurySource.Movement && ids.Contains(x.SourceId))
            .ToDictionaryAsync(x => x.SourceId, ct);
        var accounts = await db.Set<TreasurySubAccount>().AsNoTracking().Where(x => x.IsDefault && x.IsPosting && x.IsActive).ToArrayAsync(ct);
        foreach (var m in movements)
        {
            var owner = owners.GetValueOrDefault(m.Id);
            Require(owner is not null, JahezErrors.Conflict("توجد حركة دون ملكية مالية موثقة."));
            var location = m.Fund switch { JahezMoneyFund.Cashbox => TreasuryLocation.Supervisor,
                JahezMoneyFund.Bank => TreasuryLocation.Bank, _ => TreasuryLocation.PetroApp };
            var purpose = m.Purpose == JahezMoneyPurpose.AccountFee ? TreasuryPurpose.AccountFee : TreasuryPurpose.Settlement;
            var account = accounts.SingleOrDefault(a => a.SponsorId == owner!.SponsorId && a.Location == location
                && (a.Purpose == purpose || a.Purpose == TreasuryPurpose.Combined));
            Require(account is not null, JahezErrors.Conflict("حساب التوجيه المالي غير مهيأ."));
            var line = lines.Find(x => x.SubAccountId == account!.Id);
            if (line is null)
            {
                line = new TreasuryHandoverLine { HandoverId = id, SubAccountId = account!.Id, SponsorId = owner!.SponsorId,
                    AccountCode = account.Code, AccountNameAr = account.NameAr,
                    SponsorNameAr = owner.SponsorNameAr, SponsorIdentifier = owner.SponsorIdentifier };
                lines.Add(line);
            }
            line.Amount += m.Direction == JahezMoneyDirection.Receipt ? m.Amount : -m.Amount;
            line.MovementCount++;
        }
        return lines;
    }

    private async Task<(TreasuryHandoverPreview Preview, JahezMoneyMovement[] Movements, TreasuryPosting[] Postings)> HandoverPreview(DateOnly date, CancellationToken ct)
    {
        await RequireTreasury(ct);
        Require(date <= Today && date > DateOnly.MinValue, JahezErrors.Invalid("يوم التسليم غير صالح."));
        var cutoff = JahezRules.StartOfDay(date).ToUniversalTime().AddDays(1);
        var movements = await OutstandingMoneyMovements().Where(x => x.OccurredAtUtc < cutoff).ToArrayAsync(ct);
        var postings = await OutstandingCustodyPostings().Where(x => x.OccurredAtUtc < cutoff).ToArrayAsync(ct);
        var lines = await BuildHandoverLines(movements.Where(x => x.Fund != JahezMoneyFund.Cashbox).ToArray(), Guid.Empty, ct);
        foreach (var group in postings.GroupBy(x => new { x.SubAccountId, x.SponsorId }))
        {
            var sponsor = await db.Sponsors.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == group.Key.SponsorId, ct);
            lines.Add(new TreasuryHandoverLine { SubAccountId = group.Key.SubAccountId, SponsorId = sponsor.Id,
                AccountCode = await db.Set<TreasurySubAccount>().Where(a => a.Id == group.Key.SubAccountId).Select(a => a.Code).SingleAsync(ct),
                AccountNameAr = await db.Set<TreasurySubAccount>().Where(a => a.Id == group.Key.SubAccountId).Select(a => a.NameAr).SingleAsync(ct),
                SponsorNameAr = sponsor.RegistryNameAr, SponsorIdentifier = sponsor.EmployerIdentityNumber,
                Amount = group.Sum(x => x.Amount), MovementCount = group.Count() });
        }
        var accounts = await db.Set<TreasurySubAccount>().AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
        decimal Total(TreasuryLocation location) => lines.Where(x => accounts[x.SubAccountId].Location == location).Sum(x => x.Amount);
        return (new(date, lines, Total(TreasuryLocation.Supervisor), Total(TreasuryLocation.Bank), Total(TreasuryLocation.PetroApp),
            postings.Length + movements.Count(x => x.Fund != JahezMoneyFund.Cashbox)), movements, postings);
    }

    public Task<Result<TreasuryHandoverPreview>> PreviewTreasuryHandoverAsync(DateOnly businessDate, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, async () => (await HandoverPreview(businessDate, ct)).Preview, ct);

    public Task<Result<JahezPage<JahezCashboxHandover>>> TreasuryHandoversAsync(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, () => WorkflowPage(db.JahezCashboxHandovers.Where(x => x.TreasuryVersion == 3), page, pageSize, ct), ct);

    private async Task<TreasuryHandoverDetail> HandoverDetail(Guid id, CancellationToken ct)
    {
        var h = await db.JahezCashboxHandovers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        Require(h is not null, JahezErrors.NotFound);
        var lines = await db.Set<TreasuryHandoverLine>().AsNoTracking().Where(x => x.HandoverId == id).OrderBy(x => x.SubAccountId).ToArrayAsync(ct);
        return new(h!, lines);
    }

    public Task<Result<TreasuryHandoverDetail>> TreasuryHandoverAsync(Guid id, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, () => HandoverDetail(id, ct), ct);

    public Task<Result<TreasuryHandoverDetail>> SubmitTreasuryHandoverAsync(string key, JahezCashboxHandoverCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.handover.submit", request, PermissionKeys.Jahez.CashboxSubmit, async () =>
        {
            Reason(request.Reason);
            Require(!await db.JahezCashboxHandovers.AnyAsync(x => x.Status == JahezCashboxHandoverStatus.Pending, ct), JahezErrors.Conflict("يوجد تسليم معلق."));
            var snapshot = await HandoverPreview(request.BusinessDate, ct);
            Require(snapshot.Preview.MovementCount > 0, JahezErrors.Conflict("لا توجد عهدة أو حركات غير مسلمة."));
            var supervisorIds = await db.Set<TreasurySubAccount>().Where(x => x.Location == TreasuryLocation.Supervisor).Select(x => x.Id).ToArrayAsync(ct);
            Require(!snapshot.Preview.Lines.Any(x => supervisorIds.Contains(x.SubAccountId) && x.Amount < 0), JahezErrors.Conflict("توجد عهدة سالبة؛ راجع التمويل قبل التسليم."));
            var state = await db.Set<TreasuryState>().SingleAsync(x => x.Id == TreasuryState.SingletonId, ct);
            var h = new JahezCashboxHandover { TreasuryVersion = 3, DocumentNumber = $"JH-{state.NextHandoverNumber++:D10}",
                BusinessDate = request.BusinessDate, RequestedByUserId = Actor, RequestedByNameAr = commandActorName, Reason = request.Reason,
                CashboxAmount = snapshot.Preview.PhysicalCash, BankAmount = snapshot.Preview.BankActivity,
                PetroAppAmount = snapshot.Preview.PetroAppActivity, MovementCount = snapshot.Preview.MovementCount };
            db.Add(h);
            foreach (var line in snapshot.Preview.Lines) { line.HandoverId = h.Id; db.Add(line); }
            db.AddRange(snapshot.Movements.Select(m => new JahezCashboxHandoverMovement { CashboxHandoverId = h.Id, MoneyMovementId = m.Id }));
            db.AddRange(snapshot.Postings.Select(p => new TreasuryHandoverPosting { HandoverId = h.Id, PostingId = p.Id }));
            // Return the same immutable entities; executor populates generated timestamps/rowversions before serialization.
            return new TreasuryHandoverDetail(h, snapshot.Preview.Lines);
        }, ct);

    public Task<Result<TreasuryHandoverDetail>> ConfirmTreasuryHandoverAsync(string key, Guid id, TreasuryHandoverConfirm request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.handover.confirm", new { id, request }, PermissionKeys.Jahez.CashboxConfirm, async () =>
        {
            await RequireTreasury(ct); Reason(request.Reason);
            var h = await PendingCashboxHandover(id, request.RowVersion, ct);
            Require(h.TreasuryVersion == 3 && h.RequestedByUserId != Actor, JahezErrors.Forbidden);
            var lines = await db.Set<TreasuryHandoverLine>().AsNoTracking().Where(x => x.HandoverId == id).ToArrayAsync(ct);
            var cashAccounts = await db.Set<TreasurySubAccount>().Where(x => x.Location == TreasuryLocation.Supervisor).Select(x => x.Id).ToArrayAsync(ct);
            var cash = lines.Where(x => cashAccounts.Contains(x.SubAccountId)).ToArray();
            Require(request.CashCounts is not null && request.CashCounts.Count == cash.Length
                && request.CashCounts.Select(x => x.SubAccountId).Distinct().Count() == cash.Length
                && cash.All(l => request.CashCounts.Any(c => c.SubAccountId == l.SubAccountId && c.Amount == l.Amount)),
                JahezErrors.Invalid("يجب مطابقة كل حساب نقدي على حدة، بما فيه الحسابات ذات الرصيد الصفري."));
            foreach (var line in cash.Where(x => x.Amount > 0))
            {
                var from = await PostingAccount(line.SubAccountId, ct);
                var to = await Route(new TreasuryOwnership { SponsorId = line.SponsorId }, JahezMoneyFund.Cashbox,
                    from.Purpose == TreasuryPurpose.AccountFee ? JahezMoneyPurpose.AccountFee : JahezMoneyPurpose.Settlement, ct, central: true);
                await Transfer(from, to, line.Amount, h.DocumentNumber!, request.Reason, ct, h.Id);
            }
            h.Status = JahezCashboxHandoverStatus.Confirmed; h.AccountantUserId = Actor;
            h.AccountantNameAr = commandActorName;
            h.ConfirmedAtUtc = Now; h.ConfirmationReason = request.Reason;
            return new TreasuryHandoverDetail(h, lines);
        }, ct);

    public Task<Result<TreasuryHandoverDetail>> CancelTreasuryHandoverAsync(string key, Guid id, JahezCashboxHandoverCancel request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.handover.cancel", new { id, request }, PermissionKeys.Jahez.CashboxSubmit, async () =>
        {
            Reason(request.Reason);
            var h = await PendingCashboxHandover(id, request.RowVersion, ct);
            Require(h.TreasuryVersion == 3 && h.RequestedByUserId == Actor, JahezErrors.Forbidden);
            h.Status = JahezCashboxHandoverStatus.Cancelled; h.CancelledAtUtc = Now; h.CancellationReason = request.Reason;
            return new TreasuryHandoverDetail(h, await db.Set<TreasuryHandoverLine>().AsNoTracking().Where(x => x.HandoverId == id).ToArrayAsync(ct));
        }, ct);
}
