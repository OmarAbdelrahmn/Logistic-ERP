using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class ExternalRiderImportServiceTests
{
    [Fact]
    public void ParserAcceptsExactArabicHeadersAndOptionalNationality()
    {
        using var workbook = Workbook(
            ["2541316366", "سالم صالح حسين", "", "506772231", "جدة"]);

        var parsed = ExternalRiderSpreadsheetParser.Parse(workbook);

        var row = Assert.Single(parsed.Rows);
        Assert.Equal("2541316366", row.IqamaNo);
        Assert.Equal("سالم صالح حسين", row.FullNameAr);
        Assert.Null(row.Nationality);
        Assert.Equal("+966506772231", row.PhoneNumber);
        Assert.Equal("جدة", row.OperatingCity);
    }

    [Fact]
    public async Task ImportCreatesExternalEmployeeAndRiderProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        await SeedCitiesAsync(dbContext, cancellationToken);
        var service = new ExternalRiderImportService(
            dbContext, NullLogger<ExternalRiderImportService>.Instance);
        using var workbook = Workbook(
            ["2541316366", "سالم صالح حسين علي شعلان", "اليمن", "506772231", "جدة"],
            ["2621386362", "متولي محمد متولي محمد", "مصر", "547056935", "الرياض"]);

        var result = await service.ImportAsync(workbook, "external-riders.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Imported);
        Assert.Equal(2, result.Value.ValidRows);
        Assert.Equal(2, result.Value.CreatedEmployees);
        Assert.Equal(2, result.Value.CreatedRiderProfiles);
        var employees = await dbContext.Employees.OrderBy(employee => employee.IqamaNo).ToArrayAsync(cancellationToken);
        Assert.All(employees, employee =>
        {
            Assert.False(employee.IsEmployee);
            Assert.Equal(EmployeeRelationshipType.OutsideRider, employee.EngagementType);
            Assert.Equal(EmployeeStatus.Active, employee.Status);
            Assert.NotNull(employee.OperatingCityId);
        });
        Assert.Equal(2, await dbContext.RiderProfiles.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task ImportSkipsInvalidRowsAndImportsValidRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        await SeedCitiesAsync(dbContext, cancellationToken);
        var service = new ExternalRiderImportService(
            dbContext, NullLogger<ExternalRiderImportService>.Instance);
        using var workbook = Workbook(
            ["2541316366", "سالم صالح حسين", "اليمن", "506772231", "جدة"],
            ["2564235006", "عبدالرقيب محمد صالح", "مصر", "53710769", "الرياض"]);

        var result = await service.ImportAsync(workbook, "mixed-external-riders.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Imported);
        Assert.Equal(2, result.Value.TotalRows);
        Assert.Equal(1, result.Value.ValidRows);
        Assert.Equal(1, result.Value.CreatedEmployees);
        Assert.Contains(result.Value.Issues, issue => issue.RowNumber == 3 && issue.Severity == "Error");
        Assert.Single(await dbContext.Employees.ToArrayAsync(cancellationToken));
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ExternalRiderImport_{Guid.NewGuid():N}",
                options => options.EnableNullChecks(false)).Options,
        TimeProvider.System);

    private static async Task SeedCitiesAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var jeddah = new GlobalCity
        {
            Code = "JEDDAH-TEST", NameAr = "جدة", NameEn = "Jeddah",
            RegionAr = "مكة", RegionEn = "Makkah", Status = CatalogStatus.Active
        };
        var riyadh = new GlobalCity
        {
            Code = "RIYADH-TEST", NameAr = "الرياض", NameEn = "Riyadh",
            RegionAr = "الرياض", RegionEn = "Riyadh", Status = CatalogStatus.Active
        };
        dbContext.AddRange(
            jeddah,
            riyadh,
            new OperatingCity { GlobalCityId = jeddah.Id, EnabledFrom = new DateOnly(2026, 1, 1), Status = CatalogStatus.Active },
            new OperatingCity { GlobalCityId = riyadh.Id, EnabledFrom = new DateOnly(2026, 1, 1), Status = CatalogStatus.Active });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MemoryStream Workbook(params object[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("السائقون الخارجيون");
        sheet.Cell(1, 1).Value = "رقم   الإقامة";
        sheet.Cell(1, 2).Value = "الاسم";
        sheet.Cell(1, 3).Value = "الجنسية";
        sheet.Cell(1, 4).Value = "رقم   الجوال";
        sheet.Cell(1, 5).Value = "مدينة   التشغيل";
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
                sheet.Cell(row + 2, column + 1).Value =
                    XLCellValue.FromObject(rows[row][column], CultureInfo.InvariantCulture);
        }
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
