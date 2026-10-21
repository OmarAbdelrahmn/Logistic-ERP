# Chefz rider iqama — frontend handoff

Date: 18 October 2026

All Chefz responses containing rider performance or settlement data now include `riderIqamaNo: string | null` alongside the rider's existing data. Wallet settlement responses also add `riderName: string`.

The value comes from `RiderProfile.Employee.IqamaNo` for the rider identified by the record's `riderProfileId`. It is the actual worker's current ERP iqama, including for historical assignments. It is not a snapshot captured when the report was imported. Missing iqama or an unavailable rider record returns `null`; there is no fallback to an account owner, `reportIdNumber`, `externalAccountId`, or `driverId`. Preserve it as a string, including leading zeros.

## Affected endpoints and JSON paths

Updated 20 October 2026: the account list (`GET /api/chefz/accounts`, used by the admin Chefz accounts screen) also returns `items[].riderIqamaNo` for the current open assignment (`effectiveTo == null`). Ended and deleted assignments are excluded. The value is `null` when no current rider or iqama is available, with no fallback to the registered account owner's identity.

Paths below are relative to the response body. All routes use the `/api/chefz` prefix.

| Method / route | New iqama field paths |
| --- | --- |
| `GET /reports/riders` | `riderIqamaNo`, `accounts[].riderIqamaNo`, `accounts[].daily[].riderIqamaNo` |
| `GET /reports/riders/all` | `riders[].riderIqamaNo` |
| `GET /performance` | `items[].riderIqamaNo` |
| `POST /performance/import` | `riders[].riderIqamaNo`, `riders[].accounts[].riderIqamaNo` |
| `POST /settlements/preview` | `riderIqamaNo` (also adds `riderName`) |
| `POST /settlements` | `riderIqamaNo` (also adds `riderName`) |
| `GET /settlements` | `items[].riderIqamaNo` (also adds `items[].riderName`) |
| `POST /settlements/orders/preview` | `riders[].riderIqamaNo`, `riders[].accounts[].riderIqamaNo` |
| `POST /settlements/orders/import` | `riders[].riderIqamaNo`, `riders[].accounts[].riderIqamaNo` |
| `GET /settlements/orders/imports/{importId}` | `riders[].riderIqamaNo`, `riders[].accounts[].riderIqamaNo` |

`GET /reports/riders` also adds top-level `riderName: string | null`. Top-level identity fields are populated when `riderId` is supplied, even when the selected period has no performance. For account-only requests they are `null`, since an account can have multiple historical workers; use each `accounts[]` entry's identity.

Management reports and sponsor statistics contain aggregate counts rather than individual riders, so their response shapes do not change.

## Frontend changes

1. Use the updated interfaces in [chefz-new-endpoints-contracts.ts](chefz-new-endpoints-contracts.ts). [chefz-accounts-and-reports-contracts.ts](chefz-accounts-and-reports-contracts.ts) already re-exports them.
2. Add an Iqama / رقم الإقامة column or detail next to the rider name in the affected reports, previews, and settlement screens. Include it in frontend-generated exports and print views.
3. Render `riderIqamaNo ?? '—'`. Keep grouping and row keys based on `riderProfileId`, `accountId`, and existing cycle identifiers. Iqama is display data, not the row key.
4. For two accounts assigned to one rider, both account entries return that rider's iqama. For one account used by different riders over time, each account/rider entry returns its own rider's iqama.

Example rider entry (other report fields omitted):

```json
{
  "riderProfileId": "11111111-1111-1111-1111-111111111111",
  "riderName": "اسم المندوب",
  "riderIqamaNo": "0123456789"
}
```

## Backend source changes

- `src/LogisticsERP.Application/Features/Chefz/ChefzContracts.cs`: performance, import rider groups, and wallet settlement contracts.
- `src/LogisticsERP.Application/Features/Chefz/ChefzReportingContracts.cs`: rider report and account/rider report contracts.
- `src/LogisticsERP.Application/Features/Chefz/ChefzOrderSettlementContracts.cs`: order settlement rider and account contracts.
- `src/LogisticsERP.Infrastructure/Chefz/ChefzService.cs`: shared bulk rider identity lookup and wallet settlement mapping.
- `src/LogisticsERP.Infrastructure/Chefz/ChefzService.Performance.cs`: daily rows and import rider groups.
- `src/LogisticsERP.Infrastructure/Chefz/ChefzService.Reporting.cs`: individual and all-riders reports.
- `src/LogisticsERP.Infrastructure/Chefz/ChefzService.OrderSettlements.cs`: order preview/import/replay/read mapping.
- `tests/LogisticsERP.Fleet.UnitTests/ChefzWorkflowTests.RiderIqama.cs`: worker-versus-owner identity, historical workers, missing values, two-account reports, and settlement lifecycle coverage.

This is an additive response change. Request parameters and routes are unchanged. No database migration is required. The backend changes must be deployed before the new field is available from the hosted API.

## Verification

- `dotnet build LogisticsERP.slnx --no-restore`: passed with zero warnings and errors.
- Chefz workflow tests: 119 passed, 12 skipped, zero failed. The four new iqama tests passed. Skipped cases require optional SQL Server/hosted database access or external workbook fixtures; hosted API behavior was not verified in this change.
