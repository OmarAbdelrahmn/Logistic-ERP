# Ninja assignment date permission — hosted database deployment

Applied on **28 October 2026 at 14:28:28 Riyadh time** (11:28:28 UTC) to `db67927.public.databaseasp.net`, database `db67927`, following the user's instruction to update the hosted database.

## Applied changes

- Application migration: `20261028111233_AddNinjaAssignmentDatePermissionCatalog`.
- Identity migration: `20261028111320_GrantNinjaAssignmentDateCorrectionAccess`.
- Dedicated permission: `ninja.assignments.dates.update`, category `Ninja`, sensitive and requiring client-platform scope.
- Permission definition ID: `019c18d5-62e1-7000-a000-000000000319`.
- System administrator grant ID: `019c18d5-62e1-7000-b000-000000000283`.

The changes add one definition, one administrator grant and the two migration-history entries. Other roles/users receive no automatic grant. The database update does not publish API binaries; this run did not deploy or test the hosted date-correction endpoint. See the [frontend integration guide](ninja-assignment-date-corrections.md).

## Verification

Preflight found nine active Ninja definitions and nine administrator Ninja grants, with the new key absent and both prerequisite migrations present. A rollback rehearsal successfully inserted and verified the new rows, then rolled back and verified their absence through a fresh connection. The exact same script hashes were then applied in one Serializable transaction under an application lock.

Before commit, verification confirmed both new migrations and the expected scoped definition/administrator grant. Record counts and checksums matched for:

| Preserved dataset | Records |
| --- | ---: |
| Ninja accounts | 328 |
| Ninja assignments | 235 |
| Ninja performance | 3390 |
| Ninja deductions | 10 |
| Ninja daily usage | 299 |
| Existing permission definitions, excluding the new definition | 330 |
| Existing role grants, excluding the new grant | 1572 |

A new logical database connection confirmed the committed state, followed by an independent `Verify` invocation at **14:28:43 Riyadh time**. Both confirmed **ten** active Ninja definitions and **ten** administrator Ninja grants. No business dates or imported observations were changed. Credentials were read only inside the deployment process and are not included in the evidence.

## Evidence and repeatable tool

- [Read-only preflight](../outputs/ninja-assignment-dates-hosted-20261028112742Z/preflight.json).
- [Rollback rehearsal](../outputs/ninja-assignment-dates-hosted-20261028112742Z/rehearse.json).
- [Committed application report](../outputs/ninja-assignment-dates-hosted-20261028112742Z/apply.json).
- [Before/after transaction snapshot](../outputs/ninja-assignment-dates-hosted-20261028112742Z/apply-inside.xml).
- [Independent verification](../outputs/ninja-assignment-dates-hosted-20261028112742Z/verify.json).
- [Exact application SQL](../outputs/ninja-assignment-dates-hosted-20261028112742Z/ninja-assignment-dates-application.sql).
- [Exact identity SQL](../outputs/ninja-assignment-dates-hosted-20261028112742Z/ninja-assignment-dates-identity.sql).
- [Deployment tool](../tools/Apply-NinjaAssignmentDatePermissions.ps1).

Script SHA-256 values:

```text
ninja-assignment-dates-application.sql
04BD9312383EDBEE80160AAC63B82001A3A2942EF848CAE6BA42D019B73665E9

ninja-assignment-dates-identity.sql
8A03561B36DF6D5CE87041CDBF5F6D9CB493FFA3E4485E277A2FAA1A97DB0215
```

The tool supports `Preflight`, `Rehearse`, `Apply` and `Verify`. `Apply` requires a successful matching rollback rehearsal in its evidence directory. Already-applied valid migrations are verified without replaying inserts. Unexpected target hosts, missing prerequisites, conflicting/partial seed rows or unrelated DML cause the tool to stop; uncommitted changes roll back on failure.
