# Ninja account assignment, monthly attendance validity and vehicles

Account list, detail and status-update responses expose the following additional fields. Existing `status` and `riderType` remain available. No database migration is required.

| Field | Values | Meaning |
| --- | --- | --- |
| `isAssigned` | boolean | A current assignment exists (`currentAssignmentId` is present). |
| `assignmentStatus` / `assignmentStatusAr` | `Assigned` / مسند, `Unassigned` / غير مسند | Independent of operational validity. A suspended account can still be assigned. |
| `isValidForOperation` | boolean or null | Monthly attendance/leave compliance. Null before any completed evaluable day. |
| `operationalValidity` / `operationalValidityAr` | `Valid` / صالح, `Invalid` / غير صالح, `Pending` / لم يبدأ التقييم | Derived from attendance, independently of account lifecycle status and operational blocks. |
| `attendance` | object | Month, evaluation dates, qualified/required days, absence counts, attendance rate, protected-day absences and reason codes. |
| `vehicleOwnership` / `vehicleOwnershipAr` | `Company` / مركبة شركة, `Private` / مركبة خاصة, null | Only classified current assignments. |
| `vehicleType` / `vehicleTypeAr` | `Car` / سيارة, `Motorcycle` / دباب, null | Only classified current assignments. |

The validity rule was corrected on 1 November 2026: it evaluates **account attendance and leave only**. Despite the retained API field names, it does not authorize assignment/reactivation or replace ERP status or operational blocks. Timing, acceptance, uniform/equipment and monetary eligibility are not included. Sheet observations also do not affect it.

Monthly rules:

- Each read evaluates the **current Riyadh calendar month through yesterday**, excluding the incomplete current day and all future days. This is a month-to-date assessment, not a forecast or final full-month award.
- Start on the later of month start and the account's first delivery. Prefer the latest evaluation context's `confirmedFirstDeliveryDate`; otherwise use the earliest saved day with orders greater than zero across account performance sources or legacy facts. Before a first delivery is recorded, return `Pending` with `FirstDeliveryNotRecorded`. Before a completed evaluable day, return `Pending` with `NoCompletedDays` and a null `isValidForOperation`.
- A day qualifies at **8 hours or more**. Require at least `ceil(evaluatedDays * 0.85)` qualifying days. Return `AttendanceBelow85Percent` when below that threshold. `attendanceRate` is a fraction (e.g. `0.85` means 85%), not a rounded comparison.
- A completed day with no saved performance is **absent**, as requested. A saved zero-hour day is also absent. Positive hours below eight count as `shortHoursDays` and do not qualify for the 85% threshold. Zero orders with at least eight hours still qualifies after the first delivery.
- Any absence on **Thursday, Friday or Saturday** makes the account invalid even if its attendance rate exceeds 85%, with reason `LeaveOnProtectedDay`. `protectedDayAbsences` lists the dates. A positive but short shift affects the 85% calculation; it is not classified as an entire day off.
- Only those three weekdays are protected, per the user's clarified scope. No official-occasion calendar is applied.
- Use account source totals so multiple riders on one account/day are not counted as multiple attendance days. For legacy days without a source, sum the assignment shares. Never add sources and shares together for the same account/day.
- Recalculate from the new month at midnight Riyadh; **do not delete historical performance**. On the first day of a month the response is `Pending` until a completed day exists. Reassignment does not reset the account's monthly attendance.

Example subset for a 20-day period with 19 qualifying days but an absence on Friday:

```json
{
  "isValidForOperation": false,
  "operationalValidity": "Invalid",
  "operationalValidityAr": "غير صالح",
  "attendance": {
    "month": "2026-09",
    "firstDeliveryDate": "2026-09-01",
    "evaluatedFrom": "2026-09-01",
    "evaluatedThrough": "2026-09-20",
    "evaluatedDays": 20,
    "qualifiedDays": 19,
    "requiredQualifiedDays": 17,
    "absentDays": 1,
    "shortHoursDays": 0,
    "attendanceRate": 0.95,
    "protectedDayAbsences": ["2026-09-04"],
    "reasons": ["LeaveOnProtectedDay"],
    "status": "Invalid"
  }
}
```

Vehicle mapping uses the recorded current assignment:

| `riderType` | Ownership | Type |
| --- | --- | --- |
| `InternalCar` | Company / مركبة شركة | Car / سيارة |
| `ExternalCar` | Private / مركبة خاصة | Car / سيارة |
| `InternalMotorcycle` | Company / مركبة شركة | Motorcycle / دباب |
| `ExternalMotorcycle` | Private / مركبة خاصة | Motorcycle / دباب |

Unassigned accounts return null for all four vehicle fields. Assigned legacy accounts without `riderType` also return null; show an unknown classification instead of assuming ownership or type. This describes the Ninja assignment classification and does not assert a linked fleet vehicle exists.

Frontend: use the assignment and validity fields as separate badges. Show ownership and vehicle type within assigned accounts. Refresh account reads after assignment/return using the existing flow. Shared types are in [ninja-api-contracts.ts](ninja-api-contracts.ts). Frontend source is maintained outside this backend workspace; publishing the API and connecting these fields in that frontend are required for the live page to display them.

Verification on 1 November 2026: all nine focused account-attendance checks passed, including SQL Server query translation, the exact 85% boundary, eight-hour minimum, missing ordinary/protected days, zero-order attendance, first-delivery/context precedence, legacy share aggregation and Riyadh month rollover without deleting history. The full solution builds with zero warnings and zero errors. Live SQL Server execution was not available; the broader Ninja run skipped its optional SQL Server and supplied-workbook cases.
