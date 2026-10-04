# Fuel-card city: endpoint change handoff

Every fuel card belongs to one operating city through `operatingCityId`. Existing cards are assigned to **Jeddah / جدة** by the database migration. This city belongs to the fuel card itself; changing it does not change the sponsor, rider assignment, or monthly fuel history.

## City catalog and Jeddah ID

Load `GET /api/hr-catalogs/operating-cities` with permission `operating_cities.read`. Each option includes `id`, `globalCityId`, `code`, `nameAr`, `nameEn`, `status`, `enabledFrom`, `disabledAt`, and `rowVersion`.

Display `nameAr` and submit the option's **`id`**, not its `globalCityId`.

```ts
export const JEDDAH_OPERATING_CITY_ID = "019c18d5-62e1-7000-8000-000000000003";
```

Cards already contain Arabic/English city names, so displaying a card does not require fetching the catalog. Fetch the catalog for selection and editing.

## Changed endpoints

| Endpoint | Permission | Change |
|---|---|---|
| `GET /api/fuel-cards` | `fuel.read` | Each item includes city ID/names; optional `operatingCityId` filter. |
| `GET /api/fuel-cards/{id}` | `fuel.read` | Response includes city ID/names. |
| `POST /api/fuel-cards` | `fuel.manage` | Add required `operatingCityId` to JSON; returns `201` with city ID/names. |
| `PUT /api/fuel-cards/{id}/city` | `fuel.manage` | New endpoint to change the city using the card's `rowVersion`; returns `200` with the updated card. |
| `PUT /api/fuel-cards/{id}/sponsor` | `fuel.manage` | Returned card includes city ID/names; sponsor changes preserve the city. |
| `POST /api/fuel-cards/imports` | `fuel.import` | Add optional `operatingCityId` multipart field. |
| `POST /api/fuel-cards/card-number-imports/validate` | `fuel.import` | Add optional `operatingCityId` multipart field. |
| `POST /api/fuel-cards/card-number-imports` | `fuel.import` | Add optional `operatingCityId` multipart field. |
| `POST /api/import/fuel-cards/validate` | Existing anonymous access | Add optional `operatingCityId` multipart field; each preview row includes its resulting city ID. |
| `POST /api/import/fuel-cards` | Existing anonymous access | Same city field and preview behavior. |

## Card response additions

Keep all existing `FuelCard` fields. Add these three properties:

```ts
interface FuelCardCityFields {
  operatingCityId: string;
  operatingCityNameAr: string | null;
  operatingCityNameEn: string | null;
}
```

For Jeddah:

```json
{
  "operatingCityId": "019c18d5-62e1-7000-8000-000000000003",
  "operatingCityNameAr": "جدة",
  "operatingCityNameEn": "Jeddah"
}
```

The ID is required in persisted cards. Names are resolved from the catalog, including archived catalog records; clients should fall back to the ID if a name is unavailable.

## List filter

```http
GET /api/fuel-cards?operatingCityId=019c18d5-62e1-7000-8000-000000000003&page=1&pageSize=50
```

Combine the city filter with the existing `search`, `provider`, and `riderProfileId` filters. Omit `operatingCityId` to show all cities. Filtering happens before pagination and `totalCount`; an unmatched city returns an empty page. Reset `page` to `1` when changing the filter. The page-size maximum remains `300`.

## Create a card

Add a required city selector, initially set to Jeddah if appropriate for the screen:

```http
POST /api/fuel-cards
Content-Type: application/json
```

```json
{
  "provider": "PetroApp",
  "cardNumber": "BW203",
  "plateNumberText": null,
  "notes": null,
  "sponsorId": "019c18d5-62e1-7000-8000-000000000040",
  "operatingCityId": "019c18d5-62e1-7000-8000-000000000003"
}
```

Use an existing sponsor ID. The city is mandatory for this endpoint; unlike imports, missing/empty city IDs do not default to Jeddah. Successful responses remain `201 Created` with the card and its `Location` header.

## Change a card's city

```http
PUT /api/fuel-cards/{id}/city
Content-Type: application/json
```

```json
{
  "operatingCityId": "019c18d5-62e1-7000-8000-000000000005",
  "rowVersion": "latest-card-row-version"
}
```

The example city is Riyadh. Use the card's latest `rowVersion`, not `currentRider.rowVersion`. On success, replace the local card with the response and invalidate card list/detail queries; the row version changes. The action works whether or not a rider is assigned. It preserves the sponsor, card/plate values, notes, assignment periods, and monthly usage.

## Imports

Send `operatingCityId` as an additional multipart field on all fuel-card upload and validation screens. If omitted, new cards default to Jeddah. A supplied value must identify an existing, non-deleted operating city; `Guid.Empty` is invalid.

- Detailed fuel-company report: keep `file`, `sponsorId`, and optional `expectedMonth`; add optional `operatingCityId`.
- One-column PetroApp card-number import: keep `file` and `sponsorId`; add optional `operatingCityId`. The sheet header remains `number`.
- Three-column Import module: keep `file`; add optional `operatingCityId`. Sheet headers remain `number`, `sponsor 70 number`, and `company name`. The chosen city applies to every new card in the file.

Existing matching cards keep their current city, even when the upload selects another city. Use the change-city endpoint to move them. For the three-column import, each preview row now has `operatingCityId`: new rows show the selected/default city; skipped existing rows show that card's existing city. Send the same selected city during validation and import.

## Errors and frontend behavior

| Status / error | Handling |
|---|---|
| `404 fuel.operating_city_not_found` | Unknown, empty, or soft-deleted city. Error field: `operatingCityId`. Show the Arabic `detail` and refresh city options. Imports fail before saving cards or import history. |
| `404 fuel.card_not_found` | Refresh/remove the stale card detail. |
| `409 fuel.concurrency_conflict` | Reload the card, then retry with its latest row version after the user reviews the updated data. |
| `403 fuel.forbidden` | Hide/disable the action when the user lacks its permission. |
| `400` invalid JSON/form UUID | Send a valid GUID string; use normal model-binding validation handling. |

Existing sponsor/provider/duplicate-card errors still apply. The service checks that the city exists and is not soft-deleted; it does not add a catalog-status restriction. Screens may prefer active city options while still displaying an existing card's stored city.

## Database rollout

Migration: `20261001193221_AddFuelCardOperatingCity`. The [SQL script](sql/fuel-card-operating-city.sql) applies only this migration after `20261001073011_RestrictOilBarrelsByVehicleType` and is safe to rerun.

Applied to the configured database on October 1, 2026: **340 current fuel cards, all 340 assigned to Jeddah**. The enabled, trusted foreign key and non-null column/default were verified after applying the migration. Deploy the updated API code to expose the new response fields and endpoint behavior.

It adds the city column, backfills existing rows with the seeded Jeddah operating-city ID, makes the column required, creates its index, and adds a foreign key to `app.OperatingCities` with restricted deletion. The database default is Jeddah so older application writers that omit the new column remain compatible during rollout. The new manual-create endpoint still requires an explicit city.

The script requires the Jeddah operating-city row to exist and fails if it is missing. Applying the migration refreshes existing fuel-card row versions; reload any open card details before editing. Rolling back this migration drops the city column and its relationship, losing the newly assigned city values.

Monthly usage, rider-assignment response types, and import-history responses do not gain a city field or city filter in this change.

## Verification

The API build passed with no warnings or errors. Ten fuel-card city/import behavior tests, seventeen API route/permission checks, and the card list/detail query test against the configured SQL database passed. EF reported no pending model changes. The SQL migration was rehearsed in a rolled-back transaction before application, then the 340-card Jeddah backfill and required, trusted foreign key were verified. Focused fleet tests used temporary exclusions for the existing nested scratch project and three unrelated tests with outdated service constructors; those test-project issues remain.
