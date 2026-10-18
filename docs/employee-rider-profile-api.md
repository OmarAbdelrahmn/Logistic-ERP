# Employee profiles for platform accounts

An employee can have a `RiderProfile` while keeping `isEmployee: true`. The profile is an identifier for account ownership, assignments, and operational data; creating it does not change the employee's role, status, engagement, sponsor, work type, or work history. The employee remains in the employee list and is excluded from the dedicated rider list.

## Create or retrieve the profile

```http
PUT /api/employees/{employeeId}/rider-profile
```

Requires `employees.update`. No request body or `Idempotency-Key` is needed. Returns `200 OK` with the existing `EmployeeDetailsResponse` shape:

```json
{
  "employee": { "id": "<employeeId>", "isEmployee": true },
  "rider": { "id": "<riderProfileId>", "employeeId": "<employeeId>", "tShirtSize": null, "operationalNotes": null, "rowVersion": "<base64>" },
  "workHistory": [],
  "housing": null
}
```

The example abbreviates the employee and rider fields. Use `rider.id` as the profile ID. Repeated calls return the same profile without overwriting its data. A missing or deleted employee returns `404` (`hr.not_found`). A profile creation conflict returns `409` (`hr.conflict`), including when a previously deleted profile still occupies the employee's unique profile slot. No database migration is required.

The existing vehicle-profile endpoint uses the same underlying profile. An employee who already has a vehicle profile receives that profile's ID.

## Read and manage data

- `GET /api/employees/{employeeId}` (`employees.read`) returns the employee and their `rider` profile, including the current profile row version.
- `GET /api/employees` (`employees.read`) includes `riderProfileId` and `riderDetails` for employees with profiles. Use this list to include employee account owners in selection controls; `GET /api/riders` continues to list people with `isEmployee: false`.
- `PUT /api/employees/{employeeId}` (`employees.update`) manages personal and employment data. Keep `isEmployee: true` when saving this person.
- `PUT /api/riders/{riderProfileId}` (`riders.update`) manages the profile's fields:

```json
{
  "tShirtSize": "Large",
  "operationalNotes": "Employee owns platform accounts",
  "rowVersion": "<current profile rowVersion>"
}
```

Use the profile row version, rather than the employee row version. A stale or missing profile row version returns `409` (`hr.concurrency_conflict`). This update preserves `isEmployee`.

## Platform accounts

Use the returned `rider.id` as `ownerRiderProfileId` in platform account create/update requests. Employees with a visible profile are eligible account owners. Existing accounts linked through `registeredEmployeeId` automatically resolve to the new profile; provisioning does not create, reassign, or alter those accounts.

The compatibility `/api/platform-operations/accounts` endpoint also accepts an employee as `registeredEmployeeId` when they have a visible profile. All other account validation and permissions still apply.

Account ownership does not create an actual-rider assignment. Assignment remains a separate operation and requires an `Active` employee. Do not call `/role-transitions` for this workflow, because it changes `isEmployee`.
