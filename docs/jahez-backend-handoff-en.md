# Jahez backend: complete implementation and API handoff

**Scope:** Backend implementation in Logistics ERP, current as of 4 October 2026. This document is self-contained for backend operators and frontend/API integrators. The Jahez module manages account handovers, rider fees, commission, imported platform debt, settlements, approvals, a two-section cashbox, daily accountant handoffs, dispatch counts, and overdue reminders. It uses the existing platform-account, rider, assignment, authorization, and notification infrastructure. It does not call Jahez's external API or provide a frontend.

## 1. Core identities and prerequisites

| Concept | Meaning |
|---|---|
| Registered account owner | `RegisteredEmployeeId` or `OwnerRiderProfileId` on the platform account. This person may differ from the rider using it. |
| Actual rider | `riderProfileId` on a Jahez handover. Transactions, dispatches, fees, and commission for that usage period belong to this rider. |
| Employee sponsor | Existing HR relationship; separate from the platform account sponsor. |
| Account sponsor | Existing `sponsorId` on the platform account. |

The platform must have `ClientPlatform.Code = "JAHEZ"`. Create an account through the platform-account API first, with `paymentModel: "PayPerOrder"`, `status: "Available"`, and a valid `sponsorId`; then call `POST /api/jahez/handovers`. The existing platform account sponsor also supplies Jahez's sponsor relationship. There is no separate dashboard sponsor. Account creation and updates require an active account sponsor.

An active rider can hold two Jahez accounts in the existing two assignment slots. General account-assignment and release endpoints reject Jahez accounts so that the financial handover and close rules cannot be bypassed. Once a Jahez account has financial history, its platform and external account ID cannot be changed. The account owner and actual rider must appear separately in UI and reports.

A new Jahez handover creates an underlying rider/client assignment and links it one-to-one with the Jahez handover. If the Jahez platform has no active client contract, the service creates an active `JAHEZ-OPERATIONS` contract for this purpose. Use `sponsorId` in account-list filters and `SponsorId` in the account CSV export.

## 2. API conventions

JSON responses also include related account details, registered-owner and historical actual-rider IDs/names, and name companions for user IDs. These additions apply to paged items, nested approval decisions/import rows, and POST responses. The field tables and examples below describe the original fields; add the shared display fields in [Jahez response names](jahez-response-names.md).

- Base path: `/api/jahez`. All endpoints require `Authorization: Bearer <access_token>`. Successful Jahez calls return HTTP 200 with the response object directly, except the original-file download, which returns XLSX bytes.
- Every Jahez **POST** requires `Idempotency-Key: <unique operation key>` (nonempty, maximum 150 characters). Reusing the same key with the same actor, operation, and payload returns the prior result; reusing it for different payload data returns 409. Current authorization is checked again on replay. Preserve the key when retrying the same request after a network failure.
- JSON uses `camelCase`; enums are integer values. `DateOnly` uses `yyyy-MM-dd`. Instants use ISO 8601 with an offset; the server stores UTC. Business days are interpreted in Riyadh time (UTC+03:00). Amounts are SAR. Collections are rounded to halalas; imported source and ledger values support six decimal places. Change commands require a reason, at most 2,000 characters.
- Paged results are `{ "items": [...], "page": 1, "pageSize": 50 }`. Defaults are page 1 and size 50; size cannot exceed 100. There is no `totalCount` field.
- JSON POSTs use `Content-Type: application/json`. `POST /imports` uses `multipart/form-data` and a repeated field named `files`.
- Typical failures: 400 invalid input, 401 unauthenticated, 403 missing permission or separation-of-duties failure, 404 unknown record, 409 state/period/idempotency conflict, 413 request too large. Errors use the application's ProblemDetails shape with `type`, `title`, `status`, `detail`, `instance`, `errorCode`, and `correlationId`. For a service conflict, `errorCode` is usually `jahez.conflict`; model-binding errors can use `api.invalid_request`.

Example conflict response:

```json
{"type":"https://httpstatuses.io/409","title":"Data conflict","status":409,"detail":"The requested period overlaps a posted period.","instance":"/api/jahez/settlements","errorCode":"jahez.conflict","correlationId":"illustrative-correlation-id"}
```

| Integer enum | Values |
|---|---|
| Approval `kind` | 1 fee exception; 2 free switch; 3 percentage commission; 4 account reset/debt transfer |
| Approval `status` | 1 pending; 2 approved; 3 rejected; 4 cancelled |
| Ledger `bucket` | 1 account fee; 2 Jahez platform debt; 3 commission |
| Ledger `kind` | 1 charge; 2 payment; 3 adjustment; 4 transfer; 5 opening balance |
| Cashbox `section` | 1 account fees; 2 settlements |
| Cashbox handoff `status` | 1 pending; 2 accountant confirmed; 3 approved; 4 rejected |
| Import `kind` | 1 transactions; 2 daily dispatches |

### Permissions

| Permission | Purpose |
|---|---|
| `jahez.read` | Handovers, balances, debts, ledger, requests, dispatch reports |
| `jahez.handovers.manage` | New handovers and normal close |
| `jahez.collections.manage` | Rider collections and settlements |
| `jahez.requests.create` | Create/cancel approval requests |
| `jahez.requests.approve` | Decide fee, switch, and percentage requests |
| `jahez.resets.approve` | Decide reset requests |
| `jahez.earnings.manage` | Manual percentage earnings |
| `jahez.imports.manage` | Upload, preview, commit, and download imports |
| `jahez.adjustments.manage` | Ledger adjustments and legacy adoption |
| `jahez.cashbox.read` | Cashbox balances and history |
| `jahez.cashbox.submit` | Start accountant handoff |
| `jahez.cashbox.confirm` | Confirm receipt as accountant |
| `jahez.cashbox.approve` | Final cashbox decision |

Permissions are checked in the Jahez `ClientPlatform` scope, including inside the service. The approval decision controller first requires `jahez.read`; the service additionally requires `jahez.requests.approve` or, for reset, `jahez.resets.approve`. The decision maker cannot be the request creator. A cashbox submission, accountant confirmation, and final decision require three different users. The Identity migration grants all 13 permissions to `SYSTEM_ADMIN`; other roles need explicit grants.

## 3. Financial and operational rules

### Account handover and fee

A normal handover creates a SAR **200** fee for the actual rider. `initialFeePayment` may be zero, part, or all of the charge. The unpaid amount is a receivable, not cashbox money. Each later collection names how much goes toward fees. When the account is handed to another rider, the new handover creates its own fee; the old rider's liabilities remain on the old handover. An approved free switch creates a replacement handover with its new SAR 200 fee fully waived; it does not waive the old handover's unpaid fee. Fee exception approval can waive only the still-unpaid portion and cannot refund a prior collection.

### Commission

The default charge is **SAR 15 per day per active account**, starting the Riyadh calendar day after handover. It is due even with zero dispatches. The closing day counts if it is later than the handover day. A rider with two active accounts incurs separate charges for both. A settlement posts unposted commission through its cutoff and future days continue accruing; it neither ends the handover nor repeats posted days. Partial payment leaves the remainder due.

An approved percentage policy replaces the daily SAR 15 for its specified inclusive dates. Its fixed rate is **15%**. The period may cover unposted earlier dates but cannot overlap posted commission or another approved policy; request length is at most 732 inclusive days. After the policy period the daily rate resumes. Manual earnings must completely cover the percentage dates being posted. Missing, overlapping, or partially cut-off earnings make `commissionComplete: false` and prevent settlement or close. A statement can be superseded with a documented replacement only before its period is posted.

```
commissionBase = totalDeliveryPrice
               - totalPenalties - totalCashAmount
               - totalDriverDebit - totalServiceDeduction
               + totalDriverCredit + totalBonuses
               + totalTips + totalFreeOrders
percentageCommission = max(0, commissionBase) * 0.15
```

All nine earnings components must be supplied, including explicit zeroes, as nonnegative money values. `totalFreeOrders` is a **money amount**, not a dispatch count. For example, 1000 − 20 − 100 − 10 − 5 + 50 + 30 + 10 + 5 = SAR 960 base and SAR 144 commission. Calculated ledger entries retain the date range and `calculationJson` evidence identifying policy and earnings sources.

### Jahez debt, settlement, and reset

`Net Amount` from a transaction file keeps its source sign. A **negative** value becomes a positive debt receivable (`-NetAmount`) from the actual rider of that transaction's historical handover. A positive value is a rider credit against the receivable; no automatic cash payout is created. The ledger has separate fee, platform-debt, and commission buckets. Charges increase the receivable, collections/credits decrease it. Corrections use signed adjustment or reversing entries with traceable sources. Imported debt and fee/commission accrual do not create cashbox entries.

A settlement accepts separate `feePayment`, `debtPayment`, and `commissionPayment`. Each can be partial, but none can exceed its available bucket balance, and the total actual payment must be positive. Fee collections enter the **account-fees** cashbox section; debt and commission collections enter the **settlements** section. With `countsAsSettlement: true`, any actual settlement collection restarts the ten-day reminder clock. A fee-only collection outside settlement uses `countsAsSettlement: false`, with zero debt and commission payments, so the reminder anchor remains unchanged. The cutoff may not precede an already posted commission or settlement period. Closing a handover leaves unpaid items on its original rider and handover. A late imported transaction for that prior period remains with the prior rider.

A reset request, after approval, closes the usage period, posts the due commission, records a zero-value transfer marker, marks the old handover's debt as transferred to follow-up, and makes the account available for another rider. It neither creates a cash collection nor moves the debt onto the new rider. `externalResetReference` only records a manually performed Jahez-dashboard operation. Legacy adoptions instead attach an existing active assignment to a new financial starting date and explicitly supplied opening debt, fees, and commission; the system does not back-create historical SAR 200 charges or daily commission. Exclude transactions already represented by the opening balances when choosing the financial start.

### Cashbox and accountant handoff

There is one company Jahez cashbox with two independently reconciled sections. A submission reserves available collected entries through the end of `businessDate` and stores its fee and settlement totals. Entries collected later stay available for a future handoff; an entry cannot be reserved twice. A different accountant confirms the exact amount **in each section**. A third user approves after confirmation or rejects the pending/confirmed handoff. Approval removes the reserved amounts from outstanding custody while retaining the entries and audit chain. Rejection releases the reservation. Equal grand totals with mismatched section totals are invalid. Accountant handoff does not change rider debt or the last rider-settlement date.

### Ten-day reminder

The reminder anchor is handover start or the last actual payment marked as a settlement. The account is overdue only after **more than ten Riyadh calendar days**. File upload, a zero-payment action, fee-only payment, and reset do not restart the clock. Closed handovers remain eligible while debt remains. The Worker runs a scan at startup and hourly, notifying active authorized `jahez.read` users within Jahez scope. A reminder state prevents duplicates for one overdue episode; payment/resolution archives that episode, and a new overdue episode may later notify again. The balance exposes the anchor, elapsed days, overdue state, amount, commission completeness, and latest imported transaction time.

## 4. Response object dictionary

These are the **actual JSON property names**. `id` values are UUIDs; `...AtUtc` are ISO instants; `...Date`, `...On`, `fromDate`, `toDate`, and `throughDate` are date-only strings. Nullable properties can appear as `null`.

Every **History** object additionally contains `id`, `createdAtUtc`, `createdByUserId`. Every **Auditable** object additionally contains those fields plus `updatedAtUtc`, `updatedByUserId`, `rowVersion` (Base64), `isDeleted`, `deletedAtUtc`, `deletedByUserId`, and `deletionReason`. These metadata fields are returned, not accepted as Jahez create-command fields.

| Response type | Complete type-specific properties |
|---|---|
| `Handover` | `id`, `accountId`, `externalAccountId`, `riderProfileId`, `assignmentId`, `startedAtUtc`, `endedAtUtc`, `commissionStartsOn`, `commissionPostedThrough`, `lastSettlementPaymentAtUtc`, `isLegacy`, `debtTransferred` |
| `Balance` | `handoverId`, `accountId`, `externalAccountId`, `riderProfileId`, `throughDate`, `fees`, `platformDebt`, `postedCommission`, `unpostedCommission`, `totalReceivable`, `commissionComplete`, `problems` (string array), `reminderAnchorAtUtc`, `daysSinceSettlementPayment`, `isOverdue`, `debtTransferred`, `latestTransactionAtUtc` |
| `Fee` + Auditable | `handoverId`, `amount`, `waivedAmount`, `approvalRequestId`. There is no `paid`/`remaining` field; use `Balance.fees` and settlements. |
| `ApprovalRequest` + Auditable | `handoverId`, `kind`, `status`, `requestedByUserId`, `targetAccountId`, `waiverAmount`, `fromDate`, `toDate`, `effectiveAtUtc`, `reason`, `externalResetReference` |
| `Decision` + History | `requestId`, `actorUserId`, `status`, `decidedAtUtc`, `reason` |
| `ApprovalResponse` | `request: ApprovalRequest`, `decisions: Decision[]` |
| `CommissionPolicy` + History | `handoverId`, `approvalRequestId`, `fromDate`, `toDate`, `rate` (0.15) |
| `Earnings` + History | `handoverId`, `fromDate`, `toDate`, `totalDeliveryPrice`, `totalPenalties`, `totalCashAmount`, `totalDriverDebit`, `totalServiceDeduction`, `totalDriverCredit`, `totalBonuses`, `totalTips`, `totalFreeOrders`, `supersedesId`, `reason` |
| `Settlement` + History | `handoverId`, `throughDate`, `recordedAtUtc`, `collectedByUserId`, `feePayment`, `debtPayment`, `commissionPayment`, `countsAsSettlement`, `reason` |
| `LedgerEntry` + History | `handoverId`, `bucket`, `kind`, `amount` (signed), `sourceId`, `reversesEntryId`, `occurredAtUtc`, `reason`, `fromDate`, `throughDate`, `calculationJson` (nullable JSON **string**) |
| `ImportBatch` + Auditable | `kind`, `contentHash`, `uploadedByUserId`, `committedAtUtc`, `replacesBatchId`, `correctionReason` |
| `ImportPreview` | `batchId`, `kind`, `committed`, `rows: ImportRow[]`, `issues: ImportIssue[]`, `files: ImportFile[]`, `accounts: ImportAccountSummary[]` |
| `ImportRow` | `rowId`, `fileName`, `rowNumber`, `driverId`, `occurredAtUtc`, `accountId`, `handoverId`, `riderProfileId`, `netAmount`, `dispatches` |
| `ImportIssue` | `rowId`, `fileName`, `rowNumber`, `code`, `description` |
| `ImportFile` | `id`, `fileName` |
| `ImportAccountSummary` | `accountId`, `driverId`, `fromDate`, `toDate`, `validRowCount`, `netAmount`, `platformDebtChange`, `dispatches`, `hasIssues` |
| `Dispatch` | `accountId`, `externalAccountId`, `riderProfileId`, `handoverId`, `date`, `count` |
| `CashboxBalance` | `fees`, `settlements`, `reservedFees`, `reservedSettlements`, `availableFees`, `availableSettlements` |
| `CashboxEntry` + Auditable | `settlementId`, `handoverId`, `section`, `amount`, `collectedByUserId`, `receivedAtUtc`, `cashboxHandoverId` |
| `CashboxHandover` + Auditable | `businessDate`, `status`, `feeAmount`, `settlementAmount`, `accountantFeeAmount`, `accountantSettlementAmount`, `requestedByUserId`, `accountantUserId`, `approvedByUserId`, `confirmedAtUtc`, `decidedAtUtc`, `reason`, `confirmationReason`, `decisionReason` |

`Balance` is the **current receivable** evaluated with a transaction cutoff, not a historical balance snapshot after later payments. `postedCommission` has already been recorded in the ledger; `unpostedCommission` is its preview. `platformDebt` may be negative (rider credit). A missing earnings statement yields `commissionComplete: false` and explanatory `problems`. `latestTransactionAtUtc: null` means no imported transaction.

## 5. All Jahez endpoints

In the tables, `{id}` means the UUID named in the Request column. For GET lists, `page` and `pageSize` are optional unless stated otherwise. All POST routes require the idempotency header described above.

### 5.1 Handovers, balances, and history

| Method and route | Request | HTTP 200 response | Permission |
|---|---|---|---|
| `POST /api/jahez/handovers` | `HandoverRequest` below | `Handover`; use returned `id` as `handoverId` | `jahez.handovers.manage` |
| `POST /api/jahez/legacy-adoptions` | `LegacyAdoptionRequest` below | `Handover` with `isLegacy: true` | `jahez.adjustments.manage` |
| `POST /api/jahez/handovers/{id}/close` | `{id}=handoverId`; `CloseRequest` | `Handover` with `endedAtUtc` | `jahez.handovers.manage` |
| `GET /api/jahez/handovers` | Query `accountId?`, `riderId?`, `page?`, `pageSize?` | `Page<Handover>` | `jahez.read` |
| `GET /api/jahez/handovers/{id}/balance` | `{id}=handoverId`; required query `through=yyyy-MM-dd` | `Balance` | `jahez.read` |
| `GET /api/jahez/debts` | Query `riderId?`, `overdueOnly?` (default false), `page?`, `pageSize?` | `Page<Balance>`; includes old closed handovers' debts | `jahez.read` |
| `GET /api/jahez/ledger` | Query `riderId?`, `handoverId?`, `page?`, `pageSize?` | `Page<LedgerEntry>` | `jahez.read` |
| `GET /api/jahez/handovers/{id}/fee` | `{id}=handoverId` | `Fee` | `jahez.read` |
| `GET /api/jahez/handovers/{id}/settlements` | `{id}=handoverId`; `page?`, `pageSize?` | `Page<Settlement>` | `jahez.read` |
| `GET /api/jahez/handovers/{id}/earnings` | `{id}=handoverId`; `page?`, `pageSize?` | `Page<Earnings>`; includes superseded history | `jahez.read` |
| `GET /api/jahez/handovers/{id}/commission-policies` | `{id}=handoverId`; `page?`, `pageSize?` | `Page<CommissionPolicy>` | `jahez.read` |

Request bodies:

```json
{"accountId":"11111111-1111-1111-1111-111111111111","riderProfileId":"22222222-2222-2222-2222-222222222222","effectiveAtUtc":"2026-10-01T08:00:00+03:00","reason":"Account handed to rider","initialFeePayment":80,"feeApprovalRequestId":null}
```

`HandoverRequest` requires the first five non-null fields. `initialFeePayment` ranges from 0 to 200 in halalas; `feeApprovalRequestId`, if supplied, must be null. Day one incurs no commission. `initialFeePayment` enters the account-fees cashbox section immediately; it does not mark a rider settlement.

```json
{"assignmentId":"44444444-4444-4444-4444-444444444444","financialStartOn":"2026-10-01","openingDebt":500,"openingFees":70,"openingCommission":30,"reason":"Documented legacy balance"}
```

`LegacyAdoptionRequest` uses an existing active Jahez assignment. A negative `openingDebt` represents a rider credit. It does not recreate old account fees or old commission charges.

```json
{"effectiveAtUtc":"2026-10-03T08:00:00+03:00","reason":"End of account usage"}
```

`CloseRequest` posts commission through the closing day and retains liabilities on this handover. The effective instant must satisfy the handover and posted-data limits.

Illustrative `Handover` and `Balance` responses for a normal fee of 200, an initial fee payment of 80, one `Net Amount = -500` transaction, and two unposted daily commission days:

```json
{"id":"33333333-3333-3333-3333-333333333333","accountId":"11111111-1111-1111-1111-111111111111","externalAccountId":"456469","riderProfileId":"22222222-2222-2222-2222-222222222222","assignmentId":"44444444-4444-4444-4444-444444444444","startedAtUtc":"2026-10-01T05:00:00+00:00","endedAtUtc":null,"commissionStartsOn":"2026-10-02","commissionPostedThrough":null,"lastSettlementPaymentAtUtc":null,"isLegacy":false,"debtTransferred":false}
```

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","accountId":"11111111-1111-1111-1111-111111111111","externalAccountId":"456469","riderProfileId":"22222222-2222-2222-2222-222222222222","throughDate":"2026-10-03","fees":120,"platformDebt":500,"postedCommission":0,"unpostedCommission":30,"totalReceivable":650,"commissionComplete":true,"problems":[],"reminderAnchorAtUtc":"2026-10-01T05:00:00+00:00","daysSinceSettlementPayment":2,"isOverdue":false,"debtTransferred":false,"latestTransactionAtUtc":"2026-10-02T10:00:00+00:00"}
```

### 5.2 Approval requests and decisions

| Method and route | Request | HTTP 200 response | Permission |
|---|---|---|---|
| `POST /api/jahez/requests` | `ApprovalCreateRequest`; examples below | `ApprovalResponse` with `request.status: 1`, `decisions: []` | `jahez.requests.create` |
| `GET /api/jahez/requests` | Query `status?` (1–4), `page?`, `pageSize?` | `Page<ApprovalRequest>`; list items omit decision history | `jahez.read` |
| `GET /api/jahez/requests/{id}` | `{id}=approvalRequestId` | `ApprovalResponse` with decisions | `jahez.read` |
| `POST /api/jahez/requests/{id}/decision` | `{id}=approvalRequestId`; `{ "approve": true/false, "reason": "..." }` | Updated `ApprovalResponse`; status 2 or 3 | `jahez.read` plus kind-specific approval permission |
| `POST /api/jahez/requests/{id}/cancel` | `{id}=approvalRequestId`; `{ "reason": "No longer needed" }` | `ApprovalResponse`; status 4 | `jahez.requests.create`; original creator only |

Each creation body has `handoverId`, `kind`, and `reason`. Add type-specific fields:

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","kind":1,"reason":"Documented fee exception","waiverAmount":50}
```

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","kind":2,"reason":"The current account receives no orders","targetAccountId":"55555555-5555-5555-5555-555555555555","effectiveAtUtc":"2026-10-03T08:00:00+03:00"}
```

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","kind":3,"reason":"Low rider performance","fromDate":"2026-10-02","toDate":"2026-10-03"}
```

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","kind":4,"reason":"Rider did not attend","effectiveAtUtc":"2026-10-03T08:00:00+03:00","externalResetReference":"Manual Jahez dashboard reset reference"}
```

The free-switch target must be a different available Jahez account. For switch/reset, `effectiveAtUtc` must fall within the current usage period and cannot be in the future. The actual state is revalidated when a different user decides; approval is atomic, so a target that became unavailable cannot produce a partial switch. A fee waiver cannot exceed the then-unpaid amount. Decisions notify the requester internally. The response contains the request plus a `decisions` array; each decision has its actor, status, timestamp, and reason.

### 5.3 Earnings, rider collections, and adjustments

| Method and route | Request | HTTP 200 response | Permission |
|---|---|---|---|
| `POST /api/jahez/earnings` | `EarningsRequest` | `Earnings` + History metadata | `jahez.earnings.manage` |
| `POST /api/jahez/settlements` | `PaymentRequest` | `Settlement` + History metadata | `jahez.collections.manage` |
| `POST /api/jahez/adjustments` | `LedgerAdjustmentRequest` | `LedgerEntry` + History metadata | `jahez.adjustments.manage` |

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","fromDate":"2026-10-02","toDate":"2026-10-03","totalDeliveryPrice":1000,"totalPenalties":20,"totalCashAmount":100,"totalDriverDebit":10,"totalServiceDeduction":5,"totalDriverCredit":50,"totalBonuses":30,"totalTips":10,"totalFreeOrders":5,"reason":"Manually entered Jahez earnings","supersedesId":null}
```

`EarningsRequest` requires all nine numeric components, even zeroes. `supersedesId` names a prior, not-yet-posted statement for the same handover and exact date range. Neither overlapping active statements nor changes to a posted period are permitted.

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","throughDate":"2026-10-03","feePayment":50,"debtPayment":100,"commissionPayment":30,"countsAsSettlement":true,"reason":"Partial rider settlement"}
```

The example creates SAR 50 in account fees and SAR 130 in cashbox settlements. It reduces only the named buckets. To pay fees outside a settlement, set `debtPayment` and `commissionPayment` to zero and `countsAsSettlement` to false. Re-read balance and cashbox after a successful collection. A representative `Settlement` response is:

```json
{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","handoverId":"33333333-3333-3333-3333-333333333333","throughDate":"2026-10-03","recordedAtUtc":"2026-10-03T09:00:00+00:00","collectedByUserId":"77777777-7777-7777-7777-777777777777","feePayment":50,"debtPayment":100,"commissionPayment":30,"countsAsSettlement":true,"reason":"Partial rider settlement","createdAtUtc":"2026-10-03T09:00:00+00:00","createdByUserId":"77777777-7777-7777-7777-777777777777"}
```

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","bucket":2,"amount":-25,"reason":"Documented debt correction","reversesEntryId":null}
```

`LedgerAdjustmentRequest` uses positive `amount` to add a receivable and negative to reduce it. A reversal supplies `reversesEntryId`, the same bucket, and exactly the opposite amount. Cash payment entries cannot be reversed through this adjustment endpoint. Adjustments do not create cashbox receipts.

### 5.4 Imports and dispatches

| Method and route | Request | HTTP 200 response | Permission |
|---|---|---|---|
| `POST /api/jahez/imports` | Multipart `kind`, one or more repeated `files`, optional `replacesBatchId`, `correctionReason` | `ImportPreview` with `committed: false` | `jahez.imports.manage` |
| `GET /api/jahez/imports` | Query `page?`, `pageSize?` | `Page<ImportBatch>` | `jahez.imports.manage` |
| `GET /api/jahez/imports/{id}` | `{id}=batchId` | Refreshed `ImportPreview` with current binding/overlap issues | `jahez.imports.manage` |
| `POST /api/jahez/imports/{id}/commit` | `{id}=batchId`; `{}` or `ImportCommitRequest` | `ImportPreview` with `committed: true` | `jahez.imports.manage` |
| `GET /api/jahez/import-files/{id}` | `{id}=files[].id` from preview | Original XLSX bytes; XLSX content type and original filename | `jahez.imports.manage` |
| `GET /api/jahez/dispatches` | Required `from=yyyy-MM-dd`, `to=yyyy-MM-dd`; optional `riderId`, `page`, `pageSize` | `Page<Dispatch>` attributed to actual riders | `jahez.read` |

Upload 1–10 XLSX files **of the same kind** in one batch. Each file is at most 10 MB; all files together at most 30 MB; a batch may contain at most 50,000 rows. The required sheet headings can appear in a different column order. Required cells cannot contain Excel formulas. Original file bytes, each row number, source values, and six-decimal financial precision are retained.

Example multipart upload of two transaction files in one batch; use another upload with `kind=2` for dispatch files:

```bash
curl -X POST "$BASE_URL/api/jahez/imports" -H "Authorization: Bearer $TOKEN" -H "Idempotency-Key: transactions-20261003-01" -F "kind=1" -F "files=@transactions-part-1.xlsx" -F "files=@transactions-part-2.xlsx"
```

| `kind` | Sheet | Required columns |
|---|---|---|
| 1 — transactions | `SDP_Report` | `Driver ID`, `Date`, `Delivery Price`, `Cash Amount`, `Net Amount`, `Driver Adjustment` |
| 2 — daily dispatches | `Delivery Insights Report` | `Driver ID`, `From`, `To`, `Number Of Dispatches` |

`Driver ID` matches the Jahez account's `externalAccountId` as text. The row's actual date/time, not the upload filename, selects the historical handover and rider. The supplied September transaction workbook contains a 1 October row, and that row is assigned to its actual October date. The current fixtures parse to 485 transaction rows with total `Net Amount = -10188.144`, and 114 dispatch rows totalling 1,106 dispatches.

Preview `issues` can contain `parse_error`, `unknown_account`, `future_date`, `assignment_missing`, `multi_day_report`, `duplicate_daily_row`, `assignment_ambiguous`, `invalid_allocation`, `unknown_allocation_row`, and `overlapping_period`. Display the file, row, code, and description. Do not commit while unresolved issues remain. Preview account summaries report the candidate file's `netAmount`, `platformDebtChange = -netAmount` for transactions, and dispatch count; they are not cash receipts or current account balances. A duplicate file/batch does not post twice, even with another idempotency key. Identical transaction rows are **not** silently collapsed because they may be valid separate source rows.

Daily dispatch reports require `From = To`; multi-day counts are not spread automatically. If a rider changed during the same day, the source report lacks individual order times and preview marks the row ambiguous. Commit with a documented manual distribution whose counts sum to the source row and whose handover IDs were valid users of that account that day:

```json
{"allocations":[{"rowId":"cccccccc-cccc-cccc-cccc-cccccccccccc","handoverId":"33333333-3333-3333-3333-333333333333","count":10,"reason":"Orders before switch"},{"rowId":"cccccccc-cccc-cccc-cccc-cccccccccccc","handoverId":"eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee","count":4,"reason":"Orders after switch"}]}
```

For ordinary transaction imports, commit with `{}`. Transaction-file overlap for an account between the active batch's first and last transaction times is blocked. Correct a committed batch by uploading a complete replacement with `replacesBatchId` and `correctionReason`: it must cover the same accounts and date boundaries, reverses previous debt entries and posts replacement ones atomically, and retains the former evidence. A replacement does not reverse a prior cash collection or accountant handoff. Dispatch corrections exclude the superseded batch. Importing dispatches never changes financial amounts or the default daily commission.

Representative `ImportPreview` response:

```json
{"batchId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","kind":1,"committed":false,"rows":[{"rowId":"cccccccc-cccc-cccc-cccc-cccccccccccc","fileName":"transactions.xlsx","rowNumber":2,"driverId":"456469","occurredAtUtc":"2026-10-02T10:00:00+00:00","accountId":"11111111-1111-1111-1111-111111111111","handoverId":"33333333-3333-3333-3333-333333333333","riderProfileId":"22222222-2222-2222-2222-222222222222","netAmount":-500,"dispatches":null}],"issues":[],"files":[{"id":"dddddddd-dddd-dddd-dddd-dddddddddddd","fileName":"transactions.xlsx"}],"accounts":[{"accountId":"11111111-1111-1111-1111-111111111111","driverId":"456469","fromDate":"2026-10-02","toDate":"2026-10-02","validRowCount":1,"netAmount":-500,"platformDebtChange":500,"dispatches":null,"hasIssues":false}]}
```

### 5.5 Cashbox and accountant handoffs

| Method and route | Request | HTTP 200 response | Permission |
|---|---|---|---|
| `GET /api/jahez/cashbox` | No query | `CashboxBalance` | `jahez.cashbox.read` |
| `GET /api/jahez/cashbox/entries` | Query `cashboxHandoverId?`, `page?`, `pageSize?` | `Page<CashboxEntry>` | `jahez.cashbox.read` |
| `GET /api/jahez/cashbox/handovers` | Query `page?`, `pageSize?` | `Page<CashboxHandover>` | `jahez.cashbox.read` |
| `POST /api/jahez/cashbox/handovers` | `CashboxCreateRequest` | `CashboxHandover`, status 1 | `jahez.cashbox.submit` |
| `POST /api/jahez/cashbox/handovers/{id}/confirm` | `{id}=cashboxHandoverId`; `AccountantConfirmRequest` | `CashboxHandover`, status 2 | `jahez.cashbox.confirm` |
| `POST /api/jahez/cashbox/handovers/{id}/decision` | `{id}=cashboxHandoverId`; `{ "approve": true/false, "reason": "..." }` | `CashboxHandover`, status 3 or 4 | `jahez.cashbox.approve` |

```json
{"businessDate":"2026-10-03","reason":"Daily cashbox delivery to accountant"}
```

`businessDate` cannot be in the future. Submission requires at least one unreserved collected entry and reserves all entries received through that Riyadh day. The two section totals are stored separately.

```json
{"feeAmount":130,"settlementAmount":130,"reason":"Received and reconciled both sections"}
```

The accountant must be different from the submitter. Both figures must exactly match their corresponding reserved section totals, not merely the combined total. Final approval requires a confirmed handoff and a third distinct user; rejection can release a pending or confirmed handoff.

```json
{"approve":true,"reason":"Approved after accountant confirmation"}
```

For example, before reservation `CashboxBalance` may be:

```json
{"fees":130,"settlements":130,"reservedFees":0,"reservedSettlements":0,"availableFees":130,"availableSettlements":130}
```

Example `CashboxHandover` response after accountant confirmation, including Auditable metadata:

```json
{"id":"ffffffff-ffff-ffff-ffff-ffffffffffff","businessDate":"2026-10-03","status":2,"feeAmount":130,"settlementAmount":130,"accountantFeeAmount":130,"accountantSettlementAmount":130,"requestedByUserId":"77777777-7777-7777-7777-777777777777","accountantUserId":"88888888-8888-8888-8888-888888888888","approvedByUserId":null,"confirmedAtUtc":"2026-10-03T09:15:00+00:00","decidedAtUtc":null,"reason":"Daily cashbox delivery to accountant","confirmationReason":"Received and reconciled both sections","decisionReason":null,"createdAtUtc":"2026-10-03T09:00:00+00:00","createdByUserId":"77777777-7777-7777-7777-777777777777","updatedAtUtc":"2026-10-03T09:15:00+00:00","updatedByUserId":"88888888-8888-8888-8888-888888888888","rowVersion":"AAAAAAAAAAA=","isDeleted":false,"deletedAtUtc":null,"deletedByUserId":null,"deletionReason":null}
```

After the submission, `reservedFees` and `reservedSettlements` become 130 and both available fields become zero. New receipts after submission remain available. After approval, those handed-off entries leave the outstanding totals; after rejection, they become available again.

## 6. Existing platform-account and supporting endpoints

The Jahez handover uses an existing platform account and its `sponsorId`. These endpoints expose the catalog IDs needed to create an account. Their responses are **not** Jahez `Page<T>` objects unless otherwise stated.

| Method and route | Request | Response | Permission |
|---|---|---|---|
| `GET /api/platform-accounts` | Optional `accountId`, `platformId`, `operatingCityId`, `sponsorId`, `ownerRiderProfileId`, `actualRiderProfileId`, `status`, `paymentModel`, `currentOnly`, `includeArchived` | `SimplePlatformAccountResponse[]` | `platform_accounts.read` |
| `GET /api/platform-accounts/{id}` | Platform account ID | `SimplePlatformAccountResponse` | `platform_accounts.read` |
| `POST /api/platform-accounts` | `SimplePlatformAccountUpsertRequest` | `SimplePlatformAccountResponse` | `platform_accounts.manage` |
| `PUT /api/platform-accounts/{id}` | Same request plus current `rowVersion` | `SimplePlatformAccountResponse` | `platform_accounts.manage` |
| `GET /api/platform-operations/accounts` | Optional `platformId`, `sponsorId` | `PlatformAccountResponse[]` | `platform_accounts.read` |
| `POST /api/platform-operations/accounts` | `PlatformAccountUpsertRequest` | `PlatformAccountResponse` | `platform_accounts.manage` |
| `PUT /api/platform-operations/accounts/{id}` | Same request plus current `rowVersion` | `PlatformAccountResponse` | `platform_accounts.manage` |

Simple API account creation body (use actual catalog IDs):

```json
{"platformId":"12121212-1212-1212-1212-121212121212","operatingCityId":"13131313-1313-1313-1313-131313131313","sponsorId":"14141414-1414-1414-1414-141414141414","ownerRiderProfileId":"16161616-1616-1616-1616-161616161616","code":"J-456469","externalAccountId":"456469","userName":null,"paymentModel":"PayPerOrder","status":"Available","statusReason":null,"acquisitionDate":null,"startDate":null,"endDate":null,"notes":null,"archiveReason":null,"rowVersion":null}
```

Compatibility API creation body (registered owner is an employee):

```json
{"clientPlatformId":"12121212-1212-1212-1212-121212121212","registeredEmployeeId":"17171717-1717-1717-1717-171717171717","operatingCityId":"13131313-1313-1313-1313-131313131313","sponsorId":"14141414-1414-1414-1414-141414141414","code":"J-456469","externalAccountId":"456469","userName":null,"paymentModel":"PayPerOrder","status":"Available","statusReason":null,"acquisitionDate":null,"startDate":null,"endDate":null,"ownershipNotes":null,"operationalNotes":null,"rowVersion":null}
```

The **complete** `SimplePlatformAccountResponse` fields are `id`, `platformId`, `platformCode`, `platformNameAr`, `platformNameEn`, `operatingCityId`, `operatingCityNameAr`, `operatingCityNameEn`, `sponsorId`, `sponsorNameAr`, `sponsorNameEn`, `ownerRiderProfileId`, `ownerEmployeeId`, `ownerRiderNameAr`, `ownerRiderNameEn`, `code`, `externalAccountId`, `userName`, `paymentModel`, `status`, `statusReason`, `acquisitionDate`, `startDate`, `endDate`, `notes`, `currentAssignment`, `rowVersion`. When present, `currentAssignment` has `id`, `accountId`, `paymentModel`, `actualRiderProfileId`, `actualEmployeeId`, `actualRiderNameAr`, `actualRiderNameEn`, `effectiveFrom`, `effectiveTo`, `status`, `startReason`, `endReason`, `wasBackdated`, `backdatedReason`, `assignedByUserId`, `endedByUserId`, `rowVersion`.

The **complete** `PlatformAccountResponse` fields are `id`, `clientPlatformId`, `platformNameAr`, `registeredEmployeeId`, `registeredEmployeeNameAr`, `operatingCityId`, `operatingCityAr`, `sponsorId`, `sponsorNameAr`, `sponsorNameEn`, `code`, `externalAccountId`, `userName`, `paymentModel`, `status`, `statusReason`, `acquisitionDate`, `startDate`, `endDate`, `ownershipNotes`, `operationalNotes`, `rowVersion`.

Supporting reads: `GET /api/platforms` for the `JAHEZ` platform ID (`platform_accounts.read`); `GET /api/sponsors` for the existing account sponsor (`sponsors.read`); `GET /api/hr-catalogs/operating-cities` for city IDs (`operating_cities.read`); `GET /api/riders` for actual rider profiles (`riders.read`, response `RiderDetailsResponse[]`, whose `id` is the `riderProfileId`); `GET /api/notifications?permissions=jahez.read` for overdue reminders (`notifications.read`, cursor-paged `{items,nextCursor}`). Approval decisions also notify the request creator through existing notifications.

The generic `POST /api/platform-accounts/{id}/assign`, `POST /api/platform-operations/assignments`, and generic close/release routes reject Jahez accounts. Use `POST /api/jahez/handovers` and its close/approval workflows for Jahez usage periods.

## 7. Persistence, implementation, deployment, and validation

### Database and code layout

The 17 Jahez tables are in SQL schema `jahez`: `JahezAccountHandover`, `JahezAccountFee`, `JahezApprovalRequest`, `JahezApprovalDecision`, `JahezCommissionPolicyPeriod`, `JahezEarningsStatement`, `JahezImportBatch`, `JahezImportFile`, `JahezImportRow`, `JahezTransaction`, `JahezDailyDispatch`, `JahezRiderSettlement`, `JahezLedgerEntry`, `JahezCashboxEntry`, `JahezCashboxHandover`, `JahezReminderState`, and `JahezCommandReceipt`. The existing platform-account `SponsorId` supplies the sponsor relationship. The retired dashboard-sponsor column is removed by the 4 October migration. Decimal columns are configured as `decimal(19,6)`. SQL check constraints, unique indexes, restricted deletes, immutable financial history, command receipts, and serializable transactions protect financial evidence and concurrent operations. Imported files and row-level raw values remain available for audit. A zero-valued ledger transfer marker retains provenance without duplicating debt.

Relevant source areas: `src/LogisticsERP.Domain/Entities/Jahez/JahezModels.cs` (entities/enums), `src/LogisticsERP.Application/Features/Jahez/JahezContracts.cs` (requests/responses), `src/LogisticsERP.Infrastructure/Jahez/` (service, parser, reminder), `src/LogisticsERP.Infrastructure/Persistence/Configurations/JahezConfigurations.cs` (EF schema), `src/LogisticsERP.Api/Controllers/JahezController.cs` (all 31 routes), and `src/LogisticsERP.Worker/JahezReminderWorker.cs` (hourly scan).

Application migrations: `20261003151506_AddJahezOperations`, `20261003153822_PreserveJahezFinancialEvidence`. Identity migration: `20261003152452_GrantJahezPermissions`. The Jahez migrations were applied to the configured `db67927` database on **3 October 2026**. Verification found 17 Jahez tables, the new dashboard sponsor column, 13 permission definitions, 13 `SYSTEM_ADMIN` grants, no pending Application or Identity migrations, and no EF model drift at that time. Applying migrations does **not** deploy new API or Worker binaries; deploy/run the builds containing this code to serve the routes and reminders. To update another environment after its prerequisite migrations:

```powershell
dotnet ef database update --project src/LogisticsERP.Infrastructure --startup-project src/LogisticsERP.Api --context ApplicationDbContext
dotnet ef database update --project src/LogisticsERP.Infrastructure --startup-project src/LogisticsERP.Api --context IdentityDbContext
```

An environment may instead use the idempotent `docs/sql/jahez-application.sql` and `docs/sql/jahez-identity.sql` scripts after prior prerequisite migrations. Configure its own SQL connection and standard application authentication. No sample rider payments or Jahez accounts were written to the configured database as part of the schema update.

### Verification and limits

Run focused tests with:

```powershell
dotnet test tests/LogisticsERP.Jahez.Tests/LogisticsERP.Jahez.Tests.csproj
```

The most recent focused run passed **27** Jahez tests and skipped **2** optional SQL integration tests because LocalDB would not start on the development machine. The passing set covers original XLSX fixtures, partial collections, credit balances, historical dispatch assignment, dashboard-sponsor independence, reminders, scoped authorization, and SQL model/DDL checks that do not need a server. The optional tests exercise end-to-end SQL financial workflow and concurrent collection/rollback; they still need a working LocalDB environment and have not been validated against the configured live database. To opt in when LocalDB is available:

```powershell
$env:LOGISTICS_JAHEZ_SQL_TESTS = '1'
dotnet test tests/LogisticsERP.Jahez.Tests/LogisticsERP.Jahez.Tests.csproj
Remove-Item Env:LOGISTICS_JAHEZ_SQL_TESTS
```

Integration sequence for a client: fetch platform, sponsors, city, and rider; create/read the Jahez platform account; hand it over and retain its returned ID; read balance/fee; upload and preview the relevant Excel batch; resolve issues and commit; record percentage earnings if an approved percentage period applies; collect by explicit buckets; reconcile the two cashbox sections; submit, confirm, and decide the accountant handoff; use `GET /debts?overdueOnly=true` and notifications for follow-up. Treat all UUIDs and money figures shown in this document as illustrative examples, never as live database records.
