# جاهز: السداد الجزئي وتعديل عمولة المديونيات المرحلة

These endpoints act on a rider's **combined unpaid transferred debts** in **ديون المناديب غير المسددة**. Base path: `/api/jahez/v2`.

Both require `Authorization: Bearer <token>`, `Content-Type: application/json`, and `Idempotency-Key: <unique-key>`. They return HTTP 200 directly without a wrapper. All amounts are SAR with at most two decimal places; a single submitted amount must be between 0 and 1,000,000,000. A repayment must be greater than zero. A changed payload with an already-used key returns 409; retry the same key/body after a transport failure. All required permissions are checked before replay.

## 1. Partial payment

`POST /riders/{riderId}/transferred-debts/partial-repayment`

Permissions: `jahez.transferred-debt.repay` and `jahez.money.receive` in the Jahez platform scope.

```json
{
  "amount": 350,
  "expectedAmounts": { "dashboardDebt": 300, "commission": 200, "total": 500 },
  "fund": 1,
  "reference": "receipt-350",
  "reason": "سداد جزئي للمديونية المرحلة"
}
```

`expectedAmounts` is the latest rider summary's `amounts` object. A stale balance returns 409. The supported collection fund is Cashbox (1); this endpoint does not initiate any external payout or bank/platform transfer.

The payment covers **all outstanding dashboard balances first**, across the rider's unpaid debts, then commission. Each phase allocates oldest debts first (`createdAtUtc`, then ID). One receipt is added per affected debt, retaining its original account, rider, and sponsor. The receipts share the submitted reference and record their exact dashboard/commission allocations. Their sum is the submitted payment. Overpayments are rejected, rather than creating unrequested credit.

```json
{
  "riderProfileId": "<rider UUID>",
  "paidAmount": 350,
  "dashboardDebtPaid": 300,
  "commissionPaid": 50,
  "remainingAmounts": { "dashboardDebt": 0, "commission": 150, "total": 150 },
  "allocations": [
    {
      "debtId": "<debt UUID>",
      "movementId": "<receipt UUID>",
      "dashboardDebtPaid": 300,
      "commissionPaid": 50,
      "remainingAmount": 150,
      "isPaid": false
    }
  ]
}
```

The debt remains unpaid until its outstanding amount is zero. A later 150 SAR payment closes this example. Existing rider-wide and single-debt full-repayment endpoints now collect **only the remainder** and support debts with previous partial receipts.

## 2. Change the total commission

`PUT /riders/{riderId}/transferred-debts/commission`

Permissions: `jahez.settle` and `jahez.requests.adjustment.approve` in the Jahez platform scope.

```json
{
  "commission": 50,
  "expectedCommission": 200,
  "expectedAmounts": { "dashboardDebt": 300, "commission": 200, "total": 500 },
  "reason": "اعتماد تخفيض إجمالي عمولة المندوب"
}
```

`commission` replaces the **total charged commission on the currently unpaid debts**, including commission already collected against those debts. It is not an additional adjustment and is not the desired outstanding commission. Copy `expectedCommission` from the latest summary's `totalCommission`, and `expectedAmounts` from its `amounts`. Fully settled historical debts are outside this endpoint's scope.

For the example, the response is:

```json
{
  "riderProfileId": "<rider UUID>",
  "previousCommission": 200,
  "commission": 50,
  "remainingAmounts": { "dashboardDebt": 300, "commission": 50, "total": 350 },
  "adjustedDebtIds": ["<debt UUID>"]
}
```

Paying 350 SAR now clears the debt. If the rider already paid 350 against the original 500, submit the same new commission of 50, with `expectedCommission=200` and `expectedAmounts={dashboardDebt:0,commission:150,total:150}`; the remaining balance becomes zero without adding another receipt.

Commission reductions consume unpaid commission on newest debts first; increases apply to oldest debts with available amount capacity. The endpoint preserves amounts already collected and does not reduce commission below the collected or offset portion. Such a reduction returns 409 and needs a separate refund/credit workflow. A signed negative original dashboard net is a credit offset against commission; it is not a new amount due from the rider.

The source operations' saved `commission` and totals, debt totals, rider balances, and period profit report update together. Original dashboard net, calculated commission, accrual evidence, execution identity, assignments, and historical receipts are preserved. Old approval links on changed operations are cleared, while the approval records remain in history. Each change has an audit ledger event with the reason, actor, old/new amounts, paid amount, and previous approval link.

No cash movement or treasury posting is created by a commission adjustment. Prior receipts remain unchanged even when included in a pending or confirmed accountant handover. Reaching zero through an adjustment closes the debt; `paidAtUtc`/`paidByUserId` retain their existing API names and represent its closure, not a fabricated cash receipt.

## Read fields and frontend changes

- `GET /riders/transferred-debts`: `outstandingAmount` and `amounts` show **remaining** dashboard, commission, and total. New `totalCommission` is the charged commission across currently unpaid debts. New `paidAmount` sums receipts against those currently unpaid debts; it is not the rider's lifetime payment total. Both values leave the summary when their debt is fully settled. Missing historical source references keep the breakdown/total commission nullable and prevent these mutation endpoints from guessing amounts.
- Debt rows: `amount` is the current total obligation before receipts, `paidAmount` is total collected, `outstandingAmount = amount - paidAmount`, and `amounts` is the remaining breakdown. Use `outstandingAmount` when showing **غير المسدد**, rather than `amount`.
- Money movements: `dashboardDebtPaid` and `commissionPaid` show the saved allocation for new debt receipts. Both are null on legacy receipts and unrelated movements; no historical allocation is invented.
- Zero-balance debts disappear from unpaid rider/detail lists but stay in debt history.

Use `jahezClient(...).partiallyRepayRiderTransferredDebts(...)` and `.updateRiderDebtCommission(...)`. After success, refresh the rider summary, debt details, money movements, cashbox/treasury balances, and applicable profit reports. Refresh before a new command; its expected balance must match the current view. `reason` is nonblank and at most 2,000 characters; repayment `reference` is nonblank and at most 500.

## Errors and transaction behavior

400: invalid amount, precision, reason, reference, or fund. 403: required permission missing. 409: stale balance/commission, no unpaid debts, overpayment, inconsistent historical references, reducing already-collected commission, monetary capacity, or concurrent update.

Updates, receipts, ownership, treasury postings, audit, and command receipts share the existing serializable transaction. Ordered account locks and rowversions protect changes across the affected accounts. Rejected commands do not leave staged changes that a later save could persist.

## Deployment

Apply application migration `20261024115050_SupportJahezPartialDebtRepayments` **before** deploying the API build. The reviewed SQL is [jahez-partial-debt-repayments.sql](../database/scripts/jahez-partial-debt-repayments.sql). This changes the debt-receipt index to permit multiple receipts, adds `paidAmount`, and adds nullable receipt allocation fields. Existing paid debts are backfilled to remain closed. Inconsistent legacy receipts cause the migration to stop for review. Rolling back to the old schema is blocked when partial balances, zero-total adjustments, or multiple receipts cannot be represented safely.

The migration was [applied and verified on the hosted database on 24 October 2026](jahez-refunds-hosted-deployment-20261024.md), together with the account-fee refund migration. Both migration contexts have zero pending changes. The updated API still needs deployment. The main frontend is maintained outside this repository.

## Verification

Initial implementation verification: solution build with zero warnings/errors, 246 Jahez tests passed, 25 opt-in SQL/hosted cases skipped, strict TypeScript client checking passed, and EF reported no pending model changes. After refund integration, the Jahez suite passed 273 tests with 30 optional cases skipped. Hosted migration execution, schema/index/check definitions, and preservation of existing data were verified on 24 October 2026. The four existing hosted debts were unpaid and received zero `PaidAmount`; no hosted paid-debt rows were available to exercise the paid backfill. SQL concurrency and late-failure rollback workflow tests remain unverified because local SQL Server instances fail to start. API deployment was not performed.
