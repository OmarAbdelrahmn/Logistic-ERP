# Phone SIM management API

The phone SIM module keeps three distinct records:

- `PhoneSimCard`: the SIM inventory record and its one current responsible employee.
- `PhoneSimResponsibilityChange`: append-only history whenever responsibility moves to another employee.
- `RiderPhoneSimAssignment`: immutable rider handover history; an open period has `effectiveTo = null`.

The current responsible person is an active internal `Employee`. A rider handover uses a `RiderProfile` ID, not an employee ID. The underlying rider employee must be active and marked as a rider (`isEmployee = false`).

## Permissions

| Permission | Purpose |
| --- | --- |
| `phone_sims.read` | List SIMs and read responsibility and rider-assignment history. |
| `phone_sims.manage` | Create, update, transfer, assign, return, change status, and archive SIMs. |

Both permissions are included in the default `SYSTEM_ADMIN` and `MANAGER` role grants. Phone numbers are classified as sensitive operational data in the permission catalog.

## Routes

| Method | Route | Permission | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/phone-sims` | read | Paged inventory search and filters. |
| `GET` | `/api/phone-sims/{id}` | read | SIM details and current rider. |
| `POST` | `/api/phone-sims` | manage | Create a SIM and initial responsibility history. |
| `PUT` | `/api/phone-sims/{id}` | manage | Update number, ICCID, carrier, or notes. |
| `PATCH` | `/api/phone-sims/{id}/responsible-employee` | manage | Transfer responsibility with a mandatory reason. |
| `PATCH` | `/api/phone-sims/{id}/status` | manage | Set `Available`, `Suspended`, `Lost`, or `Deactivated`. |
| `PATCH` | `/api/phone-sims/{id}/archive` | manage | Soft-delete a SIM with no active rider assignment. |
| `GET` | `/api/phone-sims/{id}/responsibility-history` | read | Read responsibility changes newest first. |
| `GET` | `/api/phone-sims/{id}/assignments` | read | Read all rider handovers newest first. |
| `POST` | `/api/phone-sims/{id}/assignments` | manage | Assign an available SIM to an active rider. |
| `POST` | `/api/phone-sims/{id}/assignments/{assignmentId}/close` | manage | Return the SIM and close the assignment. |

List query parameters are `search`, `status`, `responsibleEmployeeId`, `riderProfileId`, `page`, and `pageSize`. Page size is limited to 200. The rider filter matches the current open assignment.

## Create example

```http
POST /api/phone-sims
Authorization: Bearer <token>
Content-Type: application/json

{
  "phoneNumber": "0555 123 456",
  "iccid": "8996601234567890123",
  "carrierName": "STC",
  "responsibleEmployeeId": "019d0000-0000-7000-8000-000000000001",
  "notes": "Operations pool"
}
```

Saudi formats (`05…`, `5…`, `966…`, `00966…`) and Arabic/Persian numerals normalize to canonical E.164, such as `+966555123456`. Other valid international E.164 numbers are also accepted. ICCID is optional; when supplied it must be 18–22 digits beginning with `89`. Canonical phone and ICCID values are unique among non-archived SIMs.

## Responsibility transfer

```http
PATCH /api/phone-sims/{id}/responsible-employee
Content-Type: application/json

{
  "responsibleEmployeeId": "019d0000-0000-7000-8000-000000000002",
  "reason": "Transferred to the night operations supervisor.",
  "rowVersion": "AAAAAAAAB9E="
}
```

The request updates the current employee reference and appends an immutable before/after responsibility record.

## Rider assignment and return

```http
POST /api/phone-sims/{id}/assignments
Content-Type: application/json

{
  "riderProfileId": "019d0000-0000-7000-8000-000000000101",
  "effectiveFrom": "2026-08-31",
  "reason": "Issued for active delivery duty.",
  "notes": null,
  "rowVersion": "AAAAAAAAB9E="
}
```

```http
POST /api/phone-sims/{id}/assignments/{assignmentId}/close
Content-Type: application/json

{
  "effectiveTo": "2026-08-31",
  "reason": "Returned at the end of duty.",
  "rowVersion": "AAAAAAAAB9I="
}
```

Only an `Available` SIM with no open assignment can be issued. The database independently enforces one open rider assignment per SIM. A rider may hold more than one SIM because no one-SIM-per-rider rule was requested. Assignment dates cannot be in the future, and a new period cannot overlap the previous period.

`Assigned` is derived by the assignment workflow and cannot be selected directly. Returning a SIM moves it back to `Available`. A SIM with an open assignment cannot be suspended, marked lost, deactivated, or archived until the assignment is closed.

## Concurrency and errors

Mutation responses include a base64 `rowVersion`. Send the latest value with update, transfer, status, archive, assignment, and return commands. A stale value returns HTTP `409` with error code `phone_sim.concurrency_conflict`.

Expected failures use standard problem details:

- `400`: invalid phone, ICCID, dates, status, or missing reason.
- `404`: SIM, responsible employee, rider, or assignment not found.
- `409`: duplicate identifiers, ineligible employee/rider, open-assignment conflict, or stale row version.

## Database rollout

Hosted database status (2026-08-31): both migrations below are applied to `db64865` and verified through the migration histories, table existence, permission definitions, and default role grants. Publishing the updated API application is a separate deployment step.

Application data and permissions are separate EF contexts:

- `AddPhoneSimManagement` creates SIM tables, constraints, indexes, and permission definitions.
- `GrantPhoneSimPermissions` adds the four default role grants.

The idempotent deployment scripts are regenerated in `database/scripts/application.sql` and `database/scripts/identity.sql`. Apply the application script before the identity script in the normal migration pipeline.
