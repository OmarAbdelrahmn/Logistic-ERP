# Jahez PetroApp settlement execution repair — 18 October 2026

Account: `JHZ-404986`. Operation: `01a14e94-839d-7b17-a42a-a8ae2c9d6130`.

## Confirmed database findings

Read the configured hosted application database `db67927` directly. The draft has `Fund=2` (PetroApp), `Purpose=1` (Settlement), net `-395.01`, commission `465.00`, and receipt amount `69.99` SAR.

The enabled, trusted `CK_JahezMoney_Routing` constraint on `jahez_v2.MoneyMovements` allowed PetroApp receipts only for `Purpose=2` (Fuel). A Settlement receipt therefore failed this database rule even after the domain rule allowed it. The preceding implementation missed this second enforcement layer; its InMemory tests did not exercise SQL check constraints.

`JahezWorkflowService.ExecuteAsync` maps `DbUpdateException` to the reported generic `jahez.conflict` response. This explains how a routing constraint failure can appear as a concurrency error. Server exception logs for correlations `e71add4d5ae4e6bbb9586c4b955550ee` and `f99b0bb4d24c0426e722766cb475d59d` were not accessed, so no specific logged exception stack is claimed.

At inspection, no money movement existed for the operation and no command receipt matched its execution key. The command implementation writes receipts only after successful persistence within the same transaction; this store has no Processing reservation state.

A later settlement adjustment request, `01a14eb0-eb73-73f8-9284-56a936381181`, was Pending and had moved the operation to UnderReview. The user explicitly confirmed it was created by mistake and requested removal of that approval requirement.

## Repairs applied to the hosted database

- Cancelled that specific pending request (`Status=4`) and restored its unexecuted operation to Ready (`Status=1`), using expected states, rowversions, transaction locks, and two maintenance audit entries. No manager approval was granted. Financial values stayed unchanged.
- Applied migration `20261018111059_AllowJahezPetroAppSettlementReceipts`. The routing check now accepts PetroApp receipts for Settlement or Fuel. PetroApp disbursements, fee receipts, and ordinary transferred-debt repayments remain disallowed by this rule.
- Verified the replacement constraint is enabled and trusted, its acceptance/rejection matrix matches ten expected routes, and movement/ledger counts and totals did not change during migration.
- Independently reread the database after commit: operation Ready, pending reviews zero, money movements zero, mistaken review Cancelled.

No actual collection or settlement execution was performed by this maintenance. Reload the existing draft and retry `/execute` with the same execution idempotency key. Do not create a replacement operation for the same handover.

## Validation and evidence

Jahez tests: 157 passed, 7 opt-in cases skipped. The newly added SQL Server workflow cases were also attempted explicitly, but LocalDB failed to start; they could not validate end-to-end SQL execution. Hosted constraint verification covered ten routing cases without recording financial data.

Source changes include the EF configuration, migration and model snapshot, generated deployment SQL, and SQL-provider cases for the PetroApp receipt workflow. No API binary was deployed during this repair.

Evidence directories (relative to the repository):

- `outputs/jahez-404986-execution-diagnosis-20261018/` — database diagnostics and independent post-commit JSON verification.
- `outputs/jahez-404986-cancel-mistaken-review-38744c54bd7e4f148826ae581b38f89c-applied/` — before/after XML and executed cancellation script.
- `outputs/jahez-petroapp-routing-20261018T111447059Z/` — before/after XML, applied SQL, executed migration script, and routing matrix.

Reproducible tools:

- `tools/Cancel-Jahez404986MistakenReview20261018.ps1` is narrowly guarded for the original pending request and refuses a repeat once cancelled.
- `tools/Apply-JahezPetroAppSettlementReceipts.ps1` defaults to read-only preflight; `-Apply` installs the reviewed migration atomically and records evidence.

Reverting the routing constraint requires checking for new PetroApp Settlement receipts first. SQL validation should refuse the old constraint once incompatible rows exist; do not disable or leave the constraint untrusted to force rollback.
