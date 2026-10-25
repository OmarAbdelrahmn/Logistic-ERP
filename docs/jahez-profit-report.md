# Jahez profit report

`GET /api/jahez/v2/reports/profit?fromDate=2026-10-01&toDate=2026-10-24`

Requires a bearer token and `jahez.financial-reports.read` in the Jahez platform scope. Returns HTTP 200 with the response directly, without a wrapper. No idempotency key is required.

```json
{
  "fromDate": "2026-10-01",
  "toDate": "2026-10-24",
  "accountOpeningFeesTotal": 400.00,
  "settlementCommissionTotal": 750.00,
  "totalProfit": 1150.00
}
```

All amounts are SAR. Suggested Arabic labels:

| Field | Label | Calculation |
|---|---|---|
| `accountOpeningFeesTotal` | إجمالي رسوم فتح الحسابات المقبوضة | Actual account-fee receipt amounts, filtered by `occurredAtUtc`. Includes initial and partial receipts. Unpaid and waived fees do not contribute. |
| `settlementCommissionTotal` | إجمالي عمولة التصفية | Saved `commission` on executed Settle / SettleAndWithdraw operations, filtered by `executedAtUtc`. Uses approved overrides and subsequent authorized corrections. |
| `totalProfit` | إجمالي أرباح جاهز | Opening fees collected + settlement commission. |

Both dates are inclusive in Riyadh time (UTC+3). Each omitted bound defaults independently: `fromDate` to the first day of the current Riyadh month, `toDate` to today in Riyadh. A single-day range is supported. Reversed ranges and minimum/maximum date sentinel values return 400; missing permission returns 403. Empty periods return three zero totals. Totals cover all matching records without pagination.

Commission is recognized when a settlement executes, including settlements that pay the rider, net to zero, collect fuel, or transfer the amount to rider debt. It is not a cash-collection-only measure. Commission periods (`throughDate`), draft creation dates, and backdated operation effective dates do not determine inclusion. Ready / under-review operations, standalone withdrawals, platform net amounts, fuel collections, and debt repayments do not add separate profit. Repaying a transferred settlement does not count commission again.

Confirmed accountant handovers do not remove historical receipts or change profit totals. Account reassignment or closure does not move historical amounts to another period. Authorized corrections to a saved commission update its original execution period.

Frontend integration: use `jahezClient(...).profitReport({ fromDate, toDate })` from `jahez-v2-client.ts` and display the three totals with start/end date controls. This repository provides the backend and frontend contracts; the screen belongs in the consuming frontend. No database migration is required. The API must be deployed before the new route is available remotely.
