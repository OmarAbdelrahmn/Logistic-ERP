using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Persistence;
using LogisticsERP.Application.Features.Authentication;
using LogisticsERP.Application.Features.UserProfiles;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Infrastructure.Authentication;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Identity.Interceptors;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogisticsERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LogisticsDatabase")
            ?? throw new InvalidOperationException("Connection string 'LogisticsDatabase' is required.");

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddMemoryCache();
        services.AddScoped<ApplicationPersistenceInterceptor>();
        services.AddScoped<IdentityPersistenceInterceptor>();
        services.AddSingleton<IAccessTokenFactory, AccessTokenFactory>();
        services.AddScoped<IAuthenticationSessionValidator, AuthenticationSessionValidator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            ConfigureSqlServer(options, connectionString, "__ApplicationMigrationsHistory");
            options.AddInterceptors(provider.GetRequiredService<ApplicationPersistenceInterceptor>());
        });
        services.AddDbContext<IdentityDbContext>((provider, options) =>
        {
            ConfigureSqlServer(options, connectionString, "__IdentityMigrationsHistory");
            options.AddInterceptors(provider.GetRequiredService<IdentityPersistenceInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<IdentityDbContext>();

        return services;
    }

    private static void ConfigureSqlServer(
        DbContextOptionsBuilder options,
        string connectionString,
        string migrationsHistoryTable)
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
            sqlOptions.MigrationsHistoryTable(migrationsHistoryTable, "migration");
            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        });
        options.EnableDetailedErrors();
    }
}

internal sealed class SystemCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public Guid? SessionId => null;
    public string? CorrelationId => null;
}
