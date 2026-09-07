using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.SystemServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class AccidentNotificationTests
{
    [Fact]
    public async Task FleetAndOperationsReceivePersistentDeduplicatedNotificationsWithCompletePagingAndReadOwnership()
    {
        var ct = TestContext.Current.CancellationToken;
        var clock = new FixedTime();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).AddInterceptors(new RowVersions()).Options);
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options);
        await db.Database.EnsureCreatedAsync(ct);
        await identity.Database.EnsureCreatedAsync(ct);
        var fleetUser = new CurrentUser(); var operationsUser = new CurrentUser(); var deniedUser = new CurrentUser();
        identity.Users.AddRange(new ApplicationUser { Id = fleetUser.UserId!.Value, Status = UserAccountStatus.Active },
            new ApplicationUser { Id = operationsUser.UserId!.Value, Status = UserAccountStatus.Active },
            new ApplicationUser { Id = deniedUser.UserId!.Value, Status = UserAccountStatus.Active });
        await identity.SaveChangesAsync(ct);
        var permissions = new Permissions(fleetUser.UserId.Value, operationsUser.UserId.Value);
        var notifier = new AccidentNotificationService(db, identity, permissions, clock);
        var accident = new VehicleAccident { AccidentNumber = "ACC-NOTIFY", VehicleId = Guid.NewGuid() };
        var workflow = new VehicleAccidentCase { VehicleAccidentId = accident.Id, Stage = AccidentCaseStage.AwaitingInsurance, InsuranceDueAtUtc = clock.GetUtcNow().AddSeconds(-1) };
        db.AddRange(accident, workflow);
        await db.SaveChangesAsync(ct);
        await notifier.QueueAsync(accident.Id, accident.VehicleId, accident.AccidentNumber, "reported", "Reported", ct);
        await notifier.QueueAsync(accident.Id, accident.VehicleId, accident.AccidentNumber, "reported", "Reported", ct);
        await db.SaveChangesAsync(ct);
        await notifier.QueueAsync(accident.Id, accident.VehicleId, accident.AccidentNumber, "reported", "Reported", ct);
        await notifier.QueueAsync(accident.Id, accident.VehicleId, accident.AccidentNumber, "changed-1", "Changed", ct);
        await notifier.QueueAsync(accident.Id, accident.VehicleId, accident.AccidentNumber, "changed-2", "Changed", ct);
        await db.SaveChangesAsync(ct);
        Assert.Equal(3, await db.Notifications.CountAsync(x => x.RecipientUserId == fleetUser.UserId, ct));
        Assert.Equal(3, await db.Notifications.CountAsync(x => x.RecipientUserId == operationsUser.UserId, ct));
        Assert.Equal(0, await db.Notifications.CountAsync(x => x.RecipientUserId == deniedUser.UserId, ct));

        var service = new NotificationService(db, identity, fleetUser, clock, permissions);
        var all = new List<NotificationResponse>();
        string? cursor = null;
        do
        {
            var page = await service.GetMineAsync(false, 1, cursor, cancellationToken: ct);
            Assert.True(page.IsSuccess);
            all.AddRange(page.Value!.Items); cursor = page.Value.NextCursor;
        } while (cursor is not null);
        Assert.Equal(3, all.Select(x => x.Id).Distinct().Count());
        var read = await service.ChangeStateAsync(all[0].Id, new("read", all[0].RowVersion), ct);
        Assert.True(read.IsSuccess);
        Assert.NotNull(read.Value!.ReadAtUtc);
        Assert.Equal(2, (await service.GetUnreadCountAsync(cancellationToken: ct)).Value);
        Assert.Equal(3, (await service.GetMineAsync(false, 50, null, cancellationToken: ct)).Value!.Items.Count);
        var selected = await service.QueryAsync(new([PermissionKeys.Fleet.VehiclesRead]), ct);
        Assert.True(selected.IsSuccess);
        Assert.Equal(3, selected.Value!.Items.Count);
        Assert.Equal(2, selected.Value.UnreadCount);
        Assert.Equal([PermissionKeys.Fleet.VehiclesRead], selected.Value.EffectivePermissions);
        var spoofed = await service.QueryAsync(new([PermissionKeys.Operations.PlatformAssignmentsRead]), ct);
        Assert.True(spoofed.IsSuccess);
        Assert.Empty(spoofed.Value!.Items);
        Assert.Empty(spoofed.Value.EffectivePermissions);
        Assert.Equal(0, spoofed.Value.UnreadCount);
        Assert.Equal(0, (await service.GetUnreadCountAsync([PermissionKeys.Operations.PlatformAssignmentsRead], ct)).Value);
        Assert.Empty((await service.QueryAsync(new([]), ct)).Value!.Items);
        Assert.True((await service.QueryAsync(new(["unknown.permission"]), ct)).IsFailure);
        var otherService = new NotificationService(db, identity, operationsUser, clock, permissions);
        Assert.True((await otherService.ChangeStateAsync(all[0].Id, new("read", read.Value.RowVersion), ct)).IsFailure);

        await notifier.RunDueNotificationsAsync(ct);
        await notifier.RunDueNotificationsAsync(ct);
        Assert.Equal(2, await db.Notifications.CountAsync(x => x.EventType == "fleet.accident.deadline", ct));
        workflow.Stage = AccidentCaseStage.AwaitingSupplierTransfer; workflow.SupplierTransferDueAtUtc = clock.GetUtcNow().AddMinutes(-1);
        await db.SaveChangesAsync(ct);
        await notifier.RunDueNotificationsAsync(ct);
        Assert.Equal(4, await db.Notifications.CountAsync(x => x.EventType == "fleet.accident.deadline", ct));
        permissions.Revoked = true;
        Assert.Empty((await service.QueryAsync(new([PermissionKeys.Fleet.VehiclesRead]), ct)).Value!.Items);
        Assert.Equal(0, (await service.GetUnreadCountAsync(cancellationToken: ct)).Value);
        Assert.True((await service.ChangeStateAsync(all[0].Id, new("unread", read.Value.RowVersion), ct)).IsFailure);
        workflow.Stage = AccidentCaseStage.Completed;
        await db.SaveChangesAsync(ct);
        await notifier.RunDueNotificationsAsync(ct);
        Assert.Equal(4, await db.Notifications.CountAsync(x => x.EventType == "fleet.accident.deadline", ct));
    }

    [Fact]
    public void NotificationTieBreakerAndAccidentSchemaTranslateForSqlServerWithoutConnecting()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=SchemaOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var before = DateTimeOffset.UtcNow; var id = Guid.NewGuid();
        var sql = db.Notifications.Where(x => x.VisibleAtUtc < before || x.VisibleAtUtc == before && x.Id.CompareTo(id) < 0)
            .OrderByDescending(x => x.VisibleAtUtc).ThenByDescending(x => x.Id).Take(2).ToQueryString();
        Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
        var permissionSql = NotificationPermissionFilter.Apply(db.Notifications, [PermissionKeys.Fleet.VehiclesRead], includePersonal: false).ToQueryString();
        Assert.Contains("AudiencePermissionKeysJson", permissionSql, StringComparison.Ordinal);
        var ddl = db.Database.GenerateCreateScript();
        Assert.Contains("CK_AccidentCase_OpeningFee", ddl, StringComparison.Ordinal);
        Assert.Contains("CK_AccidentInstallment_Amount", ddl, StringComparison.Ordinal);
        Assert.Contains("IX_VehicleAccidentCases_VehicleAccidentId", ddl, StringComparison.Ordinal);
        Assert.Contains("rowversion", ddl, StringComparison.Ordinal);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid(); public Guid? SessionId => null;
        public long? AuthorizationVersion => 1; public string? CorrelationId => null;
    }
    private sealed class FixedTime : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero); }
    private sealed class Permissions(Guid fleetUser, Guid operationsUser) : IPermissionChecker
    {
        public bool Revoked { get; set; }
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(!Revoked && (userId == fleetUser && permissionKey == PermissionKeys.Fleet.VehiclesRead || userId == operationsUser && permissionKey == PermissionKeys.Operations.PlatformAssignmentsRead));
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    private sealed class RowVersions : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries<AuditableEntity>().Where(x => x.State is EntityState.Added or EntityState.Modified))
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            return ValueTask.FromResult(result);
        }
    }
}
