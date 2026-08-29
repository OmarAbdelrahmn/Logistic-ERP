using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.UserManagement;

public sealed record ManagedUserResponse(
    Guid Id,
    Guid? EmployeeId,
    string UserName,
    string? Email,
    string? PhoneNumber,
    string DisplayNameAr,
    string DisplayNameEn,
    string Status,
    bool RequiresPasswordChange,
    bool IsDevelopmentOnly,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset? LastActivityAtUtc,
    DateTimeOffset CreatedAtUtc,
    string RowVersion);

public sealed record CreateManagedUserRequest(
    string UserName,
    string InitialPassword,
    string DisplayNameAr,
    string? DisplayNameEn,
    string? Email,
    string? PhoneNumber,
    Guid? EmployeeId,
    IReadOnlyList<ManagedRoleAssignmentRequest>? RoleAssignments,
    IReadOnlyList<ManagedDirectPermissionAssignmentRequest>? DirectPermissionAssignments);

public sealed record CreatedManagedUserResponse(
    ManagedUserResponse User,
    ManagedUserAuthorizationResponse Authorization);

public sealed record UpdateManagedUserRequest(
    string UserName,
    string? Email,
    string? PhoneNumber,
    string DisplayNameAr,
    string? DisplayNameEn,
    Guid? EmployeeId,
    string RowVersion);

public sealed record UpdateManagedUserStatusRequest(
    string Status,
    string? Reason,
    string RowVersion);

public sealed record ResetManagedUserPasswordRequest(string NewPassword);

public sealed record IssueTemporaryCredentialRequest(string Purpose, int ValidForMinutes);

public sealed record TemporaryCredentialResponse(
    Guid Id,
    Guid UserId,
    string Purpose,
    string Secret,
    DateTimeOffset ExpiresAtUtc);

public sealed record RevokeTemporaryCredentialRequest(string Reason);

public sealed record ArchiveManagedUserRequest(string Reason, string RowVersion);

public sealed record ManagedRoleResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    string Status,
    bool IsProtected,
    IReadOnlyList<string> PermissionKeys,
    string RowVersion);

public sealed record ManagedRoleUpsertRequest(
    string Code,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Status,
    bool IsTemplate,
    Guid? SourceTemplateId,
    string? RowVersion);

public sealed record ReplaceRolePermissionsRequest(IReadOnlyCollection<string> PermissionKeys, string RowVersion);

public sealed record PermissionCatalogItemResponse(
    string Key,
    string Module,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    bool IsSensitive,
    bool IsHighTrust,
    bool RequiresHousingScope,
    bool RequiresClientScope);

public sealed record AuthorizationScopeRequest(string Type, Guid TargetId);

public sealed record ManagedRoleAssignmentRequest(
    Guid RoleId,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? Reason,
    bool IsAllHousingScope,
    bool IsAllClientScope,
    bool IncludesFuturePlatformContracts,
    IReadOnlyList<AuthorizationScopeRequest>? Scopes);

public sealed record ReplaceManagedUserRolesRequest(IReadOnlyList<ManagedRoleAssignmentRequest>? Assignments);

public sealed record ManagedDirectPermissionAssignmentRequest(
    string PermissionKey,
    string Effect,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? Reason,
    bool IsAllHousingScope,
    bool IsAllClientScope,
    bool IncludesFuturePlatformContracts,
    IReadOnlyList<AuthorizationScopeRequest>? Scopes);

public sealed record ReplaceManagedUserPermissionsRequest(IReadOnlyList<ManagedDirectPermissionAssignmentRequest>? Assignments);

public sealed record ManagedAuthorizationScopeResponse(string Type, Guid TargetId);

public sealed record ManagedUserRoleAssignmentResponse(
    Guid AssignmentId,
    Guid RoleId,
    string RoleCode,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Reason,
    bool IsAllHousingScope,
    bool IsAllClientScope,
    bool IncludesFuturePlatformContracts,
    IReadOnlyList<ManagedAuthorizationScopeResponse> Scopes);

public sealed record ManagedUserDirectPermissionResponse(
    Guid AssignmentId,
    string PermissionKey,
    string Effect,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Reason,
    bool IsAllHousingScope,
    bool IsAllClientScope,
    bool IncludesFuturePlatformContracts,
    IReadOnlyList<ManagedAuthorizationScopeResponse> Scopes);

public sealed record ManagedUserAuthorizationResponse(
    long AuthorizationVersion,
    IReadOnlyList<ManagedUserRoleAssignmentResponse> Roles,
    IReadOnlyList<ManagedUserDirectPermissionResponse> DirectPermissions);

public interface IUserManagementService
{
    Task<Result<IReadOnlyList<ManagedUserResponse>>> GetUsersAsync(string? search, CancellationToken cancellationToken = default);
    Task<Result<ManagedUserResponse>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<CreatedManagedUserResponse>> CreateUserAsync(CreateManagedUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<ManagedUserResponse>> UpdateUserAsync(Guid userId, UpdateManagedUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<ManagedUserResponse>> UpdateStatusAsync(Guid userId, UpdateManagedUserStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(Guid userId, ResetManagedUserPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<TemporaryCredentialResponse>> IssueTemporaryCredentialAsync(Guid userId, IssueTemporaryCredentialRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeTemporaryCredentialAsync(Guid userId, Guid credentialId, RevokeTemporaryCredentialRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeSessionsAsync(Guid userId, string? reason, CancellationToken cancellationToken = default);
    Task<Result> ArchiveUserAsync(Guid userId, ArchiveManagedUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ManagedRoleResponse>>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<Result<ManagedRoleResponse>> UpsertRoleAsync(Guid? roleId, ManagedRoleUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<ManagedRoleResponse>> ReplaceRolePermissionsAsync(Guid roleId, ReplaceRolePermissionsRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveRoleAsync(Guid roleId, ArchiveManagedUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PermissionCatalogItemResponse>>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<Result<ManagedUserAuthorizationResponse>> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<ManagedUserAuthorizationResponse>> ReplaceRolesAsync(Guid userId, ReplaceManagedUserRolesRequest request, CancellationToken cancellationToken = default);
    Task<Result<ManagedUserAuthorizationResponse>> ReplacePermissionsAsync(Guid userId, ReplaceManagedUserPermissionsRequest request, CancellationToken cancellationToken = default);
}
