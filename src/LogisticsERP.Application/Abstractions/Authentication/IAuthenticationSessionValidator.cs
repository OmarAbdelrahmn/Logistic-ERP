namespace LogisticsERP.Application.Abstractions.Authentication;

public interface IAuthenticationSessionValidator
{
    Task<bool> IsValidAsync(
        Guid userId,
        Guid sessionId,
        long authorizationVersion,
        CancellationToken cancellationToken = default);

    void InvalidateSession(Guid userId, Guid sessionId, long authorizationVersion);
    void InvalidateUser(Guid userId);
}
