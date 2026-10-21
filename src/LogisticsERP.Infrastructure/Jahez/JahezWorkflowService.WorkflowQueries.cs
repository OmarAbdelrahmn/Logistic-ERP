using System.Globalization;
using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public Task<Result<JahezPage<JahezImportBatch>>> ImportBatchesAsync(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.ImportsRead, () => WorkflowPage(db.Set<JahezImportBatch>(), page, pageSize, ct), ct);
    public Task<Result<JahezPage<JahezAdministrativeDebt>>> AdministrativeDebtsAsync(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            var rows = await (from h in db.Set<JahezAccountSession>().AsNoTracking()
                join a in db.PlatformRiderAccounts on h.PlatformRiderAccountId equals a.Id
                join r in db.RiderProfiles on h.RiderProfileId equals r.Id
                join e in db.Employees on r.EmployeeId equals e.Id
                orderby h.StartedAtUtc descending, h.Id
                select new JahezAdministrativeDebt(h.Id, a.Id, a.ExternalAccountId, r.Id, e.FullNameAr,
                    db.Set<JahezLedgerEntry>().Where(x => x.HandoverId == h.Id && x.Bucket == JahezLedgerBucket.PlatformDebt).Sum(x => x.Amount)))
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezAdministrativeDebt>(rows, page, pageSize);
        }, ct);
    private IQueryable<JahezAccountRow> WorkflowAccounts(Guid? accountId = null, int? skip = null, int? take = null)
    {
        var yesterday = Today.AddDays(-1);
        var accounts = db.PlatformRiderAccounts.AsNoTracking().Where(a => db.ClientPlatforms.Any(p => p.Id == a.ClientPlatformId && p.Code == "JAHEZ"));
        if (accountId.HasValue) accounts = accounts.Where(a => a.Id == accountId);
        accounts = accounts.OrderBy(a => a.Code).ThenBy(a => a.Id);
        if (skip.HasValue) accounts = accounts.Skip(skip.Value);
        if (take.HasValue) accounts = accounts.Take(take.Value);
        var orders = from d in db.Set<JahezDailyDispatch>().AsNoTracking()
            join h in db.Set<JahezAccountSession>().IgnoreQueryFilters().AsNoTracking() on d.HandoverId equals h.Id
            where !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == d.BatchId && b.CommittedAtUtc != null)
                && db.Set<JahezImportBatch>().Any(b => b.Id == d.BatchId && b.CommittedAtUtc != null)
            select new { AccountId = h.PlatformRiderAccountId, d.Date, d.Count };
        return from a in accounts
            join city in db.OperatingCities on a.OperatingCityId equals city.Id
            join globalCity in db.GlobalCities on city.GlobalCityId equals globalCity.Id
            join sponsor in db.Sponsors on a.SponsorId equals sponsor.Id into sponsors
            from sponsor in sponsors.DefaultIfEmpty()
            join owner in db.Employees on a.RegisteredEmployeeId equals owner.Id into owners
            from owner in owners.DefaultIfEmpty()
            join ownerRider in db.RiderProfiles on a.RegisteredEmployeeId equals ownerRider.EmployeeId into ownerRiders
            from ownerRider in ownerRiders.DefaultIfEmpty()
            join assignment in db.RiderClientAssignments.Where(x => x.EffectiveTo == null) on a.Id equals assignment.PlatformRiderAccountId into assignments
            from assignment in assignments.DefaultIfEmpty()
            join rider in db.RiderProfiles on assignment.RiderProfileId equals rider.Id into riders
            from rider in riders.DefaultIfEmpty()
            join employee in db.Employees on rider.EmployeeId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            let lastDate = orders.Where(x => x.AccountId == a.Id && x.Count > 0).Select(x => (DateOnly?)x.Date).Max()
            select new JahezAccountRow(a.Id, a.Code, a.ExternalAccountId, a.UserName, a.OperatingCityId, globalCity.NameAr,
                a.SponsorId, sponsor == null ? null : sponsor.RegistryNameAr, a.Status.ToString(), a.StatusReason,
                assignment != null, db.PlatformAccountCredentialVersions.Any(x => x.PlatformRiderAccountId == a.Id && x.Ciphertext.Length >= 1),
                owner == null ? null : new JahezAccountPerson(owner.Id, ownerRider == null ? null : ownerRider.Id,
                    owner.FullNameAr, owner.FullNameEn, owner.IqamaNo, owner.PrimaryPhone, owner.SponsorId),
                employee == null ? null : new JahezAccountPerson(employee.Id, rider.Id, employee.FullNameAr, employee.FullNameEn,
                    employee.IqamaNo, employee.PrimaryPhone, employee.SponsorId),
                assignment == null ? null : assignment.Id,
                db.Set<JahezAccountSession>().Where(x => assignment != null && x.RiderClientAssignmentId == assignment.Id && x.EndedAtUtc == null)
                    .Select(x => (Guid?)x.Id).FirstOrDefault(),
                a.AcquisitionDate, a.StartDate, a.EndDate, a.OwnershipNotes, a.OperationalNotes,
                orders.Any(x => x.AccountId == a.Id && x.Date == yesterday)
                    ? orders.Where(x => x.AccountId == a.Id && x.Date == yesterday).Sum(x => (int?)x.Count) : null,
                lastDate, lastDate == null ? null : orders.Where(x => x.AccountId == a.Id && x.Date == lastDate).Sum(x => (int?)x.Count),
                Convert.ToBase64String(a.RowVersion));
    }

    public Task<Result<JahezAccountsPage>> AccountsV2Async(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            return new JahezAccountsPage(await WorkflowAccounts(null, (page - 1) * pageSize, pageSize).ToArrayAsync(ct),
                page, pageSize, await db.PlatformRiderAccounts.CountAsync(a => db.ClientPlatforms.Any(p => p.Id == a.ClientPlatformId && p.Code == "JAHEZ"), ct));
        }, ct);

    public Task<Result<JahezAccountDetails>> AccountDetailsV2Async(Guid id, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            var row = await WorkflowAccounts(id).SingleOrDefaultAsync(ct);
            Require(row is not null, JahezErrors.NotFound);
            var ids = db.Set<JahezAccountSession>().Where(x => x.PlatformRiderAccountId == id).Select(x => x.Id);
            var handovers = await GetHandoversAsync(id, null, page, pageSize, ct);
            var operations = await WorkflowPage(db.Set<JahezOperation>().Where(x => ids.Contains(x.HandoverId)), page, pageSize, ct);
            var requests = await WorkflowPage(db.Set<JahezApprovalRequest>().Where(x => ids.Contains(x.HandoverId)), page, pageSize, ct);
            var fees = await WorkflowPage(db.Set<JahezAccountFee>().Where(x => ids.Contains(x.HandoverId)), page, pageSize, ct);
            var ledger = await db.Set<JahezLedgerEntry>().AsNoTracking().Where(x => ids.Contains(x.HandoverId))
                .OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            var orders = await (from d in db.Set<JahezDailyDispatch>().AsNoTracking()
                join h in db.Set<JahezAccountSession>() on d.HandoverId equals h.Id
                where h.PlatformRiderAccountId == id && db.Set<JahezImportBatch>().Any(b => b.Id == d.BatchId && b.CommittedAtUtc != null)
                    && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == d.BatchId && b.CommittedAtUtc != null)
                orderby d.Date descending, d.Id
                select new JahezDispatchReportRow(id, row!.ExternalAccountId, h.RiderProfileId, h.Id, d.Date, d.Count))
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezAccountDetails(row!, handovers.Value!, operations, requests, fees,
                new(orders, page, pageSize), new(ledger, page, pageSize));
        }, ct);

    public Task<Result<JahezAccountStatistics>> StatisticsV2Async(CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            // No PII is materialized for aggregate statistics.
            var accounts = await (from a in db.PlatformRiderAccounts.AsNoTracking()
                join p in db.ClientPlatforms on a.ClientPlatformId equals p.Id where p.Code == "JAHEZ"
                join assignment in db.RiderClientAssignments.Where(x => x.EffectiveTo == null) on a.Id equals assignment.PlatformRiderAccountId into assignments
                from assignment in assignments.DefaultIfEmpty()
                join rider in db.RiderProfiles on assignment.RiderProfileId equals rider.Id into riders
                from rider in riders.DefaultIfEmpty()
                join employee in db.Employees on rider.EmployeeId equals employee.Id into employees
                from employee in employees.DefaultIfEmpty()
                select new { a.Id, a.SponsorId, a.Status, RiderId = assignment == null ? (Guid?)null : assignment.RiderProfileId,
                    RiderSponsorId = employee == null ? null : employee.SponsorId,
                    HasPassword = db.PlatformAccountCredentialVersions.Any(x => x.PlatformRiderAccountId == a.Id && x.Ciphertext.Length >= 1) }).ToArrayAsync(ct);
            var sponsors = await db.Sponsors.AsNoTracking().Select(x => new { x.Id, NameAr = x.RegistryNameAr }).ToArrayAsync(ct);
            var sponsored = await (from r in db.RiderProfiles.AsNoTracking() join e in db.Employees on r.EmployeeId equals e.Id
                where e.Status == EmployeeStatus.Active select new { r.Id, e.SponsorId }).ToArrayAsync(ct);
            var yesterday = Today.AddDays(-1);
            var orderData = await (from d in db.Set<JahezDailyDispatch>().AsNoTracking()
                join h in db.Set<JahezAccountSession>().IgnoreQueryFilters() on d.HandoverId equals h.Id
                where d.Date == yesterday && db.Set<JahezImportBatch>().Any(b => b.Id == d.BatchId && b.CommittedAtUtc != null)
                    && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == d.BatchId && b.CommittedAtUtc != null)
                group d by h.PlatformRiderAccountId into g select new { Id = g.Key, Count = g.Sum(x => x.Count) }).ToArrayAsync(ct);
            var accountIds = accounts.Select(x => x.Id).ToHashSet();
            var currentOrders = orderData.Where(x => accountIds.Contains(x.Id)).ToArray();
            return new JahezAccountStatistics(yesterday, accounts.Length,
                accounts.Count(x => x.Status is PlatformRiderAccountStatus.Available or PlatformRiderAccountStatus.Assigned),
                accounts.Count(x => x.RiderId == null), accounts.Count(x => x.Status == PlatformRiderAccountStatus.Suspended),
                accounts.Count(x => !x.HasPassword), currentOrders.Length == 0 ? null : currentOrders.Sum(x => x.Count), currentOrders.Length,
                sponsors.Select(s => new JahezSponsorStatistics(s.Id, s.NameAr,
                    accounts.Count(x => x.SponsorId == s.Id), accounts.Count(x => x.SponsorId == s.Id && x.RiderId.HasValue),
                    accounts.Where(x => x.SponsorId == s.Id && x.RiderId.HasValue).Select(x => x.RiderId).Distinct().Count(),
                    sponsored.Count(x => x.SponsorId == s.Id),
                    accounts.Where(x => x.RiderSponsorId == s.Id && x.SponsorId != s.Id && x.RiderId.HasValue).Select(x => x.RiderId).Distinct().Count())).ToArray());
        }, ct);

    private static async Task<JahezPage<T>> WorkflowPage<T>(IQueryable<T> query, int page, int pageSize, CancellationToken ct) where T : LogisticsERP.Domain.Common.AuditableEntity
    {
        Page(page, pageSize);
        return new(await query.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct), page, pageSize);
    }
    public Task<Result<JahezPage<JahezOperation>>> OperationsV2Async(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, () => WorkflowPage(db.Set<JahezOperation>(), page, pageSize, ct), ct);
    private IQueryable<JahezMoneyMovementResponse> WorkflowMovements(int page, int pageSize,
        DateTimeOffset fromUtc, DateTimeOffset toExclusiveUtc)
    {
        var movements = db.Set<JahezMoneyMovement>().AsNoTracking()
            .Where(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toExclusiveUtc)
            .OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize);
        return from m in movements
            join operation in db.Set<JahezOperation>() on m.OperationId equals operation.Id into operations
            from operation in operations.DefaultIfEmpty()
            join rider in db.RiderProfiles on m.RiderProfileId equals rider.Id into riders
            from rider in riders.DefaultIfEmpty()
            join employee in db.Employees on rider.EmployeeId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            join handover in db.Set<JahezAccountSession>() on m.HandoverId equals handover.Id into handovers
            from handover in handovers.DefaultIfEmpty()
            join account in db.PlatformRiderAccounts on handover.PlatformRiderAccountId equals account.Id into accounts
            from account in accounts.DefaultIfEmpty()
            join ownership in db.Set<TreasuryOwnership>().Where(x => x.Source == TreasurySource.Movement) on m.Id equals ownership.SourceId into ownerships
            from ownership in ownerships.DefaultIfEmpty()
            join sponsor in db.Sponsors on account.SponsorId equals sponsor.Id into sponsors
            from sponsor in sponsors.DefaultIfEmpty()
            join owner in db.Employees on account.RegisteredEmployeeId equals owner.Id into owners
            from owner in owners.DefaultIfEmpty()
            join ownerRider in db.RiderProfiles on account.RegisteredEmployeeId equals ownerRider.EmployeeId into ownerRiders
            from ownerRider in ownerRiders.DefaultIfEmpty()
            let cashboxHandoverId = db.JahezCashboxHandoverMovements.Where(link => link.MoneyMovementId == m.Id
                && db.JahezCashboxHandovers.Any(h => h.Id == link.CashboxHandoverId && h.Status != JahezCashboxHandoverStatus.Cancelled))
                .Select(link => (Guid?)link.CashboxHandoverId).FirstOrDefault()
            orderby m.OccurredAtUtc descending, m.Id
            select WithSettlementDates(new JahezMoneyMovementResponse(m.Id, m.HandoverId, m.RiderProfileId, m.OperationId,
                m.TransferredDebtId, m.Fund, m.Direction, m.Purpose, m.Amount, m.ActorUserId,
                m.OccurredAtUtc, m.Reference, m.Reason, m.CreatedAtUtc, m.CreatedByUserId,
                employee == null ? null : new JahezAccountPerson(employee.Id, rider.Id, employee.FullNameAr,
                    employee.FullNameEn, employee.IqamaNo, employee.PrimaryPhone, employee.SponsorId),
                owner == null ? null : new JahezAccountPerson(owner.Id, ownerRider == null ? null : ownerRider.Id,
                    owner.FullNameAr, owner.FullNameEn, owner.IqamaNo, owner.PrimaryPhone, owner.SponsorId),
                ownership == null ? null : ownership.SponsorNameAr, cashboxHandoverId,
                db.JahezCashboxHandovers.Any(h => h.Id == cashboxHandoverId && h.Status == JahezCashboxHandoverStatus.Confirmed),
                handover == null ? null : (Guid?)handover.PlatformRiderAccountId,
                account == null ? null : account.ExternalAccountId,
                operation == null ? null : (decimal?)operation.Net,
                operation == null ? null : (decimal?)operation.Commission,
                operation == null ? null : (decimal?)operation.CalculatedCommission)
                { OperationRowVersion = operation == null ? null : Convert.ToBase64String(operation.RowVersion),
                    SponsorId = ownership == null ? null : ownership.SponsorId,
                    SponsorIdentifier = ownership == null ? null : ownership.SponsorIdentifier,
                    TreasurySubAccountId = db.Set<TreasuryPosting>().Where(p => p.MoneyMovementId == m.Id && p.Kind == TreasuryEntryKind.Business)
                        .OrderByDescending(p => p.Revision).Select(p => (Guid?)p.SubAccountId).FirstOrDefault() },
                operation == null ? null : operation.CalculationJson);
    }

    private static JahezMoneyMovementResponse WithSettlementDates(JahezMoneyMovementResponse movement, string? calculationJson)
    {
        if (movement.Purpose != JahezMoneyPurpose.Settlement || string.IsNullOrWhiteSpace(calculationJson)) return movement;
        // Read the saved accrual evidence after SQL pagination; the session's commission cursor has since advanced.
        try
        {
            using var document = JsonDocument.Parse(calculationJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("Accrual", out var accrual)
                || accrual.ValueKind != JsonValueKind.Array) return movement;
            DateOnly? start = null, end = null;
            foreach (var period in accrual.EnumerateArray())
            {
                if (period.ValueKind != JsonValueKind.Object
                    || !period.TryGetProperty("FromDate", out var from) || from.ValueKind != JsonValueKind.String
                    || !period.TryGetProperty("ToDate", out var to) || to.ValueKind != JsonValueKind.String
                    || !DateOnly.TryParseExact(from.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
                    || !DateOnly.TryParseExact(to.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate)
                    || fromDate > toDate) return movement;
                if (!start.HasValue || fromDate < start.Value) start = fromDate;
                if (!end.HasValue || toDate > end.Value) end = toDate;
            }
            return movement with { StartDate = start, EndDate = end };
        }
        catch (JsonException)
        {
            // Legacy movements without readable calculation evidence still belong in the history.
            return movement;
        }
    }

    public Task<Result<JahezPage<JahezMoneyMovementResponse>>> MovementsV2Async(int page, int pageSize,
        DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, async () =>
        {
            Page(page, pageSize);
            var today = Today;
            var from = fromDate ?? new DateOnly(today.Year, today.Month, 1);
            var to = toDate ?? today;
            Require(from <= to, JahezErrors.Invalid("تاريخ البداية يجب ألا يتجاوز تاريخ النهاية."));
            var fromUtc = from == DateOnly.MinValue ? DateTimeOffset.MinValue : JahezRules.StartOfDay(from).ToUniversalTime();
            var toExclusiveUtc = to == DateOnly.MinValue ? DateTimeOffset.MinValue.AddHours(21)
                : JahezRules.StartOfDay(to).ToUniversalTime().AddDays(1);
            return new JahezPage<JahezMoneyMovementResponse>(await WorkflowMovements(page, pageSize, fromUtc, toExclusiveUtc)
                .ToArrayAsync(ct), page, pageSize);
        }, ct);
    public Task<Result<IReadOnlyList<JahezFundBalance>>> FundsV2Async(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<JahezFundBalance>>(PermissionKeys.Jahez.CashboxRead, async () =>
        {
            var groups = await OutstandingMoneyMovements().GroupBy(x => new { x.Fund, x.Direction })
                .Select(g => new { g.Key.Fund, g.Key.Direction, Amount = g.Sum(x => x.Amount) }).ToArrayAsync(ct);
            return Enum.GetValues<JahezMoneyFund>().Select(f =>
            {
                var received = groups.Where(x => x.Fund == f && x.Direction == JahezMoneyDirection.Receipt).Sum(x => x.Amount);
                var paid = groups.Where(x => x.Fund == f && x.Direction == JahezMoneyDirection.Disbursement).Sum(x => x.Amount);
                return new JahezFundBalance(f, received, paid, received - paid);
            }).ToArray();
        }, ct);

    public Task<Result<JahezAccountRow>> UpdateAccountStatusV2Async(string key, Guid id, JahezAccountStatusUpdate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.account.status", new { id, request }, JahezWorkflowPermissions.StatusUpdate, async () =>
        {
            Reason(request.Reason);
            Require(Enum.TryParse<PlatformRiderAccountStatus>(request.Status, true, out var status) && Enum.IsDefined(status), JahezErrors.Invalid("حالة الحساب غير صالحة."));
            var account = await Account(id, ct);
            Require(Convert.ToBase64String(account.RowVersion) == request.RowVersion, JahezErrors.Conflict("تغير الحساب؛ أعد تحميله."));
            var assigned = await db.RiderClientAssignments.AnyAsync(x => x.PlatformRiderAccountId == id && x.EffectiveTo == null, ct);
            Require(status != PlatformRiderAccountStatus.Assigned || assigned, JahezErrors.Conflict("تسليم الحساب يتم بإجراء التسليم."));
            Require(!assigned || status is PlatformRiderAccountStatus.Assigned or PlatformRiderAccountStatus.Suspended,
                JahezErrors.Conflict("اسحب الحساب قبل تغيير حالته إلى متاح أو مؤرشف أو متقاعد."));
            account.Status = status; account.StatusReason = request.Reason;
            await db.SaveChangesAsync(ct);
            return await WorkflowAccounts(id).SingleAsync(ct);
        }, ct);
}
