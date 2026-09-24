# Direct oil change and complete history APIs

All routes require bearer authentication. JSON uses camelCase names and numeric enum values, as with the existing API. Times are UTC. All three new read routes return `200 OK` or a Problem Details error.

## Complete an oil change without a work order

`POST /api/maintenance/vehicles/{vehicleId}/oil-changes`

Permissions: `maintenance.oil.complete` and `inventory.stock.move`. Send a unique `Idempotency-Key` header (up to 200 characters). A repeat with the same key and identical body returns the original operation; a changed body returns `409 maintenance.oil_change_idempotency_conflict`.

```json
{
  "performedAtUtc": "2026-09-24T10:00:00Z",
  "odometerAtChange": 25000,
  "inventoryLocationId": "00000000-0000-0000-0000-000000000001",
  "oilInventoryItemId": "00000000-0000-0000-0000-000000000002",
  "nextOilBarrelId": null,
  "oilFilterChanged": true,
  "oilFilterInventoryItemId": "00000000-0000-0000-0000-000000000003",
  "configuredOilQuantityLiters": null,
  "otherCost": 0,
  "notes": "Routine service",
  "vehicleRowVersion": "BASE64_ROW_VERSION"
}
```

`nextOilBarrelId` is needed only when the open barrel cannot cover the required quantity. For cars, the service enforces 4 L with a filter or 3.5 L without it. Motorcycles use `configuredOilQuantityLiters` or an active maintenance plan quantity. `oilFilterInventoryItemId` must be present exactly when `oilFilterChanged` is true. Odometer must be at least the vehicle's current reading. The inventory location must be active, linked to an active company maintenance location, and contain eligible oil and filter stock. The stock posting follows existing FIFO and oil-barrel rules.

`GET /api/maintenance/oil-inventory-locations` requires `maintenance.oil.read` and returns `{ inventoryLocationId, maintenanceLocationId, inventoryLocationNameAr, maintenanceLocationNameAr }[]` for eligible active locations. Use `inventoryLocationId` in the POST body.

`GET /api/maintenance/oil-barrels?inventoryLocationId={id}&inventoryItemId={id}` also requires `maintenance.oil.read`. It returns open and sealed barrels as `{ id, barrelNumber, inventoryLocationId, inventoryItemId, status, remainingLiters }[]`. It does not reveal costs or FIFO layers.

Success (`200 OK`):

```json
{
  "id": "00000000-0000-0000-0000-000000000010",
  "maintenanceWorkOrderId": null,
  "performedAtUtc": "2026-09-24T10:00:00Z",
  "odometerAtChange": 25000,
  "vehicleType": 2,
  "oilQuantityLiters": 4,
  "oilCost": 40,
  "oilFilterChanged": true,
  "oilFilterCost": 25,
  "laborCost": 0,
  "otherCost": 0,
  "totalCost": 65,
  "vehicleId": "00000000-0000-0000-0000-000000000011",
  "riderProfileId": null
}
```

One transaction records the oil-change operation, oil and optional filter material usage, FIFO allocations, barrel usage, stock movements and balances, vehicle expenses, odometer reading, and maintenance schedule. No work order or warehouse approval request is created. A completed oil-change material usage cannot be reversed alone, because that would leave an inaccurate oil-change record and reminder.

### Oil-change report and reminders

- `GET /api/maintenance/oil-changes` or `GET /api/maintenance/oil-changes?vehicleId={vehicleId}` requires `maintenance.oil.read`. Returns an array of `OilChangeReportResponse`, newest first. Each row contains `id`, nullable `maintenanceWorkOrderId`, `vehicleId`, `vehicleAssetNumber`, date, odometer, oil and filter item IDs and costs, total cost, material usage IDs, performer, and notes. Both direct and existing work-order oil changes appear.
- `GET /api/maintenance/oil-reminders` continues to return the last oil change and next thresholds for each car or motorcycle, now including direct operations. The vehicle maintenance schedule is updated from the completed direct operation as well.
- `GET /api/maintenance/vehicles/{vehicleId}/material-history` includes the oil and filter stock usage with nullable `maintenanceWorkOrderId`.

The frontend reminder page at `/admin/maintenance/work-orders/reminders` invokes the direct POST route. Existing work-order maintenance and oil-change APIs remain available.

## Complete vehicle history

`GET /api/vehicles/{vehicleId}/complete-history`

Permissions: `fleet.vehicles.read`, `fleet.assignments.read`, `fleet.issues.read`, `fleet.accidents.read`, `maintenance.work_orders.read`, and `inventory.stock.read`, plus vehicle scope access. No query parameters.

## Complete rider history

`GET /api/riders/{riderProfileId}/complete-history`

Permissions: `fleet.assignments.read`, `fleet.issues.read`, `fleet.accidents.read`, `maintenance.work_orders.read`, and `inventory.stock.read`. No query parameters.

Both endpoints return `CompleteHistoryResponse`:

```json
{
  "subjectId": "00000000-0000-0000-0000-000000000011",
  "subjectType": "vehicle",
  "subjectName": "CAR-101",
  "generatedAtUtc": "2026-09-24T10:01:00Z",
  "totalEvents": 1,
  "events": [
    {
      "occurredAtUtc": "2026-09-24T10:00:00Z",
      "category": "oil_change",
      "action": "direct",
      "entityId": "00000000-0000-0000-0000-000000000010",
      "vehicleId": "00000000-0000-0000-0000-000000000011",
      "riderProfileId": null,
      "assignmentId": null,
      "summary": "Oil and filter service",
      "details": {
        "maintenanceWorkOrderId": null,
        "odometerAtChange": 25000,
        "oilQuantityLiters": 4,
        "totalCost": 65
      },
      "files": []
    }
  ]
}
```

`subjectType` is `vehicle` or `rider`. `events` are sorted newest first. `category` and `action` identify the record type; `details` carries fields specific to that record. `files` contains `{ id, fileName, contentType, fileSizeBytes, downloadPath }`; paths call existing authenticated file-download routes and never expose storage paths. Downloading a file still requires the download route's own permission.

Vehicle events cover assignment handovers and endings, assignment changes and promissory notes, issues/damages and return evidence, accidents and evidence/reports (including archived older accidents), maintenance work orders, material usage and oil changes, vehicle expenses, registration/insurance/inspection/operation-card records, status, odometer, identity correction, registration transition, and vehicle attachment versions. Rider events include their assignments, vehicle returns and evidence, issues tied to their assignments, accidents, attributed maintenance/material usage/oil changes and expenses, promissory notes, issued equipment and recorded returned quantities, and supply requests.

Equipment issue lines currently store `returnedQuantity` but have no dedicated return timestamp or return transaction. The rider timeline marks that entry `return_balance_recorded` and `returnTimestampKnown: false` in `details`; its timestamp is the issue record's last update (or issue time when unavailable), not a claimed physical return time.

The complete-history endpoints have no frontend integration yet.
