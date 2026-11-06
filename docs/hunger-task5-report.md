# HungerStation Task 5 delivery

Backend/API only, 6 November 2026. No commit, deployment, hosted import, production database connection, or migration application was performed. Existing uncommitted Tasks 1–4 work was retained. Local source reference: `5f6a16fe11171771e43f7c4503c1fa39ea4bf0e8`, with a dirty working tree; this is not a claim about remote HEAD or deployed binaries.

## Delivered behavior and decisions

- **R02/R03:** account preview distinguishes New, Unchanged, ChangedField, ConflictingOwner and MissingAccount, with individual existing/incoming field differences. Every row needs an explicit Create/Update/ReplaceOwner/Keep/Skip decision. Missing owner/city prerequisites remain actionable errors; no HR record is guessed or created. A valid new account can be created without pre-registration. Duplicate/missing/invalid decisions fail before mutation. Confirm revalidates the preview under account/platform/preview locks and saves the entire selected batch atomically. Replay requires identical canonical decisions. Import does not create assignments or bypass wallet review.
- **R11:** scoped due/overdue deferred-account endpoint plus a separately registered, trusted background-job query requiring an explicit Hunger platform. Both use the same read query. Due date defaults to the Riyadh business day; future dates are rejected. No automatic activation or external notification is introduced.
- **R13/R14:** per-account wallet list and history, plus complete filtered CSV/XLSX exports. Balances are dated observations with evidence, preserving signs and null for missing data. Closing evidence, proven adjustments, restricted rights and attributed balance remain distinct. Receipt totals follow remittance allocations and their reversals. Company arrival and a supervisor's pending company remittance remain different states. History includes associated batch, receipt and reversal events; batch event amounts retain their original scope. Existing assignment-event fields remain the source of the last account movement.
- **R21/R22 classification:** optional dated original CaptainClassification in operation input, persistence, report and export. It is independent of quality Segment, internal classifications, NoShow, rejection and cancellation counts. Conflicting same-day reimports are rejected. No invented classification formula, penalty or account block is added.
- **R28:** all supported workbook input tables have XLSX templates and preview/confirm routes: accounts, quality MTD, daily operations. Typed JSON finance actions retain their existing contracts. Account/wallet exports share list filters and include all authorized matches beyond the current page. Quality/operation exports accept rider allocation membership and account filters; performance report/export share account filters and their existing rider/city/sponsor/vehicle/target filters. Account source metrics remain labeled as source metrics in quality/operation exports. Added account subfield and numeric/date-range filters are documented in the API guide. CSV quotes cells and protects formula-like user text; exports exclude credentials.
- **R29:** the verification runner reads every 100-row account page, checks stable totals and unique IDs, and summarizes the entire inventory rather than a 20-row sample. Apply mode requires a reviewed resolutions JSON file. Tests verify 221 inventory rows and exports beyond the first page. Migration SQL, test results and source provenance are retained locally; no deployment or restored-database equivalence is claimed.
- **Concurrency:** three SQL Server integration tests use independent contexts and concurrent calls for wallet confirm, withdrawal arrival and competing receipt allocations. They use random disposable database names and existing application locks, rowversions and schema constraints. They skip without SQL opt-in, following the Jahez test pattern.
- **Policy:** negative-balance handover requires **no special approval**. Existing signed-wallet policy, monthly analysis and mandatory review/confirm are preserved. No permission keys or grants are added, and no Identity migration is needed.

## Files touched by Task 5

These are Task 5's files, not the complete pre-existing dirty-tree diff. Some already contained earlier task changes.

| Area | Files |
|---|---|
| API | `src/LogisticsERP.Api/Controllers/HungerController.cs`; `HungerController.Reporting.cs` |
| Application contracts | `src/LogisticsERP.Application/Features/Hunger/HungerAccountContracts.cs`; `HungerFinanceContracts.cs`; `HungerOperationContracts.cs`; `HungerReportingContracts.cs` |
| Application helpers/hooks | `src/LogisticsERP.Application/Features/Hunger/HungerAccountVerification.cs`; `IHungerDeferredReminderQuery.cs` |
| Domain | `src/LogisticsERP.Domain/Entities/Hunger/HungerDailyOperation.cs` |
| Registration | `src/LogisticsERP.Infrastructure/DependencyInjection.cs` |
| Import | `src/LogisticsERP.Infrastructure/Hunger/HungerAccountParser.cs`; `HungerQualityService.Accounts.cs` |
| Reads/exports/reminders | `src/LogisticsERP.Infrastructure/Hunger/HungerQualityService.AccountReports.cs`; `HungerQualityService.WalletReports.cs`; `HungerDeferredReminderQuery.cs`; `HungerQualityService.Reporting.cs`; `HungerQualityService.Performance.cs` |
| Operations | `src/LogisticsERP.Infrastructure/Hunger/HungerQualityService.Operations.cs` |
| EF | `src/LogisticsERP.Infrastructure/Persistence/Configurations/HungerConfigurations.cs`; `Persistence/Migrations/Application/ApplicationDbContextModelSnapshot.cs`; generated migration pair below |
| Tests | `tests/LogisticsERP.Fleet.UnitTests/HungerFinanceTests.cs`; `HungerFinanceSqlTests.cs`; `HungerRemainingAccountsTests.cs`; `HungerWalletReportsTests.cs`; `HungerClassificationTests.cs` |
| Tool | `tools/LogisticsERP.HungerImport/Program.cs` |
| Docs | `docs/hunger-api-endpoints.md`; `docs/hunger-import-rules.md`; this report |

## Migration

`20261106143740_HungerSourceCaptainClassification` and its generated Designer file, under `src/LogisticsERP.Infrastructure/Persistence/Migrations/Application/`.

Generated with:

```powershell
dotnet ef migrations add HungerSourceCaptainClassification --project src/LogisticsERP.Infrastructure --startup-project src/LogisticsERP.Api --context ApplicationDbContext --output-dir Persistence/Migrations/Application --no-build
```

Adds only nullable `hunger.DailyOperations.CaptainClassification nvarchar(100)`. Existing rows remain null/unknown; no speculative backfill, financial writes or permission changes. Reviewed SQL: `tmp/task5-classification-migration.sql`. Up is additive; Down removes only the new classification column and would discard classification values, so deployment rollback must account for any subsequent imported data. Neither direction was applied here.

## Gate results

| Check | Result |
|---|---|
| `dotnet build LogisticsERP.slnx -m:1` | **Passed: 0 warnings, 0 errors**; restore included. NuGet audit disabled for the local build, without analyzer suppression. Default parallel invocation exited without diagnostics; the serialized build completed successfully. |
| `dotnet test tests/LogisticsERP.Fleet.UnitTests --filter FullyQualifiedName~Hunger --no-build --no-restore` | **Passed: 121 passed, 0 failed, 4 skipped; 125 total.** |
| Full Fleet regression | 1,451 passed, 2 existing failures, 42 skipped; 1,495 total. Failed names exactly match the saved Task 4 results. |
| GranularPermissionIsolationTests, within Fleet | **480 passed, 0 failed**; unchanged. |
| Jahez regression | 318 passed, 12 existing failures, 34 skipped; 364 total. Failed names exactly match Task 4. |
| Hunger import tool build | Passed: 0 warnings, 0 errors. Tool was not run against the hosted database. |
| Application / Identity pending-model checks | Both passed: no pending model changes. |
| Migration SQL | Generated and inspected: one nullable column plus migration history. |
| `git diff --check` | Passed. |

There are 15 additional passing Hunger cases and three additional SQL-only cases relative to Task 4's 106 passing/one skipped cases. Coverage includes import creation, unchanged replay, field differences and owner conflict resolution, all-row decisions and atomic rejection, stale previews and permission loss, invalid source cells, templates, reminders and the trusted job hook, multi-page verification/export, report rider filters, signed/unknown wallet balances, receipt reversals, supervisor arrival responsibility, wallet export scope and original classification persistence.

The existing Fleet failures remain `PlatformAccountValidationTests.EmployeeWithProfileCanOwnAndUpdateAnAccountWithoutChangingRole(legacy: False)` and `PlatformAccountValidationTests.ValidCreationNeedsOnlyTheExistingAccountSponsor`. The 12 Jahez failures remain the `RiderCanTakeTwoChefzPlusOneJahezInAnyOrderAndReplaceEither` permission cases. The comparison in `tmp/task5-results/regression-comparison.json` has no new failed names in either suite.

Evidence: `tmp/task5-build-final.log`, `tmp/task5-hunger-final.log`, `tmp/task5-fleet-final.log`, `tmp/task5-jahez-final.log`, `tmp/task5-import-tool-build.log`, `tmp/task5-model-check.log`, `tmp/task5-identity-model-check.log`, and TRX files under `tmp/task5-results/`. Source and build hashes are in `tmp/task5-results/source-provenance.json`; they describe full working files, including inherited changes, not an isolated commit.

## Remaining validation

The SQL concurrency tests are implemented but were **not executed against a usable SQL Server**: SQL opt-in/connection variables were absent. LocalDB is installed, but querying its instance configuration failed under this session, so availability was not assumed. Enable `LOGISTICS_HUNGER_SQL_TESTS=1` with a usable `LOGISTICS_HUNGER_SQL_TEST_CONNECTION`, or set `HUNGER_SQL_TEST_SERVER`, to run them against disposable databases. The fourth skipped Hunger case is the pre-existing SQL import test.

Migration application, restored-BAK integrity checks, live import verification, deployed source/binary reconciliation and background-job scheduling remain deployment/environment work. The read hook itself is registered and tested. No frontend implementation is included. No requested backend code is intentionally left unfinished.
