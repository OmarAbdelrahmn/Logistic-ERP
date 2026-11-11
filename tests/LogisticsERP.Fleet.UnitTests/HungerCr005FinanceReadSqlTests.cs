using LogisticsERP.Infrastructure.Persistence.Migrations.Application;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

// CR-005 phase 7. Needs SQL Server (LOGISTICS_HUNGER_SQL_TESTS=1): the views use JSON_VALUE and CREATE OR ALTER VIEW.
public sealed partial class HungerFinanceTests
{
    [Fact]
    public async Task HungerSqlFinanceReadViewsExposeSealStateAndTheWalletFormulaInputs()
    {
        RequireSql(); await using var f = await Fixture.Create(intercept: true, sql: true);
        var ct = TestContext.Current.CancellationToken;
        // Fixed migration text, not user input; run it exactly as the migration does.
        foreach (var batch in HungerCr005FinanceReadViews.CreateStatements.Select(s => "EXEC(N'" + s.Replace("'", "''") + "')"))
            await f.Db.Database.ExecuteSqlRawAsync(batch, ct);
        Value(await f.Service.PostMoneyAsync(f.Request("Funding", 1000, "fund", f.Bank.Id), ct));
        var advance = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 100, "advance"), ct));
        Value(await f.Service.SealEventAsync(advance.EventId, new(), ct));
        f.Db.RemoveRange(await f.Db.RiderClientAssignments.ToArrayAsync(ct)); await f.Db.SaveChangesAsync(ct);
        var review = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f) with { MobileBalance = -40.5m }, ct));
        var confirmed = Value(await f.Service.ConfirmWalletAsync(review.ReviewId, ct));

        var connection = f.Db.Database.GetDbConnection();
        async Task<List<object?[]>> Rows(string sql)
        {
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand(); command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(ct); var rows = new List<object?[]>();
            while (await reader.ReadAsync(ct)) { var row = new object[reader.FieldCount]; reader.GetValues(row); rows.Add(row); }
            return rows;
        }
        foreach (var view in HungerCr005FinanceReadViews.ViewNames) await Rows($"SELECT TOP 0 * FROM finance_read.{view}");

        var events = await Rows($"SELECT Kind, IsSealed, Role, ApprovalStatus FROM finance_read.vw_hunger_financial_events_v1 WHERE EventId = '{advance.EventId}'");
        Assert.Equal(["TopUpAdvance", true, "Custody", "Sealed"], Assert.Single(events));
        var reviews = await Rows($"SELECT Direction, M, C, A, ExplicitBalance, EventId, ClosingSource FROM finance_read.vw_hunger_wallet_reviews_v1 WHERE ReviewId = '{review.ReviewId}'");
        Assert.Equal(["Deliver", -40.5m, 0m, 0m, -40.5m, confirmed.EventId, "NoPriorPeriod"], Assert.Single(reviews));
        var custody = await Rows("SELECT FundingDelivered, TopUpAdvance, ExpectedBalance, PostingBalance, Difference FROM finance_read.vw_hunger_custody_v1");
        Assert.Equal([1000m, 100m, 900m, 900m, 0m], Assert.Single(custody));
    }
}
