using System.Security.Claims;
using LogisticsERP.Application.Abstractions.Authentication;

namespace LogisticsERP.Api.Authentication;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthenticationClaimNames.Subject)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(subject, out var userId) ? userId : null;
        }
    }

    public Guid? SessionId
    {
        get
        {
            var sessionId = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthenticationClaimNames.SessionId);
            return Guid.TryParse(sessionId, out var id) ? id : null;
        }
    }

    public string? CorrelationId => httpContextAccessor.HttpContext?.TraceIdentifier;
}
