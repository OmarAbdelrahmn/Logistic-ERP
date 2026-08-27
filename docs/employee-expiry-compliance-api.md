# Employee and Rider Expiry Compliance API

## Scope

These endpoints are read-only HR compliance views. They calculate the current expiry state from existing employee and rider records; they do not update documents, licences, cards, insurance, employee statuses, or rider statuses.

Fleet, vehicles, vehicle assignments, maintenance, platform assignments, contracts, leave, probation, sponsor dates, and other non-expiry HR deadlines are excluded.

## Authorization and dates

- Both endpoints require the existing `employees.read` permission.
- Send dates as `YYYY-MM-DD`.
- When `checkDate` is omitted, the API uses the current Riyadh calendar date.
- A successful request returns `200 OK`.
- Unknown or archived employee IDs on the employee-specific endpoint return the standard `hr.not_found` problem response.
- Invalid `sourceType`, `dueStatus`, or `employeeStatus` query values return the standard `hr.invalid_request` problem response.

## Calculated due statuses

| Status | Rule | `daysRemaining` |
| --- | --- | --- |
| `Valid` | More than 30 days remain | Positive integer greater than 30 |
| `Upcoming` | 1 to 30 days remain | `1` to `30` |
| `DueToday` | Expiry is the check date | `0` |
| `Expired` | Expiry is before the check date | Negative integer |
| `Missing` | An active document type requires an expiry date but has none | `null` |

## Sources and precedence

The dashboard combines the following current records:

- Active employee documents, including residency permits and Ajeer contracts.
- Current driver licences.
- Current rider cards.
- Current rider health cards.
- Current employee medical-insurance policies.

Archived employees and riders, deleted records, and terminal/superseded source records are excluded. If a specialised licence, card, health-card, or insurance record links to an employee document, only the specialised record is returned.

`sourceType` uses the following names in request query strings. In JSON responses, the current API serializes these enum values as numbers.

| Source type | JSON value |
| --- | --- |
| `EmployeeDocument` | `0` |
| `DriverLicense` | `1` |
| `RiderCard` | `2` |
| `HealthCard` | `3` |
| `MedicalInsurance` | `4` |

`dueStatus` similarly uses the following query names and JSON values: `Valid`/`0`, `Upcoming`/`1`, `DueToday`/`2`, `Expired`/`3`, and `Missing`/`4`.

## GET /api/compliance/expiries

Returns a paged compliance dashboard across employees and riders.

### Query parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `checkDate` | date | Date used to calculate compliance; defaults to today in Riyadh. |
| `employeeId` | GUID | Limit results to one employee. |
| `riderProfileId` | GUID | Limit results to one rider profile. |
| `sourceType` | string | One of the source-type names above. |
| `dueStatus` | string | `Valid`, `Upcoming`, `DueToday`, `Expired`, or `Missing`. |
| `employeeStatus` | string | Existing employee lifecycle status. |
| `operatingCityId` | GUID | Limit results by operating city. |
| `sponsorId` | GUID | Limit results by sponsor. |
| `page` | integer | One-based page number. Default: `1`. |
| `pageSize` | integer | Items per page. Default: `50`; maximum: `200`. |

### Example request

```http
GET /api/compliance/expiries?checkDate=2026-08-27&dueStatus=Upcoming&page=1&pageSize=50
Authorization: Bearer <access-token>
```

### Example response

```json
{
  "items": [
    {
      "employeeId": "2e32fec8-5dfe-459e-a6c6-83e020f0a19b",
      "riderProfileId": "ed3da5ac-a751-484e-b8b7-ad9f6a91977c",
      "employeeNameAr": "أحمد محمد",
      "employeeStatus": "Active",
      "sourceType": 1,
      "sourceId": "dfc29139-b66c-4586-b34f-134915402a3d",
      "categoryCode": "DRIVER_LICENSE",
      "categoryNameAr": "رخصة القيادة",
      "categoryNameEn": "Driver licence",
      "referenceMasked": "••••1234",
      "sourceStatus": "Active",
      "expiryDate": "2026-09-03",
      "daysRemaining": 7,
      "dueStatus": 1,
      "employeeDocumentId": "a2a1375e-17f0-4648-91b2-687b3e6fafbd"
    }
  ],
  "summary": {
    "valid": 12,
    "upcoming": 1,
    "dueToday": 0,
    "expired": 2,
    "missing": 1
  },
  "page": 1,
  "pageSize": 50,
  "totalCount": 16,
  "checkDate": "2026-08-27"
}
```

The summary is calculated across all matching rows before paging. `referenceMasked` is always masked where a protected licence, card, or insurance number is available. `employeeDocumentId` is `null` when the source is not linked to an employee document.

## GET /api/employees/{employeeId}/compliance-expiries

Returns the same calculated items and summary for one non-archived employee. This endpoint does not accept global filtering or paging parameters; it returns up to 200 items for that employee.

### Path and query parameters

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `employeeId` | path | GUID | Employee identifier. |
| `checkDate` | query | date | Date used to calculate compliance; defaults to today in Riyadh. |

### Example request

```http
GET /api/employees/2e32fec8-5dfe-459e-a6c6-83e020f0a19b/compliance-expiries?checkDate=2026-08-27
Authorization: Bearer <access-token>
```

### Example response

The response has exactly the same shape as `GET /api/compliance/expiries`. Its paging fields are returned as `page: 1` and `pageSize: 200`.

## In-app reminders

The daily worker evaluates these same calculated items at 01:00 Riyadh time. It creates in-app reminders for active users with `employees.read` at 30, 7, 1, and 0 days remaining, once after expiry, and once for a required expiry date that is missing. Notifications are bilingual, deep-link to the employee record, and are deduplicated per recipient, source, expiry date, and reminder band. A changed expiry date starts a new reminder sequence.
