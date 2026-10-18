# Ninja permission database deployment — 17 October 2026

Applied to `db67927.public.databaseasp.net`, database `db67927`, at **18:15 Riyadh time** (15:15 UTC), following the user's authorization to update the hosted database.

The Ninja endpoints require dedicated `ninja.*` keys. Omar already had all seven direct grants and an active all-client `SYSTEM_ADMIN` assignment, but the database contained zero active Ninja permission definitions and neither permission migration was recorded. `PermissionChecker` rejects undefined permissions before evaluating user grants, causing the reported denials.

Applied migrations together in one Serializable SQL transaction:

- Application: `20261017095007_AddNinjaPermissionCatalog`.
- Identity: `20261017095013_GrantNinjaPermissions`.

The changes add seven permission definitions, seven `SYSTEM_ADMIN` grants and the two migration-history entries. The reviewed scripts contain no account, rider, report or financial updates. Existing direct user permissions and role assignments were retained.

Verification before commit and through a fresh connection after commit confirmed:

- All seven expected Ninja keys are active, nondeprecated definitions requiring client scope.
- `SYSTEM_ADMIN` has all seven Ninja role grants.
- Both migration histories contain the expected entries.
- Omar is active, has completed the password-change requirement and has an active all-client administrator assignment.
- No active applicable direct Ninja permission denial exists for Omar.

The permission snapshot cache expires within 60 seconds. Refresh the application after expiry. No access tokens or passwords were used for verification. Authenticated HTTP requests were not exercised, and API binaries were not published by this deployment.

Evidence:

- [Verification summary](../outputs/ninja-permissions-migration-20261017T151500Z/verification.json).
- [Read-only preflight](../outputs/ninja-permissions-migration-20261017T151500Z/preflight.xml).
- [Post-commit verification](../outputs/ninja-permissions-migration-20261017T151500Z/post-commit-verification.xml).
- [Exact application script](../outputs/ninja-permissions-migration-20261017T151500Z/ninja-permissions-application.sql).
- [Exact identity script](../outputs/ninja-permissions-migration-20261017T151500Z/ninja-permissions-identity.sql).
- [Deployment tool](../tools/Apply-NinjaPermissions.ps1).

Run `./tools/Apply-NinjaPermissions.ps1` for a read-only preflight; `-Apply` runs the reviewed migrations. The tool validates the target, rejects partial/conflicting seed state, takes a migration lock, stores evidence and rolls back both migrations on failure before commit. Already-applied migrations are skipped by the SQL scripts and verified again.

The saved evidence includes script SHA-256 hashes. No permission checker bypass or unrelated pending migration was deployed.
