# Frontend handoff: sponsor, employee address, and vehicle-account assignments

This document describes the API changes now available in the hosted API. It covers:

1. sponsor as a required property of every platform account;
2. an optional structured address on employees and riders; and
3. vehicle-to-platform-account assignments, including non-blocking operational warnings; and
4. Keeta sponsor vehicle lease agreements.

## General rules

- Base API path: `/api`
- Send `Authorization: Bearer <accessToken>`.
- All IDs are GUIDs.
- Dates use `YYYY-MM-DD`; date-times use ISO-8601 UTC.
- `rowVersion` is an opaque Base64 concurrency token. Send the latest returned value when an update or close request requires it.
- Normal failures use `ProblemDetails`. Warnings in a vehicle/account assignment are **not** API failures.

## 1. Platform accounts now require a sponsor

`sponsorId` is required whenever a platform account is created or updated. It identifies the sponsor under which that account is registered.

This lets the same rider have multiple accounts for the same platform and city when the accounts belong to different sponsors. A suspended, retired, or archived account is historical and does not block creating a replacement account.

### Simple platform-account API (recommended frontend API)

Create or update with:

- `POST /api/platform-accounts`
- `PUT /api/platform-accounts/{id}`

Add the required `sponsorId` field:

```json
{
  "platformId": "11111111-1111-1111-1111-111111111111",
  "operatingCityId": "22222222-2222-2222-2222-222222222222",
  "sponsorId": "33333333-3333-3333-3333-333333333333",
  "ownerRiderProfileId": "44444444-4444-4444-4444-444444444444",
  "code": "KEETA-1001",
  "externalAccountId": "KT-98421",
  "userName": "keeta.account",
  "paymentModel": "PayPerOrder",
  "status": "Available",
  "statusReason": null,
  "acquisitionDate": "2026-08-30",
  "startDate": "2026-08-30",
  "endDate": null,
  "notes": null,
  "archiveReason": null,
  "rowVersion": null
}
```

Every account response now includes:

```json
{
  "sponsorId": "33333333-3333-3333-3333-333333333333",
  "sponsorNameAr": "اسم الكفيل",
  "sponsorNameEn": "Sponsor name"
}
```

List accounts with a sponsor filter:

`GET /api/platform-accounts?sponsorId={sponsorId}`

The usual platform-account list also supports `platformId`, `operatingCityId`, `ownerRiderProfileId`, `actualRiderProfileId`, `status`, `paymentModel`, `currentOnly`, and `includeArchived`.

### Compatibility platform-operations API

If the frontend still uses the older endpoint, make the same change there:

- `POST /api/platform-operations/accounts`
- `PUT /api/platform-operations/accounts/{id}`

Its request also requires `sponsorId`; its account response includes `sponsorId`, `sponsorNameAr`, and `sponsorNameEn`.

Filter with:

`GET /api/platform-operations/accounts?sponsorId={sponsorId}`

## 2. Employee and rider address

`address` is optional. Send `null` when no address is known, or send this object:

```json
{
  "buildingNumber": "1234",
  "street": "King Fahd Road",
  "district": "Al Olaya",
  "city": "Riyadh",
  "postalCode": "12211",
  "additionalNumber": "5678"
}
```

### Send `address` in these requests

- `POST /api/employees`
- `PUT /api/employees/{employeeId}`
- `POST /api/external-riders`
- `PUT /api/external-riders/{employeeId}`

### Read `address` from these responses

- `GET /api/employees` — each employee list item has `address`.
- `GET /api/employees/{employeeId}` — `employee.address`, and `rider.address` when the employee is a rider.
- Create, update, status-transition, and role-transition employee responses — use the same nested shape.
- `GET /api/riders` and `GET /api/riders/outside` — each rider has `address`.
- `GET /api/external-riders` and `GET /api/external-riders/{employeeId}` — each external-rider response has `address`.

The address is personal/contact information. It is separate from the operational city ID used to place an employee, account, or vehicle in a city.

## 3. New vehicle-to-platform-account assignment API

This is a new, separate relationship:

```text
Vehicle  <->  Platform account
```

It is **not** a rider-to-vehicle assignment and has no rider ID. The owner of the platform account does not need to be the rider currently using the vehicle.

For the complete Keeta sponsor vehicle lease workflow, including selectable vehicles, agreement lifecycle, response types, and assignment integration, see [Keeta sponsor vehicle lease frontend handoff](keeta-sponsor-vehicle-lease-frontend-handoff.md).

### Capacity rules

Capacity is calculated per **vehicle + platform + account operating city**. It is not shared between platforms and is not separated further by sponsor.

| Vehicle type | Maximum distinct active accounts for one platform and city |
| --- | ---: |
| `Car` | 2 |
| `Motorcycle` | 3 |

Examples:

- A car may be linked to two Keeta accounts in Riyadh and, at the same time, two HungerStation accounts in Riyadh.
- A motorcycle may be linked to three Keeta accounts in Riyadh.
- The platform-account owner may be different for each account.
- A vehicle/account city mismatch is allowed but reported as a warning. A sponsor mismatch is not reported when an effective Keeta sponsor vehicle lease covers that vehicle and account sponsor; otherwise it remains a warning.

### Create and approve an assignment

`POST /api/vehicle-platform-account-assignments`

Permission: `fleet.assignments.manage`

```json
{
  "vehicleId": "11111111-1111-1111-1111-111111111111",
  "platformRiderAccountId": "22222222-2222-2222-2222-222222222222",
  "effectiveFromUtc": "2026-08-30T09:00:00Z",
  "reason": "Activate Keeta account on this vehicle"
}
```

Successful result: `201 Created`.

The assignment is always returned with `approvalStatus: "Approved"` when the vehicle and platform-account IDs exist. Do not treat `hasProblems: true` as a rejected request.

Important response fields:

```json
{
  "id": "55555555-5555-5555-5555-555555555555",
  "vehicleId": "11111111-1111-1111-1111-111111111111",
  "vehicleAssetNumber": "CAR-001",
  "vehicleRegistrationNumber": "VH-01A04D6E",
  "vehiclePlateNumberAr": "أ ب ج 1234",
  "vehiclePlateNumberEn": "ABC 1234",
  "vehicleType": "Car",
  "vehicleSponsorId": "33333333-3333-3333-3333-333333333333",
  "vehicleSponsorNameAr": "اسم كفيل المركبة",
  "vehicleOperatingCityId": "66666666-6666-6666-6666-666666666666",
  "platformRiderAccountId": "22222222-2222-2222-2222-222222222222",
  "platformAccountCode": "KEETA-1001",
  "externalAccountId": "KT-98421",
  "platformId": "77777777-7777-7777-7777-777777777777",
  "platformCode": "KEETA",
  "accountSponsorId": "33333333-3333-3333-3333-333333333333",
  "accountOperatingCityId": "66666666-6666-6666-6666-666666666666",
  "accountOwnerEmployeeId": "88888888-8888-8888-8888-888888888888",
  "approvalStatus": "Approved",
  "status": "Active",
  "hasProblems": false,
  "problems": [],
  "rowVersion": "AAAAAAAAB9E="
}
```

### List assignments

`GET /api/vehicle-platform-account-assignments`

Permission: `fleet.assignments.read`

Optional query parameters:

- `vehicleId`
- `platformRiderAccountId`
- `platformId`
- `operatingCityId` — filters by the account city
- `sponsorId` — filters by the account sponsor
- `activeOnly` — defaults to `true`

### Get one assignment

`GET /api/vehicle-platform-account-assignments/{id}`

Permission: `fleet.assignments.read`

### Get active assignments that have warnings

`GET /api/vehicle-platform-account-assignments/problems`

Permission: `fleet.assignments.read`

This is the screen/dashboard endpoint for operational problems. It returns only active assignments that currently have at least one problem. It accepts the same filters as the list endpoint, except `activeOnly`.

Warnings are recalculated from current data, so fixing a vehicle/account sponsor, city, or status removes the warning automatically.

### Close an assignment

`POST /api/vehicle-platform-account-assignments/{id}/close`

Permission: `fleet.assignments.manage`

```json
{
  "effectiveToUtc": "2026-09-01T18:00:00Z",
  "reason": "Account is no longer using this vehicle",
  "rowVersion": "AAAAAAAAB9E="
}
```

Successful result: `200 OK`. Closing ends the active link and releases that account from the capacity calculation.

### Switch a platform account to another vehicle

The switch operation uses the existing **assignment ID** as the source. It does not update the existing assignment's `vehicleId`; the backend ends the old assignment and creates a new assignment for the target vehicle, preserving history.

`POST /api/vehicle-platform-account-assignments/{assignmentId}/switch`

Permission: `fleet.assignments.manage`

#### Immediate switch

Use this after the physical handover is already complete. The source assignment ends and the target assignment is created immediately.

```json
{
  "targetVehicleId": "33333333-3333-3333-3333-333333333333",
  "mode": "Immediate",
  "effectiveAtUtc": "2026-09-03T09:30:00Z",
  "reason": "Physical vehicle replacement completed",
  "rowVersion": "AAAAAAAAB9E="
}
```

`effectiveAtUtc` may be omitted to use the server time. It must not be in the future and must not precede the source assignment's `assignedAtUtc`.

Success: `201 Created`, with a switch response whose `status` is `Accepted`. Use `newAssignmentId` to load or select the new active assignment.

#### Pending switch

Use this when the replacement will happen later, for example after one or two days. The current assignment remains active until someone confirms the real-world handover.

```json
{
  "targetVehicleId": "33333333-3333-3333-3333-333333333333",
  "mode": "Pending",
  "effectiveAtUtc": null,
  "reason": "Waiting for the physical handover",
  "rowVersion": "AAAAAAAAB9E="
}
```

For a pending request, omit `effectiveAtUtc` or send `null`. Success is `201 Created`, with `status: "Pending"`. Do not update the active-assignment UI after this response.

Only one pending switch may exist for a source assignment. The UI should disable both switch actions for that assignment and offer a link to its pending request. A direct switch cannot bypass an existing pending request.

### Pending-switch work queue

`GET /api/vehicle-platform-account-assignments/switches?pendingOnly=true`

Permission: `fleet.assignments.read`

This is the page the operations user opens when the physical handover happens. `pendingOnly` defaults to `true`; pass `false` to include accepted switch history.

Each item has this shape:

```json
{
  "id": "44444444-4444-4444-4444-444444444444",
  "sourceAssignmentId": "55555555-5555-5555-5555-555555555555",
  "sourceVehicleId": "11111111-1111-1111-1111-111111111111",
  "sourceVehicleAssetNumber": "CAR-001",
  "sourceVehicleRegistrationNumber": "VH-01A04D6E",
  "sourceVehiclePlateNumberAr": "أ ب ج 1234",
  "sourceVehiclePlateNumberEn": "ABC 1234",
  "targetVehicleId": "33333333-3333-3333-3333-333333333333",
  "targetVehicleAssetNumber": "CAR-009",
  "targetVehicleRegistrationNumber": "VH-09B11C2F",
  "targetVehiclePlateNumberAr": "د هـ و 5678",
  "targetVehiclePlateNumberEn": "DEF 5678",
  "platformRiderAccountId": "22222222-2222-2222-2222-222222222222",
  "platformAccountCode": "KEETA-1001",
  "mode": "Pending",
  "status": "Pending",
  "reason": "Waiting for the physical handover",
  "requestedAtUtc": "2026-09-01T09:00:00Z",
  "requestedByUserId": "66666666-6666-6666-6666-666666666666",
  "effectiveAtUtc": null,
  "acceptedAtUtc": null,
  "acceptedByUserId": null,
  "newAssignmentId": null,
  "rowVersion": "AAAAAAAAB+Q="
}
```

Use `GET /api/vehicle-platform-account-assignments/switches/{switchId}` when opening a detail page or refreshing a specific item.

### Accept the physical handover

`POST /api/vehicle-platform-account-assignments/switches/{switchId}/accept`

Permission: `fleet.assignments.manage`

```json
{
  "effectiveAtUtc": "2026-09-03T09:30:00Z",
  "rowVersion": "AAAAAAAAB+Q="
}
```

Send the **switch request's** `rowVersion` from the pending-switch response, not the source assignment's row version. `effectiveAtUtc` is optional and defaults to server time. On `200 OK`, the response becomes `status: "Accepted"`, has `acceptedAtUtc`, `acceptedByUserId`, and `newAssignmentId`, and the source assignment is no longer active.

After a successful acceptance:

- Remove the request from the pending queue.
- Refresh the source vehicle's active assignments.
- Refresh the target vehicle's active assignments.
- Navigate to or select `newAssignmentId` if the user needs the resulting assignment details or warnings.

### Switch error handling

Treat `409 Conflict` as an operational refresh requirement. It is returned when an assignment/switch row version is stale, the source assignment was already ended, the request is no longer pending, or another pending switch already exists. Reload the affected assignment or switch request instead of retrying with the old `rowVersion`.

`400 Bad Request` means the payload is malformed, including an unknown `mode`, a missing reason, or a non-null effective time on a pending request. A same-vehicle switch, a future/past-invalid effective time, and any workflow-state conflict return `409 Conflict`. `404 Not Found` means the assignment, switch request, or target vehicle no longer exists.

## Warning UX requirements

When the create response has `hasProblems: true`, save/show the successful assignment and display its warnings. Do not present it as a failed operation.

Each warning has this shape:

```json
{
  "code": "OperatingCityMismatch",
  "severity": "Warning",
  "message": "The vehicle and platform account are assigned to different operating cities.",
  "expected": "66666666-6666-6666-6666-666666666666",
  "actual": "99999999-9999-9999-9999-999999999999",
  "maximumAccounts": null,
  "activeAccountCount": null
}
```

`expected` and `actual` can contain IDs or status values. Use the descriptive sponsor/city fields already present in the assignment response to show user-friendly names.

| Warning code | Meaning | Frontend action |
| --- | --- | --- |
| `VehicleArchived` | Vehicle was archived. | Show warning; review vehicle record. |
| `PlatformAccountArchived` | Account was archived. | Show warning; review account record. |
| `VehicleOperationalStatus` | Vehicle is not `Available` or `Assigned`. | Show its current status. |
| `PlatformAccountStatus` | Account is not `Available` or `Assigned`. | Show its current status. |
| `UnsupportedVehicleType` | No car/motorcycle capacity policy applies. | Show warning and allow the approved result. |
| `VehicleSponsorMissing` | Vehicle has no sponsor. | Prompt for vehicle sponsor data. |
| `SponsorMismatch` | Vehicle sponsor differs from account sponsor and no effective Keeta sponsor vehicle lease applies. | Highlight both sponsors or create/review the sponsor vehicle lease agreement. |
| `VehicleCityMissing` | Vehicle has no operating city. | Prompt for vehicle city data. |
| `OperatingCityMismatch` | Vehicle city differs from account city. | Highlight both cities. |
| `DuplicateActiveAssignment` | Same vehicle/account was linked more than once. | Show duplicate warning; close the extra record if appropriate. |
| `PlatformCityCapacityExceeded` | Car/motorcycle account limit is exceeded. | Show `maximumAccounts` and `activeAccountCount`; close or review an account if desired. |

## Frontend implementation checklist

- Make `sponsorId` mandatory in platform-account create/edit forms and show sponsor details in lists.
- Add the nullable `address` object to employee and external-rider forms and detail views.
- Create a separate vehicle-account assignment screen; do not reuse rider-vehicle assignment payloads or rules.
- Treat a `201 Created` vehicle assignment as success even when it contains warnings.
- Add a problems view using `/api/vehicle-platform-account-assignments/problems`.
- Send the returned `rowVersion` when closing a vehicle-account assignment.
- Add an immediate/pending switch choice to each active assignment. Pending requests need a dedicated queue and an explicit accept action.
- Store the row version separately for assignments and pending-switch requests; accept uses the pending-switch row version.
- Display `vehicleRegistrationNumber` (رقم الاستمارة) and the Arabic/English plate fields on assignment cards. For switch items, use the corresponding `sourceVehicle...` and `targetVehicle...` fields.
