# Vehicle Registered Owner — Frontend Handoff

## Delivery status

- Hosted application database `db64865`: migration `20260910164617_AddVehicleRegisteredOwnerSupplier` applied on 2026-09-10.
- Existing vehicle data was not changed or backfilled.
- Backend source and migration are complete. The updated API binary must be deployed before the new JSON fields are available from the hosted API.
- No route, HTTP verb, permission, or existing field was removed.

## Ownership rule

The registered owner is resolved as follows:

```text
registeredOwnerSupplierId != null  => selected supplier/bank is the registered owner
registeredOwnerSupplierId == null  => the vehicle sponsor is the registered owner
```

`sponsorId` continues to identify the company or person using/sponsoring the vehicle. `purchasedFromSupplierId` continues to identify the original purchase source and must not be cleared when financing finishes.

## Changed endpoints

### `POST /api/vehicles`

The existing `VehicleUpsertRequest` accepts one new nullable field:

```json
{
  "registeredOwnerSupplierId": "11111111-1111-1111-1111-111111111111"
}
```

Use the full existing vehicle request body. The value must reference an active record returned by `GET /api/vehicle-suppliers`. An unknown, archived, or invalid supplier produces the existing `404` fleet not-found response.

### `PUT /api/vehicles/{id}`

The same field is accepted when updating a vehicle. This remains a full-replacement `PUT`, so always send the value returned by the latest vehicle detail response:

- During financing: send the bank/finance supplier ID.
- After ownership transfer: send `null`.
- For a vehicle already owned by its sponsor: send `null`.

Omitting the field is equivalent to sending `null` and therefore clears the financing owner.

### `GET /api/vehicles/{id}`

`VehicleDetailResponse` now includes:

```json
{
  "registeredOwnerSupplierId": "11111111-1111-1111-1111-111111111111",
  "registeredOwnerSupplier": "مصرف الراجحي"
}
```

Both fields are `null` when the sponsor owns the vehicle. In that case, display `summary.sponsorName` as the owner.

The successful response bodies from `POST /api/vehicles` and `PUT /api/vehicles/{id}` use this same updated `VehicleDetailResponse` shape.

### `GET /api/vehicle-suppliers`

This endpoint is unchanged. Use it to populate the optional bank/financing-company selector. Banks and financing companies are maintained as normal vehicle supplier catalog records.

### Unchanged list endpoints

`GET /api/vehicles` and `GET /api/vehicles/lookup` have no response changes. Fetch `GET /api/vehicles/{id}` when registered-owner details are needed.

## Recommended form behavior

1. Add a switch such as **Financed / external registered owner**.
2. When off, submit `registeredOwnerSupplierId: null` and show the selected sponsor as owner.
3. When on, require a supplier selection and submit its ID.
4. Keep `purchasedFromSupplierId` as a separate purchase-source field.
5. Preserve and resend `registeredOwnerSupplierId` with every full vehicle update.

`ownerName` remains in the API for compatibility, but it should not be used to decide the authoritative owner. Prefer the registered-owner supplier fields, falling back to `summary.sponsorName` when they are null.

## Example: financed vehicle

```json
{
  "sponsorId": "22222222-2222-2222-2222-222222222222",
  "purchasedFromSupplierId": "33333333-3333-3333-3333-333333333333",
  "registeredOwnerSupplierId": "11111111-1111-1111-1111-111111111111"
}
```

## Example: financing completed

Read the latest vehicle, preserve all existing fields and `rowVersion`, then send the full update with:

```json
{
  "registeredOwnerSupplierId": null,
  "rowVersion": "latest-base64-row-version"
}
```

The sponsor then becomes the registered owner. `purchasedFromSupplierId` stays unchanged.
