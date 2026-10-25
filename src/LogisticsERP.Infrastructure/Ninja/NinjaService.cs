using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService(ApplicationDbContext db, ICurrentUser user, IPermissionChecker permissions,
    TimeProvider clock, IPlatformCredentialProtector protector, SimplePlatformService platforms) : INinjaService
{
    private IQueryable<PlatformRiderAccount> Accounts => from a in db.PlatformRiderAccounts
        join p in db.ClientPlatforms on a.ClientPlatformId equals p.Id where p.Code == "NINJA" select a;

    private Task<bool> Allowed(string permission, Guid platformId, CancellationToken ct) =>
        user.UserId is { } id && user.AuthorizationVersion is { } version
            ? permissions.HasPermissionAsync(id, version, permission, new(AccessScopeType.ClientPlatform, platformId), ct)
            : Task.FromResult(false);

    private async Task<Guid[]> AllowedPlatforms(string permission, CancellationToken ct)
    {
        var ids = await db.ClientPlatforms.AsNoTracking().Where(p => p.Code == "NINJA").Select(p => p.Id).ToArrayAsync(ct);
        var allowed = new List<Guid>();
        foreach (var id in ids) if (await Allowed(permission, id, ct)) allowed.Add(id);
        return allowed.ToArray();
    }
    private static bool ValidPage(int page, int size) => page is >= 1 and <= 100000 && size is >= 1 and <= 100;

    private async Task<Result<T>> Persist<T>(T response, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return Result.Success(response); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<T>(NinjaErrors.Conflict); }
    }

    public Task<Result<NinjaLifecycleResponse>> AssignAsync(Guid accountId, NinjaAssignRequest request, CancellationToken ct = default) =>
        Lifecycle(accountId, request, null, ct);
    public Task<Result<NinjaLifecycleResponse>> ReturnAsync(Guid accountId, NinjaReturnRequest request, CancellationToken ct = default) =>
        Lifecycle(accountId, null, request, ct);

    private async Task<Result<NinjaLifecycleResponse>> Lifecycle(Guid accountId, NinjaAssignRequest? assign, NinjaReturnRequest? release, CancellationToken ct)
    {
        await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(db, [accountId], ct);
        var account = await Accounts.SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null) return Result.Failure<NinjaLifecycleResponse>(NinjaErrors.NotFound);
        var permission = assign is not null ? PermissionKeys.Ninja.AssignmentsCreate : PermissionKeys.Ninja.AssignmentsDelete;
        if (!await Allowed(permission, account.ClientPlatformId, ct)
            || !await Allowed(PermissionKeys.Ninja.AccountsUpdate, account.ClientPlatformId, ct))
            return Result.Failure<NinjaLifecycleResponse>(NinjaErrors.Forbidden);
        var password = assign?.NewPassword ?? release?.NewPassword;
        if (password is not null && !await Allowed(PermissionKeys.Ninja.CredentialsRotate, account.ClientPlatformId, ct))
            return Result.Failure<NinjaLifecycleResponse>(NinjaErrors.Forbidden);
        NinjaRiderType riderType = default;
        if (assign is not null && (assign.RiderType is not ("InternalCar" or "ExternalCar" or "InternalMotorcycle" or "ExternalMotorcycle")
            || !Enum.TryParse(assign.RiderType, out riderType)))
            return Result.Failure<NinjaLifecycleResponse>(NinjaErrors.Invalid(
                "اختر نوع المندوب: InternalCar أو ExternalCar أو InternalMotorcycle أو ExternalMotorcycle."));
        PlatformAccountPaymentModel paymentModel = default;
        if (assign is not null && (assign.PaymentModel is not ("PayPerOrder" or "Salary")
            || !Enum.TryParse(assign.PaymentModel, out paymentModel)))
            return Result.Failure<NinjaLifecycleResponse>(NinjaErrors.Invalid(
                "اختر طريقة الدفع: PayPerOrder (عمولة) أو Salary (بالراتب)."));
        var effective = assign?.EffectiveFrom ?? release!.EffectiveTo;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (effective == default || effective > today || (assign is not null && effective < today && !assign.WasBackdated))
            return Result.Failure<NinjaLifecycleResponse>(NinjaErrors.Invalid("حدد تاريخاً صحيحاً؛ التكليف السابق يتطلب سبباً للتأريخ السابق."));
        var phone = assign?.PhoneNumber ?? release?.PhoneNumber;
        var plan = await Prepare(account, phone, password, release is not null, assign?.Reason ?? release?.Reason ?? "Ninja assignment", ct);
        if (plan.IsFailure) return Result.Failure<NinjaLifecycleResponse>(plan.Error);
        var p = plan.Value!;
        Result<SimplePlatformAssignmentResponse> result;
        try
        {
            result = assign is not null
                ? await platforms.AssignNinjaAccountAsync(accountId, new(assign.ActualRiderProfileId, assign.EffectiveFrom,
                    assign.Reason, assign.WasBackdated, assign.BackdatedReason), riderType, paymentModel, () => Stage(account, p), ct)
                : await platforms.ReleaseNinjaAccountAsync(accountId, new(release!.EffectiveTo, release.Status,
                    release.Reason, release.RowVersion, release.DayUsage), () => Stage(account, p), ct);
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<NinjaLifecycleResponse>(NinjaErrors.Conflict); }
        if (result.IsFailure) { db.ChangeTracker.Clear(); return Result.Failure<NinjaLifecycleResponse>(result.Error); }
        return Result.Success(new NinjaLifecycleResponse(result.Value!, Response(account, p)));
    }

    public async Task<Result<NinjaCredentialResponse>> UpdateCredentialsAsync(Guid accountId, NinjaCredentialRequest request, CancellationToken ct = default)
    {
        var account = await Accounts.SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null) return Result.Failure<NinjaCredentialResponse>(NinjaErrors.NotFound);
        if (!await Allowed(PermissionKeys.Ninja.AccountsUpdate, account.ClientPlatformId, ct)
            || request.NewPassword is not null && !await Allowed(PermissionKeys.Ninja.CredentialsRotate, account.ClientPlatformId, ct))
            return Result.Failure<NinjaCredentialResponse>(NinjaErrors.Forbidden);
        if (!HrServiceSupport.MatchesRowVersion(account.RowVersion, request.RowVersion))
            return Result.Failure<NinjaCredentialResponse>(NinjaErrors.Conflict);
        if (request.NewPassword is null && request.PhoneNumber is null)
            return Result.Failure<NinjaCredentialResponse>(NinjaErrors.Invalid("أدخل كلمة المرور أو رقم الهاتف."));
        var plan = await Prepare(account, request.PhoneNumber, request.NewPassword, false, request.Reason, ct, false);
        if (plan.IsFailure) return Result.Failure<NinjaCredentialResponse>(plan.Error);
        Stage(account, plan.Value!);
        var saved = await Persist(true, ct);
        return saved.IsSuccess ? Result.Success(Response(account, plan.Value!)) : Result.Failure<NinjaCredentialResponse>(saved.Error);
    }

    private sealed record CredentialPlan(NinjaAccountDetails Details, bool NewDetails, string? Phone,
        PlatformAccountCredentialVersion? Latest, PlatformAccountCredentialVersion? NewVersion,
        NinjaPasswordNotification? Notification, Notification? SavedNotification);

    private async Task<Result<CredentialPlan>> Prepare(PlatformRiderAccount account, string? phone, string? password,
        bool returning, string reason, CancellationToken ct, bool requirePhone = true)
    {
        phone = phone?.Trim();
        if ((requirePhone || phone is not null) && (string.IsNullOrWhiteSpace(phone) || phone.Length > 32
            || phone.Count(char.IsDigit) < 7 || phone.Any(c => !char.IsDigit(c) && c is not ('+' or ' ' or '-' or '(' or ')')))
            || password is not null && (string.IsNullOrWhiteSpace(password) || password.Length > 4096)
            || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            return Result.Failure<CredentialPlan>(NinjaErrors.Invalid("أدخل رقم هاتف صالحاً وسبباً صحيحاً وكلمة مرور صالحة عند تغييرها."));
        var details = await db.NinjaAccountDetails.SingleOrDefaultAsync(d => d.PlatformRiderAccountId == account.Id, ct);
        var latest = await db.PlatformAccountCredentialVersions.AsNoTracking().Where(v => v.PlatformRiderAccountId == account.Id)
            .OrderByDescending(v => v.KeyVersion).FirstOrDefaultAsync(ct);
        var now = clock.GetUtcNow(); var allowedAt = latest?.RotatedAtUtc.AddHours(72);
        var locked = allowedAt > now;
        var same = password is not null && latest is not null && protector.Unprotect(new(latest.Ciphertext, latest.Nonce, latest.AuthenticationTag)) == password;
        if (returning && !locked && (password is null || same))
            return Result.Failure<CredentialPlan>(NinjaErrors.Invalid("عند إعادة الحساب بعد انتهاء المهلة، أدخل كلمة مرور جديدة مختلفة."));
        PlatformAccountCredentialVersion? version = null;
        if (!locked && password is not null && !same)
        {
            var protectedValue = protector.Protect(password);
            version = new() { PlatformRiderAccountId = account.Id, Ciphertext = protectedValue.Ciphertext,
                Nonce = protectedValue.Nonce, AuthenticationTag = protectedValue.AuthenticationTag,
                KeyVersion = (latest?.KeyVersion ?? 0) + 1, RotatedAtUtc = now, RotatedByUserId = user.UserId!.Value,
                RotationReason = reason.Trim(), SupersededVersionId = latest?.Id };
        }
        NinjaPasswordNotification? notice = locked ? new("ninja.password_cooldown",
            $"لم تتغير كلمة المرور؛ يمكن تغييرها بعد {allowedAt:O} (UTC).",
            $"Password retained. It can be changed after {allowedAt:O} (UTC).", allowedAt!.Value) : null;
        var notification = notice is null ? null : new Notification { RecipientUserId = user.UserId!.Value,
            EventType = notice.Code, Severity = NotificationSeverity.Warning, TitleAr = "مهلة تغيير كلمة مرور نينجا",
            TitleEn = "Ninja password change cooldown", BodyAr = notice.MessageAr, BodyEn = notice.MessageEn,
            SourceEntityType = nameof(PlatformRiderAccount), SourceEntityId = account.Id,
            ScopeSnapshotJson = JsonSerializer.Serialize(new { ClientPlatformId = account.ClientPlatformId }),
            DeduplicationKey = $"ninja-password:{account.Id}:{Guid.NewGuid():N}", VisibleAtUtc = now, ExpiresAtUtc = allowedAt };
        return Result.Success(new CredentialPlan(details ?? new() { PlatformRiderAccountId = account.Id }, details is null,
            phone ?? details?.PhoneNumber, latest, version, notice, notification));
    }

    private void Stage(PlatformRiderAccount account, CredentialPlan p)
    {
        if (p.NewDetails) db.NinjaAccountDetails.Add(p.Details);
        p.Details.PhoneNumber = p.Phone;
        if (p.NewVersion is not null) db.PlatformAccountCredentialVersions.Add(p.NewVersion);
        if (p.SavedNotification is not null) db.Notifications.Add(p.SavedNotification);
        // All Ninja credential writes contend on the account rowversion, including phone-only changes.
        account.UpdatedAtUtc = clock.GetUtcNow();
        db.Entry(account).Property(a => a.UpdatedAtUtc).IsModified = true;
    }
    private static NinjaCredentialResponse Response(PlatformRiderAccount a, CredentialPlan p) => new(a.Id,
        p.Phone, p.NewVersion is not null || p.Latest is not null, p.NewVersion is not null,
        (p.NewVersion ?? p.Latest)?.RotatedAtUtc.AddHours(72), p.Notification, Convert.ToBase64String(a.RowVersion));
}
