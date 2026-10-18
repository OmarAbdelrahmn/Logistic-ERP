# Frontend handoff — Jahez settlement dates and Chefz/Jahez assignments

Updated 18 October 2026. This file covers the commission dates in Jahez money history, the combined rider account limits, and the Chefz money-sheet cash deduction update (section 10). There is no frontend source project in this repository, so the screen names below identify where the frontend team should implement each change rather than existing component filenames.

## 1. Deployment status

| Item | Status |
|---|---|
| Hosted database | Updated and verified on **18 October 2026 at 11:42 Riyadh time (UTC+3)** |
| Database migration | `20261018083620_AllowChefzAndJahezRiderAssignments` |
| Schema change | `app.RiderClientAssignments.RiderAccountSlot` now permits **1, 2 and 3** |
| Existing assignments | All **716** rows preserved, including **704** open assignments |
| Existing indexes | Active account uniqueness, active rider-slot uniqueness and one-Salary-account uniqueness retained |
| Jahez settlement dates | API response change; no new database columns or data backfill required |
| Chefz collected-cash deductions | Import calculation change; existing `deductions` response field and database column reused |
| API build | Changes implemented and built locally; this hosted database update **did not publish API binaries** |

Deploy the backend build containing these changes before enabling the frontend changes. Database migration alone does not change the running API's assignment validation, add the money response fields or change cash deductions in Chefz uploads. Hosted HTTP endpoint behavior was not tested as part of this deployment.

**Existing-data review:** five riders already have multiple open Jahez assignments. Their assignments were preserved. The new backend rejects further incompatible assignments; it does not automatically close or repair existing assignments. Show all of their accounts, flag the excess and route any correction through the normal authorized Jahez withdrawal/settlement workflow. Do not hide or automatically release an account.

## 2. Screen-by-screen implementation map

| Frontend location | Required change | API/data involved |
|---|---|---|
| Jahez cashbox / money-transactions table | Show the recorded company-commission period for Settlement rows | `GET /api/jahez/v2/money-movements`, `startDate`, `endDate`, `purpose` |
| Money-transaction details, print and export | Include the same recorded period wherever company commission is displayed | Selected movement row; retain the returned date strings |
| Jahez settlement creation / preview form | Keep existing input and preview flow; the new history dates are read-only response fields | Existing operation endpoints; continue submitting `throughDate` |
| Chefz account assignment dialog / rider picker | Allow a second Chefz account and allow Chefz alongside one Jahez account | Shared assignment endpoints and complete rider assignment history |
| Jahez account handover dialog / rider picker | Allow one Jahez account alongside up to two Chefz accounts | `POST /api/jahez/v2/handovers` |
| Rider details / current accounts | Support three cards or rows; display separate Chefz and Jahez counts | `GET /api/riders/{riderProfileId}/platform-history` |
| Shared platform-account edit form | Keep valid payment-model edits available when the rider has all three accounts; retain one-Salary-account validation | `PUT /api/platform-accounts/{accountId}` |
| Chefz account return action | Return only the selected account; refresh available capacity for that rider | `POST /api/platform-accounts/{accountId}/release` |
| Jahez withdrawal / account switch screens | Use Jahez workflow; a replacement Jahez account must remain possible with two Chefz accounts | Jahez operations and approved switch request endpoints |
| API error handling / shared query cache | Handle per-platform limits, preserve rejected form values, refresh current assignments after changes or conflicts | HTTP 409 and `errorCode`; error details described below |
| Chefz money-sheet upload, saved settlements and exports | Show combined deductions including cash collected by the driver; use backend net/company/rider amounts without deducting cash again | Chefz order-settlement preview/import/read endpoints; section 10 |

## 3. Jahez money transactions: settlement dates

### Endpoint and response fields

```http
GET /api/jahez/v2/money-movements?page=1&pageSize=50&fromDate=2026-10-01&toDate=2026-10-18
```

Permission: `jahez.cashbox.read`. The existing response envelope remains `{ items, page, pageSize }`. Two nullable fields are added to **each movement item**:

```ts
interface MovementSettlementDates {
  startDate: string | null; // YYYY-MM-DD; inclusive
  endDate: string | null;   // YYYY-MM-DD; inclusive
}
```

Read these fields from the movement row, not the account response: account records already have unrelated `startDate`/`endDate` fields.

Example of selected fields from a settlement movement:

```json
{
  "purpose": 1,
  "platformAccountId": "44120",
  "net": 100,
  "commission": 75,
  "calculatedCommission": 75,
  "amount": 175,
  "startDate": "2026-10-02",
  "endDate": "2026-10-06"
}
```

`purpose` values are numeric: Settlement / تصفية = **1**, Fuel = **2**, AccountFee = **3**, TransferredDebtRepayment = **4**.

### Display behavior

- For `purpose === 1`, label the period **فترة احتساب عمولة الشركة**, or use columns **بداية فترة العمولة** and **نهاية فترة العمولة**.
- Both dates are inclusive. In the example, 2–6 October covers five days. Daily and percentage segments use the combined recorded accrual range.
- For another purpose, hide the period in details or display `—` in table columns. The API returns both dates as `null`.
- For a Settlement row with no linked operation or no readable recorded accrual dates, display `—` or **الفترة غير متوفرة**. A positive commission amount does not guarantee that historical dates are available.
- Use the saved dates as returned. Later settlements, commission baseline changes and approved amount overrides do not rewrite the saved period.
- These are date-only values. Format the year/month/day components directly; avoid converting them into UTC instants and shifting the displayed day.
- Keep `commission` as the applied **عمولة الشركة**. `calculatedCommission` is the calculation before an approved override; it can differ from `commission`.
- `net` is the signed settlement net and `amount` is actual money received/disbursed. Do not substitute one for the other when displaying commission or movement amounts.

The query parameters `fromDate`/`toDate` filter **movement occurrence time** in Riyadh; they do not filter or change the returned commission period. Both filter bounds are inclusive. An omitted start defaults to the first day of the current Riyadh month, and an omitted end defaults to today, independently. Reversed bounds return HTTP 400. Filtering remains before pagination.

### Settlement form changes

Do not add editable `startDate`/`endDate` inputs to settlement creation. The existing request still uses `throughDate`; the backend determines commission accrual. Keep the existing preview/create/execute flow:

```text
POST /api/jahez/v2/operations/preview
POST /api/jahez/v2/operations
POST /api/jahez/v2/operations/{operationId}/execute
```

Use a stable `Idempotency-Key` for retries of each Jahez command; the preview is read-only and does not require one. Refresh movement history after successful execution. During backend rollout, treat absent date fields as unavailable instead of inventing dates.

## 4. Rider account assignment limits

### Exact rules

| Rider's platform combination | Chefz cap | Jahez cap | Total cap |
|---|---:|---:|---:|
| Chefz only | 2 | 0 | 2 |
| Jahez only | 0 | 1 | 1 |
| Chefz + Jahez | 2 | 1 | 3 |

Chefz aliases **CHEFZ, THECHEFZ, CHEFSZ and SHIFTZ share one combined two-account cap**. They are not separate two-account allowances. A third Chefz account or second Jahez account is rejected even when the rider has fewer than three accounts overall. Jahez can be assigned before, between or after the two Chefz accounts.

Other platforms keep the existing one-account rule. This exception does not allow a Chefz/Jahez account to be combined with an unrelated platform. Existing permissions, rider eligibility, one rider per account, account availability, sponsor/owner checks and debt/operational blocks still apply. Jahez continues to support PayPerOrder accounts only. The existing cap of one Salary account per rider is retained.

### Fetch the full assignment set

For assignment eligibility and rider cards, use:

```http
GET /api/riders/{riderProfileId}/platform-history
```

Permission: `platform_assignments.read`. Its `assignments` array includes `assignmentId`, `accountId`, `platformId`, `platformCode`, `externalAccountId`, `paymentModel`, `effectiveFrom`, `effectiveTo`, `status` and the existing owner/history fields.

Treat rows with **`effectiveTo === null`** as open assignments for these limits, matching the backend. Do not count only rows whose status says `Active`, or only the accounts visible on the current page. An account's manual Suspended/Retired/Archived status does not itself release its open rider assignment. The history endpoint reads across platforms, including retained account history.

Show counts such as **شيفز 2/2 · جاهز 1/1 · الإجمالي 3/3** when both platforms are used. Keep each account's own ID, platform, assignment and actions. Do not overwrite two Chefz accounts with a dictionary keyed only by platform ID. Preserve each account's separate performance and settlement records.

If complete cross-platform assignment data is unavailable to the current user, let the backend decide eligibility on submission. Do not assume zero assignments from an empty platform-filtered list or a failed history request.

### Capacity preview helper

Use this only for count/payment-model hints; server validation, permissions, account availability and operational blocks still decide whether assignment succeeds.

```ts
type AssignmentForCapacity = {
  assignmentId: string;
  platformCode: string;
  paymentModel: string;
  effectiveTo: string | null;
};

const chefzCodes = new Set(['CHEFZ', 'THECHEFZ', 'CHEFSZ', 'SHIFTZ']);

function capacityAllows(
  requestedPlatform: string,
  requestedPaymentModel: string,
  assignments: AssignmentForCapacity[],
  editedAssignmentId?: string,
): boolean {
  // For a payment-model edit, evaluate the replacement once rather than counting
  // the existing assignment plus its edited version as two accounts.
  const open = assignments.filter(a =>
    a.effectiveTo === null && a.assignmentId !== editedAssignmentId);

  if (requestedPaymentModel === 'Salary' &&
      open.some(a => a.paymentModel === 'Salary')) return false;

  const requestedChefz = chefzCodes.has(requestedPlatform);
  const requestedJahez = requestedPlatform === 'JAHEZ';
  if (requestedJahez && requestedPaymentModel !== 'PayPerOrder') return false;
  if (!requestedChefz && !requestedJahez) return open.length === 0;
  if (open.some(a => !chefzCodes.has(a.platformCode) &&
                     a.platformCode !== 'JAHEZ')) return false;

  const chefz = open.filter(a => chefzCodes.has(a.platformCode)).length;
  const jahez = open.filter(a => a.platformCode === 'JAHEZ').length;
  return chefz + Number(requestedChefz) <= 2 &&
         jahez + Number(requestedJahez) <= 1;
}
```

Do not use a global `currentAccountCount < 3` check: it would permit three Chefz accounts or two Jahez accounts. Do not use the previous one-account global check, which would block valid combinations.

## 5. Assignment and return requests

### Chefz assignment dialog

Continue using the existing shared route, with permission `platform_assignments.create`:

```http
POST /api/platform-accounts/{accountId}/assign
Content-Type: application/json
```

```json
{
  "actualRiderProfileId": "<rider-profile-uuid>",
  "effectiveFrom": "2026-10-18",
  "reason": "استلام حساب شيفز",
  "wasBackdated": false,
  "backdatedReason": null
}
```

The legacy `POST /api/platform-operations/assignments` route enforces the same limits for clients that already use it. Its existing contract remains in place. Do not send a new slot number or a new maximum-account field; the backend chooses a free slot.

### Jahez handover dialog

Use the Jahez route with permission `jahez.handovers.create` and an `Idempotency-Key`:

```http
POST /api/jahez/v2/handovers
Content-Type: application/json
Idempotency-Key: <stable-command-uuid>
```

```json
{
  "accountId": "<jahez-account-uuid>",
  "riderProfileId": "<rider-profile-uuid>",
  "effectiveAtUtc": "2026-10-18T08:00:00Z",
  "reason": "استلام حساب جاهز",
  "initialFeePayment": 0,
  "paymentReference": null
}
```

The timestamp is illustrative and must satisfy the existing handover-date rules. Existing fee-payment permissions and validations still apply when a positive initial fee payment is entered. Generic assign/release routes are unavailable for Jahez; they would bypass its financial workflow.

### Account return, withdrawal and switch

For Chefz, use `POST /api/platform-accounts/{accountId}/release`, permission `platform_assignments.delete`, with the assignment's current `rowVersion`:

```json
{
  "effectiveTo": "2026-10-18",
  "status": "Ended",
  "reason": "إرجاع الحساب",
  "rowVersion": "<assignment-rowVersion>"
}
```

Close only the selected account. The rider's other accounts remain assigned. Refresh the rider's complete assignment set and account availability after success. Legacy clients can keep `POST /api/platform-operations/assignments/{assignmentId}/close`.

For Jahez, use the existing operations flow for withdrawal or settlement-and-withdrawal. For an approved account switch, keep the existing workflow:

```text
POST /api/jahez/v2/requests
  { handoverId, kind: 2, targetAccountId, effectiveAtUtc, reason, ...existing fields }

POST /api/jahez/v2/requests/{requestId}/decision
  { approve: true, reason, ...existing fields }
```

Both commands use their own stable `Idempotency-Key`; existing request/approval permissions and maker/approver checks remain. The backend releases the old Jahez assignment inside the approved switch before assigning the replacement. Do not disable the switch simply because the rider has three open accounts: replacing one Jahez while keeping two Chefz is supported. After approval, refresh handovers, accounts, requests and the rider's assignments.

## 6. Account payment-model editor

`PUT /api/platform-accounts/{accountId}` retains its existing full account-update body and `rowVersion`. A rider with two Chefz and one Jahez can still have a valid Chefz payment-model edit. Keep the editor available at full account capacity; exclude the edited assignment itself when previewing its replacement model.

Changing a second account to Salary is still rejected with `platform.rider_salary_account_limit_reached`. Do not interpret the three-account allowance as a three-Salary-account allowance. Refresh the account and its rider assignments after a successful edit. The legacy `PUT /api/platform-operations/accounts/{accountId}` path follows the same validation.

## 7. Errors and cache refresh

ProblemDetails exposes the code as **`errorCode`**, not `code`. Generic assignment-limit errors add a top-level `assignmentLimit` object. For example, selected fields when two Chefz plus one Jahez are already assigned:

```json
{
  "status": 409,
  "errorCode": "platform.rider_account_limit_reached",
  "field": "actualRiderProfileId",
  "assignmentLimit": {
    "riderProfileId": "<rider-uuid>",
    "requestedAccountId": "<account-uuid>",
    "requestedPaymentModel": "PayPerOrder",
    "maximumActiveAccounts": 3,
    "maximumSalaryAccounts": 1,
    "activeAccounts": [
      { "assignmentId": "<assignment-1>", "accountId": "<account-1>", "platformId": "<chefz-platform>", "platformCode": "SHIFTZ", "platformNameAr": "شيفز", "platformNameEn": "Chefz", "externalAccountId": "chefz-1", "paymentModel": "PayPerOrder" },
      { "assignmentId": "<assignment-2>", "accountId": "<account-2>", "platformId": "<chefz-platform>", "platformCode": "SHIFTZ", "platformNameAr": "شيفز", "platformNameEn": "Chefz", "externalAccountId": "chefz-2", "paymentModel": "PayPerOrder" },
      { "assignmentId": "<assignment-3>", "accountId": "<account-3>", "platformId": "<jahez-platform>", "platformCode": "JAHEZ", "platformNameAr": "جاهز", "platformNameEn": "Jahez", "externalAccountId": "44120", "paymentModel": "PayPerOrder" }
    ],
    "allowedPaymentModels": []
  }
}
```

The angle-bracket IDs above are placeholders. `activeAccounts` entries are objects containing `assignmentId`, `accountId`, `platformId`, `platformCode`, `platformNameAr`, `platformNameEn`, `externalAccountId` and `paymentModel`. Display this list to explain the conflict.

| Error | Frontend behavior |
|---|---|
| `platform.rider_account_limit_reached` | Refresh assignments; explain the requested platform's cap. Keep entered form values. |
| `platform.rider_salary_account_limit_reached` | Explain the existing one-Salary-account cap; offer only models allowed by the response/current state. |
| `jahez.conflict` from a handover | Show `detail`, refresh assignments/account availability; do not expect `assignmentLimit` on Jahez errors. This code is also used for other Jahez conflicts. |
| `rider.jahez_debt_block` | Keep the existing debt/operational-block flow. Extra capacity does not remove the block. |
| HTTP 409 concurrency/stale row version | Reload affected data and versions before a new attempt. Do not blindly retry a changed request. |
| HTTP 403 | Keep the existing permission handling; capacity does not grant permission. |

`maximumActiveAccounts` is total capacity for the requested platform combination, not a statement that any platform can occupy every slot. For example, a third Chefz request can be rejected with two open Chefz accounts even though a first Jahez account would still be allowed. `allowedPaymentModels: []` means changing payment model will not make that requested assignment valid.

After any assignment, release, payment-model edit, withdrawal or approved switch, refresh the account list/detail and the selected rider's cross-platform assignment/history data. On a conflict, refresh before recalculating eligibility. Do not optimistically remove an existing account when a new assignment fails.

## 8. Acceptance checks for the frontend team

| Scenario | Expected result |
|---|---|
| No accounts → first Jahez | Allowed |
| One Jahez → second Jahez | Rejected, even with spare total capacity |
| No accounts → first Chefz → second Chefz | Allowed |
| Two Chefz → third Chefz | Rejected; first Jahez can still be assigned |
| Two Chefz → first Jahez | Allowed; display three separate accounts |
| One Jahez → first Chefz → second Chefz | Allowed |
| One Chefz → one Jahez → second Chefz | Allowed |
| Chefz alias + different Chefz alias + Jahez | Allowed; aliases share the Chefz cap |
| Unrelated platform account + Chefz/Jahez | Existing one-account restriction still applies |
| Three valid accounts → edit one Chefz payment model | Valid edit remains available; second Salary is rejected |
| Return either Chefz with three accounts held | Only that account closes; another Chefz can use the freed slot |
| Approved Jahez switch with two Chefz held | Old Jahez replaced; the two Chefz accounts stay assigned |
| Existing rider with multiple Jahez | Show all existing rows and excess; never auto-close accounts |
| Settlement money row with recorded dates | Show inclusive period beside company commission |
| Fuel/fee/debt-repayment row | No settlement period shown |
| Old Settlement row with unavailable dates | Display `—`; no dates guessed from current account/history filters |
| Later settlement or approved commission override | Earlier movement dates stay the same |
| Movement date filter crosses a month boundary | Filter occurrence dates normally; retain each row's original commission period |

Backend verification already completed: full solution build with zero warnings/errors; 204 tests passed, 5 optional SQL/hosted checks skipped; no pending EF model changes. Hosted migration history, trusted slot constraint and preserved assignments/indexes were separately verified after commit. This is schema verification; concurrent assignment writes and the hosted API build were not exercised.

## 9. Deployment evidence and implementation references

- Hosted report: `outputs/chefz-jahez-assignment-migration-20261018T084224487Z/report.json`.
- The same evidence folder contains the migration SQL, preflight, assignment backup and before/after-commit verification.
- Applied SQL: `database/scripts/rider-chefz-jahez-assignment-limits.sql`.
- Deployment runner: `tools/Apply-ChefzJahezAssignmentLimits.ps1` (preflight by default; explicit `-Apply` executes the migration).
- Money response: `JahezWorkflowContracts.cs` / `JahezMoneyMovementResponse`, `JahezWorkflowService.WorkflowQueries.cs`.
- Shared assignment rules: `ChefzAccountAssignmentRules.cs`, `PlatformAssignmentPolicySupport.cs`, `SimplePlatformService.cs`, `PlatformOperationsService.cs`.
- Jahez handover enforcement: `JahezWorkflowService.cs` / `CreateHandover`; Jahez cap: `JahezRules.cs`.
- Shared frontend contract: `docs/jahez-v2-api-contracts.ts` / `MovementRow` now includes nullable `startDate` and `endDate`.

Rollback requires resolving any assignment that occupies slot 3 before restoring the old two-slot constraint. Existing financial and assignment histories must be retained through normal workflows.

## 10. Chefz money-sheet cash deductions — 18 October update

The money upload now reads the optional `Cash Collected By Driver` header, including repeated whitespace such as `Cash   Collected By Driver`. Missing or blank cash values are zero, preserving older templates. The existing decimal, range, duplicate-header and formula validation applies to this column too.

```text
deliveryFeesAfterVat = round(sum(Driver Delivery Fees) / 1.15, 2)
deductions = sum(abs(Deduction Amount)) + sum(abs(Cash Collected By Driver))
netEarnings = deliveryFeesAfterVat + Bonus Amount + Driver Compensate + Driver Tip − deductions
Freelancer companyAmount = round(netEarnings × 0.15, 2)
FullTime companyAmount = assignedDays × 25 SAR
riderAmount = netEarnings − companyAmount
```

VAT is extracted from delivery fees only. Bonus Amount, Driver Compensate and Driver Tip are added at their full values afterward. Cash is included in deductions before calculating the existing company share; the company-fee rules are unchanged.

Affected routes: `POST /api/chefz/settlements/orders/preview`, `POST /api/chefz/settlements/orders/import`, `GET /api/chefz/settlements/orders/imports/{importId}` and the existing settlement list. The response shape does not change: `deductions` now includes both sources in each `riders[]` row and nested `accounts[]` row, and saved order settlements retain that combined amount.

Frontend changes: update upload help/template-column guidance; label the preview/detail/export deduction amount **إجمالي الخصومات (الخصومات + النقد المحصل)**; render returned totals. Do not subtract cash again client-side or subtract it from fees before VAT extraction. Continue blocking save on `issues` and using `canSave` normally.

Example: delivery fees 115 → 100 after VAT; bonus 30 + compensation 20 + tip 10; Deduction Amount 5 + collected cash 20 → deductions 25; net earnings 135. For Freelancer, company amount is 20.25 and rider amount is 114.75. Amounts are SAR.

This update needs the API build published; no database migration is needed. Existing saved imports and identical-file replays keep their original financial snapshots. This code change does not recalculate historical settlements. Preview, save, saved read and replay should agree for new files, including two Chefz accounts attributed to one actual rider.

Verification: full solution build passed with zero warnings/errors. The Chefz workflow suite passed **115** tests; **12** optional SQL, hosted-audit and supplied-workbook checks were skipped. This includes 17 new cash cases covering both account types, VAT-free additions, cash sign normalization, negative balances, validation, save/read/replay and two-account aggregation.
