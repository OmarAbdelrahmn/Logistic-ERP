using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService : IHungerAccountsService
{
    private sealed record SeedPlan(string Revision, IReadOnlyList<HungerAccountImportRow> Rows);
    private async Task<bool> FieldAccess(Guid[] platforms, string[] keys, CancellationToken ct)
    {
        if (user.UserId is not { } uid || user.AuthorizationVersion is not { } version) return false;
        foreach (var p in platforms)
            foreach (var key in keys)
                if (!await permissions.HasPermissionAsync(uid, version, key, new(AccessScopeType.ClientPlatform, p), ct)) return false;
        return true;
    }
    private async Task<SeedPlan> MatchSeeds(HungerAccountSeed[] input, Guid[] platforms, CancellationToken ct)
    {
        var codes = input.Select(i => i.AccountId).ToArray();
        var identities = input.SelectMany(i => new[] { i.OwnerIqama, i.ActualIqama }).Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
        var accounts = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId) && codes.Contains(a.ExternalAccountId)).OrderBy(a => a.Id).ToArrayAsync(ct);
        var employees = await db.Employees.AsNoTracking().Where(e => identities.Contains(e.IqamaNo)).OrderBy(e => e.Id).ToArrayAsync(ct);
        var eids = employees.Select(e => e.Id).ToArray();
        var riders = await db.RiderProfiles.AsNoTracking().Where(r => eids.Contains(r.EmployeeId)).OrderBy(r => r.Id).ToArrayAsync(ct);
        var aids = accounts.Select(a => a.Id).ToArray();
        var details = await db.Set<PlatformAccountBusinessDetails>().AsNoTracking().Where(d => aids.Contains(d.PlatformRiderAccountId)).OrderBy(d => d.Id).ToArrayAsync(ct);
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => aids.Contains(a.PlatformRiderAccountId)).OrderBy(a => a.Id).ToArrayAsync(ct);
        var cities = await (from oc in db.OperatingCities.AsNoTracking() join g in db.GlobalCities.AsNoTracking() on oc.GlobalCityId equals g.Id select new { oc.Id, g.NameAr }).OrderBy(c => c.Id).ToArrayAsync(ct);
        var rows = new List<HungerAccountImportRow>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in input)
        {
            var account = accounts.Where(a => a.ExternalAccountId == source.AccountId).ToArray();
            var owners = employees.Where(e => e.IqamaNo == source.OwnerIqama).ToArray();
            var actual = (from e in employees join r in riders on e.Id equals r.EmployeeId where e.IqamaNo == source.ActualIqama select r).ToArray();
            var issue = !seen.Add(source.AccountId) ? "المعرف مكرر في الملف." : account.Length > 1 ? "المعرف غير فريد داخل هنقر." : owners.Length != 1 ? "إقامة صاحب الحساب غير موجودة أو غير فريدة." : cities.Count(c => c.NameAr == source.City) != 1 ? "المدينة غير معروفة أو غير فريدة." : null;
            if (source.OwnerIqama.Length != 10 || !source.OwnerIqama.All(char.IsAsciiDigit)) issue = "إقامة صاحب الحساب غير صالحة.";
            if (source.Phone.Length > 32 || source.Registry.Length > 150 || source.SettlementModel.Length > 150 || source.RegisteredVehicleType.Length > 32
                || source.SourceStatus.Length > 150 || source.ActualIqama?.Length > 50 || source.CompensationModel?.Length > 150 || source.ActualVehicleDescription?.Length > 150)
                issue = "Source field exceeds its supported length.";
            if (SourceDisposition(source.SourceStatus) == "DeferredNextMonth" && source.AssignmentFrom is null)
                issue = "A deferred account requires an explicit due date.";
            var assignmentIssue = source.SourceStatus != "يعمل" ? "حساب غير تشغيلي؛ لا ينشأ تكليف نشط." : actual.Length != 1 ? "المستخدم الفعلي غير مسجل أو غير فريد؛ تعليق التكليف." : source.AssignmentFrom is null || source.OpeningWallet is null || source.OpeningOrders is null ? "بيانات استلام الحساب أو المحفظة غير مكتملة." : "يلزم اعتماد مراجعة المحفظة قبل إنشاء التكليف.";
            var existing = account.Length == 1 ? account[0] : null;
            var detail = details.SingleOrDefault(d => d.PlatformRiderAccountId == existing?.Id);
            var changes = new List<HungerAccountFieldChange>();
            void Compare(string field, object? oldValue, object? newValue)
            {
                string? Text(object? value) => value is IFormattable f ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : value?.ToString();
                var oldText = Text(oldValue); var newText = Text(newValue);
                if ((oldText ?? "") != (newText ?? "")) changes.Add(new(field, oldText, newText));
            }
            Compare("OwnerEmployeeId", existing?.RegisteredEmployeeId, (owners.Length == 1 ? owners[0].Id : (Guid?)null));
            Compare("OperatingCityId", existing?.OperatingCityId, cities.FirstOrDefault(c => c.NameAr == source.City)?.Id);
            Compare("PhoneNumber", detail?.PhoneNumber, source.Phone);
            Compare("RegistryIdentifier", detail?.RegistryIdentifier, source.Registry);
            Compare("SettlementModel", detail?.SettlementModel, source.SettlementModel);
            Compare("RegisteredVehicleType", detail?.RegisteredVehicleType, source.RegisteredVehicleType);
            Compare("SimOwnership", detail?.SimOwnership, source.SimOwnership);
            Compare("SourceStatus", detail?.SourceStatus, source.SourceStatus);
            Compare("OperationalDisposition", detail?.OperationalDisposition, SourceDisposition(source.SourceStatus));
            Compare("SourceActualRiderProfileId", detail?.SourceActualRiderProfileId, actual.Length == 1 ? actual[0].Id : null);
            Compare("SourceActualIqama", detail?.SourceActualIqama, source.ActualIqama);
            Compare("SourceCompensationModel", detail?.SourceCompensationModel, source.CompensationModel);
            Compare("SourceVehicleDescription", detail?.SourceVehicleDescription, source.ActualVehicleDescription);
            Compare("SourceAssignmentFrom", detail?.SourceAssignmentFrom, source.AssignmentFrom);
            Compare("SourceOpeningWallet", detail?.SourceOpeningWallet, source.OpeningWallet);
            Compare("SourceOpeningOrders", detail?.SourceOpeningOrders, source.OpeningOrders);
            Compare("DeferredUntil", detail?.DeferredUntil, SourceDisposition(source.SourceStatus) == "DeferredNextMonth" ? source.AssignmentFrom : null);
            var status = existing is null ? (platforms.Length == 1 && issue is null ? "New" : "MissingAccount")
                : existing.RegisteredEmployeeId.HasValue && owners.Length == 1 && existing.RegisteredEmployeeId != owners[0].Id ? "ConflictingOwner"
                : changes.Count > 0 ? "ChangedField" : "Unchanged";
            rows.Add(new(source.RowNumber, source.AccountId, existing?.Id,
                owners.Length == 1 ? owners[0].Id : null, actual.Length == 1 ? actual[0].Id : null,
                status, issue, assignmentIssue) { Changes = changes, PlatformId = existing?.ClientPlatformId ?? (platforms.Length == 1 ? platforms[0] : null) });
        }
        return new(Hash(Serialize(new { accounts, employees, riders, details, assignments, cities })), rows);
    }
    public async Task<Result<HungerAccountImportPreview>> PreviewAccountsAsync(string fileName, byte[] content, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.AccountsImport, ct);
        if (platforms.Length == 0 || !await FieldAccess(platforms, [PermissionKeys.PlatformFields.Owner, PermissionKeys.PlatformFields.City,
            PermissionKeys.PlatformFields.Contact, PermissionKeys.PlatformFields.Registry, PermissionKeys.PlatformFields.Settlement, PermissionKeys.PlatformFields.Vehicle], ct))
            return Result.Failure<HungerAccountImportPreview>(HungerErrors.Forbidden);
        var parsed = HungerAccountParser.Parse(fileName, content);
        if (parsed.Error is not null) return Result.Failure<HungerAccountImportPreview>(HungerErrors.Invalid(parsed.Error));
        var plan = await MatchSeeds(parsed.Rows, platforms, ct);
        var preview = new HungerImportPreview { Kind = "Accounts", UserId = user.UserId!.Value, FileName = Path.GetFileName(fileName),
            ContentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)), InputRevision = plan.Revision,
            RowsJson = Serialize(parsed.Rows), ExpiresAtUtc = clock.GetUtcNow().AddMinutes(30) };
        db.Add(preview); await db.SaveChangesAsync(ct);
        return Result.Success(new HungerAccountImportPreview(preview.Id, preview.ExpiresAtUtc, plan.Rows.All(r => r.Error is null), plan.Rows));
    }
    public Task<Result<HungerAccountImportResult>> ConfirmAccountsAsync(Guid previewId, CancellationToken ct = default)
        => Task.FromResult(Result.Failure<HungerAccountImportResult>(HungerErrors.Invalid("Explicit resolution is required for every row.")));

    public async Task<Result<HungerAccountImportResult>> ConfirmAccountsAsync(Guid previewId, HungerAccountImportConfirmation request, CancellationToken ct = default)
    {
        if (request.Resolutions is null || request.Resolutions.Count == 0 || request.Resolutions.Select(r => r.RowNumber).Distinct().Count() != request.Resolutions.Count)
            return Result.Failure<HungerAccountImportResult>(HungerErrors.Invalid("Provide one explicit resolution per row."));
        var resolutionHash = Hash(Serialize(request.Resolutions.OrderBy(r => r.RowNumber)));
        var platforms = await Platforms(PermissionKeys.Hunger.AccountsImport, ct);
        if (platforms.Length == 0 || !await FieldAccess(platforms, [PermissionKeys.PlatformFields.Owner, PermissionKeys.PlatformFields.City,
            PermissionKeys.PlatformFields.Contact, PermissionKeys.PlatformFields.Registry, PermissionKeys.PlatformFields.Settlement, PermissionKeys.PlatformFields.Vehicle], ct))
            return Result.Failure<HungerAccountImportResult>(HungerErrors.Forbidden);
        var preview = await db.Set<HungerImportPreview>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == previewId && p.UserId == user.UserId && p.Kind == "Accounts", ct);
        if (preview is null) return Result.Failure<HungerAccountImportResult>(HungerErrors.NotFound);
        var input = Read<HungerAccountSeed[]>(preview.RowsJson); var codes = input.Select(i => i.AccountId).ToArray();
        var ids = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId) && codes.Contains(a.ExternalAccountId)).Select(a => a.Id).ToArrayAsync(ct);
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear(); await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, ids.Concat(platforms).Append(previewId), ct);
                var current = await db.Set<HungerImportPreview>().SingleAsync(p => p.Id == previewId, ct);
                if (current.ResultJson is not null)
                {
                    var previous = Read<HungerAccountImportResult>(current.ResultJson);
                    return previous.ResolutionHash == resolutionHash ? Result.Success(previous)
                        : Result.Failure<HungerAccountImportResult>(HungerErrors.Conflict("The confirmed resolutions differ."));
                }
                var plan = await MatchSeeds(input, platforms, ct);
                if (request.Resolutions.Count != plan.Rows.Count || plan.Rows.Any(r => !request.Resolutions.Any(d => d.RowNumber == r.RowNumber)))
                    return Result.Failure<HungerAccountImportResult>(HungerErrors.Invalid("Resolve every preview row exactly once."));
                foreach (var row in plan.Rows)
                {
                    var decision = request.Resolutions.Single(d => d.RowNumber == row.RowNumber);
                    var valid = decision.Action == "Skip" || (row.Error is null && (decision.Action == "Keep" && row.MatchedAccountId.HasValue
                        || decision.Action == "Create" && row.MatchedAccountId == null && platforms.Contains(decision.PlatformId ?? row.PlatformId ?? Guid.Empty)
                        || decision.Action == "Update" && row.Status == "ChangedField"
                        || decision.Action == "ReplaceOwner" && row.Status == "ConflictingOwner"));
                    if (!valid || decision.PlatformId.HasValue && row.MatchedAccountId.HasValue && decision.PlatformId != row.PlatformId)
                        return Result.Failure<HungerAccountImportResult>(HungerErrors.Invalid($"Invalid resolution for row {row.RowNumber}."));
                }
                if (current.ExpiresAtUtc <= clock.GetUtcNow() || current.InputRevision != plan.Revision) return Result.Failure<HungerAccountImportResult>(HungerErrors.Conflict("المعاينة قديمة؛ أعد المطابقة."));
                var cities = await (from oc in db.OperatingCities join g in db.GlobalCities on oc.GlobalCityId equals g.Id select new { oc.Id, g.NameAr }).ToArrayAsync(ct);
                var matchedIds = plan.Rows.Where(r => r.MatchedAccountId.HasValue).Select(r => r.MatchedAccountId!.Value).ToArray();
                var matchedAccounts = await db.PlatformRiderAccounts.Where(a => matchedIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);
                var matchedDetails = await db.Set<PlatformAccountBusinessDetails>().Where(d => matchedIds.Contains(d.PlatformRiderAccountId)).ToDictionaryAsync(d => d.PlatformRiderAccountId, ct);
                var imported = new List<HungerAccountImportRow>();
                foreach (var match in plan.Rows)
                {
                    var decision = request.Resolutions.Single(d => d.RowNumber == match.RowNumber);
                    if (decision.Action is "Keep" or "Skip") continue;
                    var source = input.Single(i => i.RowNumber == match.RowNumber);
                    var account = match.MatchedAccountId.HasValue ? matchedAccounts[match.MatchedAccountId.Value]
                        : new PlatformRiderAccount { ClientPlatformId = decision.PlatformId ?? match.PlatformId!.Value,
                            ExternalAccountId = source.AccountId, Code = Guid.NewGuid().ToString("N") };
                    if (!match.MatchedAccountId.HasValue) db.Add(account);
                    imported.Add(match with { MatchedAccountId = account.Id, PlatformId = account.ClientPlatformId });
                    var details = matchedDetails.GetValueOrDefault(account.Id) ?? new PlatformAccountBusinessDetails { PlatformRiderAccountId = account.Id };
                    var before = Serialize(new { account, details });
                    account.RegisteredEmployeeId = match.OwnerEmployeeId; account.OperatingCityId = cities.Single(c => c.NameAr == source.City).Id;
                    details.PhoneNumber = source.Phone; details.RegistryIdentifier = source.Registry; details.SettlementModel = source.SettlementModel;
                    details.RegisteredVehicleType = source.RegisteredVehicleType; details.SimOwnership = source.SimOwnership;
                    details.SourceStatus = source.SourceStatus; details.OperationalDisposition = SourceDisposition(source.SourceStatus);
                    details.DispositionReason = source.SourceStatus; details.SourceActualRiderProfileId = match.ActualRiderProfileId;
                    details.SourceActualIqama = source.ActualIqama; details.SourceCompensationModel = source.CompensationModel;
                    details.SourceVehicleDescription = source.ActualVehicleDescription; details.SourceAssignmentFrom = source.AssignmentFrom;
                    details.SourceOpeningWallet = source.OpeningWallet; details.SourceOpeningOrders = source.OpeningOrders;
                    details.SourceReference = $"Import:{current.Id:D}/Sheet1/Row:{source.RowNumber}";
                    details.DeferredUntil = details.OperationalDisposition == "DeferredNextMonth" ? source.AssignmentFrom : null;
                    if (db.Entry(details).State == EntityState.Detached) db.Add(details);
                    db.Add(new PlatformAccountBusinessRevision { PlatformRiderAccountId = account.Id,
                        EffectiveFrom = source.AssignmentFrom ?? DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime),
                        Reason = details.SourceReference, BeforeJson = before, AfterJson = Serialize(new { account, details }) });
                }
                var result = new HungerAccountImportResult(current.Id, imported.Count, imported.Count(r => r.ActualRiderProfileId.HasValue),
                    imported.Count(r => input.Single(i => i.RowNumber == r.RowNumber).SourceStatus == "يعمل"),
                    plan.Rows.Select(r => imported.SingleOrDefault(i => i.RowNumber == r.RowNumber) ?? r).ToArray()) { ResolutionHash = resolutionHash };
                current.ResultJson = Serialize(result); await db.SaveChangesAsync(ct); return Result.Success(result);
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<HungerAccountImportResult>(HungerErrors.Conflict("تعارض أثناء اعتماد الحسابات؛ أعد المعاينة.")); }
    }
    private static string SourceDisposition(string text) => text switch
    { "يعمل" => "Normal", "الشهر الجديد" => "DeferredNextMonth", "لايعمل" => "AdministrativeStop", _ => "Problem" };

    public async Task<Result<HungerAccountPage>> AccountsAsync(string? disposition, int page, int pageSize, CancellationToken ct = default)
        => await AccountsFilteredAsync(new HungerAccountFilter { OperationalDisposition = disposition }, page, pageSize, ct);

    public Task<Result<HungerAccountPage>> AccountsFilteredAsync(HungerAccountFilter filter, int page, int pageSize, CancellationToken ct = default)
        => QueryAccounts(filter, page, pageSize, false, PermissionKeys.Hunger.Read, ct);

    private async Task<Result<HungerAccountPage>> QueryAccounts(HungerAccountFilter filter, int page, int pageSize, bool all, string permission, CancellationToken ct)
    {
        var platforms = await Platforms(permission, ct);
        if (platforms.Length == 0) return Result.Failure<HungerAccountPage>(HungerErrors.Forbidden);
        if (page < 1 || pageSize is < 1 or > 100) return Result.Failure<HungerAccountPage>(HungerErrors.Invalid("صفحة غير صالحة."));
        var todayForContext = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        var from = filter.From ?? new DateOnly(todayForContext.Year, todayForContext.Month, 1);
        var through = filter.Through ?? todayForContext;
        if (from == default || through < from || through > todayForContext || through.DayNumber - from.DayNumber > 366)
            return Result.Failure<HungerAccountPage>(HungerErrors.Invalid("فترة غير صالحة."));
        var query = from a in db.PlatformRiderAccounts.AsNoTracking() join details in db.Set<PlatformAccountBusinessDetails>().AsNoTracking() on a.Id equals details.PlatformRiderAccountId into ds
                    from d in ds.DefaultIfEmpty() where platforms.Contains(a.ClientPlatformId)
                    select new { Account = a, Details = d };
        var selected = await query.OrderBy(x => x.Account.ExternalAccountId).ThenBy(x => x.Account.Id).ToArrayAsync(ct);
        var ids = selected.Select(x => x.Account.Id).ToArray();
        var contexts = await PlatformAccountBusinessService.ResolveContextsAsync(db, clock, configuration,
            selected.Select(x => x.Account).ToArray(), from, through, ct);
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => ids.Contains(a.PlatformRiderAccountId) && a.Status == RiderAssignmentStatus.Active && a.EffectiveTo == null).ToArrayAsync(ct);
        var ownerIds = selected.Select(x => x.Account.RegisteredEmployeeId).Concat(contexts.Values.Select(c => c.Sim.ResponsibleEmployeeId)).ToArray();
        var employees = await db.Employees.AsNoTracking().Where(e => ownerIds.Contains(e.Id)).ToArrayAsync(ct);
        var sourceIds = selected.Select(x => x.Details?.SourceActualRiderProfileId).ToArray();
        var riderIds = sourceIds.Concat(assignments.Select(a => (Guid?)a.RiderProfileId)).Concat(contexts.Values.Select(c => c.Sim.HolderRiderProfileId)).Distinct().ToArray();
        var actual = await (from r in db.RiderProfiles.AsNoTracking() join e in db.Employees.AsNoTracking() on r.EmployeeId equals e.Id where riderIds.Contains(r.Id)
            select new { r.Id, Employee = e }).ToArrayAsync(ct);
        var owners = await db.RiderProfiles.AsNoTracking().Where(r => ownerIds.Contains(r.EmployeeId)).ToArrayAsync(ct);
        var cityIds = selected.Select(x => x.Account.OperatingCityId).Distinct().ToArray();
        var cities = await (from oc in db.OperatingCities.AsNoTracking() join g in db.GlobalCities.AsNoTracking() on oc.GlobalCityId equals g.Id
            where cityIds.Contains(oc.Id) select new { oc.Id, g.NameAr }).ToArrayAsync(ct);
        var sponsorIds = selected.Select(x => x.Account.SponsorId).Distinct().ToArray();
        var sponsors = await db.Sponsors.AsNoTracking().Where(s => sponsorIds.Contains(s.Id)).ToArrayAsync(ct);
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(3)).DateTime);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthUsages = await db.Set<HungerDailyOperation>().AsNoTracking().Where(a => ids.Contains(a.PlatformRiderAccountId)
            && a.ReportDate >= monthStart && a.ReportDate <= today && a.ActualWorkingHours > 0)
            .Select(a => a.PlatformRiderAccountId).Distinct().ToArrayAsync(ct);
        var quality = await db.Set<HungerQualitySnapshot>().AsNoTracking().Where(q => ids.Contains(q.PlatformRiderAccountId) && q.ReportDate >= monthStart && q.ReportDate <= today)
            .OrderByDescending(q => q.ReportDate).ToArrayAsync(ct);
        static Guid? SourceImportId(string? reference)
        {
            if (reference is null || !reference.StartsWith("Import:", StringComparison.Ordinal)) return null;
            var end = reference.IndexOf('/', StringComparison.Ordinal);
            return end > 7 && Guid.TryParse(reference.AsSpan(7, end - 7), out var id) ? id : null;
        }
        var sourceImportIds = selected.Select(x => SourceImportId(x.Details?.SourceReference)).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var sourceImports = await db.Set<HungerImportPreview>().AsNoTracking().Where(p => sourceImportIds.Contains(p.Id) && p.Kind == "Accounts" && p.ResultJson != null)
            .Select(p => new { p.Id, p.CreatedAtUtc }).ToArrayAsync(ct);
        var credentialAccounts = await db.PlatformAccountCredentialVersions.AsNoTracking().Where(c => ids.Contains(c.PlatformRiderAccountId)).Select(c => c.PlatformRiderAccountId).Distinct().ToArrayAsync(ct);
        var assignmentEvents = await (from e in db.RiderAssignmentEvents.AsNoTracking() join a in db.RiderClientAssignments.IgnoreQueryFilters().AsNoTracking() on e.RiderClientAssignmentId equals a.Id
            where ids.Contains(a.PlatformRiderAccountId) orderby e.OccurredAtUtc descending, e.Id descending
            select new { a.PlatformRiderAccountId, e.ToStatus, e.OccurredAtUtc }).ToArrayAsync(ct);
        var items = selected.Select(x =>
        {
            var owner = employees.SingleOrDefault(e => e.Id == x.Account.RegisteredEmployeeId); var assignment = assignments.SingleOrDefault(a => a.PlatformRiderAccountId == x.Account.Id);
            var context = contexts[x.Account.Id];
            var periods = context.ActualVehiclePeriods;
            static string? Summary(IEnumerable<string?> values)
            {
                var distinct = values.Distinct().ToArray();
                return distinct.Length == 1 ? distinct[0] : distinct.Length > 1 ? "Mixed" : null;
            }
            var latestQuality = quality.FirstOrDefault(q => q.PlatformRiderAccountId == x.Account.Id);
            var lastEvent = assignmentEvents.FirstOrDefault(e => e.PlatformRiderAccountId == x.Account.Id);
            var assignmentUsed = monthUsages.Contains(x.Account.Id);
            var import = sourceImports.SingleOrDefault(p => p.Id == SourceImportId(x.Details?.SourceReference));
            var sourceObserved = import is null ? (DateOnly?)null : DateOnly.FromDateTime(import.CreatedAtUtc.ToOffset(TimeSpan.FromHours(3)).DateTime);
            var usage = LogisticsERP.Domain.Hunger.HungerAccountUsageRules.Evaluate(today, assignmentUsed, x.Details?.SourceStatus,
                x.Details?.SourceActualIqama, x.Details?.SourceAssignmentFrom, sourceObserved);
            var importedUsed = x.Details?.SourceStatus == "يعمل" && !string.IsNullOrWhiteSpace(x.Details.SourceActualIqama)
                && x.Details.SourceAssignmentFrom <= today && sourceObserved >= monthStart && sourceObserved <= today;
            return new HungerAccountItem(x.Account.Id, x.Account.ExternalAccountId, owner?.Id, owner?.FullNameAr, owner?.IqamaNo,
                x.Details?.SourceActualRiderProfileId, actual.SingleOrDefault(a => a.Id == x.Details?.SourceActualRiderProfileId)?.Employee.FullNameAr,
                x.Details?.SourceActualIqama, assignment?.RiderProfileId, x.Details?.OperationalDisposition ?? "Unknown", assignment is null ? "Unassigned" : "Assigned",
                x.Details?.PhoneNumber, x.Details?.RegistryIdentifier, x.Details?.SettlementModel, context.RegisteredVehicleType,
                x.Details?.SourceCompensationModel, x.Details?.SourceStatus, Convert.ToBase64String(x.Details?.RowVersion ?? x.Account.RowVersion), x.Details?.DeferredUntil)
            {
                Owner = owner is null ? null : new(owner.Id, owners.SingleOrDefault(r => r.EmployeeId == owner.Id)?.Id, owner.FullNameAr, owner.FullNameEn, owner.IqamaNo, owner.PrimaryPhone)
                    { Nationality = owner.Nationality, EmployeeStatus = owner.Status.ToString(), RelationshipType = owner.EngagementType.ToString() },
                ActualRider = Person(assignment?.RiderProfileId), SourceActualRider = Person(x.Details?.SourceActualRiderProfileId),
                ReportedCurrentRider = assignment is not null ? Person(assignment.RiderProfileId) : importedUsed ? Person(x.Details?.SourceActualRiderProfileId) : null,
                CurrentRiderSource = assignment is not null ? "Assignment" : importedUsed ? "ImportedSource" : "None",
                ReportedAssignmentStatus = assignment is not null ? "Assigned" : importedUsed ? "ReportedAssigned" : "Unknown",
                CurrentAssignmentId = assignment?.Id, AssignmentFrom = assignment?.EffectiveFrom,
                AssignmentRowVersion = assignment is null ? null : Convert.ToBase64String(assignment.RowVersion),
                CompensationModel = assignment?.CompensationModel, PaymentModel = x.Account.PaymentModel.ToString(), AccountCode = x.Account.Code,
                OperatingCityId = x.Account.OperatingCityId, OperatingCityNameAr = cities.SingleOrDefault(c => c.Id == x.Account.OperatingCityId)?.NameAr,
                SponsorId = x.Account.SponsorId, SponsorNameAr = sponsors.SingleOrDefault(s => s.Id == x.Account.SponsorId)?.RegistryNameAr,
                SimOwnership = context.Sim.Ownership, SourceSimOwnership = x.Details?.SimOwnership, Sim = context.Sim,
                SimHolder = Person(context.Sim.HolderRiderProfileId),
                SimResponsibleEmployee = EmployeePerson(context.Sim.ResponsibleEmployeeId),
                RegisteredVehiclePlate = context.RegisteredVehiclePlate,
                RegisteredVehicleSerial = context.RegisteredVehicleSerial, SourceVehicleDescription = x.Details?.SourceVehicleDescription,
                ActualVehicleType = Summary(periods.Select(p => p.VehicleType)), ActualVehiclePlate = Summary(periods.Select(p => p.Plate)),
                ActualVehicleOwnership = context.ActualVehicleOwnership, ActualVehiclePeriods = periods, ContextVerifiedAtUtc = context.VerifiedAtUtc,
                ActualVehicleCustodyStatus = periods.Count == 0 || periods.Any(p => p.Ownership == "Unknown") ? "Unknown"
                    : periods.All(p => p.VehicleId == null) ? "None"
                    : periods.Select(p => p.VehicleId).Distinct().Count() > 1 ? "Mixed" : "Assigned",
                MonthlyUsageStatus = usage.Status,
                MonthlyUsageSource = usage.Source,
                UsageMonth = monthStart.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                LatestQualitySegment = latestQuality?.Segment, LatestQualityRank = LogisticsERP.Domain.Hunger.HungerQualityRules.Rank(latestQuality?.Segment),
                LatestQualityScore = latestQuality?.FinalDeliveryQualityScore, LatestQualityReportDate = latestQuality?.ReportDate,
                HasPassword = credentialAccounts.Contains(x.Account.Id), LastAssignmentEvent = lastEvent?.ToStatus.ToString(), LastAssignmentEventAtUtc = lastEvent?.OccurredAtUtc,
                SourceAssignmentFrom = x.Details?.SourceAssignmentFrom, DispositionReason = x.Details?.DispositionReason
            };
            HungerPerson? EmployeePerson(Guid? id)
            {
                var employee = employees.SingleOrDefault(e => e.Id == id);
                return employee is null ? null : new(employee.Id, owners.SingleOrDefault(r => r.EmployeeId == employee.Id)?.Id,
                    employee.FullNameAr, employee.FullNameEn, employee.IqamaNo, employee.PrimaryPhone);
            }
            HungerPerson? Person(Guid? id)
            {
                var row = actual.SingleOrDefault(a => a.Id == id);
                return row is null ? null : new(row.Employee.Id, row.Id, row.Employee.FullNameAr, row.Employee.FullNameEn, row.Employee.IqamaNo, row.Employee.PrimaryPhone)
                    { Nationality = row.Employee.Nationality, EmployeeStatus = row.Employee.Status.ToString(), RelationshipType = row.Employee.EngagementType.ToString() };
            }
        }).Where(item => Matches(item, filter)).ToArray();
        return Result.Success(new HungerAccountPage((all ? items : items.Skip((page - 1) * pageSize).Take(pageSize).ToArray()), items.Length, page, pageSize));
    }
    private static bool Matches(HungerAccountItem item, HungerAccountFilter filter)
    {
        static bool Text(string? value, string? search) => string.IsNullOrWhiteSpace(search) || value?.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) == true;
        static bool State(string? value, string? search) => string.IsNullOrWhiteSpace(search) || string.Equals(value, search.Trim(), StringComparison.OrdinalIgnoreCase);
        return Text(item.ExternalAccountId, filter.ExternalAccountId)
            && (filter.ActualRiderProfileId == null || item.CurrentActualRiderProfileId == filter.ActualRiderProfileId)
            && Text(item.ActualRider?.NameAr, filter.ActualRiderName)
            && Text(item.ActualRider?.IqamaNo, filter.ActualRiderIqama)
            && Text(item.ActualRider?.PhoneNumber, filter.ActualRiderPhone)
            && Text(item.SourceActualName, filter.SourceActualName)
            && Text(item.SourceActualIqama, filter.SourceActualIqama)
            && (filter.SponsorId == null || item.SponsorId == filter.SponsorId)
            && Text(item.SponsorNameAr, filter.SponsorNameAr)
            && State(item.PaymentModel, filter.PaymentModel)
            && Text(item.CompensationModel, filter.CompensationModel)
            && Text(item.SourceStatus, filter.SourceStatus)
            && State(item.MonthlyUsageStatus, filter.MonthlyUsageStatus)
            && State(item.LatestQualitySegment, filter.LatestQualitySegment)
            && (filter.QualityRankFrom == null || item.LatestQualityRank >= filter.QualityRankFrom)
            && (filter.QualityRankThrough == null || item.LatestQualityRank <= filter.QualityRankThrough)
            && (filter.QualityScoreFrom == null || item.LatestQualityScore >= filter.QualityScoreFrom)
            && (filter.QualityScoreThrough == null || item.LatestQualityScore <= filter.QualityScoreThrough)
            && (filter.QualityScoreEqual == null || item.LatestQualityScore == filter.QualityScoreEqual)
            && (filter.DeferredFrom == null || item.DeferredUntil >= filter.DeferredFrom)
            && (filter.DeferredThrough == null || item.DeferredUntil <= filter.DeferredThrough)
            && Text(item.AccountCode, filter.AccountCode)
            && Text(item.OwnerName, filter.OwnerName)
            && Text(item.Owner?.NameEn, filter.OwnerNameEn)
            && Text(item.OwnerIqama, filter.OwnerIqama)
            && Text(item.Owner?.PhoneNumber, filter.OwnerPhone)
            && Text(item.Owner?.Nationality, filter.OwnerNationality)
            && Text(item.Owner?.EmployeeStatus, filter.OwnerEmployeeStatus)
            && Text(item.Owner?.RelationshipType, filter.OwnerRelationshipType)
            && Text(item.PhoneNumber, filter.PhoneNumber)
            && Text(item.OperatingCityNameAr, filter.OperatingCityNameAr)
            && Text(item.RegistryIdentifier, filter.RegistryIdentifier)
            && Text(item.SettlementModel, filter.SettlementModel)
            && Text(item.RegisteredVehicleType, filter.RegisteredVehicleType)
            && Text(item.RegisteredVehiclePlate, filter.RegisteredVehiclePlate)
            && Text(item.RegisteredVehicleSerial, filter.RegisteredVehicleSerial)
            && Text(item.ActualVehicleType, filter.ActualVehicleType)
            && Text(item.ActualVehiclePlate, filter.ActualVehiclePlate)
            && State(item.ActualVehicleOwnership, filter.ActualVehicleOwnership)
            && State(item.ActualVehicleCustodyStatus, filter.ActualVehicleCustodyStatus)
            && State(item.SimOwnership, filter.SimOwnership)
            && Text(item.SourceSimOwnership, filter.SourceSimOwnership)
            && Text(item.SimResponsibleEmployee?.NameAr, filter.SimResponsibleName)
            && Text(item.SimResponsibleEmployee?.IqamaNo, filter.SimResponsibleIqama)
            && Text(item.SimResponsibleEmployee?.PhoneNumber, filter.SimResponsiblePhone)
            && Text(item.SimHolder?.NameAr, filter.SimHolderName)
            && Text(item.SimHolder?.IqamaNo, filter.SimHolderIqama)
            && Text(item.SimHolder?.PhoneNumber, filter.SimHolderPhone)
            && State(item.OperationalDisposition, filter.OperationalDisposition)
            && State(item.AssignmentStatus, filter.AssignmentStatus)
            && (filter.OwnerEmployeeId == null || item.OwnerEmployeeId == filter.OwnerEmployeeId)
            && (filter.OperatingCityId == null || item.OperatingCityId == filter.OperatingCityId)
            && (filter.SimResponsibleEmployeeId == null || item.Sim?.ResponsibleEmployeeId == filter.SimResponsibleEmployeeId)
            && (filter.SimHolderRiderProfileId == null || item.Sim?.HolderRiderProfileId == filter.SimHolderRiderProfileId);
    }
    public async Task<Result> DispositionAsync(Guid accountId, HungerDispositionRequest request, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.AccountsUpdate, ct);
        if (!await db.PlatformRiderAccounts.AnyAsync(a => a.Id == accountId && platforms.Contains(a.ClientPlatformId), ct)) return Result.Failure(HungerErrors.Forbidden);
        if (request.Disposition is not ("Normal" or "Problem" or "AdministrativeStop" or "Blocked" or "DeferredNextMonth") || string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Length > 2000 || request.Disposition == "DeferredNextMonth" && request.DeferredUntil is null) return Result.Failure(HungerErrors.Invalid("حدد الحالة والسبب وموعد السماح للتأجيل."));
        await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, [accountId], ct);
        var details = await db.Set<PlatformAccountBusinessDetails>().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
        if (details is null) return Result.Failure(HungerErrors.NotFound);
        if (Convert.ToBase64String(details.RowVersion) != request.RowVersion) return Result.Failure(HungerErrors.Conflict("تغير السجل."));
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (details.OperationalDisposition == "DeferredNextMonth" && request.Disposition == "Normal" && details.DeferredUntil > today) return Result.Failure(HungerErrors.Conflict("لم يحن موعد السماح المحدد."));
        var before = Serialize(details); details.OperationalDisposition = request.Disposition; details.DispositionReason = request.Reason; details.DeferredUntil = request.DeferredUntil;
        db.Add(new PlatformAccountBusinessRevision { PlatformRiderAccountId = accountId, EffectiveFrom = today, Reason = request.Reason, BeforeJson = before, AfterJson = Serialize(details) });
        try { await db.SaveChangesAsync(ct); return Result.Success(); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure(HungerErrors.Conflict("تغير السجل؛ أعد تحميله.")); }
    }
}
