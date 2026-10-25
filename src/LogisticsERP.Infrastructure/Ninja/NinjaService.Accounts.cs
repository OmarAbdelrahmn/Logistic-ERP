using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    public async Task<Result<NinjaPage<NinjaAccountResponse>>> GetAccountsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (!ValidPage(page, pageSize)) return Result.Failure<NinjaPage<NinjaAccountResponse>>(NinjaErrors.Invalid("حجم الصفحة من 1 إلى 100."));
        var ids = await AllowedPlatforms(PermissionKeys.Ninja.Read, ct);
        if (ids.Length == 0) return Result.Failure<NinjaPage<NinjaAccountResponse>>(NinjaErrors.Forbidden);
        var rows = await AccountResponses(Accounts.Where(a => ids.Contains(a.ClientPlatformId)), page, pageSize, ct, includePassword: true);
        return Result.Success(new NinjaPage<NinjaAccountResponse>(rows, page, pageSize));
    }

    public async Task<Result<NinjaAccountResponse>> GetAccountAsync(Guid accountId, CancellationToken ct = default)
    {
        var ids = await AllowedPlatforms(PermissionKeys.Ninja.Read, ct);
        if (ids.Length == 0) return Result.Failure<NinjaAccountResponse>(NinjaErrors.Forbidden);
        var rows = await AccountResponses(Accounts.Where(a => ids.Contains(a.ClientPlatformId) && a.Id == accountId), 1, 1, ct, includePassword: true);
        return rows.Length == 0 ? Result.Failure<NinjaAccountResponse>(NinjaErrors.NotFound) : Result.Success(rows[0]);
    }

    public async Task<Result<NinjaAccountResponse>> SetAccountStatusAsync(Guid accountId, NinjaAccountStatusRequest request, CancellationToken ct = default)
    {
        var account = await Accounts.SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null) return Result.Failure<NinjaAccountResponse>(NinjaErrors.NotFound);
        if (!await Allowed(PermissionKeys.Ninja.AccountsUpdate, account.ClientPlatformId, ct))
            return Result.Failure<NinjaAccountResponse>(NinjaErrors.Forbidden);
        if (request.Status is not ("Active" or "Suspended" or "Retired" or "Archived") || request.Reason?.Trim().Length > 500)
            return Result.Failure<NinjaAccountResponse>(NinjaErrors.Invalid("اختر Active أو Suspended أو Retired أو Archived؛ السبب بحد أقصى 500 حرف."));
        if (!HrServiceSupport.MatchesRowVersion(account.RowVersion, request.RowVersion))
            return Result.Failure<NinjaAccountResponse>(NinjaErrors.Conflict);
        if (request.Status == "Active" && await RiderOperationalBlock.IsAccountBlocked(db, accountId, ct))
            return Result.Failure<NinjaAccountResponse>(RiderOperationalBlock.Error);
        var assigned = await db.RiderClientAssignments.AnyAsync(a => a.PlatformRiderAccountId == accountId && a.EffectiveTo == null, ct);
        account.Status = request.Status == "Active"
            ? assigned ? PlatformRiderAccountStatus.Assigned : PlatformRiderAccountStatus.Available
            : Enum.Parse<PlatformRiderAccountStatus>(request.Status);
        account.StatusReason = request.Status == "Active" ? null : HrServiceSupport.TrimOrNull(request.Reason);
        // Serialize status edits with assignment, return and credential changes, even on a no-op.
        db.Entry(account).Property(a => a.Status).IsModified = true;
        var saved = await Persist(true, ct);
        if (saved.IsFailure) return Result.Failure<NinjaAccountResponse>(saved.Error);
        var rows = await AccountResponses(Accounts.Where(a => a.Id == accountId), 1, 1, ct);
        return Result.Success(rows[0]);
    }

    private async Task<NinjaAccountResponse[]> AccountResponses(IQueryable<PlatformRiderAccount> accounts, int page, int pageSize,
        CancellationToken ct, bool includePassword = false)
    {
        var rows = await (from a in accounts.AsNoTracking()
            join d in db.NinjaAccountDetails.AsNoTracking() on a.Id equals d.PlatformRiderAccountId into details
            from d in details.DefaultIfEmpty()
            join assignment in db.RiderClientAssignments.AsNoTracking().Where(r => r.EffectiveTo == null)
                on a.Id equals assignment.PlatformRiderAccountId into assignments
            from assignment in assignments.DefaultIfEmpty()
            orderby a.ExternalAccountId, a.Id
            select new { Account = a, Details = d,
                RiderId = assignment == null ? null : (Guid?)assignment.RiderProfileId,
                AssignmentId = assignment == null ? null : (Guid?)assignment.Id,
                AssignmentRowVersion = assignment == null ? null : assignment.RowVersion,
                RiderType = assignment == null ? null : assignment.NinjaRiderType,
                PaymentModel = assignment == null ? a.PaymentModel : assignment.PaymentModel,
                Credential = db.PlatformAccountCredentialVersions.AsNoTracking().Where(v => v.PlatformRiderAccountId == a.Id)
                    .OrderByDescending(v => v.KeyVersion)
                    .Select(v => new { v.Ciphertext, v.Nonce, v.AuthenticationTag, v.RotatedAtUtc }).FirstOrDefault() })
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        var passwordPlatforms = new HashSet<Guid>();
        if (includePassword)
        {
            foreach (var platformId in rows.Select(r => r.Account.ClientPlatformId).Distinct())
                if (await Allowed(PermissionKeys.Operations.PlatformCredentialsRead, platformId, ct))
                    passwordPlatforms.Add(platformId);
        }
        var riderIds = rows.Where(r => r.RiderId.HasValue).Select(r => r.RiderId!.Value).ToArray();
        var riders = await db.RiderProfiles.AsNoTracking().Where(r => riderIds.Contains(r.Id))
            .Select(r => new { r.Id, r.EmployeeId }).ToDictionaryAsync(r => r.Id, ct);
        var employeeIds = rows.Where(r => r.Account.RegisteredEmployeeId.HasValue).Select(r => r.Account.RegisteredEmployeeId!.Value)
            .Concat(riders.Values.Select(r => r.EmployeeId)).Distinct().ToArray();
        var people = await People(employeeIds, ct);
        var accountIds = rows.Select(r => r.Account.Id).ToArray();
        var latestSheets = await db.NinjaAccountUpdateRows.AsNoTracking().Where(r => r.PlatformRiderAccountId != null
                && accountIds.Contains(r.PlatformRiderAccountId.Value) && r.MatchStatus == "Found")
            .GroupBy(r => r.PlatformRiderAccountId!.Value)
            .Select(g => g.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).First())
            .ToDictionaryAsync(r => r.PlatformRiderAccountId!.Value, ct);
        var blocked = await RiderOperationalBlock.BlockedAssignmentIds(db,
            rows.Where(r => r.AssignmentId.HasValue).Select(r => r.AssignmentId!.Value).ToArray(), ct);
        return rows.Select(r => new NinjaAccountResponse(r.Account.Id, r.Account.ExternalAccountId, r.Account.Status.ToString(),
            r.Account.RegisteredEmployeeId, r.RiderId, r.Details?.PhoneNumber, r.Credential is not null, r.Credential?.RotatedAtUtc.AddHours(72),
            Convert.ToBase64String(r.Account.RowVersion), r.AssignmentId,
            r.AssignmentRowVersion is null ? null : Convert.ToBase64String(r.AssignmentRowVersion))
        {
            Password = passwordPlatforms.Contains(r.Account.ClientPlatformId) && r.Credential is { } credential
                ? protector.Unprotect(new(credential.Ciphertext, credential.Nonce, credential.AuthenticationTag)) : null,
            Owner = r.Account.RegisteredEmployeeId is { } ownerId ? people.GetValueOrDefault(ownerId) : null,
            ActualRider = r.RiderId is { } riderId && riders.TryGetValue(riderId, out var rider) ? people.GetValueOrDefault(rider.EmployeeId) : null,
            RiderType = r.RiderType?.ToString(),
            PaymentModel = r.PaymentModel.ToString(),
            StatusReason = r.Account.StatusReason, Code = r.Account.Code, OperatingCityId = r.Account.OperatingCityId, SponsorId = r.Account.SponsorId,
            LatestSheetStatus = latestSheets.GetValueOrDefault(r.Account.Id)?.SheetStatus,
            LatestSheetSuspended = latestSheets.GetValueOrDefault(r.Account.Id)?.SheetSuspended,
            LatestSheetUploadId = latestSheets.GetValueOrDefault(r.Account.Id)?.UploadId,
            SheetUpdatedAtUtc = latestSheets.GetValueOrDefault(r.Account.Id)?.CreatedAtUtc,
            IsOperationallyBlocked = r.AssignmentId.HasValue && blocked.Contains(r.AssignmentId.Value),
            OperationalBlockReason = r.AssignmentId.HasValue && blocked.Contains(r.AssignmentId.Value) ? RiderOperationalBlock.Reason : null
        }).ToArray();
    }

    private Task<Dictionary<Guid, NinjaPersonResponse>> People(Guid[] employeeIds, CancellationToken ct) =>
        db.Employees.AsNoTracking().Where(e => employeeIds.Contains(e.Id))
            .Select(e => new NinjaPersonResponse(e.Id, e.FullNameAr, e.FullNameEn, e.PrimaryPhone, e.IqamaNo))
            .ToDictionaryAsync(e => e.EmployeeId, ct);
}
