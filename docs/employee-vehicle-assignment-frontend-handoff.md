# Frontend handoff: assign a vehicle to an employee

An administrative employee can now use the existing vehicle assignment flow. The employee keeps `isEmployee: true`. The API creates a minimal `RiderProfile` only to provide the `riderProfileId` required by the vehicle assignment contract.

## Endpoints and permission

All three calls require `Authorization: Bearer <accessToken>` and `fleet.assignments.create`.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/employees/{employeeId}/vehicle-profile` | Check whether the employee has a profile for vehicle assignment. |
| `PUT` | `/api/employees/{employeeId}/vehicle-profile` | Create the minimal profile if absent; return the existing profile otherwise. No body or `Idempotency-Key` header is needed. |
| `POST` | `/api/vehicle-assignments/take` | Assign the vehicle using the returned `riderProfileId`. Requires an `Idempotency-Key` header. |

The `GET` and `PUT` responses have the same shape:

```json
{
  "employeeId": "11111111-1111-1111-1111-111111111111",
  "isEmployee": true,
  "exists": true,
  "riderProfileId": "22222222-2222-2222-2222-222222222222"
}
```

Before a profile exists, `exists` is `false` and `riderProfileId` is `null`. Both endpoints return `404` for an unknown or archived employee. Repeating `PUT` returns the same profile ID; it does not change the employee role, status, or other details. The dedicated `/api/riders` list still excludes administrative employees.

## Frontend sequence

1. Select an employee and call `GET /api/employees/{employeeId}/vehicle-profile`. The employee list and detail responses also expose an existing rider profile ID, if already loaded.
2. If `riderProfileId` is `null`, call `PUT` on that URL when the user proceeds with assignment. Use the returned `riderProfileId`; do not send an employee update or role transition.
3. Submit `POST /api/vehicle-assignments/take` as `multipart/form-data`. Put JSON text in the `metadata` field and any optional promissory note files in repeated `promissoryFiles` fields.

Example `metadata` for an employee driving the assigned vehicle:

```json
{
  "riderProfileId": "22222222-2222-2222-2222-222222222222",
  "isRealRider": true,
  "realRider": null,
  "vehicleId": "33333333-3333-3333-3333-333333333333",
  "startedAtUtc": "2026-10-07T08:00:00Z",
  "startOdometer": 12000,
  "startCondition": 2,
  "startFuelLevelPercentage": 80,
  "permissionReference": "PERM-123",
  "reason": "Vehicle handover",
  "notes": null
}
```

Send a unique `Idempotency-Key` for a new take request and reuse that key only when retrying the same request. `startCondition` uses `1` Unknown, `2` Good, `3` Fair, `4` Damaged, or `5` Unsafe. `startFuelLevelPercentage` can be `null` or 0–100. Promissory files are optional; the employee may have at most three active files in total.

## Eligibility and errors

- A profile can exist for an administrative employee, but the employee must be `Active` to take a vehicle.
- The vehicle must be available and ready for assignment, and the employee must not already have an active vehicle assignment.
- The profile lookup and creation return `hr.not_found` (`404`) when the employee does not exist. Profile creation can return `hr.conflict` (`409`) if the profile cannot be created.
- The take call returns `fleet.rider_unavailable` or `fleet.rider_already_has_vehicle` (`409`) when the person is ineligible, and `fleet.vehicle_unavailable` (`409`) when the vehicle is ineligible. Display the API `ProblemDetails` message and reload the current data.

The existing return and switch vehicle endpoints continue to use the resulting assignment ID and row version. The response remains `RiderVehicleAssignmentResponse`, including both `employeeId` and `riderProfileId`.
