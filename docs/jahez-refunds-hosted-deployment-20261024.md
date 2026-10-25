# Jahez refunds and partial repayments — hosted database deployment

Applied and independently verified on **24 October 2026 at 16:48 Riyadh time** against hosted `db67927`.

Both application migrations are now applied:

- `20261024115050_SupportJahezPartialDebtRepayments`
- `20261024132700_AddJahezAccountFeeRefunds`

Application and Identity migration histories match the current local migration files: zero pending or unknown migrations. The API application was **not deployed** by this database update.

## Verified schema

- `jahez_v2.AccountFeeRefunds`: 17 columns, three restrictive foreign keys, three unique reference indexes, the occurrence-date index, and the refund amount/commission check.
- `jahez_v2.TransferredDebts.PaidAmount`: required decimal(18,2), with a default for existing rows and the updated debt-state check.
- `jahez_v2.MoneyMovements`: nullable decimal(18,2) `DashboardDebtPaid` and `CommissionPaid`, their allocation check, and a filtered non-unique transferred-debt index supporting multiple receipts.
- All three affected check constraints and all three refund foreign keys are enabled and trusted.

## Existing data and execution

A new server-side `COPY_ONLY` backup with `CHECKSUM` completed before the update. Both reviewed SQL scripts ran in one serializable transaction with an exclusive deployment lock, bounded lock waits, and `XACT_ABORT ON`.

Original column values were compared in both directions inside the transaction for **29 existing tables**: all existing Jahez V2/V3 tables, platform accounts, and rider assignments. New partial-payment fields were verified separately. Existing data remained unchanged:

| Record | Count |
| --- | ---: |
| Money movements | 153 |
| Ledger entries | 2,084 |
| Transferred debts | 4 |
| Refund records | 0 |

All four existing debts were unpaid; their new `PaidAmount` values are zero, and their rowversions were preserved. No refunds, receipts, account withdrawals, treasury postings, or test financial transactions were created. Paid-debt backfill had no applicable hosted rows.

Verification ran before commit, on a newly opened connection after commit, and again through an independent invocation that confirmed `AlreadyApplied` and `Verified` without reapplying changes. Restore verification was not performed; the backup remains on the hosting server. Application-level rollback/concurrency tests remain unavailable locally because LocalDB fails during startup; this hosted migration does not claim to exercise those financial workflows.

## Evidence and remaining deployment

- [Deployment report](../outputs/jahez-refunds-hosted-deployment-20261024T134805958Z/report.json)
- [Preflight summaries](../outputs/jahez-refunds-hosted-deployment-20261024T134805958Z/preflight.xml)
- [Backup completion evidence](../outputs/jahez-refunds-hosted-deployment-20261024T134805958Z/server-backup.xml)
- [Post-commit schema verification](../outputs/jahez-refunds-hosted-deployment-20261024T134805958Z/verification-after-commit.xml)
- [Final migration histories](../outputs/jahez-refunds-hosted-deployment-20261024T134805958Z/migration-history-after.xml)
- [Deployment/verification tool](../tools/Apply-JahezRefundsAndPartialRepayments.ps1): default is read-only; `-Apply` applies only these two reviewed migrations when pending and verifies without writes when already applied.

Deploy the updated API application to make the new endpoints available online. The external frontend must then wire the refund checkbox using the [refund handoff](jahez-account-fee-refunds.md).
