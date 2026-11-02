# Frontend integration: completed oil-change corrections

Use [the API contract](oil-change-edit-cancel-api.md) for the exact payloads and response fields. Enable this UI only after the database migrations and matching API are deployed.

## Actions

Keep three distinct actions on each oil-change row:

- **تصحيح المركبة**: existing vehicle-only correction route and restrictions.
- **تعديل غيار الزيت**: new edit workflow, visible with `maintenance.oil.edit` and the required stock/cost permissions.
- **إلغاء غيار الزيت**: new cancellation workflow, visible with `maintenance.oil.cancel` and the required stock/cost permissions.

Display **ملغاة** when `isCancelled=true`; disable edit/cancel while retaining details and history. Display `revisionNumber` or a history action so users can see that an operation was corrected. Do not implement cancellation as a delete request.

## Edit modal

1. Load `GET /api/maintenance/oil-changes/{id}` when opening the modal. Retain its operation and work-order versions.
2. Prefill service date/time, total odometer, warehouse, oil item/quantity, filter selection, costs and notes from details. Do not reconstruct them from formatted table strings. Vehicle is displayed as identity; changing it uses the separate existing action.
3. Enable date editing only with `maintenance.oil.date_edit`. Convert the chosen local date/time into an explicit offset/UTC instant. Keep displayed accounting posting date distinct from service date.
4. Require a correction reason. Enforce the API's length/money limits and filter item/state consistency. For company vehicles keep labor cost at zero.
5. Call the correction preview route. Show the server-calculated oil quantity, filter cost, total cost, affected barrels, rider attribution, resulting mileage and work-order status. Car oil quantity can change when the filter is toggled; use the server result.
6. After review, commit with the same inputs/versions, returned `effectsHash` as `expectedEffectsHash`, and a new idempotency key for this logical action.

Any draft change after preview invalidates that preview. Disable the final submit button until a new preview succeeds. Prevent repeated button clicks while committing, and reuse the same key/payload after a network timeout. Generate a new key only for a new logical request.

## Cancellation modal

Show the selected operation ID, vehicle/external reference, service date, oil and filter details. Require a reason. Preview the cancellation and show the stock restoration, cost reversal, resulting mileage and order status. Confirming calls the cancel route using the preview hash and versions.

Explain that the historical record remains and costs are reversed in the current month. If an external customer has already paid, show that the payment remains and that customer balance resolution is separate. Cancellation of one duplicate must identify that exact operation; do not cancel another row because its plate/date appears similar.

## Error handling

- On a version or preview-effects conflict, preserve the draft, reload current details and show a fresh comparison before committing again.
- On already-cancelled, refresh the row and display its cancellation details.
- On barrel restoration conflict, show the API detail and the involved operation/barrel context from the preview/details. Do not retry silently or select another barrel automatically.
- On missing mileage basis, obtain a documented value through the authorized workflow. Do not automatically fill zero or the vehicle's current odometer.
- Display ProblemDetails `detail`; retain `correlationId` for support. Treat an unsuccessful request as no completed correction, then reload if the network result was ambiguous.

## Refresh after success

Refresh operation details/list, revision history, the affected work order and warehouse issue view, stock balances/FIFO layers, barrel usage, vehicle mileage/reminders, and vehicle/workshop expenses or reports currently visible. Use normal dataset invalidation/refetch; do not calculate local stock adjustments from the preview's barrel rows.

Use `isCancelled` when calculating active service totals. Historical `totalCost` remains on cancelled operations for audit. Use net financial entries for accounting totals; never add original and replacement costs without their reversal entries.

## History display

Use the paginated revisions route. Show action, revision number, actor, reason, posting time and selected before/after values: service date, odometer, item/quantity/filter, costs, rider and work-order effects. Render JSON values as text. The full vehicle history also includes oil-change revision events and flags voided odometer evidence.

For material usage, display `effectiveServiceAtUtc ?? usedAtUtc` as the effective service instant and retain `usedAtUtc` separately for posting evidence. Show original and reversal usage with clear direction; barrel `netUsedLiters` already accounts for reversals. Waste/loss is a separate category and must not be added to vehicle consumption again.

## Frontend acceptance checklist

- A direct operation can be previewed/edited; changing a draft clears the preview.
- Add/remove filter uses the server's updated oil quantity and cost.
- A work-order edit sends both versions and refreshes its totals/status.
- A cancellation retains the record, restores stock once, and refreshes reminders.
- A retry after timeout reuses the original key and causes no duplicate effects.
- An old operation version or changed preview produces a review flow, not an automatic resubmit.
- A service date across month end displays service and posting dates correctly.
- A cancelled row remains readable with its reason, actor and revision history.
- The UI handles unassigned riders and external vehicles without inventing an identity or mileage.

## Closed barrel message

When `effects.returnedAsWasteLiters > 0`, display **الكمية المرتجعة من البرميل المغلق سُجلت كهالك** with that quantity. Explain that the barrel remains closed and available stock will not increase. Show these entries in the existing barrel loss/missing history with `missingReason = 2` and their `correctionRevisionId`. Include this effect in the user's preview review before commit.

A barrel's `vehicles` row is a grouped total and can represent several oil changes. Its delete/cancel action must first identify the exact oil-change operation ID, service date and original usage; do not cancel every usage for that vehicle or send the barrel ID as the operation ID.
