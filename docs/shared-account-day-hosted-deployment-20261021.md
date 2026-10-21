# Shared-day account allocation — hosted database deployment

Applied on **21 October 2026 at 10:47:34 Riyadh time** (07:47:34 UTC).

- Server/database: `db67927.public.databaseasp.net` / `db67927`.
- Application migration: `20261021071103_AddPlatformAccountDayAllocations`.
- Prerequisite verified: `20261019114428_UseGranularJahezTreasuryPermissions`.
- No Identity migration or API binary deployment was performed.

## Execution and preservation

Used [Apply-PlatformAccountDayAllocations.ps1](../tools/Apply-PlatformAccountDayAllocations.ps1) with the reviewed [idempotent migration SQL](../database/scripts/platform-account-day-allocations.sql).

1. Verified the hosted target, migration history, and absence of a partial shared-day schema.
2. Completed a server `COPY_ONLY` database backup with checksums. The hosted login cannot perform `RESTORE VERIFYONLY`; the backup command completed successfully.
3. Rehearsed the migration in a serializable transaction, compared every original column of all 34 protected business tables in both directions, verified sources/constraints/reconciliation, then rolled back.
4. Confirmed rehearsal schema/history rollback, repeated the migration, validated the same preservation checks, and committed.
5. Reconnected independently and verified the migration history, schema, constraints, source data, and reconciliation results.

Verified all five new tables, all 15 relevant restrictive foreign keys enabled and trusted, five new check constraints enabled and trusted, both SQL rowversions, 12 allocation/source uniqueness indexes, and the Jahez usage columns.

| Preserved or created data | Count |
| --- | ---: |
| Existing Chefz daily performance records, unchanged | 2,772 |
| Matching Chefz account-day sources backfilled | 2,772 |
| Existing Chefz settlements, unchanged | 241 |
| Existing rider account assignments, unchanged | 753 |
| Boundary usage records created | 789 |
| Ninja performance/deduction sources | 0 / 0, matching existing datasets |
| Invented historical money rows or usage figures | 0 |

All source-total and attribution reconciliation checks passed. No existing earnings, settlements, assignments, credentials, or Jahez financial records were corrected.

## Historical review items

These were reported and left unchanged:

- **Chefz/SHIFTZ account `2607221690`, 19 October 2026:** three assignments cover the day, and their order cannot be safely inferred. Resolve their sequence explicitly before importing a shared-day report for that date.
- **Chefz/SHIFTZ FullTime account `2620593968`, 20 October 2026:** the assignment beginning 1 October ends on 20 October, and another assignment starts on 20 October. Inclusive dates therefore overlap by one day. Review the historical assignment dates before processing an affected FullTime settlement. The migration did not choose which date to change.

## Evidence and API status

Evidence is retained under `outputs/shared-day-hosted-20261021T074703533Z/`, including backup evidence, preflight, rehearsal verification, precommit/postcommit checks, legacy review, and [the deployment report](../outputs/shared-day-hosted-20261021T074703533Z/report.json).

Migration script SHA-256: `40D988A46ABBA33CF5B10E442687ABC2D1051ED089FCBC22BF144FCBDAA7C2A3`.

**The database update is complete. The updated API still needs deployment.** Older import binaries cannot safely write the new source/allocation schema; deploy the matching backend before importing Ninja/Chefz reports or using shared-day handover functionality.
