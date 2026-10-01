# Frontend handoff: vehicle and rider assignment period reports

## Requests

Both endpoints are authenticated `GET` requests. Send the access token as `Authorization: Bearer <token>`. There is no request body.

| Report | Request URL | Result |
| --- | --- | --- |
| By vehicle | `/api/reports/fleet/vehicle-assignments?fromDate=2026-09-01&toDate=2026-09-20` | One row for every vehicle, with its overlapping rider assignments |
| By actual rider | `/api/reports/fleet/rider-assignments?fromDate=2026-09-01&toDate=2026-09-20` | One row per person who actually drove during the period, with their overlapping vehicle assignments |

| Query parameter | Type | Required | Rule |
| --- | --- | --- | --- |
| `fromDate` | `YYYY-MM-DD` | Yes | First calendar date in the report |
| `toDate` | `YYYY-MM-DD` | Yes | Last calendar date in the report; must be on or after `fromDate` |

The dates are **inclusive Riyadh calendar dates (UTC+03:00)**. The backend uses the time interval from `fromDate` at 00:00 Riyadh time up to, but excluding, midnight after `toDate`. A range such as September 1–20 therefore covers all of September 20. Missing/default dates, reversed dates, and `toDate=9999-12-31` are invalid; malformed or missing values may be rejected by ASP.NET model binding before the report service runs.

There is no pagination, search, vehicle filter, rider filter, or maximum date-range limit on these endpoints. Both require all four permission keys: `reports.read`, `fleet.assignments.read`, `fleet.vehicles.read`, and `riders.read`.

## TypeScript response contracts

The API sends camelCase JSON properties. GUIDs, dates, and timestamps are strings in JSON. Day totals are JSON numbers, including fractional values. Neither endpoint wraps a successful response in a `result` or `data` property.

```ts
type Guid = string;
type CalendarDate = string; // YYYY-MM-DD
type IsoTimestamp = string; // ISO 8601 with an explicit offset, such as Z or +03:00

interface VehicleAssignmentsPeriodReport {
  fromDate: CalendarDate;
  toDate: CalendarDate;
  asOfUtc: IsoTimestamp;
  vehicles: VehicleAssignmentsPeriodRow[];
}

interface VehicleAssignmentsPeriodRow {
  vehicleId: Guid;
  assetNumber: string;
  serialNumber: string | null;
  plateNumberAr: string | null;
  totalDaysAssignedInPeriod: number;
  totalAmountToCollectInPeriodSar: number | null;
  assignments: RiderVehiclePeriodAssignment[];
}

interface RiderAssignmentsPeriodReport {
  fromDate: CalendarDate;
  toDate: CalendarDate;
  asOfUtc: IsoTimestamp;
  riders: RiderAssignmentsPeriodRow[];
}

interface RiderAssignmentsPeriodRow {
  riderKey: string;
  riderProfileId: Guid | null;
  riderName: string | null;
  riderIqamaNo: string | null;
  totalDaysWithVehiclesInPeriod: number;
  totalVehicleCostInPeriodSar: number | null;
  assignments: RiderVehiclePeriodAssignment[];
}

interface RiderVehiclePeriodAssignment extends VehicleRiderPeriodAssignment {
  vehicleType: number; // 1=Motorcycle, 2=Car, 3=Van, 4=Truck, 5=Other
  monthlyCostSar: number | null;
  dailyCostSar: number | null;
  costInPeriodSar: number | null;
}

interface VehicleRiderPeriodAssignment {
  assignmentId: Guid;
  vehicleId: Guid;
  assetNumber: string;
  serialNumber: string | null;
  plateNumberAr: string | null;
  assignedRiderProfileId: Guid;
  assignedEmployeeId: Guid | null;
  assignedRiderName: string | null;
  assignedRiderIqamaNo: string | null;
  isRealRider: boolean;
  actualRiderId: Guid | null;
  actualRiderName: string | null;
  actualRiderIqamaNo: string | null;
  relationshipToAssignedRider: string | null;
  startedAtUtc: IsoTimestamp;
  endedAtUtc: IsoTimestamp | null;
  periodStartedAtUtc: IsoTimestamp;
  periodEndedAtUtc: IsoTimestamp;
  daysInPeriod: number;
  totalAssignmentDays: number;
}
```

## Shared assignment fields

| Field | Meaning |
| --- | --- |
| `assignmentId` | Recorded vehicle assignment ID; use this as the assignment row key. |
| `vehicleId`, `assetNumber`, `serialNumber`, `plateNumberAr` | Vehicle identity. Serial and Arabic plate can be `null`. |
| `assignedRiderProfileId`, `assignedEmployeeId`, `assignedRiderName`, `assignedRiderIqamaNo` | The rider profile to which the assignment was registered. Employee, name, and Iqama can be `null` if related data is unavailable. |
| `isRealRider` | `true` means the assigned rider profile is the actual driver. `false` means the actual driver is in a separate `RealRider` record. |
| `actualRiderId`, `actualRiderName`, `actualRiderIqamaNo` | The person who actually drove. When `isRealRider=true`, the ID is the assigned rider profile ID. When false, it is the separate `RealRider` record ID. These fields can be `null` when the actual rider is unknown. |
| `relationshipToAssignedRider` | Relationship stored on the separate `RealRider` record. `null` when `isRealRider=true` or no separate record exists. |
| `startedAtUtc`, `endedAtUtc` | Original recorded assignment boundaries. `endedAtUtc=null` means the assignment is still open. |
| `periodStartedAtUtc`, `periodEndedAtUtc` | The part of the assignment counted inside the requested date range. For an open assignment, the end is capped at the report's `asOfUtc`. |
| `daysInPeriod` | Elapsed 24-hour days between the clipped period timestamps, rounded to four decimal places. Use this for the selected report's duration. |
| `totalAssignmentDays` | Elapsed days from original assignment start to its recorded end; for an open assignment, from its start to `asOfUtc`. This can be larger than `daysInPeriod`. |

The end timestamp is a boundary, not another full day. For example, an assignment from September 1 at 00:00 to September 11 at 00:00 Riyadh time is **10 days**. A 12-hour assignment contributes `0.5` days. Use the API's day values instead of counting dates in the browser. `asOfUtc` is in UTC; stored assignment timestamps can serialize with another explicit offset, including `+03:00`, even though their property names end in `Utc`. Parse their offsets rather than appending `Z`.

## Vehicle response example

`GET /api/reports/fleet/vehicle-assignments?fromDate=2026-09-01&toDate=2026-09-20` → `200 OK`

```json
{
  "fromDate": "2026-09-01",
  "toDate": "2026-09-20",
  "asOfUtc": "2026-09-20T09:00:00+00:00",
  "vehicles": [
    {
      "vehicleId": "11111111-1111-4111-8111-111111111111",
      "assetNumber": "CAR-A",
      "serialNumber": "SN-100",
      "plateNumberAr": "ا ب ج 1234",
      "totalDaysAssignedInPeriod": 15,
      "totalAmountToCollectInPeriodSar": 900,
      "assignments": [
        {
          "vehicleType": 2,
          "monthlyCostSar": 1800,
          "dailyCostSar": 60,
          "costInPeriodSar": 600,
          "assignmentId": "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
          "vehicleId": "11111111-1111-4111-8111-111111111111",
          "assetNumber": "CAR-A",
          "serialNumber": "SN-100",
          "plateNumberAr": "ا ب ج 1234",
          "assignedRiderProfileId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          "assignedEmployeeId": "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          "assignedRiderName": "Rider A",
          "assignedRiderIqamaNo": "1010101010",
          "isRealRider": true,
          "actualRiderId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          "actualRiderName": "Rider A",
          "actualRiderIqamaNo": "1010101010",
          "relationshipToAssignedRider": null,
          "startedAtUtc": "2026-08-31T21:00:00+00:00",
          "endedAtUtc": "2026-09-10T21:00:00+00:00",
          "periodStartedAtUtc": "2026-08-31T21:00:00+00:00",
          "periodEndedAtUtc": "2026-09-10T21:00:00+00:00",
          "daysInPeriod": 10,
          "totalAssignmentDays": 10
        },
        {
          "vehicleType": 2,
          "monthlyCostSar": 1800,
          "dailyCostSar": 60,
          "costInPeriodSar": 300,
          "assignmentId": "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          "vehicleId": "11111111-1111-4111-8111-111111111111",
          "assetNumber": "CAR-A",
          "serialNumber": "SN-100",
          "plateNumberAr": "ا ب ج 1234",
          "assignedRiderProfileId": "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          "assignedEmployeeId": "99999999-9999-4999-8999-999999999999",
          "assignedRiderName": "Assigned Rider B",
          "assignedRiderIqamaNo": "3030303030",
          "isRealRider": false,
          "actualRiderId": "ffffffff-ffff-4fff-8fff-ffffffffffff",
          "actualRiderName": "Actual Rider M",
          "actualRiderIqamaNo": "2020202020",
          "relationshipToAssignedRider": "Substitute",
          "startedAtUtc": "2026-09-10T21:00:00+00:00",
          "endedAtUtc": "2026-09-15T21:00:00+00:00",
          "periodStartedAtUtc": "2026-09-10T21:00:00+00:00",
          "periodEndedAtUtc": "2026-09-15T21:00:00+00:00",
          "daysInPeriod": 5,
          "totalAssignmentDays": 5
        }
      ]
    },
    {
      "vehicleId": "22222222-2222-4222-8222-222222222222",
      "assetNumber": "CAR-B",
      "serialNumber": null,
      "plateNumberAr": null,
      "totalDaysAssignedInPeriod": 0,
      "totalAmountToCollectInPeriodSar": 0,
      "assignments": []
    }
  ]
}
```

The `vehicles` array is sorted by `assetNumber`, then vehicle ID. Assignments within each vehicle are ordered by original start time. `totalDaysAssignedInPeriod` is the sum of that vehicle's `daysInPeriod` values. Vehicles with no overlapping assignments still appear with an empty array. The backend currently includes soft-deleted vehicle records in this list and does not expose an `isDeleted` flag in this response. Soft-deleted assignment records are excluded.

## Rider response example

`GET /api/reports/fleet/rider-assignments?fromDate=2026-09-01&toDate=2026-09-20` → `200 OK`

```json
{
  "fromDate": "2026-09-01",
  "toDate": "2026-09-20",
  "asOfUtc": "2026-09-20T09:00:00+00:00",
  "riders": [
    {
      "riderKey": "iqama:2020202020",
      "riderProfileId": null,
      "riderName": "Actual Rider M",
      "riderIqamaNo": "2020202020",
      "totalDaysWithVehiclesInPeriod": 5,
      "totalVehicleCostInPeriodSar": 300,
      "assignments": [
        {
          "vehicleType": 2,
          "monthlyCostSar": 1800,
          "dailyCostSar": 60,
          "costInPeriodSar": 300,
          "assignmentId": "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          "vehicleId": "11111111-1111-4111-8111-111111111111",
          "assetNumber": "CAR-A",
          "serialNumber": "SN-100",
          "plateNumberAr": "ا ب ج 1234",
          "assignedRiderProfileId": "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          "assignedEmployeeId": "99999999-9999-4999-8999-999999999999",
          "assignedRiderName": "Assigned Rider B",
          "assignedRiderIqamaNo": "3030303030",
          "isRealRider": false,
          "actualRiderId": "ffffffff-ffff-4fff-8fff-ffffffffffff",
          "actualRiderName": "Actual Rider M",
          "actualRiderIqamaNo": "2020202020",
          "relationshipToAssignedRider": "Substitute",
          "startedAtUtc": "2026-09-10T21:00:00+00:00",
          "endedAtUtc": "2026-09-15T21:00:00+00:00",
          "periodStartedAtUtc": "2026-09-10T21:00:00+00:00",
          "periodEndedAtUtc": "2026-09-15T21:00:00+00:00",
          "daysInPeriod": 5,
          "totalAssignmentDays": 5
        }
      ]
    },
    {
      "riderKey": "iqama:1010101010",
      "riderProfileId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      "riderName": "Rider A",
      "riderIqamaNo": "1010101010",
      "totalDaysWithVehiclesInPeriod": 10,
      "totalVehicleCostInPeriodSar": 600,
      "assignments": [
        {
          "vehicleType": 2,
          "monthlyCostSar": 1800,
          "dailyCostSar": 60,
          "costInPeriodSar": 600,
          "assignmentId": "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
          "vehicleId": "11111111-1111-4111-8111-111111111111",
          "assetNumber": "CAR-A",
          "serialNumber": "SN-100",
          "plateNumberAr": "ا ب ج 1234",
          "assignedRiderProfileId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          "assignedEmployeeId": "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          "assignedRiderName": "Rider A",
          "assignedRiderIqamaNo": "1010101010",
          "isRealRider": true,
          "actualRiderId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          "actualRiderName": "Rider A",
          "actualRiderIqamaNo": "1010101010",
          "relationshipToAssignedRider": null,
          "startedAtUtc": "2026-08-31T21:00:00+00:00",
          "endedAtUtc": "2026-09-10T21:00:00+00:00",
          "periodStartedAtUtc": "2026-08-31T21:00:00+00:00",
          "periodEndedAtUtc": "2026-09-10T21:00:00+00:00",
          "daysInPeriod": 10,
          "totalAssignmentDays": 10
        }
      ]
    }
  ]
}
```

The `riders` array includes only actual drivers with overlapping assignments; riders with no vehicle use in the selected period do not appear. If nobody drove in the period, `riders` is `[]`. Rows are sorted by rider name, then `riderKey`. Assignments within a rider row are sorted by their clipped period start. `totalDaysWithVehiclesInPeriod` sums the row's `daysInPeriod` values.

The backend groups by `riderKey`: `iqama:<number>` when the actual driver has an Iqama, `profile:<32-hex-digit-guid>` for an assigned actual rider without an Iqama, and `unidentified:<32-hex-digit-assignment-guid>` when `isRealRider=false` but its separate real-rider record is missing. Separate `RealRider` records with the same Iqama appear in one rider row. Use `riderKey` as a key within this response, not as a permanent person ID or a display label. `riderProfileId` is present if that person also appears as an actual assigned rider in the grouped assignments; otherwise it is `null`. Do not infer the actual driver from `assignedRiderName` when `isRealRider=false`.

## Vehicle amounts to collect and rider costs

Both reports include the same cost fields on each assignment. All amounts are in Saudi riyals (SAR).

| Field | Meaning |
| --- | --- |
| `vehicleType` | Current vehicle type: `1` motorcycle, `2` car, `3` van, `4` truck, `5` other. |
| `monthlyCostSar` | SAR `1800` for a car or `800` for a motorcycle, using a fixed 30-day month. `null` for types with no specified rate. |
| `dailyCostSar` | `monthlyCostSar / 30`, without intermediate rounding. Motorcycle rates are approximately `26.66666666666667`. `null` for types with no specified rate. |
| `costInPeriodSar` | Exact elapsed days between the clipped period timestamps multiplied by the daily rate, rounded to two decimal places with midpoints away from zero. `null` for types with no specified rate. |
| `totalVehicleCostInPeriodSar` | Sum of the displayed assignments' rounded `costInPeriodSar` amounts. `null` if any assignment has no specified rate. |
| `totalAmountToCollectInPeriodSar` | Vehicle-row total to collect from its riders for the selected period: the sum of its assignments' rounded `costInPeriodSar` amounts. `0` when it has no overlapping assignments, even if its vehicle type has no rate; `null` when it has an assignment with no specified rate. |

The vehicle report returns every vehicle with its assignments and `totalAmountToCollectInPeriodSar`. For a car used by rider A for 10 days and rider M for 5 days, their assignment amounts are SAR `600` and SAR `300`, so collect SAR `900` for that vehicle. Unassigned days add nothing. A vehicle without assignments returns `assignments: []` and `totalAmountToCollectInPeriodSar: 0`.

Vehicle totals add the individually rounded charges rather than multiplying the combined days by a daily rate. Two separate one-day motorcycle assignments each cost SAR `26.67`, making a vehicle total of SAR `53.34`. This keeps vehicle totals consistent with the charges in the rider report for the same period and `asOfUtc`. These fields calculate the amount due from recorded use; they do not subtract payments already received.

For 6 days with a car, followed by 10 days without a vehicle, then 14 days with a motorcycle, the report returns SAR `360` for the car, SAR `373.33` for the motorcycle, and `totalVehicleCostInPeriodSar: 733.33`. The 10 days without a vehicle contribute no assignment and no cost.

Use the API's cost fields for display; do not recalculate costs from the rounded `daysInPeriod` or from a rounded daily rate in the browser. Fractional days are prorated, and open assignments are charged only through `asOfUtc` or the report end, whichever comes first. The divisor is always 30: a complete 31-day car assignment costs SAR `1860`, and a complete 30-day motorcycle assignment costs SAR `800`. Totals follow the same actual-driver grouping as vehicle-use days, including substitute drivers with the same Iqama.

For vans, trucks, other types, or an unrecognized type, display a missing-rate label for the `null` amounts rather than zero. Known assignment costs can still be displayed even when the rider total is `null`. Vehicle totals are also `null` when an overlapping assignment has no rate. Overlapping assignments are each priced and added, just as their days are added. Rates and vehicle type come from the current report rules and vehicle record, rather than historical billing snapshots.

## Errors and frontend behavior

For a reversed or otherwise invalid period, the service returns `400` with a `ProblemDetails` body like this:

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "fleet.assignment_period.invalid_period",
  "status": 400,
  "detail": "حدد تاريخ بداية ونهاية صالحين، على أن لا يسبق تاريخ النهاية تاريخ البداية.",
  "instance": "/api/reports/fleet/vehicle-assignments",
  "errorCode": "fleet.assignment_period.invalid_period",
  "correlationId": "<request-trace-id>",
  "field": "toDate"
}
```

Malformed query dates may produce a different ASP.NET validation response. Missing authentication can return `401`; missing permissions can return `403`. Do not assume those authentication responses have the same body as the service validation error.

Use the returned `daysInPeriod` for the selected report totals and show original start/end timestamps separately from the clipped timestamps. Vehicle and rider names and vehicle identity fields are current related-record values, not historical snapshots. Display timestamps in Riyadh time if that is the application convention, but keep the API date strings unchanged in requests. Show an open assignment when `endedAtUtc` is `null`; `periodEndedAtUtc` then shows the last instant counted. For an unidentified actual rider, show a distinct “Unknown actual rider” label and keep the registered rider available as context. Neither endpoint calculates unassigned gaps or a unique count of calendar days; overlapping assignments, if present in data, are added in the totals.
