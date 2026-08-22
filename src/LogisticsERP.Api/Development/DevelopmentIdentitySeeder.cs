using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Api.Development;

internal static partial class DevelopmentIdentitySeeder
{
    private static readonly Guid DevelopmentUserId =
        Guid.Parse("019c18d5-62e1-7000-c000-000000000001");

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var options = configuration
            .GetSection(DevelopmentIdentityOptions.SectionName)
            .Get<DevelopmentIdentityOptions>();
        if (options is not { Enabled: true })
        {
            return;
        }

        Validate(options);

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DevelopmentIdentitySeeder");

        var roleExists = await dbContext.Roles
            .AsNoTracking()
            .AnyAsync(role => role.Id == SystemRoles.SystemAdminId, cancellationToken);
        if (!roleExists)
        {
            throw new InvalidOperationException(
                "The protected SYSTEM_ADMIN role is missing. Apply the Identity database migrations before starting the development API.");
        }

        var normalizedUserName = userManager.NormalizeName(options.UserName);
        var conflictingUser = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NormalizedUserName == normalizedUserName,
                cancellationToken);
        if (conflictingUser is not null && conflictingUser.Id != DevelopmentUserId)
        {
            throw new InvalidOperationException(
                $"The development username '{options.UserName}' is already used by a non-development account. Disable the development seed or choose another local database.");
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == DevelopmentUserId,
            cancellationToken);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = DevelopmentUserId,
                UserName = options.UserName,
                Email = options.Email,
                EmailConfirmed = true,
                DisplayNameAr = options.DisplayNameAr,
                DisplayNameEn = options.DisplayNameEn,
                PreferredLocale = "ar",
                Status = UserAccountStatus.PendingTemporaryPassword,
                RequiresPasswordChange = true,
                IsDevelopmentOnly = true,
                AuthorizationVersion = 1,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            var createResult = await userManager.CreateAsync(user, options.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(error => $"{error.Code}: {error.Description}"));
                throw new InvalidOperationException(
                    $"The development SYSTEM_ADMIN account could not be created. {errors}");
            }

            LogDevelopmentUserCreated(logger, options.UserName);
        }

        var hasSystemAdminRole = await dbContext.UserRoleAssignments
            .AnyAsync(
                assignment => assignment.UserId == user.Id
                    && assignment.RoleId == SystemRoles.SystemAdminId
                    && assignment.ExpiresAtUtc == null,
                cancellationToken);
        var authorizationChanged = false;
        if (!hasSystemAdminRole)
        {
            dbContext.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = SystemRoles.SystemAdminId,
                StartsAtUtc = timeProvider.GetUtcNow(),
                GrantedByUserId = user.Id,
                GrantReason = "Development-only initial SYSTEM_ADMIN assignment."
            });
            authorizationChanged = true;
        }

        var activeMaximumGrantKeys = await dbContext.UserDirectPermissionAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == user.Id
                && assignment.Effect == PermissionEffect.Grant
                && assignment.ExpiresAtUtc == null
                && assignment.IsAllHousingScope
                && assignment.IsAllClientScope)
            .Select(assignment => assignment.PermissionKey)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
        var permissionStartsAtUtc = timeProvider.GetUtcNow();
        foreach (var permissionKey in PermissionKeys.All.Except(activeMaximumGrantKeys))
        {
            dbContext.UserDirectPermissionAssignments.Add(new UserDirectPermissionAssignment
            {
                UserId = user.Id,
                PermissionKey = permissionKey,
                Effect = PermissionEffect.Grant,
                StartsAtUtc = permissionStartsAtUtc,
                GrantedByUserId = user.Id,
                GrantReason = "Development-only maximum-access grant.",
                IsAllHousingScope = true,
                IsAllClientScope = true,
                IncludesFuturePlatformContracts = true
            });
            authorizationChanged = true;
        }

        if (!authorizationChanged)
        {
            return;
        }

        user.AuthorizationVersion++;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(DevelopmentIdentityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.UserName)
            || string.IsNullOrWhiteSpace(options.Email)
            || string.IsNullOrWhiteSpace(options.Password)
            || string.IsNullOrWhiteSpace(options.DisplayNameAr)
            || string.IsNullOrWhiteSpace(options.DisplayNameEn))
        {
            throw new InvalidOperationException(
                "The enabled DevelopmentIdentity configuration must provide UserName, Email, Password, DisplayNameAr, and DisplayNameEn.");
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Created the development-only SYSTEM_ADMIN account {UserName}. It must change its temporary password after login.")]
    private static partial void LogDevelopmentUserCreated(ILogger logger, string userName);
}

internal sealed class DevelopmentIdentityOptions
{
    public const string SectionName = "DevelopmentIdentity";

    public bool Enabled { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayNameAr { get; init; } = string.Empty;
    public string DisplayNameEn { get; init; } = string.Empty;
}
