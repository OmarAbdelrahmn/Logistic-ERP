using System.Security.Cryptography;
using System.Text;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Authentication;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Authentication;

internal sealed class AuthenticationService(
    IdentityDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    IAccessTokenFactory accessTokenFactory,
    IAuthenticationSessionValidator sessionValidator,
    AuthenticationOptions options,
    TimeProvider timeProvider) : IAuthenticationService
{
    private const string RotatedReason = "Refresh token rotated.";
    private const string ReuseReason = "Refresh token reuse detected.";

    public async Task<Result<AuthenticationTokenResponse>> LoginAsync(
        LoginRequest request,
        AuthenticationClientContext clientContext,
        CancellationToken cancellationToken = default)
    {
        var login = request.Login?.Trim();
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidRequest);
        }

        var user = await FindUserAsync(login, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow();
        if (await userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.AccountLocked);
        }

        if (!CanAuthenticate(user.Status))
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.AccountUnavailable);
        }

        if (user.IsDevelopmentOnly && !options.DevelopmentAccountsEnabled)
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.AccountUnavailable);
        }

        if (!user.EmailConfirmed)
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.AccountNotConfirmed);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidCredentials);
        }

        if (user.RequiresPasswordChange)
        {
            var credentialHash = UserManagementService.HashTemporarySecret(request.Password);
            var temporaryCredential = await dbContext.TemporaryCredentials
                .IgnoreQueryFilters()
                .Where(item => item.UserId == user.Id && item.CredentialHash == credentialHash)
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (temporaryCredential is not null)
            {
                if (temporaryCredential.IsDeleted
                    || temporaryCredential.ConsumedAtUtc is not null
                    || temporaryCredential.RevokedAtUtc is not null
                    || temporaryCredential.ExpiresAtUtc <= now)
                {
                    return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidCredentials);
                }
                temporaryCredential.ConsumedAtUtc = now;
            }
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAtUtc = now;
        user.LastActivityAtUtc = now;

        await RevokeExcessSessionsAsync(user.Id, now, cancellationToken);

        var refreshToken = CreateRefreshToken();
        var session = CreateSession(
            user,
            refreshToken,
            Guid.CreateVersion7(),
            now,
            now.AddDays(options.RefreshTokenAbsoluteDays),
            request.DeviceLabel,
            clientContext);
        dbContext.UserSessions.Add(session);

        var roles = await GetActiveRoleCodesAsync(user.Id, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CreateTokenResponse(user, session, refreshToken, roles, now));
    }

    public async Task<Result<AuthenticationTokenResponse>> RefreshAsync(
        RefreshTokenRequest request,
        AuthenticationClientContext clientContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidRefreshToken);
        }

        var refreshTokenHash = Hash(request.RefreshToken);
        var previousSession = await dbContext.UserSessions
            .SingleOrDefaultAsync(session => session.RefreshTokenHash == refreshTokenHash, cancellationToken);
        if (previousSession is null)
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidRefreshToken);
        }

        var now = timeProvider.GetUtcNow();
        if (previousSession.RevokedAtUtc is not null)
        {
            await RevokeTokenFamilyAsync(previousSession, now, ReuseReason, cancellationToken);
            sessionValidator.InvalidateUser(previousSession.UserId);
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidRefreshToken);
        }

        if (previousSession.IdleExpiresAtUtc <= now || previousSession.AbsoluteExpiresAtUtc <= now)
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidRefreshToken);
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == previousSession.UserId,
            cancellationToken);
        if (user is null
            || !user.EmailConfirmed
            || !CanAuthenticate(user.Status)
            || user.AuthorizationVersion != previousSession.AuthorizationVersion)
        {
            await RevokeTokenFamilyAsync(previousSession, now, "Account authorization changed.", cancellationToken);
            sessionValidator.InvalidateUser(previousSession.UserId);
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidRefreshToken);
        }

        previousSession.RevokedAtUtc = now;
        previousSession.RevocationReason = RotatedReason;
        previousSession.LastUsedAtUtc = now;

        var refreshToken = CreateRefreshToken();
        var newSession = CreateSession(
            user,
            refreshToken,
            previousSession.RefreshTokenFamilyId,
            now,
            previousSession.AbsoluteExpiresAtUtc,
            previousSession.DeviceLabel,
            clientContext);
        dbContext.UserSessions.Add(newSession);
        user.LastActivityAtUtc = now;

        var roles = await GetActiveRoleCodesAsync(user.Id, now, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.ConcurrentRefresh);
        }

        sessionValidator.InvalidateSession(
            previousSession.UserId,
            previousSession.Id,
            previousSession.AuthorizationVersion);
        return Result.Success(CreateTokenResponse(user, newSession, refreshToken, roles, now));
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || currentUser.SessionId is not { } sessionId)
        {
            return Result.Failure(AuthenticationErrors.CurrentUserUnavailable);
        }

        var session = await dbContext.UserSessions.SingleOrDefaultAsync(
            item => item.Id == sessionId && item.UserId == userId,
            cancellationToken);
        if (session is null)
        {
            return Result.Failure(AuthenticationErrors.SessionNotFound);
        }

        if (session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = timeProvider.GetUtcNow();
            session.RevokedByUserId = userId;
            session.RevocationReason = "User signed out.";
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        sessionValidator.InvalidateSession(userId, sessionId, session.AuthorizationVersion);
        return Result.Success();
    }

    public async Task<Result> LogoutAllAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthenticationErrors.CurrentUserUnavailable);
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AuthenticationErrors.CurrentUserUnavailable);
        }

        var now = timeProvider.GetUtcNow();
        var sessions = await dbContext.UserSessions
            .Where(item => item.UserId == userId && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        RevokeSessions(sessions, now, userId, "User signed out from all sessions.");
        user.AuthorizationVersion++;
        user.SessionsRevokedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        sessionValidator.InvalidateUser(userId);
        return Result.Success();
    }

    public async Task<Result<AuthenticationTokenResponse>> ChangePasswordAsync(
        ChangePasswordRequest request,
        AuthenticationClientContext clientContext,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || string.IsNullOrWhiteSpace(request.CurrentPassword)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidRequest);
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.CurrentUserUnavailable);
        }

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.InvalidCurrentPassword);
        }

        if (await userManager.CheckPasswordAsync(user, request.NewPassword)
            || !await IsPasswordValidAsync(user, request.NewPassword))
        {
            return Result.Failure<AuthenticationTokenResponse>(AuthenticationErrors.PasswordRejected);
        }

        var now = timeProvider.GetUtcNow();
        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.RequiresPasswordChange = false;
        user.PasswordChangedAtUtc = now;
        user.AuthorizationVersion++;
        if (user.Status == UserAccountStatus.PendingTemporaryPassword)
        {
            user.Status = UserAccountStatus.Active;
        }

        var sessions = await dbContext.UserSessions
            .Where(item => item.UserId == userId && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        RevokeSessions(sessions, now, userId, "Password changed.");

        var refreshToken = CreateRefreshToken();
        var newSession = CreateSession(
            user,
            refreshToken,
            Guid.CreateVersion7(),
            now,
            now.AddDays(options.RefreshTokenAbsoluteDays),
            sessions.FirstOrDefault(item => item.Id == currentUser.SessionId)?.DeviceLabel,
            clientContext);
        dbContext.UserSessions.Add(newSession);

        var roles = await GetActiveRoleCodesAsync(user.Id, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        sessionValidator.InvalidateUser(userId);
        return Result.Success(CreateTokenResponse(user, newSession, refreshToken, roles, now));
    }

    public async Task<Result<IReadOnlyList<UserSessionResponse>>> GetSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<IReadOnlyList<UserSessionResponse>>(AuthenticationErrors.CurrentUserUnavailable);
        }

        var currentSessionId = currentUser.SessionId;
        var sessions = await dbContext.UserSessions
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.LastUsedAtUtc)
            .Take(50)
            .Select(item => new UserSessionResponse(
                item.Id,
                item.DeviceLabel,
                item.LastIpAddress,
                item.CreatedAtUtc,
                item.LastUsedAtUtc,
                item.IdleExpiresAtUtc,
                item.AbsoluteExpiresAtUtc,
                item.RevokedAtUtc,
                item.Id == currentSessionId))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<UserSessionResponse>>(sessions);
    }

    public async Task<Result> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthenticationErrors.CurrentUserUnavailable);
        }

        var session = await dbContext.UserSessions.SingleOrDefaultAsync(
            item => item.Id == sessionId && item.UserId == userId,
            cancellationToken);
        if (session is null)
        {
            return Result.Failure(AuthenticationErrors.SessionNotFound);
        }

        if (session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = timeProvider.GetUtcNow();
            session.RevokedByUserId = userId;
            session.RevocationReason = "Session revoked by the user.";
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        sessionValidator.InvalidateSession(userId, sessionId, session.AuthorizationVersion);
        return Result.Success();
    }

    private async Task<ApplicationUser?> FindUserAsync(string login, CancellationToken cancellationToken)
    {
        var normalizedName = userManager.NormalizeName(login);
        var normalizedEmail = userManager.NormalizeEmail(login);

        return await dbContext.Users
            .OrderByDescending(user => user.NormalizedUserName == normalizedName)
            .FirstOrDefaultAsync(
                user => user.NormalizedUserName == normalizedName
                    || user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    private async Task<bool> IsPasswordValidAsync(ApplicationUser user, string password)
    {
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            if (!result.Succeeded)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<string[]> GetActiveRoleCodesAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await (
            from assignment in dbContext.UserRoleAssignments.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            where assignment.UserId == userId
                && assignment.StartsAtUtc <= now
                && (assignment.ExpiresAtUtc == null || assignment.ExpiresAtUtc > now)
                && role.Status == RoleStatus.Active
            orderby role.Code
            select role.Code)
            .Distinct()
            .ToArrayAsync(cancellationToken);

    private async Task RevokeExcessSessionsAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sessionsToRevoke = await dbContext.UserSessions
            .Where(session => session.UserId == userId
                && session.RevokedAtUtc == null
                && session.IdleExpiresAtUtc > now
                && session.AbsoluteExpiresAtUtc > now)
            .OrderByDescending(session => session.LastUsedAtUtc)
            .Skip(options.MaxActiveSessions - 1)
            .ToListAsync(cancellationToken);

        RevokeSessions(sessionsToRevoke, now, userId, "Maximum active session limit reached.");
        foreach (var session in sessionsToRevoke)
        {
            sessionValidator.InvalidateSession(session.UserId, session.Id, session.AuthorizationVersion);
        }
    }

    private async Task RevokeTokenFamilyAsync(
        UserSession sourceSession,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.UserSessions
            .Where(session => session.RefreshTokenFamilyId == sourceSession.RefreshTokenFamilyId
                && session.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        RevokeSessions(sessions, now, null, reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var session in sessions)
        {
            sessionValidator.InvalidateSession(session.UserId, session.Id, session.AuthorizationVersion);
        }
    }

    private UserSession CreateSession(
        ApplicationUser user,
        string refreshToken,
        Guid familyId,
        DateTimeOffset now,
        DateTimeOffset absoluteExpiresAtUtc,
        string? deviceLabel,
        AuthenticationClientContext clientContext)
    {
        var idleExpiresAtUtc = Earlier(
            now.AddDays(options.RefreshTokenIdleDays),
            absoluteExpiresAtUtc);

        return new UserSession
        {
            UserId = user.Id,
            RefreshTokenFamilyId = familyId,
            RefreshTokenHash = Hash(refreshToken),
            DeviceLabel = Limit(deviceLabel, 200),
            UserAgentHash = string.IsNullOrWhiteSpace(clientContext.UserAgent)
                ? null
                : Hash(Limit(clientContext.UserAgent, 2048) ?? string.Empty),
            LastIpAddress = Limit(clientContext.IpAddress, 64),
            LastUsedAtUtc = now,
            IdleExpiresAtUtc = idleExpiresAtUtc,
            AbsoluteExpiresAtUtc = absoluteExpiresAtUtc,
            AuthorizationVersion = user.AuthorizationVersion
        };
    }

    private AuthenticationTokenResponse CreateTokenResponse(
        ApplicationUser user,
        UserSession session,
        string refreshToken,
        IReadOnlyCollection<string> roles,
        DateTimeOffset now)
    {
        var accessToken = accessTokenFactory.Create(user, session.Id, roles, now);
        return new AuthenticationTokenResponse(
            accessToken.Token,
            "Bearer",
            accessToken.ExpiresAtUtc,
            refreshToken,
            session.IdleExpiresAtUtc,
            session.Id,
            new AuthenticatedUserResponse(
                user.Id,
                user.EmployeeId,
                user.UserName ?? string.Empty,
                user.Email,
                user.DisplayNameAr,
                user.DisplayNameEn,
                user.PreferredLocale,
                user.RequiresPasswordChange,
                roles.Order(StringComparer.Ordinal).ToArray()));
    }

    private static void RevokeSessions(
        IEnumerable<UserSession> sessions,
        DateTimeOffset now,
        Guid? revokedByUserId,
        string reason)
    {
        foreach (var session in sessions)
        {
            session.RevokedAtUtc = now;
            session.RevokedByUserId = revokedByUserId;
            session.RevocationReason = reason;
        }
    }

    private static bool CanAuthenticate(UserAccountStatus status) =>
        status is UserAccountStatus.Active or UserAccountStatus.PendingTemporaryPassword;

    private static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string? Limit(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static DateTimeOffset Earlier(DateTimeOffset left, DateTimeOffset right) =>
        left <= right ? left : right;
}
