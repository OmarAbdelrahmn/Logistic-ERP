using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class HrFormTemplateService(ApplicationDbContext dbContext) : IHrFormTemplateService
{
    private const int MaximumDefinitionBytes = 512 * 1024;
    private const int MaximumFields = 250;

    public async Task<Result<IReadOnlyList<HrFormTemplateSummaryResponse>>> GetAllAsync(
        string? search,
        string? category,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.HrFormTemplates.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        var normalizedSearch = HrServiceSupport.TrimOrNull(search);
        if (normalizedSearch is not null)
        {
            query = query.Where(item => item.Code.Contains(normalizedSearch)
                || item.NameAr.Contains(normalizedSearch)
                || item.NameEn != null && item.NameEn.Contains(normalizedSearch));
        }

        var normalizedCategory = HrServiceSupport.TrimOrNull(category);
        if (normalizedCategory is not null)
        {
            query = query.Where(item => item.Category == normalizedCategory);
        }

        var templates = await query
            .OrderBy(item => item.Category)
            .ThenBy(item => item.NameAr)
            .ToArrayAsync(cancellationToken);
        var versions = await LoadReferencedVersionsAsync(templates, cancellationToken);

        return Result.Success<IReadOnlyList<HrFormTemplateSummaryResponse>>(
            templates.Select(item => ToSummary(item, versions)).ToArray());
    }

    public async Task<Result<HrFormTemplateResponse>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.HrFormTemplates.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return template is null
            ? Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.NotFound)
            : Result.Success(await ToResponseAsync(template, cancellationToken));
    }

    public async Task<Result<HrFormTemplateResponse>> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCode(code))
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.NotFound);
        }

        var normalizedCode = HrServiceSupport.NormalizeCode(code);
        var template = await dbContext.HrFormTemplates.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken);
        return template is null
            ? Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.NotFound)
            : Result.Success(await ToResponseAsync(template, cancellationToken));
    }

    public async Task<Result<HrFormTemplateResponse>> CreateAsync(
        HrFormTemplateCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidMetadata(request.Code, request.NameAr, request.NameEn, request.Category, request.DescriptionAr, request.DescriptionEn))
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.InvalidMetadata);
        }
        if (!TryPrepareDefinition(request.Definition, out var prepared))
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.InvalidDefinition);
        }
        if (request.ChangeNote?.Trim().Length is > 500)
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.ChangeNoteTooLong);
        }

        var normalizedCode = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.HrFormTemplates.AnyAsync(item => item.Code == normalizedCode, cancellationToken))
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.DuplicateCode);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var template = new HrFormTemplate
        {
            Code = normalizedCode,
            NameAr = request.NameAr.Trim(),
            NameEn = HrServiceSupport.TrimOrNull(request.NameEn),
            Category = request.Category.Trim(),
            DescriptionAr = HrServiceSupport.TrimOrNull(request.DescriptionAr),
            DescriptionEn = HrServiceSupport.TrimOrNull(request.DescriptionEn)
        };
        dbContext.HrFormTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        var version = CreateVersion(template.Id, 1, prepared, request.ChangeNote);
        dbContext.HrFormTemplateVersions.Add(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        template.CurrentDraftVersionId = version.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(await ToResponseAsync(template, cancellationToken));
    }

    public async Task<Result<HrFormTemplateResponse>> UpdateMetadataAsync(
        Guid id,
        HrFormTemplateMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidMetadata("VALID", request.NameAr, request.NameEn, request.Category, request.DescriptionAr, request.DescriptionEn))
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.InvalidMetadata);
        }

        var template = await dbContext.HrFormTemplates.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (template is null)
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.NotFound);
        }
        if (!HrServiceSupport.MatchesRowVersion(template.RowVersion, request.RowVersion))
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.ConcurrencyConflict);
        }

        template.NameAr = request.NameAr.Trim();
        template.NameEn = HrServiceSupport.TrimOrNull(request.NameEn);
        template.Category = request.Category.Trim();
        template.DescriptionAr = HrServiceSupport.TrimOrNull(request.DescriptionAr);
        template.DescriptionEn = HrServiceSupport.TrimOrNull(request.DescriptionEn);
        template.IsActive = request.IsActive;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.ConcurrencyConflict);
        }

        return Result.Success(await ToResponseAsync(template, cancellationToken));
    }

    public async Task<Result<HrFormTemplateVersionResponse>> CreateVersionAsync(
        Guid id,
        HrFormTemplateVersionCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryPrepareDefinition(request.Definition, out var prepared))
        {
            return Result.Failure<HrFormTemplateVersionResponse>(HrFormTemplateErrors.InvalidDefinition);
        }
        if (request.ChangeNote?.Trim().Length is > 500)
        {
            return Result.Failure<HrFormTemplateVersionResponse>(HrFormTemplateErrors.ChangeNoteTooLong);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var template = await dbContext.HrFormTemplates.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (template is null)
        {
            return Result.Failure<HrFormTemplateVersionResponse>(HrFormTemplateErrors.NotFound);
        }
        if (!HrServiceSupport.MatchesRowVersion(template.RowVersion, request.RowVersion))
        {
            return Result.Failure<HrFormTemplateVersionResponse>(HrFormTemplateErrors.ConcurrencyConflict);
        }

        var nextNumber = await dbContext.HrFormTemplateVersions
            .Where(item => item.HrFormTemplateId == id)
            .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) + 1 ?? 1;
        var version = CreateVersion(id, nextNumber, prepared, request.ChangeNote);
        dbContext.HrFormTemplateVersions.Add(version);
        template.CurrentDraftVersionId = version.Id;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<HrFormTemplateVersionResponse>(HrFormTemplateErrors.ConcurrencyConflict);
        }

        return Result.Success(ToVersionResponse(version));
    }

    public async Task<Result<IReadOnlyList<HrFormTemplateVersionResponse>>> GetVersionsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.HrFormTemplates.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<HrFormTemplateVersionResponse>>(HrFormTemplateErrors.NotFound);
        }

        var versions = await dbContext.HrFormTemplateVersions.AsNoTracking()
            .Where(item => item.HrFormTemplateId == id)
            .OrderByDescending(item => item.VersionNumber)
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<HrFormTemplateVersionResponse>>(
            versions.Select(ToVersionResponse).ToArray());
    }

    public async Task<Result<HrFormTemplateResponse>> PublishAsync(
        Guid id,
        Guid versionId,
        HrFormTemplatePublishRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.HrFormTemplates.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (template is null)
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.NotFound);
        }
        if (!HrServiceSupport.MatchesRowVersion(template.RowVersion, request.RowVersion))
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.ConcurrencyConflict);
        }
        if (!await dbContext.HrFormTemplateVersions.AsNoTracking()
            .AnyAsync(item => item.Id == versionId && item.HrFormTemplateId == id, cancellationToken))
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.VersionNotFound);
        }

        template.CurrentPublishedVersionId = versionId;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<HrFormTemplateResponse>(HrFormTemplateErrors.ConcurrencyConflict);
        }

        return Result.Success(await ToResponseAsync(template, cancellationToken));
    }

    public async Task<Result> ArchiveAsync(
        Guid id,
        ArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.HrFormTemplates.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (template is null)
        {
            return Result.Failure(HrFormTemplateErrors.NotFound);
        }
        if (string.IsNullOrWhiteSpace(request.Reason)
            || !HrServiceSupport.MatchesRowVersion(template.RowVersion, request.RowVersion))
        {
            return Result.Failure(HrFormTemplateErrors.ConcurrencyConflict);
        }

        template.DeletionReason = request.Reason.Trim();
        dbContext.HrFormTemplates.Remove(template);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(HrFormTemplateErrors.ConcurrencyConflict);
        }
        return Result.Success();
    }

    private async Task<HrFormTemplateResponse> ToResponseAsync(
        HrFormTemplate template,
        CancellationToken cancellationToken)
    {
        var versionIds = new[] { template.CurrentDraftVersionId, template.CurrentPublishedVersionId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var versions = await dbContext.HrFormTemplateVersions.AsNoTracking()
            .Where(item => versionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        return new HrFormTemplateResponse(
            ToSummary(template, versions),
            FindVersion(template.CurrentDraftVersionId, versions),
            FindVersion(template.CurrentPublishedVersionId, versions));
    }

    private async Task<IReadOnlyDictionary<Guid, HrFormTemplateVersion>> LoadReferencedVersionsAsync(
        IEnumerable<HrFormTemplate> templates,
        CancellationToken cancellationToken)
    {
        var ids = templates
            .SelectMany(item => new[] { item.CurrentDraftVersionId, item.CurrentPublishedVersionId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        return await dbContext.HrFormTemplateVersions.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
    }

    private static HrFormTemplateSummaryResponse ToSummary(
        HrFormTemplate template,
        IReadOnlyDictionary<Guid, HrFormTemplateVersion> versions) => new(
        template.Id,
        template.Code,
        template.NameAr,
        template.NameEn,
        template.Category,
        template.DescriptionAr,
        template.DescriptionEn,
        template.IsActive,
        template.CurrentDraftVersionId,
        FindEntity(template.CurrentDraftVersionId, versions)?.VersionNumber,
        template.CurrentPublishedVersionId,
        FindEntity(template.CurrentPublishedVersionId, versions)?.VersionNumber,
        HrServiceSupport.EncodeRowVersion(template.RowVersion));

    private static HrFormTemplateVersionResponse? FindVersion(
        Guid? id,
        IReadOnlyDictionary<Guid, HrFormTemplateVersion> versions)
    {
        var entity = FindEntity(id, versions);
        return entity is null ? null : ToVersionResponse(entity);
    }

    private static HrFormTemplateVersion? FindEntity(
        Guid? id,
        IReadOnlyDictionary<Guid, HrFormTemplateVersion> versions) =>
        id.HasValue && versions.TryGetValue(id.Value, out var version) ? version : null;

    private static HrFormTemplateVersionResponse ToVersionResponse(HrFormTemplateVersion version) => new(
        version.Id,
        version.HrFormTemplateId,
        version.VersionNumber,
        version.DefinitionSchemaVersion,
        JsonSerializer.Deserialize<JsonElement>(version.DefinitionJson),
        version.DefinitionSha256,
        version.ChangeNote,
        version.CreatedByUserId,
        version.CreatedAtUtc);

    private static HrFormTemplateVersion CreateVersion(
        Guid templateId,
        int versionNumber,
        PreparedDefinition definition,
        string? changeNote) => new()
    {
        HrFormTemplateId = templateId,
        VersionNumber = versionNumber,
        DefinitionSchemaVersion = definition.SchemaVersion,
        DefinitionJson = definition.Json,
        DefinitionSha256 = definition.Sha256,
        ChangeNote = HrServiceSupport.TrimOrNull(changeNote)
    };

    private static bool IsValidMetadata(
        string code,
        string nameAr,
        string? nameEn,
        string category,
        string? descriptionAr,
        string? descriptionEn) =>
        IsValidCode(code)
        && !string.IsNullOrWhiteSpace(nameAr) && nameAr.Trim().Length <= 200
        && nameEn?.Trim().Length is not > 200
        && !string.IsNullOrWhiteSpace(category) && category.Trim().Length <= 100
        && descriptionAr?.Trim().Length is not > 2000
        && descriptionEn?.Trim().Length is not > 2000;

    private static bool IsValidCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length is < 2 or > 100)
        {
            return false;
        }

        return code.Trim().All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    }

    private static bool TryPrepareDefinition(JsonElement definition, out PreparedDefinition prepared)
    {
        prepared = default;
        if (definition.ValueKind != JsonValueKind.Object
            || !definition.TryGetProperty("schemaVersion", out var schemaElement)
            || !schemaElement.TryGetInt32(out var schemaVersion)
            || schemaVersion != 1
            || !HasAllowedString(definition, "direction", ["rtl", "ltr"])
            || !definition.TryGetProperty("page", out var page)
            || !IsValidPage(page)
            || !definition.TryGetProperty("sections", out var sections)
            || sections.ValueKind != JsonValueKind.Object
            || !sections.TryGetProperty("body", out var body)
            || body.ValueKind != JsonValueKind.Object
            || !IsValidFields(definition))
        {
            return false;
        }

        var json = JsonSerializer.Serialize(definition);
        if (Encoding.UTF8.GetByteCount(json) > MaximumDefinitionBytes)
        {
            return false;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        prepared = new PreparedDefinition(schemaVersion, json, hash);
        return true;
    }

    private static bool IsValidPage(JsonElement page)
    {
        if (page.ValueKind != JsonValueKind.Object
            || !HasAllowedString(page, "size", ["A4", "A5", "Letter", "Custom"])
            || !HasAllowedString(page, "orientation", ["portrait", "landscape"])
            || !page.TryGetProperty("marginsMm", out var margins)
            || margins.ValueKind != JsonValueKind.Object
            || !HasNumberInRange(margins, "top", 0, 100)
            || !HasNumberInRange(margins, "right", 0, 100)
            || !HasNumberInRange(margins, "bottom", 0, 100)
            || !HasNumberInRange(margins, "left", 0, 100))
        {
            return false;
        }

        var size = page.GetProperty("size").GetString();
        return size != "Custom"
            || HasNumberInRange(page, "widthMm", 50, 1000)
            && HasNumberInRange(page, "heightMm", 50, 1000);
    }

    private static bool IsValidFields(JsonElement definition)
    {
        if (!definition.TryGetProperty("fields", out var fields))
        {
            return true;
        }
        if (fields.ValueKind != JsonValueKind.Array || fields.GetArrayLength() > MaximumFields)
        {
            return false;
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields.EnumerateArray())
        {
            if (field.ValueKind != JsonValueKind.Object
                || !TryGetShortString(field, "key", 100, out var key)
                || !TryGetShortString(field, "type", 50, out _)
                || !TryGetShortString(field, "source", 50, out _)
                || !keys.Add(key))
            {
                return false;
            }
        }
        return true;
    }

    private static bool HasAllowedString(JsonElement parent, string name, IReadOnlyCollection<string> allowed) =>
        parent.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { } text
        && allowed.Contains(text, StringComparer.OrdinalIgnoreCase);

    private static bool HasNumberInRange(JsonElement parent, string name, double minimum, double maximum) =>
        parent.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetDouble(out var number)
        && double.IsFinite(number)
        && number >= minimum
        && number <= maximum;

    private static bool TryGetShortString(JsonElement parent, string name, int maximumLength, out string value)
    {
        value = string.Empty;
        if (!parent.TryGetProperty(name, out var element)
            || element.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(element.GetString()))
        {
            return false;
        }
        value = element.GetString()!.Trim();
        return value.Length <= maximumLength;
    }

    private readonly record struct PreparedDefinition(int SchemaVersion, string Json, string Sha256);
}
