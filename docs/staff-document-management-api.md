# Configurable employee and rider documents API

The backend separates a reusable document definition from each staff member's uploaded files:

- `DocumentType` defines the name, audience, file policy, and whether a number, issue date, expiry date, or file is required.
- `DocumentRequirement` assigns that definition to a staff scope. Assignments are resolved dynamically, so they cover current and future matching staff without inserting empty `EmployeeDocument` rows.
- `EmployeeDocument` stores one staff member's metadata. A staff member may have multiple documents of the same type.
- `EmployeeDocumentVersion` keeps the immutable private-file history for each uploaded document.

## Create a document type

`POST /api/hr-catalogs/document-types`

Permission: `documents.catalog.manage`

Example for a passport image that requires a passport number and expiry date:

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

At least one audience flag must be true. Supported file types are PDF, JPEG, PNG, WebP, GIF, and BMP. Use the returned `rowVersion` with `PUT /api/hr-catalogs/document-types/{id}`.

## Assign the type

`POST /api/hr-catalogs/document-requirements`

Permission: `documents.catalog.manage`

Assign to all employees and riders:

```json
{
  "documentTypeId": "DOCUMENT_TYPE_ID",
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

Assignment scopes:

| `relationshipType` | `appliesToRiderProfile` | Matching staff |
| --- | --- | --- |
| `null` | `false` | All employees and riders |
| `null` | `true` | All riders |
| `SponsoredInternal` | `false` | All sponsored/internal staff |
| `SponsoredInternal` | `true` | Sponsored/internal riders only |
| `OutsideRider` | `false` or `true` | Outside riders |

The document type's audience flags are also enforced. A requirement never makes a type available to an audience disabled on the type.

## Read the assigned checklist

- `GET /api/employees/{employeeId}/documents/checklist`
- `GET /api/riders/{riderProfileId}/documents/checklist`

Permission: `documents.read`

Each item contains the document definition flags, whether the assignment is required, reminder offsets, all uploaded documents of that type, and one fulfillment status:

- `Missing`: a required assignment has no uploaded document.
- `Optional`: an optional assignment has no uploaded document.
- `Incomplete`: a document exists but a required number, date, active record, or file is missing.
- `Expired`: an uploaded document is expired or its required expiry date has passed.
- `Complete`: at least one active document satisfies every configured rule.

## Upload custom documents

Both routes accept `multipart/form-data` with `documentTypeId`, optional `documentNumber`, `issueDate`, `expiryDate`, `notes`, and `file`:

- `POST /api/employees/{employeeId}/documents`
- `POST /api/riders/{riderProfileId}/documents`

Permission: `documents.upload`

The backend validates the selected type's audience, required metadata, exact MIME allow-list, maximum size, and the file's actual signature. Additional uploads create additional `EmployeeDocument` records; uploading to `/api/employees/{employeeId}/documents/{documentId}/versions` creates a new immutable version of one record.

Document metadata and versions require `documents.read`; private downloads and previews require `documents.download_sensitive`.
