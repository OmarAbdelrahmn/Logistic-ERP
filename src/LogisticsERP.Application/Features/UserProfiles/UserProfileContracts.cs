namespace LogisticsERP.Application.Features.UserProfiles;

public sealed record UpdateUserPreferencesRequest(
    string? PreferredLocale,
    string? PreferredTheme,
    string? PreferredDensity);

public sealed record EmployeeUserProfileResponse(
    Guid Id,
    string EmployeeNumber,
    string FullNameAr,
    string? FullNameEn,
    string? PrimaryPhone,
    string? NationalityCountryCode,
    DateOnly? HireDate,
    string Status,
    string? RelationshipType,
    Guid? RiderProfileId,
    string? RiderStatus,
    CurrentOperationalAssignmentResponse? CurrentAssignment);

public sealed record CurrentOperationalAssignmentResponse(
    Guid JobTitleId,
    string JobTitleCode,
    string JobTitleNameAr,
    string JobTitleNameEn,
    Guid OperationalWorkTypeId,
    string OperationalWorkTypeCode,
    string OperationalWorkTypeNameAr,
    string OperationalWorkTypeNameEn,
    Guid OperatingCityId,
    string OperatingCityCode,
    string OperatingCityNameAr,
    string OperatingCityNameEn,
    DateOnly EffectiveFrom);

public sealed record UserProfileResponse(
    Guid Id,
    Guid? EmployeeId,
    string UserName,
    string? Email,
    string? PhoneNumber,
    string DisplayNameAr,
    string DisplayNameEn,
    string Status,
    string PreferredLocale,
    string PreferredTheme,
    string PreferredDensity,
    bool RequiresPasswordChange,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset? LastActivityAtUtc,
    EmployeeUserProfileResponse? Employee);

public sealed record AuthorizationScopeResponse(
    string Type,
    Guid TargetId);

public sealed record UserRoleAuthorizationResponse(
    Guid AssignmentId,
    Guid RoleId,
    string Code,
    string NameAr,
    string NameEn,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool IsAllHousingScope,
    bool IsAllClientScope,
    bool IncludesFuturePlatformContracts,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<AuthorizationScopeResponse> Scopes);

public sealed record DirectPermissionAuthorizationResponse(
    Guid AssignmentId,
    string PermissionKey,
    string Effect,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool IsAllHousingScope,
    bool IsAllClientScope,
    bool IncludesFuturePlatformContracts,
    IReadOnlyList<AuthorizationScopeResponse> Scopes);

public sealed record UserAuthorizationResponse(
    long AuthorizationVersion,
    IReadOnlyList<UserRoleAuthorizationResponse> Roles,
    IReadOnlyList<DirectPermissionAuthorizationResponse> DirectPermissions,
    IReadOnlyList<string> EffectivePermissionKeys,
    IReadOnlyList<string> DeniedPermissionKeys);
