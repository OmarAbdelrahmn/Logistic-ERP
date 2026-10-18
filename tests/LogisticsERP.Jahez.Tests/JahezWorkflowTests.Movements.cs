using System.Reflection;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Fact]
    public async Task V2MovementsIncludeRecordedRiderAndRegisteredOwnerWithIqamas()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("44120");
        var rider = await f.Db.RiderProfiles.SingleAsync(x => x.Id == f.RiderId, ct);
        var employee = await f.Db.Employees.SingleAsync(x => x.Id == rider.EmployeeId, ct);
        var owner = await f.Db.Employees.SingleAsync(x => x.Id == account.RegisteredEmployeeId, ct);
        var sponsorName = await f.Db.Sponsors.Where(x => x.Id == account.SponsorId).Select(x => x.RegistryNameAr).SingleAsync(ct);
        // The movement displays the account sponsor, independently of either employee's sponsor.
        employee.SponsorId = null; owner.SponsorId = null;
        employee.IqamaNo = "2000000001"; owner.IqamaNo = "2000000002";
        await f.Db.SaveChangesAsync(ct);
        var handover = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 50, "fee-1"), ct));
        // Closing the handover must not remove the rider from its historical money movement.
        var session = await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == handover.Id, ct);
        session.EndedAtUtc = At(2);
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();

        var page = Success(await f.Service.MovementsV2Async(1, 50, ct: ct));
        var row = Assert.Single(page.Items);
        Assert.Equal(f.RiderId, row.UserRider!.RiderProfileId);
        Assert.Equal(employee.FullNameAr, row.UserRider.NameAr);
        Assert.Equal("2000000001", row.UserRider.IqamaNo);
        Assert.Equal(owner.Id, row.OwnerRider!.EmployeeId);
        Assert.Equal(owner.FullNameAr, row.OwnerRider.NameAr);
        Assert.Equal("2000000002", row.OwnerRider.IqamaNo);
        Assert.NotEqual(row.UserRider.EmployeeId, row.OwnerRider.EmployeeId);
        Assert.Equal(50, row.Amount); Assert.Equal("fee-1", row.Reference);
        Assert.Equal(handover.Id, row.HandoverId);
        Assert.Equal(account.Id, row.AccountId);
        Assert.Equal("44120", row.PlatformAccountId);
        using var json = System.Text.Json.JsonSerializer.SerializeToDocument(row,
            System.Text.Json.JsonSerializerOptions.Web);
        Assert.Equal("44120", json.RootElement.GetProperty("platformAccountId").GetString());
        Assert.Equal(account.ExternalAccountId, row.ExternalAccountId);
        Assert.Null(row.Net);
        Assert.Null(row.Commission);
        Assert.Null(row.CalculatedCommission);
        Assert.Null(row.StartDate);
        Assert.Null(row.EndDate);
        Assert.Equal(sponsorName, row.SponsorNameAr);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(100, false)]
    [InlineData(-125, false)]
    [InlineData(0, false)]
    [InlineData(100, true)]
    public async Task V2MovementsIncludeRecordedSettlementNetAndCompanyCommission(decimal net, bool approvedOverride)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("movement-settlement");
        var handover = Success(await f.Service.HandoverAsync("start",
            new(account.Id, f.RiderId, At(1), "استلام", 50, "fee-1"), ct));
        var operation = Success(await f.Service.CreateOperationAsync("draft",
            Settle(handover.Id, net, JahezOperationMode.SettleAndWithdraw), ct));
        if (approvedOverride)
        {
            var request = Success(await f.Service.CreateWorkflowRequestAsync("adjust",
                new(handover.Id, JahezApprovalKind.Adjustment, "تصحيح التصفية", OperationId: operation.Id,
                    AdjustmentTarget: JahezAdjustmentTarget.Settlement, ProposedNet: -120, ProposedCommission: 50), ct));
            f.User.UserId = Guid.NewGuid();
            Success(await f.Service.DecideWorkflowRequestAsync("approve", request.Request.Id, new(true, "اعتماد"), ct));
        }
        var executed = Success(await f.Service.ExecuteOperationAsync("execute", operation.Id, ct));
        f.Db.ChangeTracker.Clear();
        // Later commission dates must not change the values recorded for this settlement.
        f.Clock.UtcNow = At(10).ToUniversalTime();

        var rows = Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items;
        Assert.Equal(2, rows.Count);
        var row = Assert.Single(rows, x => x.OperationId == executed.Id);
        Assert.Equal(account.Id, row.AccountId);
        Assert.Equal(account.ExternalAccountId, row.ExternalAccountId);
        Assert.Equal(approvedOverride ? -120 : net, row.Net);
        Assert.Equal(approvedOverride ? 50 : 75, row.Commission);
        Assert.Equal(75, row.CalculatedCommission);
        Assert.Equal(new DateOnly(2026, 10, 2), row.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 6), row.EndDate);
        using var json = System.Text.Json.JsonSerializer.SerializeToDocument(row,
            System.Text.Json.JsonSerializerOptions.Web);
        Assert.Equal("2026-10-02", json.RootElement.GetProperty("startDate").GetString());
        Assert.Equal("2026-10-06", json.RootElement.GetProperty("endDate").GetString());
        Assert.Equal(Math.Abs(executed.Remaining), row.Amount);
        Assert.Equal(executed.Remaining < 0 ? JahezMoneyDirection.Disbursement : JahezMoneyDirection.Receipt, row.Direction);
        var fee = Assert.Single(rows, x => x.OperationId == null);
        Assert.Equal(account.Id, fee.AccountId);
        Assert.Null(fee.Net);
        Assert.Null(fee.Commission);
        Assert.Null(fee.CalculatedCommission);
        Assert.Null(fee.StartDate);
        Assert.Null(fee.EndDate);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(JahezMoneyPurpose.Settlement)]
    [InlineData(JahezMoneyPurpose.Fuel)]
    public async Task V2MovementSettlementDatesUseEachRecordedCommissionPeriod(JahezMoneyPurpose purpose)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("movement-periods");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var first = Success(await f.Service.CreateOperationAsync("first",
            Settle(h.Id, 100) with { ThroughDate = new(2026, 10, 3), Purpose = purpose }, ct));
        Success(await f.Service.ExecuteOperationAsync("execute-first", first.Id, ct));
        var second = Success(await f.Service.CreateOperationAsync("second", Settle(h.Id, 100) with { Purpose = purpose }, ct));
        Success(await f.Service.ExecuteOperationAsync("execute-second", second.Id, ct));
        // Baseline corrections and subsequent posting must not rewrite historical response dates.
        var session = await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == h.Id, ct);
        session.CommissionStartsOn = new(2026, 9, 1);
        session.CommissionPostedThrough = new(2026, 10, 10);
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();

        var rows = Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items;
        var firstRow = Assert.Single(rows, x => x.OperationId == first.Id);
        var secondRow = Assert.Single(rows, x => x.OperationId == second.Id);
        Assert.Equal(purpose == JahezMoneyPurpose.Settlement ? new DateOnly(2026, 10, 2) : (DateOnly?)null, firstRow.StartDate);
        Assert.Equal(purpose == JahezMoneyPurpose.Settlement ? new DateOnly(2026, 10, 3) : (DateOnly?)null, firstRow.EndDate);
        Assert.Equal(purpose == JahezMoneyPurpose.Settlement ? new DateOnly(2026, 10, 4) : (DateOnly?)null, secondRow.StartDate);
        Assert.Equal(purpose == JahezMoneyPurpose.Settlement ? new DateOnly(2026, 10, 6) : (DateOnly?)null, secondRow.EndDate);
        Assert.Equal(30, firstRow.Commission);
        Assert.Equal(45, secondRow.Commission);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(1000, 195)]
    [InlineData(0, 45)]
    public async Task V2MovementSettlementDatesSpanDailyAndPercentageCommission(decimal earnings, decimal commission)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("movement-mixed-commission");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var policy = Success(await f.Service.CreateWorkflowRequestAsync("policy",
            new(h.Id, JahezApprovalKind.PercentageCommission, "عمولة نسبة", FromDate: new(2026, 10, 3), ToDate: new(2026, 10, 4)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve", policy.Request.Id, new(true, "اعتماد"), ct));
        Success(await f.Service.RecordEarningsAsync("earnings", new(h.Id, new(2026, 10, 3), new(2026, 10, 4),
            earnings, 0, 0, 0, 0, 0, 0, 0, 0, "بيانات الفترة"), ct));
        var operation = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        Success(await f.Service.ExecuteOperationAsync("execute", operation.Id, ct));
        f.Db.ChangeTracker.Clear();

        var row = Assert.Single(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items);
        Assert.Equal(new DateOnly(2026, 10, 2), row.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 6), row.EndDate);
        Assert.Equal(commission, row.Commission);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"Accrual\":[],\"UnpaidCommission\":0}")]
    [InlineData("not-json")]
    public async Task V2MovementWithoutRecordedAccrualDatesRemainsVisible(string calculationJson)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("movement-missing-period");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        // Seed historical evidence as originally stored; executed operations cannot be edited.
        var operation = new JahezOperation
        {
            HandoverId = h.Id, Mode = JahezOperationMode.Settle, Status = JahezOperationStatus.Executed,
            PaymentStatus = JahezOperationPaymentStatus.Paid, ThroughDate = new(2026, 10, 6),
            EffectiveAtUtc = At(6), ExecutedAtUtc = At(6), ExecutedByUserId = f.User.UserId,
            Net = 100, Commission = 75, CalculatedCommission = 75, Remaining = 175,
            CalculationJson = calculationJson, Fund = JahezMoneyFund.Cashbox,
            PaymentReference = "legacy", Reason = "historical settlement"
        };
        f.Db.Add(operation);
        f.Db.Add(new JahezMoneyMovement
        {
            HandoverId = h.Id, RiderProfileId = f.RiderId, OperationId = operation.Id,
            Fund = JahezMoneyFund.Cashbox, Direction = JahezMoneyDirection.Receipt,
            Purpose = JahezMoneyPurpose.Settlement, Amount = 175, OccurredAtUtc = At(6),
            ActorUserId = f.User.UserId!.Value, Reference = "legacy", Reason = "historical settlement"
        });
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();

        var row = Assert.Single(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items);
        Assert.Equal(operation.Id, row.OperationId);
        Assert.Null(row.StartDate);
        Assert.Null(row.EndDate);
        Assert.Equal(75, row.Commission);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task V2MovementsKeepRowsWhenRiderOrOwnerIsUnavailable(bool missingRider, bool missingOwner)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("movement-optional-people");
        Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 50, "fee-1"), ct));
        if (missingRider)
        {
            var employeeId = await f.Db.RiderProfiles.Where(x => x.Id == f.RiderId).Select(x => x.EmployeeId).SingleAsync(ct);
            var employee = await f.Db.Employees.SingleAsync(x => x.Id == employeeId, ct);
            employee.IsDeleted = true;
        }
        if (missingOwner)
        {
            var tracked = await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct);
            tracked.RegisteredEmployeeId = null;
        }
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();

        var row = Assert.Single(Success(await f.Service.MovementsV2Async(1, 1, ct: ct)).Items);
        Assert.Equal(missingRider, row.UserRider is null);
        Assert.Equal(missingOwner, row.OwnerRider is null);
        if (row.UserRider is not null) Assert.Null(row.UserRider.IqamaNo);
        if (row.OwnerRider is not null) Assert.Null(row.OwnerRider.IqamaNo);
        Assert.Empty(Success(await f.Service.MovementsV2Async(2, 1, ct: ct)).Items);
    }

    [Fact]
    public async Task V2MovementsKeepRowsWhenAccountSponsorIsMissing()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("movement-no-sponsor");
        Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 50, "fee-1"), ct));
        var tracked = await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct);
        tracked.SponsorId = null;
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();

        var row = Assert.Single(Success(await f.Service.MovementsV2Async(1, 1, ct: ct)).Items);
        Assert.Null(row.SponsorNameAr);
        Assert.NotNull(row.UserRider);
        Assert.NotNull(row.OwnerRider);
        Assert.Empty(Success(await f.Service.MovementsV2Async(2, 1, ct: ct)).Items);
    }

    [Fact]
    public async Task V2MovementDatesDefaultToCurrentRiyadhMonthThroughTodayBeforePaging()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        // Riyadh has entered October while UTC is still in September.
        f.Clock.UtcNow = new DateTimeOffset(2026, 9, 30, 22, 0, 0, TimeSpan.Zero);
        var start = new DateTimeOffset(2026, 9, 30, 21, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(1);
        await SeedMovementDates(f, start.AddTicks(-1), start, end.AddTicks(-1), end);

        var all = Success(await f.Service.MovementsV2Async(1, 50, ct: ct));
        Assert.Equal(new[] { end.AddTicks(-1), start }, all.Items.Select(x => x.OccurredAtUtc));
        var first = Success(await f.Service.MovementsV2Async(1, 1, ct: ct));
        var second = Success(await f.Service.MovementsV2Async(2, 1, ct: ct));
        Assert.Equal(end.AddTicks(-1), Assert.Single(first.Items).OccurredAtUtc);
        Assert.Equal(start, Assert.Single(second.Items).OccurredAtUtc);
        Assert.Empty(Success(await f.Service.MovementsV2Async(3, 1, ct: ct)).Items);
    }

    [Theory]
    [InlineData(null, null, 1, 6)]
    [InlineData(3, null, 3, 6)]
    [InlineData(null, 4, 1, 4)]
    [InlineData(3, 4, 3, 4)]
    [InlineData(3, 3, 3, 3)]
    public async Task V2MovementDatesUseInclusiveRiyadhDaysAndIndependentDefaults(int? fromDay, int? toDay,
        int expectedFrom, int expectedTo)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var dates = Enumerable.Range(1, 8)
            .Select(day => new DateTimeOffset(2026, 10, day, 0, 0, 0, TimeSpan.FromHours(3))).ToArray();
        await SeedMovementDates(f, dates);
        DateOnly? from = fromDay.HasValue ? new DateOnly(2026, 10, fromDay.Value) : null;
        DateOnly? to = toDay.HasValue ? new DateOnly(2026, 10, toDay.Value) : null;

        var page = Success(await f.Service.MovementsV2Async(1, 50, from, to, ct));
        var expected = dates.Where(x => x.Day >= expectedFrom && x.Day <= expectedTo).Reverse();
        Assert.Equal(expected, page.Items.Select(x => x.OccurredAtUtc));
    }

    [Fact]
    public async Task V2MovementDatesRejectReversedRangeAndAllowEmptyHistoricalRange()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var invalid = await f.Service.MovementsV2Async(1, 50, new(2026, 10, 7), new(2026, 10, 1), ct);
        Assert.False(invalid.IsSuccess);
        Assert.Equal("jahez.invalid_request", invalid.Error.Code);
        Assert.Empty(Success(await f.Service.MovementsV2Async(1, 50, new(2026, 9, 1), new(2026, 9, 30), ct)).Items);
    }

    private static async Task SeedMovementDates(Fixture f, params DateTimeOffset[] dates)
    {
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("movement-dates");
        var handover = Success(await f.Service.HandoverAsync("start-dates",
            new(account.Id, f.RiderId, At(1, 0), "استلام", 0), ct));
        foreach (var date in dates)
            f.Db.Add(new JahezMoneyMovement
            {
                HandoverId = handover.Id, RiderProfileId = f.RiderId, Fund = JahezMoneyFund.Cashbox,
                Direction = JahezMoneyDirection.Receipt, Purpose = JahezMoneyPurpose.AccountFee,
                Amount = 1, ActorUserId = f.User.UserId!.Value, OccurredAtUtc = date,
                Reference = "date-boundary", Reason = "date range verification"
            });
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
    }

    [Fact]
    public void V2MovementPeopleQueryTranslatesToSqlWithPaginationAndLeftJoins()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedMovements;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        var query = (IQueryable<JahezMoneyMovementResponse>)typeof(JahezWorkflowService)
            .GetMethod("WorkflowMovements", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, [2, 10, At(1).ToUniversalTime(), At(7).ToUniversalTime()])!;
        var sql = query.ToQueryString();
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("IqamaNo", sql);
        Assert.Contains("RegistryNameAr", sql);
        Assert.Contains("ExternalAccountId", sql);
        Assert.Contains("[Net]", sql);
        Assert.Contains("[Commission]", sql);
        Assert.Contains("[CalculatedCommission]", sql);
        Assert.Contains("[CalculationJson]", sql);
        Assert.Contains("OFFSET", sql);
        Assert.Contains("FETCH NEXT", sql);
        Assert.Contains("[OccurredAtUtc] >=", sql);
        Assert.Contains("[OccurredAtUtc] <", sql);
    }
}
