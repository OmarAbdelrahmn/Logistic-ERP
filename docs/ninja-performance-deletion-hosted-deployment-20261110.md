# Ninja performance deletion permission — hosted deployment

Applied to `db67927.public.databaseasp.net` / `db67927` on **10 November 2026 at 13:34:39 Riyadh time**. A separate verification completed at **13:34:46 Riyadh time**.

Applied migrations:

- Application: `20261110101250_AddNinjaPerformanceDeletePermission`.
- Identity: `20261110101358_GrantNinjaPerformanceDeletePermission`.

The deployment adds the sensitive, high-trust, client-platform-scoped `ninja.performance.delete` definition and its system administrator grant, plus their migration-history entries. There are now 11 active Ninja permission definitions and 11 administrator Ninja grants. Other roles receive no automatic grant.

Preflight verified the target, preceding migrations, and absence of conflicting definition/grant IDs. A rollback rehearsal inserted and verified both migrations, then verified rollback through a fresh connection. The exact same SQL hashes were applied together in one Serializable transaction under an application lock. Existing Ninja accounts, assignments, performance, deductions, daily usages, and unrelated permission/grant fingerprints matched inside the transaction. A fresh-connection verification confirmed the committed definition, grant, and migration history.

No performance deletion endpoint was called. This update did not publish API binaries; the API must be deployed separately to expose the new endpoint.

## Evidence

- [Preflight](../outputs/ninja-performance-deletion-hosted-20261110/preflight.json).
- [Rollback rehearsal](../outputs/ninja-performance-deletion-hosted-20261110/rehearse.json).
- [Committed application](../outputs/ninja-performance-deletion-hosted-20261110/apply.json).
- [Independent verification](../outputs/ninja-performance-deletion-hosted-20261110/verify.json).
- [Application SQL](../database/scripts/ninja-performance-deletion-application.sql).
- [Identity SQL](../database/scripts/ninja-performance-deletion-identity.sql).
- [Repeatable deployment tool](../tools/Apply-NinjaPerformanceDeletePermissions.ps1).

SHA-256:

```text
ninja-performance-deletion-application.sql
0B922EAC3CCF6ADF948BD99870E96CCC94FAE3512C992A66698599EB0165AA69

ninja-performance-deletion-identity.sql
4DB9E0213765C0F6C13CDEE7EEDF14BB93F02597A2CF3DB08D514709D79F185B
```
