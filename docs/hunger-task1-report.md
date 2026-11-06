# Hunger finance authorization and accountant receipts — Task 1

Implemented backend/API changes for R15/R16. No UI changes or commits. The explicit user policy remains: negative-balance handover needs no special approval.

Finance permissions now separate reads, funding/top-up requests and execution, collection, withdrawal requests and arrival confirmation, rider payment, wallet review, remittance submission, accountant confirmation, cancellation/reversal and configuration. Existing scoped FinanceManage grants remain a compatibility superset; explicit granular denials still win. Existing monthly settlement capture/reconciliation write permissions are preserved outside this task; their list endpoint uses FinanceRead.

Receipt batches support Draft, Submitted, PartiallyConfirmed, Confirmed and Cancelled. Confirmations persist immutable source allocations. Reversals preserve the original confirmation and allocations, create a reversal event and linked opposite treasury postings, and restore remaining amounts. Pending collections report outstanding amounts by batch and by source, including unbatched collections. Company receipt destinations must pass ownership and account validation.

Decisions:

- Drafts reserve their allocated source amounts; cancellation releases those reservations.
- The existing create-and-submit route remains available alongside explicit draft and submit routes.
- Cancellation requires no historical confirmation, including after reversal. Reverse and reconfirm to correct confirmed receipts.
- A reversal reverses one complete confirmation; partial receipt amounts are represented by separate confirmations.
- Omitted receipt destination owner/type default to the configured Company, with ownership validated identically to explicit values.
- Funding/top-up requests have no financial postings. Execution revalidates business rules and posts once with an execution key derived from the request ID; existing direct execution remains available under execute permissions.
- The production persistence interceptor permits only the defined Hunger lifecycle changes with matching staged audit/financial evidence. Other financial fields, allocations and treasury history retain their edit/delete restrictions. Existing withdrawal completion is covered by the same narrow guard.
- GranularPermissionIsolationTests still check action isolation. Their blanket prohibition of every `.manage` key now explicitly allows the three existing legacy keys required by current compatibility policy; no granular action assertion was removed.

Migrations generated with `dotnet ef`:

| Context | Migration | Effect |
|---|---|---|
| Application | `20261106083232_HungerReceiptLifecycleAndFinancePermissions` | ReceiptAllocations table and constraints, 12 permission definitions, historical allocation backfill and batch status derivation. Invalid historical allocation totals abort the migration. |
| Identity | `20261106083738_GrantHungerFinanceActionPermissions` | Appends 12 SystemAdmin grants, IDs 304–315; preserves all existing grant IDs. |

Both contexts have matching model snapshots. Generated SQL scripts are available locally at [application SQL](../artifacts/hunger-task1-application.sql) and [Identity SQL](../artifacts/hunger-task1-identity.sql). Migrations have not been applied to any database.

Changed files:

| Area | Files |
|---|---|
| API authorization | `src/LogisticsERP.Api/Authorization/RequirePermissionAttribute.cs`, `PermissionAuthorizationPolicyProvider.cs`, `PermissionAuthorizationHandler.cs` |
| Controllers | `src/LogisticsERP.Api/Controllers/HungerController.cs`, `HungerController.Reporting.cs` |
| Application contracts | `src/LogisticsERP.Application/Authorization/PermissionKeys.cs`, `src/LogisticsERP.Application/Features/Hunger/HungerFinanceContracts.cs` |
| Domain | `src/LogisticsERP.Domain/Entities/Hunger/HungerFinance.cs` |
| Service authorization | `src/LogisticsERP.Infrastructure/Authentication/PermissionChecker.cs` |
| Hunger services | `src/LogisticsERP.Infrastructure/Hunger/HungerQualityService.Finance.cs`, `HungerQualityService.Receipts.cs` (new), `HungerQualityService.Wallet.cs`, `HungerQualityService.Settlements.cs` |
| EF mapping and persistence | `src/LogisticsERP.Infrastructure/Persistence/Configurations/HungerFinanceConfigurations.cs`, `src/LogisticsERP.Infrastructure/Persistence/Interceptors/ApplicationPersistenceInterceptor.cs` |
| Permission seeds | `src/LogisticsERP.Infrastructure/Persistence/SeedData/PermissionSeedCatalog.cs`, `src/LogisticsERP.Infrastructure/Identity/SeedData/AuthorizationSeedCatalog.cs` |
| Application migrations | `src/LogisticsERP.Infrastructure/Persistence/Migrations/Application/20261106083232_HungerReceiptLifecycleAndFinancePermissions.cs`, its `.Designer.cs`, `ApplicationDbContextModelSnapshot.cs` |
| Identity migrations | `src/LogisticsERP.Infrastructure/Identity/Migrations/20261106083738_GrantHungerFinanceActionPermissions.cs`, its `.Designer.cs`, `IdentityDbContextModelSnapshot.cs` |
| Tests | `tests/LogisticsERP.Fleet.UnitTests/HungerFinanceTests.cs`, `HungerFinanceAuthorizationTests.cs` (new), `GranularPermissionIsolationTests.cs` |
| Documentation | `docs/hunger-api-endpoints.md`, this report |

Verification:

| Check | Result |
|---|---|
| `dotnet build LogisticsERP.slnx -m:1 -nr:false -p:UseSharedCompilation=false` | Passed, 0 warnings and 0 errors. Bounded MSBuild parallelism avoids the host's default-worker failure. |
| `dotnet test tests/LogisticsERP.Fleet.UnitTests --filter FullyQualifiedName~Hunger -m:1 -nr:false -p:UseSharedCompilation=false` | Passed: 55 passed, 0 failed, 1 existing SQL Server integration test skipped; 56 total. [Log](../artifacts/hunger-task1-hunger-tests.log). |
| GranularPermissionIsolationTests | 480 passed, 0 failed on the focused run. Action isolation remains checked for every case, including the new finance read permission. |
| Full Fleet regression suite | 1,385 passed, 2 failed, 39 skipped; 1,426 total. No granular permission isolation failures. The remaining failures are existing platform-account validation fixtures described below. Total failures are below the supplied 480-failure Fleet baseline. [Log](../artifacts/hunger-task1-fleet-tests.log). |
| Jahez suite | 318 passed, 12 failed, 34 skipped; 364 total. Failure count matches the supplied baseline. The 12 failures are the known `RiderCanTakeTwoChefzPlusOneJahezInAnyOrderAndReplaceEither` permission cases. [Log](../artifacts/hunger-task1-jahez-tests.log). |
| EF pending model changes, Application and Identity | Both passed: no pending changes. |
| SQL migration script generation, both contexts | Passed; no database update executed. |
| `git diff --check` | Passed. |

New/expanded tests cover draft submission, rejected transitions, cancellation and reservation release, partial/selected/all confirmation, over-confirmation rollback, destination ownership, reversal balances and posting links, duplicate requests/reversals, independent grants through direct/role assignments, explicit denies, scope preservation, request/execute separation, and production persistence-interceptor guards. Financial history mutation/deletion remains rejected outside the allowed transitions.

The two remaining Fleet failures are `PlatformAccountValidationTests.ValidCreationNeedsOnlyTheExistingAccountSponsor` and `EmployeeWithProfileCanOwnAndUpdateAnAccountWithoutChangingRole(legacy: false)`. Both fixtures construct SimplePlatformService without field permissions and with a null current user. The unchanged `SimplePlatformService.CanChangeAccountFields` guard returns false for that context before invoking any permission checker. Neither these fixtures nor that service was changed by this task. They remain outside the finance scope; an isolated checkout of the original baseline was not rebuilt.

Remaining deployment validation: review/apply both migrations against an isolated SQL Server database with representative historical data, then exercise real SQL Server locking/concurrency. SQL script generation and model consistency checks do not establish successful migration execution against live data.
