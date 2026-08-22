namespace LogisticsERP.Application.Features.Authentication;

public sealed record LoginRequest(
    string Login,
    string Password,
    string? DeviceLabel);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record AuthenticationClientContext(
    string? IpAddress,
    string? UserAgent);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    Guid? EmployeeId,
    string UserName,
    string? Email,
    string DisplayNameAr,
    string DisplayNameEn,
    string PreferredLocale,
    bool RequiresPasswordChange,
    IReadOnlyList<string> Roles);

public sealed record AuthenticationTokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    Guid SessionId,
    AuthenticatedUserResponse User);

public sealed record UserSessionResponse(
    Guid Id,
    string? DeviceLabel,
    string? LastIpAddress,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastUsedAtUtc,
    DateTimeOffset IdleExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    bool IsCurrent);
