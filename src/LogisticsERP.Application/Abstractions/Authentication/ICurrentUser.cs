namespace LogisticsERP.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? SessionId { get; }
    long? AuthorizationVersion { get; }
    string? CorrelationId { get; }
}
