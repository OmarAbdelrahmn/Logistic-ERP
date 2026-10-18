# Jahez V2 accountant handovers

Balances clear after accountant confirmation. Midnight does not clear money still awaiting handover.

`GET /api/jahez/v2/funds` returns receipts, disbursements, and net balances for movements that have **not** been included in a confirmed handover. Pending and cancelled handovers do not reduce these totals. When all outstanding movements are confirmed, every fund returns zero receipts, zero disbursements, and zero balance. Later movements remain outstanding, including movements recorded after submission with a date inside the submitted business day.

The existing movement history is preserved. `GET /money-movements` retains its inclusive date filters and current-month defaults. Each row now also has `cashboxHandoverId: string | null` (the pending or confirmed handover) and `isHandedToAccountant: boolean`. Cancelled handovers release their movements; those rows have no active handover ID. Populate balance cards from `/funds`, rather than summing the historical movement list.

## Endpoints

All paths below start with `/api/jahez/v2`. All POST requests require an `Idempotency-Key`. Permissions use the existing Jahez platform scope.

| Method | Path | Permission | Result |
|---|---|---|---|
| GET | `/cashbox/handovers?page=1&pageSize=50` | `jahez.cashbox.read` | `Page<CashboxHandover>` |
| GET | `/cashbox/handovers/{id}` | `jahez.cashbox.read` | `CashboxHandover` |
| POST | `/cashbox/handovers` | `jahez.cashbox.submit` | Pending handover |
| POST | `/cashbox/handovers/{id}/confirm` | `jahez.cashbox.confirm` | Confirmed handover; included movements clear from outstanding funds |
| POST | `/cashbox/handovers/{id}/cancel` | `jahez.cashbox.submit` | Cancelled handover; only its submitter can cancel |

Status values: `Pending=1`, `Confirmed=2`, `Cancelled=3`. These are V2 values; the V1 three-user approval workflow is not used. Accountant confirmation is the final clearing step, and the accountant must differ from the submitter.

Submit:

```json
{ "businessDate": "2026-10-17", "reason": "تسليم الأموال للمحاسب" }
```

The business date cannot be in the future. Submission snapshots all currently outstanding movements through the end of that Riyadh business date, including earlier outstanding days. There can be one pending company handover at a time. Submission with no eligible movements returns 409. Submission does not clear balances or create a payment.

The response uses the `CashboxHandover` contract in [jahez-v2-api-contracts.ts](jahez-v2-api-contracts.ts): the ID, audit metadata, Base64 `rowVersion`, date, status, submitter, exact movement count, and separate signed net amounts `cashboxAmount`, `petroAppAmount`, and `bankAmount`. Each net amount is receipts minus disbursements for that fund. Zero is valid when offsetting movements exist; a negative amount records a net outflow to reconcile. Confirming bank/PetroApp totals records reconciliation and does not initiate an external transfer.

Confirm using the exact three returned amounts and rowversion:

```json
{
  "cashboxAmount": 50,
  "petroAppAmount": 75,
  "bankAmount": -20,
  "reason": "تم الاستلام والمطابقة",
  "rowVersion": "<Base64 rowVersion from handover>"
}
```

An equal grand total with different per-fund amounts returns 400. A changed rowversion, decided handover, or second pending submission returns 409. A user confirming their own submission returns 403. Retrying the same successful command with the same key and payload is safe; permissions are rechecked before replay.

Cancel a pending handover as its submitter:

```json
{ "reason": "إلغاء وإعادة المطابقة", "rowVersion": "<Base64 rowVersion>" }
```

Cancellation retains the snapshot and membership for audit and permits submission again. Payment-method corrections to movements in pending or confirmed handovers return 409. A pending handover can be cancelled before correcting the payment method and submitting a replacement. Confirmed snapshots and membership cannot be edited or deleted.

## Frontend integration

In **الأموال وحركات الدفتر المالي**, add submission, handover history, confirmation, and cancellation actions with the permissions above. Use `/funds` for outstanding cards. Show `isHandedToAccountant` on historical rows; keep existing rider, owner, sponsor, date, and pagination fields. After successful confirmation, refetch funds, handovers, and money movements. New payments may mean the refreshed funds are nonzero even though the submitted batch was cleared.

The fetch client in [jahez-v2-client.ts](jahez-v2-client.ts) includes all five handover methods. No frontend project exists in this repository.

## Deployment

Applied to hosted database `db67927` on 17 October 2026 at 20:00 Riyadh time: `20261017165534_AddJahezV2AccountantHandovers`. It adds `jahez_v2.CashboxHandovers` and `jahez_v2.CashboxHandoverMovements`. Post-commit verification confirmed all 5 existing money movements, 19 ledger entries, and 1,845 platform accounts were preserved. Existing movements remain outstanding; no handovers were created by the migration. [Deployment evidence](../outputs/jahez-accountant-handovers-migration-20261017T170033907Z/report.json).

The API build has not been deployed as part of this migration. Deploy the current API implementation to expose the new routes. Grant `jahez.cashbox.submit` to submitters and `jahez.cashbox.confirm` to accountants using existing permission administration. The required permission definitions were verified on the hosted database; the existing catalog grants them to `SYSTEM_ADMIN`.

The applied migration-specific [jahez-v2-accountant-handovers.sql](../database/scripts/jahez-v2-accountant-handovers.sql) is available with the reusable [deployment helper](../tools/Apply-JahezAccountantHandovers.ps1). The helper supports read-only preflight and an idempotent `-Apply` command, verifies the database target, and protects existing financial rows and platform accounts in the migration transaction.

Validation: 104 Jahez tests passed, including balances across midnight, confirmation, cancellation, retries, permission checks, later transactions, payment-method locks, immutable snapshots, SQL translation, and model/migration consistency. Five opt-in database tests were skipped in that suite. The hosted read-only integration test was then explicitly enabled and passed after the migration. Transactional handover writes and concurrent confirmations have not been exercised on the hosted database. Strict TypeScript checks passed for the contracts and client. The complete endpoint handoff is [jahez-v2-money-endpoints.md](jahez-v2-money-endpoints.md).
