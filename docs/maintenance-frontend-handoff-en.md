# Frontend Handoff — Maintenance, Inventory, and Workshops

This is the implementation contract for the deployed maintenance database. It covers Jeddah Warehouse, Riyadh Workshop, FIFO spare parts, bill attachments, oil barrels, company vehicles, riders, external customers, workshop revenue, mechanic cost, and profit reporting.

## API conventions

- Prefix every endpoint with the deployed API URL. Paths below begin with `/api`.
- Send `Authorization: Bearer <access-token>` and `Accept: application/json`.
- All ordinary bodies are `application/json`; purchase receipts use `multipart/form-data`.
- Successful calls return the response object directly — there is no `{ data: ... }` envelope.
- `Guid` values are UUID strings. `DateOnly` is `YYYY-MM-DD`. `DateTimeOffset` is ISO-8601, for example `2026-09-05T10:00:00+03:00`.
- JSON field names are `camelCase`. Enums are numbers, not enum strings.
- `rowVersion` is opaque Base64 concurrency data. Save the latest response value and send it back for updates and actions that require it. Never construct it in the frontend.
- The server is the source of truth for FIFO costs, stock quantities, oil cost, and profit. Refresh data after a successful mutation.

### Error response

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "maintenance.concurrency_conflict",
  "status": 409,
  "detail": "The record was changed by another user; reload it.",
  "instance": "/api/maintenance-locations/…",
  "errorCode": "maintenance.concurrency_conflict",
  "correlationId": "00-…",
  "field": "optionalFieldName"
}
```

Use `400` for validation, `401` for unauthenticated, `403` for missing permission, `404` for not found, and `409` for stale data, invalid workflow state, insufficient stock, or FIFO conflict. For `maintenance.concurrency_conflict`, reload the entity before letting the user retry.

## Permissions

Hide or disable each action unless the signed-in user has its permission.

| Area | Permission |
|---|---|
|Read/manage locations|`maintenance.locations.read`, `maintenance.locations.manage`|
|Read/manage company work orders and plans|`maintenance.work_orders.read`, `maintenance.work_orders.manage`|
|Read/complete oil work|`maintenance.oil.read`, `maintenance.oil.complete`|
|Read/manage external jobs|`maintenance.external_jobs.read`, `maintenance.external_jobs.manage`|
|External part sales|`maintenance.part_sales.manage`|
|Customer labor charge|`maintenance.customer_labor_charges.manage`|
|Mechanic labor payment|`maintenance.mechanic_labor_payments.manage`|
|Profit report|`maintenance.profit_reports.read`|
|Read/manage items|`inventory.items.read`, `inventory.items.manage`|
|Read/move/adjust stock|`inventory.stock.read`, `inventory.stock.move`, `inventory.stock.adjust`|
|Read FIFO layers|`inventory.cost_layers.read`|
|Receipts and suppliers|`inventory.receipts.manage`|
|Supplier returns|`inventory.returns.manage`|

## Numeric enum values

| Field | Values |
|---|---|
|`locationType`|1 Warehouse, 2 Workshop, 3 WarehouseAndWorkshop|
|`serviceSubjectType`|1 CompanyVehicle, 2 ExternalVehicle|
|`maintenanceType`|1 Preventive, 2 Corrective, 3 Inspection, 4 AccidentRepair, 5 OilChange, 6 PartSaleOnly|
|Work-order `status`|1 Open, 2 InProgress, 3 Completed, 4 Closed, 5 Cancelled|
|`itemType`|1 SparePart, 2 RiderAccessory, 3 Oil, 4 Consumable|
|Units|1 Piece, 2 Liter, 3 Barrel, 4 Box, 5 Set|
|`usageType`|1 SparePart, 2 Oil, 3 OilFilter, 4 Consumable, 5 ExternalPartSale|
|`paymentMethod`|1 Cash, 2 Card, 3 BankTransfer, 4 Other|
|Oil barrel `status`|1 Sealed, 2 Open, 3 Depleted, 4 Returned|

## Location selection and rules

Read locations with `GET /api/maintenance-locations`. Do not hard-code IDs: identify locations by their returned `code`.

The seeded operational locations are:

- `JED-WH`, **Jeddah Warehouse**: company vehicles only; inventory is enabled; do not show external repair, customer labor, or part-sale actions.
- `RUH-WS`, **Riyadh Workshop**: company and external vehicles; inventory, paid repair, and part sales are enabled.

Example location response:

```json
{
  "id":"guid", "code":"RUH-WS", "nameAr":"ورشة الرياض", "nameEn":"Riyadh Workshop",
  "operatingCityId":"guid", "operatingCityNameAr":"الرياض", "locationType":3,
  "allowsCompanyVehicles":true, "allowsExternalVehicles":true,
  "allowsSparePartSales":true, "allowsPaidExternalRepairs":true,
  "inventoryEnabled":true, "status":1, "address":null, "notes":null,
  "rowVersion":"base64"
}
```

Use `allowsCompanyVehicles`, `allowsExternalVehicles`, `allowsSparePartSales`, and `allowsPaidExternalRepairs` to drive frontend visibility. Maintenance is location/city based; there is no housing-based maintenance location in this model.

Create via `POST /api/maintenance-locations`, update via `PUT /api/maintenance-locations/{id}`. The request is the response fields above except system fields (`id`, `operatingCityNameAr`, `status`); include `latitude`, `longitude`, and `rowVersion`. Send `rowVersion: null` on create.

## Items and suppliers

### Inventory items

| Operation | Endpoint |
|---|---|
|Search/list|`GET /api/maintenance-inventory/items?search=oil`|
|Create|`POST /api/maintenance-inventory/items`|
|Update|`PUT /api/maintenance-inventory/items/{id}`|

```json
{
  "sku":"OIL-10W40", "barcode":null, "itemType":3,
  "nameAr":"زيت 10W-40", "nameEn":"Oil 10W-40",
  "descriptionAr":null, "descriptionEn":null,
  "baseUnitOfMeasure":2, "purchaseUnitOfMeasure":3,
  "defaultPackageQuantity":208, "minimumStockLevel":20, "reorderQuantity":208,
  "isSerialized":false, "isLotTracked":true, "rowVersion":null
}
```

The item response is this request plus `id` and `status`.

For barrel oil, always use `itemType: 3`, `baseUnitOfMeasure: 2` (liter), and `purchaseUnitOfMeasure: 3` (barrel). Do not keep a fixed item cost: each receipt creates its own cost layer and barrels, with its own capacity and liter cost.

### Suppliers

| Operation | Endpoint |
|---|---|
|List|`GET /api/maintenance-inventory/suppliers`|
|Create|`POST /api/maintenance-inventory/suppliers`|
|Update|`PUT /api/maintenance-inventory/suppliers/{id}`|

```json
{
  "supplierNumber":"SUP-001", "legalNameAr":"مورد الزيوت", "legalNameEn":"Oil Supplier",
  "vatNumber":null, "commercialRegistrationNumber":null, "contactName":"…",
  "phone":"…", "email":null, "address":null, "paymentTermsDays":30,
  "notes":null, "rowVersion":null
}
```

Supplier response fields: `id, supplierNumber, legalNameAr, legalNameEn, vatNumber, commercialRegistrationNumber, phone, status, notes, rowVersion`.

## Purchase receipt and mandatory bill attachment

### Endpoint and exact frontend request

`POST /api/maintenance-inventory/receipts`

It **must** be `multipart/form-data` with exact case-insensitive field names `ReceiptJson` and `BillFile`. The bill is mandatory. Accept only PDF, JPEG, PNG, WebP, GIF, or BMP and enforce a 10 MB client limit. Do not manually set `Content-Type` when using browser `FormData`.

```ts
const receipt = {
  supplierId,
  supplierInvoiceNumber: "INV-88",
  invoiceDate: "2026-09-05",
  receivedAtUtc: "2026-09-05T10:00:00+03:00",
  inventoryLocationId,
  currencyCode: "SAR",
  lines: [{
    inventoryItemId: oilItemId,
    purchaseUnit: 3,
    packageCount: 1,
    declaredQuantityPerPackage: 208,
    grossWeightKg: 210,
    netWeightKg: 208,
    packageUnitPrice: 1000,
    discountAmount: 0,
    taxAmount: 150,
    lotNumber: "LOT-9",
    expiryDate: null
  }]
};
const form = new FormData();
form.append("ReceiptJson", JSON.stringify(receipt));
form.append("BillFile", selectedFile);
const response = await api.post("/api/maintenance-inventory/receipts", form);
```

Every line contains `inventoryItemId, purchaseUnit, packageCount, declaredQuantityPerPackage, grossWeightKg, netWeightKg, packageUnitPrice, discountAmount, taxAmount, lotNumber, expiryDate`.

For oil barrels, gross and net weight are required and positive; net weight cannot exceed gross weight; `packageCount` is an integer. Each package becomes an individual barrel. Multiple oil lines for the same item are allowed when the lot, capacity, or price differs.

### Receipt response

```json
{
  "id":"guid", "receiptNumber":"PR-…", "supplierId":"guid", "supplierNameAr":"…",
  "supplierInvoiceNumber":"INV-88", "invoiceDate":"2026-09-05", "receivedAtUtc":"…",
  "inventoryLocationId":"guid", "inventoryLocationNameAr":"ورشة الرياض",
  "subtotal":1000, "discountAmount":0, "taxAmount":150,
  "inventoryValuationAmount":1000, "totalAmount":1150, "currencyCode":"SAR", "status":1,
  "lines":[{
    "id":"guid", "inventoryItemId":"guid", "sku":"OIL-10W40", "purchaseUnit":3,
    "packageCount":1, "declaredQuantityPerPackage":208, "receivedBaseQuantity":208,
    "baseUnitOfMeasure":2, "grossWeightKg":210, "netWeightKg":208,
    "packageUnitPrice":1000, "lineSubtotal":1000, "discountAmount":0, "taxAmount":150,
    "inventoryValuationAmount":1000, "baseUnitCost":4.807692, "stockCostLayerId":"guid"
  }],
  "attachment":{"id":"guid","originalFileName":"invoice.pdf","contentType":"application/pdf","fileSizeBytes":1234,"sha256Checksum":"…","uploadedAtUtc":"…"},
  "oilBarrels":[{"id":"guid","barrelNumber":"OB-…","nominalCapacityLiters":208,"remainingLiters":208,"unitCostPerLiter":4.807692,"status":1,"rowVersion":"base64"}],
  "rowVersion":"base64"
}
```

Retrieve one receipt with `GET /api/maintenance-inventory/receipts/{id}`. Download its protected file with `GET /api/maintenance-inventory/receipts/{id}/bill-file` using the user's authenticated client and handle it as a Blob. There is intentionally no public bill URL.

## Stock, FIFO, and oil barrels

| Endpoint | Result |
|---|---|
|`GET /api/maintenance-inventory/balances?inventoryLocationId={id}&inventoryItemId={id}`|Rows containing `id, inventoryItemId, sku, itemNameAr, inventoryLocationId, locationNameAr, quantityOnHand, quantityReserved, reportingAverageUnitCost, inventoryValue, lastMovementAtUtc, rowVersion`|
|`GET /api/maintenance-inventory/cost-layers?inventoryLocationId={id}&inventoryItemId={id}&availableOnly=true`|FIFO rows: `id, receivedAtUtc, originalSequence, originalQuantity, remainingQuantity, baseUnitOfMeasure, unitCost, remainingValue, lotNumber, expiryDate, sourceReceiptLineId, sourceCostLayerId, rowVersion`|
|`GET /api/maintenance-inventory/oil-barrels?inventoryLocationId={id}&inventoryItemId={id}&status=open`|Oil barrel rows; status may be `sealed`, `open`, `depleted`, or `returned`|

Full oil barrel response:

```json
{
  "id":"guid", "barrelNumber":"OB-0001", "purchaseReceiptLineId":"guid", "inventoryItemId":"guid",
  "inventoryLocationId":"guid", "stockCostLayerId":"guid", "packageSequence":1,
  "nominalCapacityLiters":208, "consumedLiters":200, "remainingLiters":8,
  "unitCostPerLiter":4.807692, "remainingInventoryValue":38.461536,
  "maximumAllowedLossLiters":4.16, "recordedLossLiters":0, "remainingLossAllowanceLiters":4.16,
  "status":2, "openedAtUtc":"…", "depletedAtUtc":null, "rowVersion":"base64"
}
```

This lets the frontend show five barrels, identify the currently open barrel, its actual remaining liters, and the different price/liter for every barrel.

### Opening a barrel

`POST /api/maintenance-inventory/oil-barrels/{barrelId}/open`

```json
{ "openedAtUtc":"2026-09-05T10:00:00+03:00", "rowVersion":"latest-barrel-rowVersion" }
```

The response is always an object of this shape:

```json
{
  "barrel": { "id":"guid", "remainingLiters":8, "status":2, "rowVersion":"new-base64" },
  "opened": false,
  "hasPreviousBarrelWarning": true,
  "previousOpenBarrelsRemainingLiters":8,
  "warningCode":"oil.previous_barrel_remaining",
  "warningMessageAr":"There is an open barrel with 8 liters remaining. It must be consumed first."
}
```

`opened: false` is an intentional successful response, not an HTTP error. Show an unavoidable warning and keep the newly selected barrel sealed. Only one barrel for an oil item/location can be open. It must also be the oldest available FIFO cost layer.

### Oil loss / depreciation

`POST /api/maintenance-inventory/oil-barrels/{barrelId}/losses`

```json
{ "occurredAtUtc":"…", "quantityLiters":2, "reason":"Documented leakage", "rowVersion":"base64" }
```

Response: `id, oilBarrelId, occurredAtUtc, quantityLiters, costAmount, barrelRecordedLossLiters, barrelRemainingLiters, barrelRemainingLossAllowanceLiters`.

The maximum recorded loss is **2% of the original barrel capacity**. For a 208-liter barrel this is 4.16 liters. If exceeded, the API returns `400 maintenance.oil_loss_allowance_exceeded`. Use `remainingLossAllowanceLiters` from the latest response instead of recalculating it locally.

### Transfer, supplier return, and rider issue

`POST /api/maintenance-inventory/transfers`

```json
{ "sourceLocationId":"guid", "destinationLocationId":"guid", "postedAtUtc":"…", "reason":"Transfer to Riyadh", "lines":[{"inventoryItemId":"guid","quantity":5}] }
```

Response: `id, transferNumber, sourceLocationId, destinationLocationId, postedAtUtc, totalCost, status, rowVersion`.

Oil transfers and returns are physically restricted to whole sealed barrels. Do not offer partial-barrel transfer; the server rejects it with `maintenance.oil_transfer_requires_whole_barrels`.

`POST /api/maintenance-inventory/supplier-returns`

```json
{ "supplierId":"guid", "inventoryLocationId":"guid", "purchaseReceiptId":"guid-or-null", "returnedAtUtc":"…", "reason":"Defect", "lines":[{"inventoryItemId":"guid","stockCostLayerId":"guid","quantity":1,"reason":"Damaged"}] }
```

Response: `id, returnNumber, supplierId, inventoryLocationId, returnedAtUtc, totalCost, status, rowVersion`.

`POST /api/maintenance-inventory/rider-issues`

```json
{ "riderProfileId":"guid", "inventoryLocationId":"guid", "issuedAtUtc":"…", "notes":null, "lines":[{"inventoryItemId":"guid","quantity":1,"expectedReturn":false}] }
```

Response: `id, issueNumber, riderProfileId, relatedAssignmentId, inventoryLocationId, issuedAtUtc, totalCost, status, rowVersion`.

## Work orders

### Create a company or external order

- Company vehicle: `POST /api/maintenance-work-orders`, with `serviceSubjectType: 1`.
- External vehicle: `POST /api/maintenance-work-orders/external`, with `serviceSubjectType: 2`.

```json
{
  "serviceSubjectType":1, "vehicleId":"guid", "vehicleIssueId":null,
  "maintenanceLocationId":"guid", "maintenanceType":2,
  "openedAtUtc":"…", "scheduledAtUtc":null, "odometerAtOpen":12000,
  "estimatedCost":0, "diagnosis":"…", "notes":null, "externalVehicle":null
}
```

For external work, use the minimum customer/vehicle snapshot required for the transaction:

```json
{
  "serviceSubjectType":2, "vehicleId":null, "vehicleIssueId":null,
  "maintenanceLocationId":"riyadh-guid", "maintenanceType":2, "openedAtUtc":"…",
  "scheduledAtUtc":null, "odometerAtOpen":null, "estimatedCost":0,
  "diagnosis":null, "notes":null,
  "externalVehicle":{"plateOrReference":"XYZ 1234","vehicleType":2,"customerName":"Customer name","customerPhone":"05…","notes":null}
}
```

Never send `vehicleId` for external work, and never send an `externalVehicle` snapshot for company work. Otherwise the API returns `maintenance.invalid_subject`.

### Read and workflow actions

- `GET /api/maintenance-work-orders?maintenanceLocationId={id}&vehicleId={id}&status=open`
- `GET /api/maintenance-work-orders/{id}`

Both return: `id, workOrderNumber, serviceSubjectType, vehicleId, vehicleAssetNumber, vehicleIssueId, maintenanceLocationId, maintenanceLocationNameAr, maintenanceType, status, openedAtUtc, scheduledAtUtc, startedAtUtc, completedAtUtc, odometerAtOpen, odometerAtCompletion, riderVehicleAssignmentId, attributedRiderProfileId, estimatedCost, actualMaterialCost, actualLaborCost, actualOtherCost, actualTotalCost, externalVehicle, notes, rowVersion`.

Use the state action endpoint:

`POST /api/maintenance-work-orders/{id}/start|complete|close|cancel`

```json
{ "occurredAtUtc":"…", "workPerformed":"…", "qualityCheckNotes":"…", "notes":null, "rowVersion":"latest-order-rowVersion" }
```

Response is the updated work order. Expected flow is Open → InProgress → Completed → Closed. On `409 maintenance.invalid_state`, reload the order and do not force a local state transition.

### Material usage, reversal, and audit history

`POST /api/maintenance-work-orders/{workOrderId}/materials`

```json
{ "inventoryItemId":"guid", "inventoryLocationId":"guid", "quantity":1, "usageType":1, "usedAtUtc":"…", "notes":"…" }
```

Response fields: `id, maintenanceWorkOrderId, inventoryItemId, sku, itemNameAr, inventoryLocationId, usageType, direction, quantity, unitOfMeasure, totalCost, vehicleId, riderVehicleAssignmentId, riderProfileId, attributionStatus, usedAtUtc, reversalOfUsageId, costAllocations`.

Each `costAllocations` item is `{ stockCostLayerId, quantity, unitCost, cost }`. This is the server-calculated FIFO audit trail. The frontend must not select a cost layer for normal usage. If an old part was purchased at X and a newer one at X+3, the server consumes all available X stock before the X+3 layer.

Reverse a mistaken issue with `POST /api/maintenance-work-orders/materials/{usageId}/reverse`:

```json
{ "reversedAtUtc":"…", "reason":"Entered in error" }
```

It returns another material-usage response with `direction: 2`. Do not show reversal for an already reversed usage.

Audit endpoints:

- `GET /api/maintenance/vehicles/{vehicleId}/material-history`
- `GET /api/maintenance/riders/{riderProfileId}/material-history`

Both return the material-usage response list. Use these for vehicle and rider consumption history, including oils, filters, parts, and accessories.

## Oil reminders and oil-change operation

### Reminders and plans

`GET /api/maintenance/oil-reminders` returns rows like:

```json
[{"vehicleId":"guid","assetNumber":"CAR-1","vehicleType":2,"currentOdometer":15000,"lastCompletedAtUtc":"…","lastOilChangeOdometer":11000,"reminderFromOdometer":15000,"maximumDueOdometer":16000,"distanceSinceLastChange":4000,"status":2}]
```

Use the business thresholds: cars are reminded at 4,000 km and due by 5,000 km; motorcycles are reminded at 800 km and due by 1,000 km. Reminder status: 1 OK, 2 Due, 3 Overdue, 4 NeverDone, 5 OdometerMissing.

Plans use `GET|POST /api/maintenance/plans` and `PUT /api/maintenance/plans/{id}`. Request fields are `code, nameAr, nameEn, vehicleModelId, vehicleType, triggerType, intervalDays, intervalKilometers, reminderAfterKilometers, maximumAfterKilometers, alertDaysBefore, alertKilometersBefore, inventoryItemId, defaultOilQuantityLiters, checklistJson, rowVersion`.

### Complete an oil change

Create an OilChange work order (`maintenanceType: 5`) first, then call:

`POST /api/maintenance-work-orders/{workOrderId}/oil-change`

```json
{
  "performedAtUtc":"…", "odometerAtChange":15000,
  "inventoryLocationId":"guid", "oilInventoryItemId":"guid", "nextOilBarrelId":null,
  "oilFilterChanged":true, "oilFilterInventoryItemId":"guid",
  "configuredOilQuantityLiters":null, "laborCost":50, "otherCost":0,
  "notes":null, "workOrderRowVersion":"base64"
}
```

For cars, null `configuredOilQuantityLiters` uses 3.5 liters without a filter and 4 liters with a filter; with a filter, the filter inventory item is required. Motorcycles use a quantity in the 0.8–1 liter range according to their plan/configuration. Invalid choices return `maintenance.invalid_oil_quantity` or `maintenance.invalid_oil_filter`.

If the open barrel will be exhausted during this operation and more oil is needed, select the oldest eligible sealed FIFO barrel and send it as `nextOilBarrelId`. Example: an open barrel has 8L and the operation requires 4L — leave `nextOilBarrelId` null, because 4L is consumed from the existing barrel. If it requires 10L, supply the next eligible barrel; the server consumes the last 8L first and opens the selected next barrel only then. When no open barrel or required next barrel is available, it returns `409 maintenance.open_oil_barrel_required`.

Oil change response:

```json
{"id":"guid","maintenanceWorkOrderId":"guid","performedAtUtc":"…","odometerAtChange":15000,"vehicleType":2,"oilQuantityLiters":4,"oilCost":19.230768,"oilFilterChanged":true,"oilFilterCost":25,"laborCost":50,"otherCost":0,"totalCost":94.230768,"vehicleId":"guid","riderProfileId":"guid-or-null"}
```

After success, refetch the work order, balance, barrel list, vehicle history, rider history, and reminder list.

## Riyadh Workshop: external revenue, mechanic cost, payments, and true profit

These calls require an external order at a location that permits paid external work (Riyadh Workshop). Keep external-vehicle data limited to plate/reference and necessary customer details.

### External part sale

`POST /api/maintenance-work-orders/{id}/part-sales`

```json
{ "inventoryItemId":"guid", "inventoryLocationId":"guid", "quantity":1, "sellingUnitPriceBeforeTax":100, "discountAmount":5, "taxAmount":14.25, "occurredAtUtc":"…", "notes":null }
```

Response: `id, maintenanceWorkOrderId, inventoryItemId, quantity, partsRevenueBeforeTax, taxAmount, customerLineTotal, maintenanceMaterialUsageId`.

The selling price is customer revenue. The FIFO inventory cost is automatically recorded separately by the server; never ask the cashier to enter inventory cost.

### Customer labor charge and mechanic payment

Customer labor charge:

`POST /api/maintenance-work-orders/{id}/customer-labor-charges`

```json
{ "amountBeforeTax":150, "taxAmount":22.5, "occurredAtUtc":"…", "description":"Repair labor" }
```

Mechanic labor payment:

`POST /api/maintenance-work-orders/{id}/mechanic-labor-payments`

```json
{ "mechanicEmployeeId":"guid-or-null", "externalMechanicName":"External mechanic or null", "amount":80, "paidAtUtc":"…", "description":"Mechanic labor cost" }
```

The customer labor charge is income. The mechanic payment is a separate expense. This separation is required for real workshop profit.

Both labor/financial-entry responses are: `id, maintenanceWorkOrderId, entryType, sourceType, amountBeforeTax, taxAmount, totalAmount, occurredAtUtc, description, mechanicEmployeeId, externalMechanicName`.

### Other income/expense and customer payment

`POST /api/maintenance-work-orders/{id}/other-financial-entries?income=true|false`

```json
{ "amountBeforeTax":20, "taxAmount":0, "occurredAtUtc":"…", "description":"Additional service" }
```

Record money received from the customer independently:

`POST /api/maintenance-work-orders/{id}/customer-payments`

```json
{ "amount":200, "paymentMethod":1, "paidAtUtc":"…", "reference":"RCPT-44" }
```

Payment response: `id, maintenanceWorkOrderId, amount, paymentMethod, paidAtUtc, reference`.

### Profit report

`GET /api/maintenance/external-profit?maintenanceLocationId={riyadhId}&startDate=2026-09-01&endDate=2026-09-30`

```json
{
  "maintenanceLocationId":"guid", "from":"2026-09-01", "to":"2026-09-30",
  "totalIncomeBeforeTax":250, "totalExpense":180, "taxCollected":36.75,
  "customerInvoiceTotal":286.75, "amountPaid":200, "netProfitBeforeTax":70,
  "workOrders":[{
    "maintenanceWorkOrderId":"guid", "workOrderNumber":"WO-…", "externalVehicleReference":"XYZ 1234",
    "partsRevenueBeforeTax":95, "customerLaborRevenueBeforeTax":150, "otherIncomeBeforeTax":5,
    "fifoInventoryCost":70, "mechanicLaborCost":80, "otherExpense":0, "taxCollected":36.75,
    "customerInvoiceTotal":286.75, "amountPaid":200, "outstandingAmount":86.75,
    "paymentStatus":2, "partsGrossProfit":25, "laborProfit":70, "netProfitBeforeTax":70
  }]
}
```

Display the formula clearly:

`Net profit before tax = parts revenue + customer labor revenue + other income − FIFO inventory cost − mechanic labor cost − other expense`.

`amountPaid` is cash collection, not profit. `taxCollected` is shown separately and is not part of profit before tax. Payment status: 1 Unpaid, 2 PartiallyPaid, 3 Paid, 4 Refunded.

## Required frontend behavior for business errors

| Error code | Frontend action |
|---|---|
|`maintenance.insufficient_stock`|Refresh balances and prevent the user from issuing more stock.|
|`maintenance.oil_barrel_not_next_fifo`|Refresh barrels and select the oldest eligible sealed barrel.|
|`maintenance.open_oil_barrel_required`|Guide the user to open/select a barrel; do not post a partial oil operation.|
|`maintenance.oil_loss_allowance_exceeded`|Show remaining loss allowance and cap input at the API-provided value.|
|`maintenance.invalid_bill_file`|Require an accepted file type below 10 MB.|
|`maintenance.invalid_oil_filter`|Require a filter item only when filter change is selected.|
|`maintenance.invalid_odometer`|Require a non-decreasing vehicle odometer value.|
|`maintenance.invalid_location`|Reload locations and choose one permitted for the operation.|
|`maintenance.invalid_state`|Reload the work order before presenting valid next actions.|
|`maintenance.concurrency_conflict`|Reload the resource and replace the stale `rowVersion`.|

## Suggested screen order

1. **Setup:** locations, items, suppliers, and maintenance plans.
2. **Inventory:** receipts with attached bills, balances, FIFO layers, oil barrels, transfers, returns, and rider issues.
3. **Maintenance:** oil reminder dashboard, company work orders, materials, oil changes, vehicle history, and rider history.
4. **Riyadh Workshop:** concise external order, part sale, customer labor, mechanic payment, customer collection, and profit report.

After every stock-affecting request, refetch balances, barrels, and the current work order. This is essential when two users work with the same spare part or oil barrel at the same time.
