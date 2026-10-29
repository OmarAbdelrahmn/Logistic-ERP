# Ninja workbook synchronization review

**Update: approved corrections were applied and verified on 25 October 2026. See [deployment evidence](../../docs/ninja-workbook-hosted-sync-20261025.md). The analysis below records the original pre-change findings.**

Reviewed 25 October 2026. Hosted snapshot: **17:33:03 Riyadh time** (14:33:03 UTC), database `db67927`. This review used SELECT queries only. No hosted data, schema, application code, or API binaries were changed.

Source: `C:\Users\omarf\Downloads\تسكين نينجا سيستم.xlsx`.

SHA-256: `8101ecdd5557cbb1feb060b56c4db15af37eba368838424d1a59a46ebf4b0973`.

## User decisions recorded after the review

These decisions supersede the original proposed handling below. They have been recorded in the preview only; no hosted changes have been applied.

- Account **255527**: use **2629274197** for the registered-owner identity, following the owner-identity question. Keep the separate actual assignment rider **2526331299**. Correct the existing owner identity with dependent references preserved rather than create a duplicate person.
- Account **267570** starts on **23 October 2026**, as supplied. The user subsequently clarified that **286371 has no current work**. This agrees with the snapshot: 286371 is Available and its only assignment is ended, 20–24 October. Keep it historical only. No end-date change was supplied, so preserve its recorded 24 October end date; the historical cross-account overlap remains a documented source observation, not evidence of a second current assignment.
- Current accounts **222872 / 272573** are actual assignments, despite their source placement labels. The intended current-assignment total becomes **151**.
- History account **214299**, 1–6 October, is an actual historical assignment despite `لا يقبل`. Preserve the original source label as provenance while importing the user's corrected meaning.
- Use the **first phone number** wherever a source phone cell contains two numbers. Current account **215731** therefore uses **0558006043**. History row 19 uses **0542983073** and history row 63 uses **0558006043** as their first source contacts; retain secondary numbers as historical evidence.
- Ignore **نوع البديل** for import decisions and rider classification. The previous sponsorship-derived rider-type comparisons are superseded. The remaining vehicle labels come from **نوع المركبة**; do not infer engagement, sponsorship, or a supplier from the ignored column.
- Account **215459**: the user requested one record for the duplicate 6 October history period. Retain one canonical assignment and consolidate its two referenced day-usage rows without duplicating totals; preserve correction/audit evidence. This applies to the duplicate period, not the account's other valid history periods or its current assignment.

The remaining work includes payment/contact/nationality reconciliation, the referenced duplicate history record, independent vehicle/work-city/history properties, and secure current credential import. The 177 null rider types remain a database observation, but the original 177 proposed rider-type changes must not be applied using the ignored column.

## Scope and reconciliation

| Item | Result |
| --- | ---: |
| History sheet `البدلاء` | 78 assignment/attempt rows |
| Current sheet `الداتا الحالية` | 151 rows: 149 `تم`, 2 `لم يتم التسكين` |
| Distinct workbook account IDs | 207 |
| Workbook accounts missing from the hosted master | 0 |
| Actual riders missing/ambiguous by identity or without a rider profile | 0 |
| History rows with one exact account/rider/start/end match | 76 |
| Current rows with one exact account/rider/start/open-end match | 149 |
| Hosted Ninja assignments | 227: 149 open, 78 ended |
| Hosted Ninja master accounts | 328 total: 327 visible, 1 soft deleted |
| Visible hosted accounts absent from both sheets | 120 |
| Existing account-day usage/allocation records | 292 |
| Ninja performance/deduction records | 0 / 0 |

The history exceptions are one rejected attempt with no assignment and one workbook row matching two duplicate hosted records. The two current exceptions are explicitly not placed and have no assignment. The four exceptions therefore do not mean four missing assignments to insert.

Rows 89–90 in `البدلاء` contain summary/formula values without an account ID. Exclude them from import. Workbook cell content was treated as source data, not operational instructions. Issue counts in `issues.csv` are field-level observations and overlap across rows.

## Facts that need resolution

| Problem | Exact source | Existing state and proposed handling |
| --- | --- | --- |
| Owner identity differs | Current row 50, account **255527**: workbook **2629274197** | DB owner is **2620274197**, with the same English name. No employee exists under the workbook identity. Confirm whether this is a workbook typo or a master identity correction. Do not create a second person based on the name. |
| Rider works on two accounts over overlapping dates | History row 78: rider **2589847850**, account **286371**, 20–24 October. Current row 143: account **267570**, starts **23 October** | Both periods already exist in the DB. Confirm the correct handover dates; the overlap is 23–24 October under inclusive end dates. A correction must preserve the original history and its audit trail. |
| Two numbers in one current phone cell | Current row 75, account **215731**: **0558006043 / 0503087628** | Current DB login phone is **0570787133**. Preserve both source numbers and select the intended login number. Never concatenate them. |
| Two numbers in historical phone cells | History row 19, account **215809**: **0542983073 / 0558751163**. History row 63, account **214265**: **0558006043 / 0503087628** | Preserve both in historical source/snapshot data. They must not overwrite a later account login phone. |
| Named riders but placement not completed | Current rows **108 / 117**, accounts **222872 / 272573**, marked `لم يتم التسكين` | No current assignment exists for either account. Recommended: retain them as pending requests/source rows until placement is completed. |
| History entry not accepted | History row **25**, account **214299**, marked `لا يقبل`, proposed dates 1–6 October | No assignment exists. Recommended: store as a rejected attempt, not completed service. The account-status cell says `يعمل`, which must not override the placement result. |
| Duplicate historical assignment | History row **34**, account **215459**, rider **2553845369**, 6 October–6 October | Two DB assignment records match exactly. Each is referenced by one day-usage record. Review both references before choosing a canonical assignment; use a correction/supersession process, not a hard delete. |

The duplicate DB assignment IDs are `01a16df6-33eb-7436-b37b-b90230121a6a` and `01a16df8-6809-71c6-b939-d53d664fe930`.

## Rider-type mapping needs a business definition

The workbook has two independent dimensions:

- `نوع البديل`: `كفالة`, `خارجي`, `خارجي - عبدالله`, `خارجي - شركة السهالة`.
- `نوع المركبة`: company/private car or motorcycle.

An external rider can use a company vehicle: **25 source rows** contain `خارجي` with a company car or motorcycle. Two `كفالة` rows use a private car. Therefore company/private vehicle ownership cannot silently be substituted for internal/external rider engagement.

The analysis proposes `كفالة` → Internal, all `خارجي...` values → External, with car/motorcycle taken from the vehicle label. This mapping remains provisional until its meaning is confirmed.

There are **177 hosted assignments without a rider type**: 99 current and 78 historical. Among uniquely matched workbook rows, 175 would receive a missing type, and 2 existing current types conflict with the proposed mapping: accounts **267573 / 267570**, current rows **142 / 143**, are stored as `InternalCar` while the workbook says `خارجي` + `سيارة خاصة`.

The hosted four-value rider-type column already exists. Backfilling it needs no migration. Additional independent properties below do require model/API/schema work.

## Confirmed data differences

| Field | Difference | Handling |
| --- | --- | --- |
| Assignment payment model | **30** exact matched rows differ: **14 history + 16 current** | Set each period to its source `عمولة` → `PayPerOrder`, `راتب` → `Salary`. Preserve the existing period and record the correction. |
| Account payment model | **16 current accounts** differ | Reconcile with the corrected active assignment in the same transaction. |
| Current login phone | **7 completed placements** differ, plus **2 pending accounts** have no phone | Use the unambiguous current values. The multi-number account is a separate decision above. Pending phone data can be staged without creating assignments. |
| Employee primary phone | **14 current rows** differ | Employee contact phone and account login phone are separate fields. Preserve previous values/history and use identity matches; do not replace the phone from historical rows. |
| Actual nationality | **3 current rows** conflict | See exact rows below; use the workbook as the proposed correction source. |
| Nationality labels | **69 rows** use equivalent labels | Normalize for comparison: e.g. `مصر` and `مصري`. These are not nationality conflicts. |
| Actual work city versus account city | **8 rows / 7 accounts**, including **5 current rows** | The workbook explicitly labels actual work city. Preserve it on the assignment/source record; do not automatically change the account's registered city. |
| Rider names | **27 source rows** differ in spelling, language, abbreviation, or contractor annotations | Identities match. Preserve source display names as aliases/snapshots; avoid replacing complete names with abbreviations such as `م س`. |
| Temporary/nonstandard actual-rider identities | **15 source rows** | All map to existing rider records. Preserve these identifiers; do not manufacture legal iqamas or merge by name. |

Actual nationality conflicts:

| Current row | Account | Workbook | Hosted employee |
| --- | --- | --- | --- |
| 71 | 82581 | السودان | يمني |
| 86 | 295969 | اثيوبيا | برمي |
| 93 | 223613 | باكستان | بنغلاديشي |

Current work-city differences are accounts **159271, 214308, 272575, 286370, 286356**: source Jeddah versus registered Taif/Riyadh. The remaining three differences occur in history.

## Same-day handovers

There are **4 same-account** and **5 same-rider** period-boundary cases on the same date. The existing Ninja workflow permits same-day return/reassignment and treats end dates as inclusive. These are review cases, not automatically invalid dates. Do not subtract a day blindly. Daily attribution must use the account-day allocation records or request an explicit split if operational totals cannot identify the user.

Affected same-account IDs: **215810, 214235, 214330, 159271**. The separate two-day overlap for rider 2589847850 is not one of these boundary cases.

All 292 existing Ninja day-usage records currently have null order/hour/collected/deduction metrics. Their assignment links and sequence information still need preservation. The two duplicate assignment records each have a dependent day-usage row.

## Password handling

- Both sheets contain passwords. Extracted JSON and reports contain presence/length metadata only, not password values.
- **13 hosted accounts** currently have saved credential versions, saved today. No password equality/decryptability comparison has been performed.
- **17 accounts** have different password values between their source rows. Historical passwords must not overwrite the current sheet's password.
- **31 source rows** contain numeric passwords shorter than six digits: 12 history and 19 current. Excel uses General formatting. Preserve the literal available value; padding with zero would invent data. The workbook does not establish whether any leading digits were lost before it was saved.
- Import current credentials using the hosting application's effective encryption key and its normal encrypted version storage. Do not use an unverified local encryption key or store passwords in assignment notes/source JSON.
- For accounts occurring only in history, retain historical source evidence securely; do not assume their old password is a valid current password.

## Properties needed to reproduce the workbook

Existing account/rider identity links, period dates, payment model, four-value rider type, current login phone, employee nationality/contact fields, and encrypted credentials cover part of the source. The assignment table has no dedicated city, phone, vehicle-ownership, supplier, or placement-result fields; its general agreement notes are unpopulated.

Proposed additions should be scoped to Ninja and represent these independent concepts:

| Property | Proposed location and behavior |
| --- | --- |
| Source batch, source sheet/row and file hash | Immutable import/source records with a unique batch/row key; supports preview, traceability and retry without duplicates. No plaintext password payload. |
| Placement result | Source/request record with completed, pending and rejected states. A rejected/pending row can exist without an assignment. Keep provider account status separate. |
| Replacement/engagement label | Assignment/source snapshot retaining the original label; map to the existing rider-type enum only after its definition is confirmed. |
| External provider | Optional separate provider reference/label for عبدالله and شركة السهالة. Preserve annotations such as PrimeX in source names without inferring an unprovided legal supplier relationship. |
| Vehicle class and ownership | Separate car/motorcycle and company/private fields. This does not create a fleet assignment: the source supplies no plate/VIN/vehicle ID. |
| Actual work city | Assignment-period city snapshot independent of registered account city. |
| Rider display name, nationality and primary/secondary contact | Historical snapshots on import/assignment records, alongside links to the existing employee/rider. Retain both numbers where supplied. Current employee-master corrections remain separately audited. |

The existing Ninja sheet-checkup feature stores comparisons and deliberately does not update master accounts. Its current comparison schema is not a complete handover-history import workflow. A dedicated preview/apply workflow, API DTOs and frontend presentation are needed if this is to become a repeatable system feature rather than a one-off data repair.

## Concrete hosted-update sequence

1. Apply the recorded user decisions above to the plan. Keep 286371 historical only with its source dates, start 267570 on 23 October, consolidate the duplicate 215459 history period, and rebuild rider-type comparisons without the ignored replacement-type column.
2. Implement the required source/snapshot properties and preview/apply support, with targeted verification of duplicates, pending rows, independent vehicle ownership, multiple phones, historical snapshots, and credential handling.
3. Generate a row-specific import/correction manifest using account IDs, rider IDs and existing assignment IDs. Include old rowversions/value fingerprints and source hash. Separate data corrections from new placement requests.
4. Refresh the hosted snapshot immediately before deployment. Normal user activity occurred during this review: the employee directory grew by one record between snapshots. Fail and re-preview if relevant rows, assignments or credentials have changed.
5. Take a recoverable backup, rehearse migrations/import/corrections in a rollback transaction or disposable copy, and inspect all affected day-usage/assignment-event references.
6. Apply only the required migration and the reviewed row-specific changes transactionally. Use a correction/supersession audit path for immutable history. Preserve the 120 visible accounts absent from this workbook, the already deleted account, other platforms, and unrelated employees.
7. Verify after commit using a new connection: account/rider/period uniqueness, **151 completed current assignments** under the recorded user decisions, payment models, confirmed rider types, source-row coverage, preserved day allocations and credential decryptability using the effective hosting configuration. Publish matching API/frontend code if required; a DB-only update cannot expose new UI fields.

No executable mutation SQL has been produced because the unresolved facts affect identity and historical correctness. `issues.csv` contains every row-level observation; `analysis.json` contains the comparison and source-row mapping. Neither includes plaintext passwords.
