using System.Security.Cryptography;
using System.Text;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.UserManagement;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Authentication;

internal sealed class UserManagementService(
    IdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    IAuthenticationSessionValidator sessionValidator,
    TimeProvider timeProvider) : IUserManagementService
{
    public async Task<Result<IReadOnlyList<ManagedUserResponse>>> GetUsersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = identityDbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalized = userManager.NormalizeName(term);
            query = query.Where(user => user.NormalizedUserName!.Contains(normalized)
                || user.DisplayNameAr.Contains(term)
                || user.DisplayNameEn.Contains(term)
                || (user.Email != null && user.Email.Contains(term)));
        }

        var users = await query
            .OrderBy(user => user.DisplayNameAr)
            .ThenBy(user => user.UserName)
            .Take(500)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ManagedUserResponse>>(users.Select(ToResponse).ToArray());
    }

    public async Task<Result<ManagedUserResponse>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        return user is null
            ? Result.Failure<ManagedUserResponse>(UserManagementErrors.NotFound)
            : Result.Success(ToResponse(user));
    }

    public async Task<Result<CreatedManagedUserResponse>> CreateUserAsync(
        CreateManagedUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorId))
        {
            return Result.Failure<CreatedManagedUserResponse>(UserManagementErrors.CurrentUserUnavailable);
        }
        if (!IsValidCreateRequest(request))
        {
            return Result.Failure<CreatedManagedUserResponse>(DescribeInvalidCreateRequest(request));
        }

        IReadOnlyList<ManagedRoleAssignmentRequest> roleAssignments = request.RoleAssignments ??
        [
            new ManagedRoleAssignmentRequest(
                SystemRoles.UserId,
                null,
                null,
                "Default minimal user access.",
                false,
                false,
                false,
                null)
        ];
        IReadOnlyList<ManagedDirectPermissionAssignmentRequest> permissionAssignments =
            request.DirectPermissionAssignments ?? [];
        if (roleAssignments.Count == 0 || !ValidateRoleAssignments(roleAssignments))
        {
            return Result.Failure<CreatedManagedUserResponse>(DescribeInvalidRoleAssignments(roleAssignments));
        }
        if (!ValidatePermissionAssignments(permissionAssignments))
        {
            return Result.Failure<CreatedManagedUserResponse>(DescribeInvalidPermissionAssignments(permissionAssignments));
        }

        if (await IsUserNameInUseAsync(request.UserName, null, cancellationToken)
            || await IsEmailInUseAsync(request.Email, null, cancellationToken)
            || !await IsEmployeeAvailableAsync(request.EmployeeId, null, cancellationToken))
        {
            return Result.Failure<CreatedManagedUserResponse>(UserManagementErrors.Duplicate);
        }

        var roleIds = roleAssignments.Select(assignment => assignment.RoleId).ToArray();
        var activeRoleCount = await identityDbContext.Roles
            .AsNoTracking()
            .CountAsync(role => roleIds.Contains(role.Id) && role.Status == RoleStatus.Active, cancellationToken);
        if (activeRoleCount != roleIds.Length)
        {
            return Result.Failure<CreatedManagedUserResponse>(UserManagementErrors.NotFound);
        }

        var parsedRoleScopes = new List<ParsedScope[]>(roleAssignments.Count);
        foreach (var assignment in roleAssignments)
        {
            var validation = await ParseAndValidateScopesAsync(
                assignment.Scopes,
                assignment.IsAllHousingScope,
                assignment.IsAllClientScope,
                cancellationToken);
            if (validation.IsFailure)
            {
                return Result.Failure<CreatedManagedUserResponse>(validation.Error);
            }
            parsedRoleScopes.Add(validation.Value!);
        }

        var parsedPermissionScopes = new List<ParsedScope[]>(permissionAssignments.Count);
        foreach (var assignment in permissionAssignments)
        {
            var validation = await ParseAndValidateScopesAsync(
                assignment.Scopes,
                assignment.IsAllHousingScope,
                assignment.IsAllClientScope,
                cancellationToken);
            if (validation.IsFailure)
            {
                return Result.Failure<CreatedManagedUserResponse>(validation.Error);
            }
            parsedPermissionScopes.Add(validation.Value!);
        }

        await using var transaction = await identityDbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = TrimOrNull(request.Email),
            EmailConfirmed = true,
            PhoneNumber = TrimOrNull(request.PhoneNumber),
            EmployeeId = request.EmployeeId,
            DisplayNameAr = request.DisplayNameAr.Trim(),
            DisplayNameEn = request.DisplayNameEn?.Trim() ?? string.Empty,
            PreferredLocale = "ar",
            PreferredTheme = "light",
            PreferredDensity = "compact",
            Status = UserAccountStatus.PendingTemporaryPassword,
            RequiresPasswordChange = true,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        var created = await userManager.CreateAsync(user, request.InitialPassword);
        if (!created.Succeeded)
        {
            return Result.Failure<CreatedManagedUserResponse>(DescribeIdentityFailure(created, "initialPassword"));
        }

        var now = timeProvider.GetUtcNow();
        for (var index = 0; index < roleAssignments.Count; index++)
        {
            var input = roleAssignments[index];
            var assignment = new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = input.RoleId,
                StartsAtUtc = input.StartsAtUtc ?? now,
                ExpiresAtUtc = input.ExpiresAtUtc,
                GrantedByUserId = actorId,
                GrantReason = TrimOrNull(input.Reason) ?? "Role assigned during user creation.",
                IsAllHousingScope = input.IsAllHousingScope,
                IsAllClientScope = input.IsAllClientScope,
                IncludesFuturePlatformContracts = input.IncludesFuturePlatformContracts
            };
            identityDbContext.UserRoleAssignments.Add(assignment);
            AddScopes(assignment.Id, null, parsedRoleScopes[index]);
        }

        for (var index = 0; index < permissionAssignments.Count; index++)
        {
            var input = permissionAssignments[index];
            var assignment = new UserDirectPermissionAssignment
            {
                UserId = user.Id,
                PermissionKey = input.PermissionKey.Trim(),
                Effect = Enum.Parse<PermissionEffect>(input.Effect, true),
                StartsAtUtc = input.StartsAtUtc ?? now,
                ExpiresAtUtc = input.ExpiresAtUtc,
                GrantedByUserId = actorId,
                GrantReason = TrimOrNull(input.Reason) ?? "Direct permission assigned during user creation.",
                IsAllHousingScope = input.IsAllHousingScope,
                IsAllClientScope = input.IsAllClientScope,
                IncludesFuturePlatformContracts = input.IncludesFuturePlatformContracts
            };
            identityDbContext.UserDirectPermissionAssignments.Add(assignment);
            AddScopes(null, assignment.Id, parsedPermissionScopes[index]);
        }

        identityDbContext.TemporaryCredentials.Add(new TemporaryCredential
        {
            UserId = user.Id,
            Purpose = CredentialPurpose.InitialActivation,
            CredentialHash = HashTemporarySecret(userManager.PasswordHasher, user, request.InitialPassword),
            ExpiresAtUtc = now.AddHours(24),
            IssuedByUserId = actorId
        });
        user.AuthorizationVersion++;
        await identityDbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CreatedManagedUserResponse(
            ToResponse(user),
            await BuildAuthorizationResponseAsync(user.Id, cancellationToken)));
    }

    public async Task<Result<ManagedUserResponse>> UpdateUserAsync(Guid userId, UpdateManagedUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _) || !IsValidUpdateRequest(request))
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.InvalidRequest);
        }

        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.NotFound);
        }
        if (user.IsDevelopmentOnly)
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.ProtectedAccount);
        }
        if (!MatchesRowVersion(user.RowVersion, request.RowVersion))
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.ConcurrencyConflict);
        }
        if (await IsUserNameInUseAsync(request.UserName, user.Id, cancellationToken)
            || await IsEmailInUseAsync(request.Email, user.Id, cancellationToken)
            || !await IsEmployeeAvailableAsync(request.EmployeeId, user.Id, cancellationToken))
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.Duplicate);
        }

        user.UserName = request.UserName.Trim();
        user.NormalizedUserName = userManager.NormalizeName(user.UserName);
        user.Email = TrimOrNull(request.Email);
        user.NormalizedEmail = user.Email is null ? null : userManager.NormalizeEmail(user.Email);
        user.PhoneNumber = TrimOrNull(request.PhoneNumber);
        user.DisplayNameAr = request.DisplayNameAr.Trim();
        user.DisplayNameEn = request.DisplayNameEn?.Trim() ?? string.Empty;
        user.EmployeeId = request.EmployeeId;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        await identityDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(user));
    }

    public async Task<Result<ManagedUserResponse>> UpdateStatusAsync(Guid userId, UpdateManagedUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorId) || !Enum.TryParse<UserAccountStatus>(request.Status, true, out var status)
            || status == UserAccountStatus.Archived)
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.InvalidRequest);
        }

        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.NotFound);
        }
        if (user.IsDevelopmentOnly)
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.ProtectedAccount);
        }
        if (user.Id == actorId)
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.SelfSecurityChange);
        }
        if (!MatchesRowVersion(user.RowVersion, request.RowVersion))
        {
            return Result.Failure<ManagedUserResponse>(UserManagementErrors.ConcurrencyConflict);
        }

        var now = timeProvider.GetUtcNow();
        user.Status = status;
        user.LockoutEnd = status == UserAccountStatus.Locked ? now.AddYears(10) : null;
        await RevokeSessionsAndIncrementAuthorizationAsync(user, actorId, request.Reason ?? $"Account status changed to {status}.", now, cancellationToken);
        return Result.Success(ToResponse(user));
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, ResetManagedUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorId) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result.Failure(UserManagementErrors.InvalidRequest);
        }

        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserManagementErrors.NotFound);
        }
        if (user.IsDevelopmentOnly)
        {
            return Result.Failure(UserManagementErrors.ProtectedAccount);
        }
        if (user.Id == actorId)
        {
            return Result.Failure(UserManagementErrors.SelfSecurityChange);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!reset.Succeeded)
        {
            return Result.Failure(DescribeIdentityFailure(reset, "newPassword"));
        }

        var now = timeProvider.GetUtcNow();
        await RevokeActiveTemporaryCredentialsAsync(user.Id, now, cancellationToken);
        identityDbContext.TemporaryCredentials.Add(new TemporaryCredential
        {
            UserId = user.Id,
            Purpose = CredentialPurpose.PasswordReset,
            CredentialHash = HashTemporarySecret(userManager.PasswordHasher, user, request.NewPassword),
            ExpiresAtUtc = now.AddHours(24),
            IssuedByUserId = actorId
        });
        user.RequiresPasswordChange = true;
        user.PasswordChangedAtUtc = now;
        user.Status = UserAccountStatus.PendingTemporaryPassword;
        await RevokeSessionsAndIncrementAuthorizationAsync(user, actorId, "Password reset by an administrator.", now, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<TemporaryCredentialResponse>> IssueTemporaryCredentialAsync(
        Guid userId,
        IssueTemporaryCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorId)
            || !Enum.TryParse<CredentialPurpose>(request.Purpose, true, out var purpose)
            || request.ValidForMinutes is < 5 or > 1440)
        {
            return Result.Failure<TemporaryCredentialResponse>(UserManagementErrors.InvalidRequest);
        }

        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<TemporaryCredentialResponse>(UserManagementErrors.NotFound);
        }
        if (user.IsDevelopmentOnly)
        {
            return Result.Failure<TemporaryCredentialResponse>(UserManagementErrors.ProtectedAccount);
        }
        if (user.Id == actorId)
        {
            return Result.Failure<TemporaryCredentialResponse>(UserManagementErrors.SelfSecurityChange);
        }

        var secret = CreateTemporarySecret();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, secret);
        if (!reset.Succeeded)
        {
            return Result.Failure<TemporaryCredentialResponse>(UserManagementErrors.PasswordRejected);
        }

        var now = timeProvider.GetUtcNow();
        await RevokeActiveTemporaryCredentialsAsync(user.Id, now, cancellationToken);
        var credential = new TemporaryCredential
        {
            UserId = user.Id,
            Purpose = purpose,
            CredentialHash = HashTemporarySecret(userManager.PasswordHasher, user, secret),
            ExpiresAtUtc = now.AddMinutes(request.ValidForMinutes),
            IssuedByUserId = actorId
        };
        identityDbContext.TemporaryCredentials.Add(credential);
        user.RequiresPasswordChange = true;
        user.PasswordChangedAtUtc = now;
        user.Status = UserAccountStatus.PendingTemporaryPassword;
        await RevokeSessionsAndIncrementAuthorizationAsync(
            user,
            actorId,
            $"Temporary {purpose} credential issued.",
            now,
            cancellationToken);

        return Result.Success(new TemporaryCredentialResponse(
            credential.Id,
            user.Id,
            purpose.ToString(),
            secret,
            credential.ExpiresAtUtc));
    }

    public async Task<Result> RevokeTemporaryCredentialAsync(
        Guid userId,
        Guid credentialId,
        RevokeTemporaryCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _)
            || string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Trim().Length > 1000)
        {
            return Result.Failure(UserManagementErrors.InvalidRequest);
        }

        var credential = await identityDbContext.TemporaryCredentials.SingleOrDefaultAsync(
            item => item.Id == credentialId && item.UserId == userId,
            cancellationToken);
        if (credential is null)
        {
            return Result.Failure(UserManagementErrors.NotFound);
        }
        if (credential.ConsumedAtUtc is not null || credential.RevokedAtUtc is not null)
        {
            return Result.Failure(UserManagementErrors.Conflict);
        }

        credential.RevokedAtUtc = timeProvider.GetUtcNow();
        credential.DeletionReason = request.Reason.Trim();
        await identityDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RevokeSessionsAsync(Guid userId, string? reason, CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorId))
        {
            return Result.Failure(UserManagementErrors.CurrentUserUnavailable);
        }
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserManagementErrors.NotFound);
        }
        if (user.IsDevelopmentOnly)
        {
            return Result.Failure(UserManagementErrors.ProtectedAccount);
        }
        if (user.Id == actorId)
        {
            return Result.Failure(UserManagementErrors.SelfSecurityChange);
        }

        await RevokeSessionsAndIncrementAuthorizationAsync(user, actorId, reason ?? "Sessions revoked by an administrator.", timeProvider.GetUtcNow(), cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ArchiveUserAsync(Guid userId, ArchiveManagedUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorId) || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result.Failure(UserManagementErrors.InvalidRequest);
        }
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserManagementErrors.NotFound);
        }
        if (user.IsDevelopmentOnly)
        {
            return Result.Failure(UserManagementErrors.ProtectedAccount);
        }
        if (user.Id == actorId)
        {
            return Result.Failure(UserManagementErrors.SelfSecurityChange);
        }
        if (!MatchesRowVersion(user.RowVersion, request.RowVersion))
        {
            return Result.Failure(UserManagementErrors.ConcurrencyConflict);
        }

        var now = timeProvider.GetUtcNow();
        user.Status = UserAccountStatus.Archived;
        await RevokeSessionsAndIncrementAuthorizationAsync(user, actorId, request.Reason.Trim(), now, cancellationToken, saveChanges: false);
        user.DeletionReason = request.Reason.Trim();
        identityDbContext.Users.Remove(user);
        await identityDbContext.SaveChangesAsync(cancellationToken);
        sessionValidator.InvalidateUser(user.Id);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<ManagedRoleResponse>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await identityDbContext.Roles.AsNoTracking()
            .OrderBy(role => role.Code)
            .Select(role => new { role.Id, role.Code, role.NameAr, role.NameEn, role.Status, role.IsProtected, role.RowVersion })
            .ToListAsync(cancellationToken);
        var grants = await identityDbContext.RolePermissionGrants.AsNoTracking()
            .Select(grant => new { grant.RoleId, grant.PermissionKey })
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ManagedRoleResponse>>(roles.Select(role => new ManagedRoleResponse(
            role.Id, role.Code, role.NameAr, role.NameEn, role.Status.ToString(), role.IsProtected,
            grants.Where(grant => grant.RoleId == role.Id).Select(grant => grant.PermissionKey).Order().ToArray(),
            Convert.ToBase64String(role.RowVersion))).ToArray());
    }

    public async Task<Result<ManagedRoleResponse>> UpsertRoleAsync(
        Guid? roleId,
        ManagedRoleUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _)
            || !HasText(request.Code, 64)
            || !HasText(request.NameAr, 200)
            || !HasText(request.NameEn, 200)
            || !Enum.TryParse<RoleStatus>(request.Status, true, out var status))
        {
            return Result.Failure<ManagedRoleResponse>(UserManagementErrors.InvalidRequest);
        }

        ApplicationRole role;
        if (roleId is null)
        {
            role = new ApplicationRole();
            identityDbContext.Roles.Add(role);
        }
        else
        {
            role = await identityDbContext.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken) ?? null!;
            if (role is null)
            {
                return Result.Failure<ManagedRoleResponse>(UserManagementErrors.NotFound);
            }
            if (role.IsProtected)
            {
                return Result.Failure<ManagedRoleResponse>(UserManagementErrors.ProtectedAccount);
            }
            if (!MatchesRowVersion(role.RowVersion, request.RowVersion))
            {
                return Result.Failure<ManagedRoleResponse>(UserManagementErrors.ConcurrencyConflict);
            }
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await identityDbContext.Roles.IgnoreQueryFilters().AnyAsync(
            item => item.Id != role.Id && item.Code == code,
            cancellationToken))
        {
            return Result.Failure<ManagedRoleResponse>(UserManagementErrors.Duplicate);
        }
        if (request.SourceTemplateId is not null
            && !await identityDbContext.Roles.AnyAsync(
                item => item.Id == request.SourceTemplateId && item.IsTemplate,
                cancellationToken))
        {
            return Result.Failure<ManagedRoleResponse>(UserManagementErrors.NotFound);
        }

        role.Code = code;
        role.Name = code;
        role.NormalizedName = code;
        role.NameAr = request.NameAr.Trim();
        role.NameEn = request.NameEn.Trim();
        role.DescriptionAr = TrimOrNull(request.DescriptionAr);
        role.DescriptionEn = TrimOrNull(request.DescriptionEn);
        role.Status = status;
        role.IsTemplate = request.IsTemplate;
        role.SourceTemplateId = request.SourceTemplateId;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        await identityDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildRoleResponseAsync(role.Id, cancellationToken));
    }

    public async Task<Result<ManagedRoleResponse>> ReplaceRolePermissionsAsync(
        Guid roleId,
        ReplaceRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _)
            || request.PermissionKeys.Any(key => !PermissionKeys.All.Contains(key))
            || request.PermissionKeys.Distinct(StringComparer.Ordinal).Count() != request.PermissionKeys.Count)
        {
            return Result.Failure<ManagedRoleResponse>(UserManagementErrors.InvalidRequest);
        }

        var role = await identityDbContext.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<ManagedRoleResponse>(UserManagementErrors.NotFound);
        }
        if (role.IsProtected)
        {
            return Result.Failure<ManagedRoleResponse>(UserManagementErrors.ProtectedAccount);
        }
        if (!MatchesRowVersion(role.RowVersion, request.RowVersion))
        {
            return Result.Failure<ManagedRoleResponse>(UserManagementErrors.ConcurrencyConflict);
        }

        var current = await identityDbContext.RolePermissionGrants
            .Where(item => item.RoleId == roleId)
            .ToListAsync(cancellationToken);
        identityDbContext.RolePermissionGrants.RemoveRange(current);
        foreach (var key in request.PermissionKeys)
        {
            identityDbContext.RolePermissionGrants.Add(new RolePermissionGrant
            {
                RoleId = roleId,
                PermissionKey = key
            });
        }
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        await identityDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildRoleResponseAsync(role.Id, cancellationToken));
    }

    public async Task<Result> ArchiveRoleAsync(
        Guid roleId,
        ArchiveManagedUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _)
            || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result.Failure(UserManagementErrors.InvalidRequest);
        }
        var role = await identityDbContext.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(UserManagementErrors.NotFound);
        }
        if (role.IsProtected)
        {
            return Result.Failure(UserManagementErrors.ProtectedAccount);
        }
        if (!MatchesRowVersion(role.RowVersion, request.RowVersion))
        {
            return Result.Failure(UserManagementErrors.ConcurrencyConflict);
        }
        if (await identityDbContext.UserRoleAssignments.AnyAsync(
            item => item.RoleId == roleId && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > timeProvider.GetUtcNow()),
            cancellationToken))
        {
            return Result.Failure(UserManagementErrors.Conflict);
        }

        role.Status = RoleStatus.Archived;
        role.DeletionReason = request.Reason.Trim();
        identityDbContext.Roles.Remove(role);
        await identityDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<PermissionCatalogItemResponse>>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await applicationDbContext.PermissionDefinitions.AsNoTracking()
            .Where(permission => !permission.IsDeprecated)
            .OrderBy(permission => permission.DisplayOrder)
            .Select(permission => new PermissionCatalogItemResponse(
                permission.Key, permission.Category, permission.NameAr, permission.NameEn,
                permission.DescriptionAr, permission.DescriptionEn, permission.IsSensitive,
                permission.IsHighTrust, permission.RequiresHousingScope, permission.RequiresClientScope))
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PermissionCatalogItemResponse>>(permissions);
    }

    public async Task<Result<ManagedUserAuthorizationResponse>> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await FindUserAsync(userId, cancellationToken) is null)
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.NotFound);
        }
        return Result.Success(await BuildAuthorizationResponseAsync(userId, cancellationToken));
    }

    public async Task<Result<ManagedUserAuthorizationResponse>> ReplaceRolesAsync(Guid userId, ReplaceManagedUserRolesRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorId))
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.CurrentUserUnavailable);
        }
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.NotFound);
        }
        if (user.IsDevelopmentOnly)
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.ProtectedAccount);
        }
        if (user.Id == actorId)
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.SelfSecurityChange);
        }

        var assignments = request.Assignments ?? [];
        if (!ValidateRoleAssignments(assignments))
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.InvalidRequest);
        }
        var roleIds = assignments.Select(assignment => assignment.RoleId).ToArray();
        var roleCount = await identityDbContext.Roles.AsNoTracking()
            .CountAsync(role => roleIds.Contains(role.Id) && role.Status == RoleStatus.Active, cancellationToken);
        if (roleCount != roleIds.Length)
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.NotFound);
        }

        var parsedScopes = new List<ParsedScope[]>();
        foreach (var assignment in assignments)
        {
            var validation = await ParseAndValidateScopesAsync(assignment.Scopes, assignment.IsAllHousingScope, assignment.IsAllClientScope, cancellationToken);
            if (validation.IsFailure)
            {
                return Result.Failure<ManagedUserAuthorizationResponse>(validation.Error);
            }
            parsedScopes.Add(validation.Value!);
        }

        var oldAssignments = await identityDbContext.UserRoleAssignments.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        await SoftDeleteScopesAsync(oldAssignments.Select(item => item.Id).ToArray(), [], cancellationToken);
        identityDbContext.UserRoleAssignments.RemoveRange(oldAssignments);

        var now = timeProvider.GetUtcNow();
        for (var index = 0; index < assignments.Count; index++)
        {
            var input = assignments[index];
            var entity = new UserRoleAssignment
            {
                UserId = userId,
                RoleId = input.RoleId,
                StartsAtUtc = input.StartsAtUtc ?? now,
                ExpiresAtUtc = input.ExpiresAtUtc,
                GrantedByUserId = actorId,
                GrantReason = TrimOrNull(input.Reason) ?? "Role assigned by an administrator.",
                IsAllHousingScope = input.IsAllHousingScope,
                IsAllClientScope = input.IsAllClientScope,
                IncludesFuturePlatformContracts = input.IncludesFuturePlatformContracts
            };
            identityDbContext.UserRoleAssignments.Add(entity);
            AddScopes(entity.Id, null, parsedScopes[index]);
        }

        await SaveAuthorizationChangesAsync(user, actorId, "User roles changed by an administrator.", cancellationToken);
        return Result.Success(await BuildAuthorizationResponseAsync(userId, cancellationToken));
    }

    public async Task<Result<ManagedUserAuthorizationResponse>> ReplacePermissionsAsync(Guid userId, ReplaceManagedUserPermissionsRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorId))
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.CurrentUserUnavailable);
        }
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.NotFound);
        }
        if (user.IsDevelopmentOnly)
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.ProtectedAccount);
        }
        if (user.Id == actorId)
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.SelfSecurityChange);
        }

        var assignments = request.Assignments ?? [];
        if (!ValidatePermissionAssignments(assignments))
        {
            return Result.Failure<ManagedUserAuthorizationResponse>(UserManagementErrors.InvalidRequest);
        }
        var parsedScopes = new List<ParsedScope[]>();
        foreach (var assignment in assignments)
        {
            var validation = await ParseAndValidateScopesAsync(assignment.Scopes, assignment.IsAllHousingScope, assignment.IsAllClientScope, cancellationToken);
            if (validation.IsFailure)
            {
                return Result.Failure<ManagedUserAuthorizationResponse>(validation.Error);
            }
            parsedScopes.Add(validation.Value!);
        }

        var oldAssignments = await identityDbContext.UserDirectPermissionAssignments.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        await SoftDeleteScopesAsync([], oldAssignments.Select(item => item.Id).ToArray(), cancellationToken);
        identityDbContext.UserDirectPermissionAssignments.RemoveRange(oldAssignments);

        var now = timeProvider.GetUtcNow();
        for (var index = 0; index < assignments.Count; index++)
        {
            var input = assignments[index];
            var entity = new UserDirectPermissionAssignment
            {
                UserId = userId,
                PermissionKey = input.PermissionKey.Trim(),
                Effect = Enum.Parse<PermissionEffect>(input.Effect, true),
                StartsAtUtc = input.StartsAtUtc ?? now,
                ExpiresAtUtc = input.ExpiresAtUtc,
                GrantedByUserId = actorId,
                GrantReason = TrimOrNull(input.Reason) ?? "Direct permission assigned by an administrator.",
                IsAllHousingScope = input.IsAllHousingScope,
                IsAllClientScope = input.IsAllClientScope,
                IncludesFuturePlatformContracts = input.IncludesFuturePlatformContracts
            };
            identityDbContext.UserDirectPermissionAssignments.Add(entity);
            AddScopes(null, entity.Id, parsedScopes[index]);
        }

        await SaveAuthorizationChangesAsync(user, actorId, "User direct permissions changed by an administrator.", cancellationToken);
        return Result.Success(await BuildAuthorizationResponseAsync(userId, cancellationToken));
    }

    private async Task<ManagedUserAuthorizationResponse> BuildAuthorizationResponseAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await identityDbContext.Users.AsNoTracking().SingleAsync(item => item.Id == userId, cancellationToken);
        var roleAssignments = await (from assignment in identityDbContext.UserRoleAssignments.AsNoTracking()
                                     join role in identityDbContext.Roles.AsNoTracking() on assignment.RoleId equals role.Id
                                     where assignment.UserId == userId
                                     select new RoleAssignmentRow(assignment, role.Code)).ToListAsync(cancellationToken);
        var directAssignments = await identityDbContext.UserDirectPermissionAssignments.AsNoTracking()
            .Where(assignment => assignment.UserId == userId)
            .ToListAsync(cancellationToken);
        var roleIds = roleAssignments.Select(row => row.Assignment.Id).ToArray();
        var directIds = directAssignments.Select(row => row.Id).ToArray();
        var scopes = roleIds.Length == 0 && directIds.Length == 0 ? [] : await identityDbContext.AccessScopes.AsNoTracking()
            .Where(scope => scope.UserRoleAssignmentId.HasValue && roleIds.Contains(scope.UserRoleAssignmentId.Value)
                || scope.DirectPermissionAssignmentId.HasValue && directIds.Contains(scope.DirectPermissionAssignmentId.Value))
            .ToListAsync(cancellationToken);

        return new ManagedUserAuthorizationResponse(user.AuthorizationVersion,
            roleAssignments.OrderBy(row => row.RoleCode).Select(row => new ManagedUserRoleAssignmentResponse(
                row.Assignment.Id, row.Assignment.RoleId, row.RoleCode, row.Assignment.StartsAtUtc, row.Assignment.ExpiresAtUtc,
                row.Assignment.GrantReason, row.Assignment.IsAllHousingScope, row.Assignment.IsAllClientScope,
                row.Assignment.IncludesFuturePlatformContracts, scopes.Where(scope => scope.UserRoleAssignmentId == row.Assignment.Id)
                    .Select(ToScopeResponse).ToArray())).ToArray(),
            directAssignments.OrderBy(item => item.PermissionKey).Select(item => new ManagedUserDirectPermissionResponse(
                item.Id, item.PermissionKey, item.Effect.ToString(), item.StartsAtUtc, item.ExpiresAtUtc, item.GrantReason,
                item.IsAllHousingScope, item.IsAllClientScope, item.IncludesFuturePlatformContracts,
                scopes.Where(scope => scope.DirectPermissionAssignmentId == item.Id).Select(ToScopeResponse).ToArray())).ToArray());
    }

    private async Task<Result<ParsedScope[]>> ParseAndValidateScopesAsync(IReadOnlyList<AuthorizationScopeRequest>? scopes, bool isAllHousingScope, bool isAllClientScope, CancellationToken cancellationToken)
    {
        var parsed = new List<ParsedScope>();
        foreach (var scope in scopes ?? [])
        {
            if (scope.TargetId == Guid.Empty || !Enum.TryParse<AccessScopeType>(scope.Type, true, out var type))
            {
                return Result.Failure<ParsedScope[]>(InvalidField("scopes", "Each scope requires a supported type and a non-empty targetId."));
            }
            parsed.Add(new ParsedScope(type, scope.TargetId));
        }
        if (parsed.Distinct().Count() != parsed.Count
            || isAllHousingScope && parsed.Any(scope => scope.Type == AccessScopeType.Housing)
            || isAllClientScope && parsed.Any(scope => scope.Type is AccessScopeType.ClientPlatform or AccessScopeType.ClientContract))
        {
            return Result.Failure<ParsedScope[]>(InvalidField("scopes", "Scopes must be unique and cannot overlap with an all-housing or all-client scope flag."));
        }

        foreach (var scope in parsed)
        {
            var exists = scope.Type switch
            {
                AccessScopeType.Housing => await applicationDbContext.Housing.AnyAsync(item => item.Id == scope.TargetId, cancellationToken),
                AccessScopeType.ClientPlatform => await applicationDbContext.ClientPlatforms.AnyAsync(item => item.Id == scope.TargetId, cancellationToken),
                AccessScopeType.ClientContract => await applicationDbContext.ClientContracts.AnyAsync(item => item.Id == scope.TargetId, cancellationToken),
                _ => false
            };
            if (!exists)
            {
                return Result.Failure<ParsedScope[]>(UserManagementErrors.NotFound);
            }
        }
        return Result.Success(parsed.ToArray());
    }

    private async Task SoftDeleteScopesAsync(Guid[] roleAssignmentIds, Guid[] directAssignmentIds, CancellationToken cancellationToken)
    {
        if (roleAssignmentIds.Length == 0 && directAssignmentIds.Length == 0)
        {
            return;
        }
        var scopes = await identityDbContext.AccessScopes.Where(scope =>
            scope.UserRoleAssignmentId.HasValue && roleAssignmentIds.Contains(scope.UserRoleAssignmentId.Value)
            || scope.DirectPermissionAssignmentId.HasValue && directAssignmentIds.Contains(scope.DirectPermissionAssignmentId.Value)).ToListAsync(cancellationToken);
        identityDbContext.AccessScopes.RemoveRange(scopes);
    }

    private void AddScopes(Guid? roleAssignmentId, Guid? directAssignmentId, IEnumerable<ParsedScope> scopes)
    {
        foreach (var scope in scopes)
        {
            identityDbContext.AccessScopes.Add(new AccessScope
            {
                UserRoleAssignmentId = roleAssignmentId,
                DirectPermissionAssignmentId = directAssignmentId,
                ScopeType = scope.Type,
                TargetId = scope.TargetId
            });
        }
    }

    private async Task SaveAuthorizationChangesAsync(ApplicationUser user, Guid actorId, string reason, CancellationToken cancellationToken)
    {
        await RevokeSessionsAndIncrementAuthorizationAsync(user, actorId, reason, timeProvider.GetUtcNow(), cancellationToken);
    }

    private async Task RevokeSessionsAndIncrementAuthorizationAsync(ApplicationUser user, Guid actorId, string reason, DateTimeOffset now, CancellationToken cancellationToken, bool saveChanges = true)
    {
        var sessions = await identityDbContext.UserSessions.Where(session => session.UserId == user.Id && session.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAtUtc = now;
            session.RevokedByUserId = actorId;
            session.RevocationReason = reason.Trim()[..Math.Min(reason.Trim().Length, 1000)];
        }
        user.AuthorizationVersion++;
        user.SessionsRevokedAtUtc = now;
        if (saveChanges)
        {
            await identityDbContext.SaveChangesAsync(cancellationToken);
            sessionValidator.InvalidateUser(user.Id);
        }
    }

    private async Task<ApplicationUser?> FindUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await identityDbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    private async Task<ManagedRoleResponse> BuildRoleResponseAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await identityDbContext.Roles.AsNoTracking().SingleAsync(item => item.Id == roleId, cancellationToken);
        var permissions = await identityDbContext.RolePermissionGrants.AsNoTracking()
            .Where(item => item.RoleId == roleId)
            .OrderBy(item => item.PermissionKey)
            .Select(item => item.PermissionKey)
            .ToArrayAsync(cancellationToken);
        return new ManagedRoleResponse(
            role.Id,
            role.Code,
            role.NameAr,
            role.NameEn,
            role.Status.ToString(),
            role.IsProtected,
            permissions,
            Convert.ToBase64String(role.RowVersion));
    }

    private async Task RevokeActiveTemporaryCredentialsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var credentials = await identityDbContext.TemporaryCredentials
            .Where(item => item.UserId == userId && item.ConsumedAtUtc == null && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var credential in credentials)
        {
            credential.RevokedAtUtc = now;
        }
    }

    private async Task<bool> IsUserNameInUseAsync(string? userName, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName)) return false;
        var normalized = userManager.NormalizeName(userName.Trim());
        return await identityDbContext.Users.IgnoreQueryFilters().AnyAsync(user => user.NormalizedUserName == normalized && user.Id != exceptUserId, cancellationToken);
    }

    private async Task<bool> IsEmailInUseAsync(string? email, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var normalized = userManager.NormalizeEmail(email.Trim());
        return await identityDbContext.Users.IgnoreQueryFilters().AnyAsync(user => user.NormalizedEmail == normalized && user.Id != exceptUserId, cancellationToken);
    }

    private async Task<bool> IsEmployeeAvailableAsync(Guid? employeeId, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        if (employeeId is null) return true;
        return await applicationDbContext.Employees.AnyAsync(employee => employee.Id == employeeId.Value, cancellationToken)
            && !await identityDbContext.Users.IgnoreQueryFilters().AnyAsync(user => user.EmployeeId == employeeId && user.Id != exceptUserId, cancellationToken);
    }

    private static bool IsValidCreateRequest(CreateManagedUserRequest request) =>
        HasText(request.UserName, 256) && HasText(request.InitialPassword, 512) && HasText(request.DisplayNameAr, 200)
        && IsOptionalText(request.DisplayNameEn, 200) && IsOptionalText(request.Email, 256) && IsOptionalText(request.PhoneNumber, 50);

    private static bool IsValidUpdateRequest(UpdateManagedUserRequest request) =>
        HasText(request.UserName, 256) && HasText(request.DisplayNameAr, 200) && HasText(request.RowVersion, 128)
        && IsOptionalText(request.DisplayNameEn, 200) && IsOptionalText(request.Email, 256) && IsOptionalText(request.PhoneNumber, 50);

    private static bool ValidateRoleAssignments(IReadOnlyList<ManagedRoleAssignmentRequest> assignments) =>
        assignments.All(item => item.RoleId != Guid.Empty && IsValidWindow(item.StartsAtUtc, item.ExpiresAtUtc)
            && IsOptionalText(item.Reason, 1000)) && assignments.Select(item => item.RoleId).Distinct().Count() == assignments.Count;

    private static bool ValidatePermissionAssignments(IReadOnlyList<ManagedDirectPermissionAssignmentRequest> assignments) =>
        assignments.All(item => PermissionKeys.All.Contains(item.PermissionKey.Trim())
            && Enum.TryParse<PermissionEffect>(item.Effect, true, out _)
            && IsValidWindow(item.StartsAtUtc, item.ExpiresAtUtc) && IsOptionalText(item.Reason, 1000))
        && assignments.Select(item => item.PermissionKey.Trim()).Distinct(StringComparer.Ordinal).Count() == assignments.Count;

    private static bool IsValidWindow(DateTimeOffset? startsAtUtc, DateTimeOffset? expiresAtUtc) =>
        expiresAtUtc is null || (startsAtUtc is not null && expiresAtUtc > startsAtUtc);

    private static OperationError DescribeInvalidCreateRequest(CreateManagedUserRequest request)
    {
        var invalidFields = new List<string>();
        if (!HasText(request.UserName, 256)) invalidFields.Add("userName is required and must be 256 characters or fewer.");
        if (!HasText(request.InitialPassword, 512)) invalidFields.Add("initialPassword is required and must be 512 characters or fewer.");
        if (!HasText(request.DisplayNameAr, 200)) invalidFields.Add("displayNameAr is required and must be 200 characters or fewer.");
        if (!IsOptionalText(request.DisplayNameEn, 200)) invalidFields.Add("displayNameEn must be 200 characters or fewer.");
        if (!IsOptionalText(request.Email, 256)) invalidFields.Add("email must be 256 characters or fewer.");
        if (!IsOptionalText(request.PhoneNumber, 50)) invalidFields.Add("phoneNumber must be 50 characters or fewer.");

        return UserManagementErrors.InvalidRequest with
        {
            Field = "request",
            Details = new Dictionary<string, object?> { ["validationErrors"] = invalidFields }
        };
    }

    private static OperationError DescribeInvalidRoleAssignments(IReadOnlyList<ManagedRoleAssignmentRequest> assignments)
    {
        if (assignments.Count == 0)
        {
            return InvalidField("roleAssignments", "At least one role assignment is required. Omit roleAssignments entirely to use the default USER role.");
        }

        for (var index = 0; index < assignments.Count; index++)
        {
            var assignment = assignments[index];
            var field = $"roleAssignments[{index}]";
            if (assignment.RoleId == Guid.Empty) return InvalidField(field + ".roleId", "A non-empty roleId is required.");
            if (!IsValidWindow(assignment.StartsAtUtc, assignment.ExpiresAtUtc)) return InvalidField(field, "expiresAtUtc requires startsAtUtc and must be later than it.");
            if (!IsOptionalText(assignment.Reason, 1000)) return InvalidField(field + ".reason", "reason must be 1,000 characters or fewer.");
        }

        return InvalidField("roleAssignments", "Each role may be assigned only once.");
    }

    private static OperationError DescribeInvalidPermissionAssignments(IReadOnlyList<ManagedDirectPermissionAssignmentRequest> assignments)
    {
        for (var index = 0; index < assignments.Count; index++)
        {
            var assignment = assignments[index];
            var field = $"directPermissionAssignments[{index}]";
            if (string.IsNullOrWhiteSpace(assignment.PermissionKey) || !PermissionKeys.All.Contains(assignment.PermissionKey.Trim())) return InvalidField(field + ".permissionKey", "The permissionKey is missing or is not in the permission catalog.");
            if (!Enum.TryParse<PermissionEffect>(assignment.Effect, true, out _)) return InvalidField(field + ".effect", "effect must be Grant or Deny.");
            if (!IsValidWindow(assignment.StartsAtUtc, assignment.ExpiresAtUtc)) return InvalidField(field, "expiresAtUtc requires startsAtUtc and must be later than it.");
            if (!IsOptionalText(assignment.Reason, 1000)) return InvalidField(field + ".reason", "reason must be 1,000 characters or fewer.");
        }

        return InvalidField("directPermissionAssignments", "Each permissionKey may be assigned only once.");
    }

    private static OperationError InvalidField(string field, string reason) =>
        UserManagementErrors.InvalidRequest with
        {
            Field = field,
            Details = new Dictionary<string, object?> { ["reason"] = reason }
        };

    private static OperationError DescribeIdentityFailure(IdentityResult result, string passwordField)
    {
        var errors = result.Errors.ToArray();
        var details = new Dictionary<string, object?>
        {
            ["identityErrors"] = errors
                .Select(error => new { error.Code, error.Description })
                .ToArray()
        };

        if (errors.Any(error => error.Code.StartsWith("Password", StringComparison.Ordinal)))
        {
            return UserManagementErrors.PasswordRejected with
            {
                Field = passwordField,
                Details = details
            };
        }

        if (errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            return UserManagementErrors.Duplicate with { Details = details };
        }

        return UserManagementErrors.InvalidRequest with { Details = details };
    }

    private static bool MatchesRowVersion(byte[] rowVersion, string? supplied) =>
        !string.IsNullOrWhiteSpace(supplied) && Convert.TryFromBase64String(supplied, new Span<byte>(new byte[rowVersion.Length]), out _)
        && string.Equals(Convert.ToBase64String(rowVersion), supplied, StringComparison.Ordinal);

    private static ManagedUserResponse ToResponse(ApplicationUser user) => new(
        user.Id, user.EmployeeId, user.UserName ?? string.Empty, user.Email, user.PhoneNumber,
        user.DisplayNameAr, user.DisplayNameEn, user.Status.ToString(), user.RequiresPasswordChange,
        user.IsDevelopmentOnly, user.LastLoginAtUtc, user.LastActivityAtUtc, user.CreatedAtUtc,
        Convert.ToBase64String(user.RowVersion));

    private static ManagedAuthorizationScopeResponse ToScopeResponse(AccessScope scope) => new(scope.ScopeType.ToString(), scope.TargetId);
    private bool TryGetActor(out Guid actorId) => (actorId = currentUser.UserId ?? Guid.Empty) != Guid.Empty;
    private static bool HasText(string? value, int maxLength) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;
    private static bool IsOptionalText(string? value, int maxLength) => value is null || value.Trim().Length <= maxLength;
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    internal static string HashTemporarySecret(
        IPasswordHasher<ApplicationUser> passwordHasher,
        ApplicationUser user,
        string value) =>
        passwordHasher.HashPassword(user, value);

    internal static PasswordVerificationResult VerifyTemporarySecret(
        IPasswordHasher<ApplicationUser> passwordHasher,
        ApplicationUser user,
        string storedHash,
        string value)
    {
        // Credentials issued before the salted-hash migration use the legacy SHA-512 format.
        // Treat a successful legacy verification as a rehash request so the next sign-in upgrades it.
        if (storedHash.Length == 128 && storedHash.All(Uri.IsHexDigit))
        {
            var legacyHash = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(value)));
            return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(storedHash),
                    Encoding.UTF8.GetBytes(legacyHash))
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }

        try
        {
            return passwordHasher.VerifyHashedPassword(user, storedHash, value);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    internal static string CreateTemporarySecret() =>
        $"{Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))}aA1!";

    private sealed record ParsedScope(AccessScopeType Type, Guid TargetId);
    private sealed record RoleAssignmentRow(UserRoleAssignment Assignment, string RoleCode);
}
