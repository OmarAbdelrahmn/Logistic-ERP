# Jahez account vehicle type

The Jahez V2 account row includes nullable `vehicleType` in:

- `GET /api/jahez/v2/accounts` → `items[].vehicleType`
- `GET /api/jahez/v2/accounts/{id}` → `account.vehicleType`
- The account-status update response, which uses the same account projection.

```json
{ "accountId": "<account UUID>", "vehicleType": "Motorcycle" }
```

Values match the Fleet API: `Motorcycle`, `Car`, `Van`, `Truck`, or `Other`. `null` means there is no active approved vehicle-to-account link to an existing, non-deleted vehicle. Ended and deleted links are excluded. Where multiple active links exist, the most recently approved link wins (`approvedAtUtc` descending, then link ID descending), matching the Fleet assignment list ordering. Account pagination and counts are unchanged.

The source is the direct `VehiclePlatformAccountAssignment` relationship. A linked account can expose the type even without a current rider assignment. No vehicle is inferred from the registered owner or current rider.

Use the updated `Account` interface in `jahez-v2-api-contracts.ts`. The field is read-only and requires no database migration. Deploy the API build before using it on the hosted frontend.
