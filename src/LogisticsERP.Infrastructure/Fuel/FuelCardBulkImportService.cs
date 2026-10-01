using System.Text;
using ClosedXML.Excel;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fuel;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fuel;

internal sealed class FuelCardBulkImportService(ApplicationDbContext dbContext) : IFuelCardBulkImportService
{
    public async Task<Result<FuelCardBulkImportResponse>> ImportAsync(
        Stream content, bool validateOnly, CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            return Result.Failure<FuelCardBulkImportResponse>(FuelErrors.InvalidFile);

        List<ParsedRow> rows;
        try
        {
            rows = Parse(content);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException or FormatException or IOException)
        {
            return Result.Failure<FuelCardBulkImportResponse>(FuelErrors.InvalidFile);
        }

        var sponsors = await dbContext.Sponsors.AsNoTracking().ToArrayAsync(cancellationToken);
        var sponsorsBy70 = sponsors
            .GroupBy(sponsor => Normalize70Number(sponsor.EmployerIdentityNumber), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var cardNumbers = rows.Select(row => row.NormalizedCardNumber)
            .Where(number => number.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
        var existing = await dbContext.FuelCards.AsNoTracking()
            .Where(card => cardNumbers.Contains(card.NormalizedCardNumber))
            .Select(card => new { card.Provider, card.NormalizedCardNumber, card.SponsorId })
            .ToArrayAsync(cancellationToken);
        var existingByKey = existing.ToDictionary(
            card => (card.Provider, card.NormalizedCardNumber), card => card.SponsorId);

        var seen = new HashSet<(FuelCardProvider Provider, string Number)>();
        var issues = new List<FuelCardBulkImportIssue>();
        var previews = new List<FuelCardBulkImportRow>();
        var newCards = new List<FuelCard>();
        var existingCount = 0;
        foreach (var row in rows)
        {
            if (row.NormalizedCardNumber.Length == 0)
            {
                issues.Add(new(row.RowNumber, row.CardNumber, "رقم بطاقة الوقود غير صالح."));
                continue;
            }
            if (!TryParseCompany(row.CompanyName, out var provider))
            {
                issues.Add(new(row.RowNumber, row.CardNumber, "اسم شركة الوقود غير مدعوم؛ استخدم بترو اب أو سياره كار."));
                continue;
            }
            var sponsor70 = Normalize70Number(row.Sponsor70Number);
            if (sponsor70.Length != 10 || !sponsor70.StartsWith("70", StringComparison.Ordinal)
                || !sponsor70.All(char.IsAsciiDigit)
                || !sponsorsBy70.TryGetValue(sponsor70, out var sponsor))
            {
                issues.Add(new(row.RowNumber, row.CardNumber, "رقم الكفيل 70 غير موجود في سجل الكفلاء."));
                continue;
            }
            if (!seen.Add((provider, row.NormalizedCardNumber)))
            {
                issues.Add(new(row.RowNumber, row.CardNumber, "رقم البطاقة مكرر لدى شركة الوقود نفسها في الملف."));
                continue;
            }

            var isExisting = existingByKey.TryGetValue((provider, row.NormalizedCardNumber), out var existingSponsorId);
            if (isExisting && existingSponsorId != sponsor.Id)
            {
                issues.Add(new(row.RowNumber, row.CardNumber, "البطاقة موجودة ومسجلة لدى كفيل آخر."));
                continue;
            }
            previews.Add(new(row.RowNumber, row.CardNumber, sponsor70, sponsor.Id,
                sponsor.RegistryNameAr, row.CompanyName, provider.ToString(), !isExisting));
            if (isExisting)
            {
                existingCount++;
                continue;
            }
            newCards.Add(new FuelCard
            {
                SponsorId = sponsor.Id,
                Provider = provider,
                IdentifierType = FuelCardIdentifierType.InternalNumber,
                CardNumber = row.CardNumber,
                NormalizedCardNumber = row.NormalizedCardNumber
            });
        }

        var canImport = rows.Count > 0 && issues.Count == 0;
        if (canImport && !validateOnly)
        {
            dbContext.FuelCards.AddRange(newCards);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                dbContext.ChangeTracker.Clear();
                return Result.Failure<FuelCardBulkImportResponse>(FuelErrors.PersistenceConflict);
            }
        }
        return Result.Success(new FuelCardBulkImportResponse(validateOnly, canImport,
            !validateOnly && canImport, rows.Count, newCards.Count, existingCount, previews, issues));
    }

    private static List<ParsedRow> Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var sheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("The workbook has no worksheet.");
        var headers = sheet.Row(1).CellsUsed()
            .ToDictionary(cell => NormalizeHeader(cell.GetString()), cell => cell.Address.ColumnNumber);
        if (headers.Count != 3 || !headers.TryGetValue("number", out var numberColumn))
            throw new InvalidDataException("The workbook headers are invalid.");
        var sponsorColumn = headers.FirstOrDefault(pair => pair.Key is
            "sponsor70number" or "70number" or "رقمالكفيل70" or "رقم70").Value;
        var companyColumn = headers.FirstOrDefault(pair => pair.Key is "companyname" or "اسمالشركة").Value;
        if (sponsorColumn == 0 || companyColumn == 0)
            throw new InvalidDataException("The workbook headers are invalid.");

        var rows = new List<ParsedRow>();
        for (var rowNumber = 2; rowNumber <= (sheet.LastRowUsed()?.RowNumber() ?? 1); rowNumber++)
        {
            if (!sheet.Row(rowNumber).CellsUsed().Any()) continue;
            var cardNumber = sheet.Cell(rowNumber, numberColumn).GetString().Trim();
            string normalized;
            try { normalized = FuelCardRules.NormalizeCardNumber(cardNumber, FuelCardIdentifierType.InternalNumber); }
            catch (ArgumentException) { normalized = ""; }
            rows.Add(new ParsedRow(rowNumber, cardNumber, normalized,
                sheet.Cell(rowNumber, sponsorColumn).GetString().Trim(),
                sheet.Cell(rowNumber, companyColumn).GetString().Trim()));
        }
        return rows;
    }

    private static string NormalizeHeader(string value) => new(value.Trim()
        .Normalize(NormalizationForm.FormKC).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string Normalize70Number(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        try { return FuelCardRules.NormalizeCardNumber(value, FuelCardIdentifierType.InternalNumber); }
        catch (ArgumentException) { return ""; }
    }

    private static bool TryParseCompany(string value, out FuelCardProvider provider)
    {
        var name = NormalizeHeader(value).Replace('ة', 'ه').Replace('أ', 'ا').Replace('إ', 'ا');
        provider = name switch
        {
            "بترواب" or "شركةبترواب" or "petroapp" => FuelCardProvider.PetroApp,
            "سيارهكار" or "شركةسيارهكار" or "sayaraapp" => FuelCardProvider.SayaraApp,
            _ => default
        };
        return provider != default;
    }

    private sealed record ParsedRow(int RowNumber, string CardNumber, string NormalizedCardNumber,
        string Sponsor70Number, string CompanyName);
}
