# Hosted permission database update — 6 October 2026

Committed to the configured hosted database `db67927`.

| Verification | Result |
|---|---|
| Management families split | 46 |
| Read/Create/Edit/Delete definitions for these families | 184 |
| Existing management role grants covered | 178 |
| Existing direct assignments covered (grants and denies) | 537 |
| Distinct users holding management through grants | 29 |
| Users with corresponding role assignments | 27 |
| Users with corresponding direct assignments | 29 |
| Total supported catalog keys in the new backend | 244 |

Every existing management holder has all four corresponding action permissions. Grants and denies retain their effect, validity window, reason, flags, and exact scopes. Existing explicit denies continue to override grants. The verification queries check every role and direct assignment, rather than only the seeded roles.

Applied migrations:

- Application: `20261006074723_SplitManagementPermissionCatalog`
- Identity: `20261006074727_SplitManagementPermissionGrants`

Both migrations were executed inside one outer SQL transaction using `database/scripts/run-management-permission-split-20261006.ps1 -Commit`. Both histories and all assignment postconditions were read again after the commit. Rerunning the idempotent scripts in a rollback transaction also passed.

Before commit, both migrations passed rollback verification on the hosted SQL Server. Additional rolled-back fixtures covered scoped grants, scoped denies, future starts, expired assignments, all-client flags, future-contract flags, historical deleted scopes, and support-access JSON expansion/deduplication. No test fixtures were committed.

The local logical authorization backup is `.codex-temp/management-authorization-backup-20261006.json`. It includes the permission definitions, role grants, direct assignments, scopes, support access, authorization versions, and migration histories. It is git-ignored because it contains user access details. Business records were not changed by these migrations. Down migrations deliberately refuse to collapse independently edited permissions; recovery needs a reviewed restoration from the authorization snapshot.

## Backend rollout

The permission changes are implemented in the local backend and need to be published to activate the new endpoint checks on the hosted API. Backend publication was not performed by this database update.

The hosted database retains 46 compatibility management definitions and their assignments for the currently deployed API. There are 290 active database definitions: 244 supported new-backend keys plus these 46 compatibility keys. The new backend does not accept management keys, filters them out of permission/user/role/support-access editing responses, and authorizes only independent action keys. Compatibility entries can be retired after all hosted API instances have been updated. This preserves existing hosted access while deployment is pending.

Frontend integration details, all affected endpoints, and complete permission keys are in [the handoff](management-permissions-frontend-handoff.md), [the JSON map](management-permissions-endpoints.json), and [the TypeScript file](management-permissions.ts).

## Validation

- Solution build: successful, zero warnings/errors.
- EF model/migration consistency: both contexts report no pending model changes.
- Dedicated permission isolation/catalog tests: 435 passed, including independent Read/Create/Edit/Delete access, role/direct grants, deny precedence, and expiry.
- Fleet suite before the final read-test expansion: 701 passed, 3 SQL diagnostics skipped.
- Jahez suite: 32 passed, 3 SQL diagnostics skipped.
- Domain suite: 282 passed; two unrelated failures remain in the full suite: the fuel monthly-usage test expects non-nullable rider/employee IDs after the existing nullable-usage change, and the anonymous-entry-point allowlist does not include the existing fuel import endpoints. The 282 other domain tests pass, including the affected API permission surface checks.
