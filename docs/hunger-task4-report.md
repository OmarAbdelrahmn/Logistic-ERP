# Hunger Task 4 - performance (R20-R27)

Backend/API only. No commit, deployment, or database update was performed. Pre-existing changes from Tasks 1-3 were preserved. Negative-balance handover continues to require no special approval; this task does not alter handover policy.

## Files changed for this task

- `src/LogisticsERP.Api/Controllers/HungerController.Reporting.cs`: performance query/export and potential-fine lifecycle routes, using existing scoped permissions.
- `src/LogisticsERP.Application/Features/Hunger/HungerOperationContracts.cs`, `HungerReportingContracts.cs` and `HungerSettlementContracts.cs`: nullable observed hours, independent optional order counts, report/filter/catalog/forecast/fine contracts.
- `src/LogisticsERP.Domain/Entities/Hunger/HungerDailyOperation.cs`, `HungerAlertRule.cs`, new `HungerPotentialFine.cs`: observed hours/units, optional metrics, rule catalog, reviewed missed-shift lifecycle.
- `src/LogisticsERP.Domain/Hunger/HungerAccountUsageRules.cs` and new `HungerPerformanceRules.cs`: observed-work usage, duration conversion, numeric comparisons, mandatory weekdays, deficit and forecast calculations.
- `src/LogisticsERP.Infrastructure/Hunger/HungerQualityService.Accounts.cs`, `Operations.cs`, `Reporting.cs`, `Settlements.cs`, new `Performance.cs`: observed account usage, import/persistence, independent source totals and rider shares, calendar coverage, generic catalog evaluation, targets/forecasts/dimensions, all-result export, fine reconciliation.
- `src/LogisticsERP.Infrastructure/Persistence/Configurations/HungerConfigurations.cs`: fine entity, relationships/indexes, nullable hours and catalog columns.
- Application migration and designer `20261106131847_HungerPerformanceCatalogAndPotentialFines`, and `ApplicationDbContextModelSnapshot.cs`.
- `tests/LogisticsERP.Fleet.UnitTests/HungerAccountUsageRulesTests.cs` and new `HungerPerformanceTests.cs`: regression and feature acceptance coverage.
- `docs/hunger-api-endpoints.md` and this report.

Some listed files already contained earlier task edits. Only the performance changes described here belong to Task 4.

## Implementation decisions

1. Work derives from positive observed daily imported hours on the business date, independently of assignment presence or import creation month. WorkedDays and QualifiedDays (>=8 hours) use distinct entity/date totals; qualified days are a subset of worked days. Missing or unallocated hours remain unknown. Partial coverage can coexist with observed work; it prevents a reliable forecast. A fresh imported reported-holder snapshot remains visible independently of Unknown usage.
2. Hours use an explicit source unit: DecimalHours, Duration (hh:mm or hh:mm:ss), FractionOfDay. Legacy templates retain DecimalHours. Excel time cells require Duration. Hours are normalized to the existing eight-decimal storage precision before allocation, keeping source and rider shares equal. Daily hours are bounded to 24; aggregate hours are not. Manual comparison remains imported minus manual, and clearing manual data restores null difference.
3. Thu/Fri/Sat mandatory-day checks apply only to actual scheduled responsibility. Planned/Cancelled assignments create no duty. Missing coverage is different from a known missed day; neither creates debt or an operational block. Disabled scoped MandatoryDays versions record calendar exemptions. Unapproved contract classifications or monetary penalties are not inferred.
4. Catalog records are append-only versions with metric/operator/threshold/severity, city/platform scope and inclusive effective dates. Targets are explicit approved catalog entries; no legacy numerical target is silently activated. City overrides precede platform defaults, with deterministic version ordering.
5. Scheduled responsibility determines monthly target shares, not attendance hours. Where a shared boundary date lacks scheduled rider weights, the rider target stays unresolved rather than being divided equally. Multiple target codes remain separately visible in catalog evaluations rather than being arbitrarily added. Row summaries combine effective versions of one monthly CompletedOrders target code.
6. Forecast uses completed Riyadh dates through the earlier of yesterday and the requested through date. Monthly reports extend scheduled responsibility to the final requested month end while retaining observed source totals within the requested range. The row exposes coverageThrough and forecastObservedOrders; current-day imports can still appear in observed row totals. Run rate is O/e, forecast O+(O/e)*r, required pace max(T-O,0)/r. No elapsed days means no run rate; no remaining days means the final observed result. Gaps invalidate a reliable forecast; ended rider assignments have no later responsibility.
7. NoShow counts shifts. One Potential fine per source is saved with import and backfilled for historical positive NoShow sources. Potential -> Invoiced requires a matched invoice, the same source account/day, and a canonical source-line reference. Rider-specific lines must match the sole source rider. An optional invoice line NoShowCount bounds reconciliation of several daily reviews to one monthly line; a line without a count accepts only one source. Omitted count metadata is excluded from serialization to preserve legacy invoice idempotency hashes. Potential -> Waived requires evidence. Both are terminal and row-version protected; neither posts rider debt or treasury entries.
8. Accepted/stacked counts are independent, optional metrics. Rider attribution is available only for a sole rider source; shared-day counts without an allocation contract remain unknown. Source account totals are never added to rider allocations.
9. Grouping by rider changes the facts being aggregated; layout is presentation metadata. Day/month periods use the same facts. Sponsorship and actual vehicle ownership/type (including Mixed) are separate dimensions; totals and distinct counts precede paging. Distinct rider counts include historical source allocations even without current assignments. Export returns all filtered authorized rows with scope, cutoff and rule evidence.
10. No new permission keys were introduced, so no Identity migration or seed changes are needed.

## Migration

Generated with `dotnet ef migrations add HungerPerformanceCatalogAndPotentialFines --project src/LogisticsERP.Infrastructure --context ApplicationDbContext --output-dir Persistence/Migrations/Application --no-build` against the built model. Added data migration preserves legacy rule meanings and normalized-hour units, and creates historical PotentialFine reviews without money postings. Downgrade refuses to turn unknown hours into zero. No migration was applied to a database.

## Gate results

| Check | Observed result |
|---|---|
| `dotnet build LogisticsERP.slnx -m:1 -nr:false -p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors; analyzers enabled. |
| `dotnet test tests/LogisticsERP.Fleet.UnitTests --filter FullyQualifiedName~Hunger -m:1 -nr:false -p:UseSharedCompilation=false` | Passed: 106 passed, 0 failed, 1 existing SQL Server test skipped; 107 total. Includes 31 additional passing cases versus Task 3. |
| Full Fleet regression (`--no-build`) | 1,436 passed, 2 existing failures, 39 skipped; 1,477 total. Failed test names exactly match the saved Task 3 baseline. |
| GranularPermissionIsolationTests (in full Fleet run) | 480 passed, 0 failed; no increase. |
| Jahez regression (`--no-build`) | 318 passed, 12 existing failures, 34 skipped; 364 total. Failed test names exactly match Task 3. |
| Application / Identity pending model changes | Both passed: no pending changes. |
| Application migration SQL | Generated and inspected; only intended Hunger schema/backfill changes and migration history. No financial posting writes. |
| `git diff --check` | Passed. |

The solution build uses one MSBuild worker, disables shared compilation, and sets DOTNET_PROCESSOR_COUNT=2 for this machine; no analyzer suppression is used. The pre-existing Fleet failures remain `PlatformAccountValidationTests.EmployeeWithProfileCanOwnAndUpdateAnAccountWithoutChangingRole(legacy: False)` and `PlatformAccountValidationTests.ValidCreationNeedsOnlyTheExistingAccountSponsor`. The 12 Jahez failures remain the existing `RiderCanTakeTwoChefzPlusOneJahezInAnyOrderAndReplaceEither` permission cases. No new failures were introduced.

Evidence: `tmp/task4-gate-build-final.log`, `tmp/task4-gate-hunger-final.log`, `tmp/task4-fleet.log`, `tmp/task4-jahez.log`, TRX files and `regression-comparison.json` under `tmp/task4-results/`. The reviewed SQL is `tmp/task4-application-migration.sql`; model checks are `tmp/task4-application-model.log` and `tmp/task4-identity-model.log`.

Acceptance coverage includes business-date usage with independent imported reported-holder visibility; separate worked/qualified/unknown days; one account shared by two riders and one rider on two accounts; duration, decimal and Excel fraction/time cells; +2/-2/null manual comparisons; unrounded 7:59:59/8:00/10:00 thresholds; scheduled mandatory-day coverage; Planned exclusion; dated/city catalog overrides and future target versions; data-gap and zero-denominator forecasts; assignment-end responsibility; sponsor/Mixed vehicle dimensions; historical source rider counts without current assignments; filtering before pagination and full export, including legacy operations export column headers; NoShow idempotence, waiver, matched invoice/party validation, monthly shift-count capacity, and absence of automatic debt postings.

## Deployment and policy limits

No requested backend implementation is intentionally left undone.

Apply the application migration through the normal deployment process. The SQL integration test requires HUNGER_SQL_TEST_SERVER; execution against a live SQL Server is reported separately from in-memory verification. Contract-specific classification, activation exemptions and monetary penalties require their approved evidence; defaults never invent those financial rules. No frontend work is included.
