# Vehicle-to-platform account assignments

This API assigns a vehicle directly to a platform account. It is independent of the rider-to-vehicle assignment model: a record contains no rider ID and does not require the account owner to be the rider currently using the vehicle.

## Business behavior

Every structurally valid request is approved and stored. Business conflicts do not reject the assignment; they are returned as warning problems in the assignment response and through the dedicated problems endpoint.

A request is structurally valid when the vehicle and platform account IDs exist and the caller has permission. Authentication, missing references, malformed input, or concurrency conflicts can still return an error.

Active-account capacity is calculated separately for each combination of:

- vehicle;
- platform;
- platform-account operating city.

The limits are:

| Vehicle type | Maximum distinct active accounts per platform and city |
| --- | ---: |
| Car | 2 |
| Motorcycle | 3 |

Assignments to another platform have an independent limit. The account does not need to belong to the rider who currently has the vehicle. Vehicle types without a configured limit are approved with an `UnsupportedVehicleType` warning.

Sponsor and city are validation dimensions. A vehicle/account sponsor mismatch or a vehicle/account city mismatch is approved and reported as a warning. Sponsor does not create a separate capacity bucket.

## Endpoints

All endpoints require JWT authentication. Read operations require `fleet.assignments.read`; create, close, switch, and accept operations require `fleet.assignments.manage`.

### Approve an assignment

`POST /api/vehicle-platform-account-assignments`

```json
{
  "vehicleId": "00000000-0000-0000-0000-000000000001",
  "platformRiderAccountId": "00000000-0000-0000-0000-000000000002",
  "effectiveFromUtc": "2026-08-30T09:00:00Z",
  "reason": "Keeta account activation"
}
```

Returns `201 Created`. `approvalStatus` is always `Approved`. The same response includes `hasProblems` and `problems`, so the client can show warnings without blocking the operation.

Every assignment response (`POST`, list, get, problems, and close) includes these vehicle identity fields:

```json
{
  "vehicleAssetNumber": "VEH-01A04D6E",
  "vehicleRegistrationNumber": "VH-01A04D6E",
  "vehiclePlateNumberAr": "أ ب ج 1234",
  "vehiclePlateNumberEn": "ABC 1234"
}
```

### List assignments

`GET /api/vehicle-platform-account-assignments`

Optional query parameters:

- `vehicleId`
- `platformRiderAccountId`
- `platformId`
- `operatingCityId`
- `sponsorId`
- `activeOnly` (defaults to `true`)

### Get one assignment

`GET /api/vehicle-platform-account-assignments/{id}`

### List assignments with problems

`GET /api/vehicle-platform-account-assignments/problems`

This endpoint returns active assignments that currently have one or more problems. It supports the same ID filters as the list endpoint except `activeOnly`.

Problems are recalculated from current vehicle, account, sponsor, city, and active-assignment data. Correcting related data therefore removes the warning without rewriting the assignment.

### Close an assignment

`POST /api/vehicle-platform-account-assignments/{id}/close`

```json
{
  "effectiveToUtc": "2026-09-01T18:00:00Z",
  "reason": "Account stopped",
  "rowVersion": "AAAAAAAAB9E="
}
```

Closing releases the account from the active capacity calculation.

### Switch an account to another vehicle

`POST /api/vehicle-platform-account-assignments/{id}/switch`

Use `mode: "Immediate"` when the physical handover has already happened and the account should move now:

```json
{
  "targetVehicleId": "00000000-0000-0000-0000-000000000003",
  "mode": "Immediate",
  "effectiveAtUtc": "2026-09-01T10:00:00Z",
  "reason": "Vehicle replacement completed",
  "rowVersion": "AAAAAAAAB9E="
}
```

The source assignment is ended and a new approved assignment is created for the target vehicle in the same database transaction. The response has `status: "Accepted"` and identifies the new assignment in `newAssignmentId`.

Use `mode: "Pending"` when the physical handover will happen later:

```json
{
  "targetVehicleId": "00000000-0000-0000-0000-000000000003",
  "mode": "Pending",
  "effectiveAtUtc": null,
  "reason": "Awaiting physical vehicle handover",
  "rowVersion": "AAAAAAAAB9E="
}
```

This creates a pending switch without changing the current assignment. Only one pending switch is allowed for the same source assignment, and another immediate switch cannot bypass it; accept the existing request after handover instead.

### Find pending switches

`GET /api/vehicle-platform-account-assignments/switches?pendingOnly=true`

The default is `pendingOnly=true`, which is intended for the work queue used a day or two after the request. Pass `false` to include accepted switch history. Use `GET /api/vehicle-platform-account-assignments/switches/{switchId}` to retrieve one request.

Switch responses include identity details for both vehicles: `sourceVehicleAssetNumber`, `sourceVehicleRegistrationNumber` (رقم الاستمارة), `sourceVehiclePlateNumberAr`, `sourceVehiclePlateNumberEn`, and the equivalent `targetVehicle...` fields.

### Accept a pending switch

`POST /api/vehicle-platform-account-assignments/switches/{switchId}/accept`

```json
{
  "effectiveAtUtc": "2026-09-03T09:30:00Z",
  "rowVersion": "AAAAAAAAB+Q="
}
```

Accept this only after the real-world handover. If `effectiveAtUtc` is omitted, the server uses the acceptance time. Acceptance fails if the source assignment was closed or changed while the request was pending. On success, the old assignment is ended and the new target-vehicle assignment is created atomically.

## Problem response

Each assignment contains a problem array such as:

```json
{
  "code": "OperatingCityMismatch",
  "severity": "Warning",
  "message": "The vehicle and platform account operate in different cities.",
  "expected": "00000000-0000-0000-0000-000000000003",
  "actual": "00000000-0000-0000-0000-000000000004",
  "maximumAccounts": null,
  "activeAccountCount": null
}
```

Possible codes are:

- `VehicleArchived`
- `PlatformAccountArchived`
- `VehicleOperationalStatus`
- `PlatformAccountStatus`
- `UnsupportedVehicleType`
- `VehicleSponsorMissing`
- `SponsorMismatch`
- `VehicleCityMissing`
- `OperatingCityMismatch`
- `DuplicateActiveAssignment`
- `PlatformCityCapacityExceeded`

`PlatformCityCapacityExceeded` also returns `maximumAccounts` and `activeAccountCount`.
