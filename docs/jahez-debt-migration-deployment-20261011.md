# Hosted Jahez debt schema deployment — 11 October 2026

Migration `20261011122817_AddJahezDebtFreezeAndClearance` was applied successfully to **db67927**, hosted at `db67927.public.databaseasp.net`, after the user explicitly requested the hosted database update.

Verified at **2026-10-11 13:12:14 UTC / 16:12:14 Asia/Riyadh**. EF migration product version: 10.0.11.

## Changes applied

| New column | SQL type | Nullable |
|---|---|---|
| DebtStatus | int, default 1 | No |
| FrozenAtUtc | datetimeoffset | Yes |
| AccountClearedAtUtc | datetimeoffset | Yes |
| DebtPaidAtUtc | datetimeoffset | Yes |
| ExternalClearanceReference | nvarchar(500) | Yes |

Three check constraints were installed and verified enabled and trusted:

- CK_JahezHandover_DebtStatus
- CK_JahezHandover_FrozenRange
- CK_JahezLedger_StatusAmount

The migration was recorded in `migration.__ApplicationMigrationsHistory`.

## Existing data preservation

- Before/after handovers: **300**. All were open, nontransferred periods and were initialized to Active (DebtStatus=1).
- Before/after ledger: **844 entries**.
- Before/after signed ledger amount sum: **245,166.926000 SAR**. This is the signed sum of ledger entries, not a rider outstanding summary or cashbox balance.
- Existing original handover columns, except the database-generated RowVersion, were compared in both directions inside the transaction and preserved.
- Full existing ledger rows were compared in both directions inside the transaction and preserved.
- No account was frozen, transferred, cleared, or marked paid by this schema deployment. No rider collection was recorded.

The runner uses XACT_ABORT, an explicit transaction, a bounded lock timeout, and pre-commit schema/history/data assertions. An assertion failure rolls the transaction back. Snapshots of handovers, ledger, settlements, and migration history were saved before the schema operation; these are targeted data snapshots, not a SQL Server full-database backup.

## Artifacts

- [Applied SQL script](../database/scripts/jahez-debt-freeze-and-clearance.sql)
- [Deployment runner](../tools/Apply-JahezDebtMigration.ps1)
- [Verification JSON](../outputs/jahez-debt-migration-20261011T131211Z/verification.json)
- [Handover snapshot](../outputs/jahez-debt-migration-20261011T131211Z/handovers-before.csv)
- [Ledger snapshot](../outputs/jahez-debt-migration-20261011T131211Z/ledger-before.csv)
- [Settlement snapshot](../outputs/jahez-debt-migration-20261011T131211Z/settlements-before.csv)
- [Previous migration history](../outputs/jahez-debt-migration-20261011T131211Z/migration-history-before.csv)

SQL script SHA-256: `04626FC7203E8B0E08AB401B85162F87D7AC0AF96439DEBC75FF7A4EC9F39A4D`.

## Deployment scope

This operation updated the hosted database schema. It did not publish the updated API/Worker or implement frontend screens. Deploy compatible application code before using the new frozen-account workflow. The previous LocalDB test-startup limitation remains separate from this successful hosted SQL migration; production business-data test mutations were not run.
