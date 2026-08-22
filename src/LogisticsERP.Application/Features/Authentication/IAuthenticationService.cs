using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Authentication;

public interface IAuthenticationService
{
    Task<Result<AuthenticationTokenResponse>> LoginAsync(
        LoginRequest request,
        AuthenticationClientContext clientContext,
        CancellationToken cancellationToken = default);

    Task<Result<AuthenticationTokenResponse>> RefreshAsync(
        RefreshTokenRequest request,
        AuthenticationClientContext clientContext,
        CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(CancellationToken cancellationToken = default);
    Task<Result> LogoutAllAsync(CancellationToken cancellationToken = default);

    Task<Result<AuthenticationTokenResponse>> ChangePasswordAsync(
        ChangePasswordRequest request,
        AuthenticationClientContext clientContext,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<UserSessionResponse>>> GetSessionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
