# استرداد رسوم فتح حساب — Jahez

Use the current **handover/session ID**, not the platform account UUID. The server accepts one refund per active session, up to and including exactly 7 × 24 hours after its original `startedAtUtc`. Execution uses server time; refunds cannot be backdated. Completed orders do not prevent a refund. Existing platform debt and transferred debts remain separate obligations.

## Admin flow and checkbox

Show **استرداد رسوم فتح حساب** on the assigned account. Include a checkbox labelled **عدم احتساب العمولة**, initially unchecked:

- Unchecked (`waiveCommission: false`): deduct all outstanding calculated commission from the fee actually collected. For 200 collected and 2 commission days × 15, pay 170.
- Checked (`waiveCommission: true`): waive the outstanding calculated commission and pay the full collected fee, 200 in the example.

Re-preview when the checkbox changes. Show **الرسوم المحصلة**, **العمولة المستحقة**, **العمولة المخصومة**, **مبلغ الاسترداد**, **آخر موعد للاسترداد**, and any **رسوم غير محصلة ملغاة**. Use the server's amounts. Display the submit action as **استرداد الرسوم وسحب الحساب**.

There is no frontend application in this repository. These contracts and the TypeScript client provide the checkbox integration for the frontend application.

## Endpoints

| Method | Route under `/api/jahez/v2` | Result |
| --- | --- | --- |
| POST | `fees/refunds/preview` | Calculated refund and deadline |
| POST | `fees/refunds` | Saved immutable refund and completed withdrawal |
| GET | `handovers/{handoverId}/fee-refunds` | Saved refund history for that session (zero or one item) |

Preview body:

```json
{ "handoverId": "<session UUID>", "waiveCommission": false }
```

Execution body, with a unique `Idempotency-Key` header:

```json
{
  "handoverId": "<session UUID>",
  "waiveCommission": false,
  "reference": "REFUND-000123",
  "reason": "استرداد رسوم فتح الحساب",
  "dayUsage": { "ordersCount": 0 }
}
```

`reference` (maximum 500) and `reason` (maximum 2000) are required. `dayUsage` is optional; it follows the existing withdrawal-day allocation rules. Preview is informational: execution recalculates eligibility and amounts inside the account's write lock and serializable transaction. Reuse the same idempotency key and unchanged payload for a retry. A second key cannot issue another refund for that session; a new handover after reassignment starts a separate session.

Preview and execution require scoped `jahez.requests.fee.approve`. Execution additionally requires `jahez.withdraw` and `jahez.money.disburse`; waiving commission also requires `jahez.requests.commission.approve`. History requires `jahez.read`. All execution permissions are checked again on idempotent replays.

Business conflicts return 409: expired/closed/already-refunded session, no collected fees, inconsistent fee ledger, pending operation or approval request, missing commission evidence, or commission greater than/equal to collected fees when deduction is selected. Complete pending workflows first. If only 80 of the 200 fee was collected and commission is 30, refund 50 and cancel the uncollected 120; waiving commission refunds 80. Previously settled commission is not refunded or charged again.

## Cashbox and end-of-day reconciliation

Execution writes one cashbox disbursement (`fund: 1`, `direction: 2`, new `purpose: 5`, `AccountFeeRefund`) for the **net payout** and ends the rider assignment, marks the account Available, and closes its session in the same transaction. Commission stops accruing on withdrawal. The decision, original fee, commission calculation, actor, timestamp, and payment reference are retained.

The refund inherits the original fee's sponsor ownership even if the account sponsor has changed. It reduces the current supervisor fee cashbox, including below zero, and bypasses the fee account's usual receipt-only direction for this documented refund only. Later receipts can gradually replenish the negative balance. Ordinary disbursements retain their funding checks.

The existing V2 handover already uses signed cash totals. For treasury V3, preview/submission/confirmation now include refund-backed negative cash lines. **Do not clamp line amounts or cash counts to zero.** The accountant must acknowledge each signed line exactly. A -170 refund plus 500 other cash yields a net handover of 330. Confirmation transfers the signed balances to the corresponding central accounts, preserving sponsor attribution; a negative central balance is permitted for this refund deduction. The refund is not paid a second time. Confirmed links exclude it from subsequent handovers. A pending handover remains a frozen snapshot: later refunds appear in the next handover (cancel and resubmit if an earlier positive snapshot can no longer be funded).

Profit reporting reverses the **gross collected fee** on the refund date and counts only retained commission as commission profit. Across a 200 receipt and 170 refund, fee profit is 0 and commission profit is 30; a full refund produces 0 total profit. Receipt and refund on different days remain in their respective reporting periods.

## Deployment

The application migrations `SupportJahezPartialDebtRepayments` and `AddJahezAccountFeeRefunds` were [applied and verified on the hosted database on 24 October 2026](jahez-refunds-hosted-deployment-20261024.md). Both migration contexts now have zero pending changes. The updated API still needs deployment. For other databases, apply the migrations before the API build; the targeted idempotent refund script is [jahez-account-fee-refunds.sql](../database/scripts/jahez-account-fee-refunds.sql), for databases already migrated through `SupportJahezPartialDebtRepayments`. It adds `jahez_v2.AccountFeeRefunds`, foreign keys, one-refund-per-session/fee/movement unique indexes, and money constraints. No Identity migration or new permission grant is required.

## Verification

Solution build passed with zero warnings/errors. The Jahez suite passed 273 tests with 30 optional tests skipped; refund coverage includes 170/200 payouts, completed orders, the exact deadline, partial fee collection, previously settled commission, permissions on replay, duplicate commands, withdrawal, negative and positive net handovers, gradual cashbox replenishment, V2-to-V3 backfill, and cleanup after a late validation failure. TypeScript contracts/client passed strict checking; EF reports no pending model changes. Hosted schema, enabled/trusted constraint definitions, indexes, and preservation of existing records were verified during deployment. Disposable SQL-backed workflow tests were attempted but LocalDB fails during engine startup; application transaction rollback and concurrent refunds remain unverified against a running SQL test database.
