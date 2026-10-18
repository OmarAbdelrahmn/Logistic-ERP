# Jahez frontend changelog and implementation specification

Verified against the backend source on 2026-10-11. This document specifies the frontend work required for the indebtedness update. It does not claim that those screens have been implemented. Read it together with the [complete API reference](jahez-full-api-reference.md), [TypeScript contracts](jahez-api-contracts.ts), [36-route inventory](jahez-endpoint-inventory.json), and [Postman collection](jahez.postman_collection.json).

## 1. Product rules that drive the changes

The operator manually chooses whether to transfer debt to a departed rider or freeze the account. SAR 15,000 is an example, not an automatic threshold. Account clearance with Jahez can release a frozen account while the rider's debt remains payable. Recording actual rider payments is separate from confirming that a period is paid.

Treat these as three separate questions throughout the UI:

1. What does this rider owe across their periods?
2. Is this particular account available for another rider?
3. Has a particular period been fully paid, including all commission?

A frozen period shows the same liability under both its rider and account. It is one obligation, not two amounts to add together. A transferred or externally cleared period remains in the original rider's financial history even after its account is reused.

## 2. Detailed changelog

| Area | Required frontend change | Source / behavior |
|---|---|---|
| Rider profile | Add an all-period Jahez debt summary | New GET `/riders/{riderId}/debt-summary` includes old, frozen, transferred, and cleared periods |
| Debt cards | Show collectible outstanding separately from signed net balance | `outstandingAmount` versus `totalReceivable` |
| Period status | Add five debt lifecycle labels | `debtStatus` values 1–5 |
| Payment status | Add a separate current payment badge | `paymentStatus` values 1–2; do not infer from `debtPaidAtUtc` |
| Freeze | Add a permission-controlled freeze dialog | Stops commission after the inclusive Riyadh freeze day; retains the assignment |
| Transfer | Add direct transfer-to-rider dialog | Requires external clearance reference; releases account; preserves rider debt |
| Clearance | Add external clearance dialog for frozen periods | Uses server execution time; releases account without requiring rider payment |
| Confirm paid | Add explicit paid confirmation after collection | Requires zero positive outstanding and complete commission; can release frozen account |
| Account assignment | Block selection of frozen accounts | Account is `Suspended`, held against its original assignment |
| Commission | Display freeze cutoff and incomplete calculation warnings | `frozenAtUtc`, `commissionComplete`, `problems` |
| Ledger | Support kind 6 status events | Zero-amount events are history, not receipts |
| Collection | Refresh balance before suggesting confirmation | Collection and paid confirmation are separate POSTs with separate keys |
| History | Keep departed rider records visible after reuse | Filter using original period/rider identifiers |
| Imports | Show conflicts caused by frozen/closed intervals | Revalidate preview and enforce manual allocation rules |
| Permissions | Replace obsolete `*.manage` client checks | Use current create/read/update/delete/approve keys from API reference |
| Refresh | Refresh account and rider state together after debt actions | No optimistic financial or availability changes |

## 3. Data changes and display meanings

### 3.1 New and critical fields

| Field | Available on | UI use | Important limitation |
|---|---|---|---|
| `debtStatus` | Handover, balance | Period lifecycle badge | Does not mean the debt is paid |
| `paymentStatus` | Balance, rider summary | Current financial badge | Not on the handover DTO |
| `frozenAtUtc` | Handover, balance | Freeze date and commission cutoff | Keep visible after clearance for historical context |
| `accountClearedAtUtc` | Handover, balance | Account release history | Does not prove rider payment |
| `debtPaidAtUtc` | Handover, balance | Last paid confirmation timestamp | A late charge can make paymentStatus Outstanding again |
| `externalClearanceReference` | Handover | External reference in history | Not included in balance; fetch handover data |
| `outstandingAmount` | Balance, summary | Primary amount to collect | Sum of positive balances, rather than signed bucket netting |
| `accountReceivable` | Balance | Amount attributed to account for this period | Zero after debtTransferred; account can have a different current rider |
| `isAccountFrozen` | Balance | Frozen account warning | Use server value alongside lifecycle status |
| `isAccountReady` | Balance | Current availability indication | Can become false when another rider takes the account |
| `frozenAccountCount` | Rider summary | Number of frozen periods/accounts in this rider's summary | Does not replace a paged period list |
| `commissionComplete` / `problems` | Balance | Missing commission data notice | Summary has no completeness field |
| `debtTransferred` | Handover, balance | Rider-only liability indicator | Keep distinct from payment status |
| `lastSettlementPaymentAtUtc` | Handover | Last qualifying settlement payment | Not every receipt resets the reminder anchor |

All these response fields are read-only. The UI sends action request bodies, not edited statuses or computed totals. Do not add `debtStatus`, `paymentStatus`, `isAccountReady`, or `outstandingAmount` to request bodies.

### 3.2 Financial cards and formulas

Use the returned numbers as the authority:

```text
commission = postedCommission + unpostedCommission
totalReceivable = fees + platformDebt + commission
outstandingAmount = max(0, fees) + max(0, platformDebt) + max(0, commission)
accountReceivable = debtTransferred ? 0 : totalReceivable
```

Example: fees 200, platformDebt -300, commission 75 gives signed net -25, but collectible outstanding is 275. Show both values with different labels. A credit in one bucket does not silently settle another bucket. Do not clamp all visible negative balances to zero: a negative amount is useful credit information.

Suggested financial card labels:

| Arabic label | Value |
|---|---|
| إجمالي المطلوب من المندوب | `outstandingAmount` |
| صافي رصيد الفترة | `totalReceivable` |
| رسوم الحساب المتبقية | `fees` |
| رصيد مديونية جاهز | `platformDebt` |
| إجمالي العمولة | `postedCommission + unpostedCommission` |
| عمولة مرحلة | `postedCommission` |
| عمولة غير مرحلة | `unpostedCommission` |
| رصيد الفترة على الحساب | `accountReceivable` |

When `commissionComplete=false`, put a visible warning beside the amount: «حساب العمولة غير مكتمل؛ راجع بيانات الأرباح». Show returned `problems`, and avoid saying the displayed total is final. Payments that post commission, transfer, clearance, and paid confirmation may require completing those records first. Freeze itself can still be used to stop further accrual.

### 3.3 Status labels

| Dimension | Value | Suggested Arabic label |
|---|---|---|
| Debt lifecycle | 1 Active | فترة مفتوحة |
| Debt lifecycle | 2 Closed | فترة مغلقة |
| Debt lifecycle | 3 TransferredToRider | المديونية على المندوب |
| Debt lifecycle | 4 Frozen | الحساب مجمد |
| Debt lifecycle | 5 AccountCleared | تمت تصفية الحساب مع جاهز |
| Payment | 1 Outstanding | يوجد مستحق |
| Payment | 2 Paid | لا يوجد مستحق حاليًا |
| Operational account | Available | متاح للإسناد |
| Operational account | Assigned | مسند لمندوب |
| Operational account | Suspended | موقوف |
| Operational account | Retired / Archived | متقاعد / مؤرشف |

Display lifecycle, payment, and current operational availability separately. Use text/icon badges as well as colors. Before explicit paid confirmation, a Frozen period with paymentStatus=Paid should read «لا يوجد مستحق حاليًا — الحساب ما زال مجمدًا» and offer confirmation to authorized users. After confirmation show its timestamp. A historical cleared period with `isAccountReady=false` may simply belong to an account already assigned to a new rider.

## 4. Actions, permissions, and lifecycle matrix

Every financial page requires `jahez.read` to load balances and history. The following permissions gate buttons in addition to that read access:

| Action | Permission | Main eligibility |
|---|---|---|
| New handover | `jahez.handovers.create` | Available JAHEZ account, valid rider, slot rules |
| Normal close | `jahez.handovers.delete` | Open nonfrozen period; valid chronology and complete commission |
| Freeze | `jahez.resets.approve` | Active open period with positive outstanding or incomplete commission |
| Transfer to rider | `jahez.resets.approve` | Active open nonfrozen period with external reference |
| Clear frozen account | `jahez.resets.approve` | Frozen period, external reference, complete commission |
| Record payment | `jahez.collections.create` | Positive eligible amount within each bucket |
| Confirm paid | `jahez.collections.create` | Non-Active lifecycle, zero outstanding, complete commission |
| Adjustment / legacy adoption | `jahez.adjustments.create` | Validation described in API guide |
| Earnings | `jahez.earnings.create` | Approved percentage period and valid coverage |

| Period state | Freeze | Transfer | External clear | Normal close | Record collection | Confirm paid | New rider on same account |
|---|---|---|---|---|---|---|---|
| Active | If outstanding or commission incomplete | Yes | No | Yes | Yes if collectible | No | No |
| Closed | No | No | No | No | Yes if collectible | When complete and zero | If account currently Available |
| TransferredToRider | No | No | No | No | Yes if collectible | When complete and zero | If account currently Available |
| Frozen, outstanding | Already frozen | No | Yes if commission complete | No | Yes, subject to calculation completeness | No | No |
| Frozen, zero outstanding | Already frozen | No | Yes if commission complete | No | Only if new eligible debt appears | Yes if commission complete | No until release action |
| AccountCleared | No | No | Already cleared | No | Yes if collectible | When complete and zero | If account currently Available |

These are UI gates, not substitutes for backend validation. Refetch before submitting, handle conflicts, and let the server enforce chronology and concurrent state. No financial threshold automatically changes a row. No resume action reopens a frozen period; release closes it and later use starts a new period.

The old ResetAccount approval request remains a separate two-user process. New direct freeze, transfer, and clearance use resets.approve immediately; they do not create an approval request for another user. Explain that distinction in action labels and training.

## 5. Screen: Jahez accounts

Use the shared platform account API for operational identity/status and Jahez handover/balance APIs for financial state. See API guide section 13 for shared routes, DTOs, and permissions. Shared API status enums are strings; Jahez enums are numeric.

Required columns:

- Internal account code and external Jahez Driver ID.
- Registered owner name and employee reference.
- Actual current rider name and employee reference, shown separately from owner.
- Operating city, sponsor, payment model, current operational status.
- Frozen indicator with freeze date when the account has an active frozen financial period.
- Current period's outstanding, lifecycle, and payment badge when financial read access is available.
- Assignment action, financial details action, and permitted metadata edit action.

Do not put old rider debt onto the new rider's current account row. Offer a «سجل الفترات السابقة» link for historical liabilities. When displaying a historical period, the actual rider identity is the period's rider; current owner metadata can have changed since then.

Frozen rows display «الحساب مجمد ومحجوز للمندوب؛ يلزم تصفية الحساب أو تأكيد السداد قبل إسناد جديد». Disable assignment selection for Suspended/frozen accounts, including keyboard selection and bulk flows. A frozen hold retains an open assignment, but its commission cutoff means the rider is not accruing further work days. Do not describe the hold as ongoing work.

Account metadata edits must keep Frozen accounts Suspended. Generic account status edit cannot release them. Generic assign/release routes reject JAHEZ operations: route users through Jahez financial actions. Use the current rowVersion on shared PUT updates.

Account form: platform, code, externalAccountId, registered owner, city, active sponsor, userName, paymentModel, status/reason, acquisition/start/end dates, notes/archiveReason as applicable. Sponsor is required when creating/updating; it is not a Jahez dashboard filter or an added debt-summary field.

## 6. Screen: indebtedness list

Primary data: GET `/debts?riderId=...&overdueOnly=...&page=...&pageSize=...`. Default page 1, size 50; maximum size 100. Each item is a balance DTO.

Required columns: account code/Driver ID, period rider, owner if useful, debt lifecycle, current payment status, fees, platform debt, total commission, outstanding, account-attributed balance, days since qualifying settlement, isOverdue, freeze/clear/paid dates, commission warning, details/actions.

Required filters supported by the server: rider and overdueOnly. Reset page to 1 when these change. The endpoint has no accountId, text search, debtStatus, paymentStatus, or exact count query parameter. If you add local status/text filtering, label it «تصفية النتائج المعروضة» and apply it only to loaded rows. Global status filtering needs a backend addition. Do not call a page subtotal the company's total debt.

The overdueOnly query selects candidates; rows may still return `isOverdue=false`, for example a paid frozen period. Use each returned `isOverdue` value to paint the badge. Do not assume every returned item is overdue. Closed periods with bucket credits can also expose limitations in candidate selection; the period detail is authoritative for collection.

Pagination response contains items/page/pageSize only. When fewer than pageSize arrive, the next page is exhausted. A full page can still be the last page: support requesting the next page and receiving an empty array. Avoid showing fabricated “of N” totals.

## 7. Screen: rider profile and total liability

Call GET `/riders/{riderId}/debt-summary`. Display outstandingAmount prominently, signed totalReceivable separately, fees/platformDebt/commission breakdown, frozenAccountCount, and current paymentStatus. Add links to this rider's periods via GET `/handovers?riderId=...` and debts via GET `/debts?riderId=...`.

An existing rider without Jahez periods returns a zero summary; show «لا توجد مستحقات جاهز مسجلة». A missing rider returns 404 and is not an empty success state. Do not silently merge two different riderProfileIds with similar names.

Summary includes all original rider periods, even when the accounts are now reused. Do not add the account's balance a second time. Since summary has no commissionComplete/problems fields, load relevant period balances to explain calculation warnings and permit settlement/clearance actions. Do not infer complete calculations from a Paid summary badge alone.

## 8. Screen: period financial details

Suggested layout: identity header; three independent status badges; collectible and net cards; account control actions; collection action; history tabs. Show original rider and registered owner separately.

Load balance with GET `/handovers/{id}/balance?through=YYYY-MM-DD`, using today's Riyadh date for current actions. The through query is required. There is no GET `/handovers/{id}`. Obtain the handover from the list/cache using account/rider filters, fetching further pages if necessary; the balance is sufficient for primary identity/status fields but not externalClearanceReference. Do not invent a missing route.

Tabs and endpoints:

| Tab | Endpoint | Notes |
|---|---|---|
| Ledger | GET `/ledger?handoverId={id}` | Signed entries, reversals, status events |
| Payments | GET `/handovers/{id}/settlements` | Actual payment history and countsAsSettlement |
| Account fee | GET `/handovers/{id}/fee` | Original amount and waived amount; remaining comes from balance.fees |
| Earnings | GET `/handovers/{id}/earnings` | Original and superseding statements |
| Commission policies | GET `/handovers/{id}/commission-policies` | Approved rate windows |
| Requests | GET `/requests`, then matching details | No handoverId server filter; do not treat a single page as full period history |
| Dispatches | GET `/dispatches` with supported rider/date filters | Filter loaded records to handoverId; no server handover filter |

Fetch independent tabs lazily or concurrently; do not eagerly fetch every history page. Changing period cancels or isolates old responses. Skeletons preserve layout and disabled buttons until fresh balance arrives.

An optional UI through-date selector uses YYYY-MM-DD and the permitted server date window; the request must always send through. The balance is not a full historical snapshot: imported transaction inclusion can be cut by date while payments/adjustments reflect current ledger state. Label this «احتساب العمولة حتى تاريخ» with a brief explanation, not “exact balance at this historical instant”.

After freeze, show «توقفت العمولة بعد يوم [freeze date]». Include that day; do not run a browser timer that adds SAR 15 for later days. Normal commission begins the day after a new handover; legacy adoption can begin on its specified financial start date.

## 9. Dialog: freeze account

Trigger: Active open period with positive outstanding or incomplete commission, resets.approve. Title «تجميد حساب جاهز». A complete zero-outstanding period cannot be frozen by this command.

Fields:

| Label | Field | Rules |
|---|---|---|
| وقت التجميد | effectiveAtUtc | Required ISO instant with offset; server validates period/financial chronology and future dates |
| سبب التجميد | reason | Required nonblank, up to 2,000 characters |

Preview: account code, rider, current outstanding and commission completeness, selected Riyadh freeze day. Text: «سيظل الحساب محجوزًا للمندوب، ولن يمكن إسناده لمندوب آخر. تُحسب العمولة حتى يوم التجميد شاملًا هذا اليوم، وتبقى المديونية مستحقة».

POST `/handovers/{id}/freeze` with a new idempotency key. Success returns balance. Show success only after server success, then refresh handover/balance, rider summary, debts, ledger, operational account, and assignments. Incomplete percentage earnings do not necessarily disable this action; show the warning and explain that completion is required before clearance/payment confirmation.

## 10. Dialog: transfer debt to rider

Trigger: Active open nonfrozen period, resets.approve. Title «نقل المديونية للمندوب وتحرير الحساب».

Fields: effectiveAtUtc; required reason <=2,000; required trimmed externalClearanceReference <=500. Label reference «مرجع تصفية الحساب مع جاهز». Show the original rider's name prominently to prevent action on a similarly named current user.

Preview: «سيُحرر الحساب لإسناده لمندوب آخر، وتبقى مستحقات هذه الفترة على [rider]. لا تسجل هذه العملية تحصيلًا نقديًا». Display all three debt buckets and outstanding. The action needs complete commission and valid end chronology.

POST `/handovers/{id}/transfer-debt`. After success the original period is TransferredToRider, ended, debtTransferred=true, accountReceivable=0; rider outstanding remains. The new rider gets a new handover and their own fee. Do not hide the old period or convert its financial amounts to zero.

## 11. Dialog: clear frozen account with Jahez

Trigger: Frozen period, resets.approve. Title «تأكيد تصفية الحساب مع جاهز».

Fields: required externalClearanceReference <=500 and reason <=2,000. There is no effectiveAtUtc in this request; backend uses execution time. Do not add a backdated clearance input that the server would ignore.

Show freeze cutoff, rider outstanding, and a commission completeness warning. Copy: «سيصبح الحساب متاحًا لمندوب جديد، وتبقى المديونية الحالية على [rider]. يستمر توقف عمولة هذه الفترة عند تاريخ التجميد». This action records a completed external clearance; it does not itself contact Jahez.

POST `/handovers/{id}/clear-account`. Success is AccountCleared with endedAt/accountClearedAt set, accountReceivable=0, original rider debt preserved. If commission is incomplete, link to the earnings tab rather than offering to waive it implicitly. Refresh all related states; allow a separate new-handover action after current availability is verified.

## 12. Dialog: confirm the rider has paid

Trigger: non-Active period, outstandingAmount=0, commissionComplete=true, collections.create. Title «تأكيد اكتمال سداد المندوب». Required field: reason <=2,000.

Show each bucket and total, plus whether the account is still frozen. Copy for Frozen: «تم تسجيل جميع المستحقات. سيُغلق تجميد هذه الفترة ويصبح الحساب متاحًا». For released history: «سيُسجل تأكيد سداد هذه الفترة في سجل المندوب». Use current availability in the final result: an account assigned to someone else stays assigned.

POST `/handovers/{id}/confirm-paid`. Do not send payment amounts here. It creates a status event, not a cash receipt. If a fresh GET returns outstanding or incomplete commission, block submission and show the reason. If a late import reopens financial debt after a historical confirmation, paymentStatus becomes Outstanding and the UI must offer collection again.

## 13. Dialog: actual collection / settlement

POST `/settlements` is the only rider collection command. Required fields: handoverId, throughDate, feePayment, debtPayment, commissionPayment, countsAsSettlement, reason. See API reference for exact validations and posting chronology.

Use three amount inputs with separate current positive maxima. Do not distribute one number automatically across buckets without showing the allocation for user review. Each payment is nonnegative, maximum 1,000,000,000, up to two decimals; total must be positive. Balance DTO amounts are rounded by the backend to two decimals; import, opening, earnings and adjustment records can preserve six-place precision. Preserve that source precision in history and use returned balance/payment validation for collection. Do not force a Paid badge by locally rounding an outstanding value.

`countsAsSettlement=false` permits fee-only collection. `true` posts commission through the cutoff and resets the reminder anchor for actual qualifying receipts; complete earnings may be necessary even if commissionPayment=0. Display this option as an explicit settlement choice. Fee-only receipts with false do not reset the anchor.

Reliable full-payment flow:

1. Load fresh balance and build the reviewed payment allocation.
2. Submit settlement with its own stable retry key.
3. Retain the returned receipt and mark the collection step complete.
4. Refetch balance, summary, ledger, settlements, fee and cashbox state.
5. If non-Active, complete, and zero outstanding, offer paid confirmation as a separate step with a different key.
6. If confirmation fails, display «تم تسجيل التحصيل؛ لم يكتمل تأكيد السداد بعد» and retry only the confirmation. Do not repeat the successful payment with a new key.

Payment alone does not release a frozen account. Confirmation or external clearance releases it. Partial payments leave the account frozen and outstanding reduced. A receipt should use the settlement id and actor/date from the response rather than a browser-generated final financial identifier.

## 14. New handover and legacy adoption

New handover form: accountId, riderProfileId, effectiveAtUtc, reason, initialFeePayment. The fee is SAR 200; initial payment ranges 0–200 at two decimals. feeApprovalRequestId must be null/omitted. Fee exceptions are requested against an existing handover; do not present an approval id as a prerequisite for initial assignment.

Show owner and actual rider independently. Account picker requires current Available state, an eligible JAHEZ account, chronological availability, and valid rider slots. A rider's frozen holds still count in open assignments for slot limits. A new handover starts fresh liability; old rider debt is not inherited. Immediately refresh account assignment, fee, balance, rider summary, and cashbox if an initial fee payment was recorded.

Legacy adoption is for an existing operational assignment with no financial adoption. Fields: assignmentId, financialStartOn, openingDebt, openingFees, openingCommission, reason. Opening fees and commission are nonnegative; openingDebt may be signed. Six decimal precision is allowed within absolute 1,000,000,000. Show an opening-balance event, not a cash receipt. Reject repeated adoption and invalid assignment/date choices gracefully.

Normal close remains available for open nonfrozen periods, requires effectiveAtUtc/reason, posts complete commission, and releases the account while keeping original rider debt. Label it «إنهاء فترة الاستخدام» separately from transfer and external clearance. It yields Closed, not TransferredToRider.

## 15. Approval requests and earnings

Approval page has status tabs Pending/Approved/Rejected/Cancelled and a paged request list. Create form selects FeeException, FreeSwitch, PercentageCommission, or ResetAccount. Reveal only relevant fields:

| Kind | Fields / UI behavior |
|---|---|
| FeeException=1 | waiverAmount, reason; show remaining fee and reject over-waiver |
| FreeSwitch=2 | targetAccountId, effectiveAtUtc, reason; target must be eligible; replacement fee is waived, old fee is preserved |
| PercentageCommission=3 | fromDate, toDate, reason; approved rate is fixed at 15%, not a free editable rate; maximum 732 inclusive days |
| ResetAccount=4 | effectiveAtUtc, reason, optional externalResetReference; original two-user approval workflow |

POST `/requests/{id}/decision` needs jahez.read at controller plus requests.approve for ordinary kinds or resets.approve for ResetAccount. Requester cannot decide their own request. Cancel is only for the requester while Pending and needs requests.create. Hide unauthorized buttons; explain self-approval restriction when relevant. GET details gives full decisions; decision POST returns `{request, decisions}` with the updated request and only the newly recorded decision in the decisions array. Refetch to obtain full history/final effects and any replacement handover.

Earnings form has nine monetary fields: totalDeliveryPrice, totalPenalties, totalCashAmount, totalDriverDebit, totalServiceDeduction, totalDriverCredit, totalBonuses, totalTips, totalFreeOrders. Submit every field, including explicit zeros. Each is nonnegative, <=1 billion, six decimals. totalFreeOrders is money, not a count. Also handoverId/fromDate/toDate/reason and optional supersedesId.

Show base calculation as defined in API guide and the approved percentage period; use the backend balance as the authority for posted/unposted amounts. Missing required earnings coverage shows returned problems. Corrections use a superseding statement, preserving history; do not mutate/delete historical rows in the UI. Posted periods and already superseded statements cannot be corrected through this endpoint.

## 16. Imports and dispatches

Separate workflows for transactions (kind 1) and daily dispatches (kind 2). Upload FormData with kind and repeated files. Replacement uploads also include replacesBatchId and correctionReason. Let the browser set multipart boundary. Include idempotency header. Accept .xlsx only, 1–10 files, <=10 MiB each and <=30 MiB aggregate; the backend additionally enforces archive and row limits.

The wizard should have: choose files/type → upload/preview → review identity/date/debt effects → resolve allocation conflicts where applicable → commit → committed result/history/download. Preview success does not mean financial posting success.

Preview columns: source filename/row number, Driver ID, occurred date, matched account/rider/period, netAmount, dispatch count, issues. Show account summaries with date scope, validRowCount, netAmount, proposed platformDebtChange, dispatches, hasIssues. Label proposed effect clearly; it is not the account's current debt. Preview DTO does not expose every raw worksheet field, so do not invent unavailable columns without inspecting downloaded source.

Source schemas:

- Transactions: SDP_Report, required Driver ID / Date / Delivery Price / Cash Amount / Net Amount / Driver Adjustment. Debt change is -Net Amount; do not add Driver Adjustment again. Imported Cash Amount is report data, not a cashbox collection.
- Dispatches: Delivery Insights Report, Driver ID / From / To / Number Of Dispatches. From must equal To; integer nonnegative counts. Date formats and parser issue codes are in the API guide.

Date assignment matters after freeze: do not route post-freeze work to the frozen rider. Show no-assignment errors plainly. A late transaction that actually belongs before the cutoff can affect the old rider even after clearance or paid confirmation.

For ambiguous daily dispatches, allocations use rowId, handoverId, count, reason. All eligible periods must belong to the actual account/day, each allocation pair is unique, counts are nonnegative, and their sum equals source dispatches. Client preview can validate totals, but commit validates full identity. A cached ambiguity issue can be resolved by valid supplied allocations: do not disable commit solely because the cached preview still shows that issue. Other unresolved blocking issues remain blockers.

GET preview can re-resolve assignments as data changes. Refresh before commit and after conflicts. A duplicate binary/hash may return an existing batch; show its state rather than creating a duplicate local row. Committed replacement is a whole matching business scope, not an arbitrary subset of old rows; the backend reverses previous effects and retains source history. Explain affected old batch before posting a correction.

Virtualize large preview tables because there is no row pagination endpoint. Dispatch list supports from/to/riderId/page/pageSize, not arbitrary account/handover/status filters. Download originals using GET `/import-files/{fileId}`, a Blob, with server Content-Disposition filename; do not call it as JSON or use batchId as fileId.

## 17. Ledger and adjustments

Ledger displays occurredAtUtc, bucket, kind, signed amount, from/through coverage, reason, actor, source/reference, and reversal relationship. Charge, opening and adjustment values can be positive/negative; payments reduce the financial bucket. Use explicit Arabic labels rather than making every positive row a cash receipt.

Kind 6 (`StatusChange`) has amount zero. Render timeline items for Freeze / TransferToRider / ExternalClearance / ConfirmPaid with no receipt action and no contribution to cash totals. `calculationJson` is a JSON string; parse defensively and preserve unknown future content. For these events its inner keys are PascalCase: Action, FromStatus, ToStatus, EffectiveAtUtc, ExternalClearanceReference. A malformed or unknown payload must not break ledger rendering.

Adjustments require handoverId, bucket, signed nonzero amount, reason, and optional reversesEntryId. Precision is six decimals, absolute max 1 billion. Explain that an adjustment changes recorded liability but creates no cashbox receipt. Reversals must exactly negate an eligible original entry in the same handover/bucket, can happen once, and cannot reverse Payment, Transfer, or StatusChange through this command.

## 18. Cashbox screens

Keep AccountFees and Settlements as independent sections. Show total in custody, reserved pending handover, and available. Total includes reserved: do not sum total + reserved. Available = total - reserved.

Submit handover dialog takes businessDate and reason; the backend selects all eligible available entries through that date. There is no arbitrary amount or selected-entry list in the request. Show this before submission. Entries show settlement, handover, section, amount, collector, received time, and reservation handover id.

Cashbox lifecycle:

| State | Action | Actor restriction |
|---|---|---|
| Pending | Accountant confirm both exact amounts | confirm permission; actor differs from requester |
| Pending | Reject | approve permission; decision actor differs from requester and any accountant |
| AccountantConfirmed | Approve or reject | approve permission; actor differs from requester and accountant |
| Approved / Rejected | Read-only history | No repeat financial decision |

Confirmation body has feeAmount/settlementAmount/reason; both amounts must exactly match, including zero in an empty section. Decision body has approve/reason. Rejection releases reservations. approvedByUserId also records the reject decision actor, so label it «متخذ القرار» rather than always «المعتمد». There is no GET individual cashbox handover route; use the list. Status changes in debt workflow do not add custody funds.

## 19. Client requests, dates, errors, and retries

All Jahez POSTs require a nonblank Idempotency-Key, maximum 150 characters. Generate one per logical action and keep it during retries of the same actor/operation/body. If the user changes payload after a known validation rejection, start a new logical action/key. If a timeout leaves the result unknown, retry the identical request/key before offering a fresh attempt. A key used with changed data can return 409.

Import upload and commit, settlement and paid confirmation, and cashbox submit/confirm/decision each have independent keys. An idempotent replay may return the stored response from the first execution; refetch present state afterwards. Keep submit buttons disabled while awaiting the attempt, but preserve the key on recoverable errors. Do not claim exactly-once behavior across different actors or keys.

All Jahez success JSON returns status 200 without a shared data envelope. Paged replies are items/page/pageSize. Numeric enums must remain numbers on Jahez bodies. Download responses are binary. Standard bearer authentication and error handling still apply.

Date-only values are YYYY-MM-DD without UTC conversion. Instant inputs carry offsets; display in Asia/Riyadh. A freeze at Oct 6 affects that Riyadh business day inclusive. Show chronology errors from server rather than silently moving dates. Decimal monetary fields use numbers in JSON; format for SAR display, preserving source precision in calculations. Avoid using formatted comma-separated strings as numeric payloads.

For ProblemDetails, use title/detail and map known errorCode to actionable UI text. Preserve correlationId for support. Treat 400 as invalid input, 403 as insufficient permission/actor restriction, 404 as missing entity, and 409 as concurrent/financial conflict. Refresh related state on 409. Handle transport/non-JSON errors without trying to render an entire HTML body. A network error never means a collection definitely failed.

## 20. Query/cache refresh map

The following names are suggested query groups, not existing frontend architecture or server routes.

| Successful mutation | Refresh / invalidate |
|---|---|
| Freeze | Period balance/handover, debts, rider summary, ledger, account, assignments/slots, relevant dispatch/import previews |
| Transfer / clear / confirm paid | Same as freeze; current account picker and old/new rider screens where relevant |
| Settlement | Balance, rider summary, debts, fee, settlements, ledger, handover anchor, cashbox balance/entries |
| Handover | Account/assignments/slots, handovers, balance, fee, rider summary, ledger; cashbox for initial fee collection |
| Normal close | Period/account/assignments, rider summary, debts, ledger |
| Earnings / adjustment | Balance, summary, debts, earnings or ledger; handover posted-through when affected |
| Approval decision | Request list/detail plus affected periods/accounts/fees/policies/summary/ledger |
| Import commit | Import detail/list, debts, affected balances/summaries, ledger, dispatches; history even for previously paid periods |
| Cashbox submit / confirm / decision | Cashbox balance, handover list, entries and current detail selection |

No optimistic financial balances, Paid badges, assignment availability, or cashbox transfers. Show pending progress and use server results followed by current reads. Distinguish a successful command followed by a failed refresh: keep the receipt/action success visible and offer refresh rather than retrying a mutation with a new key.

Example action gate for a fresh default balance (integration pseudocode):

```ts
function debtActions(b: JahezBalanceResponse, has: (key: string) => boolean) {
  const canRead = has("jahez.read");
  const canReset = canRead && has("jahez.resets.approve");
  const canCollect = canRead && has("jahez.collections.create");
  return {
    freeze: canReset && b.debtStatus === JahezDebtStatus.Active
      && (b.outstandingAmount > 0 || !b.commissionComplete),
    transfer: canReset && b.debtStatus === JahezDebtStatus.Active && b.commissionComplete,
    clear: canReset && b.debtStatus === JahezDebtStatus.Frozen && b.commissionComplete,
    confirmPaid: canCollect && b.debtStatus !== JahezDebtStatus.Active
      && b.commissionComplete && b.outstandingAmount === 0,
    collect: canCollect && b.outstandingAmount > 0,
  };
}
```

This controls presentation only. Chronology, assignment consistency, bucket payment limits, and concurrency are checked by the backend. Collection may be possible in fee-only mode while a full settlement calculation is incomplete.

## 21. RTL, responsive, and interaction details

- Keep Arabic labels/reasons RTL; render codes, GUIDs, references, dates and numeric amounts in appropriate LTR spans.
- Use locale currency formatting with SAR and a consistent precision policy; show detailed six-place values on demand where relevant.
- On mobile, use cards with rider/account identity before money and action menus; retain distinct lifecycle/payment/availability labels.
- Dialogs need focus management, explicit submit labels, visible required fields, inline errors, pending states, and focus returned to the trigger.
- Show reason and clearance reference in action history; wrap long references instead of breaking the page.
- Hide absent/null display names sensibly, but retain stable ids for navigation; never use the registered owner's id for the actual rider's collection.
- Loading, no results, no permission, missing record, incomplete calculation, network failure, and mutation conflict are different states.
- Do not show destructive-looking “delete debt” controls: these actions preserve financial history.
- Reminder age continues for unpaid frozen/released periods. Only qualifying actual settlement payments reset the anchor; rely on isOverdue/daysSinceSettlementPayment.
- Notifications may lag up to the worker scan interval; immediate financial UI comes from refreshed reads.

## 22. Worked end-to-end scenarios

### A. Manual freeze and later external clearance

Assume a fresh Oct 6 balance through its inclusive freeze day: fees 200, platformDebt 15,000, total commission 75. Outstanding is 15,275. The authorized operator chooses Freeze manually; a balance of 15,000 alone does not force it.

After freeze: lifecycle Frozen, account Suspended, assignment retained, outstanding 15,275, accountReceivable 15,275. Commission does not accrue for Oct 7 onward. Rider and account screens refer to this same 15,275 obligation.

On Oct 11 the operator records external clearance with a Jahez reference. Assume earnings coverage is complete. Period becomes AccountCleared, closes at server execution time, account becomes Available, accountReceivable becomes zero, rider outstanding remains 15,275, and cashbox does not change.

A new rider takes a fresh handover after clearance. Their standard fee starts at 200 with their own payment/commission rules. The departed rider's 15,275 is still in that original rider's summary. The old period's isAccountReady can now be false because the account is Assigned to the new rider.

### B. Partial collection while frozen

Starting from 15,275, collect feePayment 50, debtPayment 1,000, commissionPayment 25, total actual receipt 1,075. Assuming no other events, remaining buckets are fees 150, platformDebt 14,000, commission 50; outstanding is 14,200. Cashbox receives 50 in fees and 1,025 in settlements. The account remains frozen and cannot be assigned. Qualifying countsAsSettlement=true updates the reminder anchor.

### C. Full rider payment and release

Collect the remaining eligible amounts after fresh balance review. PaymentStatus can become Paid while lifecycle remains Frozen. Submit confirm-paid separately after complete commission and zero outstanding are verified. The account is then released, and the original paid history remains. Confirm-paid adds no extra cashbox money.

If the account was externally cleared and assigned to another rider before this payment, confirmation of the old rider's debt must preserve the new rider's assignment.

### D. Signed credit and late financial changes

A balance of fees 200, platformDebt -300, commission 75 is not fully paid: outstanding is 275 despite net -25. Confirmation is rejected until the positive buckets are settled or legitimately adjusted.

A correctly matched late transaction from before freeze can add debt after historical paid confirmation. Display Outstanding again from the refreshed response. Preserve debtPaidAtUtc as a past event; do not force the Paid badge because it is non-null.

## 23. Acceptance checklist for frontend QA

| # | Scenario | Expected result |
|---|---|---|
| 1 | Load Active debt row | Lifecycle and current payment shown independently |
| 2 | Rider has several historical periods | Summary includes all, with no account duplicate |
| 3 | Rider exists with no periods | Zero summary and useful empty state |
| 4 | Rider does not exist | 404 state, not zero debt |
| 5 | Signed bucket credit offsets another bucket | Net and collectible differ visibly |
| 6 | Debt crosses 15,000 | No automatic freeze or new forced workflow |
| 7 | Authorized user freezes valid period | Account Suspended, assignment retained, ledger event visible |
| 8 | Freeze chosen in Riyadh day near UTC boundary | Correct inclusive local day shown |
| 9 | Refresh after freeze on a later date | No new daily commission after freeze cutoff |
| 10 | Missing percentage earnings during freeze | Warning displayed; eligible freeze still available |
| 11 | Attempt clearance with incomplete commission | Action blocked/explained; server error handled |
| 12 | Frozen account in assignment picker | Cannot select or submit through alternate UI flow |
| 13 | Metadata edit on frozen account | Current rowVersion and Suspended status preserved |
| 14 | Attempt old normal close on frozen period | Forbidden action hidden or conflict handled |
| 15 | Transfer with missing/long external reference | Inline validation and no false success |
| 16 | Transfer with complete commission | Old rider debt retained; account released |
| 17 | Clear frozen account with unpaid rider | Allowed with reference; rider outstanding remains |
| 18 | Clearance form | No editable backdated effectiveAtUtc field |
| 19 | New rider assigned after clearance | New period/fee; no inherited departed rider debt |
| 20 | Old period viewed after account reuse | Original rider identity and current availability remain distinct |
| 21 | Partial collection | Correct bucket reduction and receipt; no account release |
| 22 | Fee-only countsAsSettlement=false | Receipt recorded; reminder anchor unchanged |
| 23 | Qualifying fee-only settlement=true | Commission requirements handled and anchor updated |
| 24 | Full payment on Frozen period | Payment Paid while frozen until release action |
| 25 | Confirm-paid with positive bucket | Disabled or server conflict, no fake state change |
| 26 | Confirm-paid with incomplete earnings | Cannot falsely confirm paid |
| 27 | Confirm-paid after successful payment | New key; no extra cashbox receipt |
| 28 | Payment succeeded but confirmation fails | Payment remains success; retry only confirmation |
| 29 | Old rider confirms after new assignment | Current rider assignment remains intact |
| 30 | Late valid import after paid confirmation | Current Outstanding badge overrides historical timestamp |
| 31 | Timeout during settlement | Retry same body/key; no new duplicate attempt |
| 32 | Changed payload with used key | 409 handled; state refreshed and new logical attempt reviewed |
| 33 | Idempotent replay after later mutations | Replayed result followed by fresh reads |
| 34 | Two tabs modify same period | Conflict shown; no stale optimistic availability |
| 35 | Read-only operator | Read pages work, mutation controls absent |
| 36 | Approval requester tries self-decision | No authorized decision action |
| 37 | Reset approval versus direct transfer | Separate workflows and actor rules apparent |
| 38 | Earnings zeros/free orders/correction | All fields sent; money label; history preserved |
| 39 | Import preview with blocked rows | Preview displayed; no financial posting claimed |
| 40 | Valid manual dispatch allocations | Sum and identities validated; commit not blindly disabled by cached ambiguity |
| 41 | Post-freeze import with no assignment | Issue explained; no attribution to frozen rider |
| 42 | Replacement import | Original retained/reversed, refreshed financial totals |
| 43 | Original file download | Blob and correct file id/name |
| 44 | Large preview and paged debt results | Virtualization; no fabricated total count |
| 45 | Ledger kind 6 / bad calculationJson | Status timeline, no cash sum, robust rendering |
| 46 | Cashbox reserved balance | Available computed once, no double counting |
| 47 | Cashbox submit / confirm / approve actors | Separate actor constraints, exact section amounts |
| 48 | Cashbox rejected | Reservations released after refresh |
| 49 | OverdueOnly includes a nonoverdue candidate | Badge uses returned isOverdue |
| 50 | Arabic mobile + keyboard + screen reader | Identity, amounts, dialogs, errors and actions remain usable |

## 24. Implementation and release sequence

1. Adopt typed request/response contracts and current permissions; add error/idempotency/date helpers.
2. Add lifecycle/payment/availability components, financial cards, and ledger status events.
3. Add rider summary and period details with incomplete commission handling.
4. Implement freeze/transfer/clear/confirm dialogs, collection recovery flow, and refresh map.
5. Update account picker/metadata edit safeguards, handover, legacy adoption, approvals and earnings.
6. Update import previews/allocation/correction handling and cashbox actor restrictions.
7. Run the acceptance scenarios against a test database with the new migration.
8. Deploy compatible backend, then enable frontend features for users with correct permissions. The hosted schema migration was completed on 11 October 2026; see deployment verification below.

The prior backend implementation built and passed 45 Jahez tests; four SQL-provider checks could not execute because local SQL Server LocalDB failed to start. The hosted database `db67927` received the migration on 11 October 2026 at 16:12 Riyadh time, with schema/history verification and preservation of existing financial rows: [deployment report](jahez-debt-migration-deployment-20261011.md). That database operation did not publish the updated API or implement the frontend. Browser acceptance scenarios still require implementation/testing. Features such as global debt status search/counts, a standalone sponsor dashboard, historical snapshot balances, or a generic frozen-period resume are not currently implemented endpoints.
