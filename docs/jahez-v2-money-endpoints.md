# Jahez V2 — full money and accountant handover endpoint contracts

Database migration `20261017165534_AddJahezV2AccountantHandovers` was applied and verified on hosted `db67927` on 17 October 2026 at 20:00 Riyadh time. The backend implementation and TypeScript client are ready. The API build still needs deployment; applying the database migration alone does not publish the routes.

Base path: `/api/jahez/v2`. IDs are UUID strings, dates are `YYYY-MM-DD`, timestamps are ISO 8601, rowversions are Base64, and JSON enum values are numeric.

The period profit totals endpoint, `/reports/profit`, is documented in [Jahez profit report](jahez-profit-report.md).

Rider performance by inclusive start/end dates is available through `/reports/riders`; see [the date-range performance report](jahez-rider-performance-report.md) for filters, daily counts, and account breakdowns.

To correct **صافي المبلغ المُدخل للتصفية** on executed settlements in Cashbox, Bank, or PetroApp, see [settlement net correction](jahez-settlement-net-correction.md): `PUT /operations/{id}/net`.

For **ديون المناديب غير المسددة**, see [partial repayment and rider commission adjustment](jahez-rider-debt-partial-payments.md).

Every request needs `Authorization: Bearer <access-token>`. Every POST additionally needs `Content-Type: application/json` and `Idempotency-Key: <unique-command-key>`. Keep the same key and body when retrying the same command; keys are scoped by actor and operation. A changed payload with an already-used key returns 409. Keys must contain 1–150 characters. Reasons must be nonblank and at most 2,000 characters.

All seven endpoints below return HTTP 200 on success. Responses are returned directly, with no `data` or `success` wrapper. Permissions are enforced in the Jahez client-platform scope.

| Method | Endpoint | Permission | Response |
|---|---|---|---|
| GET | `/funds` | `jahez.cashbox.read` | `FundBalance[]` |
| GET | `/money-movements?page=1&pageSize=50&fromDate=2026-10-01&toDate=2026-10-17` | `jahez.cashbox.read` | `Page<MoneyMovement>` |
| GET | `/cashbox/handovers?page=1&pageSize=50` | `jahez.cashbox.read` | `Page<CashboxHandover>` |
| GET | `/cashbox/handovers/{id}` | `jahez.cashbox.read` | `CashboxHandover` |
| POST | `/cashbox/handovers` | `jahez.cashbox.submit` | `CashboxHandover` with status 1 |
| POST | `/cashbox/handovers/{id}/confirm` | `jahez.cashbox.confirm` | `CashboxHandover` with status 2 |
| POST | `/cashbox/handovers/{id}/cancel` | `jahez.cashbox.submit` | `CashboxHandover` with status 3 |

Pagination defaults to `page=1`, `pageSize=50`. Valid pages are 1–100000 and valid sizes are 1–100. The page response has `items`, `page`, and `pageSize`; there is no `totalCount` for these lists.

## 1. Outstanding funds

`GET /funds` returns all three funds. Each field includes only movements outside confirmed accountant handovers. Pending and cancelled handovers still count as outstanding. Receipts minus disbursements equals balance. There is no date filter on this endpoint: outstanding funds carry across days and months until confirmation.

Example after every outstanding movement was confirmed:

```json
[
  { "fund": 1, "receipts": 0, "disbursements": 0, "balance": 0 },
  { "fund": 2, "receipts": 0, "disbursements": 0, "balance": 0 },
  { "fund": 3, "receipts": 0, "disbursements": 0, "balance": 0 }
]
```

Use this endpoint for balance cards. Midnight does not reset balances. Confirmation clears only the submitted movement set; later payments can keep the refreshed balances nonzero.

## 2. Movement history

`GET /money-movements` returns the full history within the selected dates, including already-confirmed movements. Filtering uses `occurredAtUtc`, both dates are inclusive in Riyadh time, and filtering runs before pagination. Each omitted bound defaults independently: `fromDate` to the first day of the current month, `toDate` to today. A reversed range returns 400. Rows are ordered by `occurredAtUtc` descending, then ID.

Each movement also exposes `paymentReference`: the saved receipt number or actual transfer reference entered in **رقم إيصال القبض أو الصرف أو مرجع التحويل الفعلي**. It mirrors the existing `reference` value when present and returns `null` for blank legacy references. Use it to display or prefill the Cashbox reference field. It is available for settlement, fee, and transferred-debt receipt rows. No validation-message text is stored or generated as a reference.

```json
{ "reference": "RCPT-350", "paymentReference": "RCPT-350" }
```

`userRider` is the recorded movement rider, including after account reassignment. `ownerRider` is the account's registered owner. Missing persons and iqamas are nullable. `sponsorNameAr` is the account sponsor's Arabic registry name. `cashboxHandoverId` identifies the pending/confirmed accountant handover, or is null if none. `isHandedToAccountant` becomes true only after confirmation. Cancelled handovers release their movements while retaining their audit evidence.

For the Cashbox page, each movement also includes `accountId` (the internal account UUID from its historical handover) and `externalAccountId` (the Jahez account number). `net` is the signed net amount saved on the linked settlement, as entered by the admin or subsequently approved. `commission` is the company commission applied to that settlement; `calculatedCommission` is the calculated commission before any approved override. These values come from the recorded operation, without recalculating today's commission. Financial fields are null when there is no linked operation, such as a standalone account-fee receipt or transferred-debt repayment. A missing handover makes `accountId` null; a missing account makes `externalAccountId` null. `amount` remains the actual money received or disbursed, which differs from the settlement net.

When `purpose` is `1` (Settlement / تصفية), `startDate` and `endDate` return the inclusive commission accrual period saved with the operation, in `YYYY-MM-DD` format. They describe the dates used to calculate **عمولة الشركة**, including daily and percentage segments. Later settlements, baseline changes, and approved commission amount overrides do not change these recorded dates. Both fields are null for other purposes, movements without a linked operation, or calculations without recorded accrual dates. These response fields are separate from the `fromDate`/`toDate` history filters.

Example settlement fields:

```json
{ "purpose": 1, "startDate": "2026-10-02", "endDate": "2026-10-06", "commission": 75 }
```

## 3. Submit funds to the accountant

`POST /cashbox/handovers`

```json
{ "businessDate": "2026-10-17", "reason": "تسليم الأموال للمحاسب" }
```

The service captures every outstanding movement before the end of that Riyadh business date, including older outstanding days, and stores the exact membership and signed net amount of each fund. It creates a pending handover. It does not clear balances, create a financial payment, or modify rider liabilities.

The date cannot be in the future. Only one company-wide pending handover is allowed. No eligible movements or an existing pending handover returns 409. A zero net amount is valid when offsetting movements exist. Negative amounts represent net outflows to reconcile; acknowledging Bank/PetroApp totals does not initiate an external transfer.

## 4. Confirm receipt/reconciliation as accountant

`POST /cashbox/handovers/{id}/confirm`

```json
{
  "cashboxAmount": 50,
  "petroAppAmount": 75,
  "bankAmount": -20,
  "reason": "تم الاستلام والمطابقة",
  "rowVersion": "<Base64 rowVersion returned by the pending handover>"
}
```

The example amounts are illustrative. Send the exact three amounts from the pending handover. The accountant must be a different user from the submitter. Each fund must match independently; an equal grand total with different per-fund amounts returns 400. A stale rowversion or non-pending handover returns 409. Self-confirmation returns 403.

Confirmation sets status 2, `accountantUserId`, `confirmedAtUtc`, and `confirmationReason`, and clears the captured movements from `/funds`. No third approval step is required. Full movement history remains available. New transactions after submission remain outstanding, even if their timestamps fall inside the submitted business day.

## 5. Cancel a pending handover

`POST /cashbox/handovers/{id}/cancel`

```json
{
  "reason": "إلغاء وإعادة المطابقة",
  "rowVersion": "<Base64 rowVersion returned by the pending handover>"
}
```

Only the submitting user can cancel their pending handover. Cancellation sets status 3, `cancelledAtUtc`, and `cancellationReason`; it releases movements for submission again. It does not reduce balances. Confirmed/cancelled snapshots cannot be edited, cancelled again with a new command, or deleted. Replaying the original successful cancellation with its original key is idempotent.

## 6. Read handovers

`GET /cashbox/handovers?page=1&pageSize=50` returns `Page<CashboxHandover>`, including pending, confirmed, and cancelled records, ordered by creation timestamp descending, then ID.

`GET /cashbox/handovers/{id}` returns one `CashboxHandover`, or 404 if not found. The body shape is also used by all three POST endpoints.

## Complete TypeScript response contracts

```ts
export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
}

export enum Fund { Cashbox = 1, PetroApp = 2, Bank = 3 }
export enum Direction { None = 0, Receipt = 1, Disbursement = 2 }
export enum Purpose {
  Settlement = 1, Fuel = 2, AccountFee = 3, TransferredDebtRepayment = 4
}
export enum CashboxHandoverStatus { Pending = 1, Confirmed = 2, Cancelled = 3 }

export interface FundBalance {
  fund: Fund;
  receipts: number;
  disbursements: number;
  balance: number;
}

export interface RiderPerson {
  employeeId: string;
  riderProfileId: string | null;
  nameAr: string;
  nameEn: string | null;
  iqamaNo: string | null;
  mobile: string | null;
  sponsorId: string | null;
}

export interface MoneyMovement {
  id: string;
  handoverId: string;
  riderProfileId: string;
  operationId: string | null;
  transferredDebtId: string | null;
  fund: Fund;
  direction: Direction;
  purpose: Purpose;
  amount: number;
  actorUserId: string;
  occurredAtUtc: string;
  reference: string;
  reason: string;
  createdAtUtc: string;
  createdByUserId: string | null;
  userRider: RiderPerson | null;
  ownerRider: RiderPerson | null;
  sponsorNameAr: string | null;
  cashboxHandoverId: string | null;
  isHandedToAccountant: boolean;
  accountId: string | null;
  platformAccountId: string | null; // Jahez account number, e.g. "44120"; same as externalAccountId.
  externalAccountId: string | null;
  net: number | null;
  commission: number | null;
  calculatedCommission: number | null;
  startDate: string | null; // YYYY-MM-DD, inclusive; Settlement purpose only.
  endDate: string | null;   // YYYY-MM-DD, inclusive; Settlement purpose only.
}

export interface CashboxHandover {
  id: string;
  businessDate: string;
  status: CashboxHandoverStatus;
  requestedByUserId: string;
  cashboxAmount: number;
  petroAppAmount: number;
  bankAmount: number;
  movementCount: number;
  reason: string;
  accountantUserId: string | null;
  confirmedAtUtc: string | null;
  confirmationReason: string | null;
  cancelledAtUtc: string | null;
  cancellationReason: string | null;
  createdAtUtc: string;
  createdByUserId: string | null;
  updatedAtUtc: string | null;
  updatedByUserId: string | null;
  rowVersion: string;
  isDeleted: boolean;
  deletedAtUtc: string | null;
  deletedByUserId: string | null;
  deletionReason: string | null;
}

export interface CashboxHandoverCreate {
  businessDate: string;
  reason: string;
}

export interface CashboxHandoverConfirm {
  cashboxAmount: number;
  petroAppAmount: number;
  bankAmount: number;
  reason: string;
  rowVersion: string;
}

export interface CashboxHandoverCancel {
  reason: string;
  rowVersion: string;
}

export interface MoneyMovementDateRange {
  fromDate?: string | null;
  toDate?: string | null;
}
```

The compiled shared contract uses `MovementRow` for the `MoneyMovement` shape above and `Person` for `RiderPerson`. Import the shared definitions from [jahez-v2-api-contracts.ts](jahez-v2-api-contracts.ts) to avoid maintaining duplicate types.

## Complete frontend calls

The tested [jahez-v2-client.ts](jahez-v2-client.ts) implements all calls below. `baseUrl` is the API origin, without `/api/jahez/v2`. Token providers should return current tokens after any refresh. Keep command keys stable across retries.

```ts
import { jahezClient } from './jahez-v2-client';

const cashierApi = jahezClient(baseUrl, getCashierAccessToken);
const accountantApi = jahezClient(baseUrl, getAccountantAccessToken);

const funds = await cashierApi.funds();
const currentMonth = await cashierApi.movements(1, 50);
const historical = await cashierApi.movements(1, 50, {
  fromDate: '2026-10-01', toDate: '2026-10-17'
});
const history = await cashierApi.cashboxHandovers(1, 50);

const submitKey = crypto.randomUUID(); // Save and reuse this key if retrying.
const pending = await cashierApi.submitCashboxHandover({
  businessDate: '2026-10-17', reason: 'تسليم الأموال للمحاسب'
}, submitKey);

const details = await accountantApi.cashboxHandover(pending.id);
// Display the amounts for the accountant to verify before submitting confirmation.
const confirmKey = crypto.randomUUID();
const confirmed = await accountantApi.confirmCashboxHandover(details.id, {
  cashboxAmount: details.cashboxAmount,
  petroAppAmount: details.petroAppAmount,
  bankAmount: details.bankAmount,
  reason: 'تم الاستلام والمطابقة',
  rowVersion: details.rowVersion
}, confirmKey);

// After confirmation, refresh outstanding cards, movement statuses, and handover history.
const [updatedFunds, updatedMovements, updatedHistory] = await Promise.all([
  cashierApi.funds(), cashierApi.movements(1, 50), cashierApi.cashboxHandovers(1, 50)
]);
```

Cancellation is an alternative to confirmation. As the submitting user, cancel a record that is still pending:

```ts
const cancelKey = crypto.randomUUID();
const cancelled = await cashierApi.cancelCashboxHandover(pending.id, {
  reason: 'إلغاء وإعادة المطابقة', rowVersion: pending.rowVersion
}, cancelKey);
```

## Errors and integration rules

| HTTP status | Meaning |
|---|---|
| 400 | Invalid body/date/pagination/reason/key, reversed movement range, or per-fund confirmation mismatch |
| 401 | Missing/invalid login token |
| 403 | Missing platform-scoped permission, confirming your own submission, or cancelling another user's submission |
| 404 | Handover does not exist |
| 409 | Pending handover exists, no eligible movements, stale rowversion, already-decided record, conflicting command key, or a locked payment-method correction |

Business errors include `errorCode` such as `jahez.invalid_request`, `jahez.forbidden`, `jahez.not_found`, or `jahez.conflict`, with ProblemDetails fields `type`, `title`, `status`, `detail`, `instance`, and `correlationId`. Framework authentication/model-binding errors can use the existing API-level error codes. Refresh a record after a 409 instead of silently replacing its rowversion.

Movement payment-method corrections are blocked while their accountant handover is pending or confirmed. A submitter can cancel a pending handover, correct the payment method, then submit a replacement. A confirmed handover and its financial membership remain immutable.

## Backend implementation and verification

- HTTP bindings and authorization: [JahezWorkflowController.cs](../src/LogisticsERP.Api/Controllers/JahezWorkflowController.cs).
- Submission, confirmation, cancellation, reads, and outstanding movement selection: [JahezWorkflowService.Cashbox.cs](../src/LogisticsERP.Infrastructure/Jahez/JahezWorkflowService.Cashbox.cs).
- Movement status projections and outstanding fund aggregation: [JahezWorkflowService.WorkflowQueries.cs](../src/LogisticsERP.Infrastructure/Jahez/JahezWorkflowService.WorkflowQueries.cs).
- Payment-method locks: [JahezWorkflowService.PaymentMethod.cs](../src/LogisticsERP.Infrastructure/Jahez/JahezWorkflowService.PaymentMethod.cs).
- Complete executable request examples: [Postman collection](jahez-v2.postman_collection.json).
- Applied database migration evidence: [report.json](../outputs/jahez-accountant-handovers-migration-20261017T170033907Z/report.json).

Mutations use the existing service-owned serializable transaction, idempotency receipts, rowversions, audit entries, restricted foreign keys, immutable snapshots, and a unique single-pending index. Confirmation changes outstanding aggregation without deleting or zeroing journal rows. The local Jahez suite passed 104 tests; five opt-in database cases were skipped in that run. The hosted read-only integration test passed after applying this migration. Transactional handover writes and concurrent confirmations were not exercised on the hosted database.
## استرداد رسوم فتح حساب

See [the refund endpoint and checkbox handoff](jahez-account-fee-refunds.md) for refunds within seven days, optional commission waiver, account withdrawal, negative cashbox balances, and signed end-of-day deductions.
