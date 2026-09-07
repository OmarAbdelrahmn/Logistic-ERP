# Frontend handoff: warehouse approval for maintenance and rider supplies

## Purpose

Replace direct requester stock issuing with a two-person workflow:

1. A vehicle administrator creates a maintenance order and requested parts in one submission.
2. A warehouse administrator reviews the vehicle/rider and lines, then physically hands items over using one **Approve and issue** action.

The client must never reduce stock locally. Always refresh from the server after a decision.

## Roles and navigation

| User | Required permission | UI entry point |
|---|---|---|
| Vehicle administrator | `maintenance.work_orders.manage`; add `inventory.supply_requests.submit` when they must cancel a pending request | **Maintenance → New work order** |
| Rider/operations administrator | `inventory.supply_requests.submit` | **Inventory → Request rider items** |
| Warehouse administrator | `inventory.supply_requests.read`, `inventory.supply_requests.approve`, `inventory.stock.move` | **Inventory → Supply requests** |

The vehicle administrator reads the request from the work-order response. A rider requester can use the personal request endpoint. The warehouse queue is only for users with request-read permission.

## 1. Create maintenance order screen

### New fields

Keep the existing company-vehicle fields, then add a **Parts requested from warehouse** section:

- Inventory location — required only when one or more lines are added. Filter to locations at the chosen maintenance site.
- Request notes — optional.
- Repeatable lines:
  - inventory item — required;
  - quantity — required and greater than zero;
  - usage type — default from item type; spare part (`1`) or consumable (`4`);
  - line note — optional.

Do not show `expectedReturn` for vehicle lines. Do not ask for a total or estimated cost: the API calculates `estimatedCost` from the requested inventory quantities and current warehouse average costs, then calculates `actualTotalCost` from the FIFO cost actually issued.

For `maintenanceType = 5` (oil change), replace the old second oil-completion screen with fields in the initial work-order form:

- inventory location;
- oil inventory item;
- whether the oil filter will be changed;
- filter inventory item, required only when the filter toggle is on;
- odometer at change in the existing `odometerAtOpen` field.

Do not show barrel selection to the requester. Barrel handling belongs to the warehouse approval action.

### Submit payload

`POST /api/maintenance-work-orders`

```json
{
  "serviceSubjectType": 1,
  "vehicleId": "guid",
  "vehicleIssueId": null,
  "maintenanceLocationId": "guid",
  "maintenanceType": 2,
  "openedAtUtc": "2026-09-07T09:00:00+03:00",
  "scheduledAtUtc": null,
  "odometerAtOpen": 12000,
  "diagnosis": "Brake noise",
  "notes": "Repair after parts handover",
  "externalVehicle": null,
  "supplyRequest": {
    "inventoryLocationId": "guid",
    "notes": "Parts for this work order",
    "lines": [
      {
        "inventoryItemId": "guid",
        "quantity": 1,
        "maintenanceUsageType": 1,
        "expectedReturn": false,
        "notes": "Front brake pad set"
      }
    ]
  }
}
```

If the user adds no lines, send `supplyRequest: null` or omit it. Do not send a supply request for external vehicles.

For a company oil change, send `oilChange` instead of `supplyRequest` in the same create call:

```json
{
  "serviceSubjectType": 1,
  "vehicleId": "guid",
  "maintenanceLocationId": "guid",
  "maintenanceType": 5,
  "openedAtUtc": "2026-09-07T09:00:00+03:00",
  "odometerAtOpen": 12000,
  "diagnosis": "تغيير زيت",
  "externalVehicle": null,
  "supplyRequest": null,
  "oilChange": {
    "inventoryLocationId": "guid",
    "oilInventoryItemId": "guid",
    "oilFilterChanged": true,
    "oilFilterInventoryItemId": "guid",
    "notes": null
  }
}
```

The API resolves the required oil quantity, creates oil/filter request lines, and sends the single request to the warehouse queue. It rejects a new company work order while the vehicle already has an `Open`, `InProgress`, or `Completed` order.

### Success behavior

The normal work-order response now has optional `supplyRequest`:

```json
{
  "id": "work-order-guid",
  "workOrderNumber": "MWO-20260907-1234ABCD",
  "status": 1,
  "supplyRequest": {
    "id": "request-guid",
    "requestNumber": "ISR-20260907-1234ABCD",
    "subjectType": 1,
    "status": 1,
    "vehicleAssetNumber": "CAR-101",
    "vehiclePlateNumber": "أ ب ج 1234",
    "requestedAtUtc": "...",
    "lines": [
      {
        "inventoryItemId": "guid",
        "sku": "BRAKE-1",
        "itemNameAr": "فحمات فرامل",
        "requestedQuantity": 1,
        "issuedQuantity": 0,
        "issuedCost": 0
      }
    ],
    "rowVersion": "base64"
  }
}
```

Cost display rules:

- Company/internal vehicle: label `actualTotalCost` as **التكلفة الإجمالية**; it contains issued inventory/material cost only. `actualLaborCost` is omitted, so do not render **تكلفة أجور اليد والعمالة**.
- Outside/external customer vehicle: the external endpoint returns the full outside-work totals and may include `actualLaborCost`.
- `estimatedCost` is read-only and calculated by the API from the initially requested oil/parts quantities. Never render it as an editable input.

Show an amber status banner: **“Waiting for warehouse approval. Stock has not been issued.”** Do not show **Start maintenance** while this request is pending, rejected, or cancelled.

## 2. Work-order details screen

Add a **Warehouse supply request** card when `supplyRequest` is not null.

Show:

- request number and status;
- selected inventory location;
- vehicle asset number and plate;
- requester/time;
- every requested line with requested quantity, issued quantity, SKU, Arabic name, and issued cost;
- warehouse decision time/user/notes after action;
- total issued cost;
- rejection reason when rejected.

Status presentation:

| API status | Label | UI behavior |
|---|---|---|
| `1` `PendingWarehouseApproval` | Waiting for warehouse | Amber; prevent Start and direct material posting. |
| `2` `ApprovedAndIssued` | Approved and issued | Green; show issued quantities/cost; work order will be `Completed`, so offer only the existing Close action. |
| `3` `Rejected` | Rejected | Red; show reason. The requester must cancel/recreate the work order if new requested parts are needed. |
| `4` `Cancelled` | Cancelled | Gray; show cancellation note. |

The existing direct material-posting UI must be hidden for a work order that has `supplyRequest`; only warehouse approval can issue its original lines.

## 3. Warehouse supply-request queue

Route: **Inventory → Supply requests**.

Initial request:

`GET /api/maintenance-inventory/supply-requests?status=pending`

Available filters:

- `inventoryLocationId`
- `vehicleId`
- `riderProfileId`
- `status`: `pending`, `approved`/`issued`, `rejected`, `cancelled`

Use a table or cards with:

- request number and requested time;
- subject badge: Vehicle maintenance / Rider;
- vehicle asset number + plate, or rider name;
- inventory location;
- requested by;
- line count and expanded item/quantity list;
- status.

The request response already includes detailed lines; no extra item lookup is required to render name/SKU/unit.

### Warehouse detail drawer/modal

Open by request ID:

`GET /api/maintenance-inventory/supply-requests/{id}`

Before issue, show a confirmation such as:

> You are confirming that these items were physically handed over. The system will deduct stock using FIFO and cannot partially approve this request.

Required action body uses the returned `rowVersion`:

`POST /api/maintenance-inventory/supply-requests/{id}/approve-and-issue`

```json
{
  "occurredAtUtc": "2026-09-07T09:15:00+03:00",
  "rowVersion": "base64-row-version",
  "notes": "Handed to vehicle administrator",
  "nextOilBarrelId": null
}
```

`nextOilBarrelId` is a warehouse-only field used for oil requests when the current open barrel does not contain enough oil. On approval of any vehicle-maintenance supply request, the API issues the requested stock and completes the work order immediately. The vehicle administrator only closes it afterward; do not show or call a separate Complete action. For oil requests, the same approval also records the oil operation and odometer and updates the oil schedule. Do not call the legacy `/oil-change` completion endpoint for a company vehicle.

On success, close the confirmation, replace the row with the returned response, refresh stock balances and the request queue. Never calculate FIFO cost in the client.

Reject endpoint:

`POST /api/maintenance-inventory/supply-requests/{id}/reject`

```json
{
  "occurredAtUtc": "2026-09-07T09:15:00+03:00",
  "rowVersion": "base64-row-version",
  "notes": "Brake pads are out of stock"
}
```

The rejection note is required; disable submit until it is present.

## 4. Rider item request screen

Route: **Inventory → Request rider items**.

Fields:

- rider — required;
- inventory location — required;
- requested at — default to now;
- request notes — optional;
- repeatable rider-accessory lines:
  - item;
  - quantity;
  - expected return toggle;
  - line note.

Only allow items whose `itemType` is `RiderAccessory`. Do not show maintenance usage type for a rider request.

`POST /api/maintenance-inventory/rider-supply-requests`

```json
{
  "riderProfileId": "guid",
  "inventoryLocationId": "guid",
  "requestedAtUtc": "2026-09-07T09:00:00+03:00",
  "notes": "New rider equipment",
  "lines": [
    {
      "inventoryItemId": "guid",
      "quantity": 1,
      "maintenanceUsageType": null,
      "expectedReturn": true,
      "notes": "Return at end of assignment"
    }
  ]
}
```

On success show **Waiting for warehouse approval**. The requester can read their own request, including approval/issue data, without warehouse-queue permission:

`GET /api/maintenance-inventory/my-supply-requests/{id}`

## 5. Cancel behavior

The original requester can cancel only a pending request:

`POST /api/maintenance-inventory/supply-requests/{id}/cancel`

```json
{
  "occurredAtUtc": "2026-09-07T09:05:00+03:00",
  "rowVersion": "base64-row-version",
  "notes": "Wrong item selected"
}
```

Hide cancel after approval, rejection, or cancellation. Do not offer cancel to another requester.

## Error handling

| Error code | Client action |
|---|---|
| `maintenance.insufficient_stock` | Show that the warehouse cannot issue the full request. Do not mark any line locally. Refresh balances and keep the request pending. |
| `maintenance.concurrency_conflict` | Reload request details and require the warehouse user to confirm again. |
| `maintenance.supply_request_not_pending` | Reload queue; another user already acted. |
| `maintenance.supply_approval_required` | Return to the supply-request card; do not allow start/direct issue. |
| `maintenance.supply_request_ownership` | Hide cancel and show that only the original requester may cancel. |
| `maintenance.invalid_location` | Refresh allowed inventory locations for the maintenance site. |
| `maintenance.invalid_inventory_item` | Refresh items; the item type or unit no longer matches this request. |
| `maintenance.active_vehicle_work_order_exists` | Open the existing active work order; do not submit another for the same company vehicle. |
| `maintenance.oil_change_request_required` | Keep oil/filter/odometer fields on the initial create form and resubmit once. |
| `maintenance.labor_cost_external_vehicles_only` | Hide labor cost for company vehicles. Labor cost belongs only to outside/external customer work. |

## Notifications and refresh

The backend sends the requester a notification after approval or rejection. When a notification deep-links to the item:

- vehicle request: reload the work order;
- rider request: call `GET /api/maintenance-inventory/my-supply-requests/{id}`.

After a warehouse decision, refresh:

1. the warehouse queue;
2. request details;
3. stock balances;
4. the linked work order or rider history.

## Endpoint summary

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/maintenance-work-orders` | Create vehicle work order plus optional one-shot parts request. |
| `GET` | `/api/maintenance-work-orders/{id}` | Read work order and nested supply request. |
| `GET` | `/api/maintenance-work-orders` | Company/internal work orders only. |
| `GET` | `/api/maintenance-work-orders/external` | Outside/external customer work orders only. |
| `POST` | `/api/maintenance-inventory/rider-supply-requests` | Submit rider item request. |
| `GET` | `/api/maintenance-inventory/my-supply-requests/{id}` | Requester reads their own rider request. |
| `GET` | `/api/maintenance-inventory/supply-requests` | Warehouse queue and filters. |
| `GET` | `/api/maintenance-inventory/supply-requests/{id}` | Warehouse request detail. |
| `POST` | `/api/maintenance-inventory/supply-requests/{id}/approve-and-issue` | Warehouse approves and physically issues all lines. |
| `POST` | `/api/maintenance-inventory/supply-requests/{id}/reject` | Warehouse rejects pending request with reason. |
| `POST` | `/api/maintenance-inventory/supply-requests/{id}/cancel` | Original requester cancels pending request. |
