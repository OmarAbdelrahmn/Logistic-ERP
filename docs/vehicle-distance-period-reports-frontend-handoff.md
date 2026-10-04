# Vehicle GPS/manual distance and missing-records reports — frontend handoff

## Shared contract

Both endpoints require authentication and `fleet.daily_distances.read`. Responses use camelCase JSON and numeric enums. They are read-only and use existing daily-distance records; no database migration is required.

`fromDate` and `toDate` are required query parameters in `yyyy-MM-dd` format. Both dates are included, with a maximum of **366 calendar days**. These are the existing `workDate` calendar dates used by the GPS/manual distance module. Do not convert date strings into UTC instants or shift them when displaying them.

A recorded day has an applied GPS or manual distance. **Zero kilometers is a valid recorded day.** A day is missing when there is no nondeleted daily row, or its `appliedSource` is `None` (neither usable source). A GPS-only gap with usable manual distance is covered. Soft-deleted vehicles and daily rows are excluded.

GPS takes priority when both sources exist. Show GPS and manual values side by side, but use `appliedTotalKm` for the actual period distance. Adding the GPS and manual totals would double-count days with both sources. Manual odometer readings are cumulative meter readings; `manualDistanceKm` is the calculated distance for that day.

## 1. One vehicle: daily GPS and manual details

```http
GET /api/vehicle-daily-distances/reports/vehicles/{vehicleId}?fromDate=2026-09-01&toDate=2026-09-30
Authorization: Bearer <token>
```

| Parameter | Required | Meaning |
| --- | --- | --- |
| `vehicleId` | Yes, route | Vehicle UUID. Any existing nondeleted vehicle can be queried, regardless of its current status. |
| `fromDate` | Yes | First included work date. |
| `toDate` | Yes | Last included work date. |

The response contains one `days` item per requested calendar date, ordered oldest first. It includes gaps, so the frontend can display a continuous calendar or table. There is no pagination for this endpoint.

Example response for `fromDate=2026-09-01&toDate=2026-09-02`, with both sources on September 1 and a missing September 2:

```json
{
  "vehicle": {
    "vehicleId": "019c18d5-62e1-7000-8000-000000000101",
    "assetNumber": "VEH-001",
    "plateNumberAr": "2429 أ ح",
    "plateNumberEn": "2429 AH",
    "vehicleType": 2,
    "currentOperationalStatus": 2,
    "operatingCityId": "019c18d5-62e1-7000-8000-000000000003",
    "operatingCity": "جدة"
  },
  "fromDate": "2026-09-01",
  "toDate": "2026-09-02",
  "totalDays": 2,
  "recordedDays": 1,
  "gpsDays": 1,
  "manualDays": 1,
  "manualFallbackDays": 0,
  "missingDays": 1,
  "gpsTotalKm": 12.5,
  "manualTotalKm": 15,
  "appliedTotalKm": 12.5,
  "days": [
    {
      "id": "019c18d5-62e1-7000-8000-000000000201",
      "workDate": "2026-09-01",
      "hasRecord": true,
      "hasDistance": true,
      "gpsDistanceKm": 12.5,
      "gpsPlateNumber": "2429 AH",
      "manualOdometerReading": 10015,
      "manualBaselineOdometerReading": 10000,
      "manualDistanceKm": 15,
      "appliedDistanceKm": 12.5,
      "appliedSource": 2,
      "effectiveOdometerAfterKm": 10012.5,
      "gpsImportedAtUtc": "2026-09-02T05:00:00+00:00",
      "lastGpsImportId": "019c18d5-62e1-7000-8000-000000000301",
      "gpsImportedByUserId": "019c18d5-62e1-7000-c000-000000000001",
      "manualEnteredAtUtc": "2026-09-01T18:00:00+00:00",
      "manualEnteredByUserId": "019c18d5-62e1-7000-c000-000000000001",
      "manualNotes": "قراءة نهاية اليوم"
    },
    {
      "id": null,
      "workDate": "2026-09-02",
      "hasRecord": false,
      "hasDistance": false,
      "gpsDistanceKm": null,
      "gpsPlateNumber": null,
      "manualOdometerReading": null,
      "manualBaselineOdometerReading": null,
      "manualDistanceKm": null,
      "appliedDistanceKm": 0,
      "appliedSource": 0,
      "effectiveOdometerAfterKm": null,
      "gpsImportedAtUtc": null,
      "lastGpsImportId": null,
      "gpsImportedByUserId": null,
      "manualEnteredAtUtc": null,
      "manualEnteredByUserId": null,
      "manualNotes": null
    }
  ]
}
```

| Field | Meaning |
| --- | --- |
| `vehicle` | Current vehicle identity, type, status and Arabic operating-city name. Nullable plates/city are allowed. |
| `totalDays` | Inclusive period length; equals `days.length`. |
| `recordedDays` | Days with an applied GPS/manual source; includes zero-distance days. |
| `gpsDays` / `manualDays` | Days with a nonnull respective distance. They can overlap. |
| `manualFallbackDays` | Days whose applied source is manual because GPS is unavailable. |
| `missingDays` | Days without an applied source; `recordedDays + missingDays = totalDays`. |
| `gpsTotalKm` / `manualTotalKm` | Sum of each source's daily distance in the requested period, treating absent values as zero. |
| `appliedTotalKm` | Sum of each day's chosen distance, counting each day once. |
| `days[].hasRecord` | A nondeleted daily row exists, even if neither distance source is usable. |
| `days[].hasDistance` | The day is covered by GPS or manual distance. Use this flag to mark gaps. |
| `days[].effectiveOdometerAfterKm` | Stored odometer after the daily record. Null when no row exists; a gap has no inferred historical odometer. |
| `days[].gpsImportedAtUtc` / `manualEnteredAtUtc` | Nullable UTC audit timestamps; convert timestamps for display only. |
| `days[].lastGpsImportId` / `gpsImportedByUserId` / `manualEnteredByUserId` | Nullable audit references. |

Suggested screen: select a vehicle and date range; show GPS total, manual total, applied total and missing-day count. The daily table shows date, GPS km, manual odometer, manual baseline, manual km, applied km, source, effective odometer and notes. Display missing values as `—`; preserve actual `0` values. Use `hasDistance` for a `بدون سجل مسافة` badge.

## 2. Currently working vehicles: missing dates

```http
GET /api/vehicle-daily-distances/reports/missing-records?fromDate=2026-09-01&toDate=2026-09-30&page=1&pageSize=50
Authorization: Bearer <token>
```

**Working vehicles are currently `Assigned` (`currentOperationalStatus = 2`)**, as agreed. The report evaluates every requested day for those current vehicles. It does not change the eligible fleet by historical assignment/status dates, or trim the period to a vehicle's creation/assignment date. A vehicle assigned today can therefore show earlier missing dates within the requested range.

Only working vehicles with at least one missing day appear in `items`. A vehicle with no usable records in the period has every requested date in `missingDates`. Vehicles with complete GPS/manual coverage do not appear.

| Query parameter | Required | Meaning/default |
| --- | --- | --- |
| `fromDate` | Yes | First included work date. |
| `toDate` | Yes | Last included work date. |
| `search` | No | Normalized asset-number or Arabic/English plate search. |
| `operatingCityId` | No | Filter by the vehicle's current operating-city UUID. |
| `vehicleType` | No | Numeric vehicle-type enum. |
| `page` | No | Default `1`; values below `1` become `1`. |
| `pageSize` | No | Default `50`; nonpositive values become `50`; maximum `100`. |

Filters apply to the eligible working fleet before calculating summaries and pagination. Vehicles are ordered by asset number, then UUID. `missingDates` is ordered oldest first. A page beyond the last page returns an empty `items` array with the original summaries.

Example response for September 1–7: two working vehicles match, one has complete coverage, and vehicle X is missing September 4, 5 and 7:

```json
{
  "fromDate": "2026-09-01",
  "toDate": "2026-09-07",
  "totalDays": 7,
  "workingStatus": 2,
  "workingVehicleCount": 2,
  "totalCount": 1,
  "totalMissingDays": 3,
  "page": 1,
  "pageSize": 50,
  "items": [
    {
      "vehicle": {
        "vehicleId": "019c18d5-62e1-7000-8000-000000000101",
        "assetNumber": "VEH-X",
        "plateNumberAr": "2429 أ ح",
        "plateNumberEn": "2429 AH",
        "vehicleType": 2,
        "currentOperationalStatus": 2,
        "operatingCityId": "019c18d5-62e1-7000-8000-000000000003",
        "operatingCity": "جدة"
      },
      "recordedDays": 4,
      "missingDays": 3,
      "missingDates": ["2026-09-04", "2026-09-05", "2026-09-07"]
    }
  ]
}
```

| Field | Meaning |
| --- | --- |
| `workingStatus` | `2` (`Assigned`), defining report eligibility. |
| `workingVehicleCount` | All current assigned vehicles matching filters, including vehicles with complete coverage. |
| `totalCount` | Matching vehicles with at least one missing day, across all pages. Use for pagination. |
| `totalMissingDays` | Total missing vehicle-days across all matching vehicles/pages; two vehicles missing the same date count as two. |
| `items[].recordedDays` | Distinct covered days in the requested period. |
| `items[].missingDays` | Equals `missingDates.length`; recorded + missing equals `totalDays`. |
| `items[].missingDates` | Full dates, not day-of-month numbers. |

Suggested screen: month/date-range selector, city/type/search filters, summary cards for working vehicles, vehicles with gaps, and total missing vehicle-days. Each vehicle row shows the asset, plates, type, city, missing count and missing dates. Link a vehicle row to endpoint 1 using the same date range. Fetch all pages for a complete frontend export.

## Enums and errors

`appliedSource`: `0 = None`, `1 = Manual`, `2 = Gps`.

`vehicleType`: `1 = Motorcycle`, `2 = Car`, `3 = Van`, `4 = Truck`, `5 = Other`.

Errors use the existing API ProblemDetails format:

| HTTP | Error code / condition |
| --- | --- |
| `400` | `fleet.daily_distance.invalid_report_period`: omitted/default dates, end before start, or more than 366 inclusive days. |
| `400` | `fleet.invalid_request`: unsupported numeric `vehicleType`. Malformed UUIDs, dates or enum text also fail model binding. |
| `401` | Missing/invalid authentication. |
| `403` | Missing permission; service failure code `fleet.forbidden`. |
| `404` | `fleet.not_found`: vehicle detail report requested for an absent or deleted vehicle. |

For a valid vehicle with no records, endpoint 1 succeeds with all dates marked missing. With no eligible working vehicles or no missing days, endpoint 2 succeeds with empty `items` and the applicable zero summary values.

## Deployment and verification

Deploy the updated API to expose these routes. No schema/data changes are needed. Behavioral tests cover inclusive dates, gaps on days 4/5/7, zero distances, both sources without double-counting, deleted rows/vehicles, currently assigned eligibility, filters, pagination, authorization and invalid periods. SQL Server integration checks exercise both report queries against the configured database when `LOGISTICS_INTEGRATION_CONNECTION_STRING` is supplied; these checks perform read-only queries.
