# Jahez opening import — 8 October 2026

Imported 199 current rider assignments into the live application database db67927 from جاهز 8-10.xlsx, worksheet حسابات المنصات. Saved 199 legacy Jahez handovers, 199 fee records, 199 assignment events, and 597 opening ledger entries. Last settlement dates were initialized directly without inventing historical payment receipts.

Opening balances through 7 October 2026: Jahez debt SAR 77,309.838; account fees SAR 0; commission SAR 113,850. New daily commission starts 8 October, or the day after handover for a handover on 8 October. Commission uses SAR 15 per day after the last settlement, through 7 October. Where no settlement date was supplied, handover is the anchor. Negative source Jahez balances become positive receivables; positive source balances become credits. Blank debt and fees become zero, as authorized.

Future dates were corrected by swapping day and month only where that produced a valid nonfuture date. Literal extra-zero typos such as 21-04-0-2026 and 6/7/20026 were normalized. Dates that remained inconsistent were held. The 20207 year and missing handover dates were not guessed.

At the initial import, 105 assignment rows remained pending due to conflicting dates, unmatched rider Iqamas, retired accounts, or the two-account limit. See pending-rows.csv. The 76 rows without an actual rider did not create assignments. All 181 accounts outside the applied set retain their original status and row version. Registered account owners, sponsors, cities, and payment models were preserved across all 380 accounts.

A transaction dry run rolled back successfully before commit. Post-commit checks found zero assignment/date mismatches, zero ledger balance mismatches, and zero riders above the two-account limit. No cashbox receipts or settlement payments were created. Recovery table snapshots, the original workbook, and the exact import plan with a SHA-256 manifest are retained in backup/.

## Follow-up: newer handover becomes settlement anchor

At the user’s instruction, when the corrected handover date is later than the corrected source last-settlement date, the handover date becomes LastSettlementPaymentAtUtc. Opening commission is max(0, 8 October 2026 minus handover date minus one day) × SAR 15. No payment receipt is fabricated.

This resolved 73 more assignments, committed after a successful rollback trial and a second recovery snapshot in additional/backup/. Their opening balances are debt SAR 23,847.088, fees SAR 350, and commission SAR 24,210. The original 199 assignments were preserved.

The live database now contains 272 imported assignments and 816 opening ledger entries. Combined opening balances are debt SAR 101,156.926, fees SAR 350, and commission SAR 138,060. Combined verification found zero assignment/date/balance mismatches and zero riders above the two-account limit. The 108 other Jahez accounts retain their previous status; 76 have no source rider and 32 assignment rows remain pending. Current pending-rows.csv lists those 32 rows.
