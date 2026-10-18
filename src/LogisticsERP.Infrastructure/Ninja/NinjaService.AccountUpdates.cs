using System.Security.Cryptography;
using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    public async Task<Result<NinjaAccountUpdateUploadResponse>> UploadAccountSheetAsync(string fileName, byte[] content, string? note, CancellationToken ct = default)
    {
        var platforms = await AllowedPlatforms(PermissionKeys.Ninja.AccountsUpdate, ct);
        if (platforms.Length == 0) return Result.Failure<NinjaAccountUpdateUploadResponse>(NinjaErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255 || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || content.Length is 0 or > NinjaSpreadsheetParser.MaxBytes || note?.Trim().Length > 4000)
            return Result.Failure<NinjaAccountUpdateUploadResponse>(NinjaErrors.Invalid("ارفع ملف xlsx بحد أقصى 10 ميجابايت وملاحظة بحد أقصى 4000 حرف."));
        var parsed = NinjaAccountUpdateSheetParser.Parse(content, ct);
        if (parsed.Issues.Count > 0)
            return Result.Failure<NinjaAccountUpdateUploadResponse>(new("ninja.account_sheet_invalid", "تعذر حفظ الملف؛ صحح أخطاء الصفوف ثم أعد رفعه.",
                ErrorType.Validation, Details: new Dictionary<string, object?> { ["issues"] = parsed.Issues }));
        var platformId = platforms.Single();
        var accounts = await AccountUpdateSystemQuery(platformId).Take(NinjaAccountSheetParser.MaxAccounts + 1).ToArrayAsync(ct);
        if (accounts.Length > NinjaAccountSheetParser.MaxAccounts)
            return Result.Failure<NinjaAccountUpdateUploadResponse>(NinjaErrors.Invalid("المراجعة تدعم حتى 10000 حساب في النظام."));
        var byCaptain = accounts.ToLookup(a => NinjaSpreadsheetParser.NormalizeId(a.CaptainId), StringComparer.Ordinal);
        var upload = new NinjaAccountUpdateUpload { ClientPlatformId = platformId, FileName = Path.GetFileName(fileName),
            SheetName = parsed.SheetName, ContentHash = Convert.ToHexString(SHA256.HashData(content)),
            Note = HrServiceSupport.TrimOrNull(note), IgnoredFooterRows = parsed.IgnoredFooterRows };
        var rows = new List<NinjaAccountUpdateRow>();
        foreach (var source in parsed.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var matches = byCaptain[source.CaptainId].ToArray();
            var system = matches.Length == 1 ? matches[0] : null;
            var matchStatus = matches.Length switch { 0 => "NotFound", 1 => "Found", _ => "Ambiguous" };
            var problems = AccountUpdateProblems(source, system, matchStatus);
            rows.Add(new() { UploadId = upload.Id, SourceRowNumber = source.SourceRowNumber, CaptainId = source.CaptainId,
                SheetOwnerName = source.OwnerName, SheetCity = source.City, SheetIqamaNo = source.IqamaNo,
                SheetStatus = source.Status, SheetSuspended = source.Suspended, PlatformRiderAccountId = system?.AccountId,
                MatchStatus = matchStatus, HasProblems = problems.Count > 0,
                CityMismatch = problems.Any(p => p.Code is "CityMismatch" or "SystemCityMissing" or "SheetCityMissing"),
                IqamaProblem = problems.Any(p => p.Code is "SheetIqamaInvalid" or "OwnerMissing" or "OwnerIqamaMissing" or "IqamaMismatch" or "OwnerNotWorking"),
                NaqlBlocked = string.Equals(source.Status, "NAQL_BLOCKED", StringComparison.OrdinalIgnoreCase),
                SystemSnapshotJson = system is null ? null : JsonSerializer.Serialize(system), ProblemsJson = JsonSerializer.Serialize(problems) });
        }
        db.NinjaAccountUpdateUploads.Add(upload); db.NinjaAccountUpdateRows.AddRange(rows);
        var saved = await Persist(true, ct);
        if (saved.IsFailure) return Result.Failure<NinjaAccountUpdateUploadResponse>(saved.Error);
        // Saving the report never changes ERP ownership, city, operational status, assignments or credentials.
        return Result.Success((await UploadResponses([upload], ct))[0]);
    }

    internal IQueryable<NinjaAccountUpdateSystemAccount> AccountUpdateSystemQuery(Guid platformId) =>
        from a in Accounts.AsNoTracking()
        join owner in db.Employees.AsNoTracking() on a.RegisteredEmployeeId equals owner.Id into owners
        from owner in owners.DefaultIfEmpty()
        join city in db.OperatingCities.AsNoTracking() on a.OperatingCityId equals city.Id into cities
        from city in cities.DefaultIfEmpty()
        join global in db.GlobalCities.AsNoTracking() on city.GlobalCityId equals global.Id into globals
        from global in globals.DefaultIfEmpty()
        where a.ClientPlatformId == platformId
        orderby a.ExternalAccountId, a.Id
        select new NinjaAccountUpdateSystemAccount(a.Id, a.ExternalAccountId, a.Code, a.Status.ToString(), a.RegisteredEmployeeId,
            owner == null ? null : owner.FullNameAr, owner == null ? null : owner.FullNameEn,
            owner == null ? null : owner.IqamaNo, owner == null ? null : owner.Status.ToString(), a.OperatingCityId,
            global == null ? null : global.Code, global == null ? null : global.NameAr, global == null ? null : global.NameEn);

    private static List<NinjaAccountUpdateProblem> AccountUpdateProblems(NinjaParsedAccountUpdateRow row,
        NinjaAccountUpdateSystemAccount? system, string matchStatus)
    {
        var problems = new List<NinjaAccountUpdateProblem>();
        void Add(string code, string ar, string en) => problems.Add(new(code, ar, en));
        if (matchStatus == "NotFound") Add("AccountNotFound", "الحساب موجود في الملف وغير موجود في حسابات نينجا بالنظام.", "This sheet account was not found in the ERP Ninja accounts.");
        if (matchStatus == "Ambiguous") Add("AccountAmbiguous", "المعرف يطابق أكثر من حساب في النظام؛ لم يتم اختيار حساب.", "The identifier matches multiple ERP accounts; no account was selected.");
        if (row.IqamaNo.Length != 10 || row.IqamaNo.Any(c => c is < '0' or > '9'))
            Add("SheetIqamaInvalid", "رقم الهوية في الملف غير صالح؛ يجب أن يتكون من 10 أرقام.", "The sheet iqama must contain ten digits.");
        if (row.City.Length == 0) Add("SheetCityMissing", "المدينة غير مسجلة في الملف.", "The sheet city is missing.");
        if (row.Status.Length == 0) Add("SheetStatusMissing", "حالة الحساب غير مسجلة في الملف.", "The sheet account status is missing.");
        if (string.Equals(row.Status, "NAQL_BLOCKED", StringComparison.OrdinalIgnoreCase))
            Add("NaqlBlocked", "حالة الملف NAQL_BLOCKED: الحساب محظور في نقل حسب نينجا؛ يلزم مراجعة السبب.", "The sheet reports NAQL_BLOCKED; review the provider's Naql block reason.");
        if (row.Suspended == true) Add("SheetSuspended", "الحساب معلّم كموقوف في الملف.", "The sheet marks the account as suspended.");
        if (system is null) return problems;
        if (system.CityCode is null && system.CityNameAr is null && system.CityNameEn is null)
            Add("SystemCityMissing", "مدينة الحساب غير متاحة في النظام.", "The ERP account city is unavailable.");
        else if (row.City.Length > 0 && !Same(row.City, system.CityCode) && !Same(row.City, system.CityNameAr) && !Same(row.City, system.CityNameEn))
            Add("CityMismatch", "مدينة الملف تختلف عن مدينة الحساب في النظام.", "The sheet city differs from the ERP account city.");
        if (system.OwnerStatus is null)
            Add("OwnerMissing", "صاحب الحساب غير موجود أو غير متاح في النظام؛ لا يمكن التحقق من هويته.", "The registered owner is missing or unavailable; the iqama cannot be checked.");
        else
        {
            if (string.IsNullOrWhiteSpace(system.OwnerIqamaNo)) Add("OwnerIqamaMissing", "رقم هوية صاحب الحساب غير مسجل في النظام.", "The registered owner's ERP iqama is missing.");
            else if (row.IqamaNo != NinjaSpreadsheetParser.NormalizeId(system.OwnerIqamaNo))
                Add("IqamaMismatch", "هوية الملف لا تطابق هوية صاحب الحساب في النظام.", "The sheet iqama differs from the registered owner's ERP iqama.");
            if (system.OwnerStatus != nameof(EmployeeStatus.Active))
                Add("OwnerNotWorking", $"حالة صاحب الحساب في النظام ليست Active: {system.OwnerStatus}.", $"The registered owner's ERP status is {system.OwnerStatus}, not Active.");
        }
        return problems;
    }

    public async Task<Result<NinjaAccountUpdatePage<NinjaAccountUpdateUploadResponse>>> GetAccountSheetUploadsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (!ValidPage(page, pageSize)) return Result.Failure<NinjaAccountUpdatePage<NinjaAccountUpdateUploadResponse>>(NinjaErrors.Invalid("حجم الصفحة من 1 إلى 100."));
        var platforms = await AllowedPlatforms(PermissionKeys.Ninja.Read, ct);
        if (platforms.Length == 0) return Result.Failure<NinjaAccountUpdatePage<NinjaAccountUpdateUploadResponse>>(NinjaErrors.Forbidden);
        var query = db.NinjaAccountUpdateUploads.AsNoTracking().Where(u => platforms.Contains(u.ClientPlatformId));
        var count = await query.CountAsync(ct);
        var uploads = await query.OrderByDescending(u => u.CreatedAtUtc).ThenByDescending(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new NinjaAccountUpdatePage<NinjaAccountUpdateUploadResponse>(await UploadResponses(uploads, ct), page, pageSize, count));
    }

    public async Task<Result<NinjaAccountUpdateUploadResponse>> GetAccountSheetUploadAsync(Guid uploadId, CancellationToken ct = default)
    {
        var upload = await AccountUpdateUpload(uploadId, PermissionKeys.Ninja.Read, ct);
        return upload.IsFailure ? Result.Failure<NinjaAccountUpdateUploadResponse>(upload.Error)
            : Result.Success((await UploadResponses([upload.Value!], ct))[0]);
    }

    public async Task<Result<NinjaAccountUpdatePage<NinjaAccountUpdateRowResponse>>> GetAccountSheetRowsAsync(Guid uploadId, string? matchStatus,
        string? sheetStatus, bool problemsOnly, int page, int pageSize, CancellationToken ct = default)
    {
        if (!ValidPage(page, pageSize) || matchStatus is not (null or "Found" or "NotFound" or "Ambiguous") || sheetStatus?.Length > 100)
            return Result.Failure<NinjaAccountUpdatePage<NinjaAccountUpdateRowResponse>>(NinjaErrors.Invalid("تحقق من حالة المطابقة والفلاتر وحجم الصفحة من 1 إلى 100."));
        var upload = await AccountUpdateUpload(uploadId, PermissionKeys.Ninja.Read, ct);
        if (upload.IsFailure) return Result.Failure<NinjaAccountUpdatePage<NinjaAccountUpdateRowResponse>>(upload.Error);
        var query = db.NinjaAccountUpdateRows.AsNoTracking().Where(r => r.UploadId == uploadId
            && (matchStatus == null || r.MatchStatus == matchStatus) && (sheetStatus == null || r.SheetStatus == sheetStatus)
            && (!problemsOnly || r.HasProblems));
        var count = await query.CountAsync(ct);
        var rows = await query.OrderBy(r => r.SourceRowNumber).ThenBy(r => r.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new NinjaAccountUpdatePage<NinjaAccountUpdateRowResponse>(rows.Select(r => new NinjaAccountUpdateRowResponse(
            r.Id, r.SourceRowNumber, r.CaptainId, r.SheetOwnerName, r.SheetCity, r.SheetIqamaNo, r.SheetStatus, r.SheetSuspended,
            r.MatchStatus, r.SystemSnapshotJson is null ? null : JsonSerializer.Deserialize<NinjaAccountUpdateSystemAccount>(r.SystemSnapshotJson),
            JsonSerializer.Deserialize<NinjaAccountUpdateProblem[]>(r.ProblemsJson) ?? [])).ToArray(), page, pageSize, count));
    }

    public async Task<Result<NinjaAccountUpdateUploadResponse>> UpdateAccountSheetNoteAsync(Guid uploadId, NinjaAccountUpdateNoteRequest request, CancellationToken ct = default)
    {
        var result = await AccountUpdateUpload(uploadId, PermissionKeys.Ninja.AccountsUpdate, ct);
        if (result.IsFailure) return Result.Failure<NinjaAccountUpdateUploadResponse>(result.Error);
        var upload = await db.NinjaAccountUpdateUploads.SingleAsync(u => u.Id == uploadId, ct);
        if (request.Note?.Trim().Length > 4000) return Result.Failure<NinjaAccountUpdateUploadResponse>(NinjaErrors.Invalid("الملاحظة بحد أقصى 4000 حرف."));
        if (!HrServiceSupport.MatchesRowVersion(upload.RowVersion, request.RowVersion)) return Result.Failure<NinjaAccountUpdateUploadResponse>(NinjaErrors.Conflict);
        upload.Note = HrServiceSupport.TrimOrNull(request.Note);
        db.Entry(upload).Property(u => u.Note).IsModified = true;
        var saved = await Persist(true, ct);
        return saved.IsFailure ? Result.Failure<NinjaAccountUpdateUploadResponse>(saved.Error)
            : Result.Success((await UploadResponses([upload], ct))[0]);
    }

    private async Task<Result<NinjaAccountUpdateUpload>> AccountUpdateUpload(Guid id, string permission, CancellationToken ct)
    {
        var upload = await db.NinjaAccountUpdateUploads.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);
        if (upload is null) return Result.Failure<NinjaAccountUpdateUpload>(NinjaErrors.NotFound);
        if (!await db.ClientPlatforms.AnyAsync(p => p.Id == upload.ClientPlatformId && p.Code == "NINJA", ct)
            || !await Allowed(permission, upload.ClientPlatformId, ct)) return Result.Failure<NinjaAccountUpdateUpload>(NinjaErrors.Forbidden);
        return Result.Success(upload);
    }

    private async Task<NinjaAccountUpdateUploadResponse[]> UploadResponses(NinjaAccountUpdateUpload[] uploads, CancellationToken ct)
    {
        var ids = uploads.Select(u => u.Id).ToArray();
        var summaries = await db.NinjaAccountUpdateRows.AsNoTracking().Where(r => ids.Contains(r.UploadId)).GroupBy(r => r.UploadId)
            .Select(g => new { Id = g.Key, Summary = new NinjaAccountUpdateSummary(g.Count(), g.Count(r => r.MatchStatus == "Found"),
                g.Count(r => r.MatchStatus == "NotFound"), g.Count(r => r.MatchStatus == "Ambiguous"), g.Count(r => r.HasProblems),
                g.Count(r => r.CityMismatch), g.Count(r => r.IqamaProblem), g.Count(r => r.NaqlBlocked), g.Count(r => r.SheetSuspended == true)) })
            .ToDictionaryAsync(s => s.Id, s => s.Summary, ct);
        var statuses = await db.NinjaAccountUpdateRows.AsNoTracking().Where(r => ids.Contains(r.UploadId))
            .GroupBy(r => new { r.UploadId, r.SheetStatus }).Select(g => new { g.Key.UploadId, g.Key.SheetStatus, Count = g.Count() }).ToArrayAsync(ct);
        var byUpload = statuses.ToLookup(s => s.UploadId);
        return uploads.Select(u => new NinjaAccountUpdateUploadResponse(u.Id, u.FileName, u.SheetName, u.ContentHash, u.Note,
            u.CreatedAtUtc, u.CreatedByUserId, u.UpdatedAtUtc, Convert.ToBase64String(u.RowVersion), u.IgnoredFooterRows,
            summaries.GetValueOrDefault(u.Id) ?? new(0, 0, 0, 0, 0, 0, 0, 0, 0),
            byUpload[u.Id].OrderBy(s => s.SheetStatus, StringComparer.Ordinal).Select(s => new NinjaAccountUpdateStatusCount(s.SheetStatus, s.Count)).ToArray())).ToArray();
    }
}
