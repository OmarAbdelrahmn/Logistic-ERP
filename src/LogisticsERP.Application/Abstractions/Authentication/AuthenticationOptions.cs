namespace LogisticsERP.Application.Abstractions.Authentication;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string? SigningKey { get; set; }
    public int AccessTokenMinutes { get; set; } = 10;
    public int RefreshTokenIdleDays { get; set; } = 7;
    public int RefreshTokenAbsoluteDays { get; set; } = 30;
    public int MaxActiveSessions { get; set; } = 10;
    public int SessionValidationCacheSeconds { get; set; } = 15;
    public bool DevelopmentAccountsEnabled { get; set; }
}
