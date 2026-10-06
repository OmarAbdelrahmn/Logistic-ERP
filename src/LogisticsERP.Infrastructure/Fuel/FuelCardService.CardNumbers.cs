using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fuel;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fuel;

internal sealed partial class FuelCardService
{
    public async Task<Result<FuelCardNumberImportResponse>> ImportCardNumbersAsync(
        Stream content, Guid sponsorId, bool validateOnly, Guid? operatingCityId = null, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Import, cancellationToken))
            return Result.Failure<FuelCardNumberImportResponse>(FuelErrors.Forbidden);
        if (!await SponsorExistsAsync(sponsorId, cancellationToken))
            return Result.Failure<FuelCardNumberImportResponse>(FuelErrors.SponsorNotFound);
        var cityId = operatingCityId ?? OperatingCity.JeddahId;
        if (!await OperatingCityExistsAsync(cityId, cancellationToken))
            return Result.Failure<FuelCardNumberImportResponse>(FuelErrors.OperatingCityNotFound);
        if (content is null || !content.CanRead)
            return Result.Failure<FuelCardNumberImportResponse>(FuelErrors.InvalidFile);

        var rows = new List<(int RowNumber, string Number, string Normalized)>();
        try
        {
            using var workbook = new XLWorkbook(content);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null || !string.Equals(sheet.Cell(1, 1).GetString().Trim(), "number", StringComparison.OrdinalIgnoreCase)
                || sheet.Row(1).CellsUsed().Count() != 1)
                return Result.Failure<FuelCardNumberImportResponse>(FuelErrors.InvalidFile);

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var row = 2; row <= lastRow; row++)
            {
                var number = FuelCardRules.RemoveCardNumberWhitespace(sheet.Cell(row, 1).GetString());
                if (number.Length == 0 && !sheet.Row(row).CellsUsed().Any()) continue;
                string normalized;
                try { normalized = FuelCardRules.NormalizeCardNumber(number, FuelCardIdentifierType.InternalNumber); }
                catch (ArgumentException) { normalized = ""; }
                rows.Add((row, number, normalized));
            }
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException or FormatException or IOException)
        {
            return Result.Failure<FuelCardNumberImportResponse>(FuelErrors.InvalidFile);
        }

        var existing = await dbContext.FuelCards.AsNoTracking()
            .Where(card => card.Provider == FuelCardProvider.PetroApp)
            .Select(card => card.NormalizedCardNumber)
            .ToArrayAsync(cancellationToken);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);
        var issues = new List<FuelCardNumberImportIssue>();
        var newCards = new List<FuelCard>();
        var existingCount = 0;
        foreach (var row in rows)
        {
            if (row.Number.Length == 0 || row.Number.Length > 100 || row.Normalized.Length < 2)
                issues.Add(new(row.RowNumber, row.Number, "رقم البطاقة غير صالح."));
            else if (!seen.Add(row.Normalized))
                issues.Add(new(row.RowNumber, row.Number, "رقم البطاقة مكرر في الملف."));
            else if (existingSet.Contains(row.Normalized))
                existingCount++;
            else
                newCards.Add(new FuelCard
                {
                    SponsorId = sponsorId,
                    OperatingCityId = cityId,
                    Provider = FuelCardProvider.PetroApp,
                    IdentifierType = FuelCardIdentifierType.InternalNumber,
                    CardNumber = row.Number,
                    NormalizedCardNumber = row.Normalized
                });
        }

        var canImport = rows.Count > 0 && issues.Count == 0;
        if (canImport && !validateOnly)
        {
            dbContext.FuelCards.AddRange(newCards);
            var saved = await SaveAsync(cancellationToken);
            if (saved.IsFailure)
            {
                dbContext.ChangeTracker.Clear();
                return Result.Failure<FuelCardNumberImportResponse>(saved.Error);
            }
        }
        return Result.Success(new FuelCardNumberImportResponse(validateOnly, canImport, rows.Count,
            newCards.Count, existingCount, issues));
    }
}
