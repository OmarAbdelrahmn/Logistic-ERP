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
using LogisticsERP.Domain.Platform;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LogisticsERP.Infrastructure.Identity;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService(
    ApplicationDbContext db, ICurrentUser user, IPermissionChecker permissions, TimeProvider clock,
    IJahezTreasuryReportRenderer? treasuryRenderer = null,
    ILogger<JahezWorkflowService>? logger = null, IdentityDbContext? treasuryIdentity = null) : IJahezWorkflowService, IJahezTreasuryService
{
    private string? commandActorName;
    private static readonly Action<ILogger, string, Guid, Exception?> CommandCommittedLog = LoggerMessage.Define<string, Guid>(
        LogLevel.Information, new EventId(5600, "JahezCommandCommitted"), "Jahez command {Operation} committed for {Actor}");
    private static readonly Action<ILogger, string, string, Exception?> CommandRejectedLog = LoggerMessage.Define<string, string>(
        LogLevel.Warning, new EventId(5601, "JahezCommandRejected"), "Jahez command {Operation} rejected: {Code}");
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
            commandActorName = await TreasuryUserName(Actor, ct);
            var hash = Hash(JsonSerializer.SerializeToUtf8Bytes(payload));
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                var writeAccountIds = await DayWriteAccounts(payload, ct);
                await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(db, writeAccountIds, ct);
                await using var transaction = db.Database.IsRelational()
                    ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
                if (operation.StartsWith("v2.cashbox.", StringComparison.Ordinal))
                    Require(!await TreasuryActive(ct), JahezErrors.Conflict("treasury_upgrade_required: استخدم واجهة الخزينة V3 لتسليم العهدة."));
                var receipt = await db.Set<JahezCommandReceipt>().AsNoTracking().SingleOrDefaultAsync(
                    x => x.ActorUserId == Actor && x.Operation == operation && x.CommandKey == key, ct);
                if (receipt is not null)
                {
                    Require(receipt.PayloadHash == hash, JahezErrors.Conflict("استُخدم مفتاح عدم التكرار مع بيانات مختلفة."));
                    return Result.Success(JsonSerializer.Deserialize<T>(receipt.ResultJson)!);
                }
                var value = await action();
                await SynchronizeTreasuryAsync(ct);
                // Persist generated audit times and rowversions before freezing the response.
                // Both saves are protected by the same SQL transaction.
                await db.SaveChangesAsync(ct);
                var resultJson = JsonSerializer.Serialize(value);
                db.Add(new JahezCommandReceipt { ActorUserId = Actor, CommandKey = key, Operation = operation,
                    PayloadHash = hash, ResultJson = resultJson });
                await db.SaveChangesAsync(ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
                if (logger is not null) CommandCommittedLog(logger, operation, Actor, null);
                return Result.Success(JsonSerializer.Deserialize<T>(resultJson)!);
            });
        }
        catch (JahezBusinessException e)
        {
            db.ChangeTracker.Clear();
            if (logger is not null) CommandRejectedLog(logger, operation, e.Error.Code, null);
            return Result.Failure<T>(e.Error);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (logger is not null) CommandRejectedLog(logger, operation, "persistence_conflict", null);
            return Result.Failure<T>(JahezErrors.Conflict("تعارض في البيانات؛ أعد تحميل السجل وحاول مجددًا بالمفتاح نفسه."));
        }
        catch (Microsoft.Data.SqlClient.SqlException e) when (e.Number is 1205 or 1222)
        {
            db.ChangeTracker.Clear();
            if (logger is not null) CommandRejectedLog(logger, operation, "transaction_conflict", null);
            return Result.Failure<T>(JahezErrors.Conflict("تعارض متزامن؛ أعد المحاولة بالمفتاح نفسه."));
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void Require(bool condition, OperationError error) { if (!condition) throw new JahezBusinessException(error); }
    private static void Reason(string reason) => Require(!string.IsNullOrWhiteSpace(reason) && reason.Length <= 2000, JahezErrors.Invalid("السبب مطلوب، بحد أقصى 2000 حرف."));
    private static void Page(int page, int pageSize) => Require(page >= 1 && pageSize is >= 1 and <= 100 && page <= 100000, JahezErrors.Invalid("رقم الصفحة وحجمها غير صالحين؛ أقصى حجم 100."));
    private async Task<JahezAccountSession> Handover(Guid id, CancellationToken ct)
    {
        var h = await db.Set<JahezAccountSession>().SingleOrDefaultAsync(x => x.Id == id, ct);
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

    private async Task<JahezHandoverResponse> Response(JahezAccountSession h, CancellationToken ct)
    {
        var external = await db.PlatformRiderAccounts.Where(x => x.Id == h.PlatformRiderAccountId).Select(x => x.ExternalAccountId).SingleAsync(ct);
        return new(h.Id, h.PlatformRiderAccountId, external, h.RiderProfileId, h.RiderClientAssignmentId,
            h.StartedAtUtc, h.EndedAtUtc, h.CommissionStartsOn, h.CommissionPostedThrough, h.LastSettlementPaymentAtUtc);
    }

    private void Entry(JahezAccountSession h, JahezLedgerBucket bucket, JahezLedgerKind kind, decimal amount,
        Guid sourceId, string reason, DateTimeOffset? occurred = null, Guid? reverses = null) => db.Add(new JahezLedgerEntry
        { HandoverId = h.Id, Bucket = bucket, Kind = kind, Amount = amount, SourceId = sourceId,
            OccurredAtUtc = occurred ?? Now, Reason = reason, ReversesEntryId = reverses });

    public Task<Result<JahezHandoverResponse>> HandoverAsync(string key, JahezHandoverRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "handover", request, PermissionKeys.Jahez.HandoversCreate, async () =>
        {
            var h = await CreateHandover(request.AccountId, request.RiderProfileId, request.EffectiveAtUtc, request.Reason, 0m, null, ct);
            Require(request.InitialFeePayment is >= 0m and <= 200m && JahezRules.Money(request.InitialFeePayment) == request.InitialFeePayment,
                JahezErrors.Invalid("دفعة الرسوم غير صالحة."));
            if (request.InitialFeePayment > 0)
            {
                await AuthorizeAsync(JahezWorkflowPermissions.FeesRecord, ct);
                await AuthorizeAsync(JahezWorkflowPermissions.Receive, ct);
                var receipt = await StageMoney(h, JahezMoneyFund.Cashbox, JahezMoneyDirection.Receipt, JahezMoneyPurpose.AccountFee,
                    request.InitialFeePayment, request.PaymentReference, request.Reason, null, null, ct);
                Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Payment, -request.InitialFeePayment, receipt.Id, request.Reason);
            }
            return await Response(h, ct);
        }, ct);

    private async Task<JahezAccountSession> CreateHandover(Guid accountId, Guid riderId, DateTimeOffset effective,
        string reason, decimal waiver, Guid? approvalId, CancellationToken ct)
    {
        Reason(reason);
        effective = effective.ToUniversalTime();
        Require(effective <= Now && effective > DateTimeOffset.MinValue, JahezErrors.Invalid("تاريخ الاستلام يجب ألا يكون في المستقبل."));
        var account = await Account(accountId, ct);
        Require(account.SponsorId != Guid.Empty && await db.Sponsors.AnyAsync(x => x.Id == account.SponsorId, ct),
            JahezErrors.Invalid("كفيل الحساب غير صالح. استكمل كفيل الحساب قبل تسليم حساب جاهز."));
        Require(account.PaymentModel == PlatformAccountPaymentModel.PayPerOrder, JahezErrors.Invalid("وحدة جاهز تدعم حسابات الدفع حسب الطلب فقط."));
        Require(account.Status == PlatformRiderAccountStatus.Available, JahezErrors.Conflict("الحساب غير متاح للاستلام."));
        Require(!await db.RiderClientAssignments.AnyAsync(x => x.PlatformRiderAccountId == accountId && x.EffectiveTo == null, ct), JahezErrors.Conflict("الحساب مسند بالفعل."));
        Require(!await db.Set<JahezAccountSession>().AnyAsync(x => x.PlatformRiderAccountId == accountId && (x.EndedAtUtc == null || x.EndedAtUtc > effective), ct),
            JahezErrors.Conflict("تاريخ الاستلام يتداخل مع استخدام سابق."));
        var dayDates = await new PlatformAccountDayAllocationService(db).ValidateStart(accountId, JahezRules.RiyadhDate(effective), null, null, ct);
        Require(dayDates.IsSuccess, dayDates.Error);
        var rider = await (from r in db.RiderProfiles join e in db.Employees on r.EmployeeId equals e.Id
            where r.Id == riderId && e.Status == EmployeeStatus.Active select r).SingleOrDefaultAsync(ct);
        Require(rider is not null, JahezErrors.NotFound);
        var active = await PlatformAssignmentPolicySupport.CurrentAssignments(db, riderId).ToArrayAsync(ct);
        var stagedClosedIds = db.ChangeTracker.Entries<RiderClientAssignment>().Where(x => x.Entity.EffectiveTo != null).Select(x => x.Entity.Id).ToHashSet();
        active = active.Where(x => !stagedClosedIds.Contains(x.Id)).ToArray();
        Require(PlatformAssignmentPolicySupport.Evaluate("JAHEZ", active, account.PaymentModel) == PlatformAccountAssignmentDecision.Allowed,
            JahezErrors.Conflict("يسمح للمندوب بحساب جاهز واحد وحسابين شيفز كحد أقصى."));
        var contract = await db.ClientContracts.Where(x => x.ClientPlatformId == account.ClientPlatformId && x.Status == ClientContractStatus.Active).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (contract is null)
        {
            contract = new ClientContract { ClientPlatformId = account.ClientPlatformId, Code = "JAHEZ-OPERATIONS", DisplayNameAr = "تشغيل جاهز", DisplayNameEn = "Jahez operations", Status = ClientContractStatus.Active };
            db.Add(contract);
        }
        var assignment = new RiderClientAssignment { RiderProfileId = riderId, ClientContractId = contract.Id,
            PlatformRiderAccountId = accountId, PaymentModel = account.PaymentModel, RiderAccountSlot = PlatformAssignmentPolicySupport.NextSlot(active),
            EffectiveFrom = JahezRules.RiyadhDate(effective), Status = RiderAssignmentStatus.Active, StartReason = reason,
            AssignedByUserId = Actor, WasBackdated = JahezRules.RiyadhDate(effective) < Today,
            BackdatedReason = JahezRules.RiyadhDate(effective) < Today ? reason : null };
        db.Add(assignment);
        account.Status = PlatformRiderAccountStatus.Assigned;
        var h = new JahezAccountSession { PlatformRiderAccountId = accountId, RiderProfileId = riderId,
            RiderClientAssignmentId = assignment.Id, StartedAtUtc = effective, CommissionStartsOn = JahezRules.RiyadhDate(effective).AddDays(1), Reason = reason };
        db.Add(h);
        var dayStage = await new PlatformAccountDayAllocationService(db).Stage(accountId, assignment.Id, JahezRules.RiyadhDate(effective), null, reason, true, ct);
        Require(dayStage.IsSuccess, dayStage.Error);
        var fee = new JahezAccountFee { HandoverId = h.Id, WaivedAmount = waiver, ApprovalRequestId = approvalId };
        db.Add(fee);
        Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Charge, 200m - waiver, fee.Id, reason);
        db.Add(new RiderAssignmentEvent { RiderClientAssignmentId = assignment.Id, FromStatus = RiderAssignmentStatus.Planned,
            ToStatus = RiderAssignmentStatus.Active, OccurredAtUtc = Now, ActorUserId = Actor, Reason = reason });
        return h;
    }

    private async Task ValidateStopDate(JahezAccountSession h, DateTimeOffset effective, CancellationToken ct)
    {
        Require(h.EndedAtUtc is null && effective >= h.StartedAtUtc && effective <= Now, JahezErrors.Conflict("فترة الإغلاق غير صالحة."));
        Require(!h.CommissionPostedThrough.HasValue || JahezRules.RiyadhDate(effective) >= h.CommissionPostedThrough.Value,
            JahezErrors.Conflict("الإغلاق يسبق فترة عمولة مرحلة."));
        Require(!await db.Set<JahezTransaction>().AnyAsync(x => x.HandoverId == h.Id && x.OccurredAtUtc >= effective
            && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == x.BatchId && b.CommittedAtUtc != null), ct),
            JahezErrors.Conflict("توجد حركات مستوردة بعد تاريخ الإغلاق؛ صحح إسنادها أولًا."));
        Require(!await db.Set<JahezDailyDispatch>().AnyAsync(x => x.HandoverId == h.Id && x.Date > JahezRules.RiyadhDate(effective)
            && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == x.BatchId && b.CommittedAtUtc != null), ct),
            JahezErrors.Conflict("توجد طلبات بعد تاريخ الإغلاق."));
        var lastThrough = await db.Set<JahezOperation>().Where(x => x.HandoverId == h.Id && x.ExecutedAtUtc != null
            && x.Mode != JahezOperationMode.Withdraw).Select(x => (DateOnly?)x.ThroughDate).MaxAsync(ct);
        Require(!lastThrough.HasValue || JahezRules.RiyadhDate(effective) >= lastThrough.Value, JahezErrors.Conflict("السحب يسبق آخر تصفية."));
    }

    private async Task<(decimal Amount, List<string> Problems, DateOnly Through, string Evidence)> Commission(JahezAccountSession h, DateOnly through, CancellationToken ct, DateOnly? startOverride = null)
    {
        var end = h.EndedAtUtc.HasValue ? JahezRules.RiyadhDate(h.EndedAtUtc.Value) : Today;
        if (through > end) through = end;
        var start = startOverride ?? h.CommissionPostedThrough?.AddDays(1) ?? h.CommissionStartsOn;
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

    private async Task PostCommission(JahezAccountSession h, DateOnly through, Guid source, string reason, CancellationToken ct, DateOnly? startOverride = null)
    {
        var calculation = await Commission(h, through, ct, startOverride);
        Require(calculation.Problems.Count == 0, JahezErrors.Conflict(string.Join(" ", calculation.Problems)));
        if ((startOverride.HasValue || calculation.Through >= h.CommissionStartsOn) && (!h.CommissionPostedThrough.HasValue || calculation.Through > h.CommissionPostedThrough))
        {
            db.Add(new JahezLedgerEntry { Id = source, HandoverId = h.Id, Bucket = JahezLedgerBucket.Commission,
                Kind = JahezLedgerKind.Charge, Amount = calculation.Amount, SourceId = source, OccurredAtUtc = Now, Reason = reason,
                FromDate = startOverride ?? h.CommissionPostedThrough?.AddDays(1) ?? h.CommissionStartsOn, ThroughDate = calculation.Through,
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

    public Task<Result<JahezEarningsStatement>> RecordEarningsAsync(string key, JahezEarningsRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "earnings", request, PermissionKeys.Jahez.EarningsCreate, async () =>
        {
            Reason(request.Reason);
            var h = await Handover(request.HandoverId, ct);
            decimal?[] values = [request.TotalDeliveryPrice, request.TotalPenalties, request.TotalCashAmount, request.TotalDriverDebit,
                request.TotalServiceDeduction, request.TotalDriverCredit, request.TotalBonuses, request.TotalTips, request.TotalFreeOrders];
            Require(values.All(x => x.HasValue && x >= 0 && x <= 1_000_000_000m && decimal.Round(x.Value, 6) == x.Value),
                JahezErrors.Invalid("جميع تفاصيل الأرباح المالية مطلوبة، وتُدخل كمبالغ غير سالبة بدقة لا تتجاوز ست منازل."));
            Require(request.FromDate >= h.CommissionStartsOn && request.ToDate >= request.FromDate && request.ToDate <= Today
                && (!h.CommissionPostedThrough.HasValue || request.FromDate > h.CommissionPostedThrough)
                && (!h.EndedAtUtc.HasValue || request.ToDate <= JahezRules.RiyadhDate(h.EndedAtUtc.Value)), JahezErrors.Conflict("الفترة غير صالحة أو مصفاة بالفعل."));
            Require(await db.Set<JahezCommissionPolicyPeriod>().AnyAsync(x => x.HandoverId == h.Id && x.FromDate <= request.FromDate && x.ToDate >= request.ToDate, ct),
                JahezErrors.Conflict("لا توجد موافقة عمولة نسبة تغطي هذه الفترة."));
            if (request.SupersedesId.HasValue)
            {
                var old = await db.Set<JahezEarningsStatement>().SingleOrDefaultAsync(x => x.Id == request.SupersedesId, ct);
                Require(old is not null && old.HandoverId == h.Id && old.FromDate == request.FromDate && old.ToDate == request.ToDate,
                    JahezErrors.Conflict("التصحيح يجب أن يحافظ على الحساب والفترة الأصلية."));
                Require(!await db.Set<JahezEarningsStatement>().AnyAsync(x => x.SupersedesId == request.SupersedesId, ct), JahezErrors.Conflict("تم استبدال البيان بالفعل."));
            }
            Require(!await db.Set<JahezEarningsStatement>().AnyAsync(x => x.HandoverId == h.Id && x.Id != request.SupersedesId
                && x.FromDate <= request.ToDate && x.ToDate >= request.FromDate && !db.Set<JahezEarningsStatement>().Any(y => y.SupersedesId == x.Id), ct),
                JahezErrors.Conflict("تفاصيل الأرباح تتداخل مع فترة أخرى."));
            var entity = new JahezEarningsStatement { HandoverId = h.Id, FromDate = request.FromDate, ToDate = request.ToDate,
                TotalDeliveryPrice = request.TotalDeliveryPrice!.Value, TotalPenalties = request.TotalPenalties!.Value,
                TotalCashAmount = request.TotalCashAmount!.Value, TotalDriverDebit = request.TotalDriverDebit!.Value,
                TotalServiceDeduction = request.TotalServiceDeduction!.Value, TotalDriverCredit = request.TotalDriverCredit!.Value,
                TotalBonuses = request.TotalBonuses!.Value, TotalTips = request.TotalTips!.Value, TotalFreeOrders = request.TotalFreeOrders!.Value,
                SupersedesId = request.SupersedesId, Reason = request.Reason };
            db.Add(entity);
            return entity;
        }, ct);

    private void NotifyDecision(JahezApprovalRequest request, JahezApprovalDecision decision) => db.Add(new Notification
    {
        RecipientUserId = request.RequestedByUserId, EventType = "jahez.approval.decided", TitleAr = "قرار طلب جاهز",
        TitleEn = "Jahez request decision", BodyAr = $"تم {(decision.Status == JahezApprovalStatus.Approved ? "اعتماد" : "رفض")} طلبك: {NotificationReason(decision.Reason)}",
        BodyEn = $"Request {decision.Status}: {NotificationReason(decision.Reason)}", SourceEntityType = "JahezApprovalRequest", SourceEntityId = request.Id,
        DeepLink = $"/jahez/requests/{request.Id}", DeduplicationKey = $"jahez:decision:{request.Id:N}", VisibleAtUtc = Now
    });

    private static string NotificationReason(string reason) => reason.Length <= 1500 ? reason : reason[..1500];

    public Task<Result<JahezPage<JahezHandoverResponse>>> GetHandoversAsync(Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            var query = db.Set<JahezAccountSession>().AsNoTracking().Where(x => (!accountId.HasValue || x.PlatformRiderAccountId == accountId)
                && (!riderId.HasValue || x.RiderProfileId == riderId));
            var rows = await (from h in query join account in db.PlatformRiderAccounts on h.PlatformRiderAccountId equals account.Id
                orderby h.StartedAtUtc descending, h.Id
                select new JahezHandoverResponse(h.Id, account.Id, account.ExternalAccountId, h.RiderProfileId,
                    h.RiderClientAssignmentId, h.StartedAtUtc, h.EndedAtUtc, h.CommissionStartsOn, h.CommissionPostedThrough, h.LastSettlementPaymentAtUtc))
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezHandoverResponse>(rows, page, pageSize);
        }, ct);

    private sealed class JahezBusinessException(OperationError error) : Exception(error.Description)
    {
        public OperationError Error { get; } = error;
    }
}
