# Vehicle Registered Owner — Frontend Handoff

## Delivery status

- Backend source and migration are complete as of 2026-09-14.
- Hosted application database `db67927`: migration `20260914104300_AddVehicleRegisteredOwnerSponsor` was applied and verified on 2026-09-14.
- The idempotent application deployment script was regenerated at `database/scripts/application.sql`.
- The hosted API binary must still be deployed before the new response field and sponsor-owner behavior are available from the hosted API.
- No route, HTTP verb, permission, or existing request field was removed.
- Rider and vehicle-assignment behavior is unchanged.

## What changed

The existing request field keeps its current JSON name for compatibility:

```json
{
  "registeredOwnerSupplierId": "019c18d5-62e1-7000-8000-000000000041"
}
```

Despite its legacy name, the value may now identify either:

- an active vehicle supplier returned by `GET /api/vehicle-suppliers`; or
- a sponsor returned by `GET /api/sponsors`.

The frontend does not send a separate `registeredOwnerSponsorId` field. That field is an internal database relationship used to preserve foreign-key integrity.

## Ownership model

The vehicle now keeps these relationships separate:

| Relationship | API field | Meaning |
|---|---|---|
| Actual user / operating sponsor | `sponsorId` | The sponsor currently using or operating the vehicle. |
| Explicit registered owner | `registeredOwnerSupplierId` | Legacy-named polymorphic input: accepts either a supplier ID or sponsor ID. |
| Original seller | `purchasedFromSupplierId` | The supplier from which the vehicle was purchased; it is not the current registered owner. |

The actual user and registered owner may both be sponsors. They may be the same sponsor or different sponsors.

Examples:

```text
sponsorId = Sponsor A
registeredOwnerSupplierId = Sponsor B
=> Sponsor A uses the vehicle; Sponsor B is the registered owner.

sponsorId = Sponsor A
registeredOwnerSupplierId = Sponsor A
=> Sponsor A is both the user and registered owner.

sponsorId = Sponsor A
registeredOwnerSupplierId = Supplier X
=> Sponsor A uses the vehicle; Supplier X is the registered owner.
```

## Owner resolution rules

The authoritative display rule is:

```text
registeredOwnerSupplierId != null
    => use registeredOwnerSupplier and registeredOwnerType

registeredOwnerSupplierId == null
    => no explicit owner is stored; fall back to summary.sponsorId / summary.sponsorName
```

When an explicit owner is returned, `registeredOwnerType` is:

```text
"Supplier" => registeredOwnerSupplierId identifies a vehicle supplier
"Sponsor"  => registeredOwnerSupplierId identifies a sponsor
null       => no explicit registered owner; use the vehicle sponsor fallback
```

`ownerName` remains a legacy compatibility field and must not be used as the authoritative owner.

## Request contract

### `POST /api/vehicles`

The request shape is unchanged. Continue sending the full `VehicleUpsertRequest` body.

To select a supplier as registered owner:

```json
{
  "sponsorId": "11111111-1111-1111-1111-111111111111",
  "purchasedFromSupplierId": "22222222-2222-2222-2222-222222222222",
  "registeredOwnerSupplierId": "33333333-3333-3333-3333-333333333333"
}
```

To select a sponsor as registered owner:

```json
{
  "sponsorId": "11111111-1111-1111-1111-111111111111",
  "purchasedFromSupplierId": "22222222-2222-2222-2222-222222222222",
  "registeredOwnerSupplierId": "44444444-4444-4444-4444-444444444444"
}
```

In the second example, `1111...` is the actual user/operating sponsor and `4444...` is the registered-owner sponsor.

To use the existing implicit sponsor-owner behavior:

```json
{
  "sponsorId": "11111111-1111-1111-1111-111111111111",
  "registeredOwnerSupplierId": null
}
```

### `PUT /api/vehicles/{id}`

This remains a full-replacement update. Always:

1. Fetch the latest vehicle detail.
2. Preserve every existing field.
3. Preserve `summary.rowVersion`.
4. Resend `registeredOwnerSupplierId` from the detail response, regardless of whether `registeredOwnerType` is `Supplier` or `Sponsor`.

Switching the selected owner type is safe:

- Sending a supplier ID stores the supplier relationship and clears any explicit sponsor-owner relationship.
- Sending a sponsor ID stores the sponsor relationship and clears any explicit supplier-owner relationship.
- Sending `null` clears both explicit relationships and restores the sponsor fallback behavior.

## Validation and errors

- A supplier owner must exist, must not be archived, and must have `status = Active`.
- A sponsor owner must exist and must not be archived.
- An ID that matches neither an eligible supplier nor a sponsor returns the existing fleet `404 Not Found` error.
- The existing `sponsorId`, operating-city, model, purchase-supplier, identity, odometer, and row-version validations are unchanged.
- Supplier resolution is checked first, preserving the exact existing supplier behavior.
- The database guarantees that an explicit supplier owner and explicit sponsor owner cannot both be stored for one vehicle.

## Response contract

### `GET /api/vehicles/{id}`

`VehicleDetailResponse` keeps the existing fields and adds `registeredOwnerType`.

Explicit supplier owner:

```json
{
  "summary": {
    "sponsorId": "11111111-1111-1111-1111-111111111111",
    "sponsorName": "المستخدم الفعلي"
  },
  "registeredOwnerSupplierId": "33333333-3333-3333-3333-333333333333",
  "registeredOwnerSupplier": "شركة التمويل",
  "registeredOwnerType": "Supplier"
}
```

Explicit sponsor owner:

```json
{
  "summary": {
    "sponsorId": "11111111-1111-1111-1111-111111111111",
    "sponsorName": "المستخدم الفعلي"
  },
  "registeredOwnerSupplierId": "44444444-4444-4444-4444-444444444444",
  "registeredOwnerSupplier": "المالك المسجل",
  "registeredOwnerType": "Sponsor"
}
```

Implicit sponsor owner, compatible with existing records:

```json
{
  "summary": {
    "sponsorId": "11111111-1111-1111-1111-111111111111",
    "sponsorName": "المالك والمستخدم"
  },
  "registeredOwnerSupplierId": null,
  "registeredOwnerSupplier": null,
  "registeredOwnerType": null
}
```

Successful `POST /api/vehicles`, `PUT /api/vehicles/{id}`, restore, and vehicle status actions return the same updated `VehicleDetailResponse` shape.

### Unchanged list endpoints

`GET /api/vehicles` and `GET /api/vehicles/lookup` remain unchanged. They do not return the explicit registered-owner fields. Fetch `GET /api/vehicles/{id}` when owner information is needed.

## Catalog endpoints for the form

### Supplier choices

```http
GET /api/vehicle-suppliers
```

Permission: `fleet.vehicles.read`.

Use only active suppliers. The response includes `id`, `code`, Arabic/English names, commercial-registration number, tax number, phone, address, status, and notes.

### Sponsor choices

```http
GET /api/sponsors
```

Permission: `workforce.sponsors.read`.

The response includes `id`, Arabic/English registry names, employer identity number, commercial-registration number, unified national number, sponsor type, status, contact details, address, and notes.

The frontend must handle `403 Forbidden` from the sponsor catalog if the signed-in user has vehicle-management permission but lacks `workforce.sponsors.read`. Do not silently treat that as an empty sponsor list.

## Recommended frontend model

```ts
export type RegisteredOwnerType = "Supplier" | "Sponsor" | null;

export interface VehicleDetailResponse {
  // Existing fields remain unchanged.
  registeredOwnerSupplierId: string | null;
  registeredOwnerSupplier: string | null;
  registeredOwnerType: RegisteredOwnerType;
}

export interface RegisteredOwnerOption {
  id: string;
  type: Exclude<RegisteredOwnerType, null>;
  nameAr: string;
  nameEn?: string | null;
}
```

Build the combined selector options with an explicit type:

```ts
const ownerOptions: RegisteredOwnerOption[] = [
  ...suppliers
    .filter((supplier) => supplier.status === 1)
    .map((supplier) => ({
      id: supplier.id,
      type: "Supplier" as const,
      nameAr: supplier.nameAr,
      nameEn: supplier.nameEn,
    })),
  ...sponsors.map((sponsor) => ({
    id: sponsor.id,
    type: "Sponsor" as const,
    nameAr: sponsor.registryNameAr,
    nameEn: sponsor.registryNameEn,
  })),
];
```

Send only the selected option ID:

```ts
const request = {
  ...existingVehicleForm,
  registeredOwnerSupplierId: selectedOwner?.id ?? null,
};
```

Resolve the display owner as follows:

```ts
const registeredOwner = detail.registeredOwnerSupplierId
  ? {
      id: detail.registeredOwnerSupplierId,
      name: detail.registeredOwnerSupplier,
      type: detail.registeredOwnerType,
    }
  : {
      id: detail.summary.sponsorId,
      name: detail.summary.sponsorName,
      type: "Sponsor" as const,
    };
```

## Recommended form behavior

1. Keep `sponsorId` as the separate **actual user / operating sponsor** selector.
2. Rename the registered-owner label so it does not say “supplier only”. Suggested Arabic label: **المالك المسجل**.
3. Offer supplier and sponsor tabs, an owner-type toggle, or one grouped combined selector.
4. Display an owner-type badge from `registeredOwnerType` when an explicit owner is selected.
5. Allow the same sponsor to be selected in both `sponsorId` and `registeredOwnerSupplierId`.
6. Allow a different sponsor to be selected as the registered owner.
7. Keep `purchasedFromSupplierId` separate and never overwrite it when the registered owner changes.
8. Preserve and resend the owner ID and latest row version on every full vehicle update.
9. Do not add or change any rider fields for this feature.

## Frontend acceptance checklist

- Existing supplier-owned vehicles still load, edit, and save without changes.
- A vehicle can be created with a sponsor as explicit registered owner.
- The operating sponsor and registered-owner sponsor can be different.
- The same sponsor can be both operating sponsor and registered owner.
- Editing a sponsor-owned vehicle preserves the owner selection.
- Switching Sponsor → Supplier updates the type and displayed name.
- Switching Supplier → Sponsor updates the type and displayed name.
- Clearing the explicit owner falls back to `summary.sponsorName`.
- The purchase supplier remains unchanged during every owner transition.
- Unknown or archived owner selections show the API error and do not clear the form.
- A stale update handles the existing row-version conflict flow.
- Vehicle list and lookup screens continue working without response-model changes.

## Backend persistence note

The database uses two nullable restricted foreign keys internally:

- `RegisteredOwnerSupplierId` → `VehicleSuppliers.Id`
- `RegisteredOwnerSponsorId` → `Sponsors.Id`

A check constraint prevents both from being set simultaneously. Existing supplier data is not moved or rewritten, and existing records with no explicit owner keep the original sponsor-fallback behavior.
