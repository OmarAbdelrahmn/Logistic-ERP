using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.UserProfiles;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace LogisticsERP.Infrastructure.Authentication;

internal sealed class UserProfileService(
    IdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IHostEnvironment hostEnvironment) : IUserProfileService
{
    private const long MaximumProfileImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedLocales = new(StringComparer.OrdinalIgnoreCase) { "ar", "en" };
    private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase) { "light", "dark", "system" };
    private static readonly HashSet<string> AllowedDensities = new(StringComparer.OrdinalIgnoreCase) { "compact", "comfortable" };
    private static readonly Dictionary<string, string> ProfileImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };
    private readonly string profileImageDirectory = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "profile-images"));

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

    public async Task<Result<UserProfileResponse>> UpdateProfileImageAsync(
        UserProfileImageUpload image,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.CurrentUserUnavailable);
        }

        var user = await identityDbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.CurrentUserUnavailable);
        }

        var storedImage = await StoreProfileImageAsync(userId, image, cancellationToken);
        if (storedImage is null)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.InvalidProfileImage);
        }

        var previousImageUrl = user.ProfileImageUrl;
        user.ProfileImageUrl = storedImage.Url;
        try
        {
            await identityDbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            DeleteFileBestEffort(storedImage.FullPath);
            throw;
        }

        DeletePreviousProfileImageBestEffort(previousImageUrl);
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
                    item.IqamaNo,
                    item.FullNameAr,
                    item.FullNameEn,
                    item.PrimaryPhone,
                    item.Nationality,
                    item.HireDate,
                    item.Status,
                    item.EngagementType,
                    item.IsEmployee,
                    applicationDbContext.RiderProfiles
                        .Where(rider => rider.EmployeeId == item.Id)
                        .Select(rider => (Guid?)rider.Id)
                        .SingleOrDefault(),
                    item.WorkingForMeAs,
                    item.OperationalWorkTypeId,
                    item.OperatingCityId))
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
            user.ProfileImageUrl,
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
                    employee.IqamaNo,
                    employee.FullNameAr,
                    employee.FullNameEn,
                    employee.PrimaryPhone,
                    employee.Nationality,
                    employee.HireDate,
                    employee.Status.ToString(),
                    employee.EngagementType.ToString(),
                    employee.IsEmployee,
                    employee.RiderProfileId,
                    employee.WorkingForMeAs,
                    employee.OperationalWorkTypeId,
                    employee.OperatingCityId));
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

    private async Task<StoredProfileImage?> StoreProfileImageAsync(
        Guid userId,
        UserProfileImageUpload image,
        CancellationToken cancellationToken)
    {
        if (image.Length is <= 0 or > MaximumProfileImageBytes
            || image.Content is null
            || string.IsNullOrWhiteSpace(image.ContentType)
            || !ProfileImageExtensions.TryGetValue(image.ContentType, out var extension))
        {
            return null;
        }

        Directory.CreateDirectory(profileImageDirectory);
        var fileName = $"{userId:N}-{Guid.CreateVersion7():N}{extension}";
        var fullPath = Path.Combine(profileImageDirectory, fileName);
        var completed = false;
        try
        {
            await using var destination = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[64 * 1024];
            var header = new byte[12];
            var headerLength = 0;
            long total = 0;
            while (true)
            {
                var read = await image.Content.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                if (headerLength < header.Length)
                {
                    var count = Math.Min(read, header.Length - headerLength);
                    buffer.AsSpan(0, count).CopyTo(header.AsSpan(headerLength));
                    headerLength += count;
                }

                total += read;
                if (total > MaximumProfileImageBytes)
                {
                    return null;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (total != image.Length || !HeaderMatches(image.ContentType, header.AsSpan(0, headerLength)))
            {
                return null;
            }

            await destination.FlushAsync(cancellationToken);
            completed = true;
            return new StoredProfileImage(fullPath, $"/profile-images/{fileName}");
        }
        finally
        {
            if (!completed)
            {
                DeleteFileBestEffort(fullPath);
            }
        }
    }

    private void DeletePreviousProfileImageBestEffort(string? profileImageUrl)
    {
        const string pathPrefix = "/profile-images/";
        if (string.IsNullOrWhiteSpace(profileImageUrl)
            || !profileImageUrl.StartsWith(pathPrefix, StringComparison.Ordinal)
            || profileImageUrl[pathPrefix.Length..].Contains('/')
            || profileImageUrl[pathPrefix.Length..].Contains('\\'))
        {
            return;
        }

        var fileName = profileImageUrl[pathPrefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(profileImageDirectory, fileName));
        if (fullPath.StartsWith(profileImageDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            DeleteFileBestEffort(fullPath);
        }
    }

    private static bool HeaderMatches(string contentType, ReadOnlySpan<byte> header) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
        "image/png" => header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
        _ => false
    };

    private static void DeleteFileBestEffort(string fullPath)
    {
        try { File.Delete(fullPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static AuthorizationScopeResponse ToScopeResponse(ScopeRow scope) =>
        new(scope.ScopeType.ToString(), scope.TargetId);

    private sealed record EmployeeSnapshot(
        Guid Id,
        string? IqamaNo,
        string FullNameAr,
        string? FullNameEn,
        string? PrimaryPhone,
        string? Nationality,
        DateOnly? HireDate,
        EmployeeStatus Status,
        EmployeeRelationshipType EngagementType,
        bool IsEmployee,
        Guid? RiderProfileId,
        string? WorkingForMeAs,
        Guid? OperationalWorkTypeId,
        Guid? OperatingCityId);

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

    private sealed record StoredProfileImage(string FullPath, string Url);
}
