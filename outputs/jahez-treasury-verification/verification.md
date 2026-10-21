# Jahez treasury verification — 19 October 2026

Implemented in the existing checkout, preserving its existing changes. Treasury activation remains disabled until finance attribution, backfill and verified balances pass the readiness checks. No production migration or activation was performed.

## Checks

- Solution build: passed, zero warnings and errors.
- Jahez tests: 204 passed, 16 skipped. SQL cases require an available SQL Server.
- Fleet tests: 1,088 passed, 23 skipped.
- Domain tests: 282 passed, three existing unrelated failures listed below.
- TypeScript contracts and clients: strict type check passed.
- Application and Identity EF models: no pending model changes.
- Idempotent deployment scripts regenerated through the final granular permission migrations.
- All five PDF report types generated. Arabic handover names visually inspected; no GUIDs found in extracted text of any of the five PDFs. Shared Excel/PDF calculations and Excel name/legacy GUID handling covered by the report test.

## Existing Domain failures

- `ControllerAuthorizationAuditTests.ControllersDoNotRestrictAccessByRoleName`: existing maintenance correction endpoint uses a role restriction.
- `ControllerAuthorizationAuditTests.OnlyExplicitPublicEntryPointsAreAnonymous`: existing fuel import endpoint differs from the expected allowlist.
- `FuelCardApiSurfaceTests.MonthlyUsageCarriesOneRiderAndEmployeeAndImportIsMultipart`: existing nullable rider identity differs from the test expectation.

The corresponding unrelated implementation and test files were not changed by this work.

## SQL integration limitation

SQL Server LocalDB fails during engine startup with misaligned I/O and a fatal crash, before the treasury migration can run. Docker has no running daemon. SQL constraint, rowversion, transaction rollback and concurrent confirmation tests are supplied but remain unverified here. A healthy SQL Server is required before release; use the isolated database test configuration described in `docs/jahez-treasury-v3-ar.md`.

## Export names

User display names are frozen on new postings and handover approval. Reports resolve missing legacy names from Identity without rewriting financial history. Missing users have an Arabic unavailable-name label. Rider names come from employee records. Raw internal GUIDs and transfer batch identifiers are excluded from exported content; document numbers and business references remain available.
