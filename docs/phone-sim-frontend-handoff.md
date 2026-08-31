# Frontend handoff: phone SIM inventory and rider assignments

## Delivery status

- Backend implementation: complete in this repository.
- Hosted database `db64865`: application and identity migrations applied and verified on 2026-08-31.
- Hosted API binary: this handoff does not claim the new controller has been published. Deploy the updated API build before enabling the frontend route against `https://gate.premiumasp.net`.
- Base route after API deployment: `/api/phone-sims`.

The longer backend reference is [phone-sim-management-api.md](phone-sim-management-api.md). This document is the frontend implementation contract.

## Product model

A SIM has two separate people:

1. `responsibleEmployeeId`: the active internal employee responsible for the inventory item.
2. `currentRider`: the rider currently holding/using the SIM, or `null` when it is not issued.

Do not display the responsible employee as the rider. Responsibility changes and rider assignments have separate history endpoints.

The backend permits multiple active SIMs for the same rider. It only prevents one SIM from having more than one open rider assignment.

## Authorization and prerequisite lookups

| Permission | UI capability |
| --- | --- |
| `phone_sims.read` | Inventory list, details, responsibility history, and rider assignment history. |
| `phone_sims.manage` | Create/edit, transfer responsibility, change status, archive, assign, and return. |

The default `MANAGER` and `SYSTEM_ADMIN` roles have both permissions after the identity migration.

Lookup dependencies:

- Responsible employee: use `GET /api/employees`; allow records with `isEmployee: true` and status `Active` or `OnLeave`. Send the employee `id`.
- Rider: use `GET /api/riders`; send the rider response `id` as `riderProfileId`, not its `employeeId`.
- Loading those selectors also requires the existing `employees.read` and `riders.read` permissions.

## Suggested TypeScript types

```ts
export type PhoneSimStatus =
  | "Available"
  | "Assigned"
  | "Suspended"
  | "Lost"
  | "Deactivated";

export type PhoneSimCurrentRider = {
  assignmentId: string;
  riderProfileId: string;
  employeeId: string;
  fullNameAr: string;
  fullNameEn: string | null;
  effectiveFrom: string; // YYYY-MM-DD
  rowVersion: string;
};

export type PhoneSim = {
  id: string;
  phoneNumber: string; // canonical E.164, e.g. +966555123456
  iccid: string | null;
  carrierName: string | null;
  status: PhoneSimStatus;
  statusReason: string | null;
  responsibleEmployeeId: string;
  responsibleEmployeeNameAr: string;
  responsibleEmployeeNameEn: string | null;
  currentRider: PhoneSimCurrentRider | null;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  rowVersion: string;
};

export type PhoneSimPage = {
  items: PhoneSim[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type PhoneSimAssignment = {
  id: string;
  phoneSimCardId: string;
  phoneNumber: string;
  riderProfileId: string;
  employeeId: string;
  riderNameAr: string;
  riderNameEn: string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  assignmentReason: string | null;
  endReason: string | null;
  notes: string | null;
  assignedByUserId: string;
  closedByUserId: string | null;
  rowVersion: string;
};

export type PhoneSimResponsibilityChange = {
  id: string;
  phoneSimCardId: string;
  previousResponsibleEmployeeId: string | null;
  previousResponsibleEmployeeNameAr: string | null;
  previousResponsibleEmployeeNameEn: string | null;
  responsibleEmployeeId: string;
  responsibleEmployeeNameAr: string;
  responsibleEmployeeNameEn: string | null;
  changedAtUtc: string;
  changedByUserId: string;
  reason: string;
};
```

## Inventory list

`GET /api/phone-sims`

Optional query parameters:

- `search`: phone number, ICCID, or carrier.
- `status`: one exact `PhoneSimStatus` value.
- `responsibleEmployeeId`: current responsible employee.
- `riderProfileId`: current open rider assignment.
- `page`: defaults to `1`.
- `pageSize`: defaults to `50`, maximum `200`.

Recommended table columns:

- Phone number
- Carrier
- ICCID
- Status
- Responsible employee
- Current rider
- Assignment start date
- Row actions

Use both label and color for statuses. `Assigned` is backend-derived and should not be offered in the manual status dropdown.

## Create and edit

Create:

```http
POST /api/phone-sims
```

```ts
type CreatePhoneSimRequest = {
  phoneNumber: string;
  iccid: string | null;
  carrierName: string | null;
  responsibleEmployeeId: string;
  notes: string | null;
};
```

Update inventory details:

```http
PUT /api/phone-sims/{id}
```

```ts
type UpdatePhoneSimRequest = {
  phoneNumber: string;
  iccid: string | null;
  carrierName: string | null;
  notes: string | null;
  rowVersion: string;
};
```

The edit endpoint intentionally does not change the responsible employee or lifecycle status. Use their dedicated commands.

Phone input may use Saudi local formats, international E.164, Arabic digits, spaces, or dashes. After saving, replace the local row with the returned object because the backend returns the canonical number. ICCID is optional; if supplied it must be 18–22 digits beginning with `89`.

## Transfer responsibility

```http
PATCH /api/phone-sims/{id}/responsible-employee
```

```json
{
  "responsibleEmployeeId": "employee-guid",
  "reason": "Transferred to the evening-shift supervisor.",
  "rowVersion": "latest SIM rowVersion"
}
```

The employee must differ from the current responsible employee. Require a reason in the dialog. On success, replace the inventory row with the returned `PhoneSim` and refresh:

```http
GET /api/phone-sims/{id}/responsibility-history
```

History is newest first.

## Assign to a rider

Only enable the assign action when `status === "Available"` and `currentRider === null`.

```http
POST /api/phone-sims/{id}/assignments
```

```json
{
  "riderProfileId": "rider-profile-guid",
  "effectiveFrom": "2026-08-31",
  "reason": "Issued for active delivery duty.",
  "notes": null,
  "rowVersion": "latest SIM rowVersion"
}
```

Rules:

- `riderProfileId` is required and must belong to an active rider.
- `effectiveFrom` is a Riyadh calendar date and cannot be in the future.
- Reason is required.
- On success, refresh `GET /api/phone-sims/{id}`. The SIM becomes `Assigned` and gains `currentRider`.

## Return from a rider

Use `currentRider.assignmentId` and `currentRider.rowVersion` from the latest SIM response.

```http
POST /api/phone-sims/{id}/assignments/{assignmentId}/close
```

```json
{
  "effectiveTo": "2026-08-31",
  "reason": "Returned after the rider left the shift.",
  "rowVersion": "latest assignment rowVersion"
}
```

The end date cannot be before `effectiveFrom` or in the future. On success, the SIM becomes `Available`; refresh the SIM detail and assignment history.

Assignment history:

```http
GET /api/phone-sims/{id}/assignments
```

History is newest first and includes open and closed assignments.

## Manual status and archive

```http
PATCH /api/phone-sims/{id}/status
```

```json
{
  "status": "Suspended",
  "reason": "Carrier temporarily suspended the line.",
  "rowVersion": "latest SIM rowVersion"
}
```

The manual status selector may contain:

- `Available`
- `Suspended`
- `Lost`
- `Deactivated`

Never send `Assigned`; the assignment workflow owns that value. Status change is blocked while a rider assignment is open.

Archive:

```http
PATCH /api/phone-sims/{id}/archive
```

```json
{
  "reason": "SIM was permanently cancelled and removed from inventory.",
  "rowVersion": "latest SIM rowVersion"
}
```

Success is `204 No Content`. Remove the row from the active list. Archive is blocked while an assignment is open.

## Concurrency handling

`rowVersion` is an opaque Base64 token. Never decode or modify it.

- SIM update, responsibility, status, archive, and assignment creation use the SIM `rowVersion`.
- Assignment close uses the assignment/current-rider `rowVersion`.
- After every successful mutation, use the newly returned values.
- For `409 phone_sim.concurrency_conflict`, reload the SIM and show a “record changed” message rather than retrying automatically.

## ProblemDetails handling

Errors use the existing `ProblemDetails` structure with `status`, `title`, `detail`, `errorCode`, `field`, and `correlationId`.

| Error code | Status | Frontend action |
| --- | ---: | --- |
| `phone_sim.invalid_phone_number` | 400 | Mark `phoneNumber` invalid. |
| `phone_sim.invalid_iccid` | 400 | Mark `iccid` invalid. |
| `phone_sim.invalid_status` | 400 | Refresh allowed status values. |
| `phone_sim.invalid_date_range` | 400 | Mark the assignment date field invalid. |
| `phone_sim.responsible_employee_not_found` | 404 | Refresh the employee selector. |
| `phone_sim.responsible_employee_unavailable` | 409 | Select an active internal employee. |
| `phone_sim.rider_not_found` | 404 | Refresh the rider selector. |
| `phone_sim.rider_unavailable` | 409 | Select an active rider. |
| `phone_sim.duplicate_phone_number` | 409 | Mark `phoneNumber` as already used. |
| `phone_sim.duplicate_iccid` | 409 | Mark `iccid` as already used. |
| `phone_sim.active_assignment_conflict` | 409 | Reload; close the current assignment first. |
| `phone_sim.assignment_conflict` | 409 | Reload assignment history and choose a valid start date. |
| `phone_sim.concurrency_conflict` | 409 | Reload before allowing another submission. |

## Suggested frontend delivery checklist

- Add `phone_sims.read` navigation visibility and `phone_sims.manage` action guards.
- Add API types and functions without using the generic controller workspace for the operational flow.
- Build the inventory list with server pagination and filters.
- Add create/edit, responsibility-transfer, assign, return, status, and archive dialogs.
- Add separate responsibility and rider-assignment timelines.
- Use employee IDs for responsibility and rider-profile IDs for assignments.
- Keep SIM and assignment row versions separate.
- Refresh the affected SIM after every mutation.
- Add Arabic/English labels and preserve RTL behavior.
- Enable the page against the hosted API only after the updated backend application has been published.
