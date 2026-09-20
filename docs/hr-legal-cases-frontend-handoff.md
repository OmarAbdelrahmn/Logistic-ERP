# HR Legal Cases — Frontend and API Handoff

## Scope

The HR legal-case module manages a case between exactly one sponsor and one person. The person may be an employee, a rider, or an external person. Cases contain hearings, each hearing accepts at most five active private files, and every business change creates immutable before/after history.

All dates and times submitted by this API are Riyadh local values (`UTC+03:00`). Notifications are stored in UTC.

Base route: `/api/hr/legal-cases`

## Permissions

| Permission | Purpose |
|---|---|
| `legal_cases.read` | List/detail/history and notification audience |
| `legal_cases.manage` | Create/update/archive cases and hearings; upload/archive files |
| `legal_cases.files.download` | Download private hearing files |

The protected `SystemAdmin` and `Manager` roles receive all three permissions in the identity migration.

## Enumerations

- `personType`: `Employee`, `Rider`, `External`
- `sponsorPartyRole`: `Claimant`, `Defendant`
- case `status`: `Open`, `InProgress`, `Suspended`, `Closed`
- hearing `status`: `Scheduled`, `Completed`, `Postponed`, `Cancelled`

The sponsor role determines both sides:

- `Claimant`: sponsor is المدعي; person is المدعى عليه.
- `Defendant`: person is المدعي; sponsor is المدعى عليه.

## Case request

```json
{
  "caseNumber": "CASE-2026-001",
  "personType": "Employee",
  "personName": null,
  "employeeId": "019d0000-0000-7000-8000-000000000001",
  "riderProfileId": null,
  "sponsorId": "019c18d5-62e1-7000-8000-000000000040",
  "sponsorPartyRole": "Claimant",
  "caseDate": "2026-10-15",
  "caseTime": "09:30:00",
  "status": "Open",
  "details": "Case details",
  "notes": "Optional notes",
  "responsibleUserId": "019d0000-0000-7000-8000-000000000099",
  "rowVersion": null,
  "changeReason": null
}
```

Person rules:

- `Employee`: send `employeeId`; do not send `riderProfileId`. `personName` is ignored and snapshotted from the employee.
- `Rider`: send `riderProfileId`; do not send `employeeId`. `personName` is snapshotted from the rider's employee record.
- `External`: send `personName`; both internal IDs must be null.
- `sponsorId` must identify an active sponsor.
- `responsibleUserId` must identify an active application user.

On update, send the latest Base64 `rowVersion` and a non-empty `changeReason`.

## Case endpoints

| Method | Route | Result |
|---|---|---|
| GET | `/api/hr/legal-cases` | Paged case summaries |
| GET | `/api/hr/legal-cases/{id}` | Full case, resolved claimant/defendant, hearings, and file metadata |
| POST | `/api/hr/legal-cases` | Create a case; returns `201 Created` |
| PUT | `/api/hr/legal-cases/{id}` | Update a case |
| DELETE | `/api/hr/legal-cases/{id}` | Soft-archive a case; returns `204` |
| GET | `/api/hr/legal-cases/{id}/history` | Immutable newest-first history |

List query parameters:

- `search`: partial case number or person name
- `status`
- `sponsorId`, `employeeId`, `riderProfileId`
- `fromDate`, `toDate`
- `page` (default 1), `pageSize` (1–200, default 50)

Archive request:

```json
{
  "rowVersion": "base64-row-version",
  "reason": "Required archive reason"
}
```

## Hearing endpoints

| Method | Route | Result |
|---|---|---|
| POST | `/{caseId}/hearings` | Create the next sequential hearing |
| PUT | `/{caseId}/hearings/{hearingId}` | Update a hearing |
| DELETE | `/{caseId}/hearings/{hearingId}` | Soft-archive a hearing |

Create request:

```json
{
  "hearingDate": "2026-10-20",
  "hearingTime": "10:00:00",
  "status": "Scheduled",
  "details": "First hearing",
  "notes": null,
  "location": "Riyadh",
  "rowVersion": null,
  "changeReason": null
}
```

Updates require the latest `rowVersion` and a non-empty `changeReason`. Hearing numbers are assigned by the server and are unique within each active case.

## Hearing file endpoints

| Method | Route | Result |
|---|---|---|
| POST | `/{caseId}/hearings/{hearingId}/files` | Multipart upload |
| GET | `/{caseId}/hearings/{hearingId}/files/{fileId}/download` | Private streamed download |
| DELETE | `/{caseId}/hearings/{hearingId}/files/{fileId}` | Soft-archive metadata/file access |

Upload uses `multipart/form-data`:

- `file`: required
- `description`: optional

Limits and accepted content:

- At most five active files per hearing.
- Maximum 10 MiB per file (HTTP request allowance is 11 MiB).
- PDF, JPEG, PNG, WebP, GIF, and BMP.
- Extension, declared MIME type, byte signature, actual size, and SHA-256 are validated.
- Storage is private under `wwwroot/private/hr/legal-cases/...`; no public static URL is returned.

## History

History is append-only and is created for:

- case creation, field updates, and archive;
- hearing creation, field updates, and archive;
- hearing-file upload and archive.

Each row contains `changeType`, `changedFields`, `beforeJson`, `afterJson`, `changeReason`, actor, and timestamp. It cannot be updated or deleted by the API.

## Notifications

The `responsibleUserId` receives:

- an immediate notification when a case is created, updated, or archived;
- an immediate notification when a hearing is created or updated;
- case reminders 24 hours and one hour before the case date/time;
- hearing reminders 24 hours and one hour before the hearing date/time.

Changing the date/time or responsible user updates/reassigns pending reminders. Closing/archiving a case, completing/cancelling/archiving a hearing, or passing the reminder validity window archives the pending reminders. Notifications link to the relevant case/hearing route and are permission-filtered by `legal_cases.read`.

## Error codes

| Code | Meaning |
|---|---|
| `legal_cases.not_found` | Case, hearing, or file was not found |
| `legal_cases.invalid_request` | Invalid enum, date, required text, or update reason |
| `legal_cases.duplicate_case_number` | Active case number already exists |
| `legal_cases.invalid_person` | Person type and supplied link/name do not match |
| `legal_cases.sponsor_not_found` | Sponsor is missing/inactive |
| `legal_cases.responsible_user_not_found` | Responsible user is missing/inactive |
| `legal_cases.hearing_file_limit` | Five active files already exist |
| `legal_cases.concurrency_conflict` | Stale or invalid row version |

## Database artifacts

Application migration: `20260919110051_AddHrLegalCases`

- `app.HrLegalCases`
- `app.HrLegalCaseHearings`
- `app.HrLegalCaseHearingFiles`
- `app.HrLegalCaseHistory`
- three `platform.PermissionDefinitions`

Identity migration: `20260919110102_GrantHrLegalCasePermissions`

- grants the three permissions to protected SystemAdmin and Manager roles.

All operational tables use soft delete and SQL Server `rowversion`. Case number uniqueness and hearing number uniqueness apply only to active rows.
