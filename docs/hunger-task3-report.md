# HungerStation Task 3 report

Backend/API only. No commit. Pre-existing Task 1/2 changes were preserved; this report lists Task 3 changes only.

## Behavior and decisions

- Hunger accounts use the shared platform business context, with batch reads across authorized accounts. The existing scoped `hunger.read` permission authorizes the list without requiring a second platform-read grant.
- Optional inclusive Saudi `from`/`through` dates default to the current month through today. Vehicle custody intersects the account assignment and report period, preserves ended custody and gaps, and resolves substitute users by verified employee identity.
- Ownership (`Company`, `Private`, `Mixed`, `Unknown`) and custody status (`Assigned`, `Mixed`, `None`, `Unknown`) are separate. Verified absence is `None` custody with `Private` ownership. Missing evidence/conflicts remain unknown. Multiple vehicle identities are summarized with `Mixed` and full periods remain available. Registered vehicle identity is independent.
- SIM ownership comes from the shared normalized-phone registry and dated ownership revisions. Holder and responsible employee are independent; the imported label remains `sourceSimOwnership`.
- Registration filters combine with AND before count/pagination. Text uses trimmed case-insensitive contains; states and IDs use equality. The legacy `disposition` query parameter remains supported.
- `fleet.vehicles.fields.identity` is sensitive and seeded only to SystemAdmin. Changes to existing fleet identity require it in addition to action permissions. Existing create-only import/creation behavior is retained. Existing roles are not granted it automatically.
- Negative-balance handover policy was not changed; no special approval was added.

## Files changed

- `src/LogisticsERP.Infrastructure/Hunger/HungerQualityService.Accounts.cs`
- `src/LogisticsERP.Infrastructure/Hr/PlatformAccountBusinessService.cs`
- `src/LogisticsERP.Application/Features/Hunger/HungerAccountContracts.cs`
- `src/LogisticsERP.Api/Controllers/HungerController.cs` (account query binding only in Task 3)
- `src/LogisticsERP.Infrastructure/Fleet/FleetService.cs`
- `src/LogisticsERP.Application/Authorization/PermissionKeys.cs`
- `src/LogisticsERP.Infrastructure/Persistence/SeedData/PermissionSeedCatalog.cs`
- `src/LogisticsERP.Infrastructure/Identity/SeedData/AuthorizationSeedCatalog.cs`
- Both EF model snapshots and the migration/designer pairs listed below
- `tests/LogisticsERP.Fleet.UnitTests/HungerQualityServiceTests.cs` (shared fixture supports partial tests and permission-specific checking)
- `tests/LogisticsERP.Fleet.UnitTests/HungerAccountContextTests.cs`
- `tests/LogisticsERP.Fleet.UnitTests/HungerFleetIdentityPermissionTests.cs`
- `docs/hunger-api-endpoints.md`
- This report

## Migrations

Generated using `dotnet ef migrations add` with the corresponding context and repository migration directory:

- Application: `20261106093923_GrantFleetVehicleIdentityFieldPermissions` adds one permission definition.
- Identity: `20261106094011_GrantFleetVehicleIdentityFieldPermissions` appends one SystemAdmin role grant without renumbering existing grants.

No database migration was applied. EF tooling reports tool version 10.0.10 versus runtime 10.0.11; generation completed successfully.

## Verification

| Check | Result |
|---|---|
| `dotnet build LogisticsERP.slnx -m:1 -nr:false -p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors; analyzers enabled. |
| `dotnet test tests/LogisticsERP.Fleet.UnitTests --filter FullyQualifiedName~Hunger -m:1 -nr:false -p:UseSharedCompilation=false` | Passed: 75 passed, 0 failed, 1 existing SQL Server integration test skipped; 76 total. |
| Full Fleet regression (`--no-build`) | 1,405 passed, 2 existing failures, 39 skipped; 1,446 total. Failed case names exactly match the Task 2 TRX; no new failures. |
| GranularPermissionIsolationTests (in full Fleet run) | 480 passed, 0 failed. |
| Jahez regression (`--no-build`) | 318 passed, 12 existing failures, 34 skipped; 364 total. Matches Task 2 and the supplied Jahez baseline. |
| Application EF pending model changes | Passed: no changes since the last migration. |
| Identity EF pending model changes | Passed: no changes since the last migration. |
| SQL scripts for both new migrations | Generated and inspected; only the intended seed inserts and migration history records. |
| `git diff --check` | Passed. |

The solution build used one MSBuild worker because default parallel worker startup failed in this environment without reporting compiler errors. The full single-worker build completed with analyzers enabled.

The two unchanged Fleet failures are `PlatformAccountValidationTests.EmployeeWithProfileCanOwnAndUpdateAnAccountWithoutChangingRole(legacy: False)` and `PlatformAccountValidationTests.ValidCreationNeedsOnlyTheExistingAccountSponsor`. The 12 Jahez failures remain the existing `RiderCanTakeTwoChefzPlusOneJahezInAnyOrderAndReplaceEither` permission cases.

Added tests cover ended custody and Saudi date boundaries, gaps, multiple vehicle identities, None versus Unknown, custody conflicts, substitute identity matching, independent SIM ownership history/holder/responsible employee, positive combined filters and negative filters, filtering before count/pagination, invalid periods, scoped Hunger read authorization, and independent fleet identity grants.

Evidence: `tmp/task3-gate-build-final.log`, `tmp/task3-gate-hunger.log`, `tmp/task3-fleet.log`, `tmp/task3-jahez.log`, and TRX files in `tmp/task3-results/`. Reviewed scripts are `tmp/task3-application-migration.sql` and `tmp/task3-identity-migration.sql`.

## Remaining deployment/verification

No requested backend functionality is intentionally deferred. Apply both migrations through the normal deployment process. The SQL integration test requires `HUNGER_SQL_TEST_SERVER`; no SQL-server integration execution or production load test is claimed.
