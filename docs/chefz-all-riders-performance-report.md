# Chefz all-riders performance report

18 October 2026. New backend endpoint; requires API deployment before frontend use. No database migration is needed.

```http
GET /api/chefz/reports/riders/all?year=2026&month=10&half=2&halves=1
```

Requires `chefz.read` with the existing Chefz platform authorization scope. No account ID or rider ID is required. The response contains **every matching rider in one request**, without pagination. Existing `/api/chefz/reports/riders` requests remain unchanged.

`year`, `month` and `half` select the ending half-month. Half 1 covers days 1–15; half 2 covers days 16–month end. `halves` defaults to 1 and accepts 1–120, including the selected half. Invalid dates and periods preceding year 1 return `Chefz.Invalid` (400). An empty roster is a successful response with `riders: []` and zero counts.

The response contains:

- `from`, `to`, `requestedHalves`: the inclusive selected period.
- `totalRiders`: the number of unique riders returned.
- `ratingCounts`: `a`, `b`, `c`, `d`, `change`, `unrated`, counted from each rider's `selectedSummary.rating` across the whole selected period. These counts add up to `totalRiders`.
- `riders[]`: `riderProfileId`, `riderName`, `selectedSummary`, `halves[]`, and `accounts[]`.
- `accounts[]`: `accountId`, `externalAccountId`, and that account/rider's `summary` for the selected period.

Use [the TypeScript contracts and `chefzPaths.allRidersReport`](chefz-new-endpoints-contracts.ts) through the application's authenticated HTTP client. These types are also re-exported by `chefz-accounts-and-reports-contracts.ts`.

The roster includes riders with Active or Ended Chefz assignments overlapping the period, plus riders with recorded Chefz performance in the period. Cancelled assignments alone do not create roster entries. Other platforms and assignments entirely outside the selected period are excluded. An account used by different riders appears under the appropriate historical riders; two accounts used by one rider produce one rider entry. Unassigned accounts without recorded rider performance are outside a rider report.

Ratings use the current performance settings and exactly the same calculation as the individual report. Combined acceptance is weighted from total accepted offers / total valid offers. Average orders per half divides by halves containing recorded data. Days are counted once per rider, even with two accounts. A recorded zero-order streak can produce `CHANGE`; riders without data have null rating, average and acceptance, and count as `unrated`, rather than D. Each missing half has a message in `halves[]`.

Riders sort by Arabic name and then rider profile ID; accounts sort by external account ID and then account ID. Results provide selected-period and half-month summaries. Fetch the existing individual report when opening daily details or all-time history for a rider. No daily history or all-time data is included in this bulk response.
