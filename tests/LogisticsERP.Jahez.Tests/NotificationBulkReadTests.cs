using System.Reflection;
using System.Text.Json;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using LogisticsERP.Infrastructure.SystemServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed class NotificationBulkReadTests
{
    [Fact]
    public async Task ReadAllMarksEveryVisibleAuthorizedItemAcrossBatchesAndPreservesOtherStates()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = new CurrentUser();
        var clock = new FixedClock();
        await using var db = CreateDb(user, clock);
        await using var identity = CreateIdentity();
        var now = clock.GetUtcNow();
        var eligible = Enumerable.Range(0, 415).Select(i => Notification(user.UserId!.Value, now,
            i % 2 == 0 ? null : [PermissionKeys.Fleet.VehiclesRead])).ToArray();
        var alreadyRead = Notification(user.UserId!.Value, now);
        alreadyRead.ReadAtUtc = now.AddDays(-1);
        var excluded = new[]
        {
            Notification(Guid.NewGuid(), now),
            Notification(user.UserId.Value, now, [PermissionKeys.Operations.PlatformAssignmentsRead]),
            Notification(user.UserId.Value, now), Notification(user.UserId.Value, now),
            Notification(user.UserId.Value, now), Notification(user.UserId.Value, now)
        };
        excluded[2].VisibleAtUtc = now.AddMinutes(1);
        excluded[3].ExpiresAtUtc = now;
        excluded[4].ArchivedAtUtc = now.AddDays(-1);
        excluded[5].IsDeleted = true;
        db.AddRange(eligible);
        db.AddRange(excluded);
        db.Add(alreadyRead);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var initialAudits = await db.AuditEntries.CountAsync(x => x.EntityType == nameof(Notification), ct);
        var service = new NotificationService(db, identity, user, clock, new Permissions());

        var result = await service.ReadAllAsync(new(), ct);
        Assert.True(result.IsSuccess);
        Assert.Equal(415, result.Value!.MarkedCount);
        Assert.Equal(now, result.Value.ReadAtUtc);
        var rows = await db.Notifications.IgnoreQueryFilters().AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
        Assert.All(eligible, x => Assert.Equal(now, rows[x.Id].ReadAtUtc));
        Assert.All(excluded, x => Assert.Null(rows[x.Id].ReadAtUtc));
        Assert.Equal(now.AddDays(-1), rows[alreadyRead.Id].ReadAtUtc);
        Assert.All(eligible, x => {
            Assert.Null(rows[x.Id].AcknowledgedAtUtc);
            Assert.Null(rows[x.Id].ArchivedAtUtc);
            Assert.Equal(user.UserId, rows[x.Id].UpdatedByUserId);
        });
        Assert.Equal(initialAudits + 415, await db.AuditEntries.CountAsync(x => x.EntityType == nameof(Notification), ct));
        Assert.Equal(0, (await service.GetUnreadCountAsync(cancellationToken: ct)).Value);
        clock.Now = now.AddSeconds(1);
        var repeated = await service.ReadAllAsync(new(), ct);
        Assert.True(repeated.IsSuccess);
        Assert.Equal(0, repeated.Value!.MarkedCount);
        Assert.Equal(now, (await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == eligible[0].Id, ct)).ReadAtUtc);
    }

    [Fact]
    public async Task ExplicitFiltersExcludePersonalAndUnauthorizedAudiencesAndEmptyFilterChangesNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = new CurrentUser(); var clock = new FixedClock();
        await using var db = CreateDb(user, clock);
        await using var identity = CreateIdentity();
        var personal = Notification(user.UserId!.Value, clock.GetUtcNow());
        var allowed = Notification(user.UserId.Value, clock.GetUtcNow(), [PermissionKeys.Fleet.VehiclesRead]);
        var overlapping = Notification(user.UserId.Value, clock.GetUtcNow(),
            [PermissionKeys.Fleet.VehiclesRead, PermissionKeys.Operations.PlatformAssignmentsRead]);
        var denied = Notification(user.UserId.Value, clock.GetUtcNow(), [PermissionKeys.Operations.PlatformAssignmentsRead]);
        db.AddRange(personal, allowed, overlapping, denied);
        await db.SaveChangesAsync(ct);
        var service = new NotificationService(db, identity, user, clock, new Permissions());
        Assert.Equal(0, (await service.ReadAllAsync(new([]), ct)).Value!.MarkedCount);
        Assert.Equal(0, (await service.ReadAllAsync(new([PermissionKeys.Operations.PlatformAssignmentsRead]), ct)).Value!.MarkedCount);
        var result = await service.ReadAllAsync(new([PermissionKeys.Fleet.VehiclesRead, PermissionKeys.Operations.PlatformAssignmentsRead]), ct);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.MarkedCount);
        Assert.Equal([PermissionKeys.Fleet.VehiclesRead], result.Value.EffectivePermissions);
        var rows = await db.Notifications.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
        Assert.Null(rows[personal.Id].ReadAtUtc);
        Assert.Null(rows[denied.Id].ReadAtUtc);
        Assert.NotNull(rows[allowed.Id].ReadAtUtc);
        Assert.NotNull(rows[overlapping.Id].ReadAtUtc);
    }

    [Fact]
    public async Task InvalidFiltersAndMissingUserCannotMutateNotificationsAndEndpointRequiresReadPermission()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = new CurrentUser(); var clock = new FixedClock();
        await using var db = CreateDb(user, clock);
        await using var identity = CreateIdentity();
        var item = Notification(user.UserId!.Value, clock.GetUtcNow());
        db.Add(item); await db.SaveChangesAsync(ct);
        var service = new NotificationService(db, identity, user, clock, new Permissions());
        IReadOnlyList<string>[] filters = [["unknown.permission"], [" "], Enumerable.Repeat(PermissionKeys.Fleet.VehiclesRead, 257).ToArray()];
        foreach (var filter in filters)
        {
            var invalid = await service.ReadAllAsync(new(filter), ct);
            Assert.True(invalid.IsFailure);
            Assert.Equal("system.invalid_request", invalid.Error.Code);
        }
        user.UserId = null;
        Assert.True((await service.ReadAllAsync(new(), ct)).IsFailure);
        Assert.Null((await db.Notifications.AsNoTracking().SingleAsync(ct)).ReadAtUtc);

        var action = typeof(NotificationsController).GetMethod(nameof(NotificationsController.ReadAll))!;
        Assert.Equal("read-all", action.GetCustomAttribute<HttpPostAttribute>()!.Template);
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + PermissionKeys.Reporting.NotificationsRead,
            action.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
        user.UserId = item.RecipientUserId;
        var controller = new NotificationsController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var response = Assert.IsType<OkObjectResult>(await controller.ReadAll(new(), ct));
        var payload = Assert.IsType<NotificationReadAllResponse>(response.Value);
        Assert.Equal(1, payload.MarkedCount);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(payload, JsonSerializerOptions.Web));
        Assert.Equal(1, json.RootElement.GetProperty("markedCount").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("readAtUtc", out _));
        Assert.True(json.RootElement.TryGetProperty("effectivePermissions", out _));
    }

    private static ApplicationDbContext CreateDb(CurrentUser user, TimeProvider clock) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"NotificationsReadAll_{Guid.NewGuid():N}")
            .AddInterceptors(new ApplicationPersistenceInterceptor(user, clock)).Options, clock);
    private static IdentityDbContext CreateIdentity() => new(new DbContextOptionsBuilder<IdentityDbContext>()
        .UseInMemoryDatabase($"NotificationsReadAllIdentity_{Guid.NewGuid():N}").Options);
    private static Notification Notification(Guid recipient, DateTimeOffset now, string[]? audience = null) => new()
    {
        RecipientUserId = recipient, VisibleAtUtc = now.AddMinutes(-1), DeduplicationKey = Guid.NewGuid().ToString(),
        AudiencePermissionKeysJson = audience is null ? null : JsonSerializer.Serialize(audience)
    };
    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "notifications-read-all-test";
    }
    private sealed class FixedClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Permissions : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(
                permissionKey is PermissionKeys.Fleet.VehiclesRead or PermissionKeys.Reporting.NotificationsRead);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
