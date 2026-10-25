# Jahez rider performance by date range

```http
GET /api/jahez/v2/reports/riders?startDate=2026-10-01&endDate=2026-10-24
Authorization: Bearer <token>
```

For one rider:

```http
GET /api/jahez/v2/reports/riders?startDate=2026-10-01&endDate=2026-10-24&riderId=<rider-profile-UUID>
```

Requires `jahez.read` within the Jahez client-platform scope. This is a read-only endpoint and does not need an idempotency key.

## Query parameters

| Parameter | Required | Meaning |
| --- | --- | --- |
| `startDate` | Yes | First business date, `YYYY-MM-DD`, inclusive |
| `endDate` | Yes | Last business date, inclusive; must be today or earlier in Riyadh |
| `riderId` | No | Actual historical rider's profile UUID |
| `accountId` | No | Internal Jahez platform-account UUID, including a historical archived/deleted account |
| `page` | No | Rider page, default 1 |
| `pageSize` | No | Riders per page, default 50, maximum 100 |

The date range must contain 1–366 days. Supplying both ID filters intersects them. Unknown riders, unknown accounts, and accounts from other platforms return 404. Reversed, missing, malformed, future, or overlong dates and invalid pagination return 400. A valid rider with no Jahez assignment or imported performance in the requested period returns an empty `items` array. Out-of-range pages also return empty items while retaining `totalCount`.

## Response

```json
{
  "startDate": "2026-10-02",
  "endDate": "2026-10-05",
  "riderId": null,
  "accountId": null,
  "page": 1,
  "pageSize": 50,
  "totalCount": 1,
  "items": [
    {
      "riderProfileId": "<rider-profile-UUID>",
      "riderName": "اسم المندوب",
      "riderIqamaNo": "1234567890",
      "selectedSummary": {
        "periodDays": 4,
        "daysWithData": 3,
        "daysWithOrders": 2,
        "zeroOrderDays": 1,
        "missingDays": 1,
        "orders": 13,
        "averageOrdersPerDayWithData": 4.33,
        "maxOrdersPerDay": 8
      },
      "daily": [
        { "date": "2026-10-02", "orders": 5 },
        { "date": "2026-10-03", "orders": 0 },
        { "date": "2026-10-04", "orders": null },
        { "date": "2026-10-05", "orders": 8 }
      ],
      "accounts": [
        {
          "accountId": "<platform-account-UUID>",
          "externalAccountId": "472844",
          "summary": {
            "periodDays": 4,
            "daysWithData": 3,
            "daysWithOrders": 2,
            "zeroOrderDays": 1,
            "missingDays": 1,
            "orders": 13,
            "averageOrdersPerDayWithData": 4.33,
            "maxOrdersPerDay": 8
          },
          "daily": [
            { "date": "2026-10-02", "orders": 5 },
            { "date": "2026-10-03", "orders": 0 },
            { "date": "2026-10-04", "orders": null },
            { "date": "2026-10-05", "orders": 8 }
          ]
        }
      ]
    }
  ]
}
```

## Calculation and history rules

Use `selectedSummary` for the rider's period performance and `accounts` for each account's breakdown. Counts come from committed Jahez daily-order imports and their saved handover allocations. Draft uploads and committed reports replaced by a later committed correction are excluded. An uncommitted correction does not hide the original report.

Orders follow the rider who actually held the account on the reported day, using saved historical sessions and shared-day allocations. They are not assigned to the account's current rider or registered owner. If a rider used several accounts during the period, their totals are combined and account breakdowns are retained. Several account rows on one day still count as one rider day with data. Accounts from other platforms and deleted sessions/import batches/rider identities do not contribute.

Riders assigned during the requested period appear even without an upload. `daily` includes every requested business date in ascending order. A day with no eligible imported row has `orders: null`; a confirmed zero-order day has `orders: 0`. `missingDays` includes all requested dates lacking an eligible imported row, including days outside an assignment. It does not establish absence or non-performance. If only some of a rider's accounts have data for a date, the rider's daily value sums those available rows; inspect account-level daily values for coverage gaps.

`orders` sums known daily counts. `averageOrdersPerDayWithData` divides orders by distinct days with data, including explicit zeros; it rounds to two decimals. `maxOrdersPerDay` is the largest known daily total. Both average and maximum are null when no daily data exists. These metrics use Jahez's recorded order counts; no Chefz acceptance-rate or rating thresholds are applied.

Riders are ordered by name then profile UUID; accounts by external account number then internal UUID. Pagination applies to riders, with complete requested-period summaries for every returned rider. The backend groups daily metrics and pages riders in SQL before loading the current page's details.

The frontend client is `jahezClient(...).riderPerformance({ startDate, endDate, riderId, accountId, page, pageSize })`; contracts are in [jahez-v2-api-contracts.ts](jahez-v2-api-contracts.ts).

## Deployment

No entity, permission catalog, or database schema changes are needed. The existing hosted tables support the report. Deploy the updated API application to expose this route online.

## Verification

The Jahez suite passed **288 tests**, with 31 optional SQL/hosted cases skipped in its default run. Report coverage includes inclusive dates, zero versus missing data, replacement uploads, shared-day rider allocations, multiple accounts, pagination, historical accounts, deleted sources/identities, cross-platform exclusion, authorization, and SQL Server query translation. Strict TypeScript checking passed. A separate opt-in read-only hosted test also passed for all-rider, rider-filtered, and account-filtered report queries, and confirmed that the EF model still matches its migration snapshot. No hosted financial records were changed. Evidence: [hosted read verification](../outputs/jahez-rider-performance-hosted-read-verification.json). HTTP route availability still requires API deployment.
