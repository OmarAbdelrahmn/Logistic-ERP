# Vehicle and rider assignment period reports

Both endpoints accept `fromDate` and `toDate` in `yyyy-MM-dd` format. The dates are inclusive calendar dates in Riyadh time (UTC+03:00). Both require `reports.read`, `fleet.assignments.read`, `fleet.vehicles.read`, and `riders.read` permissions.

| Method | Route | Grouping |
| --- | --- | --- |
| GET | `/api/reports/fleet/vehicle-assignments?fromDate=2026-09-01&toDate=2026-09-30` | Every vehicle, with its rider assignments in start-time order |
| GET | `/api/reports/fleet/rider-assignments?fromDate=2026-09-01&toDate=2026-09-30` | Each person who actually drove, with vehicles used in start-time order |

An assignment appears when any of its time overlaps the requested period. Each row gives its original `startedAtUtc` and `endedAtUtc`, the clipped `periodStartedAtUtc` and `periodEndedAtUtc`, `daysInPeriod`, and `totalAssignmentDays`. Days are elapsed 24-hour periods rounded to four decimal places; for example, 10 complete days are `10`, and 12 hours are `0.5`. An open assignment is counted through the response's `asOfUtc` time, never beyond it. A completed assignment's `totalAssignmentDays` uses its full recorded start-to-end interval, even if the report only shows part of it.

The vehicle report includes vehicles with an empty `assignments` array and `totalDaysAssignedInPeriod: 0`. Its total is the sum of the displayed assignment days. This is a report of recorded assignments; gaps between assignments are not attributed to a rider.

Each assignment includes both the assigned rider profile and the actual driver. When `isRealRider` is `true`, `actualRiderId`, `actualRiderName`, and `actualRiderIqamaNo` come from the assigned rider profile and its employee. When `isRealRider` is `false`, those fields come from the separate `RealRider` record, while `assignedRiderProfileId` and `assignedRiderName` continue to show the registered profile. The rider report groups the same person across assignments by Iqama number, including separate `RealRider` rows created during vehicle switches. If a non-real assignment has no `RealRider` record, it appears under an `unidentified:` rider key with no actual-driver name, rather than being attributed to the assigned profile.

For example, if CAR-A was with rider A from September 1 at midnight until September 11 at midnight, then with rider M from September 11 until September 16, the vehicle report lists two rows of 10 and 5 days and a vehicle total of 15 days. The rider report shows 10 days for A and 5 days for M.
