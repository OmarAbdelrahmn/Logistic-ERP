using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.SupportAccess;

public sealed record SupportAccessScopeRequest(string Type, Guid TargetId);

public sealed record RequestSupportAccessRequest(
    Guid? PlatformOperatorUserId,
    IReadOnlyCollection<string> PermissionKeys,
    IReadOnlyCollection<SupportAccessScopeRequest> Scopes,
    string Reason,
    DateTimeOffset RequestedStartAtUtc,
    DateTimeOffset RequestedEndAtUtc,
    bool IsBreakGlass,
    string? BreakGlassJustification);

public sealed record ResolveSupportAccessRequest(bool Approve, string Reason, string RowVersion);
public sealed record RevokeSupportAccessRequest(string Reason, string RowVersion);

public sealed record SupportAccessResponse(
    Guid Id,
    Guid PlatformOperatorUserId,
    IReadOnlyList<string> PermissionKeys,
    IReadOnlyList<SupportAccessScopeRequest> Scopes,
    string Reason,
    string Status,
    DateTimeOffset RequestedStartAtUtc,
    DateTimeOffset RequestedEndAtUtc,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAtUtc,
    bool IsBreakGlass,
    string? BreakGlassJustification,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserId,
    string RowVersion);

public interface ISupportAccessService
{
    Task<Result<IReadOnlyList<SupportAccessResponse>>> GetAsync(Guid? operatorUserId, string? status, CancellationToken cancellationToken = default);
    Task<Result<SupportAccessResponse>> RequestAsync(RequestSupportAccessRequest request, CancellationToken cancellationToken = default);
    Task<Result<SupportAccessResponse>> ResolveAsync(Guid id, ResolveSupportAccessRequest request, CancellationToken cancellationToken = default);
    Task<Result<SupportAccessResponse>> RevokeAsync(Guid id, RevokeSupportAccessRequest request, CancellationToken cancellationToken = default);
}

