using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    // Permission-scoped HTTP query; background jobs use IHungerDeferredReminderQuery.
    // Reading a reminder never activates the account or clears the disposition.
    public async Task<Result<IReadOnlyList<HungerDeferredReminder>>> DeferredRemindersAsync(DateOnly? dueThrough = null, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.Read, ct);
        if (platforms.Length == 0) return Result.Failure<IReadOnlyList<HungerDeferredReminder>>(HungerErrors.Forbidden);
        var due = dueThrough ?? BusinessToday;
        if (due == default || due > BusinessToday) return Result.Failure<IReadOnlyList<HungerDeferredReminder>>(HungerErrors.Invalid("Use a due date no later than today."));
        return Result.Success(await HungerDeferredReminderQuery.ReadDueAsync(db, platforms, BusinessToday, due, ct));
    }

    public async Task<Result<HungerFile>> AccountTemplateAsync(CancellationToken ct = default)
    {
        if ((await Platforms(PermissionKeys.Hunger.AccountsImport, ct)).Length == 0) return Result.Failure<HungerFile>(HungerErrors.Forbidden);
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Sheet1");
        string[] headers = ["رقم اليوزر", "رقم هوية اليوزر", "رقم الهوية1", "رقم الهاتف", "مدينة اليوزر", "سجل اليوزر", "نظام التسوية",
            "حالة اليوزر", "نوع مركبة اليوزر", "نوع الشريحة", "نظام المحاسبة", "نوع المركبة1", "تاريخ الإستلام1", "استلام المحفظة1", "استلام عدد الطلبات1"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Columns(1, 4).Style.NumberFormat.Format = "@";
        sheet.Column(13).Style.DateFormat.Format = "yyyy-mm-dd";
        return Result.Success(ExportBook(book, "Hunger-Accounts-Template.xlsx"));
    }

    private static HungerFile TabularFile(string name, string format, string[] headers, IEnumerable<object?[]> rows)
    {
        static string Text(object? value) => value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) ?? "" : value?.ToString() ?? "";
        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            static string Cell(string value)
            {
                // Spreadsheet applications must treat user supplied text as data.
                if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            var csv = new StringBuilder(); csv.AppendLine(string.Join(",", headers.Select(Cell)));
            foreach (var row in rows) csv.AppendLine(string.Join(",", row.Select(v => v is string ? Cell(Text(v)) : "\"" + Text(v).Replace("\"", "\"\"") + "\"")));
            return new(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", name + ".csv");
        }
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet(name);
        for (var col = 0; col < headers.Length; col++) sheet.Cell(1, col + 1).Value = headers[col];
        var index = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Length; col++) sheet.Cell(index, col + 1).Value = XLCellValue.FromObject(row[col], CultureInfo.InvariantCulture);
            index++;
        }
        return ExportBook(book, name + ".xlsx");
    }

    public async Task<Result<HungerFile>> ExportAccountsAsync(HungerAccountFilter filter, string format = "xlsx", CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.PerformanceExport, ct);
        if (platforms.Length == 0 || !await FieldAccess(platforms, [PermissionKeys.Hunger.Read], ct)) return Result.Failure<HungerFile>(HungerErrors.Forbidden);
        if (format.ToLowerInvariant() is not ("xlsx" or "csv")) return Result.Failure<HungerFile>(HungerErrors.Invalid("Use csv or xlsx."));
        var result = await QueryAccounts(filter, 1, 100, true, PermissionKeys.Hunger.PerformanceExport, ct);
        if (result.IsFailure) return Result.Failure<HungerFile>(result.Error);
        return Result.Success(TabularFile("Hunger-Accounts", format,
            ["AccountId", "ExternalAccountId", "OwnerName", "OwnerIqama", "Phone", "City", "Registry", "Settlement", "Disposition", "DeferredUntil", "ActualRider", "VehicleType", "VehiclePlate", "SimOwnership", "LastAssignmentEvent"],
            result.Value!.Items.Select(a => new object?[] { a.AccountId.ToString(), a.ExternalAccountId, a.OwnerName, a.OwnerIqama, a.PhoneNumber,
                a.OperatingCityNameAr, a.RegistryIdentifier, a.SettlementModel, a.OperationalDisposition, a.DeferredUntil?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                a.ActualRider?.NameAr, a.ActualVehicleType, a.ActualVehiclePlate, a.SimOwnership, a.LastAssignmentEvent })));
    }
}
