# Fuel-card period usage: frontend handoff

## Endpoint

`GET /api/fuel-cards/period-usage`

Requires bearer authentication and `fuel.read`. Send dates as `YYYY-MM-DD` query parameters:

```http
GET /api/fuel-cards/period-usage?from=2026-09-15&to=2026-10-05&provider=PetroApp&page=1&pageSize=50
```

| Parameter | Required | Meaning |
|---|---|---|
| `from` | Yes | First requested assignment date, inclusive. |
| `to` | Yes | Last requested assignment date, inclusive. |
| `provider` | No | `PetroApp` or `SayaraApp`. |
| `search` | No | Card number or plate text. |
| `page` | No | Starts at `1`; lower values become `1`. |
| `pageSize` | No | Defaults to `50`; maximum `100`. |

The range may touch up to 36 calendar months. Invalid or reversed dates return `400 fuel.invalid_report_period`. An unsupported provider returns `400 fuel.invalid_provider`; missing permission returns `403 fuel.forbidden`. Normal authentication failures use the application's standard response.

## Response

The endpoint returns cards with at least one saved monthly fuel-usage record in the touched months. It includes archived cards so historical spending remains visible. A rider who held one of those cards during the requested dates appears even if that rider has zero recorded fuel cost. A rider with recorded cost but no assignment row in the requested dates also appears, with an empty `assignments` array.

```ts
type FuelProvider = "PetroApp" | "SayaraApp";

interface FuelCardPeriodUsagePage {
  items: FuelCardPeriodUsage[];
  from: string;              // requested YYYY-MM-DD
  to: string;                // requested YYYY-MM-DD
  usageMonthFrom: string;    // first included month, YYYY-MM-01
  usageMonthTo: string;      // last included month, YYYY-MM-01
  page: number;
  pageSize: number;
  totalCount: number;        // all matching cards, not just this page
  totalLiters: number;       // all matching cards
  totalAmount: number;       // all matching cards
  unassignedTotalLiters: number; // included in totalLiters
  unassignedTotalAmount: number; // included in totalAmount
}

interface FuelCardPeriodUsage {
  fuelCardId: string;
  provider: FuelProvider;
  providerNameAr: string;
  cardNumber: string;
  plateNumberText: string | null;
  totalLiters: number;
  totalAmount: number;
  unassignedTotalLiters: number;
  unassignedTotalAmount: number;
  unassignedUsageMonths: FuelCardPeriodMonth[];
  riders: FuelCardPeriodRider[];
}

interface FuelCardPeriodRider {
  riderProfileId: string;
  employeeId: string;
  riderNameAr: string | null;
  riderNameEn: string | null;
  totalLiters: number;
  totalAmount: number;
  assignments: FuelCardPeriodAssignment[];
  usageMonths: FuelCardPeriodMonth[];
}

interface FuelCardPeriodAssignment {
  assignmentId: string;
  effectiveFrom: string;      // original assignment start
  effectiveTo: string | null; // original end; null means still active
  from: string;               // start clipped to requested range
  to: string;                 // end clipped to requested range
}

interface FuelCardPeriodMonth {
  reportMonth: string;        // YYYY-MM-01
  totalLiters: number;
  totalAmount: number;
}
```

For example, with `from=2026-09-15&to=2026-10-05`, a rider assigned from September 1 through September 30 has `effectiveFrom: "2026-09-01"`, `effectiveTo: "2026-09-30"`, `from: "2026-09-15"`, and `to: "2026-09-30"`.

## Cost meaning

Fuel spending is saved as **one total per card and calendar month**. A record with no rider is kept on the card with `unassignedUsageMonths`; its cost is included in the card and page totals. The endpoint includes every monthly record from `usageMonthFrom` through `usageMonthTo`, inclusive. `totalAmount` and `totalLiters` are based on the full included months, even when `from` or `to` falls mid-month. The system does not store transaction amounts needed to calculate an exact daily cost or split a month between assignment periods.

The same rider may have multiple assignment periods; show every period from `assignments` and one combined rider cost. `usageMonths` shows which monthly records make up that cost. Card totals equal the riders' costs plus `unassignedTotalAmount`. Page totals cover every filtered card, including those outside the current page.

## Suggested screen

Add a **Fuel card period usage** view with required From and To date controls, optional provider and card search, and pagination. Show `totalAmount`, `totalLiters`, and `totalCount` above the grid. Each card row can expand to show its riders, clipped assignment dates, rider cost, and monthly cost breakdown. Show unassigned cost and months as a separate review section. Use `riderProfileId` to open a rider and `fuelCardId` to open the existing card detail. If `riderNameAr` is null, display the rider ID as a fallback.

Label the cost summary **“Full monthly fuel totals for months touched by this date range”** when either date is inside a month. Reset `page` to `1` when a filter changes. Use the request parameters as the cache key; refresh after fuel imports or assignment changes. Render card/plate values with `dir="auto"` as on the existing fuel screens.
