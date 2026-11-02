# Completed oil changes: edit and cancel API

Implementation source: `MaintenanceOilChangeWorkflowService`, `MaintenanceOilChangeWorkflowEffects`, and `OilChangeWorkflowContracts`. Database migration and matching API deployment are required. This file is an integration contract, not a deployment record.

## Routes and permissions

All routes require authentication. Base path: `/api/maintenance`.

| Method | Route | Permissions |
| --- | --- | --- |
| GET | `/oil-changes/{id}` | `maintenance.oil.read`, `inventory.stock.read`, `inventory.cost_layers.read` |
| GET | `/oil-changes/{id}/revisions?page=1&pageSize=50` | Same as details |
| POST | `/oil-changes/{id}/corrections/preview` | `maintenance.oil.edit`, `inventory.stock.move`, `inventory.cost_layers.read` |
| PUT | `/oil-changes/{id}` | Same as edit preview |
| POST | `/oil-changes/{id}/cancellation/preview` | `maintenance.oil.cancel`, `inventory.stock.adjust`, `inventory.cost_layers.read` |
| POST | `/oil-changes/{id}/cancel` | Same as cancel preview |

Changing `performedAtUtc` additionally requires `maintenance.oil.date_edit` in both preview and commit. A replay of a date-edit request checks that permission again. Permission keys must be assigned by an authorized administrator after migration.

The existing `PUT /oil-changes/{id}/vehicle` remains separate and keeps its direct-operation, same-vehicle-type and administrator restrictions. The new edit route does not change vehicle identity.

## Load details first

Details contain `operation` (the existing oil-change report fields plus `rowVersion`, `isCancelled`, `revisionNumber`), `workOrderRowVersion`, `inventoryLocationId`, `materials`, and nullable `cancelledAtUtc`, `cancelledByUserId`, `cancellationReason`.

`materials` includes the current oil and optional filter usage, quantities/costs/FIFO allocations, rider attribution, original `usedAtUtc` and nullable `effectiveServiceAtUtc`. Display `effectiveServiceAtUtc ?? usedAtUtc` as the service date. Keep `usedAtUtc` available as stock-posting evidence.

## Preview an edit

```http
POST /api/maintenance/oil-changes/{id}/corrections/preview
Content-Type: application/json
```

```json
{
  "performedAtUtc": "2026-10-29T09:00:00Z",
  "odometerAtChange": 100,
  "inventoryLocationId": "warehouse-guid",
  "oilInventoryItemId": "oil-item-guid",
  "configuredOilQuantityLiters": 3.5,
  "oilFilterChanged": false,
  "oilFilterInventoryItemId": null,
  "nextOilBarrelId": null,
  "laborCost": 0,
  "otherCost": 0,
  "notes": "Corrected service details",
  "reason": "Incorrect entry",
  "rowVersion": "CURRENT_OPERATION_VERSION",
  "workOrderRowVersion": null,
  "expectedEffectsHash": null,
  "mileageBaselineKm": null
}
```

For linked work orders, send their current `workOrderRowVersion` from details. Direct operations use null. A filter item must be present exactly when `oilFilterChanged` is true. The server applies existing oil quantity rules, including 3.5 L for a car without filter and 4 L with filter. Labor cost is supported for external work orders; company-vehicle/direct operations require zero. Costs must be nonnegative, have at most two decimals and not exceed SAR 1 billion each. Service date must be nondefault and no later than server time; notes allow 2,000 characters and reason allows 1,000 nonblank characters.

## Preview result and commit

Both previews return the same response structure as mutation:

```json
{
  "operationId": "operation-guid",
  "revisionId": null,
  "preview": true,
  "rowVersion": "CURRENT_OPERATION_VERSION",
  "workOrderRowVersion": null,
  "postedAtUtc": "2026-10-31T10:00:00Z",
  "effectsHash": "SERVER_HASH",
  "effects": {
    "cancelled": false,
    "materialsReplaced": false,
    "oilQuantityLiters": 3.5,
    "oilCost": 35,
    "filterCost": 0,
    "laborCost": 0,
    "otherCost": 0,
    "totalCost": 35,
    "riderProfileId": null,
    "vehicleMileageKm": 100,
    "barrels": [
      { "barrelId": "barrel-guid", "issuedLiters": 3.5, "reversedLiters": 0 }
    ],
    "workOrderStatus": null,
    "returnedAsWasteLiters": 0
  }
}
```

Examples use illustrative IDs and amounts. `barrels` describes the involved old/current usage and linked reversals, not the barrel's entire lifetime usage or a simple quantity to add/subtract client-side. `workOrderStatus` is the resulting status for a linked order, otherwise null. Numeric enums follow existing contracts. `vehicleMileageKm` is null for external vehicles.

To commit, use `PUT /oil-changes/{id}`, the reviewed edit payload with `expectedEffectsHash` set to `effectsHash`, and an `Idempotency-Key` header (nonblank, maximum 200 characters). Do not replace the input versions with a locally invented value. Successful response has `preview=false`, a `revisionId`, updated versions and final effects.

Preview executes the calculation inside a transaction that is rolled back. It saves no business effects and reserves no stock. Commit repeats validation. Changed calculation, stock selection, costs, mileage or versions can require a fresh preview. A preview made on a previous Riyadh calendar date must be refreshed because the posting date participates in the fingerprint.

## Cancel

Preview body:

```json
{
  "reason": "Duplicate oil change",
  "rowVersion": "CURRENT_OPERATION_VERSION",
  "workOrderRowVersion": null,
  "expectedEffectsHash": null,
  "mileageBaselineKm": null
}
```

Send it to `POST /oil-changes/{id}/cancellation/preview`. Commit the same body with the returned hash to `POST /oil-changes/{id}/cancel` and provide an `Idempotency-Key`.

Cancellation reverses oil/filter usage and the operation's active costs once, marks the operation cancelled, retains its original recorded costs for history, voids its maintenance odometer evidence and recalculates mileage/reminders. Effective cancelled cost is `effects.totalCost=0`. Do not include cancelled operation costs in active totals simply by summing their historical `operation.totalCost`.

Customer receipts are retained. An order with customer payments or unrelated remaining costs retains its previous lifecycle status; a zero-cost order without payments is cancelled. The oil-change operation is cancelled in either case. Customer balance resolution is separate from cancelling the oil service.

## Dates, accounting and mileage

- Original stock movements and costs remain traceable. Date/odometer/notes edits do not reissue stock. Material edits reverse old allocations and issue replacements against current validated stock and FIFO costs.
- Service date and stock/accounting posting time are separate. Corrections/reversals post using server time in the current month; original financial rows remain, with linked negative and replacement rows. Vehicle expense posting dates use Riyadh dates.
- Corrected rider attribution uses the assignment covering the corrected service instant, with start inclusive/end exclusive, excluding cancelled/corrected assignments. No covering assignment yields unassigned attribution.
- Oil reminders use the latest active operation. Oil-change workshop service reporting uses the effective service date; other workshop jobs retain their existing opened-date filtering. Accounting evidence retains its own posting dates.
- Mileage reconciliation preserves later authoritative readings and replays daily GPS/manual records. Old maintenance evidence is voided, not deleted. If there is no reliable initial basis, the API returns `maintenance.oil_change_mileage_basis_required`. A documented `mileageBaselineKm` may then be supplied; never automatically send zero. It is the basis before the earliest replayed evidence, not an arbitrary desired current reading.

## Errors and retries

Use ProblemDetails `errorCode`, `detail` and `correlationId`.

| Error | Frontend response |
| --- | --- |
| `maintenance.concurrency_conflict` | Reload details and re-preview; preserve the user's draft |
| `maintenance.oil_change_preview_changed` | Show changed effects and require review of a fresh preview |
| `maintenance.oil_change_cancelled` | Refresh and display cancelled state; do not retry as an edit |
| `maintenance.oil_change_idempotency_conflict` | Do not reuse a key for different content |
| `maintenance.oil_change_barrel_restore_conflict` | Show the detail; inventory reconciliation is required before retrying |
| `maintenance.oil_change_mileage_basis_required` | Obtain a documented baseline or correct the mileage evidence first |
| `maintenance.oil_change_mileage_conflict` | Explain that the recorded mileage sequence prevents the proposed correction |
| `maintenance.invalid_state` | Show the detail; original usage/cost integrity or a persistence constraint prevented the action |
| Stock/item/filter validation errors | Correct the input or stock issue, then preview again |
| Forbidden | Hide the unauthorized action and display the server message if requested |

One key identifies one logical commit. Repeating the same request/key replays its saved response without new stock or cost effects. Reuse with different content conflicts. Cancelling again under a new key returns already-cancelled. A replay is the original response snapshot; refresh details to obtain the current state after subsequent corrections.

## Revision history

History is ordered by revision number descending. `pageSize` defaults to 50, maximum 100. Response: `items`, `page`, `pageSize`, `totalCount`. Each item contains `id`, `revisionNumber`, `action` (`Edit`, `EditDate`, `Cancel`), reason, actor, posting time, `beforeJson`, `afterJson`, `effectsJson`. JSON snapshots contain operation/material/work-order evidence; render selected fields as text, never raw HTML. Reversal financial rows reference the revision ID; original usage and odometer evidence remain linked.

## Closed-barrel returns become waste

When reversed usage belongs to a depleted or returned barrel, the barrel stays closed. Its returned liters are recorded as a classified missing entry with `missingReason = 2` (Wasted), linked by `correctionRevisionId`. A reversal and an offsetting waste movement are recorded atomically; available stock and the source cost-layer quantity do not increase. `recordedMissingLiters` increases, without consuming the ordinary 2% loss allowance. Preview/commit return `effects.returnedAsWasteLiters`. Barrel usage `lossEntries` includes the revision link. The action requires `inventory.stock.adjust` when this rule is triggered, including an edit that replaces material from a closed barrel.

Open barrels continue to receive valid restored stock. Moved barrels or inconsistent source allocations can still return a reconciliation conflict. A material replacement after writing off the old return must consume other available stock; it cannot reuse the wasted liters.
