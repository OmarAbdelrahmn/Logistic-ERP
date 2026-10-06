# Fuel cards: frontend handoff

This module supports `شركة بترو اب` (`PetroApp`) and `شركة سيارة اب` (`SayaraApp`). A card identifier may be an internal value such as `BW203`, or a plate string supplied by the fuel company.

The plate is text owned by the fuel-card record. It has no relationship to a real vehicle, and the API never returns or accepts a `vehicleId` for this module.

## Frontend constants

```ts
export type FuelProvider = "PetroApp" | "SayaraApp";
export type FuelCardIdentifierType = "InternalNumber" | "PlateNumber";

export const fuelProviderLabels: Record<FuelProvider, string> = {
  PetroApp: "شركة بترو اب",
  SayaraApp: "شركة سيارة اب",
};
```

All requests require the normal bearer token. JSON uses camel case. Send `DateOnly` values as `YYYY-MM-DD` and render UTC timestamps after converting them to the user's timezone.

Permissions:

- `fuel.read`: cards, assignments, monthly usage, and import history.
- `fuel.manage`: create cards, change card sponsors/cities, assign riders, and stop assignments.
- `fuel.import`: upload fuel-company spreadsheets.

The `omar` account receives all three permissions directly through the Identity migration. `SYSTEM_ADMIN` and `MANAGER` roles also receive all three permissions.

## Sponsor link: frontend implementation

Every fuel card has exactly one sponsor. Add a required sponsor selector to the create-card form and show the sponsor name in the cards grid and detail view. The fuel-card response provides `sponsorId`; resolve the name from the sponsor catalog. A sponsor can be linked to any number of fuel cards.

Load selector options with `GET /api/sponsors` (`sponsors.read`). It returns an array of sponsor records with `id`, `registryNameAr`, `registryNameEn`, and `status`, among other fields. Display `registryNameAr` (or `registryNameEn` where appropriate), and submit the selected `id`. The fuel-card API does not provide an embedded sponsor name. The catalog includes its records without an active-status filter; the UI may highlight the status but must still display the sponsor of an existing card even if its status changes. A user who can read fuel cards but lacks `sponsors.read` can see the stored `sponsorId` but cannot resolve a name through this endpoint.

Use the required sponsor selector on the one-column PetroApp card-number upload. Its selected `sponsorId` is assigned only to new cards. The monthly usage upload accepts existing cards only and needs no sponsor selector. The separate Import module bulk upload accepts a city name, sponsor 70 number, and fuel company on each card row; see [fuel-card-bulk-import-api.md](fuel-card-bulk-import-api.md).

For an existing card, provide a Change sponsor action under `fuel.manage`. Send `PUT /api/fuel-cards/{id}/sponsor` with the chosen `sponsorId` and the card's latest `rowVersion`. The response is the updated `FuelCard`; update the local card and invalidate the cards list and detail query. On `409 fuel.concurrency_conflict`, refresh the card before retrying. On `404 fuel.sponsor_not_found`, refresh the sponsor options. On `404 fuel.card_not_found`, remove or refresh the stale card detail.

`sponsorId` is a GUID, not the sponsor's employer identity number. The API does not currently have a `sponsorId` query filter on `GET /api/fuel-cards`; any sponsor filter in the UI would need to filter only the loaded page or require a separate backend change.

## City link

Each fuel card has an `operatingCityId` foreign key and returns `operatingCityNameAr`/`operatingCityNameEn`. Existing cards are backfilled to Jeddah (`019c18d5-62e1-7000-8000-000000000003`). Manual creation requires a city ID. The one-column card-number import accepts an optional city ID; the Import module bulk sheet takes a city name per row. Missing city data defaults new cards to Jeddah. Monthly usage imports use existing cards and do not accept a city ID. Use `PUT /api/fuel-cards/{id}/city` with `operatingCityId` and the card's latest `rowVersion` to change it. The list accepts an optional `operatingCityId` query filter. See the complete [city endpoint handoff](fuel-card-city-frontend-handoff.md) for request examples, selector options, upload changes, errors, and rollout details.

## Plate direction and normalization

Do not reverse, split, transliterate, or otherwise normalize a plate in the frontend. Send and display the API value exactly as returned. The backend removes hidden RTL/LTR markers, normalizes Arabic/Persian/Latin digits, and uses the same Saudi Arabic-to-Latin plate mapping as GPS matching.

Use isolated bidirectional rendering for every plate/card cell so mixed Arabic letters and Latin digits remain readable:

```tsx
<span dir="auto" className="fuel-plate">{value}</span>
```

```css
.fuel-plate {
  unicode-bidi: plaintext;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}
```

`cardNumber` and `plateNumberText` are display values. `normalizedCardNumber` is diagnostic/search data and should not replace the display value.

Card numbers are stored without whitespace. Manual creation and all three fuel-card spreadsheet imports remove spaces (including tabs and nonbreaking spaces) from the card number before saving or matching it. Existing saved numbers are cleaned by the `RemoveFuelCardNumberWhitespace` data migration. The separate plate text remains as supplied for display.

## Endpoint summary

| Method | Route | Permission | Success |
|---|---|---|---|
| `GET` | `/api/fuel-cards` | `fuel.read` | `200` card page |
| `GET` | `/api/fuel-cards/{id}` | `fuel.read` | `200` card |
| `POST` | `/api/fuel-cards` | `fuel.manage` | `201` card |
| `PUT` | `/api/fuel-cards/{id}/sponsor` | `fuel.manage` | `200` updated card |
| `PUT` | `/api/fuel-cards/{id}/city` | `fuel.manage` | `200` updated card |
| `GET` | `/api/fuel-cards/{id}/assignments` | `fuel.read` | `200` assignment array |
| `POST` | `/api/fuel-cards/{id}/assignments` | `fuel.manage` | `200` assignment |
| `POST` | `/api/fuel-cards/{id}/stop-rider` | `fuel.manage` | `200` closed assignment |
| `GET` | `/api/fuel-cards/monthly-usage` | `fuel.read` | `200` monthly page and totals |
| `GET` | `/api/fuel-cards/period-usage` | `fuel.read` | `200` cards, rider assignment periods, and monthly costs for a date range |
| `GET` | `/api/fuel-cards/unassigned-usage` | `fuel.read` | `200` saved usage without a rider, for investigation |
| `POST` | `/api/fuel-cards/imports` | `fuel.import` | `200` import result |
| `GET` | `/api/fuel-cards/imports` | `fuel.read` | `200` latest import array |

## Shared response types

```ts
export interface FuelCardCurrentRider {
  assignmentId: string;
  riderProfileId: string;
  employeeId: string;
  riderNameAr: string;
  riderNameEn: string | null;
  effectiveFrom: string;
  rowVersion: string;
}

export interface FuelCard {
  id: string;
  sponsorId: string;
  operatingCityId: string;
  operatingCityNameAr: string | null;
  operatingCityNameEn: string | null;
  provider: FuelProvider;
  providerNameAr: string;
  identifierType: FuelCardIdentifierType;
  cardNumber: string;
  normalizedCardNumber: string;
  plateNumberText: string | null;
  currentRider: FuelCardCurrentRider | null;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  rowVersion: string;
}

export interface FuelCardAssignment {
  id: string;
  fuelCardId: string;
  cardNumber: string;
  riderProfileId: string;
  employeeId: string;
  riderNameAr: string;
  riderNameEn: string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  assignmentReason: string;
  endReason: string | null;
  notes: string | null;
  assignedByUserId: string;
  closedByUserId: string | null;
  rowVersion: string;
}
```

## 1. List cards

`GET /api/fuel-cards`

Query parameters:

- `search?: string`: card number, plate text, or Arabic/English rider name.
- `provider?: FuelProvider`
- `riderProfileId?: UUID`
- `operatingCityId?: UUID`: filter cards by their own operating city before pagination.
- `page?: number` defaults to `1`.
- `pageSize?: number` defaults to `50` and is capped at `300`.

Response:

```ts
interface FuelCardPage {
  items: FuelCard[];
  page: number;
  pageSize: number;
  totalCount: number;
}
```

Use this endpoint for the cards grid. `currentRider === null` means the Assign action is available; otherwise show Stop and use `currentRider.rowVersion` in the stop request.

## 2. Get one card

`GET /api/fuel-cards/{id}`

Returns `FuelCard`. Use it to refresh the detail drawer before an assignment action. A missing card returns `404 fuel.card_not_found`.

## 3. Create a card

`POST /api/fuel-cards`

```json
{
  "provider": "PetroApp",
  "cardNumber": "BW203",
  "plateNumberText": null,
  "notes": "Optional note",
  "sponsorId": "019c18d5-62e1-7000-8000-000000000040",
  "operatingCityId": "019c18d5-62e1-7000-8000-000000000003"
}
```

- `provider` accepts `PetroApp` or `SayaraApp`.
- `cardNumber` is required, 2–100 normalized characters.
- `plateNumberText` is optional, maximum 100 characters.
- `notes` is optional, maximum 4,000 characters.
- `sponsorId` is required and must identify an existing sponsor. Each card belongs to one sponsor; a sponsor may own many cards.
- `operatingCityId` is required and must identify an existing, non-deleted operating city. Load options from `GET /api/hr-catalogs/operating-cities`; submit the option's `id`, not `globalCityId`.
- Values shaped as `BW` plus digits are classified as `InternalNumber`; all others are `PlateNumber`.

Returns `201`, the created `FuelCard`, and a `Location` header. Duplicate normalized card numbers are rejected only within the same provider with `409 fuel.duplicate_card`.

To change the sponsor of an existing card, send `PUT /api/fuel-cards/{id}/sponsor` with `{"sponsorId":"...","rowVersion":"..."}`. Use the card's current `rowVersion`; a stale value returns `409 fuel.concurrency_conflict`. An unknown sponsor returns `404 fuel.sponsor_not_found`.

## 4. Assignment history

`GET /api/fuel-cards/{id}/assignments`

Returns `FuelCardAssignment[]`, newest `effectiveFrom` first. Use the latest active row (`effectiveTo === null`) as the concurrency source when stopping a rider.

## 5. Assign a rider

`POST /api/fuel-cards/{id}/assignments`

```json
{
  "riderProfileId": "00000000-0000-0000-0000-000000000000",
  "effectiveFrom": "2026-09-01",
  "reason": "Monthly fuel-card assignment",
  "notes": null
}
```

Returns the new `FuelCardAssignment` with `200`.

UI rules:

- Select a rider profile, not an employee ID.
- `effectiveFrom` cannot be after today in Riyadh.
- A card cannot have two active assignments.
- A card cannot belong to two different riders in the same calendar month, even if the first assignment was stopped mid-month.
- The rider must be an active rider and not an office employee.
- After success, invalidate the card list, card detail, assignment history, and affected monthly queries.

Important conflicts are `fuel.active_assignment_conflict`, `fuel.monthly_rider_conflict`, and `fuel.rider_unavailable`.

## 6. Stop the current rider

`POST /api/fuel-cards/{id}/stop-rider`

```json
{
  "effectiveTo": "2026-09-15",
  "reason": "Card returned",
  "rowVersion": "AAAAAAAAB9E="
}
```

Get `rowVersion` from `FuelCard.currentRider.rowVersion` or the active assignment row. Returns the closed `FuelCardAssignment` with `200`.

- `effectiveTo` must be on/after `effectiveFrom` and cannot be after today in Riyadh.
- If another user changed the assignment, refresh on `409 fuel.concurrency_conflict`.
- If there is no active assignment, the API returns `404 fuel.assignment_not_found`.
- Stopping a rider does not permit a different rider on the same card during that same month.

## 7. Monthly usage

`GET /api/fuel-cards/monthly-usage?month=2026-09-01`

Query parameters:

- `month: YYYY-MM-DD` is required; any day is normalized to the first day of that month.
- `search?: string`: card, plate, or rider name.
- `provider?: FuelProvider`
- `riderProfileId?: UUID`
- `page?: number` defaults to `1`.
- `pageSize?: number` defaults to `100` and is capped at `300`.

```ts
interface FuelMonthlyUsage {
  id: string;
  fuelCardId: string;
  provider: FuelProvider;
  providerNameAr: string;
  cardNumber: string;
  plateNumberText: string | null;
  reportMonth: string;
  riderProfileId: string | null;
  employeeId: string | null;
  riderNameAr: string | null;
  riderNameEn: string | null;
  needsReview: boolean;
  totalLiters: number;
  totalAmount: number;
  amountBeforeTax: number | null;
  vatAmount: number | null;
  transactionCount: number | null;
  fuelType: string | null;
  firstTransactionAtUtc: string | null;
  lastTransactionAtUtc: string | null;
  reportThroughAtUtc: string | null;
  lastImportId: string;
  updatedAtUtc: string | null;
  rowVersion: string;
}

interface FuelMonthlyUsagePage {
  items: FuelMonthlyUsage[];
  month: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalLiters: number;
  totalAmount: number;
  unassignedCount: number;
  unassignedTotalLiters: number;
  unassignedTotalAmount: number;
}
```

`totalLiters` and `totalAmount` cover the entire filtered query, including unassigned usage, not only the current page. The unassigned fields identify the portion needing investigation. When `riderProfileId` is null, display the card and plate with a review badge rather than a rider name. A `riderProfileId` filter excludes unassigned usage.

### Unassigned usage review queue

`GET /api/fuel-cards/unassigned-usage?from=2026-09-01&to=2026-10-31&page=1&pageSize=50`

Requires `fuel.read`. `from` and `to` are required dates; all monthly usage records in the touched calendar months are included (maximum 36 months). Optional `provider` (`PetroApp` or `SayaraApp`) and `search` (card or plate) filters apply before pagination. Page size defaults to 50 and is capped at 100. The response includes archived cards and total count, liters, and amount across all matching rows.

```ts
interface FuelUnassignedUsagePage {
  items: FuelUnassignedUsage[];
  from: string;
  to: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalLiters: number;
  totalAmount: number;
}

interface FuelUnassignedUsage {
  usageId: string;
  fuelCardId: string;
  provider: FuelProvider;
  cardNumber: string;
  plateNumberText: string | null;
  cardNotes: string | null;
  reportMonth: string;
  totalLiters: number;
  totalAmount: number;
  transactionCount: number | null;
  firstTransactionAtUtc: string | null;
  lastTransactionAtUtc: string | null;
  lastImportId: string;
  originalFileName: string;
  importedAtUtc: string;
  reviewReason: "card_not_assigned";
}
```

The queue shows saved usage with no rider attribution. Use the card number, plate, month, transaction dates, source file, and card notes to investigate who used it. Linking the cost to a rider requires correcting the assignment for that month and re-importing the report.

## 8. Import a spreadsheet

`POST /api/fuel-cards/imports`

Send `multipart/form-data`:

- `File`: required `.xls` or `.xlsx`, maximum 25 MiB.
- `ExpectedMonth`: optional `YYYY-MM-DD`; use it to prevent uploading a file for the wrong month.

Do not manually set the multipart `Content-Type` boundary in browser code:

```ts
const data = new FormData();
data.append("File", file);
data.append("ExpectedMonth", selectedMonth); // e.g. 2026-09-01
await api.post("/api/fuel-cards/imports", data);
```

```ts
interface FuelImportRowError {
  rowNumber: number;
  cardNumber: string | null;
  code: string;
  message: string;
}

interface FuelImportResult {
  importId: string;
  provider: FuelProvider;
  providerNameAr: string;
  reportMonth: string;
  reportThroughAtUtc: string | null;
  originalFileName: string;
  sha256Checksum: string;
  sourceRows: number;
  cardRows: number;
  createdCards: number;
  createdMonthlyRecords: number;
  updatedMonthlyRecords: number;
  unassignedCards: number;
  invalidRows: number;
  errors: FuelImportRowError[];
  importedAtUtc: string;
}
```

Import behavior:

- The detailed PetroApp sheet is transaction/day based. The backend sums all rows by card for the report month and calculates the transaction count and first/last transaction dates.
- The SayaraApp vehicle-consumption sheet already contains one total per vehicle/card for the period; its values are used directly.
- The format and provider are detected from the sheet headers.
- Re-uploading the same or a later file for a card/month updates that one monthly record; it does not append another monthly record.
- Unknown card numbers are skipped with `card_not_found` row errors and increment `invalidRows`. Register those cards through manual creation, the card-number import, or the Import module, then re-upload the usage report. `createdCards` is always `0` for new imports; it remains in the response for compatibility with historical import records.
- An unassigned card's monthly usage and cost are saved with null rider/employee IDs. The import increments `unassignedCards` and returns `card_not_assigned` as a review notice; it does not increment `invalidRows`. Show these rows in the review queue. Assigning the card later does not silently attribute historical cost; re-upload the report after correcting the assignment to link the usage to the rider.
- Treat a `200` response with non-empty `errors` as a completed import with row-level attention required, not as a failed HTTP request.
- `400 fuel.month_mismatch` means the detected month differs from `ExpectedMonth`.

## 9. Import history

`GET /api/fuel-cards/imports`

Optional query parameters are `month=YYYY-MM-DD` and `provider=PetroApp|SayaraApp`. The endpoint returns up to the latest 100 imports, newest first.

Each item has the same summary fields as `FuelImportResult`, without `errors`, and additionally has `id` and `importedByUserId`. Use it for an audit/history tab; row errors are available in the immediate upload response only.

## Error handling

Service errors use RFC 7807 Problem Details:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "fuel.monthly_rider_conflict",
  "status": 409,
  "detail": "لا يمكن إسناد بطاقة الوقود إلى رايدرين مختلفين في الشهر نفسه.",
  "instance": "/api/fuel-cards/.../assignments",
  "errorCode": "fuel.monthly_rider_conflict",
  "correlationId": "...",
  "field": "optionalFieldName"
}
```

Use `errorCode` for UI branching and `detail` for the Arabic message. Preserve `correlationId` in support logs. Authentication/authorization and ASP.NET model-binding failures may use standard Problem Details without a fuel-specific code.

## Recommended frontend flow

Provide five views: Cards, Monthly Usage, Period Usage, Upload, and Import History. The Cards view owns assignment actions; Upload should show counters plus a row-error table and a direct link back to Cards filtered by the failed card number. Always invalidate cached list/detail/month/period/import queries after a successful mutation or import. See the [Period Usage frontend handoff](fuel-card-period-usage-frontend-handoff.md) for its request and response contract.
