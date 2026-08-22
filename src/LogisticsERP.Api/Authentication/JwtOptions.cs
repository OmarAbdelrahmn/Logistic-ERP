namespace LogisticsERP.Api.Authentication;

internal sealed class JwtOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string? SigningKey { get; init; }
}
