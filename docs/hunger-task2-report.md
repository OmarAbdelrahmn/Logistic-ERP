# Task 2 — Wallet review integrity and rider swap

Implemented backend/API support for R17, shared/05 section 5 and MONTHLY_WALLET_SETTLEMENT section 5. Existing Task 1 working-tree changes were preserved. No commit, UI change, database update or deployment was performed.

## Behavior and decisions

- New standalone or paired reviews supersede all earlier unconfirmed reviews for the account, across users. Superseded and expired previews cannot confirm; successful confirmations remain idempotent.
- Source changes supersede pending reviews in the same ApplicationDbContext SaveChanges, including shared account-service writes. Revision hashing independently covers account/assignment/details, usage/contributions, financial events and related platform events, settlement/invoice/adjustment documents, quality snapshots and daily operations. Invoice lines and their source links are retained within settlement DataJson and included in the hash.
- Platform-wide finance/settlement changes conservatively invalidate reviews for that platform, including invoices without an account ID. This can require fresh evidence for other accounts on the same platform.
- A swap review accepts one Return evidence payload plus the new rider's assignment request, produces two reviews sharing SwapId, evidence, revision and expiry, and prevents standalone confirmation of either leg. Both actions use the existing `hunger.finance.wallet.review` permission and platform/feature scope checks. No new permission or Identity migration is required.
- Confirmation encloses both existing lifecycle service calls, financial events, review results and audit writes in one serializable relational transaction. A failed handover rolls back the already-saved return. Changed balance/source data requires a new paired review and fresh evidence.
- A positive old-rider entitlement becomes reserved before calculating the new rider's amount. Negative handovers retain the intentional user policy: no special approval is required. Unknown prior closure still prevents confirmation. Existing assignment limits, row-version checks, operational restrictions and day allocation validation remain in force.

## Files changed by Task 2

Paths are relative to the repository root. Some files already contained Task 1 edits before this task.

| Area | Files |
|---|---|
| API | `src/LogisticsERP.Api/Controllers/HungerController.cs` |
| Contracts | `src/LogisticsERP.Application/Features/Hunger/HungerFinanceContracts.cs` |
| Domain | `src/LogisticsERP.Domain/Entities/Hunger/HungerFinance.cs` |
| Services | `src/LogisticsERP.Infrastructure/Hunger/HungerQualityService.Wallet.cs`, `HungerQualityService.RiderSwap.cs` (new) |
| Persistence | `src/LogisticsERP.Infrastructure/Persistence/ApplicationDbContext.cs`, `ApplicationDbContext.HungerWalletReviews.cs` (new), `Configurations/HungerFinanceConfigurations.cs` |
| Migration | `src/LogisticsERP.Infrastructure/Persistence/Migrations/Application/20261106090616_HungerWalletReviewIntegrityAndRiderSwap.cs`, its `.Designer.cs`, `ApplicationDbContextModelSnapshot.cs` |
| Tests | `tests/LogisticsERP.Fleet.UnitTests/HungerFinanceTests.cs`, `HungerWalletTestModelCustomizer.cs` (new) |
| Documentation | `docs/hunger-api-endpoints.md`, this report |

## Migration

Generated with `dotnet ef migrations add HungerWalletReviewIntegrityAndRiderSwap --project src/LogisticsERP.Infrastructure --context ApplicationDbContext --output-dir Persistence/Migrations/Application` (using the built model).

Adds Status and nullable SwapId to hunger.WalletReviews, plus account/status and swap indexes. The explicit data backfill marks historical completed reviews Confirmed and historical pending reviews Superseded, requiring fresh evidence under the expanded revision scheme. No financial history is changed. Migration script generation succeeded; the migration was not applied to a database.

## Verification

| Check | Result |
|---|---|
| `dotnet build LogisticsERP.slnx -m:1 -nr:false -p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| `dotnet test tests/LogisticsERP.Fleet.UnitTests --filter FullyQualifiedName~Hunger -m:1 -nr:false -p:UseSharedCompilation=false` | Passed: 67 passed, 0 failed, 1 existing SQL Server integration test skipped; 68 total. |
| Full Fleet regression | 1,397 passed, 2 failed, 39 skipped; 1,438 total. The same two existing PlatformAccountValidationTests failures recorded before Task 2 remain; no failure-count increase. |
| GranularPermissionIsolationTests | 480 passed, 0 failed in the full Fleet run. No permission changes were made by Task 2. |
| Jahez regression | 318 passed, 12 failed, 34 skipped; 364 total. Failure count matches the supplied baseline; failures are the existing `RiderCanTakeTwoChefzPlusOneJahezInAnyOrderAndReplaceEither` permission cases. |
| Application EF pending model changes | Passed: no changes since the migration. |
| SQL migration script generation | Passed. |
| `git diff --check` | Passed. |

Logs and generated SQL are in `tmp/task2-*.log` and `tmp/task2-wallet-migration.sql`; the full Fleet TRX is in `tmp/task2-results/task2-fleet.trx`.

The two existing Fleet failures are `PlatformAccountValidationTests.ValidCreationNeedsOnlyTheExistingAccountSponsor` and `EmployeeWithProfileCanOwnAndUpdateAnAccountWithoutChangingRole(legacy: false)`, matching the Task 1 report already present at the start of this task. Their existing field-permission/current-user fixtures remain outside this wallet scope. Neither failure count exceeds the supplied baseline.

Tests cover cross-user supersession, explicit invalidation and independent hash rejection for snapshots/invoices/adjustments/rights/contributions, expiry, positive/zero/negative atomic swaps, evidence linkage, idempotent replay, rejection of standalone leg confirmation, changed-source rejection, and rollback after the return has saved but new-rider validation fails. Relational transaction tests use SQLite with the production persistence interceptor and test-only SQL Server generated-value emulation.

## Remaining validation

No requested backend functionality is intentionally deferred. Applying the migration to representative historical data and testing actual SQL Server locking/concurrency remain deployment validation; SQLite transaction tests and generated SQL do not establish that validation. The existing opt-in Hunger SQL Server integration test was skipped because its SQL Server test environment was not enabled.
