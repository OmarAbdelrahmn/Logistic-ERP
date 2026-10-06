# Platform account sponsor: frontend handoff

Effective contract: 4 October 2026. The platform account's existing `sponsorId` is the single account sponsor for platform accounts, Jahez handovers, account filters, and exports. The employee's HR sponsor remains a separate relationship.

## Frontend changes

1. Remove the Dashboard sponsor / كفيل لوحة التحكم selector and its validation from create and edit forms.
2. Remove `dashboardSponsorId` from request and response types, form state, defaults, saved filters, query parameters, tables, and cached account objects. Do not introduce a replacement field.
3. Keep the existing required Account sponsor / كفيل الحساب selector. Populate it from `GET /api/sponsors` (`sponsors.read`), include only `status === "Active"`, display `registryNameAr`, and submit the selected `id` as `sponsorId`.
4. For edit forms, initialize that selector from the account's existing `sponsorId`. Do not derive it from the rider's sponsor or any previously cached dashboard-sponsor value.
5. Use `sponsorNameAr` / `sponsorNameEn` from account responses wherever the account sponsor is displayed. Use `sponsorId` for both account-list endpoints' sponsor filter. Delete the old dashboard filter rather than sending both filters.
6. Invalidate/refetch account lists, account details, sponsor-filtered views, and relevant Jahez views after deploying the updated frontend. Clear the retired filter from persisted browser state.

## Affected endpoints

| Endpoint | Change |
| --- | --- |
| `POST /api/platform-accounts` | Send required `sponsorId`; no dashboard-sponsor property. |
| `PUT /api/platform-accounts/{id}` | Same sponsor field; preserve other fields and current `rowVersion`. |
| `GET /api/platform-accounts` | Filter with `sponsorId`. Response contains `sponsorId`, `sponsorNameAr`, `sponsorNameEn`. |
| `GET /api/platform-accounts/{id}` | Same single sponsor relationship in account details. |
| `POST /api/platform-operations/accounts` | Compatibility API uses the same existing `sponsorId`. |
| `PUT /api/platform-operations/accounts/{id}` | Same existing `sponsorId` and current `rowVersion`. |
| `GET /api/platform-operations/accounts` | Filter with `sponsorId`; retired query parameter removed. |
| `POST /api/jahez/handovers` | Request unchanged. It references `accountId`; the backend reads that account's `SponsorId`. |
| Platform account CSV export | Contains `SponsorId`; the retired `DashboardSponsorId` column is removed. Map CSV columns by header rather than their former numeric position. |

Use real catalog IDs in this primary-API creation body:

```json
{
  "platformId": "12121212-1212-1212-1212-121212121212",
  "operatingCityId": "13131313-1313-1313-1313-131313131313",
  "sponsorId": "14141414-1414-1414-1414-141414141414",
  "ownerRiderProfileId": "16161616-1616-1616-1616-161616161616",
  "code": "J-456469",
  "externalAccountId": "456469",
  "userName": null,
  "paymentModel": "PayPerOrder",
  "status": "Available",
  "statusReason": null,
  "acquisitionDate": null,
  "startDate": null,
  "endDate": null,
  "notes": null,
  "archiveReason": null,
  "rowVersion": null
}
```

The compatibility API uses `clientPlatformId` instead of `platformId`, `registeredEmployeeId` instead of `ownerRiderProfileId`, and `ownershipNotes` / `operationalNotes` instead of `notes` / `archiveReason`. See its complete contract in the existing Jahez handoff.

## Validation and retained rules

The primary API returns HTTP 400 for an omitted or empty `sponsorId`:

```json
{
  "status": 400,
  "detail": "حقل الكفيل مطلوب.",
  "errorCode": "platform.account_required_field",
  "field": "sponsorId",
  "errors": { "sponsorId": ["حقل الكفيل مطلوب."] }
}
```

An unknown, deleted, or inactive sponsor is rejected. Show the returned `detail` in the notification and use `field` / `errors` to attach validation to the form. The compatibility API retains its existing generic error envelope; do not assume every primary-API error code is also returned by that endpoint.

Accounts with an active assignment cannot change sponsor until released. The existing duplicate-account rule remains owner + platform + city + `sponsorId`. Jahez continues to use its handover and close workflows; general assignment endpoints still cannot bypass fees. Existing financial history, account ownership, rider assignments, and account `SponsorId` values are not rewritten by this migration.

Unknown old JSON properties or query parameters may be ignored by binding; they are not aliases for `sponsorId`. Always send the current contract.

## Backend deployment

Migration: `20261004092536_RemovePlatformAccountDashboardSponsor`. It removes only the retired account column, its index, and its foreign key. Older applied migrations are retained as history. The reviewed idempotent SQL is [remove-platform-account-dashboard-sponsor.sql](sql/remove-platform-account-dashboard-sponsor.sql).

Deploy the revised API and any running Worker before applying the removal migration. An older runtime still maps the retired column and can fail after the column is dropped. The API and Worker deployment states must be recorded separately; updating the database alone does not deploy application code.

Deployment verification on 4 October 2026: the revised API is deployed at `https://gat.premiumasp.net`, `/health/live` returns 200, and the live OpenAPI account request schemas and list filters contain `sponsorId` without the retired field. Existing hosted settings and private files were preserved. A recoverable copy of the previous hosted API is stored locally in `artifacts/sponsor-unification/previous-hosted-api.zip`.

The configured hosted database is `db67927`, confirmed against the hosted API's settings. Preflight found zero platform accounts. After a successful transaction rehearsal and rollback, the removal migration was committed on 4 October 2026 at the user's explicit request. Post-commit verification confirmed that the retired column, index, and foreign key are absent; the existing `SponsorId` column and foreign key remain intact; the migration is recorded once; and all 17 Jahez tables remain. Sponsor values and Jahez table counts stayed unchanged during the migration. The live API still returns health 200 and exposes only the existing account sponsor.

The before/after database evidence is stored locally in `artifacts/sponsor-unification/account-sponsors-before-apply.json` and `artifacts/sponsor-unification/hosted-database-after-removal.json`. The revised Worker build is available in `artifacts/sponsor-unification/worker`; its deployment state is unconfirmed. If a separate older Worker is running, redeploy/restart it with this build because its old EF model still references the removed column.

Validation passed 80 automated checks across Jahez workflows, account validation/query behavior, and the account API contracts. Two LocalDB integration tests were skipped because LocalDB is unavailable. The API and Worker both published successfully in Release configuration; EF reports no pending model changes. The ordinary Fleet test project has pre-existing constructor/temporary-subproject compilation failures, so its account checks ran from an isolated test project using the same test source files.

## Frontend acceptance checks

- Create and edit a platform account using only the existing account-sponsor selector.
- Confirm account responses and sponsor filters use only `sponsorId` and sponsor names.
- Create a Jahez handover for an account with a valid existing sponsor.
- Confirm missing/invalid sponsor errors are attached to the account-sponsor input.
- Confirm released-account sponsor edits still work and active-assignment sponsor changes remain blocked.
- Confirm CSV consumers handle the removed column by header name.
