# Hunger CR-005 — deployment notes and front-end hand-off

- **Date:** 2026-11-11
- **State:** all eight phases implemented in the working tree. Nothing is committed, deployed, or applied to any database.
- **Contract reference:** the "CR-005 phase N" sections at the end of `docs/hunger-api-endpoints.md`.
- **Plan and open decisions:** `docs/hunger-cr005-wallet-implementation-plan.md`.

## 1. What ships

### Migrations

Application (in order):

| Migration | Change |
|---|---|
| `20261111071640_HungerCr005SealEvidenceExceptions` | Seal columns on `hunger.FinancialEvents`; tables `hunger.EvidenceFiles`, `hunger.ExceptionApprovals`; five permission definitions; backfill of existing rows to `Sealed` |
| `20261111073327_HungerCr005InvoiceFedMonthClose` | `SettlementDocuments.RiderBalanceTotal` |
| `20261111074551_HungerCr005AdjustmentApprovalDate` | `SettlementDocuments.ApprovalDate` |
| `20261111081325_HungerCr005WalletPendingDisposition` | `WalletPending` added to `CK_PlatformBusiness_Disposition` |
| `20261111082612_HungerCr005FinanceReadViews` | Schema `finance_read` and five views |

Identity: `20261111071648_GrantHungerCr005Permissions` (five grants to SYSTEM_ADMIN).

All are additive. `Down` exists for each; rolling back the first one drops the evidence and exception tables with their rows.

### Feature flag

`Features:HungerFinanceCr005` (default off). With it off, existing clients keep working. What changes regardless of the flag:

- wallet reviews ignore the typed closing balance and adjustments and use the server-derived values;
- financial events carry `approvalStatus`, and the seal, evidence, exception and custody-settlement endpoints exist;
- invoices that carry `riderBalance` generate month closes, and accounts not listed in such an invoice close at 0;
- cancelled or not-yet-sealed collected top-ups are no longer remittable;
- wallet list items have `alert`;
- reconciling an adjustment no longer fails under the persistence guard.

With it on: screenshots, zero-or-approval hand-over, close-adjustment rider entries, top-up evidence, owner-vault collected top-ups, withdrawal channel, custody proposals with `WalletPending`, the funding chain, and the custody mismatch block.

## 2. Before deploying

1. **Commit and pin.** The working tree also holds about 80 files of earlier uncommitted work. Record the commit that matches the deployed build (CR-005 F12).
2. **Run the SQL Server tests.** `LOGISTICS_HUNGER_SQL_TESTS=1 dotnet test tests/LogisticsERP.Fleet.UnitTests --filter "FullyQualifiedName~HungerSql"`. The `finance_read` view SQL has never been executed; LocalDB would not start on the development machine.
3. **Check live data against the new constraint.** `SELECT ApprovalStatus, COUNT(*)` is irrelevant before the migration, but check `SELECT Kind, Status, COUNT(*) FROM hunger.FinancialEvents GROUP BY Kind, Status` so the backfill result is known, and that no `PlatformAccountBusinessDetails` row has an unexpected disposition.
4. **Seed `hunger.FinanceConfigurations`.** It had 0 rows on 2026-11-10. Every money post fails without a configuration for the supervisor.
5. **Generate the deployment scripts.** No `database/scripts/*.sql` or `tools/Apply-*.ps1` was written for CR-005. Follow the pattern of `tools/Apply-HungerV21Migrations.ps1`.
6. **Grant the reader.** After the views exist: `GRANT SELECT ON SCHEMA::finance_read TO <reader login>`.
7. **Assign the new permissions.** Only SYSTEM_ADMIN receives them. The accountant needs `hunger.finance.seal`, `hunger.finance.funding.approve`, `hunger.finance.exception.approve`; supervisors need `hunger.finance.exception.request`; whoever confirms owner cash needs `hunger.finance.owner_collections.confirm`. None is implied by `hunger.finance.manage`.
8. **Two people.** The maker of an event cannot seal it, the requester of an exception or a funding request cannot decide it. One shared admin account cannot complete these flows.

## 3. Switching the flag on

Do it only when the front-end changes in section 4 are live. With the flag on and an old client:

- wallet review and rider swap without an image are refused;
- top-ups without two evidence ids are refused, withdrawals without channel and screenshot are refused;
- direct funding is refused;
- remittance confirmation without an explicit channel is refused;
- adjustment reconciliation without approval date and supervisor is refused.

## 4. Front-end changes (FrontProject — not edited)

### Wallet review and hand-over

- Remove the prior-close and adjustments inputs. Show `closingBalance`, `provenAdjustments` and `closingSource` from the review response. Keep confirm disabled while `canConfirm` is false.
- Balance input: three exclusive controls (+ / − / 0) over a magnitude; send one signed number.
- `walletEvidence` image is required.
- When the response has `requiresException`: open an exception request (`POST finance/exceptions`, kind `NonZeroHandover`, amount = `attributedBalance`), wait for approval, then review again with `exceptionApprovalId`.
- Rider swap review: send `multipart/form-data` with field `request` (the JSON swap request as a string) and file `walletEvidence`. Use `handoverExceptionApprovalId` when the hand-over preview has `requiresException`.

### Top-up

- Two sources only: عهدة (`TopUpAdvance`) and تحصيل من المندوب (`TopUpCollected`).
- Upload two images with `POST finance/evidence` (`WalletBefore`, `WalletAfter`) and send their ids as `evidenceBeforeId` / `evidenceAfterId`. For a requested top-up send them in the body of `execute`.
- A rejected top-up can be replaced: post the corrected one with `supersedesEventId`. It shows as `AwaitingSeal` until sealed.
- Make the top-up modal openable from the withdraw screen with the account prefilled.
- Collected top-ups no longer appear in pending collections or remittance sources.

### Withdraw and remittance

- Channel selector: STC / خزنة / بنك → `destinationChannel` `Stc` / `Cashbox` / `Bank`; upload one `Wallet` image and send `walletEvidenceId`.
- Remittance confirmation: send `destinationType = "Company"`, `destinationOwnerId`, `destinationChannel`.

### Accountant screens

- Seal queue: `GET finance/seal-queue`, with seal (optional account codes and note) and reject (reason).
- Funding: approve (edit amount, choose source account) or reject; a separate deliver action for the cashier or teller.
- Exceptions: list, approve, reject.
- Owner collections: list `OwnerCollectionReceipt` rows with `PendingReceipt` from the events list; confirm with capacity `Accountant` or `Owner`.
- Custody settlement report per supervisor, with a warning when `balanced` is false.
- Adjustment reconciliation: add approval date and supervisor.
- Invoice capture: add `riderBalance` per rider line and `riderBalanceControlTotal`; show `generatedCloses` and `closeConflictAccountIds`.

### Wallets tab

- Show `alert` as a banner or badge; add the `alertOnly` filter.
- Show the `WalletPending` disposition and explain that only a hand-over with a zero balance reopens the account.

## 5. Known gaps

| Gap | Effect |
|---|---|
| A rejected wallet delivery or return has no replacement path | The row stays rejected with its effect in place |
| A rejected collected top-up cannot be replaced once its owner cash is confirmed | Sealing the replacement returns 409 |
| An invoice close that disagrees with an existing month close is reported, not applied | The earlier close stays authoritative; no way to prefer the invoice |
| Adjustments reconciled before the flag is on get no rider entries | No backfill |
| `Kind` on financial events has no closed-list constraint | Deferred until the live Kind histogram is checked |
| A `NeedsReview` invoice has no path to `Matched` | Existing behaviour, more visible now that invoices feed closes |
| Zeroing a negative wallet after a return uses an ordinary top-up | `TopUpAdvance` records a second advance on a rider whose debt is already on the `WalletReturn`; needs a finance decision |
| Three unrelated tests fail in `LogisticsERP.Fleet.UnitTests` | One Ninja permission count, two `PlatformAccountValidationTests`; believed to come from the earlier uncommitted work, not verified on a clean checkout |

## 6. Decisions taken by default — confirm with Ghazal

| # | Decision as built |
|---|---|
| D1 | A replacement for a rejected row has no money effect until sealed; the seal reverses the old row and posts the new one together |
| D2 | An account not listed in a matched invoice with rider balances closes at 0 |
| D3 | The close-adjustment holder is decided by an explicit approval date |
| D4 | Owner-collections cash has no treasury posting; it is an expected receipt with a confirmation |
| D5 | Custody identity = posting balance vs. delivered funding minus posted top-ups; a mismatch blocks top-ups unless that exact difference is approved |
| D6 | Checker permissions are separate and not implied by the legacy manage grant; maker cannot check |
| D7 | Rows that existed before the release are treated as sealed at their creation time, with no approver |
| D8 | `WalletPending` parks an account; a hand-over proving a zero wallet reopens it |
| — | Close-adjustment entries are signed: a negative adjustment credits the holder and debits the closer |
| — | A swap passes a negative wallet to the receiver under an approved exception and never raises a custody proposal |
| — | A custody-mismatch approval is not consumed; it covers top-ups while the difference is unchanged |
