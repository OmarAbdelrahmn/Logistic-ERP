# Vacation and Leave API — Frontend Handoff

## Purpose

This document covers the vacation/leave workflow under `/api/hr-workflows`, including leave types (`نوع الإجازة`), requests, approvals, date changes, cancellations, and documents.

The API uses camel-case JSON property names. Leave workflow values are returned as strings, not numeric enums.

## `نوع الإجازة` — leave type

There are two sources for the Arabic leave-type label:

1. `GET /api/hr-workflows/leave-types` returns the leave-type catalog including `nameAr` and `nameEn`.
2. `GET /api/hr-workflows/leave-requests` returns `leaveTypeId` and `leaveTypeNameAr` for every leave request.

`LeaveRequestResponse` does **not** currently return `leaveTypeNameEn`. For English display, load the leave-type catalog and join on `leaveTypeId`.

## Permissions

| Permission | Use |
|---|---|
| `leave_requests.read` | Read leave types, approval workflows, leave requests, date changes, cancellations, and leave documents |
| `leave_requests.manage` | Create/update leave types, workflows, requests, date changes, cancellations, and document metadata |
| `leave_requests.approve` | Approve/reject/return requests, resolve changes/cancellations, force-cancel |
| `documents.upload` | Upload leave documents and new document versions |
| `documents.read` | Read leave-document versions |
| `documents.download_sensitive` | Download leave files |

## Leave-type endpoints

### Get leave types

```http
GET /api/hr-workflows/leave-types
```

Permission: `leave_requests.read`.

The response is an array ordered by `nameAr`.

```ts
interface LeaveTypeResponse {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  requiresBalance: boolean;
  requiresHrDocuments: boolean;
  requiresExitReentryVisa: boolean;
  maximumCalendarDays: number | null;
  status: "Active" | "Disabled" | "Archived";
  rowVersion: string;
}
```

Use only `Active` values in the new-leave-request dropdown.

### Create a leave type

```http
POST /api/hr-workflows/leave-types
```

Permission: `leave_requests.manage`.

```json
{
  "code": "ANNUAL",
  "nameAr": "إجازة سنوية",
  "nameEn": "Annual Leave",
  "descriptionAr": null,
  "descriptionEn": null,
  "requiresBalance": true,
  "requiresHrDocuments": false,
  "requiresExitReentryVisa": false,
  "maximumCalendarDays": 30,
  "status": "Active",
  "rowVersion": null
}
```

### Update a leave type

```http
PUT /api/hr-workflows/leave-types/{leaveTypeId}
```

Use the same body and send the latest `rowVersion`.

Validation:

- `code`, `nameAr`, and `nameEn` are required.
- `code` is unique.
- `maximumCalendarDays`, if supplied, must be greater than zero.
- A stale `rowVersion` returns `409 Conflict`.

## Leave approval workflow endpoints

### Get workflows

```http
GET /api/hr-workflows/leave-approval-workflows
```

Permission: `leave_requests.read`.

```ts
interface LeaveWorkflowStep {
  id: string;
  stepKey: string;
  sequence: number;
  nameAr: string;
  nameEn: string;
  requiredPermissionKey: string;
  scopeSource:
    | "CompanyWide"
    | "EmployeeHousing"
    | "ActiveClientPlatform"
    | "ActiveClientContract";
  allowsReturnForChanges: boolean;
  requiresCommentOnApproval: boolean;
  targetResponseHours: number | null;
}

interface LeaveWorkflow {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  version: number;
  leaveTypeId: string | null;
  relationshipType: "SponsoredInternal" | "OutsideRider" | null;
  appliesToRider: boolean | null;
  clientPlatformId: string | null;
  priority: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  status: "Active" | "Disabled" | "Archived";
  steps: LeaveWorkflowStep[];
  rowVersion: string;
}
```

### Create or update workflow

```http
POST /api/hr-workflows/leave-approval-workflows
PUT /api/hr-workflows/leave-approval-workflows/{workflowId}
```

Permission: `leave_requests.manage`.

The request contains all `LeaveWorkflow` fields except server-generated IDs, plus `steps`.

Validation:

- `version` must be greater than zero.
- At least one step is required.
- Step keys and sequences must be unique.
- Each sequence must be greater than zero.
- `requiredPermissionKey` must be a known system permission.
- `effectiveTo` cannot be before `effectiveFrom`.
- `(code, version)` is unique.

On submission, the backend picks the highest-priority matching active workflow, then the highest version.

## Leave-request endpoints

### List leave requests

```http
GET /api/hr-workflows/leave-requests
GET /api/hr-workflows/leave-requests?employeeId={employeeId}
```

Permission: `leave_requests.read`.

The response is an unpaged array ordered by `startDate` descending.

```ts
interface LeaveRequestResponse {
  id: string;
  requestNumber: string;
  employeeId: string;
  employeeNameAr: string;
  leaveTypeId: string;
  leaveTypeNameAr: string;
  startDate: string;
  endDate: string;
  expectedReturnDate: string;
  calendarDays: number;
  reason: string;
  status: LeaveWorkflowStatus;
  hrStatus: LeaveHrStatus;
  approvalWorkflowId: string | null;
  currentApprovalStepKey: string | null;
  currentApprovalStepSequence: number | null;
  submittedAtUtc: string | null;
  approvedAtUtc: string | null;
  activatedAtUtc: string | null;
  completedAtUtc: string | null;
  rejectionReason: string | null;
  cancellationReason: string | null;
  relatedClientContractId: string | null;
  notes: string | null;
  rowVersion: string;
}
```

Example:

```json
{
  "id": "guid",
  "requestNumber": "LV-20260919-...",
  "employeeId": "guid",
  "employeeNameAr": "اسم الموظف",
  "leaveTypeId": "guid",
  "leaveTypeNameAr": "إجازة سنوية",
  "startDate": "2026-10-01",
  "endDate": "2026-10-15",
  "expectedReturnDate": "2026-10-16",
  "calendarDays": 15,
  "reason": "Annual vacation",
  "status": "Approved",
  "hrStatus": "Ready",
  "approvalWorkflowId": "guid",
  "currentApprovalStepKey": null,
  "currentApprovalStepSequence": null,
  "submittedAtUtc": "2026-09-19T08:00:00Z",
  "approvedAtUtc": "2026-09-19T10:00:00Z",
  "activatedAtUtc": null,
  "completedAtUtc": null,
  "rejectionReason": null,
  "cancellationReason": null,
  "relatedClientContractId": null,
  "notes": null,
  "rowVersion": "..."
}
```

Current API limitations:

- No `GET /leave-requests/{id}` route.
- No paging or status filter.
- No leave-balance calculation endpoint.
- No approval-decision history read endpoint.

### Create leave request

```http
POST /api/hr-workflows/leave-requests
```

Permission: `leave_requests.manage`.

```ts
interface LeaveRequestUpsertRequest {
  employeeId: string;
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  expectedReturnDate: string;
  reason: string;
  destinationCountryCode?: string | null;
  contactPhoneDuringLeave?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  relatedClientContractId?: string | null;
  notes?: string | null;
  rowVersion?: string | null;
}
```

### Update leave request

```http
PUT /api/hr-workflows/leave-requests/{leaveRequestId}
```

Permission: `leave_requests.manage`.

Use the same request body with the current `rowVersion`.

Rules:

- A request can be edited only in `Draft` or `ReturnedForChanges` status.
- `endDate` must be on or after `startDate`.
- `expectedReturnDate` must be on or after `endDate`.
- `calendarDays` is calculated by the backend, inclusive of both dates.
- `reason` is required.
- `destinationCountryCode` is at most two characters.
- The selected leave type must be `Active`.
- Duration cannot exceed `maximumCalendarDays` when the leave type has a maximum.
- The employee cannot have overlapping leave records except cancelled or rejected records.

## Request transitions and approvals

All transition requests use:

```ts
interface LeaveTransitionRequest {
  action: string;
  comment: string;
  rowVersion?: string | null;
}
```

### Submit, activate, or complete

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/transitions
```

Permission: `leave_requests.manage`.

| Action | Allowed source status | Result |
|---|---|---|
| `submit` | `Draft`, `ReturnedForChanges` | `PendingApproval` after a matching workflow is selected |
| `activate` | `Approved` | `Active` |
| `complete` | `Active` | `Completed` |

### Approve, reject, or return

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/approval-decisions
```

Permission: `leave_requests.approve` plus the required permission and scope of the active workflow step.

| Action | Notes |
|---|---|
| `approve` | Advances to the next step; the final approval changes status to `Approved` |
| `reject` | Requires a comment and changes status to `Rejected` |
| `return` | Requires a comment; allowed only when the step allows return-for-changes; changes status to `ReturnedForChanges` |

An approval comment is required only when the active workflow step sets `requiresCommentOnApproval: true`.

### Force-cancel

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/force-cancel
```

Permission: `leave_requests.approve`.

```json
{
  "action": "force-cancel",
  "comment": "Administrative cancellation",
  "rowVersion": "..."
}
```

Force cancellation is rejected when the request is already `Completed` or `Cancelled`.

## Date-change endpoints

### List date changes

```http
GET /api/hr-workflows/leave-requests/{leaveRequestId}/date-change-requests
```

Permission: `leave_requests.read`.

### Request date change

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/date-change-requests
```

Permission: `leave_requests.manage`.

```json
{
  "requestedStartDate": "2026-10-05",
  "requestedEndDate": "2026-10-20",
  "reason": "Travel schedule changed"
}
```

Allowed only while leave status is `Approved` or `Active`. Only one pending date-change request is allowed. The requested dates cannot overlap another eligible leave record for the employee.

### Resolve date change

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/date-change-requests/{changeId}/resolve
```

Permission: `leave_requests.approve`.

```json
{
  "approve": true,
  "resolutionReason": "Approved by HR",
  "rowVersion": "..."
}
```

When approved, the backend updates start/end dates and recalculates `calendarDays`. If `expectedReturnDate` becomes earlier than the new end date, it becomes `endDate + 1 day`.

## Cancellation endpoints

### List cancellations

```http
GET /api/hr-workflows/leave-requests/{leaveRequestId}/cancellation-requests
```

Permission: `leave_requests.read`.

### Request cancellation

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/cancellation-requests
```

Permission: `leave_requests.manage`.

```json
{
  "reason": "Employee cancelled travel"
}
```

Allowed for `PendingApproval`, `Approved`, and `Active` leave. The request changes leave status to `CancellationPending`. Only one pending cancellation request is allowed.

### Resolve cancellation

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/cancellation-requests/{cancellationId}/resolve
```

Permission: `leave_requests.approve`.

```json
{
  "approve": true,
  "resolutionReason": "Cancellation approved",
  "rowVersion": "..."
}
```

Approval changes leave status to `Cancelled`. Rejection restores its previous status.

## Leave-document endpoints

Base route:

```text
/api/hr-workflows/leave-requests/{leaveRequestId}/documents
```

### List documents

```http
GET /api/hr-workflows/leave-requests/{leaveRequestId}/documents
```

Permission: `leave_requests.read`.

```ts
interface LeaveDocumentResponse {
  id: string;
  leaveRequestId: string;
  kind: "Ticket" | "ExitReentryVisa" | "ApprovalLetter" | "Other";
  referenceNumber: string | null;
  issuedOn: string | null;
  expiresOn: string | null;
  notes: string | null;
  currentVersionId: string | null;
  currentVersionNumber: number | null;
  currentFileName: string | null;
  currentContentType: string | null;
  currentFileSizeBytes: number | null;
  rowVersion: string;
}
```

### Upload document

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/documents
Content-Type: multipart/form-data
```

Permission: `documents.upload`.

Form fields: `kind`, `referenceNumber`, `issuedOn`, `expiresOn`, `notes`, and `file`.

Supported `kind` values: `Ticket`, `ExitReentryVisa`, `ApprovalLetter`, `Other`.

Maximum stored file size is 10 MiB; the HTTP request limit is 11 MiB.

### Upload new version

```http
POST /api/hr-workflows/leave-requests/{leaveRequestId}/documents/{documentId}/versions
Content-Type: multipart/form-data
```

Permission: `documents.upload`.

Form field: `file`.

### Update document metadata

```http
PUT /api/hr-workflows/leave-requests/{leaveRequestId}/documents/{documentId}
```

Permission: `leave_requests.manage`.

```json
{
  "metadata": {
    "kind": "Ticket",
    "referenceNumber": "TKT-123",
    "issuedOn": "2026-09-01",
    "expiresOn": null,
    "notes": null
  },
  "rowVersion": "..."
}
```

### List document versions

```http
GET /api/hr-workflows/leave-requests/{leaveRequestId}/documents/{documentId}/versions
```

Permission: `documents.read`.

### Download document

```http
GET /api/hr-workflows/leave-requests/{leaveRequestId}/documents/{documentId}/download
GET /api/hr-workflows/leave-requests/{leaveRequestId}/documents/{documentId}/download?versionId={versionId}
```

Permission: `documents.download_sensitive`.

### Archive document

```http
PATCH /api/hr-workflows/leave-requests/{leaveRequestId}/documents/{documentId}/archive
```

Permission: `leave_requests.manage`.

```json
{
  "reason": "Uploaded by mistake",
  "rowVersion": "..."
}
```

Success response: `204 No Content`.

## Status values

### Leave workflow status

```text
Draft
PendingApproval
ReturnedForChanges
Approved
Active
Completed
Rejected
CancellationPending
Cancelled
Expired
```

### HR status

```text
NotRequired
PendingDocuments
InProgress
Ready
Completed
```

### Date-change or cancellation status

```text
Pending
Approved
Rejected
Cancelled
```

## Error behavior

| HTTP status | Common error codes |
|---|---|
| `400` | `hr.invalid_request`, `documents.invalid_file` |
| `401` | `hr.current_user_unavailable` |
| `403` | `leave.approval_forbidden` |
| `404` | `hr.not_found`, `documents.file_missing` |
| `409` | `hr.conflict`, `hr.duplicate`, `hr.concurrency_conflict` |

All errors use `ProblemDetails` and include `errorCode` and `correlationId`.

## Frontend refresh rules

1. Refetch leave requests after create, update, submit, approval action, date-change resolution, cancellation resolution, or force cancellation.
2. Refetch date-change/cancellation lists after creating or resolving their respective requests.
3. Refetch document list after upload, new-version upload, metadata update, or archive.
4. Send the latest `rowVersion` for updates and resolution operations; on `409`, refetch the resource before retrying.

