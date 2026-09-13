using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HrExcelImportServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ImportCreatesResidencyMetadataAndEveryConfiguredLicenseWithDefaultExpiries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        await SeedLicenseCategoriesAsync(dbContext, cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["رقم الاقامة", "الاسم", "العمل الفعلي", "حالة الكفالة", "حالة الموظف", "نوع الرخصة"],
            ["1234567890", "Test Rider", "دباب", "خارج الكفالة", "Active", "خصوصي + دراجة نارية"]);

        var result = await service.ImportAsync(workbook, "riders.xlsx", validateOnly: false, cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanImport);
        Assert.True(result.Value.Imported);
        Assert.Equal(1, result.Value.CreatedEmployees);
        Assert.Equal(1, result.Value.CreatedRiders);
        Assert.Equal(1, result.Value.CreatedResidencyDocuments);
        Assert.Equal(2, result.Value.CreatedDriverLicenses);
        Assert.Equal(3, result.Value.DefaultedExpiryDates);
        Assert.Equal(1, await dbContext.Employees.CountAsync(cancellationToken));
        Assert.Equal(1, await dbContext.RiderProfiles.CountAsync(cancellationToken));
        Assert.Equal(1, await dbContext.EmployeeDocuments.CountAsync(cancellationToken));
        var licenses = await dbContext.EmployeeDriverLicenses.ToArrayAsync(cancellationToken);
        Assert.Equal(2, licenses.Length);
        Assert.All(licenses, license =>
        {
            Assert.Equal(new DateOnly(2026, 9, 12), license.IssueDate);
            Assert.Equal(new DateOnly(2027, 9, 12), license.ExpiryDate);
            Assert.Equal(DriverLicenseStatus.Active, license.LicenseStatus);
            Assert.True(license.IsCurrent);
        });
    }

    [Fact]
    public async Task SuppliedLicenseDatesAreAppliedToEverySplitCategory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        await SeedLicenseCategoriesAsync(dbContext, cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            [
                "رقم الاقامة", "الاسم", "العمل الفعلي", "حالة الكفالة", "نوع الرخصة",
                "تاريخ اصدار الرخصة", "تاريخ انتهاء الرخصة"
            ],
            [
                "1234567890", "Test Rider", "دباب", "خارج الكفالة", "نقل خفيف + دراجة نارية",
                new DateTime(2026, 2, 1), new DateTime(2030, 2, 1)
            ]);

        var result = await service.ImportAsync(workbook, "riders.xlsx", validateOnly: false, cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Imported);
        Assert.Equal(1, result.Value.DefaultedExpiryDates);
        var licenses = await dbContext.EmployeeDriverLicenses.ToArrayAsync(cancellationToken);
        Assert.Equal(2, licenses.Length);
        Assert.All(licenses, license =>
        {
            Assert.Equal(new DateOnly(2026, 2, 1), license.IssueDate);
            Assert.Equal(new DateOnly(2030, 2, 1), license.ExpiryDate);
        });
    }

    [Fact]
    public async Task SuppliedResidencyExpiryIsPreservedInsteadOfUsingTheDefault()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["رقم الاقامة", "الاسم", "العمل الفعلي", "حالة الكفالة", "تاريخ اصدار الاقامة", "تاريخ انتهاء الاقامة"],
            ["1234567890", "Test Employee", "اداري", "خارج الكفالة", new DateTime(2025, 1, 1), new DateTime(2028, 1, 1)]);

        var result = await service.ImportAsync(workbook, "employees.xlsx", validateOnly: false, cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Imported);
        Assert.Equal(0, result.Value.DefaultedExpiryDates);
        var document = await dbContext.EmployeeDocuments.SingleAsync(cancellationToken);
        Assert.Equal(new DateOnly(2025, 1, 1), document.IssueDate);
        Assert.Equal(new DateOnly(2028, 1, 1), document.ExpiryDate);
    }

    [Fact]
    public async Task ValidationDoesNotWriteEvenWhenTheWorkbookCanBeImported()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["رقم الاقامة", "الاسم", "العمل الفعلي", "حالة الكفالة"],
            ["1234567890", "Test Rider", "دباب", "خارج الكفالة"]);

        var result = await service.ImportAsync(workbook, "validate.xlsx", validateOnly: true, cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanImport);
        Assert.False(result.Value.Imported);
        Assert.Equal(0, await dbContext.Employees.CountAsync(cancellationToken));
        Assert.Equal(0, await dbContext.RiderProfiles.CountAsync(cancellationToken));
        Assert.Equal(0, await dbContext.EmployeeDocuments.CountAsync(cancellationToken));
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task AnyValidationErrorBlocksAllImportWrites()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["رقم الاقامة", "الاسم", "العمل الفعلي", "حالة الكفالة"],
            ["1234567890", "Valid Rider", "دباب", "خارج الكفالة"],
            ["123", "Invalid Rider", "دباب", "خارج الكفالة"]);

        var result = await service.ImportAsync(workbook, "invalid.xlsx", validateOnly: false, cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanImport);
        Assert.False(result.Value.Imported);
        Assert.Contains(result.Value.Issues, issue => issue.Severity == "Error");
        Assert.Equal(0, await dbContext.Employees.CountAsync(cancellationToken));
        Assert.Equal(0, await dbContext.RiderProfiles.CountAsync(cancellationToken));
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .ConfigureWarnings(options => options.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options,
        new FixedTimeProvider());

    private static HrExcelImportService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, new FixedTimeProvider(), NullLogger<HrExcelImportService>.Instance);

    private static async Task SeedLicenseCategoriesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        dbContext.DriverLicenseCategories.AddRange(
            new DriverLicenseCategory
            {
                Id = DriverLicenseCategory.LightTransportId,
                Code = "LIGHT_TRANSPORT",
                NameAr = "نقل خفيف",
                NameEn = "Light Transport",
                Status = CatalogStatus.Active
            },
            new DriverLicenseCategory
            {
                Id = DriverLicenseCategory.MotorcycleId,
                Code = "MOTORCYCLE",
                NameAr = "دراجة نارية",
                NameEn = "Motorcycle",
                Status = CatalogStatus.Active
            },
            new DriverLicenseCategory
            {
                Id = Guid.Parse("01A0334C-C508-79D6-8585-5B90DE0DE3EE"),
                Code = "PRIVATE",
                NameAr = "خصوصي",
                NameEn = "Private",
                Status = CatalogStatus.Active
            });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MemoryStream CreateWorkbook(string[] headers, params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Employees");
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                sheet.Cell(row + 2, column + 1).Value = XLCellValue.FromObject(rows[row][column], CultureInfo.InvariantCulture);
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }
}
