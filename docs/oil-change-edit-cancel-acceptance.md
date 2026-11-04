# Oil-change correction verification and release status

Backend implementation is present for details, edit/cancel preview and commit, revision history, permissions, stock/cost reversals, mileage reconciliation, reminder refresh and report metadata. Financial-month closing controls are excluded by request.

## Executed workflow checks

The dedicated workflow suite passed 14 executable relational tests, with one SQL Server test skipped. The executed checks cover:

- Preview rollback; filter addition and removal; current oil quantities and costs.
- Cancellation restores open-barrel stock once; replay causes no duplicate effects.
- Selecting one duplicate leaves the other operation and its mileage active.
- Date/odometer correction preserves original evidence, separates service and posting dates, and reassigns the historical rider.
- Following GPS mileage is recalculated after the earlier maintenance reading is cancelled.
- Company and external work-order totals; customer payments retained.
- Stale version, date permission and changed-preview/idempotency conflicts.
- Late injected audit failure rolls back stock, costs, lifecycle and revisions.
- Stock disappearing after preview rolls back the preceding restoration.
- Returned/depleted-barrel reversals become waste, keep the barrel closed and do not increase available stock, including when another barrel is open.

These tests use SQLite with a test-only model adapter for transactions and orchestration. The adapter simulates row versions and removes SQL Server-specific check constraints; it does not establish SQL Server locking, trigger, rowversion or constraint guarantees.

An opt-in SQL Server test creates and deletes its own isolated database. Set `LOGISTICS_OIL_WORKFLOW_SQL` to a dedicated local/test SQL Server connection and run `OilChangeWorkflowTests`. Never point this test at production. LocalDB failed to start in this environment; Docker Desktop could not start because its installation registry entry is unavailable. No production financial records were used as test fixtures.

## Release requirements

Apply the pending oil-change concurrency migration and the workflow schema/permission migration before deploying the matching API. Inspect duplicate reversal references before creating the new filtered unique indexes. Existing operation/usage records are preserved; new lifecycle fields default to active/not voided.

Database migrations were applied and verified on hosted `db67927` on 31 October 2026. See [deployment verification](oil-change-workflow-hosted-deployment-20261031.md). The API has not been deployed by that update; enable frontend actions after the matching API is available.

## Explicit outcomes

Missing mileage baselines, missing/duplicate operation readings and decreasing mileage history do not reject an edit or cancellation. Replay infers its starting mileage from remaining evidence and retains cached vehicle mileage when no evidence remains. Lower daily manual readings become corrections. Mileage-only changes after preview do not invalidate the effects hash. Moved or inconsistent barrel/layer sources can require inventory reconciliation. Customer-payment reversal is not automatic: cancelling an oil service retains the receipt and keeps a work order with payment evidence available for its financial workflow.

See [API contract](oil-change-edit-cancel-api.md) and [frontend integration](oil-change-edit-cancel-frontend.md). Test output from this session is retained under `outputs/oil-workflow-test.log`.

## Final local checks

- API build: passed with zero warnings/errors.
- Focused oil, supply-request, mileage and workflow regression: 62 passed, 2 SQL-only checks skipped.
- Maintenance API surface checks: 9 passed.
- EF pending-model check: no changes since the latest migration.
- Generated deployment SQL: `database/scripts/oil-change-edit-cancel.sql`, from `20261031095204_AllowDailyOdometerCorrections` through `20261031132700_AddOilChangeEditCancellationWorkflow`, including `20261031112328_AddOilChangeConcurrency`. Execute using deployment tooling that stops on SQL errors; duplicate-reversal preflight failures require reconciliation rather than deleting data.

The generated SQL was applied and schema-verified on the hosted database; no production business-data mutation tests were run. Concurrent locking and end-to-end HTTP verification remain release checks for an available test SQL Server and deployed API.
