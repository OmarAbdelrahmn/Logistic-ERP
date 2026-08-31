namespace LogisticsERP.Application.Features.UserProfiles;

public sealed record UpdateUserPreferencesRequest(
    string? PreferredLocale,
    string? PreferredTheme,
    string? PreferredDensity);

public sealed record UserProfileImageUpload(
    Stream Content,
    string ContentType,
    long Length);

public sealed record EmployeeUserProfileResponse(
    Guid Id,
    string? IqamaNo,
    string FullNameAr,
    string? FullNameEn,
    string? PrimaryPhone,
    string? Nationality,
    DateOnly? HireDate,
    string Status,
    string EngagementType,
    bool IsEmployee,
    Guid? RiderProfileId,
    string? WorkingForMeAs,
    Guid? OperationalWorkTypeId,
    Guid? OperatingCityId);

public sealed record UserProfileResponse(
    Guid Id,
    Guid? EmployeeId,
    string UserName,
    string? Email,
    string? PhoneNumber,
    string DisplayNameAr,
    string DisplayNameEn,
    string? ProfileImageUrl,
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
