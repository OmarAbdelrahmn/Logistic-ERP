# Platform performance deletion — hosted permission deployment

Applied to `db67927.public.databaseasp.net` / `db67927` on **10 November 2026 at 14:07:08 Riyadh time**, with independent verification at **14:07:12 Riyadh time**.

## Applied changes

- Application migration: `20261110105351_AddHungerJahezPerformanceDeletePermissions`.
- Identity migration: `20261110105334_GrantHungerJahezPerformanceDeletePermissions`.
- New permission definitions: `hunger.performance.delete` and `jahez.performance.delete`, both sensitive, high-trust and requiring client-platform scope.
- New administrator grants: Hunger, Jahez, and the existing `chefz.performance.delete` permission, whose administrator grant was missing.

Only two definitions, three administrator grants, and the two migration-history entries were inserted. No performance endpoint was called. Other roles receive no automatic grant. The API binaries were not published by this deployment.

## Verification

Preflight confirmed the exact database, preceding Ninja migrations, existing Chefz definition, administrator role, and absence of conflicting IDs/grants. A rollback rehearsal verified the new rows and their rollback through a fresh connection. The same script hashes were applied in one Serializable transaction under an application lock.

Counts and fingerprints matched inside the transaction for accounts, assignments, Hunger operations/potential fines, Chefz performance, Jahez daily orders, and unrelated permission definitions/grants. A separate Verify invocation confirmed both committed migrations, exact scoped permission metadata and all three administrator grants.

Code validation: 304 selected tests passed; 36 SQL Server/hosted-environment tests were skipped. This includes date/sponsor scoping, HTTP authorization and binding, rollback after audit failure, same-file recovery, and Jahez corrected-rider allocation. Both EF contexts report no pending model changes. Compilation used `RunAnalyzers=false` and `UseSharedCompilation=false`; the full static-analyzer pass was not part of this validation. An existing assignment-test fixture was corrected to inject its permission checker for salary edits.

## Evidence

- [Preflight](../outputs/platform-performance-deletion-hosted-20261110/preflight.json).
- [Final rollback rehearsal](../outputs/platform-performance-deletion-hosted-20261110/rehearse.json).
- [Committed application](../outputs/platform-performance-deletion-hosted-20261110/apply.json).
- [Independent verification](../outputs/platform-performance-deletion-hosted-20261110/verify.json).
- [Application SQL](../database/scripts/platform-performance-deletion-application.sql).
- [Identity SQL](../database/scripts/platform-performance-deletion-identity.sql).
- [Deployment tool](../tools/Apply-PlatformPerformanceDeletePermissions.ps1).
- [Endpoint and correction guide](platform-performance-day-deletion.md).

SHA-256:

```text
platform-performance-deletion-application.sql
811FB0D240204C8D0B6930F246F884B4531F5179094DD0732A25225B2E4063FD

platform-performance-deletion-identity.sql
5781604EDDD762F27BCD98DAAEAA8C79A2967042735249955C2BDABB38F3CFBC
```
