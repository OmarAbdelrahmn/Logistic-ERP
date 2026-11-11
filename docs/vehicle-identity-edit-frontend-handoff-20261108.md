# Vehicle update: identity fields are now editable (frontend handoff)

Date: 2026-11-08

## Endpoint

`PUT /api/vehicles/{id}`, permission `fleet.vehicles.update`. Body is the same `VehicleUpsertRequest` as before, including `rowVersion`.

## What changed

Previously, changing any of these fields returned **409** `fleet.vehicle_identity_correction_required`:

- `serialNumber`
- `chassisNumber`
- `plateNumberAr`, `plateNumberEn`
- `plateLettersAr`, `plateLettersEn`, `plateDigits`
- `registrationType`

They are now saved directly by the PUT. `assetNumber`, `vin`, `engineNumber`, vehicle type, model and manufacturer were already editable.

## Frontend work

1. Remove the "protected / read-only" state and banner from the identity section of the vehicle edit form. Make all of the fields above editable.
2. Send the full form as before, with the latest `rowVersion`.
3. Stop routing identity edits to the identity-correction flow. That endpoint still exists but is no longer required.

## Permission

Changing any identity field (the list above, plus `assetNumber`, `vin`, vehicle type, model or manufacturer) still needs `fleet.vehicles.fields.identity`. Without it the API returns **403**. Recommendation: make the identity inputs read-only when the user lacks that permission.

## Behavior notes

- **Blank required values on update:** a blank `serialNumber`, `chassisNumber`, `plateNumberAr` or `plateNumberEn` keeps the current value. Validate these as required in the form.
- **Optional parts:** `plateLettersAr`, `plateLettersEn` and `plateDigits` can be cleared by sending empty or null.
- **`registrationType`:** null keeps the current value.
- **Errors:**
  - 409 `Duplicate`: another vehicle already uses that serial, chassis, plate, asset number or VIN.
  - 409 concurrency conflict: stale `rowVersion`. Reload and retry.
  - 403: missing identity permission.
- **Audit:** direct edits are not recorded in the identity-correction history. Use the correction endpoint if a reason and history are needed.
