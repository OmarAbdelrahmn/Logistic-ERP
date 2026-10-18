# Frontend handoff: تعديل طريقة دفع التصفية — جاهز

Date: 17 October 2026. Backend implemented locally; deployment of this version is required before frontend integration against a hosted API. This repository contains the backend and frontend contracts.

Add **تعديل طريقة الدفع** to the existing Jahez settlement row actions and settlement details. It lets an authorized user correct an executed payout between **الخزنة** and **الحساب البنكي**. The payout amount and settlement calculation stay the same; fund totals reflect the corrected method.

## API contract

```http
PUT /api/jahez/v2/operations/{operationId}/payment-method
Authorization: Bearer <access-token>
Content-Type: application/json
Idempotency-Key: <unique-command-key>
```

Use the settlement operation's `id` as `operationId`. Account, handover and money movement IDs are different identifiers.

Cash → bank:

```json
{
  "fund": 3,
  "paymentReference": "BANK-TRANSFER-123",
  "reason": "تصحيح طريقة صرف التصفية إلى الحساب البنكي",
  "rowVersion": "<latest operation.rowVersion>"
}
```

Bank → cash uses the same endpoint with `fund: 1`, the appropriate payment reference, a reason and the latest `rowVersion`.

| Field | Type | Required | Validation / source |
|---|---|---|---|
| `fund` | number | Yes | `1` = الخزنة; `3` = الحساب البنكي |
| `paymentReference` | string | Yes | Nonblank, maximum 500 characters |
| `reason` | string | Yes | Nonblank, maximum 2000 characters |
| `rowVersion` | string | Yes | Latest operation's Base64 version, copied unchanged |

Send enum values as numbers. `PetroApp=2` is rejected. The endpoint selects the Bank fund; it has no `bankAccountId`, bank account selector or IBAN field.

## Action visibility and permissions

Both permissions are required within the Jahez platform scope:

- `jahez.settle`
- `jahez.money.disburse`

Show the action only when the user has both permissions and the operation satisfies:

```ts
import type { Operation } from './jahez-v2-api-contracts';

function canEditPaymentMethod(
  operation: Operation,
  canSettle: boolean,
  canDisburse: boolean,
): boolean {
  return canSettle && canDisburse
    && operation.status === 3             // Executed
    && operation.paymentStatus === 2      // Paid
    && operation.remaining < 0            // Money paid to rider
    && operation.transferToRider === false
    && (operation.fund === 1 || operation.fund === 3);
}
```

These permissions are supplied by the ERP's existing permission system. The backend independently enforces them, including on idempotency replay. Reading settlements requires `jahez.read`; reading money movements and fund balances requires `jahez.cashbox.read`.

Ready drafts continue to use `PUT /operations/{id}`. This action is unavailable for pending review, receipts from riders, zero settlements, transferred obligations and withdrawal without settlement.

## Edit dialog

Use the existing Arabic RTL dialog and form components.

| Element | Arabic copy / behavior |
|---|---|
| Action and dialog title | تعديل طريقة الدفع |
| Settlement context | Show existing account/rider identity and settlement date |
| Read-only payout | المبلغ المصروف — display `Math.abs(operation.remaining)` in SAR |
| Current method | طريقة الدفع الحالية |
| Method select | طريقة الدفع — الخزنة (`1`), الحساب البنكي (`3`) |
| Reference input | مرجع الدفع — initialize from `operation.paymentReference` |
| Reason textarea | سبب التعديل — initially empty, required |
| Explanatory text | يتم تحديث طريقة تسجيل الصرف مع الاحتفاظ بسجل التعديل. |
| Save button | حفظ التعديل |
| Loading button | جارٍ الحفظ… |
| Cancel button | إلغاء |
| Success toast | تم تحديث طريقة دفع التصفية بنجاح |

Initialize the method with `operation.fund`. Require a method/reference change before enabling Save, along with valid reference and reason. The backend also permits reference-only corrections. Identical method/reference values are a successful no-op after validation.

This form corrects the recorded payout method. Saving does not initiate a bank transfer. Submit only the four request fields above.

## Submission and retry behavior

1. Load the current operation and retain its `rowVersion` with the form.
2. Validate the form. Generate a command key once, for example `crypto.randomUUID()`.
3. Freeze the submitted request and key; disable duplicate submissions while pending.
4. Send the PUT. Use the existing token refresh/authentication flow.
5. On HTTP 200, apply the returned operation and its new `rowVersion`, close the dialog and show the success toast.
6. Refresh affected settlement lists/details, the account's operations and ledger, money movements, and fund balances where available to the current user.

Use the same key and exact body if the response is lost, the connection times out, or the same command is retried after authentication refresh. Editing any request field, including `rowVersion`, creates a new command and needs a new key. Reusing a key with different data returns 409. A successful replay returns the saved response; invalidate/refetch queries so later changes are reflected too.

The backend updates the existing payout movement, rather than adding a second payout. Fund totals are recomputed from those records. Use server responses for amounts and balances.

## Success response and existing client

HTTP **200** returns the complete `Operation` directly, without a `data` wrapper. Its `fund` and `paymentReference` contain the saved values, and `rowVersion` is the version for the next edit. Amounts, commission, execution time/user, withdrawal state and original settlement reason are preserved. The correction reason is saved separately in the audit history and a zero-amount status ledger entry.

The request type and fetch helper are already included in:

- [jahez-v2-api-contracts.ts](jahez-v2-api-contracts.ts): `OperationPaymentMethodUpdate` and `Operation`.
- [jahez-v2-client.ts](jahez-v2-client.ts): `updatePaymentMethod(id, value, key)`.
- [jahez-v2.postman_collection.json](jahez-v2.postman_collection.json): cash-to-bank request example.

```ts
import { jahezClient } from './jahez-v2-client';
import type { OperationPaymentMethodUpdate } from './jahez-v2-api-contracts';

// Supply the existing ERP API base URL and access-token provider.
const api = jahezClient(baseUrl, getAccessToken);
const body: OperationPaymentMethodUpdate = {
  fund: 3,
  paymentReference: 'BANK-TRANSFER-123',
  reason: 'تصحيح طريقة صرف التصفية إلى الحساب البنكي',
  rowVersion: operation.rowVersion,
};
const commandKey = crypto.randomUUID();
const updatedOperation = await api.updatePaymentMethod(operation.id, body, commandKey);
// Retain body + commandKey for retry until this command has a known outcome.
```

## Errors

Business errors use ProblemDetails with `status`, `detail`, `errorCode` and `correlationId`. Display `detail` and keep the user's input when it remains applicable. Standard request-binding errors can also include field validation details; use the ERP's existing validation handler.

| HTTP | Situation | Frontend behavior |
|---|---|---|
| 400 | Missing/blank/oversized reference or reason, unsupported fund, invalid request | Display validation feedback; correct fields and submit with a new key |
| 401 | Authentication expired or missing | Follow existing authentication flow |
| 403 | Missing settlement or disbursement permission | Show the server message; refresh permission state |
| 404 | Operation not found | Refresh the list and dismiss the unavailable row |
| 409 | Stale version, ineligible operation, inconsistent linked movement, or key reused with different data | Display the server message; reload current operation before allowing a reviewed resubmission |
| Network / 5xx | Command outcome may be unknown | Preserve request and key for retry; do not show a success toast |

For `تغيرت التصفية؛ أعد تحميلها.`, reload the current operation and let the user review the latest method/reference before submitting a fresh command. For `حركة صرف التصفية غير متسقة؛ راجع السجل المالي.`, display the message for financial review; do not automatically repeat the edit.

## Acceptance checks

- An eligible cash payout can switch to bank, and a bank payout can switch to cash.
- Only users with both required permissions see the action.
- Reference and reason validation runs before submission.
- The amount remains unchanged and the new method appears in both settlement and movement views.
- Cash/bank totals refresh after success; a correction is visible in the account ledger.
- Repeating the same command does not duplicate payouts or correction history.
- A stale `rowVersion` shows the server conflict and reloads current data.
- A failed or interrupted request preserves form state and the original retry key/body.
- A second edit uses the latest returned/refetched `rowVersion` and a new key.
