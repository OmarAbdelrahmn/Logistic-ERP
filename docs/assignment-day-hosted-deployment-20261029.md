# Hosted deployment: assignment day contributions

On 29 October 2026, application migration `20261029071925_MoveDayContributionsToAssignment` was applied to the existing hosted `db67927` database.

Preflight confirmed the expected host and database, all preceding local migrations in history, no unknown migrations, and no pre-existing contribution table. A server COPY_ONLY backup with checksum completed. The migration was first executed in a serializable transaction and rolled back, then executed and committed in a second serializable transaction with an application lock. Post-commit verification confirmed migration history, table, rowversion, two enabled trusted restrictive foreign keys, and a filtered unique index. The new table had zero rows at verification. Existing business tables are not modified by this additive migration.

Evidence: [deployment report](../outputs/assignment-day-hosted-20261029T073310598Z/report.json), server backup record, migration history, rehearsal and post-commit verification in the same directory. Backup creation was confirmed; restore verification was not performed.

The API and frontend have **not** been deployed as part of the database update. Coordinate their release; the new context endpoint and monthly fields are unavailable until the matching API is deployed. The generated SQL assumes the preceding migration `20261028111233_AddNinjaAssignmentDatePermissionCatalog` is already applied. The migration Down path refuses to discard populated contribution records; use forward corrections after use.

Frontend instructions: [frontend-assignment-day-and-monthly-usage.md](frontend-assignment-day-and-monthly-usage.md).
