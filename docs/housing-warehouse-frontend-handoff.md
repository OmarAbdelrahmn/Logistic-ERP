# Housing Default Warehouse — Frontend Handoff

## Final UX model

Every housing has one default warehouse. The warehouse is completely separate from spare parts, maintenance inventory, suppliers, receipts, and bills.

The item form is intentionally minimal. It contains only:

- `الاسم بالعربية *`
- `الكمية *`
- `الحالة *` — `Unused`, `Used`, or `Damaged`
- `ملاحظات` (optional)

Do not display or send `الوحدة`, `رمز الصنف`, or `الاسم بالإنجليزية`.

There is one visible item row per Arabic item name. Each row contains three balances: unused, used, and damaged. Users never create another item row when its status changes. They select an existing item and transfer a quantity to the new status. The backend atomically decreases the source balance and creates or increments the destination balance.

All IDs are GUID strings. Quantities are decimals with up to three fractional digits and cannot be negative. `rowVersion` is a base64 concurrency token; always send the latest value back for edit, quantity correction, transfer, or delete.

Permissions:

- Read: `operations.housing.read`
- Create, edit, correct quantity, transfer status, or delete: `operations.housing.manage`

## Item response

```json
{
  "id": "019b...",
  "warehouseId": "019a...",
  "housingId": "0199...",
  "nameAr": "سرير مفرد",
  "totalQuantity": 12.0,
  "unusedQuantity": 7.0,
  "usedQuantity": 4.0,
  "damagedQuantity": 1.0,
  "notes": null,
  "rowVersion": "AAAAAAAACCQ="
}
```

`totalQuantity` is always the sum of the three status balances.

## Warehouse response

`GET /api/housing/{housingId}/warehouse`

```json
{
  "id": "019a...",
  "housingId": "0199...",
  "housingCode": "HOU-RUH-01",
  "housingNameAr": "سكن الرياض",
  "housingNameEn": "Riyadh Housing",
  "nameAr": "المستودع الافتراضي",
  "nameEn": "Default warehouse",
  "isDefault": true,
  "itemCount": 12,
  "totalQuantity": 83.5,
  "rowVersion": "AAAAAAAACBU="
}
```

## Item endpoints

### List items

`GET /api/housing/{housingId}/warehouse/items?search={arabicName}&status={Unused|Used|Damaged}`

Both query parameters are optional. A status filter returns items whose balance for that status is greater than zero. The response contains one row per item, never one row per status.

### Get one item

`GET /api/housing/{housingId}/warehouse/items/{itemId}`

### Create item

`POST /api/housing/{housingId}/warehouse/items`

```json
{
  "nameAr": "سرير مفرد",
  "quantity": 10,
  "status": "Unused",
  "notes": null
}
```

Returns `201` with the complete item response. The Arabic item name must be unique inside the housing warehouse.

### Edit item name or notes

`PUT /api/housing/{housingId}/warehouse/items/{itemId}`

```json
{
  "nameAr": "سرير مفرد",
  "notes": "للغرف الجديدة",
  "rowVersion": "AAAAAAAACCQ="
}
```

Quantity and status are not edited through this endpoint.

### Transfer quantity to another status

`POST /api/housing/{housingId}/warehouse/items/{itemId}/status-transfers`

Example: mark three unused beds as used.

```json
{
  "fromStatus": "Unused",
  "toStatus": "Used",
  "quantity": 3,
  "rowVersion": "AAAAAAAACCQ="
}
```

The operation is atomic:

- subtracts `3` from `unusedQuantity`;
- creates the used balance automatically when it does not exist;
- otherwise adds `3` to the existing `usedQuantity`;
- keeps `totalQuantity` unchanged;
- returns the complete updated item with a new `rowVersion`.

The same endpoint supports all valid directions, including `Used` → `Damaged`, `Damaged` → `Used`, and `Used` → `Unused`. Source and destination must differ. The requested quantity must be greater than zero and cannot exceed the source balance.

### Transfer unused quantity to another housing warehouse

`POST /api/housing/{housingId}/warehouse/items/{itemId}/housing-transfers`

Only the source item's `Unused` balance can be transferred between housing warehouses.

```json
{
  "destinationHousingId": "019c...",
  "quantity": 4,
  "rowVersion": "AAAAAAAACCQ="
}
```

Successful `200` response:

```json
{
  "sourceHousingId": "0199...",
  "destinationHousingId": "019c...",
  "quantity": 4,
  "sourceItem": {
    "id": "019b...",
    "warehouseId": "019a...",
    "housingId": "0199...",
    "nameAr": "سرير مفرد",
    "totalQuantity": 8,
    "unusedQuantity": 3,
    "usedQuantity": 4,
    "damagedQuantity": 1,
    "notes": null,
    "rowVersion": "AAAAAAAACDg="
  },
  "destinationItem": {
    "id": "019d...",
    "warehouseId": "019e...",
    "housingId": "019c...",
    "nameAr": "سرير مفرد",
    "totalQuantity": 4,
    "unusedQuantity": 4,
    "usedQuantity": 0,
    "damagedQuantity": 0,
    "notes": null,
    "rowVersion": "AAAAAAAACDk="
  }
}
```

The operation is atomic:

- subtracts the quantity from the source `unusedQuantity`;
- finds the destination housing's default warehouse automatically;
- reuses the destination item when the same Arabic name already exists;
- otherwise creates the destination item automatically;
- creates or increments only the destination `Unused` balance;
- does not copy or move any `Used` or `Damaged` quantity;
- rolls back the entire operation if any validation or concurrency check fails.

The destination must be a different housing and the quantity must be greater than zero and no more than the source `unusedQuantity`.

### Correct one status balance directly

`PATCH /api/housing/{housingId}/warehouse/items/{itemId}/statuses/{status}/quantity`

```json
{
  "quantity": 7.5,
  "rowVersion": "AAAAAAAACCw="
}
```

Use this only for an administrative correction or initial reconciliation. It sets the absolute quantity for the selected status. The backend creates the status balance automatically if it does not exist.

### Delete item

`DELETE /api/housing/{housingId}/warehouse/items/{itemId}`

```json
{
  "reason": "لم يعد الصنف مستخدماً في السكن",
  "rowVersion": "AAAAAAAACDA="
}
```

Returns `204` and soft-deletes the single item master record.

## Recommended Arabic UI

Use one compact row/card per item:

| الصنف | غير مستخدم | مستخدم | تالف | الإجمالي | الإجراءات |
|---|---:|---:|---:|---:|---|
| سرير مفرد | 7 | 4 | 1 | 12 | نقل حالة، نقل إلى سكن، تصحيح كمية، تعديل، حذف |

The primary row action is `نقل حالة`. Open a small dialog with:

1. `من الحالة`
2. `إلى الحالة`
3. `الكمية`

Show the available source balance beside the quantity field and prevent submission above that value, while still handling the server conflict. Keep status labels visible in addition to color. After success, replace the returned item in local state and refetch the warehouse summary.

Add a second row action named `نقل إلى سكن`. Disable it when `unusedQuantity` is zero. Its dialog contains:

1. `السكن الوجهة` — searchable housing selector populated from `GET /api/housing`; exclude the current housing.
2. `الكمية غير المستخدمة` — numeric input with the current `unusedQuantity` shown as the available maximum.

After a successful housing transfer, replace `sourceItem` in the current list and refetch the current warehouse summary. The response also includes `destinationItem`; use it if the frontend already caches the destination warehouse, otherwise no destination refetch is required until the user opens it. Restore focus to the triggering row action after closing the dialog and announce the successful transfer with both the quantity and destination housing name.

## Errors

Problem responses use RFC 7807 plus `errorCode`, `correlationId`, and sometimes `field`.

- `400 housing.nameAr_required`
- `400 housing.invalid_nameAr`
- `400 housing.invalid_status`
- `400 housing.invalid_quantity`
- `404 housing.not_found`
- `404 housing.warehouse_not_found`
- `404 housing.warehouse_item_not_found`
- `409 housing.warehouse_item_name_duplicate`
- `409 housing.warehouse_item_insufficient_quantity`
- `409 hr.concurrency_conflict`
- `401` unauthenticated
- `403` missing permission

On a concurrency conflict, preserve the entered transfer, refetch the item, and ask the user to confirm again against the new balances. Do not silently overwrite newer data.
