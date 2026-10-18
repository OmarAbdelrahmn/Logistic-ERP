# Shefz order-money reconciliation

Follow-up: the user subsequently confirmed that delivery fees include VAT and must be divided by 1.15. The uncertainty described below is resolved, and the original VAT formula is incorrect for the confirmed requirement. See the [tested correction and proposed saved-record totals](../chefz-vat-correction-20261017/correction-report.md). This report preserves the original audit findings and stored amounts.

Reviewed 17 October 2026, Riyadh time. Period: 1–15 October 2026. Amounts are SAR.

The two original files exactly match the SHA-256 hashes of two saved imports in hosted database `db67927`. All 9,115 source rows are represented in 231 saved account-assignment settlements. Source monetary columns reconcile with the database. There are no missing settlements, duplicate orders within or between these files, duplicate assignment-period settlements, or invalid stored arithmetic.

The saved amounts follow the documented flat 15% delivery-fee deduction. This alone does not establish that the deduction is the correct VAT treatment under the Shefz agreement. One settlement remains based on an assignment end date corrected after import, producing a SAR 25 difference from today's calculation.

## Source and saved totals

| Item | Almokbela, 2151034 | Farei, 2151351 | Combined |
| --- | ---: | ---: | ---: |
| Source rows | 5,067 | 4,048 | 9,115 |
| Saved settlements | 143 | 88 | 231 |
| Delivery fees | 117,096.00 | 93,517.00 | 210,613.00 |
| Deduction labelled VAT | 17,564.40 | 14,027.55 | 31,591.95 |
| Delivery fees after deduction | 99,531.60 | 79,489.45 | 179,021.05 |
| Tips | 953.00 | 1,032.00 | 1,985.00 |
| Compensation | 961.00 | 664.00 | 1,625.00 |
| Bonuses | 0.00 | 0.00 | 0.00 |
| Other deductions | 3,365.00 | 2,789.00 | 6,154.00 |
| Net earnings before company share | 98,080.60 | 78,396.45 | 176,477.05 |
| Company share, saved | 16,044.11 | 12,408.78 | 28,452.89 |
| Rider share, saved | 82,036.49 | 65,987.67 | 148,024.16 |

Sources: `C:/Users/omarf/Downloads/2151034 Albawaba Almokbela (8).xlsx` and `C:/Users/omarf/Downloads/2151351 Albawaabah Farei (8).xlsx`, each `Sheet1`, rows 2 through 5068 and 4049 respectively. Headers are in row 1. Column G is delivery fees, H deductions, M bonuses, N compensation, and O tips. The files contain no separate VAT amount or statement of whether fees include VAT. Their company-commission column F totals zero in both files. The ERP applies its own company-share rules independently of that column.

The saved import filenames have an additional `(1)` suffix, but their content hashes are identical to the supplied files:

- Almokbela: `6F9DE399C84DD320FD8290E43F4B5005DD94C99E185F41BED4FFC849F7681B45`, import `01a149c2-8e1c-7a9d-956a-7ddfab26cc0d`.
- Farei: `F03F3F3E3220D22529491AE1A87D1136416D8336A9FB8073ACEDB930D15D897C`, import `01a149c3-f79a-7d1d-beb0-bb8a766a149c`.

## How the current calculation works

1. Sum delivery fees by payable rider and half-month cycle, retain 85%, and reconcile cents across that rider's accounts.
2. Add tips, compensation, and bonuses. Subtract the magnitude of deductions.
3. For freelancer accounts, charge another 15% of that account's resulting net earnings, with cents reconciled by rider/cycle. For full-time accounts, charge SAR 25 per inclusive assigned calendar day, including days without orders.
4. Rider share equals net earnings minus company share. Negative balances remain negative.

The two 15% amounts have different bases. For a freelancer with SAR 1,000 delivery fees and no adjustments, the first deduction is SAR 150, company share is SAR 127.50, and rider share is SAR 722.50. A full-time account assigned for all 15 days instead incurs a SAR 375 company charge.

The stored field named `GrossEarnings` means earnings **after** the delivery-fee deduction and adjustments for order imports. The import response calls this amount `netEarnings`. A screen that labels `GrossEarnings` as original delivery fees, subtracts VAT a second time, or recomputes a company percentage on every account would display the wrong result. The frontend was not available in this backend checkout, so those display failures have not been established.

## VAT basis requires confirmation

The code uses `delivery fees × 0.85`, as explicitly documented in the existing settlement specification. It does not extract VAT from a VAT-inclusive total.

For a VAT-inclusive amount, ZATCA's guidance specifies an embedded VAT fraction of 15/115. The amount excluding VAT is consequently `inclusive amount ÷ 1.15`. Example: SAR 115 includes SAR 15 VAT and SAR 100 before VAT. The current ERP would deduct SAR 17.25 and retain SAR 97.75 from that same amount.

If these delivery-fee columns are VAT-inclusive, the current formula over-deducts compared with extracting included VAT:

| Conditional comparison | Almokbela | Farei | Combined |
| --- | ---: | ---: | ---: |
| Current deduction | 17,564.40 | 14,027.55 | 31,591.95 |
| Embedded VAT at 15/115, rounded per rider/cycle | 15,273.38 | 12,197.85 | 27,471.23 |
| Difference before company-share recalculation | 2,291.02 | 1,829.70 | 4,120.72 |

This SAR 4,120.72 is conditional, and is not a verified rider payout adjustment. Company percentages would also need recalculation if the VAT basis changes. If the commercial agreement instead requires a flat 15% withholding from these fees, the current arithmetic matches that rule. The workbooks do not resolve the agreement's tax basis.

Official reference: [ZATCA Economic Activity guideline, section 8.3](https://www.zatca.gov.sa/en/HelpCenter/guidelines/Documents/Economic%20Activity.pdf).

## Confirmed SAR 25 difference after a date correction

Account / Driver ID `2591985425`, historical assignment `ce52e580-07b7-4476-9711-8a48db7039bb`, belongs to the Almokbela import.

| Item | Saved at import | Recalculated with current assignment |
| --- | ---: | ---: |
| Charged dates | 1–6 October | 1–7 October |
| Assigned days | 6 | 7 |
| Company charge | 150.00 | 175.00 |
| Net earnings | 546.70 | 546.70 |
| Rider share | 396.70 | 371.70 |

The import was saved at 15:07:29 Riyadh time on 17 October. The database assignment audit records a user-requested end-date correction from 6 October to 7 October at 15:47:21 Riyadh time, after that import. The saved settlement preserves the original six-day snapshot. Its original arithmetic is correct, but it no longer matches the corrected assignment dates.

Reuploading identical file bytes returns the saved snapshot. A changed-byte preview recalculates seven days but reports existing-settlement conflicts, so it cannot overwrite the saved record. If the corrected date must govern the financial settlement, an explicit SAR 25 company adjustment / rider reduction is required through an approved correction process. This audit did not change the settlement.

The other 230 settlements match current source data, assignment attribution, account types, company rules, and rounding.

## Another import and list pagination can change the displayed total

The database also contains `2151670 Commercial Gate (2).xlsx`, imported at 18:21 Riyadh time on 17 October. Its 263 source rows produced 10 settlements: delivery fees SAR 5,973.00, VAT deduction SAR 895.95, net earnings SAR 4,940.05, company share SAR 741.01, and rider share SAR 4,199.04.

Including all three imports produces net earnings SAR 181,417.10, company share SAR 29,193.90, and rider share SAR 152,223.20 across 241 settlements. These are different scopes from the two supplied files. The third source workbook was not supplied or independently audited here; its values above are the stored database values only.

The general `GET /api/chefz/settlements` endpoint returns one page of at most 100 settlements and does not return an overall aggregate or total-record count. Summing one page would not cover these 241 settlements. Its current filters are account and rider; the saved-import endpoint returns a complete specified import. Whether the live screen combines imports or sums only a page remains unverified.

## Collected cash is outside the earnings calculation

Farei `Sheet1!K2139` contains SAR 66.75 cash collected by Driver ID `2551645738`, order `297437887`, dated 13 October. This is the only nonzero collected-cash entry in the two files. The importer does not read column K and neither deducts nor records it in these settlement amounts.

That full-time account has saved net earnings of SAR 2,015.75, company share SAR 375, and rider share SAR 1,640.75. If the rider retains the SAR 66.75 and it must offset a company transfer, the transfer would be SAR 1,574.00 before any other cash reconciliation. If Shefz already settles this cash elsewhere, an additional deduction could double-count it. Confirm its settlement route before changing money. This is an unmodelled reconciliation item, not proof of an earnings-import arithmetic error.

## Verification and limits

- Independent Python Decimal reconciliation reads the original files, resolves Driver IDs to active/non-deleted database accounts and covering assignments, recomputes monetary rules, and compares every saved settlement field.
- Hosted SQL queries are SELECT-only apart from transaction/session controls. The snapshot has zero invalid stored arithmetic and zero duplicate assignment-period keys.
- Existing focused Chefz workflow suite: 79 passed, 8 skipped. Skips cover opt-in SQL/concurrency/older sample checks; their absence is not evidence of SQL integration success.
- Two additional opt-in tests passed. They run the actual .NET parser and service against the hosted database using a SaveChanges interceptor that rejects writes. They verify identical-file replay and a fresh in-memory ZIP repackage of each original workbook. The fresh calculation preserves all worksheet values, re-resolves current assignments, reproduces the SAR 25 difference, and is blocked from saving by existing-settlement conflicts. Counts and tracked changes are checked before/after.
- The .NET service is tested from this checkout. This does not establish the deployed API binary version, browser rendering, frontend filters, pagination totals, or production HTTP authorization behavior. Test permission checks use the existing test double.
- No source workbook, business record, or production configuration was edited. No deployment or financial correction was performed.

Evidence in this directory: `database-snapshot.json`, `read-only-queries.sql`, `reconciliation.json`, `settlement-reconciliation.csv`, `2151034-service-verification.json`, `2151351-service-verification.json`, and `chefz-money-readonly.trx`.
