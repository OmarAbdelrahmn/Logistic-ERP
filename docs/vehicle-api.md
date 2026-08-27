# Response`.

### `POST /api/vehicle-assignments/switch`

Atomically ends the current vehicle assignment and starts a new vehicle assignment for the same rider. Uses multipart form like `take`.

`metadata` JSON shape (`SwitchVehicleRequest`):

```json
{
  "currentAssignmentId": "00000000-0000-0000-0000-000000000000",
  "newVehicleId": "00000000-0000-0000-0000-000000000000",
  "switchedAtUtc": "2026-08-26T12:00:00Z",
  "oldVehicleOdometer": 12050,
  "newVehicleOdometer": 5000,
  "oldVehicleCondition": 2,
  "newVehicleCondition": 2,
  "oldFuelLevelPercentage": 60,
  "newFuelLevelPercentage": 90,
  "permissionReference": "PERM-124",
  "reason": "Vehicle replacement",
  "rowVersion": "AAAAAAA..."
}
```

Requires `Idempotency-Key`; response is `RiderVehicleAssignmentResponse` for the resulting assignment.

### `POST /api/vehicle-assignments/{assignmentId}/renew-permission`

Updates the permission date/reference for an assignment. Requires `Idempotency-Key`.

```json
{
  "permissionStartsOn": "2026-08-27",
  "permissionReference": "PERM-125",
  "reason": "Permission renewed",
  "rowVersion": "AAAAAAA..."
}
```

Response: `200 OK`, `RiderVehicleAssignmentResponse`.

## Timeline endpoints

### `GET /api/riders/{riderProfileId}/vehicle-timeline`

Returns a rider's vehicle assignment timeline. Each item includes assignment data and related issues and accidents.

Response: `RiderVehicleTimelineResponse[]`.

### `GET /api/riders/{riderProfileId}/promissory-files`

Returns the rider's active promissory-file metadata. The service enforces a maximum of three active promissory-note files.

Response: `RiderPromissoryFileResponse[]`.

### `GET /api/riders/{riderProfileId}/promissory-files/{fileId}/download?versionId={guid}`

Downloads the current or requested historical version of a rider promissory file. Response is binary.

## Compliance endpoints
Vehicle API Reference

This document describes the vehicle and fleet endpoints implemented by the Logistics ERP API.

The reference is based on the controllers, application contracts, fleet services, domain enums, and error handling currently in the repository.

## Contents

- [API conventions](#api-conventions)
- [Authorization](#authorization)
- [Common data conventions](#common-data-conventions)
- [Vehicle catalog endpoints](#vehicle-catalog-endpoints)
- [Supplier endpoints](#supplier-endpoints)
- [Vehicle endpoints](#vehicle-endpoints)
- [Vehicle file endpoints](#vehicle-file-endpoints)
- [Assignment endpoints](#assignment-endpoints)
- [Timeline endpoints](#timeline-endpoints)
- [Compliance endpoints](#compliance-endpoints)
- [Issue endpoints](#issue-endpoints)
- [Accident endpoints](#accident-endpoints)
- [Enum values](#enum-values)
- [Error responses](#error-responses)
- [Recommended lifecycle](#recommended-lifecycle)

## API conventions

### Base URL

All routes are relative to the deployed API base URL. The routes in this document include the `/api` prefix.

### Authentication

The API uses JWT bearer authentication. Send:

```http
Authorization: Bearer <access-token>
Content-Type: application/json
```

The application has a global authorization fallback policy. Fleet services also perform permission and vehicle-scope checks before executing operations.

### JSON naming

Requests and responses use the ASP.NET web JSON configuration, so C# property names are normally serialized as camelCase:

```json
{
  "vehicleId": "00000000-0000-0000-0000-000000000000",
  "currentOdometer": 12500
}
```

### Successful response status codes

Most successful operations return `200 OK` with the result value directly in the response body. Archive operations return `204 No Content`. File downloads return the stored file bytes with the stored content type and download filename.

### Dates and times

- `DateOnly` values use `YYYY-MM-DD`, for example `2026-08-26`.
- `DateTimeOffset` values should include an offset or use UTC, for example `2026-08-26T10:30:00Z`.
- Fields ending in `Utc` are expected to represent UTC timestamps.

### Pagination

Paged endpoints accept `page` and `pageSize`. Defaults are `page=1` and `pageSize=50`. The service normalizes pagination values before querying.

Paged responses have this shape:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 50,
  "totalCount": 0
}
```

### Row-version concurrency

Mutable catalog, vehicle, assignment, issue, and archive actions use a base64 `rowVersion` returned by a previous response. Send that value back in the request. If the record changed after it was read, the API returns `409 Conflict` with `fleet.concurrency_conflict`.

### Idempotency

The following commands require a non-empty `Idempotency-Key` header:

- create vehicle issue
- create accident
- take vehicle
- return vehicle
- switch vehicle
- renew vehicle permission

Repeat the same request with the same key to replay the original result. Reusing a key with a different request returns `409 Conflict` with `fleet.idempotency_conflict`.

## Authorization

The relevant permission keys are:

| Permission | Used for |
|---|---|
| `fleet.vehicles.read` | Vehicle identity, lists, lookups, status, readiness, and catalogs |
| `fleet.vehicles.manage` | Create/update vehicles, manufacturers, models, suppliers, and normal status changes |
| `fleet.vehicles.archive` | Archive and restore vehicles or suppliers |
| `fleet.vehicles.decommission` | Decommission a vehicle |
| `fleet.assignments.read` | Assignment and rider/vehicle timelines |
| `fleet.assignments.manage` | Take, return, switch, and permission renewal |
| `fleet.assignments.correct` | High-trust assignment corrections handled by the service layer |
| `fleet.issues.read` | Issue list |
| `fleet.issues.manage` | Create and transition issues |
| `fleet.compliance.read` | Compliance history and due records |
| `fleet.compliance.manage` | Add or renew compliance records |
| `fleet.files.read` | Vehicle file metadata and versions |
| `fleet.files.upload` | Upload vehicle files |
| `fleet.files.download` | Download vehicle and rider promissory files |
| `fleet.accidents.read` | Accident details and lists |
| `fleet.accidents.report` | Create accidents and upload evidence |
| `fleet.accidents.finalize` | Finalize, correct, and close accidents |
| `fleet.accidents.download` | Download accident evidence and PDF reports |
| `fleet.corrections.manage` | Identity and odometer corrections |
| `fleet.registration_transitions.manage` | Private-to-public registration transition |

Access can also be restricted by the vehicle's configured scope, such as sponsor or operating city.

## Common data conventions

### Identifier fields

All `id`, `vehicleId`, `riderProfileId`, `assignmentId`, `attachmentId`, `fileId`, `manufacturerId`, `modelId`, and `supplierId` values are GUIDs.

### Nullable fields

Properties shown with `?` or described as nullable may be omitted or sent as `null`. The service still enforces required business fields such as asset number, catalog references, dates, reasons, and row versions for specific operations.

### Binary files

Uploads use `multipart/form-data`. Downloads return a binary response, not JSON. The file service uses private storage and validates file metadata, size, and content before accepting an upload.

## Vehicle catalog endpoints

### Manufacturers

#### `GET /api/vehicle-catalogs/manufacturers`

Returns all vehicle manufacturers ordered by display order and then English name. Requires `fleet.vehicles.read`.

Response: `200 OK`, `VehicleManufacturerResponse[]`.

#### `POST /api/vehicle-catalogs/manufacturers`

Creates a manufacturer. The service normalizes the code, trims names, rejects missing code/names, and rejects duplicate codes. Requires `fleet.vehicles.manage`.

Request body: `VehicleManufacturerRequest`.

```json
{
  "code": "TOYOTA",
  "nameAr": "تويوتا",
  "nameEn": "Toyota",
  "status": 1,
  "displayOrder": 10,
  "rowVersion": null
}
```

Response: `200 OK`, `VehicleManufacturerResponse`.

#### `PUT /api/vehicle-catalogs/manufacturers/{id}`

Updates a manufacturer using the same request shape. The `rowVersion` must match the current record. Returns `404` when the manufacturer does not exist, `409` for a duplicate code or stale row version.

Response: `200 OK`, `VehicleManufacturerResponse`.

### Models

#### `GET /api/vehicle-catalogs/models?manufacturerId={guid}`

Returns vehicle models. `manufacturerId` is optional; when supplied, only models belonging to that manufacturer are returned. Requires `fleet.vehicles.read`.

Response: `200 OK`, `VehicleModelResponse[]`.

#### `POST /api/vehicle-catalogs/models`

Creates a model. The manufacturer must exist, and the code must be unique within that manufacturer. Requires `fleet.vehicles.manage`.

Request body: `VehicleModelRequest`.

```json
{
  "vehicleManufacturerId": "00000000-0000-0000-0000-000000000000",
  "code": "COROLLA",
  "nameAr": "كورولا",
  "nameEn": "Corolla",
  "vehicleType": 2,
  "defaultFuelType": 1,
  "status": 1,
  "rowVersion": null
}
```

Response: `200 OK`, `VehicleModelResponse`.

#### `PUT /api/vehicle-catalogs/models/{id}`

Updates a model with the same validation and optimistic-concurrency rules. Response: `200 OK`, `VehicleModelResponse`.

### Catalog response schemas

`VehicleManufacturerResponse`:

| Field | Type | Description |
|---|---|---|
| `id` | GUID | Manufacturer identifier |
| `code` | string | Normalized unique code |
| `nameAr` / `nameEn` | string | Arabic and English names |
| `status` | enum number | Catalog status |
| `displayOrder` | integer | Ordering value |
| `rowVersion` | string | Base64 concurrency token |

`VehicleModelResponse` contains `id`, `vehicleManufacturerId`, `code`, `nameAr`, `nameEn`, `vehicleType`, `defaultFuelType`, `status`, and `rowVersion`.

## Supplier endpoints

### `GET /api/vehicle-suppliers`

Returns suppliers ordered by English name. Requires `fleet.vehicles.read`.

Response: `200 OK`, `VehicleSupplierResponse[]`.

### `GET /api/vehicle-suppliers/{id}`

Returns one supplier. Response: `200 OK`, `VehicleSupplierResponse`; `404` if absent.

### `POST /api/vehicle-suppliers`

Creates a supplier. Code, Arabic name, English name, and address are required. Code, commercial registration number, and tax number are checked for duplicates. Requires `fleet.vehicles.manage`.

### `PUT /api/vehicle-suppliers/{id}`

Updates a supplier. Uses `rowVersion` and the same duplicate checks. Response: `200 OK`, `VehicleSupplierResponse`.

### `PATCH /api/vehicle-suppliers/{id}/archive`

Soft-deletes/archives a supplier. The request must include a non-empty reason and current row version. Response: `204 No Content`.

Request body:

```json
{
  "reason": "Supplier no longer used",
  "rowVersion": "AAAAAAA..."
}
```

### Supplier request and response fields

`VehicleSupplierRequest` fields are `code`, `nameAr`, `nameEn`, `commercialRegistrationNumber`, `taxNumber`, `phone`, `address`, `status`, `notes`, and `rowVersion`.

`address` contains `buildingNumber`, `street`, `district`, `city`, `postalCode`, and `additionalNumber`.

`VehicleSupplierResponse` adds the generated `id` and returns the normalized values plus `rowVersion`.

## Vehicle endpoints

### `GET /api/vehicles`

Returns a paged vehicle list. Optional query parameters:

| Parameter | Type | Description |
|---|---|---|
| `search` | string | Searches the configured vehicle identity fields |
| `status` | string | Operational status name, parsed case-insensitively |
| `operatingCityId` | GUID | Restricts results to an operating city |
| `page` | integer | Page number, default `1` |
| `pageSize` | integer | Page size, default `50` |

Response: `200 OK`, `PagedResponse<VehicleSummaryResponse>`.

### `GET /api/vehicles/lookup?search={text}`

Returns a lightweight list for selectors and autocomplete. The service asks for up to 200 vehicles and returns `VehicleLookupResponse[]`.

### `GET /api/vehicles/{id}`

Returns the full vehicle detail including the summary, identity, ownership, registration type, catalog references, acquisition, lease, decommissioning, and notes.

Response: `200 OK`, `VehicleDetailResponse`.

### `POST /api/vehicles`

Creates a vehicle from `VehicleUpsertRequest`. Requires `fleet.vehicles.manage`. The manufacturer and model references must be valid and the identity must satisfy fleet business rules. When `assetNumber` is omitted, `null`, or whitespace, the backend generates it as `VEH-YYYYMMDD-XXXXXXXX`, for example `VEH-20260826-1A2B3C4D`. A supplied asset number is preserved after normalization and must be unique.

Response: `200 OK`, `VehicleDetailResponse`.

### `PUT /api/vehicles/{id}`

Updates a vehicle using the same request shape. `assetNumber` remains required for updates; automatic generation applies only when creating a new vehicle. Include the current `rowVersion`; stale writes return `409`.

### Vehicle upsert request

```json
{
  "assetNumber": "VH-0001",
  "serialNumber": "SN-123",
  "plateNumberAr": "أ ب ج 1234",
  "plateNumberEn": "ABC 1234",
  "plateLettersAr": "أ ب ج",
  "plateLettersEn": "ABC",
  "plateDigits": "1234",
  "vin": "1HGBH41JXMN000000",
  "chassisNumber": "CH-123",
  "engineNumber": "EN-123",
  "sponsorId": null,
  "operatingCityId": null,
  "purchasedFromSupplierId": null,
  "registrationType": 2,
  "vehicleManufacturerId": "00000000-0000-0000-0000-000000000000",
  "vehicleModelId": "00000000-0000-0000-0000-000000000000",
  "modelYear": 2025,
  "vehicleType": 2,
  "fuelType": 1,
  "transmissionType": 2,
  "colorAr": "أبيض",
  "colorEn": "White",
  "ownershipType": 1,
  "ownerName": null,
  "acquisitionDate": "2025-01-15",
  "leaseReference": null,
  "currentOdometer": 0,
  "notes": null,
  "rowVersion": null
}
```

### `PATCH /api/vehicles/{id}/archive`

Archives a vehicle with a reason and current row version. This is a no-content operation and is distinct from operational decommissioning. Requires `fleet.vehicles.archive`.

### `PATCH /api/vehicles/{id}/restore`

Restores an archived vehicle using a `RowVersionRequest`:

```json
{ "rowVersion": "AAAAAAA..." }
```

Response: `200 OK`, `VehicleDetailResponse`. Requires `fleet.vehicles.archive`.

### `POST /api/vehicles/{id}/{statusAction}`

Changes an administrative status. `statusAction` must be one of `stolen`, `recover`, `out-of-service`, `restore`, or `decommission`.

Request body: `VehicleStatusCommandRequest`.

```json
{
  "effectiveAtUtc": "2026-08-26T10:30:00Z",
  "reason": "Reported stolen by operations",
  "rowVersion": "AAAAAAA..."
}
```

Response: `200 OK`, `VehicleDetailResponse`. `decommission` requires the high-trust decommission permission; the other actions use vehicle-management permission. Invalid state transitions return `fleet.invalid_state`.

### `GET /api/vehicles/{id}/status-history`

Returns the immutable operational status periods for the vehicle, including the source and source entity. Response: `VehicleStatusPeriodResponse[]`.

### `POST /api/vehicles/{id}/odometer`

Records a reading. A correction must be explicitly marked with `isCorrection=true`, include a correction reason, and be authorized. Normal readings cannot decrease the current odometer.

```json
{
  "reading": 12500,
  "recordedAtUtc": "2026-08-26T10:30:00Z",
  "notes": "Dashboard reading",
  "isCorrection": false,
  "correctionReason": null,
  "rowVersion": "AAAAAAA..."
}
```

Response: `200 OK`, `VehicleOdometerReadingResponse`.

### `GET /api/vehicles/{id}/odometer`

Returns the vehicle's odometer history, including source type, correction flag, correction reason, notes, reading, and recorded timestamp.

### `GET /api/vehicles/{id}/rider-timeline`

Returns vehicle assignment timeline entries. Each entry includes the assignment plus related vehicle issues and accidents.

### `GET /api/vehicles/{id}/readiness`

Evaluates whether a vehicle can be assigned. The response identifies missing core identity fields, missing photo sides, missing documents, warnings, and the final `isEligibleForAssignment` decision.

### `POST /api/vehicles/{id}/identity-corrections`

Applies a high-trust correction to vehicle identity. The request requires complete corrected identity values, sponsor/city, registration type, a reason, effective timestamp, current row version, and optionally document version references. The service records before/after JSON for auditability.

Response: `200 OK`, `VehicleDetailResponse`.

### `GET /api/vehicles/{id}/identity-corrections`

Returns the correction audit history. Each item contains `beforeJson`, `afterJson`, optional document references, reason, effective time, actor, and creation time.

### `POST /api/vehicles/{id}/registration-transitions/private-to-public`

Converts a vehicle from private transport registration to public transport registration. The request is `multipart/form-data` and must contain both registration documents and transition metadata.

Form fields:

| Field | Type | Required | Description |
|---|---|---:|---|
| `plateNumberAr` | string | yes | New Arabic plate number |
| `plateNumberEn` | string | yes | New English plate number |
| `plateLettersAr` | string | no | New Arabic letters |
| `plateLettersEn` | string | no | New English letters |
| `plateDigits` | string | no | New plate digits |
| `effectiveAtUtc` | DateTimeOffset | yes | Effective transition time |
| `reason` | string | yes | Business reason |
| `rowVersion` | string | yes | Current vehicle concurrency token |
| `istimara` | file | yes | New registration document |
| `operationCard` | file | yes | New operation card |

The controller limits this request to 22 MiB and rejects missing or empty documents with `400 Bad Request`. Response: `200 OK`, `VehicleRegistrationTransitionResponse`.

### `GET /api/vehicles/{id}/registration-transitions`

Returns the immutable registration transition history, including old/new plate values, from/to registration types, effective date, reason, document version IDs, actor, and creation time.

## Vehicle file endpoints

### `GET /api/vehicles/{vehicleId}/files`

Returns file attachment metadata for the vehicle. Response: `VehicleAttachmentResponse[]`.

### `PUT /api/vehicles/{vehicleId}/files/{kind}`

Uploads or replaces the fixed file slot identified by `kind`. Supported kinds include `istimara`, `operationCard`, `frontImage`, `rearImage`, `leftImage`, and `rightImage` (enum values may also be sent numerically depending on JSON settings).

Request: `multipart/form-data` with a `file` field. The controller rejects an empty file and limits the request to 11 MiB. Response: `200 OK`, `VehicleAttachmentResponse`.

### `GET /api/vehicles/{vehicleId}/files/{attachmentId}/versions`

Returns all versions of an attachment, including version number, original filename, content type, size, checksum, and upload timestamp.

### `GET /api/vehicles/{vehicleId}/files/{attachmentId}/download?versionId={guid}`

Downloads the current attachment version when `versionId` is omitted, or the requested historical version when supplied. Response is the file bytes with `Content-Type`, `Content-Disposition`, and range processing enabled.

## Assignment endpoints

Assignments connect a rider to a vehicle and update operational status, odometer history, and audit events.

### `POST /api/vehicle-assignments/take`

Starts an assignment. This is `multipart/form-data` because promissory-note files may be uploaded with the command. The JSON command is sent as a string in the `metadata` form field, and files are sent as one or more `promissoryFiles` fields.

Required header: `Idempotency-Key`.

`metadata` JSON shape (`TakeVehicleRequest`):

```json
{
  "riderProfileId": "00000000-0000-0000-0000-000000000000",
  "vehicleId": "00000000-0000-0000-0000-000000000000",
  "startedAtUtc": "2026-08-26T08:00:00Z",
  "startOdometer": 12000,
  "startCondition": 2,
  "startFuelLevelPercentage": 80,
  "permissionReference": "PERM-123",
  "reason": "Daily vehicle handover",
  "notes": null
}
```

The vehicle must be available and the rider must be eligible without another active vehicle. The service creates the assignment and associated operational history. Response: `200 OK`, `RiderVehicleAssignmentResponse`.

The request limit is 32 MiB. Invalid or missing `metadata` returns `400 Bad Request`.

### `POST /api/vehicle-assignments/return`

Completes an active assignment and records end time, end odometer, condition, fuel level, reason, and concurrency token. Requires `Idempotency-Key`.

```json
{
  "assignmentId": "00000000-0000-0000-0000-000000000000",
  "endedAtUtc": "2026-08-26T18:00:00Z",
  "endOdometer": 12120,
  "endCondition": 2,
  "endFuelLevelPercentage": 65,
  "reason": "End of shift",
  "rowVersion": "AAAAAAA..."
}
```

Response: `200 OK`, `RiderVehicleAssignment
### `GET /api/vehicles/{vehicleId}/{type}`

Returns compliance history. `type` must be one of:

- `registrations`
- `insurance-policies`
- `inspections`

Response: `VehicleComplianceResponse[]`, ordered by expiry date descending. Invalid types return `400`.

### `POST /api/vehicles/{vehicleId}/registrations`

Adds a registration record and makes it current. The previous current record, when present, is marked superseded.

Request (`VehicleRegistrationRequest`): `registrationNumber`, `issuingAuthority`, `issueDate`, `expiryDate`, `notes`.

`expiryDate` cannot be earlier than `issueDate`. Response: `200 OK`, `VehicleComplianceResponse`.

### `POST /api/vehicles/{vehicleId}/insurance-policies`

Adds and makes current an insurance policy. Previous current policy records are superseded.

Request (`VehicleInsuranceRequest`): `providerName`, `policyNumber`, `coverageType`, `effectiveFrom`, `expiryDate`, `claimReference`, `claimContact`, `notes`.

`expiryDate` cannot be earlier than `effectiveFrom`. Response: `VehicleComplianceResponse`.

### `POST /api/vehicles/{vehicleId}/inspections`

Adds and makes current a periodic inspection. Previous current inspections are superseded.

Request (`VehicleInspectionRequest`): `inspectionNumber`, `stationName`, `inspectionDate`, `expiryDate`, `result`, `odometer`, `failureNotes`, `notes`.

`expiryDate` cannot be earlier than `inspectionDate`; an odometer value cannot be negative. Response: `VehicleComplianceResponse`.

### `GET /api/vehicle-compliance/due?checkDate=YYYY-MM-DD`

Returns all non-valid compliance items for the requested date. If `checkDate` is omitted, the service uses the current local business date (`UTC+3`). Results include registration, insurance, and inspection entries whose status is not `Valid`, ordered by expiry date.

Response: `VehicleComplianceDueResponse[]`.

## Issue endpoints

### `GET /api/vehicle-issues`

Returns a paged issue list. Optional query parameters are `vehicleId`, `status`, `page`, and `pageSize`. `status` is parsed as a `VehicleIssueStatus` name.

Response: `PagedResponse<VehicleIssueSummaryResponse>`.

### `POST /api/vehicle-issues`

Creates an issue for a vehicle. Requires `Idempotency-Key` and `fleet.issues.manage`.

Request (`CreateVehicleIssueRequest`): `vehicleId`, `category`, `severity`, `description`, `reportedAtUtc`, `locationDescription`, `odometerAtReport`, and `blocksOperation`.

If `blocksOperation=true`, the service ends the active assignment and places the vehicle in `ProblemHold`.

### `POST /api/vehicle-issues/{id}/{operation}`

Performs a state transition. `operation` must be `review`, `close`, or `reject`.

Request: `{ "reason": "...", "rowVersion": "..." }`.

Allowed transitions:

| Operation | Required current state | New state |
|---|---|---|
| `review` | `Open` | `UnderReview` |
| `reject` | `Open` or `UnderReview` | `Rejected` |
| `close` | `Resolved` or `Rejected` | `Closed` |

Rejecting a blocking issue can restore the vehicle after the issue state is changed. Response: `VehicleIssueSummaryResponse`.

### `POST /api/vehicle-issues/{id}/resolve`

Resolves an issue in `Open` or `UnderReview` state. Requires a non-empty resolution summary and current row version. A blocking issue triggers vehicle restoration logic.

Request: `{ "resolutionSummary": "Brake repaired", "rowVersion": "AAAAAAA..." }`.

Response: `200 OK`, `VehicleIssueSummaryResponse`.

## Accident endpoints

### `GET /api/vehicle-accidents`

Returns a paged accident list. Optional filters are `vehicleId`, `riderProfileId`, `page`, and `pageSize`.

Response: `PagedResponse<VehicleAccidentSummaryResponse>`.

### `GET /api/vehicle-accidents/{id}`

Returns full accident detail, including rider and vehicle snapshot data, evidence attachments, and generated report versions.

Response: `VehicleAccidentDetailResponse`.

### `POST /api/vehicle-accidents`

Creates an accident. Requires `Idempotency-Key`, accident-report permission, an existing vehicle, and a rider who held that vehicle at the reported time. The service creates or associates the relevant issue/assignment records and places the vehicle into accident handling as applicable.

Request (`CreateVehicleAccidentRequest`):

```json
{
  "vehicleId": "00000000-0000-0000-0000-000000000000",
  "riderProfileId": "00000000-0000-0000-0000-000000000000",
  "occurredAtUtc": "2026-08-26T14:30:00Z",
  "locationDescription": "King Fahd Road",
  "latitude": 24.7136,
  "longitude": 46.6753,
  "policeReportNumber": "POL-123",
  "insuranceClaimNumber": "CLM-123",
  "severity": 2,
  "isDrivable": true,
  "hasInjuries": false,
  "injuryDetails": null,
  "thirdPartyDetails": null,
  "damageDescription": "Front bumper damage",
  "faultAssessment": null,
  "narrative": "The vehicle was hit from behind."
}
```

Response: `200 OK`, `VehicleAccidentDetailResponse`.

### `POST /api/vehicle-accidents/{id}/evidence`

Uploads one accident evidence file using `multipart/form-data` with `evidenceType` and `file` fields. Empty files are rejected and the controller limit is 11 MiB. Response: `VehicleAccidentAttachmentResponse`.

### `GET /api/vehicle-accidents/{id}/evidence/{attachmentId}/download`

Downloads accident evidence as binary with range processing enabled.

### `POST /api/vehicle-accidents/{id}/finalize`

Finalizes the accident report and generates a report version. Request: `AccidentActionRequest` with `reason` and `rowVersion`. Response: `VehicleAccidentReportVersionResponse`.

### `POST /api/vehicle-accidents/{id}/correct`

Corrects accident details. It accepts the corrected police/insurance references, location, coordinates, severity, drivability, injury data, third-party data, damage, fault assessment, narrative, a mandatory correction reason, and current row version. Response: `VehicleAccidentReportVersionResponse`.

### `POST /api/vehicle-accidents/{id}/close`

Closes an accident after the appropriate finalized state. Request: `AccidentActionRequest`. Response: `VehicleAccidentDetailResponse`.

### `GET /api/vehicle-accidents/{id}/pdf?reportVersionId={guid}`

Downloads the current generated accident PDF, or the requested report version when `reportVersionId` is supplied. Response is binary, normally `application/pdf`.

## Response schemas

### Vehicle summary

`VehicleSummaryResponse` fields:

| Field | Description |
|---|---|
| `id`, `assetNumber` | Primary identity |
| `plateNumberAr`, `plateNumberEn`, `serialNumber` | Main registration/serial identifiers |
| `manufacturer`, `model` | Display names |
| `vehicleType`, `registrationType`, `status` | Classification and operational state |
| `sponsorId`, `sponsorName`, `operatingCityId`, `operatingCity` | Scope and ownership relationships |
| `currentOdometer` | Latest odometer reading |
| `currentAssignmentId`, `currentRiderProfileId`, `currentRiderName` | Active assignment, if any |
| `registrationExpiryDate`, `registrationStatus` | Registration compliance |
| `insuranceExpiryDate`, `insuranceStatus` | Insurance compliance |
| `inspectionExpiryDate`, `inspectionStatus` | Inspection compliance |
| `isReadyForAssignment` | Current readiness decision |
| `rowVersion` | Concurrency token |

`VehicleDetailResponse` contains `summary` plus `serialNumber`, `vin`, `chassisNumber`, `engineNumber`, sponsor/city/supplier IDs and supplier name, registration type, manufacturer/model IDs, model year, fuel/transmission, colors, ownership, owner name, acquisition date, lease reference, decommissioning data, and notes.

### Assignment response

`RiderVehicleAssignmentResponse` contains assignment `id`, rider and employee IDs, vehicle ID and asset number, rider name, start/end timestamps, location snapshots, start/end odometers, permission reference and dates, status, assignment reason, completion reason, operation ID, promissory-file version IDs, and row version.

### Issue response

`VehicleIssueSummaryResponse` contains `id`, `issueNumber`, `vehicleId`, category, severity, blocking flag, status, report timestamp, description, location, optional resolution summary, and row version.

### Accident response

`VehicleAccidentSummaryResponse` contains accident number, vehicle/rider/assignment/issue IDs, occurrence time, severity, drivability, status, location, and row version. `VehicleAccidentDetailResponse` adds rider/vehicle display values, police and insurance references, injury and third-party details, damage, fault assessment, narrative, evidence attachments, and report versions.

## Enum values

The API contracts use numeric enum values by default. The names below are the canonical domain names.

| Enum | Values |
|---|---|
| `VehicleCatalogStatus` | `Active=1`, `Disabled=2`, `Archived=3` |
| `VehicleType` | `Motorcycle=1`, `Car=2`, `Van=3`, `Truck=4`, `Other=5` |
| `VehicleFuelType` | `Petrol=1`, `Diesel=2`, `Electric=3`, `Hybrid=4`, `Other=5` |
| `VehicleTransmissionType` | `Manual=1`, `Automatic=2`, `Other=3` |
| `VehicleOwnershipType` | `Owned=1`, `Leased=2`, `ThirdParty=3` |
| `VehicleRegistrationType` | `Private=1`, `PrivateTransport=2`, `SmallBus=3`, `Taxi=4`, `PublicTransport=5`, `PublicBus=6`, `Motorcycle=7`, `PublicWorks=8` |
| `VehicleOperationalStatus` | `Available=1`, `Assigned=2`, `ProblemHold=3`, `AccidentHold=4`, `Stolen=5`, `OutOfService=6`, `Decommissioned=7` |
| `VehicleCondition` | `Unknown=1`, `Good=2`, `Fair=3`, `Damaged=4`, `Unsafe=5` |
| `VehicleInspectionResult` | `Passed=1`, `Conditional=2`, `Failed=3` |
| `VehicleComplianceDueStatus` | `Valid=1`, `Upcoming=2`, `DueToday=3`, `Expired=4`, `Missing=5` |
| `VehicleFileKind` | `Istimara=1`, `OperationCard=2`, `FrontImage=3`, `RearImage=4`, `LeftImage=5`, `RightImage=6`, `Legacy=99` |
| `RiderVehicleAssignmentStatus` | `Active=1`, `Completed=2`, `Cancelled=3`, `Corrected=4` |
| `VehicleIssueCategory` | `Problem=1`, `Accident=2`, `Theft=3`, `Damage=4`, `Administrative=5` |
| `VehicleIssueSeverity` | `Low=1`, `Medium=2`, `High=3`, `Critical=4` |
| `VehicleIssueStatus` | `Open=1`, `UnderReview=2`, `Resolved=3`, `Closed=4`, `Rejected=5` |
| `VehicleAccidentStatus` | `Reported=1`, `Finalized=2`, `Closed=3` |
| `VehicleAccidentSeverity` | `Minor=1`, `Moderate=2`, `Serious=3`, `Critical=4` |
| `VehicleAccidentEvidenceType` | `Image=1`, `UploadedReport=2`, `Other=3` |

## Error responses

Business failures are returned as RFC-style `ProblemDetails` with this shape:

```json
{
  "status": 409,
  "title": "fleet.concurrency_conflict",
  "detail": "The record changed after it was loaded. Reload it and retry.",
  "type": "https://httpstatuses.io/409",
  "instance": "/api/vehicles/00000000-0000-0000-0000-000000000000",
  "errorCode": "fleet.concurrency_conflict",
  "correlationId": "request-correlation-id"
}
```

Common fleet error codes:

| Code | HTTP status | Meaning |
|---|---:|---|
| `fleet.invalid_request` | 400 | Required data missing or business validation failed |
| `fleet.idempotency_required` | 400 | Required `Idempotency-Key` was not supplied |
| `fleet.invalid_file` | 400 | File is empty, unsupported, too large, or invalid |
| `fleet.not_found` | 404 | Record or file was not found |
| `fleet.concurrency_conflict` | 409 | Stale or missing row version on a protected update |
| `fleet.conflict` | 409 | Generic state or business conflict |
| `fleet.vehicle_unavailable` | 409 | Vehicle cannot be assigned |
| `fleet.rider_unavailable` | 409 | Rider cannot receive another active vehicle |
| `fleet.invalid_state` | 409 | Requested state transition is not allowed |
| `fleet.odometer_decreased` | 409 | Reading decreased without authorized correction |
| `fleet.duplicate` | 409 | Unique catalog or supplier value already exists |
| `fleet.idempotency_conflict` | 409 | Same key used with a different request |
| `fleet.file_limit` | 409 | Rider exceeded the active promissory-file limit |
| `fleet.accident_assignment_mismatch` | 409 | Rider did not hold the vehicle at accident time |
| `fleet.current_user_unavailable` | 401 | Authenticated user could not be resolved |
| `fleet.forbidden` | 403 | User lacks the required permission or scope |

The controller may also return a plain `400 Bad Request` for malformed multipart metadata or missing/empty required upload files before the service is called.

## Recommended lifecycle

1. Load manufacturers and models from the catalog endpoints.
2. Create or update the vehicle with catalog references and initial identity.
3. Upload the required vehicle photos and registration documents.
4. Add current registration, insurance, and inspection records.
5. Call the readiness endpoint and resolve missing identity, files, documents, or warnings.
6. Take the vehicle for a rider using an idempotency key.
7. Record returns or switches with current odometers and row versions.
8. Report issues or accidents immediately when they occur.
9. Resolve/reject and close issues, or finalize/correct/close accidents through their explicit state transitions.
10. Use archive for administrative retention and decommission for the permanent operational end state.

## Source files

The primary implementation sources for this reference are:

- `src/LogisticsERP.Api/Controllers/VehiclesController.cs`
- `src/LogisticsERP.Api/Controllers/VehicleCatalogsController.cs`
- `src/LogisticsERP.Api/Controllers/VehicleSuppliersController.cs`
- `src/LogisticsERP.Api/Controllers/VehicleAssignmentsController.cs`
- `src/LogisticsERP.Api/Controllers/VehicleIssuesController.cs`
- `src/LogisticsERP.Api/Controllers/VehicleAccidentsController.cs`
- `src/LogisticsERP.Api/Controllers/VehicleComplianceController.cs`
- `src/LogisticsERP.Application/Features/Fleet/FleetContracts.cs`
- `src/LogisticsERP.Application/Features/Fleet/IFleetService.cs`
- `src/LogisticsERP.Application/Features/Fleet/FleetErrors.cs`
- `src/LogisticsERP.Domain/Enums/FleetEnums.cs`
