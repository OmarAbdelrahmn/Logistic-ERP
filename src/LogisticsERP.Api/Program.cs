using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using LogisticsERP.Api.Authentication;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Development;
using LogisticsERP.Api.Middleware;
using LogisticsERP.Api.OpenApi;
using LogisticsERP.Application;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole();

var authenticationOptions = builder.Configuration
    .GetSection(AuthenticationOptions.SectionName)
    .Get<AuthenticationOptions>()
    ?? throw new InvalidOperationException("Authentication configuration is required.");

authenticationOptions.DevelopmentAccountsEnabled = builder.Environment.IsDevelopment();

if (string.IsNullOrWhiteSpace(authenticationOptions.Issuer)
    || string.IsNullOrWhiteSpace(authenticationOptions.Audience))
{
    throw new InvalidOperationException("Authentication issuer and audience are required.");
}

if (string.IsNullOrWhiteSpace(authenticationOptions.SigningKey))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Authentication:SigningKey must be supplied from a secret source.");
    }

    authenticationOptions.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}

if (Encoding.UTF8.GetByteCount(authenticationOptions.SigningKey) < 64)
{
    throw new InvalidOperationException("Authentication:SigningKey must contain at least 64 bytes.");
}

if (authenticationOptions.AccessTokenMinutes is < 1 or > 60
    || authenticationOptions.RefreshTokenIdleDays is < 1 or > 30
    || authenticationOptions.RefreshTokenAbsoluteDays < authenticationOptions.RefreshTokenIdleDays
    || authenticationOptions.RefreshTokenAbsoluteDays > 90
    || authenticationOptions.MaxActiveSessions is < 1 or > 50
    || authenticationOptions.SessionValidationCacheSeconds is < 1 or > 60)
{
    throw new InvalidOperationException("Authentication lifetime configuration is outside the allowed security limits.");
}

builder.Services.AddSingleton(authenticationOptions);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
    context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddResponseCompression();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.SaveToken = false;
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authenticationOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authenticationOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationOptions.SigningKey)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = AuthenticationClaimNames.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst(AuthenticationClaimNames.Subject)?.Value;
                var session = context.Principal?.FindFirst(AuthenticationClaimNames.SessionId)?.Value;
                var version = context.Principal?.FindFirst(AuthenticationClaimNames.AuthorizationVersion)?.Value;
                if (!Guid.TryParse(subject, out var userId)
                    || !Guid.TryParse(session, out var sessionId)
                    || !long.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var authorizationVersion))
                {
                    context.Fail("The access token is missing required session claims.");
                    return;
                }

                var validator = context.HttpContext.RequestServices
                    .GetRequiredService<IAuthenticationSessionValidator>();
                if (!await validator.IsValidAsync(
                    userId,
                    sessionId,
                    authorizationVersion,
                    context.HttpContext.RequestAborted))
                {
                    context.Fail("The access token session is no longer valid.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    var standardPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireClaim(AuthenticationClaimNames.PasswordChangeRequired, "false")
        .Build();

    options.DefaultPolicy = standardPolicy;
    options.FallbackPolicy = standardPolicy;
    options.AddPolicy(AuthenticationPolicies.AllowPasswordChangeRequired, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey = context.User.FindFirst(AuthenticationClaimNames.Subject)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DevelopmentIdentitySeeder.SeedAsync(
        app.Services,
        app.Configuration,
        app.Lifetime.ApplicationStopping);
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Al Bawaba Logistics ERP API v1");
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapControllers();

await app.RunAsync();

public partial class Program;
