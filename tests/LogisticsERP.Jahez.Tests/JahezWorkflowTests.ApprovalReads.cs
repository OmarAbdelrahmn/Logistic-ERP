using System.Reflection;
using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Theory]
    [InlineData(JahezApprovalStatus.Pending)]
    [InlineData(JahezApprovalStatus.Approved)]
    [InlineData(JahezApprovalStatus.Rejected)]
    public async Task V2ApprovalReadsIncludeOriginalRiderAssignmentAndOperation(JahezApprovalStatus status)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("approval-source");
        var rider = await f.Db.RiderProfiles.SingleAsync(x => x.Id == f.RiderId, ct);
        var employee = await f.Db.Employees.SingleAsync(x => x.Id == rider.EmployeeId, ct);
        employee.IqamaNo = "2000000001";
        employee.FullNameEn = "Original Rider";
        employee.PrimaryPhone = "0500000001";
        var owner = await f.Db.Employees.SingleAsync(x => x.Id == account.RegisteredEmployeeId, ct);
        owner.IqamaNo = "2000000002";
        await f.Db.SaveChangesAsync(ct);
        var handover = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var operation = Success(await f.Service.CreateOperationAsync("draft",
            Settle(handover.Id, 100, JahezOperationMode.SettleAndWithdraw, null, true), ct));
        var created = Success(await f.Service.CreateWorkflowRequestAsync("request",
            new(handover.Id, JahezApprovalKind.ResetAccount, "ترحيل", OperationId: operation.Id), ct));
        Assert.Equal(new JahezSettlementAmounts(100, 75, 175), created.Amounts);
        if (status != JahezApprovalStatus.Pending)
        {
            f.User.UserId = Guid.NewGuid();
            Success(await f.Service.DecideWorkflowRequestAsync("decision", created.Request.Id,
                new(status == JahezApprovalStatus.Approved, "قرار المدير"), ct));
        }
        if (status == JahezApprovalStatus.Approved)
        {
            f.Clock.UtcNow = At(10, 23).ToUniversalTime();
            var next = Success(await f.Service.HandoverAsync("next", new(account.Id, f.OtherRiderId, At(7), "استلام آخر", 0), ct));
            Assert.NotEqual(handover.AssignmentId, next.AssignmentId);
        }
        f.Db.ChangeTracker.Clear();

        var row = Assert.Single(Success(await f.Service.RequestsV2Async(1, 50, ct)).Items);
        var detail = Success(await f.Service.RequestV2Async(created.Request.Id, ct));
        Assert.Equal(JsonSerializer.Serialize(row), JsonSerializer.Serialize(detail.Request));
        Assert.Equal(status, row.Status);
        Assert.Equal(created.Request.Id, row.Id);
        Assert.Equal(employee.Id, row.UserRider!.EmployeeId);
        Assert.Equal(f.RiderId, row.UserRider.RiderProfileId);
        Assert.Equal(employee.FullNameAr, row.UserRider.NameAr);
        Assert.Equal("Original Rider", row.UserRider.NameEn);
        Assert.Equal("2000000001", row.UserRider.IqamaNo);
        Assert.Equal("0500000001", row.UserRider.Mobile);
        Assert.NotEqual(owner.Id, row.UserRider.EmployeeId);
        Assert.Equal(handover.AssignmentId, row.Assignment!.AssignmentId);
        Assert.Equal(handover.Id, row.Assignment.HandoverId);
        Assert.Equal(f.RiderId, row.Assignment.RiderProfileId);
        Assert.Equal(handover.StartedAtUtc, row.Assignment.StartedAtUtc);
        Assert.Equal(new DateOnly(2026, 10, 1), row.Assignment.EffectiveFrom);
        Assert.Equal(handover.CommissionStartsOn, row.Assignment.CommissionStartsOn);
        Assert.Equal(status == JahezApprovalStatus.Approved ? "Ended" : "Active", row.Assignment.Status);
        Assert.Equal(account.Id, row.Account!.AccountId);
        Assert.Equal(account.ExternalAccountId, row.Account.ExternalAccountId);
        Assert.Equal(account.Code, row.Account.Code);
        Assert.Equal(account.SponsorId, row.Account.SponsorId);
        Assert.NotNull(row.Account.SponsorNameAr);
        Assert.NotNull(row.Account.CityNameAr);
        Assert.Null(row.TargetAccount);
        Assert.Equal(operation.Id, row.Operation!.Id);
        Assert.Equal(handover.Id, row.Operation.HandoverId);
        Assert.Equal(100, row.Operation.Net);
        Assert.Equal(75, row.Operation.Commission);
        Assert.Equal(175, row.Operation.Remaining);
        Assert.Equal(created.Amounts, row.Amounts);
        Assert.Equal(operation.CalculationJson, row.Operation.CalculationJson);
        Assert.Equal(status switch
        {
            JahezApprovalStatus.Pending => JahezOperationStatus.UnderReview,
            JahezApprovalStatus.Approved => JahezOperationStatus.Executed,
            _ => JahezOperationStatus.Ready
        }, row.Operation.Status);
        if (status == JahezApprovalStatus.Pending) Assert.Empty(detail.Decisions);
        else
        {
            var decision = Assert.Single(detail.Decisions);
            Assert.Equal(status, decision.Status);
            Assert.Equal(f.User.UserId, decision.ActorUserId);
            Assert.Equal("قرار المدير", decision.Reason);
            Assert.NotNull(decision.DecisionJson);
        }
        // All old scalar/audit JSON fields stay at their original paths and values.
        var stored = await f.Db.JahezApprovalRequests.AsNoTracking().SingleAsync(ct);
        using var original = JsonSerializer.SerializeToDocument(stored, JsonSerializerOptions.Web);
        using var enriched = JsonSerializer.SerializeToDocument(row, JsonSerializerOptions.Web);
        foreach (var property in original.RootElement.EnumerateObject())
            Assert.Equal(property.Value.GetRawText(), enriched.RootElement.GetProperty(property.Name).GetRawText());
        Assert.Empty(f.Db.ChangeTracker.Entries());

        (await f.Db.Employees.SingleAsync(x => x.Id == employee.Id, ct)).IsDeleted = true;
        (await f.Db.RiderProfiles.SingleAsync(x => x.Id == f.RiderId, ct)).IsDeleted = true;
        (await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == handover.Id, ct)).IsDeleted = true;
        (await f.Db.RiderClientAssignments.SingleAsync(x => x.Id == handover.AssignmentId, ct)).IsDeleted = true;
        (await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct)).IsDeleted = true;
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
        var historical = Success(await f.Service.RequestV2Async(row.Id, ct));
        Assert.Equal("2000000001", historical.Request.UserRider!.IqamaNo);
        Assert.Equal(handover.AssignmentId, historical.Request.Assignment!.AssignmentId);
        Assert.Equal(account.ExternalAccountId, historical.Request.Account!.ExternalAccountId);
        Assert.Single(Success(await f.Service.RequestsV2Async(1, 50, ct)).Items);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task V2ApprovalReadsIncludeTargetAccountAndTypeSpecificProposals()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("approval-old");
        var target = await f.Account("approval-target");
        var handover = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var fee = Success(await f.Service.CreateWorkflowRequestAsync("fee",
            new(handover.Id, JahezApprovalKind.FeeException, "إعفاء", WaiverAmount: 50), ct));
        var percentage = Success(await f.Service.CreateWorkflowRequestAsync("percentage",
            new(handover.Id, JahezApprovalKind.PercentageCommission, "نسبة", FromDate: new(2026, 10, 2), ToDate: new(2026, 10, 6)), ct));
        var debt = Success(await f.Service.CreateWorkflowRequestAsync("debt",
            new(handover.Id, JahezApprovalKind.Adjustment, "مديونية", AdjustmentTarget: JahezAdjustmentTarget.AdministrativeDebt,
                DebtAdjustment: 123), ct));
        var change = Success(await f.Service.CreateWorkflowRequestAsync("switch",
            new(handover.Id, JahezApprovalKind.FreeSwitch, "تبديل", TargetAccountId: target.Id,
                WaiverAmount: 100, EffectiveAtUtc: At(6, 12)), ct));
        var operation = Success(await f.Service.CreateOperationAsync("draft", Settle(handover.Id, 100), ct));
        var adjustment = Success(await f.Service.CreateWorkflowRequestAsync("adjustment",
            new(handover.Id, JahezApprovalKind.Adjustment, "تصحيح تصفية", OperationId: operation.Id,
                AdjustmentTarget: JahezAdjustmentTarget.Settlement, ProposedNet: -120, ProposedCommission: 50), ct));
        f.Db.ChangeTracker.Clear();

        var rows = Success(await f.Service.RequestsV2Async(1, 50, ct)).Items.ToDictionary(x => x.Id);
        Assert.Equal(5, rows.Count);
        Assert.Equal(50, rows[fee.Request.Id].WaiverAmount);
        Assert.Equal(new DateOnly(2026, 10, 2), rows[percentage.Request.Id].FromDate);
        Assert.Equal(new DateOnly(2026, 10, 6), rows[percentage.Request.Id].ToDate);
        Assert.Equal(123, rows[debt.Request.Id].DebtAdjustment);
        Assert.Equal(target.Id, rows[change.Request.Id].TargetAccount!.AccountId);
        Assert.Equal(target.ExternalAccountId, rows[change.Request.Id].TargetAccount!.ExternalAccountId);
        Assert.Equal(account.Id, rows[change.Request.Id].Account!.AccountId);
        Assert.Equal(100, rows[change.Request.Id].WaiverAmount);
        Assert.Equal(At(6, 12), rows[change.Request.Id].EffectiveAtUtc);
        Assert.Equal(-120, rows[adjustment.Request.Id].ProposedNet);
        Assert.Equal(50, rows[adjustment.Request.Id].ProposedCommission);
        Assert.Equal(100, rows[adjustment.Request.Id].Operation!.Net);
        foreach (var row in rows.Values)
        {
            Assert.Equal(f.RiderId, row.UserRider!.RiderProfileId);
            if (row.OperationId is null) Assert.Null(row.Operation);
            if (row.TargetAccountId is null) Assert.Null(row.TargetAccount);
            var detail = Success(await f.Service.RequestV2Async(row.Id, ct));
            Assert.Equal(JsonSerializer.Serialize(row), JsonSerializer.Serialize(detail.Request));
        }
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task V2ApprovalReadsPreserveAllStatusesPaginationAndMissingReferences()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var requests = new List<JahezApprovalRequest>();
        foreach (var status in new[] { JahezApprovalStatus.Pending, JahezApprovalStatus.Approved, JahezApprovalStatus.Rejected, JahezApprovalStatus.Cancelled })
        {
            f.Clock.UtcNow = f.Clock.UtcNow.AddMinutes(1);
            // Legacy references can be missing; left joins must retain the request itself.
            var request = new JahezApprovalRequest { HandoverId = Guid.NewGuid(), OperationId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(), Kind = JahezApprovalKind.FeeException, Status = status, Reason = "قديم" };
            f.Db.Add(request);
            await f.Db.SaveChangesAsync(ct);
            requests.Add(request);
        }
        var deleted = new JahezApprovalRequest { HandoverId = Guid.NewGuid(), IsDeleted = true };
        f.Db.Add(deleted);
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();

        var all = Success(await f.Service.RequestsV2Async(1, 50, ct));
        Assert.Equal(requests.AsEnumerable().Reverse().Select(x => x.Id), all.Items.Select(x => x.Id));
        for (var page = 1; page <= requests.Count; page++)
        {
            var result = Success(await f.Service.RequestsV2Async(page, 1, ct));
            Assert.Equal(page, result.Page);
            Assert.Equal(1, result.PageSize);
            var row = Assert.Single(result.Items);
            Assert.Equal(requests[^page].Id, row.Id);
            Assert.Null(row.UserRider);
            Assert.Null(row.Assignment);
            Assert.Null(row.Account);
            Assert.Null(row.TargetAccount);
            Assert.Null(row.Operation);
            Assert.Equal(row.Id, Success(await f.Service.RequestV2Async(row.Id, ct)).Request.Id);
        }
        Assert.Empty(Success(await f.Service.RequestsV2Async(5, 1, ct)).Items);
        Assert.Equal(JahezErrors.NotFound, (await f.Service.RequestV2Async(deleted.Id, ct)).Error);
        Assert.Equal(JahezErrors.NotFound, (await f.Service.RequestV2Async(Guid.NewGuid(), ct)).Error);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task V2ApprovalReadsRequireJahezScopeAndValidatePagination()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        f.Permissions.Denied = PermissionKeys.Jahez.Read;
        Assert.Equal(JahezErrors.Forbidden, (await f.Service.RequestsV2Async(1, 50, ct)).Error);
        Assert.Equal(JahezErrors.Forbidden, (await f.Service.RequestV2Async(Guid.NewGuid(), ct)).Error);
        f.Permissions.Denied = null;
        f.Permissions.RequiredScope = Guid.NewGuid();
        Assert.Equal(JahezErrors.Forbidden, (await f.Service.RequestsV2Async(1, 50, ct)).Error);
        Assert.Equal(JahezErrors.Forbidden, (await f.Service.RequestV2Async(Guid.NewGuid(), ct)).Error);
        f.Permissions.RequiredScope = f.PlatformId;
        foreach (var (page, size) in new[] { (0, 50), (1, 0), (1, 101), (100001, 50) })
            Assert.Equal("jahez.invalid_request", (await f.Service.RequestsV2Async(page, size, ct)).Error.Code);
        Success(await f.Service.RequestsV2Async(1, 100, ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void V2ApprovalReadsTranslateToSqlForListAndDetail(bool detail)
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedApprovalReads;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        var requests = db.JahezApprovalRequests.Where(x => !x.IsDeleted);
        requests = detail ? requests.Where(x => x.Id == Guid.Empty)
            : requests.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip(10).Take(10);
        var query = (IQueryable<JahezApprovalRequestResponse>)typeof(JahezWorkflowService)
            .GetMethod("WorkflowApprovalRequests", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, [requests])!;
        var sql = query.ToQueryString();
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("IqamaNo", sql);
        Assert.Contains("RiderClientAssignmentId", sql);
        Assert.Contains("ExternalAccountId", sql);
        Assert.Contains("RegistryNameAr", sql);
        Assert.Contains("CalculationJson", sql);
        Assert.Contains("[IsDeleted] = CAST(0 AS bit)", sql);
        if (!detail)
        {
            Assert.Contains("OFFSET", sql);
            Assert.Contains("FETCH NEXT", sql);
            Assert.True(sql.IndexOf("FETCH NEXT", StringComparison.Ordinal) < sql.IndexOf("LEFT JOIN", StringComparison.Ordinal));
        }
    }
}
