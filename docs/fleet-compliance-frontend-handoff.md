# Fleet Compliance Frontend Handoff

## Purpose

Fleet compliance now exposes document-upload state separately from compliance-date state. The frontend must not infer whether a file is uploaded from `recordId`, `effectiveFrom`, `expiryDate`, `dateStatus`, or `status`.

Use:

- `hasUploadedFile` / `uploadedFile` for the document indicator.
- `effectiveFrom` / `expiryDate` / `dateStatus` for the date indicator.
- `status` only for the combined compliance badge.

The API uses camel-case JSON properties and numeric enum values.

## Primary dashboard endpoint

### `GET /api/vehicle-compliance/due?checkDate=YYYY-MM-DD`

Returns compliance items whose combined `status` is not `Valid`. `checkDate` is optional. When omitted, the backend uses the current Riyadh business date (`UTC+3`).

Current backend limitation: this endpoint evaluates the first 200 vehicles returned by the vehicle-list service. The frontend cannot page this endpoint yet.

Current permission behavior requires both:

- `fleet.compliance.read`
- `fleet.vehicles.read`

The second permission is currently required because the compliance service builds the response through the vehicle-list service.

### TypeScript contracts

```ts
export enum VehicleComplianceDueStatus {
  Valid = 1,
  Upcoming = 2,
  DueToday = 3,
  Expired = 4,
  Missing = 5,
  UploadedWithoutDates = 6,
}

export type VehicleComplianceType =
  | "Registration"
  | "Insurance"
  | "Inspection"
  | "Permit"
  | "OperationCard";

export interface VehicleComplianceUploadedFile {
  attachmentId: string;
  versionId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAtUtc: string;
}

export interface VehicleComplianceDueItem {
  vehicleId: string;
  assetNumber: string;
  type: VehicleComplianceType;
  recordId: string | null;
  effectiveFrom: string | null; // YYYY-MM-DD
  expiryDate: string | null; // YYYY-MM-DD
  dateStatus: VehicleComplianceDueStatus;
  status: VehicleComplianceDueStatus;
  daysRemaining: number | null;
  hasUploadedFile: boolean;
  uploadedFile: VehicleComplianceUploadedFile | null;
}
```

### Uploaded file without dates

```json
[
  {
    "vehicleId": "019c0000-0000-7000-8000-000000000001",
    "assetNumber": "VEH-000001",
    "type": "Registration",
    "recordId": null,
    "effectiveFrom": null,
    "expiryDate": null,
    "dateStatus": 5,
    "status": 6,
    "daysRemaining": null,
    "hasUploadedFile": true,
    "uploadedFile": {
      "attachmentId": "019c0000-0000-7000-8000-000000000010",
      "versionId": "019c0000-0000-7000-8000-000000000011",
      "originalFileName": "istimara.pdf",
      "contentType": "application/pdf",
      "fileSizeBytes": 245760,
      "uploadedAtUtc": "2026-09-19T08:15:00Z"
    }
  }
]
```

Interpretation:

- The document is uploaded because `hasUploadedFile` is `true`.
- The dates are missing because `dateStatus` is `5`.
- The combined badge is “Uploaded — dates not entered” because `status` is `6`.

### Missing file and missing dates

```json
{
  "recordId": null,
  "effectiveFrom": null,
  "expiryDate": null,
  "dateStatus": 5,
  "status": 5,
  "daysRemaining": null,
  "hasUploadedFile": false,
  "uploadedFile": null
}
```

Interpretation: show “Not uploaded” for the document and “Dates missing” for the date data.

### File and dates both present

```json
{
  "recordId": "019c0000-0000-7000-8000-000000000020",
  "effectiveFrom": "2026-01-01",
  "expiryDate": "2026-12-31",
  "dateStatus": 2,
  "status": 2,
  "daysRemaining": 20,
  "hasUploadedFile": true,
  "uploadedFile": {
    "attachmentId": "019c0000-0000-7000-8000-000000000010",
    "versionId": "019c0000-0000-7000-8000-000000000011",
    "originalFileName": "istimara.pdf",
    "contentType": "application/pdf",
    "fileSizeBytes": 245760,
    "uploadedAtUtc": "2026-09-19T08:15:00Z"
  }
}
```

Interpretation: show the file as uploaded and use the date status for the combined badge.

## Required display logic

```ts
function getFileLabel(item: VehicleComplianceDueItem): string {
  return item.hasUploadedFile ? "Uploaded" : "Not uploaded";
}

function getDateLabel(item: VehicleComplianceDueItem): string {
  if (item.effectiveFrom === null && item.expiryDate === null) {
    return "Dates not entered";
  }

  switch (item.dateStatus) {
    case VehicleComplianceDueStatus.Valid:
      return "Valid";
    case VehicleComplianceDueStatus.Upcoming:
      return "Expiring soon";
    case VehicleComplianceDueStatus.DueToday:
      return "Due today";
    case VehicleComplianceDueStatus.Expired:
      return "Expired";
    default:
      return "Dates missing";
  }
}

function getCombinedLabel(item: VehicleComplianceDueItem): string {
  if (item.status === VehicleComplianceDueStatus.UploadedWithoutDates) {
    return "Uploaded — dates not entered";
  }

  return getDateLabel(item);
}
```

Do not use `dateStatus === Missing` to display “Not uploaded.” A file can be uploaded while `dateStatus` is `Missing`.

## File-to-compliance mapping

| Compliance type | Vehicle file kind | Numeric kind | File metadata returned by dashboard |
|---|---|---:|---|
| `Registration` | `Istimara` | `1` | Yes |
| `OperationCard` | `OperationCard` | `2` | Yes |
| `Insurance` | No vehicle file slot | — | No |
| `Inspection` | No vehicle file slot | — | No |
| `Permit` | No vehicle file slot | — | No |

For insurance, inspection, and permit items, `hasUploadedFile` is currently `false` and `uploadedFile` is `null` because those file kinds do not exist in the vehicle file model.

## Vehicle list and detail

### `GET /api/vehicles`

Query parameters:

- `search`
- `status`
- `operatingCityId`
- `page` — default `1`
- `pageSize` — default `50`, maximum `200`

Permission: `fleet.vehicles.read`.

The paged response contains `items`, `page`, `pageSize`, and `totalCount`. Each vehicle summary now includes:

```ts
export interface VehicleComplianceSummaryFields {
  registrationExpiryDate: string | null;
  registrationStatus: VehicleComplianceDueStatus;
  registrationFileUploaded: boolean;

  insuranceExpiryDate: string | null;
  insuranceStatus: VehicleComplianceDueStatus;

  inspectionExpiryDate: string | null;
  inspectionStatus: VehicleComplianceDueStatus;

  permitEndDate: string | null;
  permitStatus: VehicleComplianceDueStatus;

  operationCardExpiryDate: string | null;
  operationCardStatus: VehicleComplianceDueStatus;
  operationCardFileUploaded: boolean;
}
```

The same summary is nested under `summary` in `GET /api/vehicles/{vehicleId}`.

Use `registrationFileUploaded` and `operationCardFileUploaded` for compact list indicators. Use the primary dashboard endpoint when filename, upload time, or attachment/version IDs are needed.

## Vehicle file endpoints

### List current vehicle files

`GET /api/vehicles/{vehicleId}/files`

Permission: `fleet.files.read`.

```ts
export interface VehicleAttachment {
  id: string;
  vehicleId: string;
  kind: number;
  displayName: string;
  currentVersionId: string | null;
  currentVersionNumber: number | null;
  originalFileName: string | null;
  contentType: string | null;
  fileSizeBytes: number | null;
  isLegacy: boolean;
  rowVersion: string;
}
```

This endpoint returns existing attachment rows only. It does not return placeholder rows for file kinds that were never uploaded.

### Upload or replace a file

`PUT /api/vehicles/{vehicleId}/files/{kind}`

Permission: `fleet.files.upload`.

Send `multipart/form-data` with one field named `file`.

```ts
async function uploadVehicleFile(
  vehicleId: string,
  kind: 1 | 2,
  file: File,
): Promise<VehicleAttachment> {
  const body = new FormData();
  body.append("file", file);

  const response = await fetch(`/api/vehicles/${vehicleId}/files/${kind}`, {
    method: "PUT",
    body,
  });

  if (!response.ok) throw await response.json();
  return response.json();
}
```

Use kind `1` for Istimara and kind `2` for an operation card. Operation-card upload is accepted only for a vehicle whose registration type is `PublicTransport` (`5`). Maximum file size is 10 MiB; the HTTP request limit is 11 MiB.

After a successful upload, invalidate/refetch:

- `GET /api/vehicle-compliance/due`
- `GET /api/vehicles`
- `GET /api/vehicles/{vehicleId}/files`

### Download the file returned by compliance

`GET /api/vehicles/{vehicleId}/files/{attachmentId}/download?versionId={versionId}`

Permission: `fleet.files.download`.

Use `uploadedFile.attachmentId` and `uploadedFile.versionId` directly from the compliance response.

## Compliance date endpoints

These endpoints create a dated compliance record. They do not upload a file.

Permission: `fleet.compliance.manage`.

### Registration

`POST /api/vehicles/{vehicleId}/registrations`

```ts
interface VehicleRegistrationRequest {
  registrationNumber: string;
  issuingAuthority: string;
  issueDate: string; // YYYY-MM-DD
  expiryDate: string; // YYYY-MM-DD
  notes?: string | null;
}
```

### Insurance policy

`POST /api/vehicles/{vehicleId}/insurance-policies`

```ts
interface VehicleInsuranceRequest {
  providerName: string;
  policyNumber: string;
  coverageType?: string | null;
  effectiveFrom: string;
  expiryDate: string;
  claimReference?: string | null;
  claimContact?: string | null;
  notes?: string | null;
}
```

### Periodic inspection

`POST /api/vehicles/{vehicleId}/inspections`

```ts
interface VehicleInspectionRequest {
  inspectionNumber: string;
  stationName: string;
  inspectionDate: string;
  expiryDate: string;
  result: 1 | 2 | 3; // Passed, Conditional, Failed
  odometer?: number | null;
  failureNotes?: string | null;
  notes?: string | null;
}
```

### Operation card

`POST /api/vehicles/{vehicleId}/operation-cards`

```ts
interface VehicleOperationCardRequest {
  cardNumber: string;
  issuingAuthority: string;
  issueDate: string;
  expiryDate: string;
  notes?: string | null;
}
```

Operation-card dates are accepted only for a public-transport vehicle.

After saving dates, refetch the compliance dashboard and vehicle summary. The file remains independently uploaded; saving dates does not replace or remove it.

## Compliance history endpoints

Permission: `fleet.compliance.read`.

- `GET /api/vehicles/{vehicleId}/registrations`
- `GET /api/vehicles/{vehicleId}/insurance-policies`
- `GET /api/vehicles/{vehicleId}/inspections`
- `GET /api/vehicles/{vehicleId}/operation-cards`

These endpoints return dated compliance-record history only. They do not return standalone uploaded files. Therefore, an Istimara PDF uploaded without dates can produce an empty registration-history array while the dashboard correctly returns `hasUploadedFile: true` and `status: 6`.

```ts
export interface VehicleComplianceHistoryItem {
  id: string;
  vehicleId: string;
  type: string;
  number: string;
  issuer: string;
  effectiveFrom: string;
  expiryDate: string;
  dueStatus: VehicleComplianceDueStatus;
  isCurrent: boolean;
  previousRecordId: string | null;
  rowVersion: string;
}
```

## Recommended row design

Display independent columns/actions:

| Column | Source |
|---|---|
| Vehicle | `assetNumber` |
| Compliance item | `type` |
| Document | `hasUploadedFile` and `uploadedFile.originalFileName` |
| Start date | `effectiveFrom` |
| End date | `expiryDate` |
| Date condition | `dateStatus` |
| Overall condition | `status` |
| Actions | Upload/replace file, download file, add/renew dates |

For `status = 6`, render a neutral/informational badge such as “Uploaded — dates not entered,” not an error badge saying “Not uploaded.”

## Refresh rules

1. After file upload/replacement, refetch file metadata, vehicle summary, and due compliance.
2. After adding/renewing dates, refetch vehicle summary, compliance history, and due compliance.
3. Do not optimistically copy date values from the file upload response; file upload responses contain no compliance dates.
4. Do not optimistically mark a document as uploaded after a failed request. Use the returned `currentVersionId` or a refetched `hasUploadedFile` value.
