# Ninja worksheet corrections applied to the hosted database

Applied **25 October 2026, 18:23:26 Riyadh time**, database `db67927`.

Batch: `01a173a0-341b-72bb-93fc-b28dafcc0961`. Source SHA-256: `8101ecdd5557cbb1feb060b56c4db15af37eba368838424d1a59a46ebf4b0973`.

## Result

All **229 source periods** are represented exactly once among visible assignments: **151 current** and **78 historical**. The latest account payment model/work city matches the worksheet for **207 accounts**. Current assignments include **21 salary** and **130 commission** accounts.

| Correction | Applied result |
| --- | --- |
| Existing assignment payment models | 30 periods corrected: 16 current, 14 historical |
| Account payment defaults | 28 corrected: 16 current, 12 historical-only accounts using their latest period |
| Account operating cities | 7 corrected: 5 current and 2 historical-only accounts |
| Historical work cities | Preserved separately on every source-period snapshot |
| Account login phone | 10 corrected, using the first number for multi-number cells |
| Current rider contact | All 151 match normalized worksheet phones; 109 stored strings updated, including 14 actual number differences and 95 formatting-only changes |
| Current rider nationality | All 151 match worksheet labels; 72 updates include 3 nationality conflicts and 69 equivalent-label normalizations |
| Registered owner of 255527 | Existing employee identity corrected to 2629274197; actual assignment rider 2526331299 preserved |
| Newly confirmed work | Current 222872 and 272573 plus historical 214299 created with explicit user authorization |
| Registration prerequisites | 3 registrations and 2 missing registered-owner rider profiles created; existing employee status preserved |
| Duplicate history of 215459 | One 6 October assignment retained; duplicate assignment and its empty daily-allocation row soft-deleted, with audit records preserved |
| Account 286371 | Remains without current work; recorded historical dates retained |
| Account 267570 | Current work starts 23 October as instructed |
| Source period details | 229 immutable-source snapshots retained in existing assignment agreement reference/notes fields |

The source snapshots include worksheet names, nationality, first/secondary phones, vehicle class/ownership, actual work city, source placement labels and source-row/file hash. They contain no password values and exclude the ignored **نوع البديل** column. Original pending/rejected labels are retained as provenance; the actual assignments reflect the user's explicit override.

Current account operating cities represent the latest worksheet work location. Existing registration-city records were preserved. Historical work-city values remain in their period snapshots. Existing complete employee names were retained rather than overwritten with abbreviations.

## Verification and preservation

- The correction was rehearsed on SQL Server and rolled back; a fresh read confirmed full rollback of all compared tables.
- The final apply used a serializable transaction, account write locks and strict source-entity rowversion guards. Dataset counters incremented their locked current values so unrelated live work was retained.
- Before commit, exact row backups were written and read back with matching fingerprints. Bidirectional SQL comparisons verified credentials and rows outside the correction manifest. Original row IDs were retained, with no hard deletes.
- Three new assignments and the duplicate correction were audited. There are 535 Ninja assignment events after the update.
- A fresh post-commit connection verified every mapping, payment, account city and current contact value. An independent JSON reconciliation confirmed 229 source snapshots and 207 latest account values.
- A repeat verification returned `AlreadyAppliedVerified`, with **zero database writes**.
- The 120 visible accounts absent from the workbook, other platforms, original registration records and unrelated employee rows were preserved at commit. Normal user activity added an unrelated employee/profile between snapshots; these additions were not removed or overwritten.

## Backup and recovery evidence

The server COPY_ONLY checksum backup completed. The hosting login cannot run `RESTORE VERIFYONLY` because it lacks CREATE DATABASE permission in master. Server restore verification is therefore **not confirmed**. Exact pre-change table backups were independently read back and verified before commit; transaction rollback was exercised in rehearsal. Restoring the saved XML backup after commit has not been exercised.

- Verified row backup: `C:\Users\omarf\OneDrive\Documents\ChatGPT\Logistic ERP\artifacts\ninja-workbook-sync-apply-20261025T152244586Z\before-and-verification.xml`
- SHA-256: `FFE79650728B2BD87EF98B12D5BAD1C6098C0C1B82299E867E7F3806255B09F0`
- Server backup metadata: `C:\Users\omarf\OneDrive\Documents\ChatGPT\Logistic ERP\artifacts\ninja-workbook-sync-apply-20261025T152244586Z\server-backup.xml`
- Machine reconciliation: `outputs/ninja-workbook-sync-20261025/final-reconciliation.json`
- Exact correction manifest: `outputs/ninja-workbook-sync-20261025/approved-plan.json`
- Parameterized SQL: `outputs/ninja-workbook-sync-20261025/approved-sync.sql`
- Fresh-connection evidence: `outputs/ninja-workbook-sync-20261025/postcommit-verification.xml`

Recovery must use the exact batch IDs and compare live values before restoring; do not blindly overwrite later user work. Superseded duplicate rows remain available, and assignment events/audit records retain their pre-change values. The encrypted credential table backup contains ciphertext only.

## Scope

This applies the approved items 1–4 and the earlier identity, placement, date and duplicate instructions. Saved passwords and existing Ninja rider classifications were **unchanged**. No schema migration, API binary publication or frontend deployment was performed. Existing APIs expose the corrected account and employee fields; the generic assignment-history API already exposes agreement notes containing source-period details. Dedicated UI fields for those additional source details remain a separate presentation change.
