# Hunger CR-005 — hosted database deployment

Applied to `db67927.public.databaseasp.net` / `db67927` on **11 November 2026 at 11:39 Riyadh time** (08:39:58 UTC), with independent verification at 08:40:07 UTC.

**Database only.** The API binaries were not published by this deployment, and `Features:HungerFinanceCr005` was not changed.

## Applied changes

Application migrations:

- `20261111071640_HungerCr005SealEvidenceExceptions`
- `20261111073327_HungerCr005InvoiceFedMonthClose`
- `20261111074551_HungerCr005AdjustmentApprovalDate`
- `20261111081325_HungerCr005WalletPendingDisposition`
- `20261111082612_HungerCr005FinanceReadViews`

Identity migration: `20261111071648_GrantHungerCr005Permissions`.

Result on the hosted database:

- nine seal columns on `hunger.FinancialEvents`, tables `hunger.EvidenceFiles` and `hunger.ExceptionApprovals`;
- `SettlementDocuments.RiderBalanceTotal` and `SettlementDocuments.ApprovalDate`;
- `WalletPending` allowed as an account disposition (no account uses it);
- schema `finance_read` with five views, each of which compiled and returned rows;
- five permission definitions and five SYSTEM_ADMIN grants;
- the five existing financial events (1 `WalletDelivery`, 4 `WalletReturn`) backfilled to `ApprovalStatus = Sealed`, `Role = Wallet`.

## Verification

1. **Preflight** (read-only): exact server and database, baseline `20261110105351` / `20261110105334`, none of the CR-005 migrations, objects or seed ids present.
2. **Rehearsal**: all 44 statements run in one Serializable transaction under an application lock, completeness checks passed, then rolled back; the rollback was confirmed on a fresh connection.
3. **Backup**: `D:\Services\MSSQL\Backup\db67927_before_hunger_cr005_20261111_083943.bak` (`COPY_ONLY`, `CHECKSUM`). Restore was not tested.
4. **Apply**: same script hashes as the rehearsal, one Serializable transaction, committed.
5. **Verify**: separate run on a fresh connection confirmed all six migrations and all objects.

Counts and fingerprints were identical before and after inside the transaction for financial events, settlement documents, wallet reviews, remittance lines, account business details, treasury postings, accounts, assignments, and all other permission definitions and grants.

## Still to do

- **Publish the API.** Until then the running API ignores the new columns; any financial event it creates gets `ApprovalStatus = NotRequired` and will not appear in the seal queue.
- **Grant the finance reader.** `GRANT SELECT ON SCHEMA::finance_read TO <reader login>` was not run; no reader login was specified.
- **Seed `hunger.FinanceConfigurations`.** Still 0 rows; every money post fails without one.
- **Assign the five new permissions** to the accountant and supervisor roles. Only SYSTEM_ADMIN has them.
- Keep `Features:HungerFinanceCr005` off until the front-end changes are live.

## Evidence

- `outputs/hunger-cr005-hosted-20261111/` — `preflight.json`, `rehearse.json`, `apply.json`, `verify.json`, before/inside/after state and the seal backfill.
- `database/scripts/hunger-cr005-application.sql`, `database/scripts/hunger-cr005-identity.sql`
- `tools/Apply-HungerCr005Migrations.ps1`

SHA-256:

```text
hunger-cr005-application.sql
916E959C54BA934C8B9D15ADABF1DA8739BDB41446BFC625A005684B2A2748E4

hunger-cr005-identity.sql
0BE8C403B8D8F410616D8A349E972780C36F3AEB601BF673D67C20F037BF8DCA
```
