# Frontend handoff: maintenance item and vehicle compatibility

This handoff covers the vehicle compatibility change for maintenance inventory items. It supplements [maintenance-frontend-handoff-en.md](maintenance-frontend-handoff-en.md). The frontend should let users specify which vehicle types an item fits, show that information wherever items are selected, and handle compatibility errors from maintenance workflows.

Hosted database `db67927`: migration `20260926085500_AddInventoryItemVehicleCompatibility` was applied and verified on 2026-09-26. Publishing the updated API binary is a separate deployment step.

## Vehicle type values

The API uses numeric `VehicleType` values:

| Value | English label | Arabic label |
| --- | --- | --- |
| `1` | Motorcycle | دراجة نارية |
| `2` | Car | سيارة |
| `3` | Van | فان |
| `4` | Truck | شاحنة |
| `5` | Other | أخرى |

An item can support one, several, or all five types. Use `compatibleVehicleTypes` in API requests and responses; the database mask is an implementation detail and is not an API field.

## Inventory item API changes

| Operation | Endpoint | Change |
| --- | --- | --- |
| List/search | `GET /api/maintenance-inventory/items?search={text}&vehicleType={number}` | Optional `vehicleType` filter. Each returned item includes `compatibleVehicleTypes`. |
| Create | `POST /api/maintenance-inventory/items` | Accepts optional `compatibleVehicleTypes`. |
| Update | `PUT /api/maintenance-inventory/items/{id}` | Accepts optional `compatibleVehicleTypes`. |

The query parameter can be used on its own: `GET /api/maintenance-inventory/items?vehicleType=2`. Omit it to retrieve all items. Filtering includes items that support the selected type, including items that support every type. An unknown numeric value returns `maintenance.invalid_request`.

### Create request example: car and motorcycle spare part

```json
{
  "sku": "BRAKE-PAD-001",
  "barcode": null,
  "itemType": 1,
  "nameAr": "فحمات فرامل",
  "nameEn": "Brake pads",
  "descriptionAr": null,
  "descriptionEn": null,
  "baseUnitOfMeasure": 1,
  "purchaseUnitOfMeasure": 1,
  "defaultPackageQuantity": null,
  "minimumStockLevel": 5,
  "reorderQuantity": 10,
  "isSerialized": false,
  "isLotTracked": false,
  "compatibleVehicleTypes": [1, 2],
  "rowVersion": null
}
```

`itemType: 1` means spare part; `baseUnitOfMeasure: 1` and `purchaseUnitOfMeasure: 1` mean piece. Other existing item fields and rules are unchanged.

### Item response shape

Both write endpoints return an item object, and the list endpoint returns an array of these objects. The response includes these fields:

```json
{
  "id": "<guid>",
  "sku": "BRAKE-PAD-001",
  "barcode": null,
  "itemType": 1,
  "nameAr": "فحمات فرامل",
  "nameEn": "Brake pads",
  "baseUnitOfMeasure": 1,
  "purchaseUnitOfMeasure": 1,
  "defaultPackageQuantity": null,
  "minimumStockLevel": 5,
  "reorderQuantity": 10,
  "status": 1,
  "rowVersion": "<base64-row-version>",
  "compatibleVehicleTypes": [1, 2]
}
```

The item response does not currently include `descriptionAr`, `descriptionEn`, `isSerialized`, or `isLotTracked`. The existing item editor should keep its current handling for those fields.

### Create and update rules

- On create, omitting `compatibleVehicleTypes` or sending `null` makes the item compatible with all five types. The response then contains `[1, 2, 3, 4, 5]`.
- On update, omitting the field or sending `null` preserves the item's existing compatibility. Send a nonempty array to replace the selected types.
- `[]` and unknown enum values are rejected with `maintenance.invalid_request` (HTTP 400). Duplicate values have no additional effect; the response contains each type once.
- Send the latest `rowVersion` on update, as for other item edits. On a concurrency conflict, refetch the item list before retrying.
- Existing items are compatible with all five types after the database migration. Users may narrow them in the item editor.

## Frontend screens and interactions

1. **Item create/edit:** Add a multiselect labeled “Compatible vehicle types” / “أنواع المركبات المتوافقة”. Offer all five values. Default a new item to all five. Load the selected values from `compatibleVehicleTypes` for an existing item. Send the selection when a user changes it.
2. **Item list:** Show the selected vehicle types as labels or chips. Add an optional vehicle-type filter using the `vehicleType` query parameter. Search text and vehicle type can be combined.
3. **Company vehicle work orders and material usage:** Once the vehicle is chosen, pass its `vehicleType` when loading items. This applies to spare parts, oil, filters, and consumables. The backend also checks compatibility when stock is issued.
4. **Supply requests and oil changes:** Filter candidate items for the selected company vehicle. A request containing an incompatible item is rejected before a new work order or supply request is saved; approval and issue also recheck the item.
5. **External vehicle work orders and part sales:** Use the external vehicle's `vehicleType` to filter candidates. If the external vehicle type is unknown, show only items whose `compatibleVehicleTypes` contains all five values. A restricted item cannot be issued to an external vehicle with an unknown type.
6. **Batch spare-part usage:** The existing `POST /api/SparePart/spare-parts?date={date}` route checks each line against the identified vehicle. Its per-line failure message for this case is `Spare part is not compatible with this vehicle type`.

The client-side filter is for selection only. Keep handling server errors because an item's compatibility can change between selection and submission.

```ts
type VehicleType = 1 | 2 | 3 | 4 | 5;

function canUseItem(
  compatibleVehicleTypes: VehicleType[],
  vehicleType: VehicleType | null,
): boolean {
  return vehicleType === null
    ? compatibleVehicleTypes.length === 5
    : compatibleVehicleTypes.includes(vehicleType);
}
```

## New failure to handle

Maintenance material usage, work-order supply requests, oil changes, direct oil changes, and external part sales can now return HTTP 400 with `errorCode: "maintenance.incompatible_vehicle_type"` and `field: "inventoryItemId"`. Show a message such as “This item is not compatible with the selected vehicle type.” Keep the user's form data, then refresh the item choices before another submission.

The API uses Problem Details. Read `errorCode` for logic; `title` contains the same code. For example:

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "maintenance.incompatible_vehicle_type",
  "status": 400,
  "detail": "صنف المخزون غير متوافق مع نوع المركبة.",
  "instance": "/api/maintenance-work-orders/<id>/materials",
  "errorCode": "maintenance.incompatible_vehicle_type",
  "field": "inventoryItemId",
  "correlationId": "<request-id>"
}
```

## Delivery checks

- A car-only item is visible with `vehicleType=2` and absent with `vehicleType=1`.
- An item set to `[1, 2]` is visible for either car or motorcycle; an unrestricted item is visible for all five filters.
- Editing an item can change its selected types, and a subsequent read shows the new values.
- A motorcycle work order cannot issue a car-only part; the form shows the compatibility error and stock remains unchanged.
- The same car-only part can be issued to a car work order when stock is available.
- An external vehicle without a type can use unrestricted items only.

For other environments, apply the `AddInventoryItemVehicleCompatibility` application migration before the updated frontend uses this field. The idempotent script is in `database/scripts/application.sql`.
