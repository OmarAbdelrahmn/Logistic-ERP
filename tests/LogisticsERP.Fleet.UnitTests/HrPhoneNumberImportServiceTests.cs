using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HrPhoneNumberImportServiceTests
{
    [Fact]
    public async Task HandlerUpdatesEmployeeAndRiderPrimaryPhonesByIqama()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        dbContext.Employees.AddRange(
            new Employee { IqamaNo = "1234567890", FullNameAr = "Employee", IsEmployee = true },
            new Employee { IqamaNo = "2234567890", FullNameAr = "Rider", IsEmployee = false });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["Iqama number", "Phone number"],
            ["1234567890", "0555 123 456"],
            ["2234567890", "00966555123457"]);

        var result = await service.UpdatePhoneNumbersAsync(
            workbook,
            "phones.xlsx",
            validateOnly: false,
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanUpdate);
        Assert.True(result.Value.Updated);
        Assert.Equal(1, result.Value.MatchedEmployees);
        Assert.Equal(1, result.Value.MatchedRiders);
        Assert.Equal(2, result.Value.ChangedPhoneNumbers);
        var employees = await dbContext.Employees.OrderBy(employee => employee.IqamaNo).ToArrayAsync(cancellationToken);
        Assert.Equal("+966555123456", employees[0].PrimaryPhone);
        Assert.Equal("+966555123457", employees[1].PrimaryPhone);
    }

    [Fact]
    public async Task CheckerReportsChangesWithoutWriting()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        dbContext.Employees.Add(new Employee
        {
            IqamaNo = "1234567890",
            FullNameAr = "Employee",
            IsEmployee = true,
            PrimaryPhone = "+966555000000"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["Iqama number", "Phone number"],
            ["1234567890", "0555123456"]);

        var result = await service.UpdatePhoneNumbersAsync(
            workbook,
            "phones.xlsx",
            validateOnly: true,
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Equal(1, result.Value.ChangedPhoneNumbers);
        Assert.Equal(
            "+966555000000",
            (await dbContext.Employees.SingleAsync(cancellationToken)).PrimaryPhone);
    }

    [Fact]
    public async Task UnknownIqamaBlocksEveryUpdate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        dbContext.Employees.Add(new Employee
        {
            IqamaNo = "1234567890",
            FullNameAr = "Employee",
            IsEmployee = true,
            PrimaryPhone = "+966555000000"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["Iqama number", "Phone number"],
            ["1234567890", "0555123456"],
            ["2234567890", "0555123457"]);

        var result = await service.UpdatePhoneNumbersAsync(
            workbook,
            "phones.xlsx",
            validateOnly: false,
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Contains(result.Value.Issues, issue =>
            issue.IqamaNo == "2234567890" && issue.Message.Contains("No employee or rider", StringComparison.Ordinal));
        Assert.Equal(
            "+966555000000",
            (await dbContext.Employees.SingleAsync(cancellationToken)).PrimaryPhone);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .ConfigureWarnings(options => options.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options,
        TimeProvider.System);

    private static HrExcelImportService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, TimeProvider.System, NullLogger<HrExcelImportService>.Instance);

    private static MemoryStream CreateWorkbook(string[] headers, params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Phone numbers");
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                sheet.Cell(row + 2, column + 1).Value =
                    XLCellValue.FromObject(rows[row][column], CultureInfo.InvariantCulture);
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
