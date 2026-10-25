using System.IO.Compression;
using System.Text.Json;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Infrastructure.Chefz;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    private static readonly JsonSerializerOptions MoneyAuditJsonOptions = new() { WriteIndented = true };

    [Theory]
    [InlineData("2151034 Albawaba Almokbela (8).xlsx", 5067, 117096, 953, 961, 3365, 98080.60, 16044.11, 82036.49, 25, 15273.38, 100371.62, 16239.23, 84132.39)]
    [InlineData("2151351 Albawaabah Farei (8).xlsx", 4048, 93517, 1032, 664, 2789, 78396.45, 12408.78, 65987.67, 0, 12197.85, 80226.15, 12518.30, 67707.85)]
    public async Task UploadedOctoberOrderFilesReconcileReadOnly(string fileName, int count, decimal fees,
        decimal tips, decimal compensation, decimal deductions, decimal net, decimal company,
        decimal rider, decimal companyChangeAfterAssignmentCorrection, decimal correctedVat,
        decimal correctedNet, decimal correctedCompany, decimal correctedRider)
    {
        var root = Environment.GetEnvironmentVariable("CHEFZ_MONEY_AUDIT_ROOT");
        var source = Environment.GetEnvironmentVariable("CHEFZ_MONEY_AUDIT_SOURCE");
        Assert.SkipUnless(root is not null && source is not null,
            "Opt-in read-only audit: set CHEFZ_MONEY_AUDIT_ROOT and CHEFZ_MONEY_AUDIT_SOURCE.");
        var ct = TestContext.Current.CancellationToken;
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(root!, "src/LogisticsERP.Api/appsettings.json"), ct));
        var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("LogisticsDatabase").GetString();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection).AddInterceptors(new RejectMoneyAuditWrites()).Options);
        Assert.Equal("db67927", db.Database.GetDbConnection().Database);
        Assert.Equal("db67927.public.databaseasp.net", db.Database.GetDbConnection().DataSource);
        var importsBefore = await db.ChefzOrderSettlementImports.CountAsync(ct);
        var settlementsBefore = await db.ChefzWalletSettlements.CountAsync(ct);
        var service = new ChefzService(db, new CurrentUser(), new Checker(), TimeProvider.System,
            new PlatformCredentialProtector(() => new byte[32]));
        await using var sourceStream = new FileStream(Path.Combine(source!, fileName),
            FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sourceBytes = new MemoryStream();
        await sourceStream.CopyToAsync(sourceBytes, ct);
        var bytes = sourceBytes.ToArray();
        var parsed = ChefzOrderSpreadsheetParser.Parse(bytes, ct);
        Assert.Empty(parsed.Issues);
        Assert.Equal(count, parsed.Rows.Count);
        Assert.Equal(fees, parsed.Rows.Sum(r => r.DeliveryFees));
        Assert.Equal(tips, parsed.Rows.Sum(r => r.Tips));
        Assert.Equal(compensation, parsed.Rows.Sum(r => r.Compensation));
        var collectedCash = parsed.Rows.Sum(r => r.CashCollected);
        Assert.Equal(deductions + collectedCash, parsed.Rows.Sum(r => r.Deductions));
        Assert.All(parsed.Rows, r => Assert.InRange(r.Date, new(2026, 10, 1), new(2026, 10, 15)));

        // An identical-file preview must preserve the saved financial snapshot.
        var saved = Value(await service.ProcessOrderSettlementsAsync(fileName, bytes, true, ct));
        Assert.True(saved.AlreadyImported);
        Assert.NotNull(saved.ImportId);
        Assert.Empty(saved.Issues);
        Assert.Equal(count, saved.SourceRows);
        Assert.Equal(net, saved.NetEarnings);
        Assert.Equal(company, saved.CompanyAmount);
        Assert.Equal(rider, saved.RiderAmount);
        Assert.Equal(fees * .15m, saved.Riders.Sum(r => r.VatAmount));

        // Repackage the ZIP in memory to bypass byte-hash replay while retaining
        // every original workbook entry and value. Only preview is called.
        using var input = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach (var entry in input.Entries)
            {
                var copy = archive.CreateEntry(entry.FullName, CompressionLevel.NoCompression);
                using var from = entry.Open();
                using var to = copy.Open();
                await from.CopyToAsync(to, ct);
            }
        }
        var fresh = Value(await service.ProcessOrderSettlementsAsync(fileName, output.ToArray(), true, ct));
        Assert.False(fresh.AlreadyImported);
        Assert.Null(fresh.ImportId);
        Assert.False(fresh.CanSave);
        Assert.Equal(correctedNet - collectedCash, fresh.NetEarnings);
        // The October reference totals predate collected-cash deductions. Restore that
        // component to verify the historical baseline, then verify the current split.
        decimal AccountCash(ChefzOrderSettlementResponse account) => parsed.Rows.Where(r => r.DriverId == account.DriverId
            && r.Date >= account.ChargedFrom && r.Date <= account.ChargedTo).Sum(r => r.CashCollected);
        var referenceCompany = fresh.Riders.Sum(r => r.Accounts.Where(a => a.AccountType == "FullTime").Sum(a => a.AssignedDays * 25m)
            + ChefzOrderSettlementRules.Money(r.Accounts.Where(a => a.AccountType == "Freelancer").Sum(a => a.NetEarnings + AccountCash(a)) * .15m));
        var currentCompany = fresh.Riders.Sum(r => r.Accounts.Where(a => a.AccountType == "FullTime").Sum(a => a.AssignedDays * 25m)
            + ChefzOrderSettlementRules.Money(r.Accounts.Where(a => a.AccountType == "Freelancer").Sum(a => a.DeliveryFeesAfterVat) * .15m));
        Assert.Equal(correctedCompany + companyChangeAfterAssignmentCorrection, referenceCompany);
        Assert.Equal(correctedRider - companyChangeAfterAssignmentCorrection, correctedNet - referenceCompany);
        Assert.Equal(currentCompany, fresh.CompanyAmount);
        Assert.Equal(fresh.NetEarnings - currentCompany, fresh.RiderAmount);
        Assert.Equal(fees, fresh.Riders.Sum(r => r.DeliveryFees));
        Assert.Equal(correctedVat, fresh.Riders.Sum(r => r.VatAmount));
        Assert.All(fresh.Issues, i =>
        {
            Assert.Equal(0, i.RowNumber);
            Assert.Contains("توجد تصفية محفوظة", i.Message, StringComparison.Ordinal);
        });
        Assert.Equal(fresh.Riders.Sum(r => r.Accounts.Count), fresh.Issues.Count);
        Assert.Equal(importsBefore, await db.ChefzOrderSettlementImports.CountAsync(ct));
        Assert.Equal(settlementsBefore, await db.ChefzWalletSettlements.CountAsync(ct));
        Assert.False(db.ChangeTracker.HasChanges());

        var evidence = new { fileName, parsedRows = count, saved, fresh,
            importsBefore, settlementsBefore, writesAttempted = false };
        await File.WriteAllTextAsync(Path.Combine(root!, "outputs/chefz-vat-correction-20261017",
            fileName[..7] + "-service-verification.json"), JsonSerializer.Serialize(evidence,
            MoneyAuditJsonOptions), ct);
    }

    [Theory]
    [InlineData(115, 15, 100)]
    [InlineData(230, 30, 200)]
    [InlineData(-115, -15, -100)]
    [InlineData(0, 0, 0)]
    [InlineData(1, .13, .87)]
    [InlineData(1000, 130.43, 869.57)]
    public void OrderFeesExtractIncludedVat(decimal fees, decimal vat, decimal beforeVat)
    {
        var row = ChefzOrderSettlementRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1,
            new(2026, 10, 1), null, 1, fees, 0, 0, 0, 0);
        Assert.Equal(vat, row.VatAmount);
        Assert.Equal(beforeVat, row.GrossEarnings);
        Assert.Equal(fees, row.GrossEarnings + row.VatAmount);
        Assert.Equal(beforeVat, row.CompanyAmount + row.RiderAmount);
    }

    [Fact]
    public async Task StoredOrdersVatCorrectionPlanKeepsHistoricalSplitAndCurrentFeeBasedCommissionReadOnly()
    {
        var root = Environment.GetEnvironmentVariable("CHEFZ_MONEY_AUDIT_ROOT");
        Assert.SkipUnless(root is not null, "Set CHEFZ_MONEY_AUDIT_ROOT for the stored-snapshot correction audit.");
        var path = Path.Combine(root!, "outputs/chefz-vat-correction-20261017/correction-plan.json");
        using var plan = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        var cases = plan.RootElement.GetProperty("settlements").EnumerateArray().ToArray();
        var computed = cases.Select(s =>
        {
            decimal Amount(string key) => decimal.Parse(s.GetProperty(key).ToString(), System.Globalization.CultureInfo.InvariantCulture);
            DateOnly Date(string key) => DateOnly.ParseExact(s.GetProperty(key).GetString()![..10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var period = Date("PeriodFrom");
            var row = ChefzOrderSettlementRules.Calculate((ChefzAccountType)s.GetProperty("AccountType").GetInt32(),
                period.Year, period.Month, period.Day <= 15 ? 1 : 2, Date("ChargedFrom"), Date("ChargedTo"),
                s.GetProperty("SourceRows").GetInt32(), Amount("DeliveryFees"), Amount("Tips"), Amount("Compensation"), Amount("Bonuses"), Amount("Deductions"));
            row.Id = s.GetProperty("Id").GetGuid();
            row.ImportId = s.GetProperty("ImportId").GetGuid();
            row.RiderProfileId = s.GetProperty("RiderProfileId").GetGuid();
            row.PlatformRiderAccountId = s.GetProperty("PlatformRiderAccountId").GetGuid();
            row.RiderClientAssignmentId = s.GetProperty("RiderClientAssignmentId").GetGuid();
            return row;
        }).ToArray();
        foreach (var batch in computed.GroupBy(s => s.ImportId)) ChefzOrderSettlementRules.ReconcileRiderCycles(batch);
        // This retained correction plan predates the delivery-fees-only commission rule.
        // Validate its historical net-based split independently of today's calculated split.
        var historicalCompany = computed.ToDictionary(s => s.Id, s => s.AccountType == ChefzAccountType.Freelancer
            ? ChefzOrderSettlementRules.Money(s.GrossEarnings * ChefzRules.FreelancerRate) : s.CompanyAmount);
        foreach (var cycle in computed.GroupBy(s => new { s.ImportId, s.RiderProfileId, s.PeriodFrom }))
        {
            var freelancers = cycle.Where(s => s.AccountType == ChefzAccountType.Freelancer)
                .OrderByDescending(s => Math.Abs(s.GrossEarnings)).ThenBy(s => s.PlatformRiderAccountId)
                .ThenBy(s => s.RiderClientAssignmentId).ToArray();
            if (freelancers.Length > 0)
                historicalCompany[freelancers[0].Id] += ChefzOrderSettlementRules.Money(
                    freelancers.Sum(s => s.GrossEarnings) * ChefzRules.FreelancerRate)
                    - freelancers.Sum(s => historicalCompany[s.Id]);
        }
        foreach (var expected in cases)
        {
            var row = Assert.Single(computed, s => s.Id == expected.GetProperty("Id").GetGuid());
            decimal Amount(string key) => decimal.Parse(expected.GetProperty(key).ToString(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(Amount("VatAmount"), row.VatAmount);
            Assert.Equal(Amount("GrossEarnings"), row.GrossEarnings);
            Assert.Equal(Amount("CompanyAmount"), historicalCompany[row.Id]);
            Assert.Equal(Amount("RiderAmount"), row.GrossEarnings - historicalCompany[row.Id]);
            Assert.Equal(expected.GetProperty("AssignedDays").GetInt32(), row.AssignedDays);
        }
        Assert.Equal(241, computed.Length);
    }

    private sealed class RejectMoneyAuditWrites : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData,
            InterceptionResult<int> result) => throw new InvalidOperationException("Read-only money audit forbids database writes.");

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Read-only money audit forbids database writes.");
    }
}
