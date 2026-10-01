# Housing stay report API — frontend handoff

## Request

`GET /api/housing/stay-report`

Requires a bearer token with `operations.housing.read`. All parameters are optional query parameters:

| Parameter | Type | Default | Meaning |
| --- | --- | --- | --- |
| `fromDate` | `YYYY-MM-DD` | none | Include stays overlapping this day or later. |
| `toDate` | `YYYY-MM-DD` | none | Include stays overlapping this day or earlier. |
| `housingId` | GUID | none | Limit to one housing; omit for all housing. |
| `page` | integer, at least 1 | `1` | Page number. |
| `pageSize` | integer, 1–5000 | `100` | Rows per page. |

Examples:

```http
GET /api/housing/stay-report?fromDate=2026-09-01&toDate=2026-09-30&page=1&pageSize=100
GET /api/housing/stay-report
GET /api/housing/stay-report?housingId=7e76e86b-bbc3-471b-b55d-d5ec3a377528&toDate=2026-09-30
```

Omitting both dates returns all stored linked residence periods, including closed periods, plus the current name-only external and pending occupants. A single date bound is supported. A range is inclusive; `fromDate > toDate` returns `400` with `errorCode: "housing.invalid_toDate"`. Invalid page values return `400` with `errorCode: "hr.invalid_request"`. Invalid date/GUID syntax is rejected by ASP.NET request binding.

## Response

`200 OK` with JSON camel-case properties:

```json
{
  "fromDate": "2026-09-01",
  "toDate": "2026-09-30",
  "asOfDate": "2026-09-29",
  "page": 1,
  "pageSize": 100,
  "totalCount": 3,
  "items": [
    {
      "recordId": "<residence-period-guid>",
      "recordType": "Rider",
      "housingId": "<housing-guid>",
      "housingCode": "RYD-NASEEM-2",
      "housingNameAr": "النسيم 2",
      "housingNameEn": "Al Naseem 2",
      "roomId": "<room-guid>",
      "roomName": "4",
      "floorId": "<floor-guid>",
      "floorName": "1",
      "employeeId": "<employee-guid>",
      "riderProfileId": "<rider-profile-guid>",
      "iqamaNo": "<iqama-number>",
      "nameAr": "<Arabic name>",
      "nameEn": null,
      "moveInDate": "2026-09-05",
      "moveOutDate": null,
      "isCurrentlyInside": true,
      "totalStayDays": 25,
      "daysInSelectedPeriod": 25,
      "moveInReason": null,
      "moveOutReason": null,
      "sourceReference": "Riyadh housing workbook"
    }
  ]
}
```

The example is illustrative; IDs, dates, day totals, and people depend on live data. `asOfDate` is the current date in Riyadh at request time. `totalCount` is the number of matching rows across all pages. Fetch pages until `page * pageSize >= totalCount` to show the full report or build an Excel workbook in the frontend.

## Row types and dates

| `recordType` | Who it represents | `iqamaNo` | Stay dates and day totals |
| --- | --- | --- | --- |
| `Rider` | Linked employee with a rider profile | Employee's current iqama, if present | Available from stored residence period. |
| `Employee` | Linked employee without a rider profile | Employee's current iqama, if present | Available from stored residence period. |
| `External` | Current name-only room occupant without an employee record | `null` | `null`; entry/exit dates are not stored. |
| `PendingMatch` | Current occupant imported with an iqama that has not matched an employee | Imported iqama | `null`; entry/exit dates are not stored. |

`recordId` is the residence period ID for `Rider`/`Employee`, or the external/pending occupant ID for the other types. `employeeId` and `riderProfileId` are `null` for unlinked people. `nameEn` may be `null`. `floorId`/`floorName` may be `null` for older data without a floor. Housing and room IDs allow navigation to their detail views, including archived rooms from old residence periods.

For linked stays, `moveInDate` and `moveOutDate` are the **actual stored period dates**. `moveOutDate: null` means the period is open. `isCurrentlyInside` is true when `asOfDate` falls within the stored period, including a scheduled future move-out date. The end date is inclusive. `totalStayDays` counts from move-in through move-out, or through `asOfDate` while open. `daysInSelectedPeriod` counts only the overlap between that stay and the requested dates, capped at `asOfDate` so future days are never charged. Both counts include their start and end day. A person who changes room has a separate row for each room period.

Current `External` and `PendingMatch` rows are included only if the requested date range contains `asOfDate`; with no date filters they are included. They are excluded from purely historical windows because the database cannot prove when they lived there. For these rows `isCurrentlyInside` is true, while `moveInDate`, `moveOutDate`, `totalStayDays`, and `daysInSelectedPeriod` are `null`. Show an “Entry date unavailable” label, not zero days. A pending row has a `sourceReference` such as `Riyadh workbook row 41`.

The report supplies the employee's **current** name and iqama. It does not reconstruct past name/iqama values. The Riyadh workbook import used the import day as `moveInDate` for linked people; earlier residence days cannot be inferred from that import.

## Frontend behavior

1. Offer optional start/end date inputs, an optional housing selector, and a report button. Send only populated query parameters.
2. Display one row per residence period or current unlinked occupant. Suggested columns: housing, floor, room, person type, name, iqama, move-in, move-out or “Still inside”, days in selected period, and total stay days.
3. Use `daysInSelectedPeriod` for period totals. Treat `null` as unknown, never as zero. If you sum days by person, decide whether stays in different rooms on the same day should count once; this API returns period rows.
4. Fetch all pages before offering a complete Excel download. Keep the returned `asOfDate` with the export so ongoing stay counts have a clear cutoff. Data can change between page requests; reload the report if exact snapshot consistency is required.
5. Display the API's Problem Details response for `400`, `401`, and `403`. The route does not mutate data.
