# Jahez October roster update — 17 October 2026

Applied to the hosted application database `db67927` and verified using a fresh connection. All seven reconciliation checks and two additional owner/date checks returned zero mismatches. Every employee dashboard owner now has a rider profile, and all nine date corrections were independently verified. Before-image backups and the exact source workbook are retained in this folder.

| Change | Result |
| --- | ---: |
| Workbook data rows | 384 |
| Unique source accounts | 380 |
| Identical duplicate rows ignored | 4 |
| Existing account records reconciled | 378 |
| New accounts created | 2 |
| Employee-owner rider profiles created | 19 |
| Account sponsors corrected by 70-number | 39 |
| Inactive accounts set to Suspended | 46 |
| New rider assignments with both dates set | 295 |
| Existing assignments preserved | 3 |
| Rider entries pending resolution | 13 |
| Vehicle-to-account links created | 374 |
| Owner HR sponsor differences reported | 87 |

The source covers 380 accounts. Two missing accounts (405057 and 405009) were created against their exact owner Iqamas and dashboard sponsors, using the owners’ existing operating city (also matching the listed vehicle city). Existing accounts 472477 and 472847 are absent from the source and were preserved, leaving 382 Jahez master accounts.

Owners were matched by Iqama and existing owner links were validated. 19 employee owners received new rider profiles without changing their employment classification or HR status. One additional missing profile was created by another operation during preparation and was reused. Account sponsor IDs were resolved through the full `Sponsor.EmployerIdentityNumber` values 7034861059 and 7015658094. The 87 differences between owner HR sponsor and dashboard sponsor are recorded in `owner-sponsor-differences.csv`; the HR sponsor records were preserved.

Imported rider `EffectiveFrom`, session `StartedAtUtc` and `LastSettlementPaymentAtUtc` use the same source/corrected date at midnight Riyadh time. Commission starts on the following day. No historical payment receipt, account fee, opening balance, ledger entry or settlement operation was created. Existing financial records were compared before commit and preserved.

The source’s Active status maps to Assigned when an assignment exists, otherwise Available. Inactive maps to Suspended for future reactivation. The complete result for each account is in `account-results.csv`.

## Date corrections supplied in chat

| Account | Handover and last تصفية |
| --- | --- |
| 460555 | 2026-10-17 |
| 404899 | 2026-09-12 |
| 472431 | 2026-10-15 |
| 492241 | 2026-10-11 |
| 472844 | 2026-10-13 |
| 474642 | 2026-10-13 |
| 475162 | 2026-10-17 |
| 470544 | 2026-10-04 |
| 460711 | 2026-10-16 |

## Pending rider entries

Three inactive accounts are intentionally held Suspended at the user’s instruction. The other ten entries need account selection, corrected rider data or reconciliation with existing payment history.

| Account | Listed rider Iqama | Source date | Reason |
| --- | --- | --- | --- |
| 420509 | 2215204666 | 2026-10-11 | Exceeds the global two-account rider limit; account selection needed. |
| 456454 | 2539354528 | 2026-10-11 | User confirmed: keep the account Suspended and leave the listed rider assignment pending. |
| 458394 | 2626069906 | 2026-10-15 | User confirmed: keep the account Suspended and leave the listed rider assignment pending. |
| 476885 | 2501676890 | 2026-10-12 | Exceeds the global two-account rider limit; account selection needed. |
| 399990 | 2630669634 | 2026-10-05 | Current rider differs and existing fee receipts/settlement requests are linked. Current assignment and financial history preserved. |
| 404654 | 2546390671 | 2026-10-08 | Current rider differs and existing fee receipts/settlement requests are linked. Current assignment and financial history preserved. |
| 404719 | 2608130296 | 2026-10-13 | Current rider differs and existing fee receipts/settlement requests are linked. Current assignment and financial history preserved. |
| 491306 | 2501676890 | 2026-10-14 | Exceeds the global two-account rider limit; account selection needed. |
| 457195 | #N/A | Missing | Source rider is missing or invalid (#N/A). No handover/last clearance date supplied. |
| 458167 | 2501676890 | 2026-10-01 | Exceeds the global two-account rider limit; account selection needed. |
| 459049 | 2215204666 | 2026-10-05 | Exceeds the global two-account rider limit; account selection needed. |
| 457357 | 2460730217 | 2026-10-15 | User confirmed: keep the account Suspended and leave the listed rider assignment pending. |
| 456420 | 2462687274 | 2026-10-12 | Exceeds the global two-account rider limit; account selection needed. |

Capacity is shared across platforms. Iqama 2501676890 has three source Jahez accounts (476885, 491306, 458167); select two. Iqama 2215204666 already has one Chefz account, so only one of Jahez 420509/459049 fits. Iqama 2462687274 already has two Chefz accounts, so Jahez 456420 remains pending. All other-platform assignments were preserved.

For accounts 399990, 404654 and 404719, current riders differ from the sheet and current sessions already have account-fee receipts and unexecuted settlement requests. Their assignments, sessions, dates and payments were retained. Resolve those records before replacing the riders; this import did not reattribute money to another person.

## Vehicle matching and warnings

Vehicle-to-platform-account links were created using exact serial-number matches. These are direct account links, independent of physical rider-to-vehicle handover records. Their effective date is the import time because the sheet supplies no historical vehicle-link date. Existing physical vehicle assignments were preserved.

Serial 720438120 was not found (accounts 472969 and 472484). Serial 400604220 was not found (account 472526). Accounts 405144, 405059 and 408243 have no serial supplied. No vehicle was guessed or created.

The existing fleet policy permits links with warnings. `vehicle-link-warnings.csv` lists 218 links with sponsor, city, account-status or vehicle-status warnings. `vehicle-capacity-warnings.csv` lists 53 vehicle/city groups exceeding the configured capacity (three motorcycle accounts or two car accounts per platform and city). These warnings do not change sponsor ownership, city, odometer or physical vehicle handover history.

## Recovery and verification

The import was first executed and verified in a transaction that rolled back. It was then committed after another verification. A fresh database connection confirmed owner/sponsor/status matches, created profiles, actual rider Iqamas, assignment dates, both clearance dates, vehicle serial links, rider capacity, absence of imported financial charges/receipts and the audit record.

Every pre-existing rider profile, rider assignment, account session, vehicle-account assignment and all Jahez financial/import tables were preserved byte-for-byte. Employees, sponsors, vehicles, physical vehicle assignments, credentials, platform definitions, contracts and accounts outside the source were also checked for preservation inside the transaction.

Before images are saved as `*-before.xml`; `source.xlsx`, `plan.json`, `executed.sql`, `verify.sql`, `transaction-verification.xml` and `post-commit-verification.xml` identify the exact input, executed changes and checks. A unique audit correlation ID blocks accidental replay. This import adds records and updates only source account sponsor/status metadata; no rows were deleted. Recover only the affected records from the backups after reviewing subsequent live activity.
