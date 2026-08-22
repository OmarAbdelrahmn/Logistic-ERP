using System.Collections.Concurrent;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogisticsERP.Infrastructure.Authentication;

internal sealed class AuthenticationSessionValidator(
    IdentityDbContext dbContext,
    IMemoryCache cache,
    AuthenticationOptions options,
    TimeProvider timeProvider) : IAuthenticationSessionValidator
{
    private static readonly ConcurrentDictionary<Guid, long> UserEpochs = new();

    public async Task<bool> IsValidAsync(
        Guid userId,
        Guid sessionId,
        long authorizationVersion,
        CancellationToken cancellationToken = default)
    {
        var userEpoch = UserEpochs.GetOrAdd(userId, 0);
        var cacheKey = CreateCacheKey(userId, sessionId, authorizationVersion, userEpoch);

        if (cache.TryGetValue(cacheKey, out bool isValid))
        {
            return isValid;
        }

        var now = timeProvider.GetUtcNow();
        isValid = await (
            from session in dbContext.UserSessions.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on session.UserId equals user.Id
            where session.Id == sessionId
                && session.UserId == userId
                && session.AuthorizationVersion == authorizationVersion
                && user.AuthorizationVersion == authorizationVersion
                && session.RevokedAtUtc == null
                && session.IdleExpiresAtUtc > now
                && session.AbsoluteExpiresAtUtc > now
                && user.EmailConfirmed
                && (!user.IsDevelopmentOnly || options.DevelopmentAccountsEnabled)
                && (user.Status == UserAccountStatus.Active
                    || user.Status == UserAccountStatus.PendingTemporaryPassword)
            select session.Id)
            .AnyAsync(cancellationToken);

        cache.Set(
            cacheKey,
            isValid,
            TimeSpan.FromSeconds(options.SessionValidationCacheSeconds));

        return isValid;
    }

    public void InvalidateSession(Guid userId, Guid sessionId, long authorizationVersion)
    {
        var userEpoch = UserEpochs.GetOrAdd(userId, 0);
        cache.Remove(CreateCacheKey(userId, sessionId, authorizationVersion, userEpoch));
    }

    public void InvalidateUser(Guid userId) =>
        UserEpochs.AddOrUpdate(userId, 1, static (_, epoch) => epoch + 1);

    private static string CreateCacheKey(
        Guid userId,
        Guid sessionId,
        long authorizationVersion,
        long userEpoch) =>
        $"auth-session:{userId:N}:{sessionId:N}:{authorizationVersion}:{userEpoch}";
}
