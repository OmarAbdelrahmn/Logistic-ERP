# Ninja October performance — unresolved rows

Source: the supplied October workbook, 3,072 rows covering 1–24 October 2026.

**User decision:** import the 3,032 uniquely matched rows as a distinct validated batch. Keep these 40 rows pending; do not change assignment periods or invent shared-day splits.

| Source row | Captain ID | Business date | Orders | Hours | Issue |
|---:|---|---|---:|---:|---|
| 27 | 215723 | 2026-10-01 | 16 | 12.85000000 | No historical assignment covers this date |
| 39 | 215460 | 2026-10-01 | 17 | 13.28333333 | No historical assignment covers this date |
| 45 | 214310 | 2026-10-01 | 11 | 9.70000000 | No historical assignment covers this date |
| 53 | 214271 | 2026-10-01 | 2 | 3.31666667 | No historical assignment covers this date |
| 115 | 215810 | 2026-10-01 | 13 | 9.13333333 | Shared handover day: earlier rider orders and hours are missing |
| 152 | 215723 | 2026-10-02 | 19 | 13.61666667 | No historical assignment covers this date |
| 167 | 215460 | 2026-10-02 | 17 | 12.25000000 | No historical assignment covers this date |
| 201 | 26528 | 2026-10-02 | 14 | 11.95000000 | No historical assignment covers this date |
| 321 | 215774 | 2026-10-03 | 3 | 2.60000000 | No historical assignment covers this date |
| 323 | 215769 | 2026-10-03 | 8 | 6.65000000 | No historical assignment covers this date |
| 344 | 215460 | 2026-10-03 | 17 | 12.73333333 | No historical assignment covers this date |
| 378 | 26528 | 2026-10-03 | 6 | 7.33333333 | No historical assignment covers this date |
| 472 | 215460 | 2026-10-04 | 15 | 13.53333333 | No historical assignment covers this date |
| 502 | 214197 | 2026-10-04 | 7 | 5.93333333 | No historical assignment covers this date |
| 550 | 215460 | 2026-10-05 | 16 | 13.70000000 | No historical assignment covers this date |
| 674 | 215460 | 2026-10-06 | 18 | 13.60000000 | No historical assignment covers this date |
| 848 | 215460 | 2026-10-07 | 18 | 12.40000000 | No historical assignment covers this date |
| 922 | 215460 | 2026-10-08 | 18 | 13.41666667 | No historical assignment covers this date |
| 1006 | 295975 | 2026-10-08 | 13 | 12.85000000 | No historical assignment covers this date |
| 1007 | 295966 | 2026-10-09 | 2 | 6.61666667 | No historical assignment covers this date |
| 1014 | 222883 | 2026-10-09 | 4 | 5.93333333 | No historical assignment covers this date |
| 1042 | 215460 | 2026-10-09 | 14 | 12.45000000 | No historical assignment covers this date |
| 1143 | 222883 | 2026-10-10 | 4 | 4.23333333 | No historical assignment covers this date |
| 1167 | 215460 | 2026-10-10 | 16 | 12.70000000 | No historical assignment covers this date |
| 1290 | 215460 | 2026-10-11 | 16 | 13.31666667 | No historical assignment covers this date |
| 1416 | 215460 | 2026-10-12 | 14 | 12.90000000 | No historical assignment covers this date |
| 1544 | 215460 | 2026-10-13 | 16 | 13.46666667 | No historical assignment covers this date |
| 1676 | 215460 | 2026-10-14 | 17 | 12.26666667 | No historical assignment covers this date |
| 1681 | 214330 | 2026-10-14 | 10 | 11.63333333 | Shared handover day: earlier rider orders and hours are missing |
| 1838 | 279831 | 2026-10-15 | 5 | 6.08333333 | No historical assignment covers this date |
| 1852 | 215771 | 2026-10-15 | 4 | 3.11666667 | No historical assignment covers this date |
| 1872 | 215460 | 2026-10-15 | 12 | 11.91666667 | No historical assignment covers this date |
| 1940 | 215460 | 2026-10-16 | 13 | 12.05000000 | No historical assignment covers this date |
| 2133 | 215460 | 2026-10-17 | 9 | 10.95000000 | No historical assignment covers this date |
| 2203 | 215460 | 2026-10-18 | 13 | 12.46666667 | No historical assignment covers this date |
| 2223 | 214205 | 2026-10-18 | 4 | 5.16666667 | No historical assignment covers this date |
| 2382 | 215460 | 2026-10-19 | 13 | 12.03333333 | No historical assignment covers this date |
| 2659 | 203023 | 2026-10-21 | 1 | 1.60000000 | No historical assignment covers this date |
| 2680 | 272575 | 2026-10-22 | 11 | 7.68333333 | No historical assignment covers this date |
| 3000 | 159271 | 2026-10-24 | 1 | 2.18333333 | Shared handover day: earlier rider orders and hours are missing |

## Assignment-history context

The following periods are existing database facts, not proposed corrections. A name in the workbook or the current account owner does not establish who worked on an uncovered date.

| Captain ID | Held dates | Recorded assignment periods |
|---|---|---|
| 203023 | 2026-10-21 | No assignment recorded |
| 214197 | 2026-10-04 | 2026-10-01 → 2026-10-03 (assignment 01a16dc8-babf-7b75-bed8-d3759e5c51d6) |
| 214205 | 2026-10-18 | 2026-10-01 → 2026-10-17 (assignment 01a16e27-61b8-70c5-8f2a-7e8c08221a46) |
| 214271 | 2026-10-01 | 2026-10-02 → 2026-10-03 (assignment 01a16dba-5d39-78df-95b3-dd322a743205); 2026-10-04 → current (assignment 01a16e9b-db9d-7eb9-8936-047c68a6a916) |
| 214310 | 2026-10-01 | 2026-10-02 → current (assignment 01a16ec6-ba30-7a91-8a2f-02f352ab6185) |
| 215460 | 2026-10-01, 2026-10-02, 2026-10-03, 2026-10-04, 2026-10-05, 2026-10-06, 2026-10-07, 2026-10-08, 2026-10-09, 2026-10-10, 2026-10-11, 2026-10-12, 2026-10-13, 2026-10-14, 2026-10-15, 2026-10-16, 2026-10-17, 2026-10-18, 2026-10-19 | 2026-10-20 → current (assignment 01a17279-6ca2-7ffb-ba2e-d686f9c877d6) |
| 215723 | 2026-10-01, 2026-10-02 | 2026-10-03 → current (assignment 01a16eca-bd24-7225-a903-fe48b8b0f4a9) |
| 215769 | 2026-10-03 | 2026-10-01 → 2026-10-01 (assignment 01a16da3-d217-74e8-9c72-584b6fbfdcf1); 2026-10-04 → current (assignment 01a1729e-c2b3-7836-87c8-53b295eefea5) |
| 215771 | 2026-10-15 | 2026-10-16 → current (assignment 01a172a2-b712-7022-9e1f-78e0f27319dd) |
| 215774 | 2026-10-03 | 2026-10-04 → 2026-10-06 (assignment 01a16dea-1ea7-72f5-b701-25f72b5104f5) |
| 222883 | 2026-10-09, 2026-10-10 | 2026-10-01 → 2026-10-08 (assignment 01a16dfe-51d2-7756-9196-512dc642f0ca) |
| 26528 | 2026-10-02, 2026-10-03 | 2026-10-09 → 2026-10-11 (assignment 01a16dc1-05ad-753b-b763-038cd45b25ca) |
| 272575 | 2026-10-22 | 2026-10-23 → current (assignment 01a16ed7-3d27-76a6-b8ef-75625448809a) |
| 279831 | 2026-10-15 | 2026-10-16 → 2026-10-17 (assignment 01a16e14-4f0f-72cd-b2a1-416dc1b8ada5); 2026-10-18 → current (assignment 01a16ebf-f9ee-7ee3-a868-b45f6591ccb9) |
| 295966 | 2026-10-09 | No assignment recorded |
| 295975 | 2026-10-08 | No assignment recorded |

## Information required to release the held rows

- For each uncovered date, confirm the actual rider and correct assignment period with evidence. Any history correction must use a reviewed workflow.
- For Captain IDs 215810 (1 October), 214330 (14 October), and 159271 (24 October), supply earlier-holder completed orders and working hours. The existing allocator will calculate the last holder’s remainder.
- Re-run validation as a separate batch after those facts are confirmed.

No held row is represented as zero performance or credited to the registered owner.
