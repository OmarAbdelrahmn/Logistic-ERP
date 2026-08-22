using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.UserProfiles;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Authentication;

internal sealed class UserProfileService(
    IdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IUserProfileService
{
    private static readonly HashSet<string> AllowedLocales = new(StringComparer.OrdinalIgnoreCase) { "ar", "en" };
    private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase) { "light", "dark", "system" };
    private static readonly HashSet<string> AllowedDensities = new(StringComparer.OrdinalIgnoreCase) { "compact", "comfortable" };

    public async Task<Result<UserProfileResponse>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.CurrentUserUnavailable);
        }

        var user = await identityDbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.CurrentUserUnavailable);
        }

        return Result.Success(await CreateResponseAsync(user, cancellationToken));
    }

    public async Task<Result<UserProfileResponse>> UpdatePreferencesAsync(
        UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || !HasValidPreferences(request))
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.InvalidPreferences);
        }

        var user = await identityDbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.CurrentUserUnavailable);
        }

        if (request.PreferredLocale is not null)
        {
            user.PreferredLocale = request.PreferredLocale.Trim().ToLowerInvariant();
        }

        if (request.PreferredTheme is not null)
        {
            user.PreferredTheme = request.PreferredTheme.Trim().ToLowerInvariant();
        }

        if (request.PreferredDensity is not null)
        {
            user.PreferredDensity = request.PreferredDensity.Trim().ToLowerInvariant();
        }

        await identityDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await CreateResponseAsync(user, cancellationToken));
    }

    public async Task<Result<UserAuthorizationResponse>> GetAuthorizationAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<UserAuthorizationResponse>(UserProfileErrors.CurrentUserUnavailable);
        }

        var authorizationVersion = await identityDbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (long?)user.AuthorizationVersion)
            .SingleOrDefaultAsync(cancellationToken);
        if (authorizationVersion is null)
        {
            return Result.Failure<UserAuthorizationResponse>(UserProfileErrors.CurrentUserUnavailable);
        }

        var now = timeProvider.GetUtcNow();
        var roleRows = await (
            from assignment in identityDbContext.UserRoleAssignments.AsNoTracking()
            join role in identityDbContext.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            join permission in identityDbContext.RolePermissionGrants.AsNoTracking()
                on role.Id equals permission.RoleId into permissionGroup
            from permission in permissionGroup.DefaultIfEmpty()
            where assignment.UserId == userId
                && assignment.StartsAtUtc <= now
                && (assignment.ExpiresAtUtc == null || assignment.ExpiresAtUtc > now)
                && role.Status == RoleStatus.Active
            select new RoleGrantRow(
                assignment.Id,
                role.Id,
                role.Code,
                role.NameAr,
                role.NameEn,
                assignment.StartsAtUtc,
                assignment.ExpiresAtUtc,
                assignment.IsAllHousingScope,
                assignment.IsAllClientScope,
                assignment.IncludesFuturePlatformContracts,
                permission == null ? null : permission.PermissionKey))
            .ToListAsync(cancellationToken);

        var directRows = await identityDbContext.UserDirectPermissionAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId
                && assignment.StartsAtUtc <= now
                && (assignment.ExpiresAtUtc == null || assignment.ExpiresAtUtc > now))
            .Select(assignment => new DirectPermissionRow(
                assignment.Id,
                assignment.PermissionKey,
                assignment.Effect,
                assignment.StartsAtUtc,
                assignment.ExpiresAtUtc,
                assignment.IsAllHousingScope,
                assignment.IsAllClientScope,
                assignment.IncludesFuturePlatformContracts))
            .ToListAsync(cancellationToken);

        var roleAssignmentIds = roleRows.Select(row => row.AssignmentId).Distinct().ToArray();
        var directAssignmentIds = directRows.Select(row => row.AssignmentId).ToArray();
        var scopeRows = roleAssignmentIds.Length == 0 && directAssignmentIds.Length == 0
            ? []
            : await identityDbContext.AccessScopes
                .AsNoTracking()
                .Where(scope =>
                    scope.UserRoleAssignmentId.HasValue
                        && roleAssignmentIds.Contains(scope.UserRoleAssignmentId.Value)
                    || scope.DirectPermissionAssignmentId.HasValue
                        && directAssignmentIds.Contains(scope.DirectPermissionAssignmentId.Value))
                .Select(scope => new ScopeRow(
                    scope.UserRoleAssignmentId,
                    scope.DirectPermissionAssignmentId,
                    scope.ScopeType,
                    scope.TargetId))
                .ToArrayAsync(cancellationToken);

        var roleScopes = scopeRows
            .Where(scope => scope.UserRoleAssignmentId.HasValue)
            .ToLookup(scope => scope.UserRoleAssignmentId!.Value);
        var directScopes = scopeRows
            .Where(scope => scope.DirectPermissionAssignmentId.HasValue)
            .ToLookup(scope => scope.DirectPermissionAssignmentId!.Value);

        var roles = roleRows
            .GroupBy(row => row.AssignmentId)
            .Select(group =>
            {
                var first = group.First();
                return new UserRoleAuthorizationResponse(
                    first.AssignmentId,
                    first.RoleId,
                    first.Code,
                    first.NameAr,
                    first.NameEn,
                    first.StartsAtUtc,
                    first.ExpiresAtUtc,
                    first.IsAllHousingScope,
                    first.IsAllClientScope,
                    first.IncludesFuturePlatformContracts,
                    group.Where(row => row.PermissionKey is not null)
                        .Select(row => row.PermissionKey!)
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)
                        .ToArray(),
                    roleScopes[first.AssignmentId]
                        .Select(ToScopeResponse)
                        .OrderBy(scope => scope.Type, StringComparer.Ordinal)
                        .ThenBy(scope => scope.TargetId)
                        .ToArray());
            })
            .OrderBy(role => role.Code, StringComparer.Ordinal)
            .ToArray();

        var directPermissions = directRows
            .OrderBy(row => row.PermissionKey, StringComparer.Ordinal)
            .Select(row => new DirectPermissionAuthorizationResponse(
                row.AssignmentId,
                row.PermissionKey,
                row.Effect.ToString(),
                row.StartsAtUtc,
                row.ExpiresAtUtc,
                row.IsAllHousingScope,
                row.IsAllClientScope,
                row.IncludesFuturePlatformContracts,
                directScopes[row.AssignmentId]
                    .Select(ToScopeResponse)
                    .OrderBy(scope => scope.Type, StringComparer.Ordinal)
                    .ThenBy(scope => scope.TargetId)
                    .ToArray()))
            .ToArray();

        var deniedKeys = directRows
            .Where(row => row.Effect == PermissionEffect.Deny)
            .Select(row => row.PermissionKey)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var deniedSet = deniedKeys.ToHashSet(StringComparer.Ordinal);
        var effectiveKeys = roles
            .SelectMany(role => role.Permissions)
            .Concat(directRows
                .Where(row => row.Effect == PermissionEffect.Grant)
                .Select(row => row.PermissionKey))
            .Where(key => !deniedSet.Contains(key))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return Result.Success(new UserAuthorizationResponse(
            authorizationVersion.Value,
            roles,
            directPermissions,
            effectiveKeys,
            deniedKeys));
    }

    private async Task<UserProfileResponse> CreateResponseAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        EmployeeSnapshot? employee = null;
        if (user.EmployeeId is { } employeeId)
        {
            employee = await applicationDbContext.Employees
                .AsNoTracking()
                .Where(item => item.Id == employeeId)
                .Select(item => new EmployeeSnapshot(
                    item.Id,
                    item.EmployeeNumber,
                    item.FullNameAr,
                    item.FullNameEn,
                    item.PrimaryPhone,
                    item.NationalityCountryCode,
                    item.HireDate,
                    item.CurrentStatus,
                    item.CurrentRelationshipType,
                    applicationDbContext.RiderProfiles
                        .Where(rider => rider.EmployeeId == item.Id)
                        .Select(rider => (Guid?)rider.Id)
                        .SingleOrDefault(),
                    applicationDbContext.RiderProfiles
                        .Where(rider => rider.EmployeeId == item.Id)
                        .Select(rider => (RiderStatus?)rider.Status)
                        .SingleOrDefault(),
                    (from assignment in applicationDbContext.EmployeeJobTitlePeriods
                     join jobTitle in applicationDbContext.JobTitles on assignment.JobTitleId equals jobTitle.Id
                     join workType in applicationDbContext.OperationalWorkTypes on assignment.OperationalWorkTypeId equals workType.Id
                     join operatingCity in applicationDbContext.OperatingCities on assignment.OperatingCityId equals operatingCity.Id
                     join globalCity in applicationDbContext.GlobalCities on operatingCity.GlobalCityId equals globalCity.Id
                     where assignment.EmployeeId == item.Id && assignment.EffectiveTo == null
                     select new OperationalAssignmentSnapshot(
                         jobTitle.Id,
                         jobTitle.Code,
                         jobTitle.NameAr,
                         jobTitle.NameEn,
                         workType.Id,
                         workType.Code,
                         workType.NameAr,
                         workType.NameEn,
                         operatingCity.Id,
                         globalCity.Code,
                         globalCity.NameAr,
                         globalCity.NameEn,
                         assignment.EffectiveFrom))
                    .SingleOrDefault()))
                .SingleOrDefaultAsync(cancellationToken);
        }

        return new UserProfileResponse(
            user.Id,
            user.EmployeeId,
            user.UserName ?? string.Empty,
            user.Email,
            user.PhoneNumber,
            user.DisplayNameAr,
            user.DisplayNameEn,
            user.Status.ToString(),
            user.PreferredLocale,
            user.PreferredTheme,
            user.PreferredDensity,
            user.RequiresPasswordChange,
            user.LastLoginAtUtc,
            user.LastActivityAtUtc,
            employee is null
                ? null
                : new EmployeeUserProfileResponse(
                    employee.Id,
                    employee.EmployeeNumber,
                    employee.FullNameAr,
                    employee.FullNameEn,
                    employee.PrimaryPhone,
                    employee.NationalityCountryCode,
                    employee.HireDate,
                    employee.Status.ToString(),
                    employee.RelationshipType?.ToString(),
                    employee.RiderProfileId,
                    employee.RiderStatus?.ToString(),
                    employee.CurrentAssignment is null
                        ? null
                        : new CurrentOperationalAssignmentResponse(
                            employee.CurrentAssignment.JobTitleId,
                            employee.CurrentAssignment.JobTitleCode,
                            employee.CurrentAssignment.JobTitleNameAr,
                            employee.CurrentAssignment.JobTitleNameEn,
                            employee.CurrentAssignment.OperationalWorkTypeId,
                            employee.CurrentAssignment.OperationalWorkTypeCode,
                            employee.CurrentAssignment.OperationalWorkTypeNameAr,
                            employee.CurrentAssignment.OperationalWorkTypeNameEn,
                            employee.CurrentAssignment.OperatingCityId,
                            employee.CurrentAssignment.OperatingCityCode,
                            employee.CurrentAssignment.OperatingCityNameAr,
                            employee.CurrentAssignment.OperatingCityNameEn,
                            employee.CurrentAssignment.EffectiveFrom)));
    }

    private static bool HasValidPreferences(UpdateUserPreferencesRequest request)
    {
        var hasValue = request.PreferredLocale is not null
            || request.PreferredTheme is not null
            || request.PreferredDensity is not null;

        return hasValue
            && IsAllowed(request.PreferredLocale, AllowedLocales)
            && IsAllowed(request.PreferredTheme, AllowedThemes)
            && IsAllowed(request.PreferredDensity, AllowedDensities);
    }

    private static bool IsAllowed(string? value, HashSet<string> allowed) =>
        value is null || allowed.Contains(value.Trim());

    private static AuthorizationScopeResponse ToScopeResponse(ScopeRow scope) =>
        new(scope.ScopeType.ToString(), scope.TargetId);

    private sealed record EmployeeSnapshot(
        Guid Id,
        string EmployeeNumber,
        string FullNameAr,
        string? FullNameEn,
        string? PrimaryPhone,
        string? NationalityCountryCode,
        DateOnly? HireDate,
        EmployeeStatus Status,
        EmployeeRelationshipType? RelationshipType,
        Guid? RiderProfileId,
        RiderStatus? RiderStatus,
        OperationalAssignmentSnapshot? CurrentAssignment);

    private sealed record OperationalAssignmentSnapshot(
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

    private sealed record RoleGrantRow(
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
        string? PermissionKey);

    private sealed record DirectPermissionRow(
        Guid AssignmentId,
        string PermissionKey,
        PermissionEffect Effect,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset? ExpiresAtUtc,
        bool IsAllHousingScope,
        bool IsAllClientScope,
        bool IncludesFuturePlatformContracts);

    private sealed record ScopeRow(
        Guid? UserRoleAssignmentId,
        Guid? DirectPermissionAssignmentId,
        AccessScopeType ScopeType,
        Guid TargetId);
}
