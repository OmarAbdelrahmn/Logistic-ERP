using System.Text;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("LogisticsDatabase")))
{
    Console.Error.WriteLine(
        "Set ConnectionStrings__LogisticsDatabase in the current process before running this tool.");
    return 1;
}

builder.Services.AddInfrastructure(builder.Configuration);
using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
var now = timeProvider.GetUtcNow();

var roleExists = await dbContext.Roles
    .AsNoTracking()
    .AnyAsync(role => role.Id == SystemRoles.SystemAdminId);
if (!roleExists)
{
    Console.Error.WriteLine(
        "SYSTEM_ADMIN is missing. Apply the Identity migrations before running this tool.");
    return 2;
}

var activeSystemAdminExists = await (
    from assignment in dbContext.UserRoleAssignments.AsNoTracking()
    join existingUser in dbContext.Users.AsNoTracking() on assignment.UserId equals existingUser.Id
    where assignment.RoleId == SystemRoles.SystemAdminId
        && assignment.StartsAtUtc <= now
        && (assignment.ExpiresAtUtc == null || assignment.ExpiresAtUtc > now)
        && !existingUser.IsDevelopmentOnly
        && existingUser.Status != UserAccountStatus.Archived
        && existingUser.Status != UserAccountStatus.Suspended
    select assignment.Id)
    .AnyAsync();
if (activeSystemAdminExists)
{
    Console.Error.WriteLine(
        "An active SYSTEM_ADMIN already exists. This one-time bootstrap tool will not create another account.");
    return 3;
}

var userName = ReadRequired("Username: ");
var email = ReadRequired("Email: ");
var displayNameAr = ReadRequired("Arabic display name: ");
var displayNameEn = ReadRequired("English display name: ");
var password = ReadPassword("Temporary password: ");
var passwordConfirmation = ReadPassword("Confirm temporary password: ");
if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
{
    Console.Error.WriteLine("The password confirmation does not match.");
    return 4;
}

var user = new ApplicationUser
{
    Id = Guid.CreateVersion7(),
    UserName = userName,
    NormalizedUserName = userManager.NormalizeName(userName),
    Email = email,
    NormalizedEmail = userManager.NormalizeEmail(email),
    EmailConfirmed = true,
    DisplayNameAr = displayNameAr,
    DisplayNameEn = displayNameEn,
    PreferredLocale = "ar",
    Status = UserAccountStatus.PendingTemporaryPassword,
    RequiresPasswordChange = true,
    IsDevelopmentOnly = false,
    AuthorizationVersion = 1,
    LockoutEnabled = true,
    SecurityStamp = Guid.NewGuid().ToString(),
    ConcurrencyStamp = Guid.NewGuid().ToString()
};

var validationErrors = new List<IdentityError>();
foreach (var validator in userManager.UserValidators)
{
    var result = await validator.ValidateAsync(userManager, user);
    if (!result.Succeeded)
    {
        validationErrors.AddRange(result.Errors);
    }
}

foreach (var validator in userManager.PasswordValidators)
{
    var result = await validator.ValidateAsync(userManager, user, password);
    if (!result.Succeeded)
    {
        validationErrors.AddRange(result.Errors);
    }
}

if (validationErrors.Count > 0)
{
    foreach (var error in validationErrors.DistinctBy(error => error.Code))
    {
        Console.Error.WriteLine($"{error.Code}: {error.Description}");
    }

    return 5;
}

user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);
dbContext.Users.Add(user);
dbContext.UserRoleAssignments.Add(new UserRoleAssignment
{
    UserId = user.Id,
    RoleId = SystemRoles.SystemAdminId,
    StartsAtUtc = now,
    GrantedByUserId = user.Id,
    GrantReason = "One-time production bootstrap of the first SYSTEM_ADMIN."
});
await dbContext.SaveChangesAsync();

Console.WriteLine(
    "The first SYSTEM_ADMIN was created. Login and change the temporary password immediately.");
return 0;

static string ReadRequired(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var value = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        Console.Error.WriteLine("A value is required.");
    }
}

static string ReadPassword(string prompt)
{
    Console.Write(prompt);
    var value = new StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return value.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (value.Length > 0)
            {
                value.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            value.Append(key.KeyChar);
        }
    }
}
