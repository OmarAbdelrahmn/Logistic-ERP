# Chefz account type changes

Changing a configured account's type now records an immutable dated transition. The conversion day belongs to the previous type; the new type applies from the next calendar day, using Riyadh business dates. Initial configuration still establishes the account's baseline type without inventing a conversion.

## Account update

The existing account details request accepts optional `accountTypeChangeDate` (`yyyy-MM-dd`). If omitted on a type change, it defaults to today in Riyadh. Example:

```json
{
  "accountType": "FullTime",
  "rowVersion": "<current details row version>",
  "accountTypeChangeDate": "2026-10-07"
}
```

October 7 remains Freelancer; FullTime starts October 8. Date selection is needed only when changing an already configured type. Future conversion dates, repeated/out-of-order transitions, and changes affecting any saved settlement are rejected. Changing other details without changing the type does not create history. Shared days before the new FullTime start remain allowed; shared days during FullTime are rejected.

Account detail GET returns `accountTypeHistory`: `previousType`, `accountType`, `changeDate`, `effectiveFrom`. The account's main `accountType` remains the latest selected type; settlement calculations and daily allocation use the type applicable on the actual business date.

## Order import settlements

Order dates determine the commission period automatically. For Freelancer, the existing 15% commission on VAT-exclusive delivery fees applies, excluding tips, compensation, bonuses and deductions. FullTime charges SAR 25 for each assigned calendar day belonging to that type, including days without orders. One settlement per assignment/cycle is retained. Multiple changes in one cycle are supported.

Mixed settlements persist and return `fullTimeDays` and `freelancerCommissionBase`. `companyAmount` combines the percentage commission and the daily charge. `assignedDays` still describes all assigned days; do not multiply it by 25 for mixed settlements. `accountType` describes the last applicable segment, so consumers must use the breakdown fields when present. Existing records without breakdown fields retain null values.

Example: assignment October 1–15, conversion October 7, SAR 1,150 delivery fees through October 7 and SAR 2,300 afterward. Freelancer commission = 1,000 × 15% = SAR 150; FullTime = 8 × 25 = SAR 200; total company amount = SAR 350.

## Manual wallet settlements

Total start/end balances cannot establish earnings earned before a conversion. For mixed periods, preview/save require `walletAmountsAtTypeChanges`, keyed by each **new type's first day**, with the balance at the start of that day (equivalently, the end of conversion day). Obtain those boundary dates from account history and intersect them with the assignment/cycle.

```json
{
  "assignmentId": "<assignment id>",
  "year": 2026,
  "month": 10,
  "half": 1,
  "startWalletAmount": 100,
  "endWalletAmount": 3100,
  "walletAmountsAtTypeChanges": { "2026-10-08": 1100 }
}
```

The Freelancer earnings are 1,100 − 100 = SAR 1,000, so company amount is 150 + 8 × 25 = SAR 350. Missing, decreasing, out-of-range or more-than-two-decimal boundary balances are rejected. No time-based prorating of total earnings is used.

## Deployment and historical data

Apply migration `20261101140621_AddChefzAccountTypeHistory` before deploying the updated API. The incremental SQL is `database/scripts/chefz-account-type-history.sql`; it adds the transition table and two nullable settlement columns. Existing settlement values are preserved. The application migration history must already include `20261031143205_AllowJahezPetroAppDebtReceipts` before using that incremental script.

Historical conversion dates that were never recorded cannot be inferred from the current type. This migration intentionally does not invent them or recalculate saved settlements. Any legacy correction requires the actual conversion date and a separately reviewed data repair. Roll back API before rolling back the migration; schema rollback removes the new history/breakdown data.

## Verification

- Solution build succeeded with zero warnings/errors.
- Focused Chefz and platform day allocation tests: 155 passed, 13 skipped, zero failed. Skips cover optional SQL and supplied-workbook checks.
- The new SQL-backed test was attempted, but LocalDB failed to start; real SQL Server execution and account-lock behavior remain unverified in this environment. It follows the existing `CHEFZ_SQL_TESTS=1` opt-in convention.
- EF reports no pending model changes. The incremental migration SQL was generated and inspected before the hosted application recorded below.

## Hosted database application — November 1, 2026

At 17:17 Riyadh time, migration `20261101140621_AddChefzAccountTypeHistory` was applied to the verified hosted database `db67927` using `tools/Apply-ChefzAccountTypeHistory.ps1 -Apply`. The predecessor migration was present. The update and verification ran in one transaction, then committed successfully.

All 494 existing settlements and 464 account detail records were preserved, with settlement values compared across every original column. The new nullable columns, conversion history table, unique index, trusted check constraint, foreign key, and migration-history entry were verified. No conversion history or financial values were backfilled.

Backup and evidence: `outputs/chefz-account-type-history-20261101T141710617Z/` (`before.xml`, `migration.sql`, `verification.xml`, `verification.json`, `executed.ps1`). This action updated the hosted database schema only; API deployment remains separate.
