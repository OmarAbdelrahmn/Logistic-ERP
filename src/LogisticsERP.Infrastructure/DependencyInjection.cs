using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Persistence;
using LogisticsERP.Application.Features.Authentication;
using LogisticsERP.Application.Features.UserProfiles;
using LogisticsERP.Application.Features.UserManagement;
using LogisticsERP.Application.Features.Company;
using LogisticsERP.Application.Features.Tags;
using LogisticsERP.Application.Features.SupportAccess;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Infrastructure.Authentication;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Identity.Interceptors;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Company;
using LogisticsERP.Infrastructure.Tags;
using LogisticsERP.Infrastructure.SystemServices;
using LogisticsERP.Infrastructure.Files;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Application.Features.Hr;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<ISupportAccessService, SupportAccessService>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IHrCatalogService, HrCatalogService>();
        services.AddScoped<IWorkforceService, WorkforceService>();
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<IEmployeeExpiryComplianceService, EmployeeExpiryComplianceService>();
        services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
        services.AddScoped<IHousingService, HousingService>();
        services.AddScoped<IPlatformOperationsService, PlatformOperationsService>();
        services.AddScoped<ISimplePlatformService, SimplePlatformService>();
        services.AddScoped<IHrWorkflowService, HrWorkflowService>();
        services.AddScoped<IHrExcelImportService, HrExcelImportService>();
        services.AddScoped<ILeaveDocumentService, LeaveDocumentService>();
        services.AddScoped<IHrFormTemplateService, HrFormTemplateService>();
        services.AddScoped<ICompanyProfileService, CompanyProfileService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<ISavedViewService, SavedViewService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IDatasetVersionService, DatasetVersionService>();
        services.AddScoped<IPrivateFileStorage, PrivateFileStorage>();
        services.AddScoped<FleetServiceSupport>();
        services.AddScoped<IFleetService, FleetService>();
        services.AddScoped<IVehicleFileService, VehicleFileService>();
        services.AddOptions<PdfGenerationOptions>()
            .Bind(configuration.GetSection(PdfGenerationOptions.SectionName))
            .Validate(options => options.QuestPdfLicense is "Community" or "Professional" or "Enterprise", "A valid QuestPDF license tier must be configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ArabicFontFamily), "An Arabic PDF font family is required.")
            .ValidateOnStart();
        services.AddSingleton<IAccidentPdfGenerator, AccidentPdfGenerator>();
        services.AddScoped<IVehicleAccidentService, VehicleAccidentService>();
        services.AddScoped<IFleetComplianceNotificationService, FleetComplianceNotificationService>();
        services.AddSingleton<ISensitiveValueProtector>(provider => new SensitiveValueProtector(
            ResolveSensitiveDataKey(configuration, provider.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>().IsDevelopment())));
        services.AddSingleton<IPlatformCredentialProtector>(provider => new PlatformCredentialProtector(
            ResolveSensitiveDataKey(configuration, provider.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>().IsDevelopment())));

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            ConfigureSqlServer(options, connectionString, "__ApplicationMigrationsHistory", enableRetryOnFailure: true);
            options.AddInterceptors(provider.GetRequiredService<ApplicationPersistenceInterceptor>());
        });
        services.AddDbContext<IdentityDbContext>((provider, options) =>
        {
            // Authentication deliberately uses explicit transactions for session replacement.
            // Client retries are safer than an execution strategy retrying a security transaction.
            ConfigureSqlServer(options, connectionString, "__IdentityMigrationsHistory", enableRetryOnFailure: false);
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

    private static byte[] ResolveSensitiveDataKey(IConfiguration configuration, bool isDevelopment)
    {
        var configured = configuration["SensitiveData:EncryptionKey"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                var decoded = Convert.FromBase64String(configured);
                if (decoded.Length >= 32)
                {
                    return decoded;
                }
            }
            catch (FormatException)
            {
                // The validation exception below intentionally avoids echoing secret material.
            }

            throw new InvalidOperationException("SensitiveData:EncryptionKey must be a Base64 value containing at least 32 bytes.");
        }

        var developmentSigningKey = configuration["Authentication:SigningKey"];
        if (isDevelopment && !string.IsNullOrWhiteSpace(developmentSigningKey))
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(developmentSigningKey));
        }

        throw new InvalidOperationException("SensitiveData:EncryptionKey must be supplied from a secret source outside Development.");
    }

    private static void ConfigureSqlServer(
        DbContextOptionsBuilder options,
        string connectionString,
        string migrationsHistoryTable,
        bool enableRetryOnFailure)
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
            sqlOptions.MigrationsHistoryTable(migrationsHistoryTable, "migration");
            if (enableRetryOnFailure)
            {
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }
        });
        options.EnableDetailedErrors();
    }
}

internal sealed class SystemCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public Guid? SessionId => null;
    public long? AuthorizationVersion => null;
    public string? CorrelationId => null;
}
