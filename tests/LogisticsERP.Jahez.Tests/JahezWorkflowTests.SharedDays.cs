using System.Text.Json;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Fact]
    public async Task WithdrawalDraftRetainsUsageAndDailyImportAutomaticallySplitsTwoAndFour()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("shared-day");
        var first = Success(await f.Service.HandoverAsync("start-shared", new(account.Id, f.RiderId, At(1), "Start"), ct));
        var draft = Success(await f.Service.CreateOperationAsync("withdraw-draft", new(first.Id, JahezOperationMode.Withdraw,
            new(2026, 10, 6), At(6, 12), 0, "Return", DayUsage: new(OrdersCount: 2)), ct));
        Assert.Equal(2, JsonSerializer.Deserialize<PlatformAccountDayUsageInput>(draft.DayUsageJson!)!.OrdersCount);
        Assert.Null(await f.Db.PlatformAccountDayUsages.Where(u => u.RiderClientAssignmentId == first.AssignmentId
            && u.BusinessDate == new DateOnly(2026, 10, 6)).Select(u => u.OrdersCount).SingleOrDefaultAsync(ct));
        Success(await f.Service.ExecuteOperationAsync("withdraw-execute", draft.Id, ct));
        var second = Success(await f.Service.HandoverAsync("second-shared", new(account.Id, f.OtherRiderId, At(6, 13), "Next"), ct));
        var upload = Success(await f.Service.UploadAsync("daily-shared", new(JahezImportKind.DailyDispatches,
            [new("daily.xlsx", Dispatches("shared-day", "06/10/2026", 6))]), ct));
        Assert.Empty(upload.Issues); Assert.Equal(2, upload.ResolvedAllocations.Count);
        Success(await f.Service.CommitImportAsync("commit-shared", upload.BatchId, new(), ct));
        var rows = await f.Db.Set<JahezDailyDispatch>().Where(d => d.BatchId == upload.BatchId).ToArrayAsync(ct);
        Assert.Equal(2, rows.Single(d => d.HandoverId == first.Id).Count);
        Assert.Equal(4, rows.Single(d => d.HandoverId == second.Id).Count);
        Assert.All(rows, d => Assert.NotNull(d.DayUsageId));
        Assert.True((await f.Service.HandoverAsync("cannot-insert", new(account.Id, f.RiderId, At(6, 14), "Invalid"), ct)).IsFailure);
    }

    [Fact]
    public async Task ManualDispatchAllocationCannotContradictRecordedOutgoingOrders()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("manual-shared");
        var first = Success(await f.Service.HandoverAsync("start-manual", new(account.Id, f.RiderId, At(1), "Start"), ct));
        var withdrawal = Success(await f.Service.CreateOperationAsync("draft-manual", new(first.Id, JahezOperationMode.Withdraw,
            new(2026, 10, 6), At(6, 12), 0, "Return", DayUsage: new(2)), ct));
        Success(await f.Service.ExecuteOperationAsync("execute-manual", withdrawal.Id, ct));
        var second = Success(await f.Service.HandoverAsync("second-manual", new(account.Id, f.OtherRiderId, At(6, 13), "Next"), ct));
        var upload = Success(await f.Service.UploadAsync("upload-manual", new(JahezImportKind.DailyDispatches,
            [new("daily.xlsx", Dispatches("manual-shared", "06/10/2026", 6))]), ct));
        var row = Assert.Single(upload.Rows);
        var rejected = await f.Service.CommitImportAsync("wrong-manual", upload.BatchId,
            new([new(row.RowId, first.Id, 3, "Manual"), new(row.RowId, second.Id, 3, "Manual")]), ct);
        Assert.True(rejected.IsFailure);
        Assert.Empty(await f.Db.Set<JahezDailyDispatch>().ToArrayAsync(ct));
    }
}
