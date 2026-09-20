using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HrEmployeeStatusImportServiceTests
{
    [Fact]
    public async Task HandlerUpdatesEmployeeAndRiderStatusesByIqama()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var employee = new Employee
        {
            IqamaNo = "1234567890",
            FullNameAr = "Employee",
            IsEmployee = true,
            Status = EmployeeStatus.Active
        };
        var rider = new Employee
        {
            IqamaNo = "2234567890",
            FullNameAr = "Rider",
            IsEmployee = false,
            Status = EmployeeStatus.Active
        };
        dbContext.Employees.AddRange(employee, rider);
        dbContext.RiderProfiles.Add(new RiderProfile { EmployeeId = rider.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["Iqama number", "EmployeeStatus"],
            ["1234567890", 4],
            ["2234567890", 5]);

        var result = await service.UpdateStatusesAsync(
            workbook,
            "statuses.xlsx",
            validateOnly: false,
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanUpdate);
        Assert.True(result.Value.Updated);
        Assert.Equal(1, result.Value.MatchedEmployees);
        Assert.Equal(1, result.Value.MatchedRiders);
        Assert.Equal(2, result.Value.ChangedStatuses);
        var employees = await dbContext.Employees.OrderBy(item => item.IqamaNo).ToArrayAsync(cancellationToken);
        Assert.Equal(EmployeeStatus.Suspended, employees[0].Status);
        Assert.Equal(EmployeeStatus.OnLeave, employees[1].Status);
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
            Status = EmployeeStatus.Active
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["Iqama number", "EmployeeStatus"],
            ["1234567890", 6]);

        var result = await service.UpdateStatusesAsync(
            workbook,
            "statuses.xlsx",
            validateOnly: true,
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Equal(1, result.Value.ChangedStatuses);
        Assert.Equal(EmployeeStatus.Active, (await dbContext.Employees.SingleAsync(cancellationToken)).Status);
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
            Status = EmployeeStatus.Active
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["Iqama number", "EmployeeStatus"],
            ["1234567890", 4],
            ["2234567890", 5]);

        var result = await service.UpdateStatusesAsync(
            workbook,
            "statuses.xlsx",
            validateOnly: false,
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Contains(result.Value.Issues, issue =>
            issue.IqamaNo == "2234567890" && issue.Message.Contains("No employee or rider", StringComparison.Ordinal));
        Assert.Equal(EmployeeStatus.Active, (await dbContext.Employees.SingleAsync(cancellationToken)).Status);
    }

    [Fact]
    public async Task ActiveAssignmentBlocksArchivingRider()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var riderEmployee = new Employee
        {
            IqamaNo = "1234567890",
            FullNameAr = "Rider",
            IsEmployee = false,
            Status = EmployeeStatus.Active
        };
        var rider = new RiderProfile { EmployeeId = riderEmployee.Id };
        dbContext.Employees.Add(riderEmployee);
        dbContext.RiderProfiles.Add(rider);
        dbContext.RiderVehicleAssignments.Add(new RiderVehicleAssignment
        {
            RiderProfileId = rider.Id,
            VehicleId = Guid.NewGuid(),
            StartedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            ["Iqama number", "EmployeeStatus"],
            ["1234567890", 7]);

        var result = await service.UpdateStatusesAsync(
            workbook,
            "statuses.xlsx",
            validateOnly: false,
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanUpdate);
        Assert.Contains(result.Value.Issues, issue => issue.Message.Contains("cannot be archived", StringComparison.Ordinal));
        Assert.Equal(EmployeeStatus.Active, (await dbContext.Employees.SingleAsync(cancellationToken)).Status);
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
        var sheet = workbook.AddWorksheet("Statuses");
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
