using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezService(
    ApplicationDbContext db, ICurrentUser user, IPermissionChecker permissions, TimeProvider clock) : IJahezService
{
    private Guid Actor => user.UserId ?? throw new JahezBusinessException(JahezErrors.Forbidden);
    private DateTimeOffset Now => clock.GetUtcNow();
    private DateOnly Today => JahezRules.RiyadhDate(Now);

    private async Task<Guid> AuthorizeAsync(string permission, CancellationToken ct)
    {
        var platformId = await db.ClientPlatforms.Where(x => x.Code == "JAHEZ").Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        Require(platformId.HasValue, JahezErrors.NotFound);
        Require(user.UserId.HasValue && user.AuthorizationVersion.HasValue
            && await permissions.HasPermissionAsync(Actor, user.AuthorizationVersion!.Value, permission,
                new PermissionScope(AccessScopeType.ClientPlatform, platformId!.Value), ct), JahezErrors.Forbidden);
        return platformId!.Value;
    }

    private async Task<Result<T>> ReadAsync<T>(string permission, Func<Task<T>> action, CancellationToken ct)
    {
        try { await AuthorizeAsync(permission, ct); return Result.Success(await action()); }
        catch (JahezBusinessException e) { return Result.Failure<T>(e.Error); }
    }

    // The service owns the whole transaction, including the receipt. A failed operation leaves no staged writes.
    private async Task<Result<T>> ExecuteAsync<T>(string key, string operation, object payload, string permission,
        Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            await AuthorizeAsync(permission, ct);
            Require(!string.IsNullOrWhiteSpace(key) && key.Length <= 150, JahezErrors.Invalid("مفتاح عدم التكرار مطلوب، بحد أقصى 150 حرفًا."));
            var hash = Hash(JsonSerializer.SerializeToUtf8Bytes(payload));
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = db.Database.IsRelational()
                    ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
                var receipt = await db.Set<JahezCommandReceipt>().AsNoTracking().SingleOrDefaultAsync(
                    x => x.ActorUserId == Actor && x.Operation == operation && x.CommandKey == key, ct);
                if (receipt is not null)
                {
                    Require(receipt.PayloadHash == hash, JahezErrors.Conflict("استُخدم مفتاح عدم التكرار مع بيانات مختلفة."));
                    return Result.Success(JsonSerializer.Deserialize<T>(receipt.ResultJson)!);
                }
                var value = await action();
                // Persist generated audit times and rowversions before freezing the response.
                // Both saves are protected by the same SQL transaction.
                await db.SaveChangesAsync(ct);
                var resultJson = JsonSerializer.Serialize(value);
                db.Add(new JahezCommandReceipt { ActorUserId = Actor, CommandKey = key, Operation = operation,
                    PayloadHash = hash, ResultJson = resultJson });
                await db.SaveChangesAsync(ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
                return Result.Success(JsonSerializer.Deserialize<T>(resultJson)!);
            });
        }
        catch (JahezBusinessException e) { db.ChangeTracker.Clear(); return Result.Failure<T>(e.Error); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<T>(JahezErrors.Conflict("تعارض في البيانات؛ أعد تحميل السجل وحاول مجددًا بالمفتاح نفسه.")); }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void Require(bool condition, OperationError error) { if (!condition) throw new JahezBusinessException(error); }
    private static void Reason(string reason) => Require(!string.IsNullOrWhiteSpace(reason) && reason.Length <= 2000, JahezErrors.Invalid("السبب مطلوب، بحد أقصى 2000 حرف."));
    private static void Page(int page, int pageSize) => Require(page >= 1 && pageSize is >= 1 and <= 100 && page <= 100000, JahezErrors.Invalid("رقم الصفحة وحجمها غير صالحين؛ أقصى حجم 100."));
    private async Task<JahezAccountHandover> Handover(Guid id, CancellationToken ct)
    {
        var h = await db.Set<JahezAccountHandover>().SingleOrDefaultAsync(x => x.Id == id, ct);
        Require(h is not null, JahezErrors.NotFound);
        return h!;
    }

    private async Task<PlatformRiderAccount> Account(Guid id, CancellationToken ct)
    {
        var account = await (from a in db.PlatformRiderAccounts join p in db.ClientPlatforms on a.ClientPlatformId equals p.Id
            where a.Id == id && p.Code == "JAHEZ" select a).SingleOrDefaultAsync(ct);
        Require(account is not null, JahezErrors.NotFound);
        return account!;
    }

    private async Task<JahezHandoverResponse> Response(JahezAccountHandover h, CancellationToken ct)
    {
        var external = await db.PlatformRiderAccounts.IgnoreQueryFilters().Where(x => x.Id == h.PlatformRiderAccountId).Select(x => x.ExternalAccountId).SingleAsync(ct);
        return new(h.Id, h.PlatformRiderAccountId, external, h.RiderProfileId, h.RiderClientAssignmentId,
            h.StartedAtUtc, h.EndedAtUtc, h.CommissionStartsOn, h.CommissionPostedThrough,
            h.LastSettlementPaymentAtUtc, h.IsLegacy, h.DebtTransferred);
    }

    private void Entry(JahezAccountHandover h, JahezLedgerBucket bucket, JahezLedgerKind kind, decimal amount,
        Guid sourceId, string reason, DateTimeOffset? occurred = null, Guid? reverses = null) => db.Add(new JahezLedgerEntry
        { HandoverId = h.Id, Bucket = bucket, Kind = kind, Amount = amount, SourceId = sourceId,
            OccurredAtUtc = occurred ?? Now, Reason = reason, ReversesEntryId = reverses });

    public Task<Result<JahezHandoverResponse>> HandoverAsync(string key, JahezHandoverRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "handover", request, PermissionKeys.Jahez.HandoversManage, async () =>
        {
            Require(request.FeeApprovalRequestId is null, JahezErrors.Invalid("سجل الاستلام ثم أرسل طلب استثناء الرسوم، أو استخدم التبديل المجاني المعتمد."));
            var h = await CreateHandover(request.AccountId, request.RiderProfileId, request.EffectiveAtUtc, request.Reason, 0m, null, ct);
            Require(request.InitialFeePayment is >= 0m and <= 200m && JahezRules.Money(request.InitialFeePayment) == request.InitialFeePayment,
                JahezErrors.Invalid("دفعة الرسوم غير صالحة."));
            if (request.InitialFeePayment > 0) StagePayment(h, JahezRules.RiyadhDate(request.EffectiveAtUtc), request.InitialFeePayment, 0, 0, false, request.Reason);
            return await Response(h, ct);
        }, ct);

    private async Task<JahezAccountHandover> CreateHandover(Guid accountId, Guid riderId, DateTimeOffset effective,
        string reason, decimal waiver, Guid? approvalId, CancellationToken ct)
    {
        Reason(reason);
        effective = effective.ToUniversalTime();
        Require(effective <= Now && effective > DateTimeOffset.MinValue, JahezErrors.Invalid("تاريخ الاستلام يجب ألا يكون في المستقبل."));
        var account = await Account(accountId, ct);
        Require(account.DashboardSponsorId.HasValue, JahezErrors.Invalid("استكمل كفيل الداشبورد قبل تسليم حساب جاهز."));
        Require(account.PaymentModel == PlatformAccountPaymentModel.PayPerOrder, JahezErrors.Invalid("وحدة جاهز تدعم حسابات الدفع حسب الطلب فقط."));
        Require(account.Status == PlatformRiderAccountStatus.Available, JahezErrors.Conflict("الحساب غير متاح للاستلام."));
        Require(!await db.RiderClientAssignments.AnyAsync(x => x.PlatformRiderAccountId == accountId && x.EffectiveTo == null, ct), JahezErrors.Conflict("الحساب مسند بالفعل."));
        Require(!await db.Set<JahezAccountHandover>().AnyAsync(x => x.PlatformRiderAccountId == accountId && (x.EndedAtUtc == null || x.EndedAtUtc > effective), ct),
            JahezErrors.Conflict("تاريخ الاستلام يتداخل مع استخدام سابق."));
        var rider = await (from r in db.RiderProfiles join e in db.Employees on r.EmployeeId equals e.Id
            where r.Id == riderId && e.Status == EmployeeStatus.Active select r).SingleOrDefaultAsync(ct);
        Require(rider is not null, JahezErrors.NotFound);
        var active = await db.RiderClientAssignments.Where(x => x.RiderProfileId == riderId && x.EffectiveTo == null).ToArrayAsync(ct);
        var stagedClosedIds = db.ChangeTracker.Entries<RiderClientAssignment>().Where(x => x.Entity.EffectiveTo != null).Select(x => x.Entity.Id).ToHashSet();
        active = active.Where(x => !stagedClosedIds.Contains(x.Id)).ToArray();
        Require(active.Length < JahezRules.MaximumActiveAccountsPerRider,
            JahezErrors.Conflict("تجاوز حد حسابات المندوب."));
        var contract = await db.ClientContracts.Where(x => x.ClientPlatformId == account.ClientPlatformId && x.Status == ClientContractStatus.Active).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (contract is null)
        {
            contract = new ClientContract { ClientPlatformId = account.ClientPlatformId, Code = "JAHEZ-OPERATIONS", DisplayNameAr = "تشغيل جاهز", DisplayNameEn = "Jahez operations", Status = ClientContractStatus.Active };
            db.Add(contract);
        }
        var assignment = new RiderClientAssignment { RiderProfileId = riderId, ClientContractId = contract.Id,
            PlatformRiderAccountId = accountId, PaymentModel = account.PaymentModel, RiderAccountSlot = active.Any(x => x.RiderAccountSlot == 1) ? 2 : 1,
            EffectiveFrom = JahezRules.RiyadhDate(effective), Status = RiderAssignmentStatus.Active, StartReason = reason,
            AssignedByUserId = Actor, WasBackdated = JahezRules.RiyadhDate(effective) < Today,
            BackdatedReason = JahezRules.RiyadhDate(effective) < Today ? reason : null };
        db.Add(assignment);
        account.Status = PlatformRiderAccountStatus.Assigned;
        var h = new JahezAccountHandover { PlatformRiderAccountId = accountId, RiderProfileId = riderId,
            RiderClientAssignmentId = assignment.Id, StartedAtUtc = effective, CommissionStartsOn = JahezRules.RiyadhDate(effective).AddDays(1), Reason = reason };
        db.Add(h);
        var fee = new JahezAccountFee { HandoverId = h.Id, WaivedAmount = waiver, ApprovalRequestId = approvalId };
        db.Add(fee);
        Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Charge, 200m - waiver, fee.Id, reason);
        db.Add(new RiderAssignmentEvent { RiderClientAssignmentId = assignment.Id, FromStatus = RiderAssignmentStatus.Planned,
            ToStatus = RiderAssignmentStatus.Active, OccurredAtUtc = Now, ActorUserId = Actor, Reason = reason });
        return h;
    }

    public Task<Result<JahezHandoverResponse>> AdoptLegacyAsync(string key, JahezLegacyAdoptionRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "adopt-legacy", request, PermissionKeys.Jahez.AdjustmentsManage, async () =>
        {
            Reason(request.Reason);
            var assignment = await db.RiderClientAssignments.SingleOrDefaultAsync(x => x.Id == request.AssignmentId, ct);
            Require(assignment is not null && assignment.EffectiveTo is null, JahezErrors.NotFound);
            await Account(assignment!.PlatformRiderAccountId, ct);
            Require(request.FinancialStartOn >= assignment.EffectiveFrom && request.FinancialStartOn <= Today,
                JahezErrors.Invalid("بداية المحاسبة يجب أن تكون ضمن فترة الاستخدام وحتى اليوم."));
            Require(request.OpeningFees >= 0 && request.OpeningCommission >= 0, JahezErrors.Invalid("أرصدة الرسوم والعمولة لا تكون سالبة."));
            Require(new[] { request.OpeningFees, request.OpeningCommission, request.OpeningDebt }
                .All(x => Math.Abs(x) <= 1_000_000_000m && decimal.Round(x, 6) == x), JahezErrors.Invalid("الرصيد الافتتاحي يتجاوز الحجم أو الدقة المسموحة."));
            Require(!await db.Set<JahezAccountHandover>().AnyAsync(x => x.RiderClientAssignmentId == assignment.Id, ct), JahezErrors.Conflict("الإسناد مهيأ بالفعل."));
            var h = new JahezAccountHandover { PlatformRiderAccountId = assignment.PlatformRiderAccountId,
                RiderProfileId = assignment.RiderProfileId, RiderClientAssignmentId = assignment.Id,
                StartedAtUtc = JahezRules.StartOfDay(assignment.EffectiveFrom).ToUniversalTime(), CommissionStartsOn = request.FinancialStartOn,
                IsLegacy = true, Reason = request.Reason };
            db.Add(h);
            db.Add(new JahezAccountFee { HandoverId = h.Id, Amount = request.OpeningFees });
            Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.OpeningBalance, request.OpeningFees, h.Id, request.Reason);
            Entry(h, JahezLedgerBucket.PlatformDebt, JahezLedgerKind.OpeningBalance, request.OpeningDebt, h.Id, request.Reason);
            Entry(h, JahezLedgerBucket.Commission, JahezLedgerKind.OpeningBalance, request.OpeningCommission, h.Id, request.Reason);
            return await Response(h, ct);
        }, ct);

    public Task<Result<JahezHandoverResponse>> CloseAsync(string key, Guid id, JahezCloseRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "close", new { id, request }, PermissionKeys.Jahez.HandoversManage, async () =>
        {
            var h = await Handover(id, ct);
            await CloseHandover(h, request.EffectiveAtUtc, request.Reason, false, ct);
            return await Response(h, ct);
        }, ct);

    private async Task CloseHandover(JahezAccountHandover h, DateTimeOffset effective, string reason, bool transfer, CancellationToken ct)
    {
        Reason(reason);
        effective = effective.ToUniversalTime();
        Require(h.EndedAtUtc is null && effective >= h.StartedAtUtc && effective <= Now, JahezErrors.Conflict("فترة الإغلاق غير صالحة."));
        Require(!h.CommissionPostedThrough.HasValue || JahezRules.RiyadhDate(effective) >= h.CommissionPostedThrough.Value,
            JahezErrors.Conflict("الإغلاق يسبق فترة عمولة مرحلة."));
        Require(!await db.Set<JahezTransaction>().AnyAsync(x => x.HandoverId == h.Id && x.OccurredAtUtc >= effective
            && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == x.BatchId && b.CommittedAtUtc != null), ct),
            JahezErrors.Conflict("توجد حركات مستوردة بعد تاريخ الإغلاق؛ صحح إسنادها أولًا."));
        Require(!await db.Set<JahezDailyDispatch>().AnyAsync(x => x.HandoverId == h.Id && x.Date > JahezRules.RiyadhDate(effective)
            && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == x.BatchId && b.CommittedAtUtc != null), ct),
            JahezErrors.Conflict("توجد طلبات بعد تاريخ الإغلاق."));
        h.EndedAtUtc = effective;
        await PostCommission(h, JahezRules.RiyadhDate(effective), Guid.CreateVersion7(), reason, ct);
        h.DebtTransferred = transfer;
        if (transfer) Entry(h, JahezLedgerBucket.PlatformDebt, JahezLedgerKind.Transfer, 0m, Guid.CreateVersion7(), reason);
        var assignment = await db.RiderClientAssignments.SingleAsync(x => x.Id == h.RiderClientAssignmentId, ct);
        var from = assignment.Status;
        assignment.EffectiveTo = JahezRules.RiyadhDate(effective);
        assignment.Status = RiderAssignmentStatus.Ended;
        assignment.EndedByUserId = Actor;
        assignment.EndReason = reason;
        db.Add(new RiderAssignmentEvent { RiderClientAssignmentId = assignment.Id, FromStatus = from,
            ToStatus = RiderAssignmentStatus.Ended, OccurredAtUtc = Now, ActorUserId = Actor, Reason = reason });
        (await Account(h.PlatformRiderAccountId, ct)).Status = PlatformRiderAccountStatus.Available;
    }

    private async Task<(decimal Amount, List<string> Problems, DateOnly Through, string Evidence)> Commission(JahezAccountHandover h, DateOnly through, CancellationToken ct)
    {
        var end = h.EndedAtUtc.HasValue ? JahezRules.RiyadhDate(h.EndedAtUtc.Value) : Today;
        if (through > end) through = end;
        var start = h.CommissionPostedThrough?.AddDays(1) ?? h.CommissionStartsOn;
        if (start > through) return (0m, [], through, "[]");
        var policies = await db.Set<JahezCommissionPolicyPeriod>().AsNoTracking()
            .Where(x => x.HandoverId == h.Id && x.ToDate >= start && x.FromDate <= through).OrderBy(x => x.FromDate).ToArrayAsync(ct);
        var statements = await db.Set<JahezEarningsStatement>().AsNoTracking().Where(x => x.HandoverId == h.Id
            && !db.Set<JahezEarningsStatement>().Any(y => y.SupersedesId == x.Id)).OrderBy(x => x.FromDate).ToArrayAsync(ct);
        var cursor = start;
        decimal amount = 0m;
        List<string> problems = [];
        List<object> evidence = [];
        foreach (var p in policies)
        {
            var from = p.FromDate < start ? start : p.FromDate;
            var daily = JahezRules.DailyAmount(cursor, from.AddDays(-1));
            amount += daily;
            if (daily > 0) evidence.Add(new { Kind = "Daily", FromDate = cursor, ToDate = from.AddDays(-1), DailyRate = 15m, Amount = daily });
            var policyEnd = p.ToDate < through ? p.ToDate : through;
            var details = statements.Where(x => x.FromDate >= from && x.ToDate <= policyEnd).ToArray();
            var expected = from;
            decimal basis = 0m;
            foreach (var statement in details)
            {
                if (statement.FromDate != expected) break;
                basis += JahezRules.EarningsBase(statement);
                expected = statement.ToDate.AddDays(1);
            }
            var percentage = JahezRules.Money(Math.Max(0m, basis) * p.Rate);
            amount += percentage;
            evidence.Add(new { Kind = "Percentage", PolicyId = p.Id, p.ApprovalRequestId, FromDate = from, ToDate = policyEnd, p.Rate,
                Earnings = details.Select(x => new { x.Id, Basis = JahezRules.EarningsBase(x) }).ToArray(), Amount = percentage });
            if (expected != policyEnd.AddDays(1)) problems.Add($"تفاصيل الأرباح غير مكتملة للفترة {from:yyyy-MM-dd} — {policyEnd:yyyy-MM-dd}.");
            cursor = policyEnd.AddDays(1);
        }
        var tail = JahezRules.DailyAmount(cursor, through);
        amount += tail;
        if (tail > 0) evidence.Add(new { Kind = "Daily", FromDate = cursor, ToDate = through, DailyRate = 15m, Amount = tail });
        return (JahezRules.Money(amount), problems, through, JsonSerializer.Serialize(evidence));
    }

    private async Task PostCommission(JahezAccountHandover h, DateOnly through, Guid source, string reason, CancellationToken ct)
    {
        var calculation = await Commission(h, through, ct);
        Require(calculation.Problems.Count == 0, JahezErrors.Conflict(string.Join(" ", calculation.Problems)));
        if (calculation.Through >= h.CommissionStartsOn && (!h.CommissionPostedThrough.HasValue || calculation.Through > h.CommissionPostedThrough))
        {
            db.Add(new JahezLedgerEntry { Id = source, HandoverId = h.Id, Bucket = JahezLedgerBucket.Commission,
                Kind = JahezLedgerKind.Charge, Amount = calculation.Amount, SourceId = source, OccurredAtUtc = Now, Reason = reason,
                FromDate = h.CommissionPostedThrough?.AddDays(1) ?? h.CommissionStartsOn, ThroughDate = calculation.Through,
                CalculationJson = calculation.Evidence });
            h.CommissionPostedThrough = calculation.Through;
        }
    }

    private async Task<decimal> BucketBalance(Guid h, JahezLedgerBucket bucket, DateOnly through, CancellationToken ct)
    {
        var cut = JahezRules.StartOfDay(through.AddDays(1)).ToUniversalTime();
        var persisted = await db.Set<JahezLedgerEntry>().AsNoTracking().Where(x => x.HandoverId == h && x.Bucket == bucket
            && (x.Bucket != JahezLedgerBucket.PlatformDebt || x.Kind != JahezLedgerKind.Charge && x.ReversesEntryId == null || x.OccurredAtUtc < cut))
            .SumAsync(x => x.Amount, ct);
        return persisted + db.ChangeTracker.Entries<JahezLedgerEntry>().Where(x => x.State == EntityState.Added
            && x.Entity.HandoverId == h && x.Entity.Bucket == bucket && (bucket != JahezLedgerBucket.PlatformDebt || x.Entity.Kind != JahezLedgerKind.Charge && x.Entity.ReversesEntryId == null || x.Entity.OccurredAtUtc < cut)).Sum(x => x.Entity.Amount);
    }

    private async Task<JahezBalanceResponse> Balance(JahezAccountHandover h, DateOnly through, CancellationToken ct)
    {
        Require(through <= Today && through >= JahezRules.RiyadhDate(h.StartedAtUtc), JahezErrors.Invalid("تاريخ المعاينة خارج الفترة المتاحة."));
        Require(!h.CommissionPostedThrough.HasValue || through >= h.CommissionPostedThrough.Value,
            JahezErrors.Invalid("تاريخ المعاينة يسبق آخر فترة عمولة مرحلة؛ كشف الرصيد يعرض الالتزام الحالي."));
        var lastThrough = await db.Set<JahezRiderSettlement>().Where(x => x.HandoverId == h.Id && x.CountsAsSettlement).Select(x => (DateOnly?)x.ThroughDate).MaxAsync(ct);
        Require(!lastThrough.HasValue || through >= lastThrough.Value, JahezErrors.Invalid("تاريخ المعاينة يسبق آخر تصفية مسجلة."));
        var fees = JahezRules.Money(await BucketBalance(h.Id, JahezLedgerBucket.AccountFees, through, ct));
        var debt = JahezRules.Money(await BucketBalance(h.Id, JahezLedgerBucket.PlatformDebt, through, ct));
        var posted = JahezRules.Money(await BucketBalance(h.Id, JahezLedgerBucket.Commission, through, ct));
        var commission = await Commission(h, through, ct);
        var anchor = h.LastSettlementPaymentAtUtc ?? h.StartedAtUtc;
        var days = Today.DayNumber - JahezRules.RiyadhDate(anchor).DayNumber;
        var account = await db.PlatformRiderAccounts.IgnoreQueryFilters().SingleAsync(x => x.Id == h.PlatformRiderAccountId, ct);
        var latest = await db.Set<JahezTransaction>().Where(x => x.HandoverId == h.Id && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == x.BatchId && b.CommittedAtUtc != null)).Select(x => (DateTimeOffset?)x.OccurredAtUtc).MaxAsync(ct);
        return new(h.Id, h.PlatformRiderAccountId, account.ExternalAccountId, h.RiderProfileId, through, fees, debt, posted,
            commission.Amount, fees + debt + posted + commission.Amount, commission.Problems.Count == 0, commission.Problems,
            anchor, days, days > 10 && (h.EndedAtUtc is null || fees + debt + posted + commission.Amount > 0), h.DebtTransferred, latest);
    }

    internal Task<JahezBalanceResponse> BalanceForReminderAsync(JahezAccountHandover h, CancellationToken ct) => Balance(h, Today, ct);

    public Task<Result<JahezBalanceResponse>> GetBalanceAsync(Guid id, DateOnly through, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () => await Balance(await Handover(id, ct), through, ct), ct);

    public Task<Result<JahezPage<JahezHandoverResponse>>> GetHandoversAsync(Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            var query = db.Set<JahezAccountHandover>().AsNoTracking().Where(x => (!accountId.HasValue || x.PlatformRiderAccountId == accountId) && (!riderId.HasValue || x.RiderProfileId == riderId));
            var rows = await query.OrderByDescending(x => x.StartedAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            List<JahezHandoverResponse> result = [];
            foreach (var h in rows) result.Add(await Response(h, ct));
            return new JahezPage<JahezHandoverResponse>(result, page, pageSize);
        }, ct);

    public Task<Result<JahezPage<JahezBalanceResponse>>> GetDebtsAsync(Guid? riderId, bool overdueOnly, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            var threshold = JahezRules.StartOfDay(Today.AddDays(-10)).ToUniversalTime();
            var query = db.Set<JahezAccountHandover>().AsNoTracking().Where(x => !riderId.HasValue || x.RiderProfileId == riderId);
            if (overdueOnly) query = query.Where(x => (x.LastSettlementPaymentAtUtc ?? x.StartedAtUtc) < threshold
                && (x.EndedAtUtc == null || db.Set<JahezLedgerEntry>().Where(l => l.HandoverId == x.Id).Sum(l => l.Amount) > 0));
            var rows = await query.OrderBy(x => x.LastSettlementPaymentAtUtc ?? x.StartedAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            List<JahezBalanceResponse> result = [];
            foreach (var h in rows) result.Add(await Balance(h, Today, ct));
            return new JahezPage<JahezBalanceResponse>(result, page, pageSize);
        }, ct);

    public Task<Result<JahezPage<JahezLedgerEntry>>> GetLedgerAsync(Guid? riderId, Guid? handoverId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            var items = await db.Set<JahezLedgerEntry>().AsNoTracking().Where(x => (!handoverId.HasValue || x.HandoverId == handoverId)
                && (!riderId.HasValue || db.Set<JahezAccountHandover>().Any(h => h.Id == x.HandoverId && h.RiderProfileId == riderId)))
                .OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezLedgerEntry>(items, page, pageSize);
        }, ct);

    private sealed class JahezBusinessException(OperationError error) : Exception(error.Description)
    {
        public OperationError Error { get; } = error;
    }
}
