# Keeta account import — 7 October 2026

Follow-up on 8 October 2026: 35 additional vehicle assignments were created and account payment models were corrected from column M. See [the follow-up record](keeta-account-followup-20261008.md). The counts below describe the initial 7 October import.

Source: `C:\Users\omarf\Downloads\Keeta.xlsx`, worksheet `ورقة1`, 259 data rows. SHA-256: `0E3852755F99FA71D2B36D967C99E5F8027D2B1094D7BEC20440AF7BD18BB2CC`.

The live application database `db67927` had no Keeta accounts or vehicle-platform-account assignments before this import. Every source iqama matched exactly one existing employee. The `KEETA` platform, both operating cities, all three source sponsors, and the existing `VehiclePlatformAccountAssignments` relationship were present. No schema change was needed.

The import created 259 accounts. The Keeta rider ID became `ExternalAccountId` and `KEETA-<rider ID>` became the account code. `RegisteredEmployeeId` was resolved by iqama; the source iqama and sheet row were also recorded in `OwnershipNotes`. The workbook's branch and registry label determined `OperatingCityId` and `SponsorId`. The source name became `UserName`. The original vehicle type, serial, plate, and settlement mode were retained in `OperationalNotes`. The 255 accepted reviews are `Available`; the two rejected and two pending reviews are `Suspended`. The source settlement labels do not correspond directly to the ERP's salary versus pay-per-order account model, so the account model remains `PayPerOrder` and the exact source label is retained.

Vehicle matching required a unique ERP vehicle serial and matching normalized Arabic plate. The import created 221 active assignments for accepted accounts whose matched ERP vehicles were `Available` or `Assigned`. Assignments use the existing vehicle-to-account table and record the source sheet row. The current user `Omar` was recorded as the import and approval actor.

| Reason left unassigned | Accounts |
| --- | ---: |
| Review rejected or pending | 4 |
| Source plate conflicts with ERP vehicle plate | 12 |
| No source vehicle identity | 1 |
| Matched vehicle has unavailable ERP status | 21 |
| Total | 38 |

Of the 21 unavailable-vehicle rows, 17 matched vehicles are `OutOfService`, two are `UnderMovementResponsibility`, one is on `AccidentHold`, and one is on `ProblemHold`. The two pending-review rows also have source vehicle identifiers of `0`; they are counted in the review category only.

The 221 assignments include 51 account/vehicle sponsor mismatches. No Keeta sponsor vehicle lease agreement exists in the live database, so these remain sponsor warnings under the existing assignment policy. Two Riyadh cars each have three Keeta accounts, exceeding the policy limit of two active accounts per platform and city; the existing API permits these assignments and reports capacity warnings.

A transaction dry run rolled back successfully before the committed import. Post-import verification found 259 Keeta accounts, 221 assignments, and zero mismatches for account ID, employee owner, city, sponsor, status, or assigned vehicle against the staged source. Existing Jahez, HungerStation, and SHIFTZ account counts remained 380, 438, and 464 respectively.
