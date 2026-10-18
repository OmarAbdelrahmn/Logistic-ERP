# Oil barrel warehouse write-offs and shared use

## Record missing, wasted, or stolen oil

Add **Record missing oil / تسجيل زيت مفقود أو تالف** to an open barrel's actions. This is a warehouse-responsible inventory write-off, independent of the existing 2% normal loss allowance.

```http
POST /api/maintenance-inventory/oil-barrels/{id}/missing
Content-Type: application/json
Authorization: Bearer <token>
```

```json
{
  "occurredAtUtc": "2026-10-10T12:00:00Z",
  "quantityLiters": 25,
  "missingReason": 1,
  "reason": "Physical stock count: all remaining oil is missing",
  "rowVersion": "<latest barrel rowVersion>"
}
```

| Choice | `missingReason` |
| --- | --- |
| Missing / مفقود | 1 |
| Wasted or spilled / تالف أو مسكوب | 2 |
| Theft / مسروق | 3 |

Requires `inventory.stock.adjust`. Accept a positive quantity with at most three decimal places, up to the current remaining liters. Require an occurrence date and a nonblank explanation, maximum 1,000 characters after trimming. Only open barrels can be written off. A full write-off is allowed, including quantities greater than the 2% allowance. Sealed, depleted, returned, and nonexistent barrels cannot be written off.

Show the warehouse and **Warehouse responsible / مسؤولية المستودع** in the dialog. The backend derives `responsibleInventoryLocationId` from the barrel's warehouse; the frontend cannot choose another warehouse or assign this write-off to a driver. This records inventory responsibility and cost; it does not create an employee debt, salary deduction, or general-ledger posting.

Show a confirmation with liters, estimated cost (`quantityLiters × unitCostPerLiter`, rounded to two decimal places), and the remaining quantity after the action. Include **All remaining liters / كامل الكمية المتبقية** as a convenience action. Prevent duplicate submissions while the request is running.

HTTP 200 returns:

```json
{
  "entry": {
    "id": "<write-off ID>",
    "oilBarrelId": "<barrel ID>",
    "responsibleInventoryLocationId": "<barrel warehouse ID>",
    "occurredAtUtc": "2026-10-10T12:00:00Z",
    "quantityLiters": 25,
    "costAmount": 250,
    "missingReason": 1,
    "reason": "Physical stock count: all remaining oil is missing",
    "recordedByUserId": "<current user ID>",
    "recordedAtUtc": "<server UTC timestamp>",
    "stockMovementId": "<inventory movement ID>"
  },
  "barrel": {
    "id": "<barrel ID>",
    "remainingLiters": 0,
    "recordedMissingLiters": 25,
    "recordedLossLiters": 0,
    "status": 3,
    "rowVersion": "<new barrel rowVersion>",
    "allowedVehicleType": 2,
    "allowBothVehicleTypes": false
  }
}
```

The example cost assumes a unit cost of 10; actual cost uses the barrel's stock cost layer. The sample shows selected barrel fields; the API returns the complete existing barrel object plus `allowBothVehicleTypes` and `recordedMissingLiters`. Replace the barrel row and rowVersion from this response and refresh warehouse inventory totals and barrel pickers. Do not subtract quantities again on the frontend.

For a 25-liter barrel written off by 25 liters:

- `remainingLiters` becomes `0`, `remainingInventoryValue` becomes `0`, and `status` becomes `3` (Depleted).
- `depletedAtUtc` is the supplied occurrence time.
- `recordedMissingLiters` increases by `25`. `recordedLossLiters` and `remainingLossAllowanceLiters` stay unchanged.
- The stock cost layer and warehouse stock balance both decrease by `25`. A stock movement and FIFO cost allocation are recorded in the same transaction.
- The next eligible barrel can then be opened through the existing opening endpoint. A shared next barrel also requires that the motorcycle barrel be depleted; a dedicated car next barrel can open while the motorcycle barrel still has stock.

`consumedLiters` excludes both normal losses and warehouse write-offs. Display vehicle consumption, normal loss, and warehouse missing liters separately. Do not label missing liters as vehicle usage.

## Write-off history

```http
GET /api/maintenance-inventory/oil-barrels/{id}/missing?page=1&pageSize=50
```

Requires `inventory.stock.read` and `inventory.cost_layers.read`. Available for depleted barrels too. Response: `oilBarrelId`, `totalMissingLiters`, `totalCostAmount`, `entries`, `page`, `pageSize`, `totalCount`. Each entry has the same fields as the POST `entry`. Entries are ordered by occurrence time descending and ID descending. Page size is capped at 200; totals cover all write-offs, regardless of pagination. This history excludes normal allowance losses.

Show date, quantity, cost, reason category, explanation, responsible warehouse, and recorded user. Resolve names from existing warehouse/user lookup data. Display UTC timestamps in the application's local timezone. The endpoint is append-only; no edit or deletion endpoint is provided.

## Opening for cars, motorcycles, or both

Add a third required choice to **Open barrel / فتح البرميل**:

| Choice | `allowedVehicleType` | `allowBothVehicleTypes` |
| --- | --- | --- |
| Motorcycles only / دراجات نارية فقط | 1 | false |
| Cars only / سيارات فقط | 2 | false |
| Both cars and motorcycles / سيارات ودراجات نارية | null | true |

```http
POST /api/maintenance-inventory/oil-barrels/{id}/open
```

```json
{
  "openedAtUtc": "2026-10-10T12:05:00Z",
  "rowVersion": "<latest barrel rowVersion>",
  "allowedVehicleType": null,
  "allowBothVehicleTypes": true
}
```

Requires `inventory.stock.move`. Existing requests with `allowedVehicleType: 1` or `2` remain valid and default `allowBothVehicleTypes` to false. For Both, send `allowedVehicleType: null`; a numeric type combined with `true` is rejected. Do not send `3` for Both: `VehicleType: 3` means Van and remains invalid for oil barrels. Both requires an oil item compatible with both cars and motorcycles.

A shared barrel supplies either type automatically. Both vehicle-filtered inventory lists and the direct oil-change picker include it. These responses expose `allowBothVehicleTypes`; check this flag before interpreting a null `allowedVehicleType` as unclassified. Null plus false still means unclassified. Usage reports retain each vehicle's actual type.

One dedicated car barrel and one dedicated motorcycle barrel may remain open together for the same oil item and warehouse. A shared barrel occupies both scopes: it cannot open while either dedicated barrel has stock, and neither dedicated type can open while the shared barrel has stock. Database unique indexes enforce this even under concurrent requests. The existing `opened: false`, `hasPreviousBarrelWarning: true`, and `previousOpenBarrelsRemainingLiters` response remains the UI warning contract. For a Both request, the warning quantity covers all overlapping open barrels.

The selection remains locked once opened. The existing `PATCH /api/maintenance-inventory/oil-barrels/{id}/vehicle-type` compatibility endpoint accepts `allowBothVehicleTypes` as well, but only classifies an unclassified open barrel and does not change already assigned barrels. New barrels choose their scope in the opening dialog. Automatic next-barrel opening keeps a preclassified shared barrel shared; an unclassified next barrel continues to be classified for the current vehicle's type.

## Error handling and refresh

Errors use the existing ProblemDetails format:

| Error code | UI behavior |
| --- | --- |
| `maintenance.invalid_request` | Check date, reason, category, quantity precision, and rowVersion format. |
| `maintenance.invalid_oil_barrel` | Refresh the barrel; it may no longer be open or have sufficient liters. |
| `maintenance.concurrency_conflict` | Another operation changed stock. Refresh and ask the user to review; do not automatically resubmit with a new rowVersion. |
| `maintenance.insufficient_stock` | Warehouse balance or cost layer cannot cover the write-off. |
| `maintenance.invalid_oil_barrel_vehicle_type` | Require a valid single type, or null plus the shared flag. |
| `maintenance.incompatible_vehicle_type` | The oil item is not compatible with the selected scope. |
| `maintenance.oil_barrel_vehicle_type_locked` | The already-open barrel's scope cannot be changed. |
| `maintenance.oil_barrel_not_next_fifo` | Choose the next eligible FIFO barrel. |

Reload current barrel data before submitting. POSTs require the latest base64 rowVersion. Reusing the old rowVersion cannot deduct stock twice; it returns a conflict. If the network result is unknown, refresh stock and history before retrying.

## Deployment

The database migration adds shared-use support, separate warehouse write-off counters, responsible warehouse and reason category on the existing loss ledger, and SQL Server indexes preventing overlapping open scopes. Existing quantities and history remain intact, and existing barrels keep their vehicle scope. Deploy the updated backend before enabling these frontend actions. The new API code must be deployed for the endpoints to become available; applying the database migration alone does not publish them.

Hosted database `db67927` was updated on 2026-10-10 with `20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels`. All six barrel quantities and their assigned vehicle types were verified unchanged. The car barrel `OB-20260928-2E748D48` still has 25 liters; no real write-off was submitted. The motorcycle barrel still has 1.999 liters. The schema migration is saved in `database/scripts/warehouse-oil-writeoffs-and-shared-barrels.sql` and appended to `database/scripts/application.sql`. Before/after barrel snapshots are in `outputs/oil-barrel-writeoffs-20261010/`.

Validation: the solution builds, 43 focused oil/maintenance tests and 9 API surface tests pass, and EF reports no pending model changes. The hosted SQL Server migration rehearsal verified constraints, inventory write-off records, cost and warehouse deductions, stale rowversion rejection, next-barrel opening and shared scopes, then rolled back all test changes. The optional isolated LocalDB concurrency test is included but was skipped because LocalDB fails to start on this machine. The hosted SQL rehearsal exercises SQL records and constraints; it does not replace the skipped simultaneous service-call integration test. Backend deployment and frontend implementation are still required.

For rollback, keep the additive schema if reverting the backend. Removing the new columns after write-offs exist would discard responsibility/history and counters; reconcile and retain that data before attempting a database downgrade.
