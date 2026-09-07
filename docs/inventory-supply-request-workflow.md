# Vehicle and rider warehouse supply workflow

This workflow makes maintenance-part and rider-item requests simple for the requester while keeping warehouse control and FIFO accounting intact.

## Lifecycle

```text
Requester submits once
        |
        v
PendingWarehouseApproval
        |---------------------> Rejected
        |---------------------> Cancelled (requester only)
        v
Warehouse clicks "Approve and issue"
        |
        v
ApprovedAndIssued
```

Submitting a request does not reserve, deduct, or value stock. `approve-and-issue` represents the real physical handover at the warehouse counter. It checks current availability, deducts every line by FIFO, records the warehouse user and time, and commits all lines in one transaction. If any line has insufficient stock, nothing is issued.

## One-shot vehicle maintenance request

The vehicle administrator creates the work order and requests all parts in the same call:

`POST /api/maintenance-work-orders`

```json
{
  "serviceSubjectType": 1,
  "vehicleId": "vehicle-guid",
  "vehicleIssueId": null,
  "maintenanceLocationId": "maintenance-location-guid",
  "maintenanceType": 2,
  "openedAtUtc": "2026-09-07T09:00:00+03:00",
  "scheduledAtUtc": null,
  "odometerAtOpen": 12000,
  "estimatedCost": 0,
  "diagnosis": "Brake noise and weak braking",
  "notes": "Send the car after warehouse approval",
  "externalVehicle": null,
  "supplyRequest": {
    "inventoryLocationId": "inventory-location-guid",
    "notes": "Parts required for this repair",
    "lines": [
      {
        "inventoryItemId": "brake-pad-item-guid",
        "quantity": 1,
        "maintenanceUsageType": 1,
        "expectedReturn": false,
        "notes": "Front brake-pad set"
      },
      {
        "inventoryItemId": "cleaner-item-guid",
        "quantity": 2,
        "maintenanceUsageType": 4,
        "expectedReturn": false,
        "notes": null
      }
    ]
  }
}
```

The response contains the work order and a nested `supplyRequest` with its number, status, vehicle asset/plate, requested lines, requester, decision data, issue data, cost, and `rowVersion`.

Rules:

- This nested request is for company vehicles only.
- The inventory location must belong to the work order's maintenance location.
- Accepted item types are spare parts and consumables. An oil filter is accepted only as a piece item with `maintenanceUsageType: 3`.
- Oil-change orders continue to use the dedicated atomic oil-change API; they cannot use this general request.
- A work order with a supply request cannot be started or receive direct manual material postings while approval is pending.
- Approval posts the material usages and moves the work order from `Open` to `Completed` automatically. The vehicle administrator only needs to close it afterward; no separate complete action is required.

## Rider request

`POST /api/maintenance-inventory/rider-supply-requests`

```json
{
  "riderProfileId": "rider-guid",
  "inventoryLocationId": "inventory-location-guid",
  "requestedAtUtc": "2026-09-07T09:00:00+03:00",
  "notes": "New rider equipment",
  "lines": [
    {
      "inventoryItemId": "helmet-item-guid",
      "quantity": 1,
      "maintenanceUsageType": null,
      "expectedReturn": true,
      "notes": "Return when assignment ends"
    }
  ]
}
```

Rider requests accept active `RiderAccessory` items. Approval creates the normal `RiderInventoryIssue`, links the active rider/vehicle assignment at the issue time when one exists, and preserves `expectedReturn`.

## Warehouse queue and decisions

Warehouse queue:

`GET /api/maintenance-inventory/supply-requests?inventoryLocationId={id}&status=pending`

Optional filters are `inventoryLocationId`, `vehicleId`, `riderProfileId`, and `status`. Status accepts `pending`, `approved`, `issued`, `rejected`, or `cancelled`.

Single request:

`GET /api/maintenance-inventory/supply-requests/{id}`

The original requester can read their own request without warehouse queue access:

`GET /api/maintenance-inventory/my-supply-requests/{id}`

Approve and physically issue:

`POST /api/maintenance-inventory/supply-requests/{id}/approve-and-issue`

```json
{
  "occurredAtUtc": "2026-09-07T09:15:00+03:00",
  "rowVersion": "base64-row-version",
  "notes": "Items handed to vehicle administrator"
}
```

Reject (a reason is required):

`POST /api/maintenance-inventory/supply-requests/{id}/reject`

Cancel by the original requester while pending:

`POST /api/maintenance-inventory/supply-requests/{id}/cancel`

All decisions require the latest `rowVersion`. The requester receives a persistent notification after approval or rejection. Vehicle administrators can also see the current request and all issued details inside the work-order response.

## Permissions

- `inventory.supply_requests.submit`: create rider supply requests and cancel one's own pending request.
- `inventory.supply_requests.read`: view the warehouse queue and request details.
- `inventory.supply_requests.approve`: approve/issue or reject requests.
- `inventory.stock.move`: additionally required for `approve-and-issue` because this action posts stock movements.
- `maintenance.work_orders.manage`: creates the vehicle work order and its nested request.

## Relevant errors

- `maintenance.supply_request_required`: no valid request lines were supplied.
- `maintenance.supply_request_not_pending`: another user already processed the request.
- `maintenance.supply_request_ownership`: a user tried to cancel another user's request.
- `maintenance.supply_approval_required`: the client tried to start/post directly before warehouse approval.
- `maintenance.insufficient_stock`: current FIFO stock cannot fulfill every line; no line is issued.
- `maintenance.concurrency_conflict`: the submitted `rowVersion` is stale.
