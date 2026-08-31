# Frontend handoff: configurable employee and rider documents

## Delivery status

- Backend implementation: complete in this repository.
- Hosted database `db64865`: Application and Identity migration histories were checked and both databases were confirmed up to date on 2026-08-31.
- Database migration for this feature: none. The implementation uses the existing `DocumentType`, `DocumentRequirement`, `EmployeeDocument`, and `EmployeeDocumentVersion` tables.
- Hosted API binary: this handoff does not claim that the updated API build has been published. Deploy the backend before enabling these routes against `https://gate.premiumasp.net`.

The backend reference and copy-ready request examples are in [staff-document-management-api.md](staff-document-management-api.md).

## Product model

The frontend should treat the feature as two layers:

1. A document definition, such as `Passport image`, describes which staff can use it and whether it requires a special number, issue date, expiry date, and file.
2. A document requirement assigns that definition to all staff or a selected staff scope. The assignment is dynamic, so matching current and future employees/riders receive it automatically.

Do not create placeholder document uploads for every employee. Load the checklist endpoint; the API calculates `Missing`, `Optional`, `Incomplete`, `Expired`, or `Complete` from requirements and real uploads.

Employees and riders share the underlying employee document storage. For rider routes, always send the rider response `id` as `riderProfileId`, not its `employeeId`.

## Permissions

| Permission | Frontend capability |
| --- | --- |
| `documents.read` | View document types, assignments, employee/rider checklists, metadata, and version history. |
| `documents.catalog.manage` | Create and update document definitions and assignments. |
| `documents.upload` | Upload documents and versions, edit metadata, and archive an uploaded document. |
| `documents.download_sensitive` | Preview or download private employee document files. |

Hide configuration actions without `documents.catalog.manage`. Hide upload actions without `documents.upload`. Do not infer download access from `documents.read`.

## Suggested TypeScript types

```ts
export type CatalogStatus = "Active" | "Disabled" | "Archived";

export type DocumentType = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  appliesToSponsoredInternal: boolean;
  appliesToOutsideRider: boolean;
  appliesToRiderProfile: boolean;
  requiresNumber: boolean;
  requiresIssueDate: boolean;
  requiresExpiryDate: boolean;
  requiresFile: boolean;
  allowedMimeTypes: string[];
  maxFileSizeBytes: number;
  status: CatalogStatus;
  rowVersion: string;
};

export type DocumentRequirement = {
  id: string;
  documentTypeId: string;
  documentTypeCode: string;
  relationshipType: "SponsoredInternal" | "OutsideRider" | null;
  appliesToRiderProfile: boolean;
  isRequired: boolean;
  reminderOffsetsDays: number[];
  effectiveFrom: string; // YYYY-MM-DD
  effectiveTo: string | null;
  status: CatalogStatus;
  rowVersion: string;
};

export type EmployeeDocument = {
  id: string;
  employeeId: string;
  documentTypeId: string;
  documentTypeCode: string;
  documentTypeNameAr: string;
  documentNumber: string | null;
  issueDate: string | null;
  expiryDate: string | null;
  status: "Active" | "Expired" | "Superseded" | "Archived";
  notes: string | null;
  currentVersionId: string | null;
  currentVersionNumber: number | null;
  currentFileName: string | null;
  currentContentType: string | null;
  currentFileSizeBytes: number | null;
  rowVersion: string;
};

export type DocumentFulfillmentStatus =
  | "Missing"
  | "Optional"
  | "Incomplete"
  | "Expired"
  | "Complete";

export type StaffDocumentChecklistItem = {
  documentTypeId: string;
  documentTypeCode: string;
  documentTypeNameAr: string;
  documentTypeNameEn: string;
  requiresNumber: boolean;
  requiresIssueDate: boolean;
  requiresExpiryDate: boolean;
  requiresFile: boolean;
  isRequired: boolean;
  reminderOffsetsDays: number[];
  fulfillmentStatus: DocumentFulfillmentStatus;
  missingFields: Array<
    | "document"
    | "activeDocument"
    | "documentNumber"
    | "issueDate"
    | "expiryDate"
    | "validExpiryDate"
    | "file"
  >;
  documents: EmployeeDocument[];
};
```

## Document-type administration

List definitions:

```http
GET /api/hr-catalogs/document-types
```

Create:

```http
POST /api/hr-catalogs/document-types
```

Update:

```http
PUT /api/hr-catalogs/document-types/{id}
```

Example request for a passport image:

```json
{
  "code": "PASSPORT_IMAGE",
  "nameAr": "صورة جواز السفر",
  "nameEn": "Passport image",
  "descriptionAr": null,
  "descriptionEn": null,
  "appliesToSponsoredInternal": true,
  "appliesToOutsideRider": true,
  "appliesToRiderProfile": true,
  "requiresNumber": true,
  "requiresIssueDate": false,
  "requiresExpiryDate": true,
  "requiresFile": true,
  "allowedMimeTypes": ["application/pdf", "image/jpeg", "image/png"],
  "maxFileSizeBytes": 10485760,
  "status": "Active",
  "rowVersion": null
}
```

Frontend rules:

- `code` should be an uppercase stable key, for example `PASSPORT_IMAGE`.
- At least one audience flag must be true.
- Supported MIME values are `application/pdf`, `image/jpeg`, `image/png`, `image/webp`, `image/gif`, and `image/bmp`.
- Maximum configured size is 100 MB. The current HTTP upload limit is 11 MB, so keep the initial UI limit at 10 MB unless the API request-size limit is changed.
- Use `Disabled` or `Archived` instead of deleting a definition.
- For edit, send the latest returned `rowVersion` unchanged.

## Assignment administration

List assignments, optionally filtered by definition:

```http
GET /api/hr-catalogs/document-requirements
GET /api/hr-catalogs/document-requirements?documentTypeId={documentTypeId}
```

Create and update:

```http
POST /api/hr-catalogs/document-requirements
PUT /api/hr-catalogs/document-requirements/{id}
```

Assign the passport definition to all employees and riders:

```json
{
  "documentTypeId": "document-type-guid",
  "relationshipType": null,
  "appliesToRiderProfile": false,
  "isRequired": true,
  "reminderOffsetsDays": [90, 60, 30, 7, 0],
  "effectiveFrom": "2026-08-31",
  "effectiveTo": null,
  "status": "Active",
  "rowVersion": null
}
```

Scope mapping:

| Relationship | Rider-profile switch | Assignment target |
| --- | --- | --- |
| `null` | `false` | All employees and riders |
| `null` | `true` | All riders |
| `SponsoredInternal` | `false` | All sponsored/internal staff |
| `SponsoredInternal` | `true` | Sponsored/internal riders only |
| `OutsideRider` | either value | Outside riders |

The document definition's audience flags are enforced in addition to the assignment. For an “all staff” definition, enable sponsored/internal, outside rider, and rider-profile audiences as shown in the passport example.

## Staff checklist

Employee:

```http
GET /api/employees/{employeeId}/documents/checklist
```

Rider:

```http
GET /api/riders/{riderProfileId}/documents/checklist
```

Example response:

```json
[
  {
    "documentTypeId": "document-type-guid",
    "documentTypeCode": "PASSPORT_IMAGE",
    "documentTypeNameAr": "صورة جواز السفر",
    "documentTypeNameEn": "Passport image",
    "requiresNumber": true,
    "requiresIssueDate": false,
    "requiresExpiryDate": true,
    "requiresFile": true,
    "isRequired": true,
    "reminderOffsetsDays": [90, 60, 30, 7, 0],
    "fulfillmentStatus": "Missing",
    "missingFields": ["document"],
    "documents": []
  }
]
```

Render every checklist item, including missing items. Use the definition flags to show fields:

- `requiresNumber`: show and require “special/document number”.
- `requiresIssueDate`: show and require issue date.
- `requiresExpiryDate`: show and require expiry/end date.
- `requiresFile`: show and require the file picker.

Use `missingFields` for inline validation/status hints. A checklist item is complete when at least one active uploaded document satisfies every configured rule and its required expiry date has not passed.

## Upload documents

Employee:

```http
POST /api/employees/{employeeId}/documents
Content-Type: multipart/form-data
```

Rider using any dynamic type:

```http
POST /api/riders/{riderProfileId}/documents
Content-Type: multipart/form-data
```

Form fields:

| Field | Type | Rule |
| --- | --- | --- |
| `documentTypeId` | GUID | Always send the checklist item's definition ID. |
| `documentNumber` | string | Required only when `requiresNumber` is true. |
| `issueDate` | `YYYY-MM-DD` | Required only when `requiresIssueDate` is true. |
| `expiryDate` | `YYYY-MM-DD` | Required only when `requiresExpiryDate` is true. |
| `notes` | string | Optional. |
| `file` | binary | Required by the current upload routes. |

Use `FormData`; do not JSON-encode metadata inside one field:

```ts
const body = new FormData();
body.append("documentTypeId", item.documentTypeId);
if (documentNumber) body.append("documentNumber", documentNumber);
if (issueDate) body.append("issueDate", issueDate);
if (expiryDate) body.append("expiryDate", expiryDate);
if (notes) body.append("notes", notes);
body.append("file", file);
```

The backend validates the employee/rider audience, required metadata, exact MIME allow-list, declared size, file extension, and actual file signature. Refresh the checklist after success.

Multiple records of the same type are allowed. When a document number is supplied, it must be unique for that document type. Use a new upload for a separate passport/document; use a version upload when replacing the file for the same logical record.

## Existing document actions

List actual employee uploads only:

```http
GET /api/employees/{employeeId}/documents
```

Upload a new immutable file version:

```http
POST /api/employees/{employeeId}/documents/{documentId}/versions
Content-Type: multipart/form-data
```

The version request contains only `file`.

Update metadata:

```http
PUT /api/employees/{employeeId}/documents/{documentId}
```

```json
{
  "metadata": {
    "documentNumber": "P12345678",
    "issueDate": null,
    "expiryDate": "2031-08-31",
    "notes": null
  },
  "rowVersion": "latest document rowVersion"
}
```

Version history, download, preview, and archive:

```http
GET   /api/employees/{employeeId}/documents/{documentId}/versions
GET   /api/employees/{employeeId}/documents/{documentId}/download?versionId={optionalVersionId}
GET   /api/employees/{employeeId}/documents/{documentId}/preview?versionId={optionalVersionId}
PATCH /api/employees/{employeeId}/documents/{documentId}/archive
```

Archive body:

```json
{
  "reason": "Replaced by a corrected record.",
  "rowVersion": "latest document rowVersion"
}
```

Archive success is `204 No Content`. Remove the document locally and refresh the checklist.

## Errors and concurrency

Errors use the existing ProblemDetails response. Important codes:

| Error code | Typical status | Frontend action |
| --- | ---: | --- |
| `hr.invalid_request` | 400 | Check definition audience/status or malformed assignment values. |
| `documents.invalid_metadata` | 400 | Mark the fields required by the selected definition. |
| `documents.invalid_file` | 400 | Check size, MIME, extension, and actual file content. |
| `hr.not_found` | 404 | Refresh employee/rider/type data. |
| `documents.file_missing` | 404 | Show that the stored version is unavailable. |
| `hr.duplicate` | 409 | Mark the document code or document number as already used. |
| `hr.concurrency_conflict` | 409 | Reload the record and do not retry automatically. |

`rowVersion` is opaque Base64. Never decode or modify it. Replace local objects with the mutation response and use the newly returned version for the next update.

## Suggested frontend delivery checklist

- Add a document-definition administration page guarded by `documents.catalog.manage`.
- Add create/edit forms for definition flags, audience, allowed formats, size, and status.
- Add assignment scope, required/optional, effective dates, and reminder offsets.
- Add the checklist panel to employee and rider details.
- Render number/date/file inputs from the checklist flags instead of hard-coding passport fields.
- Show all five fulfillment statuses with text and color, not color alone.
- Use `employeeId` for employee routes and `riderProfileId` for rider routes.
- Use `FormData` for uploads and refresh the checklist after mutations.
- Keep document record IDs, document type IDs, employee IDs, and rider-profile IDs distinct.
- Add Arabic and English copy and preserve RTL layout.
- Enable hosted calls only after the updated API application has been published.
