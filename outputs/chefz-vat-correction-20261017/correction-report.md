# Shefz VAT correction

17 October 2026. The user confirmed that delivery fees include VAT: SAR 115 / 1.15 = SAR 100 before company charges. The former flat 15% subtraction is incorrect for this requirement.

The backend calculation and rider/cycle reconciliation now use `round(sum(delivery fees) / 1.15, 2)`. VAT equals inclusive fees minus that amount. Existing treatment of tips, compensation, bonuses, deductions, freelancer company commission, full-time daily fees, and manual wallet settlements is preserved. Documentation and monetary examples were updated.

The fix is in the local checkout. It has not been deployed. No saved production settlement was changed.

## Prepared correction for the two supplied imports

Amounts are SAR. These corrections preserve each settlement's original assigned days and charged dates, isolating the VAT fix from the previously identified SAR 25 assignment-date adjustment.

| Item | Almokbela | Farei | Combined |
| --- | ---: | ---: | ---: |
| Settlements | 143 | 88 | 231 |
| Old VAT deduction | 17,564.40 | 14,027.55 | 31,591.95 |
| Correct VAT deduction | 15,273.38 | 12,197.85 | 27,471.23 |
| Correct net before company share | 100,371.62 | 80,226.15 | 180,597.77 |
| Correct company share | 16,239.23 | 12,518.30 | 28,757.53 |
| Old rider share | 82,036.49 | 65,987.67 | 148,024.16 |
| Correct rider share | 84,132.39 | 67,707.85 | 151,840.24 |
| Rider increase | 2,095.90 | 1,720.18 | 3,816.08 |

The SAR 4,120.72 reduction in VAT increases net earnings. Freelancer company shares also increase by SAR 304.64, so the rider increase is SAR 3,816.08.

Account 2591985425's later assignment-date correction is separate: applying its seventh charged day would move SAR 25 from rider to company relative to the figures above. The prepared VAT correction does not apply that date change or change collected-cash treatment.

## Third saved import

The same VAT issue affects the stored Commercial Gate import. From its stored financial breakdown, its rider share would change from SAR 4,199.04 to SAR 4,298.36, an increase of SAR 99.32. Its company share would change from SAR 741.01 to SAR 758.54, and VAT from SAR 895.95 to SAR 779.10.

The original Commercial Gate workbook was not supplied. The calculation uses its saved database snapshots, and the correction script excludes that import by default. An explicit scope switch includes it if authorized.

## Verification

- Focused Chefz workflow suite: **88 passed, 0 failed, 8 skipped**. Skips are the existing opt-in SQL/concurrency/older-sample checks.
- The actual two source workbooks pass the .NET parser and fresh read-only service previews with the new formula. Identical-file replay still returns the original saved snapshots, demonstrating why the database also needs an explicit correction.
- Regression examples include SAR 115 -> 100, SAR 230 -> 200, negative reversals, zero, cents, and SAR 1,000 -> 869.57. Multi-account, mixed account types, calendar days, signed balances, and save/replay workflows pass.
- An independent Decimal calculation generated proposed values for all 241 stored settlements. A .NET test recalculated every proposed settlement with the domain implementation and matched VAT, net, company share, rider share, and original assigned days exactly.
- The prepared SQL was executed only in its default preview mode. It confirmed **231 unchanged source snapshots** and returned the exact proposed totals shown above. It performed no production financial updates.

## Applying the saved-record correction

`review-and-apply-vat-correction.sql` defaults to `@Apply=0` and the two supplied imports only. Applying requires an explicit switch and an existing audit actor. Before updating it validates every original financial amount and the stored assignment/date/type snapshots. It aborts if any has changed since this plan was prepared.

Apply mode updates only VAT, net earnings, company share, and rider share, records before/after audit entries for every settlement, advances the Chefz dataset version, checks arithmetic, and commits atomically. It does not create another settlement, import, or payment. Current source snapshots are retained in `database-snapshot.json`, and every old/new amount is listed in `settlement-corrections.csv`.

Production application and financial-record correction remain pending. The live screen and production HTTP/API binary have not been verified with this patch.

Evidence: `correction-plan.json`, `settlement-corrections.csv`, `sql-preview-verification.xml`, `chefz-vat-correction.trx`, and the two `*-service-verification.json` files in this directory.
