# TASK-MNT-03 — remaining implementation plan

Status: backend workflow implemented in source; see `oil-change-edit-cancel-api.md` and `oil-change-edit-cancel-acceptance.md` for the current contract, verification and deployment limits. The steps below retain the original implementation plan.

Later user decision: returns from depleted/returned barrels become classified waste/missing, keep the barrel closed and do not increase usable stock. This supersedes the closed-barrel conflict-only proposal below.

## Scope and accepted decisions

Complete backend editing and cancellation of completed oil changes, followed by frontend integration Markdown. Cover direct changes, company-vehicle work orders issued by the warehouse, and external-vehicle work orders. Keep the existing vehicle-change action as a separate action with its existing restrictions.

- No physical deletion of completed operations, stock movements, financial entries or original odometer evidence.
- Require a reason, actor, operation row version and idempotency key for every new mutation.
- Do not implement financial-month closing controls, as requested.
- Post financial correction entries in the current month using the server clock. Retain the original and corrected service dates as separate business evidence. Historical service reports may change when a date is corrected; financial posting reports must retain the original entries and show the adjustment in the current month.
- Preserve existing oil quantity rules: changing the filter may also change the required oil quantity. The client must preview the server-calculated quantities and costs.
- Do not directly change the vehicle's current mileage from a backdated oil change; reconcile it with authoritative later readings and daily distances.

## Existing foundation to retain

- `OilChangeOperation.RowVersion`, its response fields and the pending `AddOilChangeConcurrency` migration.
- Existing vehicle correction, its audit entry, rider reassignment and schedule refresh.
- `StageMaterialReversalAsync`, which stages material, FIFO, balance and expense changes without committing them separately.
- Existing direct/work-order posting helpers and barrel allocations.

These are not proof of a complete edit/cancel workflow. The concurrency migration has not been applied to the hosted database.

## Implementation approach

Retain a stable oil-change operation ID. Add immutable revision records containing the old and new snapshots, action, reason, actor, service dates, accounting posting time, request hash and linked stock/expense/odometer references. A successful edit updates the operation's current projection; the prior revision remains available. Cancellation marks the operation cancelled and links the reversing records. This preserves the existing one-operation-per-work-order relationship.

Classify edits before writing:

1. Date, odometer or notes only: preserve physical stock history; record the revision, correct effective service attribution and create any required financial reclassification entries.
2. Oil item, quantity, stock location or filter changes: reverse the affected original usage once and post replacement usage using validated current stock and FIFO costs. Preserve unaffected usage where possible. Save both sets of references in the revision.
3. Cancellation: reverse all unreversed oil/filter usage and linked operation costs, invalidate the operation's effective odometer evidence, and recalculate schedules and affected projections.

Never call the public create or public material-reversal endpoint from the new service: their separate commits cannot provide an atomic full operation.

## 1. Define contracts and schema

Add contracts to `MaintenanceContracts.cs`, methods to `IMaintenanceService`, and a dedicated partial workflow service in the existing maintenance slice.

Proposed routes under `/api/maintenance`:

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/oil-changes/{id}` | Current details, versions, material/barrel references and supported actions |
| POST | `/oil-changes/{id}/corrections/preview` | Validate a proposed edit; return quantities, costs, attribution, warnings and blockers |
| PUT | `/oil-changes/{id}` | Apply the reviewed edit |
| POST | `/oil-changes/{id}/cancellation/preview` | Show stock, cost, mileage and work-order effects |
| POST | `/oil-changes/{id}/cancel` | Cancel with linked reversals |
| GET | `/oil-changes/{id}/revisions` | Paginated audit/reversal history |

Edit fields: `performedAtUtc`, `odometerAtChange`, `inventoryLocationId`, `oilInventoryItemId`, `configuredOilQuantityLiters`, `oilFilterChanged`, `oilFilterInventoryItemId`, optional next barrel, applicable labor/other costs, notes, `reason`, `rowVersion`, and linked work-order version when applicable. Keep vehicle reassignment on the existing vehicle route.

Cancel fields: `reason`, `rowVersion`, linked work-order version when applicable. Server determines the correction posting time. Both mutations require `Idempotency-Key`; reuse with different content returns conflict.

Preview must return the current operation version and calculated effects. Mutation reruns validation and calculation; a preview is not a stock reservation or authorization grant. Changed versions/effects must cause a reviewable conflict, not silently apply a different result.

Persistence changes:

- Operation lifecycle: active/cancelled, cancellation actor/time/reason and last revision reference.
- Revision table: immutable snapshots, action, request identity/hash, original/replacement usage references and reversal links.
- Database uniqueness preventing two reversals of the same usage/allocation/expense, after checking existing data for duplicates. Apply only to the actual reversal records, not normal multi-layer allocations.
- An explicit way to supersede/void maintenance odometer evidence without deleting it; update all readers that determine effective mileage.
- Revision-aware reporting links for effective service date and accounting posting date. Preserve historical date semantics outside this workflow.
- Backfill existing operations as active; do not change existing stock quantities, costs, dates or rider assignments.

## 2. Authorization and transaction ownership

Add separately seeded permissions for oil-change editing, date editing and cancellation. Existing read permissions cover preview/details/history, with cost-sensitive fields retaining their existing cost permissions. Preview and mutation must enforce the same action authorization. Check date-edit permission only when the date changes.

The application service owns one database transaction for the complete action, using the existing EF execution strategy and serializable isolation where needed for allocation and reversal decisions. Helpers stage changes and do not commit.

Read operation, linked work order, usage, stock layers, allocations and prior reversals. Validate the operation/work-order versions. Coordinate concurrent inventory changes through existing stock/barrel versions plus database constraints. Lock resources in a stable order. Store the successful request identity with the revision in the same transaction so retries cannot reverse or issue stock twice.

On failure, roll back and remove/dispose abandoned tracked changes so a subsequent save cannot commit a partial attempt. Preserve other callers' behavior when changing shared helpers. Do not implement this through multiple independent API calls.

## 3. Complete cancellation first

Implement cancellation as the reusable base for inventory-changing edits:

1. Resolve idempotent replay before applying stock changes; reject a key reused with a different request.
2. Validate operation state, versions, original usages, allocations and existing reversal references.
3. Stage oil and optional filter reversals through the shared helper; restore the exact original FIFO quantities and costs.
4. Reverse linked labor/other costs and external workshop financial entries exactly once, in addition to material costs. Preserve customer receipts; expose any resulting customer balance for the workshop workflow to settle separately.
5. Update work-order material/labor/other totals and lifecycle consistently. Preserve warehouse supply-request issue evidence and link its correction; do not rewrite an issued request as if no issue occurred.
6. Mark the oil-change operation cancelled, append the revision, reconcile mileage and reminders, then commit once.

Repeated cancellation must return a documented already-cancelled result or replay the original success; it must never return stock twice. Cancellation does not delete a duplicate automatically: the user selects the exact operation to cancel.

Barrel rules must be explicit in preview and mutation. Restore to the original barrel/layer only when capacity and status permit. A depleted barrel can reopen if no incompatible active barrel exists. If the barrel was returned or reopening would violate the one-open-barrel constraint, return a specific conflict with the affected barrel IDs and required inventory reconciliation; roll back the entire action. Do not silently restore liters to a different barrel, exceed capacity, or create an extra open barrel.

## 4. Implement editing

Use the same revision and transaction infrastructure. Compute affected usages first, then stage reversal/replacement only for changed materials. Adding a filter must consume stock exactly once; removing it must reverse the original filter usage. Recalculate car oil quantity when filter state changes. Replacement costs come from validated FIFO allocations; original reversal costs come from original allocations.

For corrected service dates, resolve the rider/vehicle assignment that existed at the corrected time. Preserve original attribution on the old records and put corrected attribution on the replacement/reclassification records. Cover assignments starting/ending at the exact boundary and gaps without an assigned rider.

Keep `performedAtUtc` (service time), correction posting time, and audit recording time distinct. A date-only edit must not pretend physical stock was issued again. If backdated material entry uses current stock, record the current stock posting time and the historical service date separately; validate that existing readers can represent both before exposing the action.

Do not undo an unrelated later reading or GPS correction. Rebuild the affected mileage projection using valid evidence and daily sources; when historical evidence is insufficient, return a specific conflict with the missing basis instead of guessing. Refresh maintenance schedules from the latest active operation, not from the last edited request.

## 5. Update reports and existing callers

Audit all consumers of `OilChangeOperations`, maintenance material usage, vehicle expenses, external financial entries and maintenance odometer readings:

- Current oil-change list/details expose lifecycle, row version, revisions and reversal/replacement references.
- Oil reminders and schedule refresh ignore cancelled operations and use the latest effective active revision.
- Vehicle complete history retains cancelled/replaced records and labels their status and links.
- Barrel usage includes issue/reversal quantities without double-counting replacements; loss/waste stays separate.
- Service-date reports use corrected effective dates; accounting posting reports retain original entries and show current-month corrections.
- Work-order and warehouse reports retain original issue evidence and show corrections explicitly.
- Existing creation idempotency replays must not recreate a cancelled operation or misrepresent its current state.

## 6. Verification and acceptance

Use unit tests for classification/calculation and isolated SQL Server integration tests for transactions, rowversion, uniqueness, FIFO and rollback. EF InMemory tests alone cannot prove those guarantees. Do not use production financial mutations as test fixtures.

Required scenarios:

| Scenario | Required outcome |
| --- | --- |
| Single operation cancelled | Correct oil/filter quantities and costs reversed once |
| Two genuine duplicate entries | Only the selected operation cancelled; other remains active |
| Retry, concurrent cancellation, reused key with different payload | One set of effects; deterministic replay/conflict |
| Filter absent/present/added/removed | Correct filter stock and oil-quantity rules |
| Direct, warehouse company work order, external work order | Correct linked totals, attribution and preserved source evidence |
| Multi-barrel and multi-layer usage | Restore original allocation quantities/costs exactly |
| Depleted barrel, another open barrel, returned barrel | Valid restore or documented conflict; no partial changes |
| Stale operation/work-order/inventory versions | Conflict with no effects |
| Date across month end and Riyadh midnight | Correct service date and separate current-month posting evidence |
| Rider switch at corrected date | Correct historical attribution and preserved old attribution |
| Later GPS/manual readings; cancel latest/older/only oil change | Valid mileage and reminders; no unrelated reading overwritten |
| Fault after oil restore, filter issue, expense reversal or audit save | Entire transaction rolled back |
| Negative stock, capacity overflow, missing original allocations | Action rejected atomically with actionable error |

Also test authorization for each action, old vehicle-correction compatibility, empty/paginated revision history, report totals and migration application against representative legacy data.

## 7. Frontend Markdown deliverables

Create the following documents after contracts and behavior are implemented and tested:

1. `oil-change-edit-cancel-api.md`: exact routes, permissions, requests/responses, idempotency/version rules, enums and errors.
2. `oil-change-edit-cancel-frontend.md`: separate change-vehicle/edit/cancel actions; prefilled details; reason; server preview; effect confirmation; conflict reload; success refresh of operation, stock, barrel, expenses and reminders; cancelled-state display and history.
3. `oil-change-edit-cancel-acceptance.md`: tested examples, test evidence, migration/deployment status and remaining limitations.

Rewrite the existing concurrency preparation document to link to the final handoff. Never document proposed routes as available before they are implemented.

## Delivery order and completion gate

1. Finalize contracts, invariants and revision schema; retain the existing pending concurrency migration.
2. Add permissions and idempotent transaction infrastructure.
3. Finish cancellation and SQL rollback/retry tests.
4. Finish metadata and material editing with attribution/mileage tests.
5. Update every affected report and existing caller.
6. Run focused integration tests, regression checks and API build; inspect generated SQL and pending model consistency.
7. Write frontend handoff from the verified implementation.
8. Prepare migration/deployment artifacts. Database application and API deployment must be reported separately; this plan does not deploy either.

The task is complete only when the new endpoints perform their effects atomically, the acceptance scenarios have recorded results, the Markdown matches the actual contracts, and any unsupported scenario has an explicit tested error. A helper refactor, version token or successful build alone does not complete TASK-MNT-03.
