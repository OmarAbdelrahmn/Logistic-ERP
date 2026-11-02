# Oil-change workflow — hosted database update

On 31 October 2026, the hosted `db67927` database received:

- `20261031112328_AddOilChangeConcurrency`
- `20261031132700_AddOilChangeEditCancellationWorkflow`

Migration history and duplicate-reversal preflight passed. A server COPY_ONLY backup with checksum completed. Both migrations were rehearsed in a serializable transaction and rolled back, then applied atomically in a second transaction with a deployment lock. Post-commit verification and a second read-only check passed.

Verification confirmed operation rowversion, revision history, lifecycle/service-date/void/waste-link columns, trusted checks and foreign keys, filtered reversal uniqueness, revision/request uniqueness, and all three new permission definitions. Counts and totals for operations, material usages, vehicle expenses, external financial entries, barrel losses, barrels, stock balances and odometer readings matched before and after the schema update inside each transaction. All 1,201 original oil-change operations remain; no corrections or cancellations were executed as test data.

Evidence: [deployment report](../outputs/oil-workflow-hosted-20261031T133245624Z/report.json), backup record and business-total snapshots in that directory. Backup creation was confirmed; restore verification was not performed.

The matching API has **not** been deployed by this database update. Deploy the API and assign the required permissions before enabling the frontend actions. Database deployment does not establish end-to-end HTTP workflow or concurrent-locking acceptance.
