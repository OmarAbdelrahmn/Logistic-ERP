# Oil barrels: vehicle type and usage

## Choose the type when opening

The **Open barrel / فتح البرميل** dialog must require one choice, with no default selection:

| Label | `allowedVehicleType` |
| --- | --- |
| Motorcycles / دراجات نارية | `1` |
| Cars / سيارات | `2` |

Send the choice in the existing opening request:

```http
POST /api/maintenance-inventory/oil-barrels/{id}/open
Content-Type: application/json
```

```json
{
  "openedAtUtc": "2026-10-01T09:00:00Z",
  "rowVersion": "<latest barrel rowVersion>",
  "allowedVehicleType": 2
}
```

Disable confirmation until a choice is made. Missing, zero, vans, trucks and other types are rejected. Do not ask the user to classify a new barrel in a separate step.

After opening, display a **Cars only / سيارات فقط** or **Motorcycles only / دراجات نارية فقط** badge. The chosen type is locked. A warehouse can have one open barrel per oil item **per vehicle type**, so car and motorcycle barrels can be open together. A second barrel of the same type returns the existing `opened: false` / `hasPreviousBarrelWarning: true` response while the first still has oil. Opening still respects FIFO among sealed barrels eligible for the chosen type.

Opening requires `inventory.stock.move`.

## Oil changes and the next barrel

The backend resolves the actual company vehicle type, or the external vehicle snapshot type, and automatically consumes the matching open barrel. Users cannot use a motorcycle barrel for a car, or a car barrel for a motorcycle. Cost allocations follow the actual barrel consumed.

Filter the oil change barrel picker using:

```http
GET /api/maintenance/oil-barrels?inventoryLocationId={warehouseId}&inventoryItemId={oilItemId}&vehicleType=2
```

This returns matching open barrels and eligible sealed barrels. A sealed barrel with `allowedVehicleType: null` can be selected as `nextOilBarrelId`; if needed to finish the change, it opens for the vehicle's type automatically. If the current barrel has enough oil, the next barrel stays sealed. A wrong-type next barrel is rejected. External vehicle oil usage requires an explicitly known car or motorcycle type.

The inventory list also accepts:

```http
GET /api/maintenance-inventory/oil-barrels?inventoryLocationId={warehouseId}&status=Open&vehicleType=2
```

The optional inventory list type filter returns only barrels already assigned to that type. Omit it to show unclassified barrels as well. Both list responses include `allowedVehicleType`.

## Usage per vehicle

Add **View usage / عرض الاستهلاك** to an open barrel's actions:

```http
GET /api/maintenance-inventory/oil-barrels/{id}/usage?page=1&pageSize=50
```

The endpoint also supports depleted barrels, so their history remains accessible. It requires `inventory.stock.read`, `inventory.cost_layers.read` and `maintenance.oil.read` because the barrel summary includes inventory costs.

Response fields:

| Field | Meaning |
| --- | --- |
| `barrel` | Existing barrel response, including type, remaining liters and row version |
| `totalIssuedLiters` | All liters issued from this barrel |
| `totalReversedLiters` | Liters restored through usage reversals |
| `netUsedLiters` | Issued minus reversed liters |
| `vehicles` | One row per company vehicle, or per external work order |
| `page`, `pageSize`, `totalCount` | Vehicle row pagination; page size capped at 200 |

Each vehicle row contains `vehicleId`, `assetNumber`, current `plateNumberAr`, current `plateNumberEn`, `vehicleType`, `issuedLiters`, `reversedLiters`, `netUsedLiters`, `issueCount` and `lastUsedAtUtc`. External vehicles have `vehicleId: null`, plus `externalWorkOrderId` and `externalPlateOrReference`. Archived company vehicles remain identifiable in historical usage.

Display plate/asset, vehicle type and **Net used liters / الاستهلاك باللتر**. Optional columns are issued, reversed, issue count and last use. Totals cover the entire barrel regardless of pagination. Losses recorded through the loss endpoint remain separate from vehicle consumption. Old usage without a vehicle or external work order is shown in an unidentified row.

## Existing open barrels and deployment

Migration: `20261001073011_RestrictOilBarrelsByVehicleType` adds nullable `AllowedVehicleType`, restricts values to `1` or `2`, and changes the unique open-barrel index to include vehicle type. Existing history and quantities remain intact. Existing barrels are unclassified after migration.

The idempotent SQL is in `database/scripts/restrict-oil-barrels-by-vehicle-type.sql` and appended to `database/scripts/application.sql`.

For a barrel already open before this feature, show a one-time **Assign vehicle type / تحديد نوع المركبة** action, using the same two choices:

```http
PATCH /api/maintenance-inventory/oil-barrels/{id}/vehicle-type
Content-Type: application/json
```

```json
{
  "allowedVehicleType": 1,
  "rowVersion": "<latest barrel rowVersion>"
}
```

This compatibility endpoint requires `inventory.stock.move` and cannot change an already assigned open barrel's type or classify a sealed barrel. New sealed barrels choose their type when opened. Unclassified open barrels cannot supply new oil usage and block manual opening until assigned. Assignment does not rewrite earlier consumption; the usage report can therefore show historical mixed types on an old barrel.

Hosted database `db67927` was updated on 2026-10-01 with migration `20261001073011_RestrictOilBarrelsByVehicleType`. Its existing open barrel `OB-20260928-2E748D48` (ID `01a0e5c1-696a-7c7d-8d59-e9412e748d48`) was assigned to Cars (`allowedVehicleType: 2`). Its remaining quantity stayed at 196.500 liters. Other barrels remain unclassified until opened. The guarded, repeatable assignment is recorded in `database/scripts/assign-open-oil-barrel-to-cars-20261001.sql`.

Backend and frontend deployment have not been performed by this change. Deploy the updated backend to enforce type restrictions and expose the usage endpoint; implement the required choice in the frontend opening dialog.
