# تصحيح بداية احتساب عمولة تصفية جاهز

## Behavior

Add **تصحيح بداية احتساب العمولة** to a settlement and its cashbox row. The operator selects a new inclusive start date; the saved end date (`operation.throughDate`) stays fixed. The server recalculates **عمولة الشركة المعتمدة** (`commission`) and calculated commission, preserving `net`, then sets `remaining = net + commission`. For paid settlements, the existing cashbox movement and fund balance reflect the corrected amount and receipt/disbursement direction.

Example: a settlement has net 100, start 02/10/2026, end 06/10/2026 and daily commission 75. Correcting the start to 04/10/2026 gives commission 45 and cashbox receipt 145. The end remains 06/10/2026. Percentage commission uses the existing approved policy and earnings evidence; incomplete earnings cause a conflict, without saving partial changes.

This repository contains the backend and a typed frontend client. The consuming frontend must add the action/dialog described here. Apply migration `20261018144507_AddJahezSettlementCommissionStart` before deploying the updated API; no hosted database is changed by authoring this feature.

## Endpoint

```http
PUT /api/jahez/v2/operations/{operationId}/commission-start
Authorization: Bearer <access-token>
Content-Type: application/json
Idempotency-Key: <unique-key-for-this-submission>
```

```json
{
  "startDate": "2026-10-04",
  "reason": "تصحيح تاريخ بداية احتساب عمولة التصفية",
  "rowVersion": "<latest operation rowVersion>"
}
```

Only the start date, reason and concurrency version are normally needed. The request has no end date or net field. `reason` is required and limited to 2000 characters. Use ISO `YYYY-MM-DD` calendar dates in Riyadh time.

Optional `fund` and `paymentReference` select a valid money route if needed. Defaults preserve the current method/reference. For a previously zero settlement becoming nonzero, supply a fund and payment reference. If a PetroApp receipt becomes a payout, select Cashbox (`1`) or Bank (`3`); PetroApp (`2`) cannot disburse. Bank cannot receive. References are nonblank for nonzero paid settlements, maximum 500 characters. This corrects the recorded amount; it does not initiate an external bank transfer.

HTTP 200 returns the complete updated `Operation` directly. The response includes `commissionStartDate`, `commission`, `calculatedCommission`, `remaining`, `paymentStatus` and the new `rowVersion`. Explicitly correcting the date replaces any prior manual commission override with the recalculated amount and clears its approval link; the old approval decision remains in history.

## Permissions and supported states

All four existing permissions are required in the Jahez platform scope, including retries:

- `jahez.settle`
- `jahez.requests.adjustment.approve`
- `jahez.money.receive`
- `jahez.money.disburse`

The action supports Ready (`1`) and Executed (`3`) operations with `purpose = Settlement (1)`, mode Settle (`1`) or SettleAndWithdraw (`2`), and `transferToRider = false`. Pending-review operations must complete their existing review first. Transferred debt and withdrawal-only operations use their own workflows.

The start must be within the recorded account-use period and on or before the fixed end date. It must follow the preceding executed settlement's end, preventing overlapping commission periods. A Ready settlement must also start after the session's posted commission cursor. Closed usage periods can be corrected, and earlier settlements can be corrected without moving later settlement dates or the session's commission cursor.

An existing movement included in a Pending or Confirmed accountant handover is protected. A Pending handover can be cancelled through the existing cancellation workflow, then the settlement corrected and a replacement handover submitted. Confirmed accountant snapshots remain immutable. HTTP 409 explains the restriction.

## Frontend integration

Use `movement.operationId`, not the movement/account/handover ID. Cashbox rows now include `operationRowVersion`; alternatively obtain the operation and its version from the account details or operations list. Existing saved accrual evidence populates cashbox `startDate` and `endDate`.

The Arabic RTL dialog should show:

| Control | Behavior |
|---|---|
| بداية احتساب العمولة | Editable date, initialized from the saved start date |
| نهاية احتساب العمولة | Read-only saved end date |
| صافي جاهز | Read-only net |
| سبب التصحيح | Required textarea |
| جهة المال / مرجع الدفع | Existing route/reference, editable when necessary |
| حفظ التصحيح | Submit once; disable while saving |

Hide the action for unsupported states or missing permissions. Disable it for cashbox rows with `cashboxHandoverId != null`; show the handover restriction. Let the server validate policy-based commission and use the returned amounts. Do not compute percentage commission in the browser.

```ts
const updated = await api.updateCommissionStart(operationId, {
  startDate: selectedDate,
  reason,
  rowVersion: movement.operationRowVersion!,
}, crypto.randomUUID());
```

Retain the exact request and key for a network retry. Use a new key after changing any input/version. Refresh the settlement, cashbox movements, balances, account details and ledger after success. On stale version (409), reload before resubmitting. Validation errors return 400, missing permissions 403, and missing operations 404.

## Accounting and audit

Ready settlements produce no money movement until execution. Execution uses the corrected start and the unchanged end.

Paid corrections keep the movement ID, original occurrence time and actor, account/rider ownership, and settlement execution identity. A zero correction retains the movement as amount 0 with direction None (`0`), so it contributes nothing to fund totals. A subsequent correction can reactivate that same movement. A previously zero settlement gets one movement if it becomes nonzero.

Original ledger charges and payments are append-only. The correction adds a zero status entry with the old/new calculation, dates, commission and remaining amounts, plus paired commission adjustment/payment differences when needed. The commission balance and next accrual date are preserved. Normal audit entries record changed operation/movement fields, and the Jahez dataset version invalidates caches. The original unpaid commission embedded in a paid settlement is retained during recalculation.

## Deployment and recovery

The migration adds one nullable date column and its upper-bound check, and permits zero-valued settlement movements only when direction is None and an operation is linked. Existing rows require no backfill. `commissionStartDate = null` preserves existing behavior.

Prepare/apply the migration through the normal deployment process. Roll forward to correct a mistaken date again. A database downgrade cannot restore the old positive-only movement check while corrected zero movements exist; preserve audit evidence and resolve those records through an explicit recovery plan before downgrading.
