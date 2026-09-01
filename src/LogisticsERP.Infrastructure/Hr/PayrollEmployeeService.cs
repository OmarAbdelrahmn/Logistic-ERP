using System.Text.RegularExpressions;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class PayrollEmployeeService(ApplicationDbContext dbContext) : IPayrollEmployeeService
{
    public async Task<Result<IReadOnlyList<PayrollEmployeeResponse>>> GetAllAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PayrollEmployees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.Name.Contains(term)
                || item.NationalId.Contains(term)
                || item.Country.Contains(term)
                || item.PersonalIban.Contains(term)
                || item.Status.Contains(term));
        }

        var items = await (
            from item in query
            join sponsor in dbContext.Sponsors.AsNoTracking()
                on item.SponsorId equals sponsor.Id
            orderby item.Number
            select ToResponse(item, sponsor))
            .ToArrayAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PayrollEmployeeResponse>>(items);
    }

    public async Task<Result<PayrollEmployeeResponse>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await (
            from item in dbContext.PayrollEmployees.AsNoTracking()
            join sponsor in dbContext.Sponsors.AsNoTracking()
                on item.SponsorId equals sponsor.Id
            where item.Id == id
            select new PayrollEmployeeProjection(item, sponsor))
            .SingleOrDefaultAsync(cancellationToken);

        return result is null
            ? Result.Failure<PayrollEmployeeResponse>(PayrollEmployeeErrors.NotFound)
            : Result.Success(ToResponse(result.Employee, result.Sponsor));
    }

    public async Task<Result<PayrollEmployeeResponse>> CreateAsync(
        CreatePayrollEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateAndNormalize(
            request.Number,
            request.SponsorId,
            request.Name,
            request.NationalId,
            request.Country,
            request.JoiningDate,
            request.PersonalIban,
            request.Salary,
            request.Status);
        if (validation.IsFailure)
        {
            return Result.Failure<PayrollEmployeeResponse>(validation.Error);
        }

        var sponsor = await FindActiveSponsorAsync(validation.Value!.SponsorId, cancellationToken);
        if (sponsor is null)
        {
            return Result.Failure<PayrollEmployeeResponse>(PayrollEmployeeErrors.SponsorNotFound);
        }

        var duplicate = await FindDuplicateAsync(
            Guid.Empty,
            validation.Value.Number,
            validation.Value.NationalId,
            validation.Value.PersonalIban,
            cancellationToken);
        if (duplicate is not null)
        {
            return Result.Failure<PayrollEmployeeResponse>(duplicate);
        }

        var item = new PayrollEmployee();
        Apply(item, validation.Value);
        dbContext.PayrollEmployees.Add(item);

        return await SaveAsync(item, sponsor, cancellationToken);
    }

    public async Task<Result<PayrollEmployeeResponse>> UpdateAsync(
        Guid id,
        UpdatePayrollEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.PayrollEmployees
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (item is null)
        {
            return Result.Failure<PayrollEmployeeResponse>(PayrollEmployeeErrors.NotFound);
        }

        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
        {
            return Result.Failure<PayrollEmployeeResponse>(PayrollEmployeeErrors.ConcurrencyConflict);
        }

        var validation = ValidateAndNormalize(
            request.Number,
            request.SponsorId,
            request.Name,
            request.NationalId,
            request.Country,
            request.JoiningDate,
            request.PersonalIban,
            request.Salary,
            request.Status);
        if (validation.IsFailure)
        {
            return Result.Failure<PayrollEmployeeResponse>(validation.Error);
        }

        var sponsor = await FindActiveSponsorAsync(validation.Value!.SponsorId, cancellationToken);
        if (sponsor is null)
        {
            return Result.Failure<PayrollEmployeeResponse>(PayrollEmployeeErrors.SponsorNotFound);
        }

        var duplicate = await FindDuplicateAsync(
            id,
            validation.Value.Number,
            validation.Value.NationalId,
            validation.Value.PersonalIban,
            cancellationToken);
        if (duplicate is not null)
        {
            return Result.Failure<PayrollEmployeeResponse>(duplicate);
        }

        Apply(item, validation.Value);
        return await SaveAsync(item, sponsor, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        string rowVersion,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.PayrollEmployees
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (item is null)
        {
            return Result.Failure(PayrollEmployeeErrors.NotFound);
        }

        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, rowVersion))
        {
            return Result.Failure(PayrollEmployeeErrors.ConcurrencyConflict);
        }

        item.DeletionReason = string.IsNullOrWhiteSpace(reason)
            ? "Deleted through payroll employee CRUD."
            : reason.Trim();
        dbContext.PayrollEmployees.Remove(item);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(PayrollEmployeeErrors.ConcurrencyConflict);
        }
    }

    private async Task<Result<PayrollEmployeeResponse>> SaveAsync(
        PayrollEmployee item,
        Sponsor sponsor,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success(ToResponse(item, sponsor));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<PayrollEmployeeResponse>(PayrollEmployeeErrors.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<PayrollEmployeeResponse>(PayrollEmployeeErrors.PersistenceConflict);
        }
    }

    private async Task<OperationError?> FindDuplicateAsync(
        Guid id,
        int number,
        string nationalId,
        string personalIban,
        CancellationToken cancellationToken)
    {
        var duplicates = await dbContext.PayrollEmployees
            .AsNoTracking()
            .Where(item => item.Id != id && (
                item.Number == number
                || item.NationalId == nationalId
                || item.PersonalIban == personalIban))
            .Select(item => new { item.Number, item.NationalId, item.PersonalIban })
            .ToArrayAsync(cancellationToken);

        if (duplicates.Any(item => item.Number == number))
        {
            return PayrollEmployeeErrors.DuplicateNumber;
        }

        if (duplicates.Any(item => item.NationalId == nationalId))
        {
            return PayrollEmployeeErrors.DuplicateNationalId;
        }

        return duplicates.Any(item => item.PersonalIban == personalIban)
            ? PayrollEmployeeErrors.DuplicateIban
            : null;
    }

    private static Result<NormalizedPayrollEmployee> ValidateAndNormalize(
        int number,
        Guid sponsorId,
        string name,
        string nationalId,
        string country,
        DateOnly joiningDate,
        string personalIban,
        decimal salary,
        string status)
    {
        if (number <= 0
            || sponsorId == Guid.Empty
            || string.IsNullOrWhiteSpace(name)
            || name.Trim().Length > 200
            || string.IsNullOrWhiteSpace(country)
            || country.Trim().Length > 100
            || joiningDate == default
            || salary < 0
            || status is null
            || status.Trim().Length > 100)
        {
            return Result.Failure<NormalizedPayrollEmployee>(PayrollEmployeeErrors.InvalidRequest);
        }

        var normalizedNationalId = nationalId?.Trim() ?? string.Empty;
        if (!NationalIdRegex().IsMatch(normalizedNationalId))
        {
            return Result.Failure<NormalizedPayrollEmployee>(PayrollEmployeeErrors.InvalidNationalId);
        }

        var normalizedIban = new string((personalIban ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character))
            .ToArray())
            .ToUpperInvariant();
        if (!SaudiIbanRegex().IsMatch(normalizedIban))
        {
            return Result.Failure<NormalizedPayrollEmployee>(PayrollEmployeeErrors.InvalidIban);
        }

        return Result.Success(new NormalizedPayrollEmployee(
            number,
            sponsorId,
            name.Trim(),
            normalizedNationalId,
            country.Trim(),
            joiningDate,
            normalizedIban,
            salary,
            status.Trim()));
    }

    private static void Apply(PayrollEmployee item, NormalizedPayrollEmployee value)
    {
        item.Number = value.Number;
        item.SponsorId = value.SponsorId;
        item.Name = value.Name;
        item.NationalId = value.NationalId;
        item.Country = value.Country;
        item.JoiningDate = value.JoiningDate;
        item.PersonalIban = value.PersonalIban;
        item.Salary = value.Salary;
        item.Status = value.Status;
    }

    private async Task<Sponsor?> FindActiveSponsorAsync(Guid sponsorId, CancellationToken cancellationToken) =>
        await dbContext.Sponsors
            .AsNoTracking()
            .SingleOrDefaultAsync(
                sponsor => sponsor.Id == sponsorId && sponsor.Status == CatalogStatus.Active,
                cancellationToken);

    private static PayrollEmployeeResponse ToResponse(PayrollEmployee item, Sponsor sponsor) => new(
        item.Id,
        item.Number,
        item.SponsorId,
        new PayrollEmployeeSponsorResponse(
            sponsor.Id,
            sponsor.EmployerIdentityNumber,
            sponsor.RegistryNameAr,
            sponsor.RegistryNameEn),
        item.Name,
        item.NationalId,
        item.Country,
        item.JoiningDate,
        item.PersonalIban,
        item.Salary,
        item.Status,
        HrServiceSupport.EncodeRowVersion(item.RowVersion));

    [GeneratedRegex("^[0-9]{10}$", RegexOptions.CultureInvariant)]
    private static partial Regex NationalIdRegex();

    [GeneratedRegex("^SA[0-9]{22}$", RegexOptions.CultureInvariant)]
    private static partial Regex SaudiIbanRegex();

    private sealed record NormalizedPayrollEmployee(
        int Number,
        Guid SponsorId,
        string Name,
        string NationalId,
        string Country,
        DateOnly JoiningDate,
        string PersonalIban,
        decimal Salary,
        string Status);

    private sealed record PayrollEmployeeProjection(PayrollEmployee Employee, Sponsor Sponsor);
}
