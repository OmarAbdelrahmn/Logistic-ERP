# Debt collection methods — 31 October 2026

Debt repayments now accept the same incoming methods as settlement receipts: Cashbox (1) and PetroApp (2). Individual full repayments, rider full repayments, and partial repayments share the domain routing rule. Purpose remains TransferredDebtRepayment (4), including for PetroApp; allocations and debt balances keep their existing behavior. Bank receipts remain unavailable, matching settlement receipt rules.

Updated domain routing, EF configuration/model snapshot, migration `20261031143205_AllowJahezPetroAppDebtReceipts`, workflow tests, and [frontend handoff](jahez-rider-debt-partial-payments.md). No frontend application source exists in this repository; its debt dialogs must offer both incoming methods. Updated API binaries have not been deployed by this task and are required for future PetroApp debt receipt requests through the hosted API.

The hosted database migration was applied and independently reread after commit. Its routing constraint remains enabled and trusted. A transaction rehearsal validated 40 incoming/outgoing routing combinations and performed then rolled back the exact receipt correction. The final transaction preserved money movement count and total amount.

User-confirmed correction committed: Jahez account 456433, movement `01a19250-7397-7943-828b-7e919ff916b3`, reference `JHZ/2026/10/00821`, amount 40.66 SAR, Cashbox → PetroApp. All other receipt fields and the fully paid debt were preserved. Treasury is inactive and no posting or non-cancelled cashbox handover includes the receipt. No treasury activation or posting was created. One zero-amount status entry, one maintenance audit entry, and a Jahez dataset version increment record the correction.

Validation: Jahez tests passed 318, skipped 34 opt-in SQL/hosted cases, failed 0. `dotnet ef migrations has-pending-model-changes` reported no pending model changes. Provider validation used the real hosted SQL routing constraint in a rolled-back transaction; the skipped tests do not establish full SQL workflow coverage.

Evidence:

- [Rehearsal](../outputs/jahez-debt-method-9e477d47d545497f8392fb392ce3aac4-validation/verification.xml)
- [Committed verification](../outputs/jahez-debt-method-ab7fd4e05b54428882f7957bf80ae4e0-applied/post-commit.xml)
- [Guarded deployment and correction tool](../tools/Apply-JahezPetroAppDebtReceipts.ps1)
- [Migration SQL](../database/scripts/jahez-petroapp-debt-receipts.sql)

Rollback of the migration is blocked by SQL constraint validation after PetroApp debt receipts exist. Correct those receipts through an audited forward correction before attempting rollback; do not disable the constraint. The guarded tool refuses repeat execution once the migration is recorded.
