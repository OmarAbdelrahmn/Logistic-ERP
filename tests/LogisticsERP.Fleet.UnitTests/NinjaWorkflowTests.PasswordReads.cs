using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Ninja;
using Microsoft.EntityFrameworkCore;
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
