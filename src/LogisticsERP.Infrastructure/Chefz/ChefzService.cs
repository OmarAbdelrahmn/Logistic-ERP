using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService(ApplicationDbContext db, ICurrentUser user, IPermissionChecker permissions,
    TimeProvider clock, IPlatformCredentialProtector protector) : IChefzService
{
    private IQueryable<PlatformRiderAccount> Accounts =>
        from a in db.PlatformRiderAccounts join p in db.ClientPlatforms on a.ClientPlatformId equals p.Id
        where p.Code == "CHEFZ" || p.Code == "THECHEFZ" || p.Code == "CHEFSZ" || p.Code == "SHIFTZ" select a;

    private async Task<bool> Allowed(string permission, CancellationToken ct)
    {
        if (user.UserId is not { } id || user.AuthorizationVersion is not { } version) return false;
        var platforms = await db.ClientPlatforms.Where(p => p.Code == "CHEFZ" || p.Code == "THECHEFZ" || p.Code == "CHEFSZ" || p.Code == "SHIFTZ")
            .Select(p => p.Id).ToArrayAsync(ct);
        if (platforms.Length == 0) return false;
        foreach (var platform in platforms)
            if (!await permissions.HasPermissionAsync(id, version, permission,
                new(AccessScopeType.ClientPlatform, platform), ct)) return false;
        return true;
    }

    private static bool ValidPage(int page, int size) => page is >= 1 and <= 100000 && size is >= 1 and <= 100;

    private async Task<Result<T>> Persist<T>(T response, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return Result.Success(response); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return Result.Failure<T>(ChefzErrors.Conflict("تعارض في البيانات؛ أعد تحميل السجل وحاول مجدداً."));
        }
    }

    private static ChefzAccountResponse AccountResponse(PlatformRiderAccount account, ChefzAccountDetails? details, bool password) =>
        new(account.Id, account.ExternalAccountId, details?.ReportIdNumber ?? account.ExternalAccountId, details?.AccountType.ToString() ?? "Unconfigured",
            details?.AccountType == ChefzAccountType.Freelancer ? ChefzRules.FreelancerRate : 0,
            details?.AccountType == ChefzAccountType.FullTime ? ChefzRules.FullTimeDailyFee : 0,
            password, details is null ? "" : Convert.ToBase64String(details.RowVersion), account.SponsorId, account.OperatingCityId,
            details?.VehicleType ?? "Unknown", details?.DashboardStatus ?? account.Status.ToString(),
            account.Status.ToString(), account.StatusReason, Convert.ToBase64String(account.RowVersion));

    public async Task<Result<ChefzAccountResponse>> SetAccountStatusAsync(Guid accountId, ChefzAccountStatusRequest request, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.AccountsUpdate, ct))
            return Result.Failure<ChefzAccountResponse>(ChefzErrors.Forbidden);
        if (request.Status is not ("Active" or "Suspended" or "Retired" or "Archived") || request.Reason?.Trim().Length > 500)
            return Result.Failure<ChefzAccountResponse>(ChefzErrors.Invalid("اختر Suspended أو Retired أو Archived أو Active لاستعادة الحالة التلقائية، والسبب بحد أقصى 500 حرف."));
        var account = await Accounts.SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null) return Result.Failure<ChefzAccountResponse>(ChefzErrors.NotFound);
        if (request.Status == "Active" && await RiderOperationalBlock.IsAccountBlocked(db, accountId, ct))
            return Result.Failure<ChefzAccountResponse>(RiderOperationalBlock.Error);
        if (!HrServiceSupport.MatchesRowVersion(account.RowVersion, request.RowVersion))
            return Result.Failure<ChefzAccountResponse>(ChefzErrors.Conflict("تم تعديل الحساب؛ أعد تحميله وحاول مجدداً."));

        var status = request.Status == "Active"
            ? ChefzAccountStatusRules.Active(await db.RiderClientAssignments.AnyAsync(a => a.PlatformRiderAccountId == accountId && a.EffectiveTo == null, ct))
            : Enum.Parse<PlatformRiderAccountStatus>(request.Status);
        account.Status = status;
        account.StatusReason = request.Status == "Active" ? null : HrServiceSupport.TrimOrNull(request.Reason);
        // Always participate in account concurrency, including restoring an already active account.
        db.Entry(account).Property(a => a.Status).IsModified = true;
        var result = await Persist(true, ct);
        if (result.IsFailure) return Result.Failure<ChefzAccountResponse>(result.Error);
        var details = await db.ChefzAccountDetails.AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
        return Result.Success(AccountResponse(account, details,
            await db.PlatformAccountCredentialVersions.AnyAsync(v => v.PlatformRiderAccountId == accountId, ct)));
    }

    public async Task<Result<ChefzAccountResponse>> GetAccountAsync(Guid accountId, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzAccountResponse>(ChefzErrors.Forbidden);
        var account = await Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        var details = await db.ChefzAccountDetails.AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
        if (account is null) return Result.Failure<ChefzAccountResponse>(ChefzErrors.NotFound);
        var blocked = await RiderOperationalBlock.IsAccountBlocked(db, accountId, ct);
        return Result.Success(AccountResponse(account, details, await db.PlatformAccountCredentialVersions.AnyAsync(v => v.PlatformRiderAccountId == accountId, ct))
            with { IsOperationallyBlocked = blocked, OperationalBlockReason = blocked ? RiderOperationalBlock.Reason : null });
    }

    public async Task<Result<ChefzAccountResponse>> SaveAccountAsync(Guid accountId, ChefzAccountDetailsRequest request, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.AccountsUpdate, ct)) return Result.Failure<ChefzAccountResponse>(ChefzErrors.Forbidden);
        if (!Enum.TryParse<ChefzAccountType>(request.AccountType, true, out var type) || !Enum.IsDefined(type)
            || request.ReportIdNumber?.Trim().Length > 100
            || request.VehicleType is not (null or "Car" or "Motorcycle")
            || request.DashboardStatus is not (null or "active" or "Block By CHEFZ" or "The supervisor suspended"))
            return Result.Failure<ChefzAccountResponse>(ChefzErrors.Invalid("حدد نوع الحساب Freelancer أو FullTime ورقم الهوية الموجود في التقرير."));
        await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(db, [accountId], ct);
        var account = await Accounts.SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null) return Result.Failure<ChefzAccountResponse>(ChefzErrors.NotFound);
        var ownerId = await db.Employees.AsNoTracking().Where(e => e.Id == account.RegisteredEmployeeId).Select(e => e.IqamaNo).SingleOrDefaultAsync(ct);
        var reportId = ChefzSpreadsheetParser.NormalizeId(!string.IsNullOrWhiteSpace(request.ReportIdNumber)
            ? request.ReportIdNumber : !string.IsNullOrWhiteSpace(ownerId) ? ownerId : account.ExternalAccountId);
        if (reportId.Length is 0 or > 100)
            return Result.Failure<ChefzAccountResponse>(ChefzErrors.Invalid("أضف رقم الهوية الموجود في تقرير شيفز."));
        if (await db.ChefzAccountDetails.AnyAsync(d => d.ReportIdNumber == reportId && d.PlatformRiderAccountId != accountId, ct))
            return Result.Failure<ChefzAccountResponse>(ChefzErrors.Conflict("رقم الهوية مرتبط بحساب شيفز آخر."));
        var details = await db.ChefzAccountDetails.SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
        if (details is not null && !HrServiceSupport.MatchesRowVersion(details.RowVersion, request.RowVersion))
            return Result.Failure<ChefzAccountResponse>(ChefzErrors.Conflict("تم تعديل البيانات؛ أعد تحميل الحساب."));
        if (type == ChefzAccountType.FullTime && (details is null || details.AccountType != ChefzAccountType.FullTime))
        {
            var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => a.PlatformRiderAccountId == accountId
                && a.Status != RiderAssignmentStatus.Cancelled).ToArrayAsync(ct);
            if (assignments.Any(a => assignments.Any(b => b.Id != a.Id && a.EffectiveFrom <= (b.EffectiveTo ?? DateOnly.MaxValue)
                && b.EffectiveFrom <= (a.EffectiveTo ?? DateOnly.MaxValue))))
                return Result.Failure<ChefzAccountResponse>(ChefzErrors.Conflict("Cannot convert an account with shared-day assignments to FullTime."));
        }
        if (details is null)
        {
            details = new() { PlatformRiderAccountId = accountId };
            db.ChefzAccountDetails.Add(details);
        }
        details.AccountType = type;
        details.ReportIdNumber = reportId;
        if (request.VehicleType is not null) details.VehicleType = request.VehicleType;
        // Workbook metadata must not override an admin's lifecycle selection.
        if (request.DashboardStatus is not null) details.DashboardStatus = request.DashboardStatus;
        var result = await Persist(true, ct);
        if (result.IsFailure) return Result.Failure<ChefzAccountResponse>(result.Error);
        return Result.Success(AccountResponse(account, details, await db.PlatformAccountCredentialVersions.AnyAsync(v => v.PlatformRiderAccountId == accountId, ct)));
    }

    public async Task<Result> SetPasswordAsync(Guid accountId, ChefzPasswordRequest request, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Operations.PlatformCredentialsRotate, ct)) return Result.Failure(ChefzErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length > 4096
            || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            return Result.Failure(ChefzErrors.Invalid("كلمة المرور وسبب حفظها مطلوبان."));
        if (!await Accounts.AnyAsync(a => a.Id == accountId, ct)) return Result.Failure(ChefzErrors.NotFound);
        var latest = await db.PlatformAccountCredentialVersions.Where(v => v.PlatformRiderAccountId == accountId)
            .OrderByDescending(v => v.KeyVersion).FirstOrDefaultAsync(ct);
        var encrypted = protector.Protect(request.Password);
        db.PlatformAccountCredentialVersions.Add(new()
        {
            PlatformRiderAccountId = accountId, Ciphertext = encrypted.Ciphertext, Nonce = encrypted.Nonce,
            AuthenticationTag = encrypted.AuthenticationTag, KeyVersion = (latest?.KeyVersion ?? 0) + 1,
            RotatedAtUtc = clock.GetUtcNow(), RotatedByUserId = user.UserId!.Value,
            RotationReason = request.Reason.Trim(), SupersededVersionId = latest?.Id
        });
        return await Persist(true, ct);
    }

    public async Task<Result<string>> GetPasswordAsync(Guid accountId, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Operations.PlatformCredentialsRead, ct)) return Result.Failure<string>(ChefzErrors.Forbidden);
        if (!await Accounts.AnyAsync(a => a.Id == accountId, ct)) return Result.Failure<string>(ChefzErrors.NotFound);
        var latest = await db.PlatformAccountCredentialVersions.AsNoTracking().Where(v => v.PlatformRiderAccountId == accountId)
            .OrderByDescending(v => v.KeyVersion).FirstOrDefaultAsync(ct);
        if (latest is null) return Result.Failure<string>(ChefzErrors.NotFound);
        return Result.Success(protector.Unprotect(new(latest.Ciphertext, latest.Nonce, latest.AuthenticationTag)));
    }

    private static ChefzSettlementResponse SettlementResponse(ChefzWalletSettlement s) => new(s.Id,
        s.PlatformRiderAccountId, s.RiderClientAssignmentId, s.RiderProfileId, s.AccountType.ToString(),
        s.PeriodFrom, s.PeriodTo, s.ChargedFrom, s.ChargedTo, s.AssignedDays, s.StartWalletAmount,
        s.EndWalletAmount, s.GrossEarnings, s.CommissionRate, s.DailyFee, s.CompanyAmount, s.RiderAmount,
        s.Source, s.ImportId, s.SourceRows, s.DeliveryFees, s.VatAmount, s.Tips, s.Compensation, s.Bonuses, s.Deductions);

    private sealed record RiderIdentity(Guid RiderProfileId, string RiderName, string? RiderIqamaNo);

    private async Task<Dictionary<Guid, RiderIdentity>> RiderIdentities(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var riderIds = ids.Distinct().ToArray();
        if (riderIds.Length == 0) return [];
        // Historical reports identify the recorded worker, including former employees, never the account owner.
        return await (from rider in db.RiderProfiles.IgnoreQueryFilters().AsNoTracking()
            join employee in db.Employees.IgnoreQueryFilters().AsNoTracking() on rider.EmployeeId equals employee.Id
            where riderIds.Contains(rider.Id)
            select new RiderIdentity(rider.Id, employee.FullNameAr, employee.IqamaNo))
            .ToDictionaryAsync(r => r.RiderProfileId, ct);
    }

    private async Task<ChefzSettlementResponse[]> SettlementResponses(ChefzWalletSettlement[] settlements, CancellationToken ct)
    {
        if (settlements.Length == 0) return [];
        var accountIds = settlements.Select(s => s.PlatformRiderAccountId).Distinct().ToArray();
        var riders = await RiderIdentities(settlements.Select(s => s.RiderProfileId), ct);
        var sponsors = await (from account in Accounts.AsNoTracking()
            join sponsor in db.Sponsors.AsNoTracking() on account.SponsorId equals sponsor.Id
            where accountIds.Contains(account.Id)
            select new { account.Id, sponsor.RegistryNameAr, sponsor.RegistryNameEn }).ToDictionaryAsync(s => s.Id, ct);
        return settlements.Select(s =>
        {
            var sponsor = sponsors.GetValueOrDefault(s.PlatformRiderAccountId);
            var rider = riders.GetValueOrDefault(s.RiderProfileId);
            return SettlementResponse(s) with
            {
                SponsorNameAr = sponsor?.RegistryNameAr, SponsorNameEn = sponsor?.RegistryNameEn,
                RiderName = rider?.RiderName ?? "مندوب سابق", RiderIqamaNo = rider?.RiderIqamaNo
            };
        }).ToArray();
    }

    private async Task<ChefzSettlementResponse> SettlementWithSponsor(ChefzWalletSettlement settlement, CancellationToken ct) =>
        (await SettlementResponses([settlement], ct))[0];

    private async Task<Result<ChefzWalletSettlement>> Calculate(ChefzSettlementRequest request, CancellationToken ct)
    {
        var assignment = await db.RiderClientAssignments.AsNoTracking().SingleOrDefaultAsync(a => a.Id == request.AssignmentId
            && (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended), ct);
        if (assignment is null || !await Accounts.AnyAsync(a => a.Id == assignment.PlatformRiderAccountId, ct))
            return Result.Failure<ChefzWalletSettlement>(ChefzErrors.NotFound);
        var details = await db.ChefzAccountDetails.AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == assignment.PlatformRiderAccountId, ct);
        if (details is null) return Result.Failure<ChefzWalletSettlement>(ChefzErrors.Invalid("أضف نوع الحساب أولاً."));
        try
        {
            var settlement = ChefzRules.Calculate(details.AccountType, request.Year, request.Month, request.Half,
                assignment.EffectiveFrom, assignment.EffectiveTo, request.StartWalletAmount, request.EndWalletAmount);
            settlement.PlatformRiderAccountId = assignment.PlatformRiderAccountId;
            settlement.RiderClientAssignmentId = assignment.Id;
            settlement.RiderProfileId = assignment.RiderProfileId;
            return Result.Success(settlement);
        }
        catch (ArgumentException) { return Result.Failure<ChefzWalletSettlement>(ChefzErrors.Invalid("تحقق من الفترة والتكليف ورصيدي البداية والنهاية؛ النهاية يجب ألا تقل عن البداية.")); }
    }

    public async Task<Result<ChefzSettlementResponse>> PreviewSettlementAsync(ChefzSettlementRequest request, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzSettlementResponse>(ChefzErrors.Forbidden);
        var result = await Calculate(request, ct);
        if (result.IsFailure) return Result.Failure<ChefzSettlementResponse>(result.Error);
        result.Value!.Id = Guid.Empty;
        return Result.Success(await SettlementWithSponsor(result.Value, ct));
    }

    public async Task<Result<ChefzSettlementResponse>> SaveSettlementAsync(ChefzSettlementRequest request, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.SettlementsCreate, ct)) return Result.Failure<ChefzSettlementResponse>(ChefzErrors.Forbidden);
        var result = await Calculate(request, ct);
        if (result.IsFailure) return Result.Failure<ChefzSettlementResponse>(result.Error);
        var settlement = result.Value!;
        var existing = await db.ChefzWalletSettlements.AsNoTracking().SingleOrDefaultAsync(s => s.RiderClientAssignmentId == request.AssignmentId
            && s.PeriodFrom == settlement.PeriodFrom, ct);
        if (existing is not null)
            return existing.Source == "Wallet" && existing.StartWalletAmount == request.StartWalletAmount && existing.EndWalletAmount == request.EndWalletAmount
                ? Result.Success(await SettlementWithSponsor(existing, ct))
                : Result.Failure<ChefzSettlementResponse>(ChefzErrors.Conflict("تم حفظ تصفية لهذه الفترة؛ لا يمكن تكرارها بمبالغ مختلفة."));
        // Do not freeze future days on an assignment that is still in progress.
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (settlement.ChargedTo > today)
            return Result.Failure<ChefzSettlementResponse>(ChefzErrors.Invalid("لا يمكن حفظ تصفية قبل انتهاء أيام التكليف في الفترة؛ يمكنك معاينتها."));
        db.ChefzWalletSettlements.Add(settlement);
        return await Persist(await SettlementWithSponsor(settlement, ct), ct);
    }

    public async Task<Result<ChefzPage<ChefzSettlementResponse>>> GetSettlementsAsync(Guid? accountId, Guid? riderId,
        int page, int pageSize, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzPage<ChefzSettlementResponse>>(ChefzErrors.Forbidden);
        if (!ValidPage(page, pageSize)) return Result.Failure<ChefzPage<ChefzSettlementResponse>>(ChefzErrors.Invalid("حجم الصفحة من 1 إلى 100."));
        var rows = await db.ChefzWalletSettlements.AsNoTracking()
            .Where(s => (accountId == null || s.PlatformRiderAccountId == accountId) && (riderId == null || s.RiderProfileId == riderId))
            .OrderByDescending(s => s.PeriodFrom).ThenBy(s => s.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new ChefzPage<ChefzSettlementResponse>(await SettlementResponses(rows, ct), page, pageSize));
    }
}
