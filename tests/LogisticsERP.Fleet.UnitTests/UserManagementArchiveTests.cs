using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Features.UserManagement;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Authentication;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class UserManagementArchiveTests
{
    [Theory]
    [InlineData("userName")]
    [InlineData("email")]
    public async Task CreateUserIdentifiesTheDuplicateField(string duplicateField)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = CreateFixture();
        var existing = NewUser("existing", isArchived: true);
        existing.Email = "existing@example.com";
        existing.NormalizedEmail = "EXISTING@EXAMPLE.COM";
        fixture.Identity.Users.Add(existing);
        await fixture.Identity.SaveChangesAsync(cancellationToken);

        var request = NewCreateRequest(
            userName: duplicateField == "userName" ? "existing" : "new-user",
            email: duplicateField == "email" ? "existing@example.com" : null);
        var result = await fixture.Service.CreateUserAsync(request, cancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserManagementErrors.Duplicate.Code, result.Error.Code);
        Assert.Equal(duplicateField, result.Error.Field);
        Assert.NotEqual(UserManagementErrors.Duplicate.Description, result.Error.Description);
    }

    [Fact]
    public async Task CreateUserIdentifiesAnUnknownEmployee()
    {
        await using var fixture = CreateFixture();
        var result = await fixture.Service.CreateUserAsync(
            NewCreateRequest(employeeId: Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserManagementErrors.NotFound.Code, result.Error.Code);
        Assert.Equal("employeeId", result.Error.Field);
    }

    [Fact]
    public async Task CreateUserIdentifiesAnEmployeeLinkedToAnArchivedAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = CreateFixture();
        var employeeId = Guid.NewGuid();
        fixture.Application.Employees.Add(new Employee { Id = employeeId, FullNameAr = "موظف" });
        await fixture.Application.SaveChangesAsync(cancellationToken);
        var existing = NewUser("archived", isArchived: true);
        existing.EmployeeId = employeeId;
        fixture.Identity.Users.Add(existing);
        await fixture.Identity.SaveChangesAsync(cancellationToken);

        var result = await fixture.Service.CreateUserAsync(
            NewCreateRequest(employeeId: employeeId), cancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserManagementErrors.Duplicate.Code, result.Error.Code);
        Assert.Equal("employeeId", result.Error.Field);
    }

    [Fact]
    public async Task ArchivedUsersQueryReturnsOnlyArchivedUsersInNewestFirstOrder()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = CreateFixture();
        var older = NewUser("older", isArchived: true, fixture.Now.AddDays(-2));
        var newer = NewUser("newer", isArchived: true, fixture.Now.AddDays(-1));
        var active = NewUser("active", isArchived: false);
        fixture.Identity.Users.AddRange(older, newer, active);
        await fixture.Identity.SaveChangesAsync(cancellationToken);

        var activeResult = await fixture.Service.GetUsersAsync(null, cancellationToken);
        var archivedResult = await fixture.Service.GetArchivedUsersAsync(null, cancellationToken);

        Assert.True(activeResult.IsSuccess);
        Assert.Equal([active.Id], activeResult.Value!.Select(user => user.Id));
        Assert.True(archivedResult.IsSuccess);
        Assert.Equal([newer.Id, older.Id], archivedResult.Value!.Select(user => user.Id));
        Assert.All(archivedResult.Value!, user => Assert.Equal(nameof(UserAccountStatus.Archived), user.Status));
    }

    [Fact]
    public async Task RestoreUserClearsArchiveStateAndReactivatesAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = CreateFixture();
        var archived = NewUser("archived", isArchived: true, fixture.Now.AddDays(-1));
        archived.AuthorizationVersion = 4;
        archived.LockoutEnd = fixture.Now.AddYears(1);
        fixture.Identity.Users.Add(archived);
        await fixture.Identity.SaveChangesAsync(cancellationToken);

        var result = await fixture.Service.RestoreUserAsync(
            archived.Id,
            new RestoreManagedUserRequest(Convert.ToBase64String(archived.RowVersion)),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(UserAccountStatus.Active), result.Value!.Status);
        var restored = await fixture.Identity.Users
            .IgnoreQueryFilters()
            .SingleAsync(user => user.Id == archived.Id, cancellationToken);
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DeletedAtUtc);
        Assert.Null(restored.DeletedByUserId);
        Assert.Null(restored.DeletionReason);
        Assert.Null(restored.LockoutEnd);
        Assert.Equal(UserAccountStatus.Active, restored.Status);
        Assert.Equal(5, restored.AuthorizationVersion);
        Assert.Equal(archived.Id, fixture.Sessions.InvalidatedUserId);
    }

    [Fact]
    public async Task RestoreUserRejectsAStaleRowVersion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = CreateFixture();
        var archived = NewUser("archived", isArchived: true, fixture.Now.AddDays(-1));
        fixture.Identity.Users.Add(archived);
        await fixture.Identity.SaveChangesAsync(cancellationToken);

        var result = await fixture.Service.RestoreUserAsync(
            archived.Id,
            new RestoreManagedUserRequest(Convert.ToBase64String(Guid.NewGuid().ToByteArray())),
            cancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserManagementErrors.ConcurrencyConflict.Code, result.Error.Code);
        var unchanged = await fixture.Identity.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(user => user.Id == archived.Id, cancellationToken);
        Assert.True(unchanged.IsDeleted);
    }

    private static Fixture CreateFixture()
    {
        var databaseName = Guid.NewGuid().ToString();
        var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName + "-identity")
            .Options);
        var application = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName + "-application")
            .Options);
        var currentUser = new CurrentUser();
        var sessions = new SessionValidator();
        var clock = new FixedTimeProvider();
        var userManager = new UserManager<ApplicationUser>(
            new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser, ApplicationRole, IdentityDbContext, Guid>(identity),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);
        var service = new UserManagementService(identity, application, userManager, currentUser, sessions, clock);
        return new Fixture(identity, application, userManager, service, sessions, clock.GetUtcNow());
    }

    private static CreateManagedUserRequest NewCreateRequest(string userName = "new-user", string? email = null, Guid? employeeId = null) =>
        new(userName, "TemporaryP@ss123", "مستخدم جديد", null, email, null, employeeId, null, null);

    private static ApplicationUser NewUser(string userName, bool isArchived, DateTimeOffset? deletedAtUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        DisplayNameAr = userName,
        DisplayNameEn = userName,
        Status = isArchived ? UserAccountStatus.Archived : UserAccountStatus.Active,
        IsDeleted = isArchived,
        DeletedAtUtc = deletedAtUtc,
        DeletedByUserId = isArchived ? Guid.NewGuid() : null,
        DeletionReason = isArchived ? "Archived for testing." : null,
        CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        SecurityStamp = Guid.NewGuid().ToString(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
        RowVersion = Guid.NewGuid().ToByteArray()
    };

    private sealed record Fixture(
        IdentityDbContext Identity,
        ApplicationDbContext Application,
        UserManager<ApplicationUser> UserManager,
        UserManagementService Service,
        SessionValidator Sessions,
        DateTimeOffset Now) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            UserManager.Dispose();
            await Identity.DisposeAsync();
            await Application.DisposeAsync();
        }
    }

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }

    private sealed class SessionValidator : IAuthenticationSessionValidator
    {
        public Guid? InvalidatedUserId { get; private set; }

        public Task<bool> IsValidAsync(Guid userId, Guid sessionId, long authorizationVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public void InvalidateSession(Guid userId, Guid sessionId, long authorizationVersion) { }

        public void InvalidateUser(Guid userId) => InvalidatedUserId = userId;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
    }
}
