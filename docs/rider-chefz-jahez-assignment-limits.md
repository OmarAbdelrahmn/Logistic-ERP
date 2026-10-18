# Rider assignment limits — 18 October 2026

Hosted update: migration `20261018083620_AllowChefzAndJahezRiderAssignments` was applied to db67927 and verified on 18 October 2026 at 11:42 Riyadh time. All 716 assignment rows (704 open) and assignment indexes were preserved. Five riders with pre-existing multiple Jahez assignments remain for operational review. API binaries were not published by this database update. The complete frontend handoff is [one consolidated file](frontend-handoff-jahez-chefz-changes-20261018.md).

| Platforms used | Active Chefz accounts | Active Jahez accounts | Maximum total |
|---|---:|---:|---:|
| Chefz only | 2 | 0 | 2 |
| Jahez only | 0 | 1 | 1 |
| Chefz and Jahez | 2 | 1 | 3 |

Chefz includes CHEFZ, THECHEFZ, CHEFSZ and SHIFTZ; the two-account limit applies across these aliases together. A third Chefz account or second Jahez account is rejected even if the total is below three. Either platform can be assigned first. Existing assignments on other platforms retain the one-account restriction; they cannot be combined using this exception.

Chefz assignment uses `POST /api/platform-accounts/{id}/assign` or the legacy platform assignment service. Jahez assignment still uses `POST /api/jahez/v2/handovers`; generic assign/release routes remain unavailable for Jahez. Payment-model edits on assigned Chefz accounts use the same limits. The existing maximum of one Salary account per rider remains in force.

Returning an account frees only that account's slot. Approved Jahez FreeSwitch operations can replace the Jahez account while two Chefz accounts remain assigned. Existing account ownership, permissions, rider eligibility, operational blocks, financial records and history checks still apply.

The API's existing `platform.rider_account_limit_reached` conflict includes the current account list and no allowed payment models when the requested platform is full. `maximumActiveAccounts` describes total capacity for that platform combination; the separate platform caps above also apply.

Deployment requires application migration `20261018083620_AllowChefzAndJahezRiderAssignments` before the new API; it is now applied on the hosted database. The [idempotent SQL script](../database/scripts/rider-chefz-jahez-assignment-limits.sql) applies that migration after `20261017165534_AddJahezV2AccountantHandovers`. It only expands `CK_RiderClientAssignments_RiderAccountSlot` to permit slots 1, 2 and 3. Existing active-slot, active-account and Salary uniqueness indexes stay in place. Existing assignments are preserved. The [hosted report](../outputs/chefz-jahez-assignment-migration-20261018T084224487Z/report.json) records post-commit verification. Before rollback, release or move any assignments in slot 3; the previous constraint cannot accept those rows.

The new combined-assignment suite passes 36 cases, covering all four Chefz aliases, both generic assignment service paths, all assignment orders, per-platform rejection, payment-model edits, Salary limits, released-slot reuse and approved Jahez switches at full capacity. The complete Jahez project passes 152 tests with 5 optional SQL/hosted checks skipped; the focused existing Chefz/employee assignment regressions pass 31 tests and the platform API/model/policy checks pass 21 (204 passed in total). The full solution builds with zero warnings and errors, and EF reports no pending model changes. Service tests use EF InMemory; database constraints and concurrent writes have not been executed against SQL Server for this update.
