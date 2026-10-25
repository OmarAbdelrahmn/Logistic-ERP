# Ninja rider type — hosted database deployment

Applied and verified on **25 October 2026 at 09:17 Riyadh time** against hosted `db67927`.

Migration `20261024150306_AddNinjaAssignmentRiderType` is applied. Application and Identity migration histories match the current local migration files, with no pending or unknown migrations. The API application was not published by this database update.

Verified schema:

- `app.RiderClientAssignments.NinjaRiderType`: nullable `nvarchar(32)`, without a default.
- `CK_RiderClientAssignments_NinjaRiderType`: enabled and trusted, allowing the four internal/external car/motorcycle values or null.
- Existing payment model and encrypted credential storage columns require no changes for the related API additions.

A server-side `COPY_ONLY` backup with `CHECKSUM` completed before the update. The reviewed migration ran in one serializable transaction with an exclusive deployment lock, bounded lock waits and `XACT_ABORT ON`. All original column values, including rowversions, were compared in both directions inside the transaction. All **860 existing assignments were preserved**, and their new rider-type values remain null. No rider types were guessed or backfilled.

Verification ran before commit, on a new connection after commit, and again through an independent invocation that returned `AlreadyApplied` and `Verified` without reapplying the migration. Backup restore verification was not performed. No live account assignments, passwords, salaries or commission transactions were created or changed.

Evidence and tooling:

- [Deployment report](../outputs/ninja-rider-type-hosted-deployment-20261025T061751988Z/report.json)
- [Server backup completion](../outputs/ninja-rider-type-hosted-deployment-20261025T061751988Z/server-backup.xml)
- [Post-commit verification](../outputs/ninja-rider-type-hosted-deployment-20261025T061751988Z/verification-after-commit.xml)
- [Final migration histories](../outputs/ninja-rider-type-hosted-deployment-20261025T061751988Z/migration-history-after.xml)
- [Applied SQL](../outputs/ninja-rider-type-hosted-deployment-20261025T061751988Z/migration.sql)
- [Deployment tool](../tools/Apply-NinjaAssignmentRiderType.ps1): default is read-only; `-Apply` applies only this migration when pending and otherwise verifies without database writes.

The updated API and external frontend still need deployment for the assignment selectors and account response changes to become available online. See the [assignment handoff](ninja-assignment-rider-types.md) and [password response handoff](ninja-account-password-reads.md).
