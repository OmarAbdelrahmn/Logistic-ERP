# تعديل صافي المبلغ المُدخل للتصفية — جاهز

`PUT /api/jahez/v2/operations/{id}/net`

Changes the entered settlement `net` for **all three settlement funds**: Cashbox (1), PetroApp (2), and Bank (3). The previous cashbox-only restriction has been removed from the endpoint and persistence policy. Eligibility requires an executed Settle / SettleAndWithdraw operation with `purpose=1` (Settlement), no transferred rider debt, and a consistent money movement outside any pending or confirmed accountant handover. A cancelled handover releases the settlement for correction. This check covers both V2 movement membership and V3 treasury posting membership.

## Request

Headers: `Authorization: Bearer <access-token>`, `Content-Type: application/json`, `Idempotency-Key: <unique-command-key>`.

```json
{
  "net": 125.50,
  "reason": "تصحيح صافي المبلغ المُدخل للتصفية",
  "rowVersion": "<latest operation rowVersion>"
}
```

- `{id}` is the operation UUID (`operationId` on a movement), rather than the movement ID.
- `net` is a signed SAR amount within ±1,000,000,000, rounded to two decimal places, with midpoint rounding away from zero. The resulting settlement total must also stay within that range.
- `reason` is required, nonblank, and at most 2,000 characters.
- `rowVersion` is the latest Base64 version from the operation or movement's `operationRowVersion`.
- Optional `fund` and `paymentReference` replace the recorded routing/reference. Omitted or null values preserve the existing values. Existing three-field requests and their idempotency hashes remain compatible.
- Requires all four permissions in the Jahez client-platform scope: `jahez.settle`, `jahez.requests.adjustment.approve`, `jahez.money.receive`, `jahez.money.disburse`. Permissions are rechecked before an idempotent replay.

Receipts support Cashbox/PetroApp; payouts support Cashbox/Bank. If a net change switches the direction and the old fund cannot support it, supply a valid `fund` (and a new `paymentReference` when appropriate). For example, a PetroApp receipt changed to a payout can send `fund=1` or `fund=3`. A Bank payout changed to a receipt can send `fund=1` or `fund=2`. Unsupported routing returns 400 without writes. Zero results retain the movement and selected fund, and can later be reactivated.

## Recalculation

The endpoint returns HTTP 200 with the updated `Operation` directly, without a wrapper.

| Field | Updated value |
|---|---|
| Operation `net` | Rounded new input |
| Operation `remaining` | `net + commission` |
| Operation `paymentStatus` | `Paid` (2) for a nonzero result; `None` (1) for zero |
| Movement `amount` | Absolute value of `remaining` |
| Movement `direction` | `Receipt` (1) when positive, `Disbursement` (2) when negative, `None` (0) when zero |
| Operation/movement fund and payment reference | Existing values, unless replacements are supplied |
| Fund / treasury balances | Recomputed from the corrected movement and synchronized treasury postings |

Example with saved commission of 75 SAR: changing net from 100 to 125.50 changes remaining and cash received from 175 to 200.50. Changing net to -125 changes the result to a 50 SAR payout. Changing net to -75 produces zero; the existing cash movement is retained with zero amount and can be corrected again later through this endpoint.

Commission comes from the saved settlement calculation, separately from entered net. Both `commission` and `calculatedCommission`, including any approved commission override, retain their saved values. The commission period, calculation evidence, original execution user/time, movement identity, and account/rider assignment history are retained. Net correction clears the operation's old `approvedRequestId` because that approval described the previous settlement values; the approved request and its decisions remain in history.

A zero-value audit ledger event records the correction reason, user, old/new net, remaining, amount, direction, and previous approval link. No extra commission charge or payment is posted. V3 synchronizes the cash change using reversal/replacement postings, preserving the historical treasury journal and original sponsor ownership. Treasury funding/reservation checks still apply: a correction requiring unavailable funds returns 409 and rolls back. Operation, movement, treasury changes, audit, and idempotency receipt share one transaction.

The Jahez profit report keeps the same commission and opening-fee totals when only net changes.

## Frontend

Use `jahezClient(...).updateNet(operationId, { net, reason, rowVersion, fund?, paymentReference? }, key)` from `jahez-v2-client.ts`. Remove the frontend `fund === Cashbox` eligibility check and the disabled tooltip **تعديل الصافي متاح لحركات الخزنة النقدية فقط**. Display the action for eligible settlements in every fund. The main frontend is outside this repository, so its disabled state must be updated there in addition to deploying the API. `isHandedToAccountant=false` alone is not enough to prove eligibility because pending accountant handovers also lock the snapshot; handle 409 by refreshing the row and handover state.

After success, replace the operation from the response and refresh `/money-movements`, `/funds`, and relevant V3 balances, journal, reports, and handover preview. Use the returned operation `rowVersion` for the next change.

Retry the exact same key and body after a transport failure. A successful replay returns its original response; a changed body with the same key returns 409. An identical rounded net with a fresh key is a no-op after eligibility/version checks.

## Errors and scope

| HTTP | Meaning |
|---|---|
| 400 | Invalid amount/reason/reference, unsupported fund/direction, or resulting settlement total outside limits |
| 403 | One of the required permissions is missing |
| 404 | Operation or required Jahez account/session is missing |
| 409 | Stale version, ineligible status/fund/purpose, transferred settlement, inconsistent movement, active accountant handover, insufficient available treasury funds, concurrent write, or changed idempotency payload |

Ready / UnderReview operations, standalone withdrawals, fuel-purpose operations, transferred rider debts, and original zero settlements without a money movement are ineligible. Fee receipts and debt repayments do not have their own settlement net to edit through this endpoint. For Ready operations, continue using the existing `PUT /operations/{id}` workflow. Cancel a pending accountant handover before correcting its settlement, then submit a replacement; confirmed handovers remain fixed.

No database migration is required. Deploy the API build to expose this endpoint remotely.
