using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Ninja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NinjaAccountReadsReturnActualCurrentPasswordIncludingCooldownAndRotation(bool list)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12),
            "0501111111", "initial-password", RiderType: "InternalCar", PaymentModel: "Salary"), ct));
        var initial = await ReadPasswordAccount(f.Service, f.Account.Id, list, ct);
        Assert.True(initial.HasPassword);
        Assert.Equal("initial-password", initial.Password);
        Assert.Equal(assigned.Credentials.PasswordChangeAllowedAtUtc, initial.PasswordChangeAllowedAtUtc);
        var ignored = Value(await f.Service.UpdateCredentialsAsync(f.Account.Id,
            new("ignored-password", null, "Cooldown attempt", initial.RowVersion), ct));
        Assert.False(ignored.PasswordChanged);
        Assert.Equal("initial-password", (await ReadPasswordAccount(f.Service, f.Account.Id, list, ct)).Password);
        f.Clock.Now = initial.PasswordChangeAllowedAtUtc!.Value;
        var rotated = Value(await f.Service.UpdateCredentialsAsync(f.Account.Id,
            new("new-current-password", null, "Rotate", ignored.RowVersion), ct));
        Assert.True(rotated.PasswordChanged);
        f.Db.ChangeTracker.Clear();
        var current = await ReadPasswordAccount(f.Service, f.Account.Id, list, ct);
        Assert.True(current.HasPassword);
        Assert.Equal("new-current-password", current.Password);
        Assert.Equal(rotated.PasswordChangeAllowedAtUtc, current.PasswordChangeAllowedAtUtc);
        Assert.Equal(2, await f.Db.PlatformAccountCredentialVersions.CountAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NinjaAccountReadsWithoutSavedCredentialReturnNullPassword(bool list)
    {
        await using var f = await Fixture.Create();
        var account = await ReadPasswordAccount(f.Service, f.Account.Id, list, TestContext.Current.CancellationToken);
        Assert.False(account.HasPassword);
        Assert.Null(account.Password);
        Assert.Null(account.PasswordChangeAllowedAtUtc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NinjaReadersWithoutCredentialReadPermissionDoNotDecryptOrExposePassword(bool list)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        // Invalid encrypted material makes accidental decryption fail this test.
        f.Db.PlatformAccountCredentialVersions.Add(new PlatformAccountCredentialVersion
        {
            PlatformRiderAccountId = f.Account.Id, KeyVersion = 1, RotatedAtUtc = f.Clock.Now,
            RotatedByUserId = f.User.UserId!.Value, RotationReason = "Test unreadable credential"
        });
        await f.Db.SaveChangesAsync(ct);
        f.Checker.Granted = [PermissionKeys.Ninja.Read];
        var account = await ReadPasswordAccount(f.Service, f.Account.Id, list, ct);
        Assert.True(account.HasPassword);
        Assert.Null(account.Password);
        Assert.Equal(f.Clock.Now.AddHours(72), account.PasswordChangeAllowedAtUtc);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task NinjaPasswordReadPermissionRequiresTheAccountsPlatformScope(bool list, bool matchingScope)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12),
            "0501111111", "stored-password", RiderType: "ExternalMotorcycle", PaymentModel: "PayPerOrder"), ct));
        var checker = new CredentialScopeChecker(matchingScope ? f.Account.ClientPlatformId : Guid.NewGuid());
        var service = new NinjaService(f.Db, f.User, checker, f.Clock, f.Protector, f.Simple);
        var account = await ReadPasswordAccount(service, f.Account.Id, list, ct);
        Assert.True(account.HasPassword);
        Assert.Equal(matchingScope ? "stored-password" : null, account.Password);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CredentialReadPermissionAloneCannotReadNinjaAccounts(bool list)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        f.Checker.Granted = [PermissionKeys.Operations.PlatformCredentialsRead];
        var error = list ? (await f.Service.GetAccountsAsync(1, 50, ct)).Error
            : (await f.Service.GetAccountAsync(f.Account.Id, ct)).Error;
        Assert.Equal("ninja.forbidden", error.Code);
    }

    [Fact]
    public async Task AccountStatusWriteDoesNotReturnPasswordEvenWithCredentialReadPermission()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12),
            "0501111111", "stored-password", RiderType: "InternalCar", PaymentModel: "Salary"), ct));
        var changed = Value(await f.Service.SetAccountStatusAsync(f.Account.Id,
            new("Suspended", assigned.Credentials.RowVersion, "Review"), ct));
        Assert.True(changed.HasPassword);
        Assert.Null(changed.Password);
        Assert.Equal("stored-password", Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).Password);
    }

    [Fact]
    public async Task AccountPasswordReadsRespectPagingAndNeverReturnAnotherAccountsCredential()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var second = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId,
            OperatingCityId = f.Account.OperatingCityId, Code = "NINJA-2", ExternalAccountId = "999999" };
        f.Db.Add(second);
        var firstSecret = f.Protector.Protect("first-account-password");
        var secondSecret = f.Protector.Protect("second-account-password");
        foreach (var (id, secret) in new[] { (f.Account.Id, firstSecret), (second.Id, secondSecret) })
            f.Db.PlatformAccountCredentialVersions.Add(new PlatformAccountCredentialVersion { PlatformRiderAccountId = id,
                Ciphertext = secret.Ciphertext, Nonce = secret.Nonce, AuthenticationTag = secret.AuthenticationTag,
                KeyVersion = 1, RotatedAtUtc = f.Clock.Now, RotatedByUserId = f.User.UserId!.Value, RotationReason = "Test" });
        await f.Db.SaveChangesAsync(ct);
        var firstPage = Assert.Single(Value(await f.Service.GetAccountsAsync(1, 1, ct)).Items);
        var secondPage = Assert.Single(Value(await f.Service.GetAccountsAsync(2, 1, ct)).Items);
        Assert.Equal(f.Account.Id, firstPage.AccountId);
        Assert.Equal("first-account-password", firstPage.Password);
        Assert.Equal(second.Id, secondPage.AccountId);
        Assert.Equal("second-account-password", secondPage.Password);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task UnreadableCredentialKeepsAccountReadableWithoutChangingStoredPassword(bool list, bool corrupt)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var legacyProtector = new PlatformCredentialProtector(() => Enumerable.Repeat((byte)7, 32).ToArray());
        var secret = (corrupt ? f.Protector : legacyProtector).Protect("legacy-password");
        if (corrupt) secret.AuthenticationTag[0] ^= 1;
        var stored = new PlatformAccountCredentialVersion { PlatformRiderAccountId = f.Account.Id,
            Ciphertext = secret.Ciphertext, Nonce = secret.Nonce, AuthenticationTag = secret.AuthenticationTag,
            KeyVersion = 1, RotatedAtUtc = f.Clock.Now, RotatedByUserId = f.User.UserId!.Value, RotationReason = "Legacy" };
        f.Db.PlatformAccountCredentialVersions.Add(stored);
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
        var logger = new PasswordReadLogger();
        var service = new NinjaService(f.Db, f.User, f.Checker, f.Clock, f.Protector, f.Simple, logger);

        var account = await ReadPasswordAccount(service, f.Account.Id, list, ct);

        Assert.Equal(f.Account.Id, account.AccountId);
        Assert.True(account.HasPassword);
        Assert.Null(account.Password);
        Assert.Equal(f.Clock.Now.AddHours(72), account.PasswordChangeAllowedAtUtc);
        Assert.Equal(Convert.ToBase64String(f.Account.RowVersion), account.RowVersion);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(f.Account.Id.ToString(), entry.Message);
        Assert.DoesNotContain("legacy-password", entry.Message);
        Assert.Null(entry.Exception);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        var retained = await f.Db.PlatformAccountCredentialVersions.AsNoTracking().SingleAsync(ct);
        Assert.Equal(secret.Ciphertext, retained.Ciphertext);
        Assert.Equal(secret.Nonce, retained.Nonce);
        Assert.Equal(secret.AuthenticationTag, retained.AuthenticationTag);
        Assert.Equal(1, retained.KeyVersion);
    }

    [Fact]
    public async Task UnreadableAccountDoesNotHideReadablePasswordsInTheSamePage()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var second = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId,
            OperatingCityId = f.Account.OperatingCityId, Code = "NINJA-2", ExternalAccountId = "999999" };
        f.Db.Add(second);
        var legacyProtector = new PlatformCredentialProtector(() => Enumerable.Repeat((byte)7, 32).ToArray());
        foreach (var (id, protector) in new[] { (f.Account.Id, legacyProtector), (second.Id, f.Protector) })
        {
            var secret = protector.Protect("readable-password");
            f.Db.PlatformAccountCredentialVersions.Add(new PlatformAccountCredentialVersion { PlatformRiderAccountId = id,
                Ciphertext = secret.Ciphertext, Nonce = secret.Nonce, AuthenticationTag = secret.AuthenticationTag,
                KeyVersion = 1, RotatedAtUtc = f.Clock.Now, RotatedByUserId = f.User.UserId!.Value, RotationReason = "Test" });
        }
        await f.Db.SaveChangesAsync(ct);

        var accounts = Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items;

        Assert.Null(Assert.Single(accounts, a => a.AccountId == f.Account.Id).Password);
        Assert.Equal("readable-password", Assert.Single(accounts, a => a.AccountId == second.Id).Password);
    }

    [Fact]
    public async Task CredentialConfigurationErrorsStillSurfaceForDiagnosis()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var secret = f.Protector.Protect("stored-password");
        f.Db.PlatformAccountCredentialVersions.Add(new PlatformAccountCredentialVersion { PlatformRiderAccountId = f.Account.Id,
            Ciphertext = secret.Ciphertext, Nonce = secret.Nonce, AuthenticationTag = secret.AuthenticationTag,
            KeyVersion = 1, RotatedAtUtc = f.Clock.Now, RotatedByUserId = f.User.UserId!.Value, RotationReason = "Test" });
        await f.Db.SaveChangesAsync(ct);
        var protector = new PlatformCredentialProtector(() => throw new InvalidOperationException("Missing encryption configuration"));
        var service = new NinjaService(f.Db, f.User, f.Checker, f.Clock, protector, f.Simple);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAccountsAsync(1, 50, ct));
    }

    private sealed class PasswordReadLogger : ILogger<NinjaService>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }

    private static async Task<NinjaAccountResponse> ReadPasswordAccount(NinjaService service, Guid accountId, bool list, CancellationToken ct) =>
        list ? Assert.Single(Value(await service.GetAccountsAsync(1, 50, ct)).Items) : Value(await service.GetAccountAsync(accountId, ct));

    private sealed class CredentialScopeChecker(Guid passwordPlatformId) : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(
                permissionKey == PermissionKeys.Ninja.Read || permissionKey == PermissionKeys.Operations.PlatformCredentialsRead
                    && scope is { Type: AccessScopeType.ClientPlatform } && scope.TargetId == passwordPlatformId);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
