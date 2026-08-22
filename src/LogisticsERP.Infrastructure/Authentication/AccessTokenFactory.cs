using System.Security.Claims;
using System.Text;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LogisticsERP.Infrastructure.Authentication;

internal sealed record AccessTokenEnvelope(
    string Token,
    DateTimeOffset ExpiresAtUtc);

internal interface IAccessTokenFactory
{
    AccessTokenEnvelope Create(
        ApplicationUser user,
        Guid sessionId,
        IReadOnlyCollection<string> roles,
        DateTimeOffset now);
}

internal sealed class AccessTokenFactory(AuthenticationOptions options) : IAccessTokenFactory
{
    private readonly SigningCredentials signingCredentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            options.SigningKey ?? throw new InvalidOperationException("The authentication signing key is required."))),
        SecurityAlgorithms.HmacSha256);

    public AccessTokenEnvelope Create(
        ApplicationUser user,
        Guid sessionId,
        IReadOnlyCollection<string> roles,
        DateTimeOffset now)
    {
        var expiresAtUtc = now.AddMinutes(options.AccessTokenMinutes);
        var claims = new List<Claim>(roles.Count + 9)
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(JwtRegisteredClaimNames.Name, user.DisplayNameAr),
            new(AuthenticationClaimNames.SessionId, sessionId.ToString()),
            new(AuthenticationClaimNames.AuthorizationVersion, user.AuthorizationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("preferred_username", user.UserName ?? string.Empty),
            new("preferred_locale", user.PreferredLocale),
            new(AuthenticationClaimNames.PasswordChangeRequired, user.RequiresPasswordChange ? "true" : "false")
        };

        claims.AddRange(roles.Select(role => new Claim(AuthenticationClaimNames.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = signingCredentials
        };

        return new AccessTokenEnvelope(new JsonWebTokenHandler().CreateToken(descriptor), expiresAtUtc);
    }
}
