# Hunger CR-005 / CR-005b — backend implementation plan

- **Date:** 2026-11-11
- **Scope:** `HungerQualityService` wallet / finance / settlement partials, their contracts, entities, controller and tests.
- **Sources:** `CR-005-hungerstation-erp-changes.md` (decisions T1–T4, T8), `CR-005b-hungerstation-wallet-impl.md` (gap map), and the local working tree (`master` @ `ea5f33b` + uncommitted changes).
- **Out of scope:** Jahez opening-fee box (T8-E2), T5–T7, payroll journals, Gate-Front code (handoff list only, §9).
- **Rule kept from CR-005b:** extend the existing ledger (`hunger.FinancialEvents` + `TreasuryPostings`). No second wallet ledger, no soft-delete of money rows, every write idempotent.

---

## Status

- Phase 1 (seal, evidence, exceptions): implemented 2026-11-11.
- Phase 2 (hand-over, invoice-fed close): implemented 2026-11-11.
- Phase 3 (close-adjustment rider entries): implemented 2026-11-11.
- Phase 4 (top-up evidence, owner-vault collected top-up, custody proposal): implemented 2026-11-11.
- Phase 5 (withdraw channel, custody proposal, WalletPending, wallet alert): implemented 2026-11-11.
- Phase 6 (funding chain, custody settlement identity): implemented 2026-11-11.
- Phase 7 (finance_read views): implemented 2026-11-11; view SQL not yet executed on SQL Server.
- Phase 8 (docs and hand-off): see `docs/hunger-cr005-deployment-and-frontend-handoff.md`.
- Phase 0 (commit, live checks, finance configuration seed) is still to do. Nothing is committed or deployed.

As-built details are at the end of `docs/hunger-api-endpoints.md`.

---

## 1. Where the code differs from what CR-005b assumed

CR-005b was written from `origin/master`. The local tree is ahead, and a few statements in its "current code" column are not accurate. The plan below is built on the real behaviour.

| CR-005b says | Actual code | Consequence |
|---|---|---|
| `ClosingBalance` is optional on the review request | `ValidateWallet` rejects the request unless `request.ClosingBalance` **equals** the Matched `MonthClose` and `request.ProvenAdjustments` equals the Matched adjustments (`HungerQualityService.Wallet.cs:57`) | The user still has to type the close. "Drop manual prior-close" is a real contract change, not only UI. |
| `WalletEvidence` reference is required | Image is optional: `ReviewWalletWithImageAsync` falls through to the no-image path, and validation only rejects `null`, so an empty string passes (`Wallet.cs:47`, `Wallet.cs:99`) | Screenshot is not enforced today. |
| Rider swap is covered by the same review | `rider-swap/review` is a JSON endpoint with no image at all (`HungerController.cs:145`) | Swap needs its own multipart path. |
| Deliver with a signed balance is "allowed" | Deliver always posts `WalletDelivery` with `RiderBalanceChange = -attributed`; swap passes `min(0, attributed)` to the receiver (`RiderSwap.cs:101`) | Zero-or-ApprovalId needs a new guard in two places. |
| `TopUpCollected` only debits custody | It also posts `RiderClearing → Collections` (supervisor collections) and is a remittance source into company cashbox/bank (`Finance.cs:160`, `Finance.cs:228`) | This directly contradicts T8-4 (owner collections never enter company books). Biggest posting change in the pack. |
| Request → Execute is the maker-checker | Execute posts immediately; nothing records approver or approval time. `finance/events` also lets a top-up be posted directly, skipping the request | Seal is a new state, not a rename. |
| Withdrawal destination = STC / cashbox / bank | `DestinationType` is `Company` or `Supervisor`; the channel is implied by the sub-account `Location` | Add an explicit channel, keep the owner type. |

Also: 79 files are uncommitted locally (wallet-review multipart, performance deletion, etc.). **Commit or stash that work before starting**; every phase below touches `HungerQualityService.Wallet.cs`, `HungerFinanceContracts.cs`, `HungerController.cs` and `HungerFinanceTests.cs`, all of which are dirty.

---

## 2. Decisions needed before coding

These change the design. Each has a recommended default so work can start; confirm with Ghazal.

| # | Question | Recommended default |
|---|---|---|
| D1 | A rejected top-up/withdrawal is replaced by a new row and the old one is cancelled when the successor is approved. Do **both** rows carry custody/wallet effect in between? | No. The successor is stored without postings; at seal, the old row's postings are reversed and the new ones applied in one transaction. Otherwise custody is debited twice until approval. |
| D2 | Account absent from a Matched invoice for a month: is its close `C = 0` or unknown? | `C = 0` once an invoice for that period is Matched; unknown (confirm blocked) until then. |
| D3 | "Approval day" of a close-adjustment: which date? | A new explicit `ApprovalDate` on the Adjustment document (not `ObservedOn`). |
| D4 | Does the owner-collections cash leg get a `TreasuryPosting`? | No. Track it as an event with a receive-confirmation step and no posting, so it never appears in company treasury balances (T8-4). |
| D5 | Custody settlement identity: what exactly is compared? | Sum of custody `TreasuryPostings` vs. (delivered Funding − all top-ups ± reversals) from events. Mismatch → warning on report, block new top-ups unless an exception approval is attached. |
| D6 | Who may approve exceptions (non-zero hand-over, custody mismatch)? | New permission `hunger.finance.exception.approve`; maker cannot approve their own request. |
| D7 | Existing 5 `FinancialEvents` on live: treat as sealed? | Yes, backfill `ApprovalStatus = 'Sealed'` with `ApprovedAtUtc = CreatedAtUtc` and a note in `DetailsJson`. Re-query live first (CR-005b §6.1). |
| D8 | New account status "unavailable" while a custody proposal is open | Add disposition value `WalletPending` to `CK_PlatformBusiness_Disposition`; Deliver is already blocked for anything other than `Normal`. |

---

## 3. Schema changes (one migration per phase, all additive)

### 3.1 `hunger.FinancialEvents` — new nullable columns

| Column | Type | Purpose |
|---|---|---|
| `ApprovalStatus` | nvarchar(16), default `'Sealed'` for old rows | `PendingSeal` / `Sealed` / `Rejected` / `Cancelled` (F2) |
| `ApprovedAtUtc`, `ApprovedByUserId` | datetimeoffset, uniqueidentifier | Seal audit (F2) |
| `RejectionReason` | nvarchar(1000) | |
| `SupersedesEventId` | uniqueidentifier FK → self | Reject → new row chain (T2-5, T3-5) |
| `Role` | nvarchar(16) | `Custody` / `Collections` / `Wallet` / `RiderClearing` / `OwnerCollections` (F9) |
| `DestinationChannel` | nvarchar(16) | `Stc` / `Cashbox` / `Bank` / `OwnerVault` (T3-3, T8-1) |
| `ExceptionApprovalId` | uniqueidentifier FK | Hand-over / mismatch exception |
| `SealAccountsJson` | nvarchar(max) | CoA codes the accountant picked at seal; ERP stores, never interprets |

Add `CK_HungerFinancialEvents_Kind` with the closed list (F4): existing kinds + `OwnerCollectionReceipt`, `CloseAdjustmentAdvance`, `CloseAdjustmentWash`, `CustodyProposal`, `FundingApproval`, `Seal`, `SealRejection`. Check the live Kind histogram before adding the constraint.

### 3.2 New tables

- **`hunger.EvidenceFiles`** — `Id, ClientPlatformId, PlatformRiderAccountId?, UploadedByUserId, StoragePath, ContentType, Purpose (WalletBefore|WalletAfter|Wallet|Arrival), ObservedOn, FinancialEventId?`. Server-issued; a file can be attached to one event only. Keeps the existing rule that only the server supplies the image link.
- **`hunger.ExceptionApprovals`** — `Id, ClientPlatformId, PlatformRiderAccountId?, SupervisorEmployeeId?, Kind (NonZeroHandover|CustodyIdentityMismatch), Amount, Reason, EvidenceReference, Status (Requested|Approved|Rejected|Consumed), RequestedByUserId, DecidedByUserId, DecidedAtUtc, ConsumedByEventId, IdempotencyKey, RequestHash`.

### 3.3 Other

- `hunger.FinanceConfigurations`: add `OwnerCollectionsLabel` nvarchar(100) (vault vs. bank account label, T8-1). No new sub-account id if D4 stands.
- `hunger.SettlementDocuments`: add `ApprovalDate` (date, Adjustment only) and `Source` (`Manual` / `Invoice`).
- `CK_PlatformBusiness_Disposition`: add `'WalletPending'` (D8). Shared table — run the Jahez/Ninja/Chefz account tests after this.
- Register new entities in `ReadWalletState` only if they affect attribution (they do not; leave the revision hash inputs unchanged so open reviews are not invalidated by uploads).

### 3.4 Permissions (PermissionKeys, both seed catalogs, one Identity migration)

`hunger.finance.seal`, `hunger.finance.funding.approve`, `hunger.finance.exception.request`, `hunger.finance.exception.approve`, `hunger.finance.owner_collections.confirm`. Add to `HungerFinancePermissions`. Update `docs/hunger-new-permissions.md`.

---

## 4. Phases

Order follows CR-005b §3 with two foundations pulled forward, because top-up, withdraw and funding all depend on them.

### Phase 0 — Prerequisites (no code)

1. Commit the dirty tree; record which SHA matches the live DLLs (CR-005 D-4, F12).
2. Re-run live counts and Kind/Status histograms (read-only).
3. Seed `FinanceConfigurations` for each HS supervisor through the existing `PUT finance/configuration`. It needs four distinct, active, posting, `Dual` treasury sub-accounts per supervisor. Until this has rows, every money post fails with "لم يعتمد ربط حسابات المشرف المالية" — nothing else in this plan is testable on live.

### Phase 1 — Foundations: evidence, seal, exceptions

**Files:** `HungerFinance.cs`, `HungerFinanceConfigurations.cs`, `HungerFinanceContracts.cs`, new `HungerQualityService.Approvals.cs`, new `HungerQualityService.Evidence.cs`, `HungerController.cs`, `PermissionKeys.cs`.

- `POST finance/evidence` (multipart, `WalletReview`/`TopUpExecute`/`WithdrawalRequest` permission): stores under `hunger/evidence/{accountId}`, same content-type and 5 MB limits as the wallet image, returns `EvidenceFileId`. Reuse `files.StoreAsync` / `DeleteBestEffort`.
- `POST finance/events/{id}/seal` and `/reject` (`hunger.finance.seal`): seal sets `ApprovalStatus`, approver, optional `SealAccountsJson`; maker ≠ checker. Reject requires a reason and leaves postings untouched.
- Supersede rule (D1): a request with `SupersedesEventId` must point at a `Rejected` row of the same kind/account/supervisor; it is stored `PendingSeal` with **no postings**. Sealing it reverses the old row's postings with `TreasuryEntryKind.Reversal` (same pattern as `ReverseRemittanceConfirmationAsync`), marks the old row `Cancelled`, then stages the new postings.
- `POST finance/exceptions` + `/approve` + `/reject`.
- `MoneyResult` / `HungerFinancialEventItem` gain `ApprovalStatus`, `ApprovedAtUtc`, `SupersedesEventId` (append as optional trailing parameters so existing callers and stored `ResultJson` still deserialize).
- All events created by existing paths get `ApprovalStatus = PendingSeal` and a `Role`.

**Tests:** seal/reject authorization, self-approval refused, supersede reversal nets to the new amount, idempotent replays.

### Phase 2 — Hand-over / return (T4)

**Files:** `HungerQualityService.Wallet.cs`, `HungerQualityService.RiderSwap.cs`, `HungerWalletReviewForm.cs`, `HungerController.cs`, `HungerFinanceContracts.cs`, `HungerQualityService.Settlements.cs`, `HungerSettlementContracts.cs`.

1. **Server-derived close.** In `ValidateWallet`, stop comparing `request.ClosingBalance` / `ProvenAdjustments` with the documents. Compute both server-side and use them in `ReviewWalletAsync`, `ConfirmWalletAsync`, `WalletPreview`. Keep the request fields (ignored) so old clients and stored `RequestJson` still parse. Response adds `ProvenAdjustments` and `ClosingSource` (`Invoice` / `Manual` / `NoPriorPeriod`). Confirm stays blocked when the close is unknown.
2. **Mandatory screenshot.** `ReviewWalletWithImageAsync` returns Invalid when `image is null`. `ReviewWalletAsync` becomes private to the image path (drop it from `IHungerFinanceService` after checking callers/tests). Convert `rider-swap/review` to multipart with one image that is stored once and referenced by both reviews.
3. **Zero or ApprovalId on Deliver.** Add `ExceptionApprovalId` to `HungerWalletReviewRequest`. In `ConfirmWalletAsync` and `ConfirmRiderSwapAsync`: if the amount attributed to the receiver ≠ 0, require an `Approved`, unconsumed `NonZeroHandover` approval for the same account with a matching amount; mark it `Consumed` in the same transaction. Preview returns `RequiresException` so the UI can open the request before confirm. Return is unaffected.
4. **Invoice-fed MonthClose.** Add optional `RiderBalance` to `HungerInvoiceSourceLine`. When an Invoice document is stored as `Matched`, for each `RiderLVL` line with an `AccountId` create a `MonthClose` (`Source = Invoice`, `WalletBalance = −RiderBalance`, `InvoiceDocumentId` set, `Status = Matched`) for the invoice period; accounts on the platform with a prior period and no line get `C = 0` (D2). If a manual Matched close already exists with a different value, store the invoice close as `NeedsReview` and surface it — never two Matched closes for one account/month (`WalletClosing` uses `SingleOrDefault`). Add a filtered unique index to enforce it. Control: Σ RiderBalance must equal the Co LVL deduction total (October: 88 lines, 33,725.16), otherwise the invoice is `NeedsReview` and no closes are generated.
5. **Balance sign control** is front-end only; the API keeps receiving one signed decimal.

**Tests:** extend `HungerFinanceTests`, `HungerWalletImageTests`, `HungerWalletImageHttpTests`: review without image rejected; close taken from invoice; duplicate close guarded; non-zero Deliver blocked / allowed with approval / approval not reusable; swap with negative carry-over needs approval.

### Phase 3 — Close-adjustment postings (T4 #4–6)

**Files:** `HungerQualityService.Settlements.cs`, new domain helper in `HungerWalletRules.cs`.

On `ReconcileAdjustmentAsync` (Adjustment → Matched), in the same transaction and with **no** `TreasuryPosting`:

- closer = rider assigned to the account on the close's `ChargeThrough`; holder = rider assigned on `ApprovalDate`.
- holder ≠ closer → `CloseAdjustmentAdvance` on the holder (`RiderBalanceChange = −|amount|`) and `CloseAdjustmentWash` on the closer (`+|amount|`).
- holder = closer → the same two events on one rider, net zero.
- no rider on either date → document goes `NeedsReview`, nothing posted.
- Idempotency key `CloseAdj:{documentId}:{Advance|Wash}`; `RelatedEventId` links to the newest open `TopUpAdvance` on the account when one exists, so the 300 → 50 timing-gap line (T4 #6) can be derived in the view rather than by mutating the top-up row.

Pure rule goes in `HungerWalletRules` with unit tests for the three cases plus the 250/300 example.

### Phase 4 — Top-up (T2, T8-E1)

**Files:** `HungerQualityService.Finance.cs`, `HungerQualityService.Receipts.cs`, `HungerFinanceContracts.cs`.

1. `HungerMoneyRequest` gains `EvidenceBeforeId`, `EvidenceAfterId`, `SupersedesEventId`. For both top-up kinds both evidence files are required, must belong to the same account, be unattached, and be `WalletBefore` / `WalletAfter`.
2. `TopUpAdvance`: unchanged posting (custody → wallet, `RiderBalanceChange = −Amount`), `Role = Custody`.
3. `TopUpCollected`: keep custody → wallet; **remove** the `RiderClearing → Collections` leg; `RiderBalanceChange` stays 0; `Role = Custody`, `DestinationChannel = OwnerVault`, and a flag in `DetailsJson` (`ExpenseLabel: "HungerWalletTopUpExpense"`) for the finance view. The rider's cash is recorded by a child event `OwnerCollectionReceipt` (status `PendingReceipt` → `Received`), confirmed through `POST finance/owner-collections/{id}/confirm` by accountant or owner, storing who and in which capacity (T8-2). No posting (D4).
4. Remove `TopUpCollected` from remittance sources for rows created after this change (`CreateRemittance`, `PendingCollectionsAsync`, `FinanceReportAsync`, `QueryWallets`). Old rows that already have a collections posting stay eligible — discriminate on `DestinationChannel IS NULL`. Put the source predicate in one shared expression; today it is copy-pasted in four places.
5. Custody short: execution stays hard-blocked. `RequestMoneyAsync` for a top-up above custody is accepted as a draft and also creates a `CustodyProposal` (Phase 5.3).
6. `finance/events` direct post for top-ups remains the supervisor's "perform + post"; result is `PendingSeal`.

**Tests:** both kinds debit custody; collected has no rider advance, no collections posting, is not remittable; missing before/after evidence rejected; legacy collected rows still remittable.

### Phase 5 — Withdraw / remittance (T3)

**Files:** `HungerQualityService.Finance.cs`, `HungerQualityService.WalletReports.cs`, `HungerQualityService.Accounts.cs`.

1. **Destination channel mandatory.** `HungerMoneyRequest.DestinationChannel` required for withdrawals and on `HungerRemittanceConfirmation`. `ValidWithdrawalDestination` maps: `Stc` → `WithdrawalSupervisor` into the supervisor's `CollectionsSubAccountId` (their custody-linked receive account, existing rule); `Cashbox` → `Location = Central`; `Bank` → `Location = Bank`. Remove the silent `?? "Company"` default in `ConfirmRemittanceAsync`.
2. **Screenshot** required on withdrawal request (one `Wallet` evidence file) in addition to the transfer reference.
3. **Custody proposal.** When a Return confirms with attributed < 0 and custody < |attributed|: create a `CustodyProposal` event (shortfall amount, `RelatedEventId` = the `WalletReturn`), and set the account disposition to `WalletPending`. The proposal does not skip T1: it only pre-fills a normal Funding request. Disposition returns to `Normal` automatically when the proposal's funding is delivered **and** a later observation shows wallet = 0.
4. **Persistent alert.** `HungerWalletItem` gains `Alert` (`None` / `WalletNotZero` / `CustodyProposalOpen`) and `HungerWalletFilter.AlertOnly`. Rule: account has no active assignment and latest observed mobile balance ≠ 0, or an open proposal exists. Clears itself; nothing to dismiss.
5. Withdrawals follow the seal flow from Phase 1; arrival confirmation stays a separate step.

**Tests:** channel/location matrix; return with short custody creates proposal and blocks Deliver; alert set and cleared.

### Phase 6 — Custody issue chain and identity (T1)

**Files:** `HungerQualityService.Receipts.cs`, `HungerQualityService.Finance.cs`.

1. Funding becomes three steps on the existing `MoneyRequest` row: `Requested` (supervisor) → `Approved` / `Rejected` (accountant, `funding.approve`; may edit amount and picks `CounterSubAccountId` from any active cashbox/bank account of the sponsor — nothing hard-wired) → `Delivered` (cashier/teller, existing `funding.execute`; this is the moment postings are created).
2. `ExecuteMoneyRequestAsync` for Funding requires `Approved` and uses the approved amount/source, not the supervisor's original. Direct `finance/events` posting of `Funding` is removed (it would bypass approval).
3. `GET finance/custody-settlement?platformId&supervisorEmployeeId`: funding delivered, top-ups by kind, reversals, posting balance, difference (D5). A non-zero difference returns a warning; top-up execution is then blocked unless an approved `CustodyIdentityMismatch` exception is supplied.

**Tests:** request → approve (edited) → deliver posts the edited amount from the chosen source; execute without approval refused; mismatch blocks top-up.

### Phase 7 — `finance_read` views (CR-005 §C)

One raw-SQL migration, shipped last so column names are final:

- schema `finance_read`; `vw_hunger_financial_events_v1` (stable names, `Role`, `ApprovalStatus`, approver, `SupersedesEventId`, channel, expense label), `vw_hunger_wallet_reviews_v1` (Direction, ExplicitBalance, ApprovalId, C, A, M, MObservedAt — extracted from `RequestJson`/`ResultJson` with `JSON_VALUE`), `vw_hunger_month_close_v1`, `vw_hunger_custody_v1`.
- Views expose every row with its status; payroll filters `ApprovalStatus = 'Sealed'`.
- `GRANT SELECT ON SCHEMA::finance_read` to the payroll login in the deployment script, not in the migration.
- Extend `HungerFinanceSqlTests` to assert the views exist and their column lists.

### Phase 8 — Docs and hand-off

Update `docs/hunger-api-endpoints.md`, `docs/hunger-new-permissions.md`, add a deployment note with the two migration sets (Application + Identity) and the published SHA.

---

## 5. Backward compatibility

- Records only get optional trailing parameters; `DetailsJson`, `RequestJson`, `ResultJson` written before the change must still deserialize (there are 26 live reviews and 5 events).
- Request hashes are computed from the serialized request. New optional fields must be omitted from JSON when null (`JsonIgnoreCondition.WhenWritingNull`, as already done for `NoShowCount`) or old idempotency keys will stop matching on replay.
- Open wallet reviews expire in 20 minutes; deploy in a quiet window rather than migrating them.
- Feature flag: put the new enforcement (mandatory image, zero-or-approval, seal required) behind `Features:HungerFinanceCr005` so the migration can ship before the front-end does.

## 6. Risks

| Risk | Mitigation |
|---|---|
| `TopUpCollected` posting change alters remittance totals mid-month | Discriminate old/new rows by `DestinationChannel`; reconcile pending-collections before and after deploy |
| Invoice-generated closes collide with manually entered closes | Unique filtered index + `NeedsReview`, never overwrite |
| Seal introduces a state every report must respect | Central `IsSealed` predicate; operational screens show all, views expose status |
| `WalletPending` on a shared disposition constraint | Run full Fleet + Jahez test projects; value is only ever set by Hunger code |
| Live host is ahead of GitHub | Phase 0 step 1 before any migration is generated |

## 7. Verification (maps to CR-005b §5)

- [ ] `TopUpAdvance`: custody ↓, rider advance, before+after evidence, `PendingSeal` until sealed.
- [ ] `TopUpCollected`: custody ↓, no rider advance, no collections posting, owner-receipt child event, not remittable.
- [ ] Deliver with attributed ≠ 0 blocked without an approved exception; approval single-use.
- [ ] Confirm blocked without a Matched close; close comes from invoice `Rider Balance`.
- [ ] Withdraw requires channel; short custody on return → proposal + `WalletPending`.
- [ ] Rejected row cancelled and reversed only when its successor is sealed.
- [ ] Funding cannot post without accountant approval and teller delivery.
- [ ] No new wallet table; all money rows are `HistoryEntity`, cancel/reverse only.
- [ ] `dotnet test` green for `LogisticsERP.Fleet.UnitTests` and `LogisticsERP.Jahez.Tests`.

## 8. Suggested commit sequence

1. Foundations schema + evidence + seal + exceptions (Phase 1)
2. Hand-over rules + invoice-fed close (Phase 2)
3. Close-adjustment events (Phase 3)
4. Top-up posting and evidence (Phase 4)
5. Withdraw channel, proposal, alert (Phase 5)
6. Funding chain + custody settlement report (Phase 6)
7. `finance_read` views (Phase 7)
8. Docs (Phase 8)

Each commit carries its own migration and tests and leaves the suite green.

## 9. Front-end hand-off (FrontProject — list only, not edited here)

- Wallet review: remove prior-close and adjustments inputs, show server `C` and its source; three exclusive sign controls (+ / − / 0) over a magnitude; image required; exception request flow when `RequiresException`.
- Rider swap review: switch to multipart with image.
- Top-up modal: two sources only (عهدة / تحصيل من المندوب), before and after screenshots, replace-rejected action; openable from the withdraw screen with the account prefilled.
- Withdraw: channel selector STC / cashbox / bank, screenshot.
- Finance: seal / reject queue for the accountant, funding approve (edit amount, choose source) and teller deliver screens, owner-collections confirm, custody settlement report.
- Wallets tab: alert banner and `AlertOnly` filter, `WalletPending` status.
