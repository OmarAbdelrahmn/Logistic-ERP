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
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2TransferredDebtsIdentifyOriginalRiderAndAssignmentAfterReassignment(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("debt-source");
        var rider = await f.Db.RiderProfiles.SingleAsync(x => x.Id == f.RiderId, ct);
        var employee = await f.Db.Employees.SingleAsync(x => x.Id == rider.EmployeeId, ct);
        var owner = await f.Db.Employees.SingleAsync(x => x.Id == account.RegisteredEmployeeId, ct);
        employee.FullNameEn = "Original Rider";
        employee.IqamaNo = "2000000001";
        employee.PrimaryPhone = "0500000001";
        owner.IqamaNo = "2000000002";
        await f.Db.SaveChangesAsync(ct);
        var handover = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var operation = Success(await f.Service.CreateOperationAsync("draft",
            Settle(handover.Id, 100, JahezOperationMode.SettleAndWithdraw, null, true), ct));
        var request = Success(await f.Service.CreateWorkflowRequestAsync("request",
            new(handover.Id, JahezApprovalKind.ResetAccount, "ترحيل", OperationId: operation.Id), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve", request.Request.Id, new(true, "اعتماد"), ct));
        var debt = await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(ct);

        f.Clock.UtcNow = At(10, 23).ToUniversalTime();
        var next = Success(await f.Service.HandoverAsync("next", new(account.Id, f.OtherRiderId, At(7), "استلام آخر", 0), ct));
        f.Db.ChangeTracker.Clear();
        var row = Assert.Single(Success(await f.Service.TransferredDebtsAsync(1, 50, ct)).Items);
        Assert.Equal(debt.Id, row.Id);
        Assert.Equal(debt.Amount, row.Amount);
        Assert.Equal(debt.CreatedAtUtc, row.CreatedAtUtc);
        Assert.Equal(debt.CreatedByUserId, row.CreatedByUserId);
        Assert.Equal(debt.RowVersion, row.RowVersion);
        Assert.Null(row.PaidAtUtc);
        Assert.Equal(employee.Id, row.UserRider!.EmployeeId);
        Assert.Equal(f.RiderId, row.UserRider.RiderProfileId);
        Assert.Equal(employee.FullNameAr, row.UserRider.NameAr);
        Assert.Equal("Original Rider", row.UserRider.NameEn);
        Assert.Equal("2000000001", row.UserRider.IqamaNo);
        Assert.Equal("0500000001", row.UserRider.Mobile);
        Assert.Equal(employee.SponsorId, row.UserRider.SponsorId);
        Assert.NotEqual(owner.Id, row.UserRider.EmployeeId);
        Assert.Equal(handover.Id, row.Assignment!.HandoverId);
        Assert.Equal(handover.AssignmentId, row.Assignment.AssignmentId);
        Assert.NotEqual(next.AssignmentId, row.Assignment.AssignmentId);
        Assert.Equal(account.Id, row.Assignment.AccountId);
        Assert.Equal(account.ExternalAccountId, row.Assignment.ExternalAccountId);
        Assert.Equal(f.RiderId, row.Assignment.RiderProfileId);
        Assert.Equal(At(1).ToUniversalTime(), row.Assignment.StartedAtUtc);
        Assert.Equal(operation.EffectiveAtUtc, row.Assignment.EndedAtUtc);
        Assert.Equal(new DateOnly(2026, 10, 1), row.Assignment.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 10, 6), row.Assignment.EffectiveTo);
        Assert.Equal("Ended", row.Assignment.Status);
        Assert.Equal("استلام", row.Assignment.StartReason);
        Assert.Equal(operation.Reason, row.Assignment.EndReason);
        Assert.Equal(operation.Id, row.SourceOperation!.Id);
        Assert.Equal(operation.Mode, row.SourceOperation.Mode);
        Assert.Equal(operation.ThroughDate, row.SourceOperation.ThroughDate);
        Assert.Equal(operation.EffectiveAtUtc, row.SourceOperation.EffectiveAtUtc);
        Assert.Equal(100, row.SourceOperation.Net);
        Assert.Equal(75, row.SourceOperation.Commission);
        Assert.Equal(debt.Amount, row.SourceOperation.Remaining);
        Assert.Equal(operation.Reason, row.SourceOperation.Reason);
        Assert.Equal(request.Request.Id, row.SourceOperation.ApprovedRequestId);
        Assert.NotNull(row.SourceOperation.ExecutedAtUtc);
        using var json = JsonSerializer.SerializeToDocument(row, JsonSerializerOptions.Web);
        Assert.Equal("2000000001", json.RootElement.GetProperty("userRider").GetProperty("iqamaNo").GetString());
        Assert.Equal(Convert.ToBase64String(debt.RowVersion), json.RootElement.GetProperty("rowVersion").GetString());
        Assert.Empty(f.Db.ChangeTracker.Entries());

        Success(await f.Service.RepayTransferredDebtAsync("repay", debt.Id,
            new(JahezMoneyFund.Cashbox, "debt-receipt", "سداد كامل"), ct));
        var paid = Assert.Single(Success(await f.Service.TransferredDebtsAsync(1, 50, ct)).Items);
        Assert.NotNull(paid.PaidAtUtc);
        Assert.Equal(f.User.UserId, paid.PaidByUserId);

        // Archived historical references must remain identifiable in the debt history.
        (await f.Db.Employees.SingleAsync(x => x.Id == employee.Id, ct)).IsDeleted = true;
        (await f.Db.RiderProfiles.SingleAsync(x => x.Id == f.RiderId, ct)).IsDeleted = true;
        (await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == handover.Id, ct)).IsDeleted = true;
        (await f.Db.RiderClientAssignments.SingleAsync(x => x.Id == handover.AssignmentId, ct)).IsDeleted = true;
        (await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct)).IsDeleted = true;
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
        var historical = Assert.Single(Success(await f.Service.TransferredDebtsAsync(1, 50, ct)).Items);
        Assert.Equal("2000000001", historical.UserRider!.IqamaNo);
        Assert.Equal(handover.AssignmentId, historical.Assignment!.AssignmentId);
        Assert.Equal(account.ExternalAccountId, historical.Assignment.ExternalAccountId);
        Assert.NotNull(historical.PaidAtUtc);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task V2TransferredDebtsPreservePaginationAndRowsWithMissingHistoricalReferences()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        // InMemory permits missing legacy references; these debts must not disappear through inner joins.
        var older = new JahezTransferredDebt { OperationId = Guid.NewGuid(), RiderProfileId = f.RiderId, Amount = 10 };
        f.Db.Add(older);
        await f.Db.SaveChangesAsync(ct);
        f.Clock.UtcNow = At(7).ToUniversalTime();
        var newer = new JahezTransferredDebt { OperationId = Guid.NewGuid(), RiderProfileId = Guid.NewGuid(), Amount = 20 };
        f.Db.Add(newer);
        f.Db.Add(new JahezTransferredDebt { OperationId = Guid.NewGuid(), RiderProfileId = f.RiderId, Amount = 30, IsDeleted = true });
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();

        var first = Success(await f.Service.TransferredDebtsAsync(1, 1, ct));
        Assert.Equal(1, first.Page);
        Assert.Equal(1, first.PageSize);
        var missing = Assert.Single(first.Items);
        Assert.Equal(newer.Id, missing.Id);
        Assert.Null(missing.UserRider);
        Assert.Null(missing.Assignment);
        Assert.Null(missing.SourceOperation);
        var second = Assert.Single(Success(await f.Service.TransferredDebtsAsync(2, 1, ct)).Items);
        Assert.Equal(older.Id, second.Id);
        Assert.Equal(f.RiderId, second.UserRider!.RiderProfileId);
        Assert.Null(second.UserRider.IqamaNo);
        Assert.Null(second.Assignment);
        Assert.Null(second.SourceOperation);
        Assert.Empty(Success(await f.Service.TransferredDebtsAsync(3, 1, ct)).Items);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task V2TransferredDebtsRequireJahezReadScopeAndValidatePageBounds()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        f.Permissions.Denied = PermissionKeys.Jahez.Read;
        Assert.Equal(JahezErrors.Forbidden, (await f.Service.TransferredDebtsAsync(1, 50, ct)).Error);
        f.Permissions.Denied = null;
        f.Permissions.RequiredScope = Guid.NewGuid();
        Assert.Equal(JahezErrors.Forbidden, (await f.Service.TransferredDebtsAsync(1, 50, ct)).Error);
        f.Permissions.RequiredScope = f.PlatformId;
        Success(await f.Service.TransferredDebtsAsync(1, 100, ct));
        foreach (var (page, pageSize) in new[] { (0, 50), (1, 0), (1, 101), (100001, 50) })
            Assert.Equal("jahez.invalid_request", (await f.Service.TransferredDebtsAsync(page, pageSize, ct)).Error.Code);
    }

    [Fact]
    public void V2TransferredDebtsQueryTranslatesToSqlWithHistoricalJoinsAndBoundedPagination()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedTransferredDebts;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        var query = (IQueryable<JahezTransferredDebtResponse>)typeof(JahezWorkflowService)
            .GetMethod("WorkflowTransferredDebts", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, [2, 10, null])!;
        var sql = query.ToQueryString();
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("IqamaNo", sql);
        Assert.Contains("PrimaryPhone", sql);
        Assert.Contains("RiderClientAssignmentId", sql);
        Assert.Contains("ExternalAccountId", sql);
        Assert.Contains("EffectiveFrom", sql);
        Assert.Contains("EndReason", sql);
        Assert.Contains("ApprovedRequestId", sql);
        Assert.Contains("[IsDeleted] = CAST(0 AS bit)", sql);
        Assert.True(sql.IndexOf("FETCH NEXT", StringComparison.Ordinal) < sql.IndexOf("LEFT JOIN", StringComparison.Ordinal),
            "Debts must be paginated before historical enrichment.");
        Assert.Contains("OFFSET", sql);
        Assert.Contains("FETCH NEXT", sql);
    }
}
