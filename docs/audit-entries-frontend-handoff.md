# Audit entries API — frontend handoff

## Access and scope

Both endpoints require the authenticated user's `audit.read` permission. Audit data is sensitive: do not display it to users without that permission, cache it in shared browser storage, or expose it in client-side logs.

The API returns camel-case JSON. Audit entries are append-only. New audit records also retain request evidence where available; older records can have `null` request fields because that context was not captured when they were created.

| Endpoint | Purpose |
| --- | --- |
| `GET /api/audit-entries` | Paginated, filterable audit timeline. |
| `GET /api/audit-entries/{eventId}` | Complete detail for one audit event. |

## 1. List audit entries

```http
GET /api/audit-entries?pageSize=50&entityType=Employee&fromUtc=2026-09-01T00:00:00Z
Authorization: Bearer <access-token>
```

### Query parameters

| Parameter | Type | Required | Description |
| --- | --- | --- | --- |
| `actorUserId` | UUID | No | Return actions performed by one user. |
| `entityType` | string | No | Exact affected record type, such as `Employee`, `Vehicle`, or `FuelCard`. |
| `entityId` | UUID | No | Return the timeline for one affected record. |
| `action` | string | No | Exact action: normally `Created`, `Updated`, `SoftDeleted`, or `Closed`. |
| `correlationId` | string | No | Groups events that were created by the same request/workflow. |
| `fromUtc` | ISO 8601 UTC datetime | No | Include events at or after this time. |
| `toUtc` | ISO 8601 UTC datetime | No | Include events at or before this time. |
| `pageSize` | integer | No | Number of entries to return. Default: `100`; allowed range: `1`–`200`. |
| `beforeSequence` | integer | No | Cursor for the next, older page. Supply the previous response's `nextCursor`. |

Entries are ordered newest first by `sequence`.

### Successful response — `200 OK`

```json
{
  "items": [
    {
      "eventId": "0199f18c-774a-7bea-8a3a-ccf8f1a4b5e2",
      "sequence": 5021,
      "actorUserId": "0199f180-1f82-796a-a6c4-ff8306c6070e",
      "actorType": "User",
      "action": "Updated",
      "category": "Persistence",
      "entityType": "Employee",
      "entityId": "0199ec47-b500-7d79-a577-dfa8e8ffd313",
      "occurredAtUtc": "2026-09-20T08:30:00+00:00",
      "correlationId": "0199f18c-7739-7108-bafa-40d9829974a0",
      "reason": null,
      "beforeJson": "{\"PhoneNumber\":\"0500000000\"}",
      "afterJson": "{\"PhoneNumber\":\"0555555555\"}",
      "source": "ApplicationDbContext",
      "schemaVersion": 1,
      "actor": {
        "userId": "0199f180-1f82-796a-a6c4-ff8306c6070e",
        "actorType": "User",
        "userName": "omar",
        "displayNameAr": "عمر",
        "displayNameEn": "Omar",
        "employeeId": "0199ec47-b500-7d79-a577-dfa8e8ffd313",
        "status": "Active"
      },
      "record": {
        "entityType": "Employee",
        "entityId": "0199ec47-b500-7d79-a577-dfa8e8ffd313",
        "displayLabel": "محمد أحمد",
        "displayCode": "EMP-1024"
      },
      "changes": [
        {
          "field": "PhoneNumber",
          "before": "0500000000",
          "after": "0555555555"
        }
      ],
      "request": {
        "sessionId": "0199f180-21ef-7d2e-97a8-37b9b9d63b0d",
        "supportAccessGrantId": null,
        "correlationId": "0199f18c-7739-7108-bafa-40d9829974a0",
        "traceId": "0HN9B1...",
        "ipAddress": "192.168.1.10",
        "userAgent": "Mozilla/5.0 ...",
        "source": "ApplicationDbContext"
      }
    }
  ],
  "nextCursor": "4920"
}
```

`nextCursor` is `null` when there are no more older entries. For the next page, send it unchanged as `beforeSequence`:

```http
GET /api/audit-entries?pageSize=50&beforeSequence=4920
```

## 2. Get one audit entry

```http
GET /api/audit-entries/0199f18c-774a-7bea-8a3a-ccf8f1a4b5e2
Authorization: Bearer <access-token>
```

### Successful response — `200 OK`

The response is one `AuditEntry` object with exactly the same shape as an item in the list response. Use this endpoint for the audit-detail drawer/page when the user selects an event.

## Audit-entry fields

| Field | Description |
| --- | --- |
| `eventId` | Permanent ID for this audit event; use it to request the detail endpoint. |
| `sequence` | Monotonically increasing timeline sequence. Use only for paging; do not treat it as an entity ID. |
| `actorUserId`, `actor` | The acting account. `actor` is `System` when no human account performed the action. The user may be archived but is still resolved for audit evidence. |
| `action` | The operation performed. Show this prominently in the timeline. |
| `category` | Audit category; current persistence events use `Persistence`. |
| `entityType`, `entityId`, `record` | The affected business record. `record.displayLabel` and `record.displayCode` are convenience values and can be `null`. |
| `occurredAtUtc` | UTC time of the event. Convert to the user's selected display timezone. |
| `reason` | Business reason for the event, normally populated for soft deletion or explicit workflows. |
| `changes` | Preferred structured before/after view. Each item is one non-sensitive changed field. |
| `beforeJson`, `afterJson` | Raw JSON snapshots retained for advanced inspection. They are JSON strings, so parse them only when the raw view is opened. |
| `request` | Session/request evidence. `ipAddress`, `userAgent`, `traceId`, and `sessionId` may be `null`, particularly on historical or worker-generated events. |
| `correlationId` | Identifies related events from a single request or workflow. Use it to link/filter related timeline rows. |

## Supported `entityType` target records

`entityType` is the exact .NET entity class name (case-sensitive for filtering). The frontend should use this catalog to translate a type into a localized module/record label, but must still gracefully render an unknown type because new modules can be added without a frontend deployment.

### Platform and reference data

`CompanyProfile`, `GlobalCity`, `OperatingCity`, `ClientPlatform`, `PermissionDefinition`

### Workforce, HR, legal, leave, and compliance

`Employee`, `PayrollEmployee`, `RiderProfile`, `EmployeeWorkHistory`, `JobTitle`, `Sponsor`, `ResidencyProfession`, `OperationalWorkType`, `JobTitleOperationalWorkType`, `DriverLicenseCategory`, `EmployeeDriverLicense`, `RiderCard`, `RiderHealthCard`, `EmployeePromissoryNote`, `HrFormTemplate`, `HrFormTemplateVersion`, `InsuranceCompany`, `InsurancePlanLevel`, `EmployeeMedicalInsurancePolicy`, `LeaveType`, `LeaveRequest`, `LeaveApprovalWorkflow`, `LeaveApprovalWorkflowStep`, `LeaveApprovalDecision`, `LeaveDateChangeRequest`, `LeaveCancellationRequest`, `LeaveRequestDocument`, `LeaveRequestDocumentVersion`, `EmployeeAbsenceComplianceCase`, `EmployeeAbsenceComplianceCaseEvent`, `EmployeeStatusChangeRequest`, `HrLegalCase`, `HrLegalCaseHearing`, `HrLegalCaseHearingFile`, `HrLegalCaseHistory`

### Housing

`Housing`, `HousingRoom`, `HousingSupervisorPeriod`, `HousingResidencePeriod`

### Client platforms, contracts, and rider accounts

`ClientContract`, `PlatformRiderAccount`, `PlatformAccountCredentialVersion`, `RiderClientAssignment`, `RiderAssignmentEvent`, `PlatformAccountRegistration`

### Documents and tags

`DocumentType`, `DocumentRequirement`, `EmployeeDocument`, `EmployeeDocumentVersion`, `Tag`, `EmployeeTag`, `HousingTag`, `ClientContractTag`, `PlatformRiderAccountTag`

### System operations

`Notification`, `ExportJob`, `SavedView`

### Fleet, vehicle records, compliance, and incidents

`VehicleManufacturer`, `VehicleModel`, `VehicleSupplier`, `Vehicle`, `VehicleIdentityCorrection`, `VehicleRegistrationTransition`, `VehicleRegistrationTransitionSnapshot`, `VehicleOperationalStatusPeriod`, `VehicleOdometerReading`, `VehicleDailyDistance`, `VehicleDailyDistanceImport`, `RiderVehicleAssignment`, `RealRider`, `SponsorVehicleLeaseAgreement`, `SponsorVehicleLeaseAgreementVehicle`, `VehiclePlatformAccountAssignment`, `VehiclePlatformAccountSwitch`, `RiderVehicleAssignmentEvent`, `RiderVehicleAssignmentPromissoryFile`, `FleetCommandReceipt`, `VehicleRegistration`, `VehicleInsurancePolicy`, `VehiclePeriodicInspection`, `VehicleOperationCard`, `VehicleAttachment`, `VehicleAttachmentVersion`, `RiderPromissoryFile`, `RiderPromissoryFileVersion`, `VehicleIssue`, `VehicleIssueEvidence`, `VehicleIssueEvent`, `VehicleAccident`, `VehicleAccidentCase`, `VehicleAccidentInstallment`, `VehicleAccidentEvent`, `VehicleAccidentAttachment`, `VehicleAccidentReportVersion`

### Fuel

`FuelCard`, `FuelCardRiderAssignment`, `FuelCardMonthlyUsage`, `FuelCardImport`

### Maintenance, inventory, oil, and workshop finance

`MaintenanceLocation`, `InventoryLocation`, `InventoryItem`, `MaintenanceSupplier`, `StockBalance`, `StockCostLayer`, `StockMovement`, `StockMovementLine`, `StockCostAllocation`, `PurchaseReceipt`, `PurchaseReceiptLine`, `PurchaseReceiptAttachment`, `OilBarrel`, `OilBarrelUsageAllocation`, `OilBarrelLoss`, `StockTransfer`, `StockTransferLine`, `SupplierReturn`, `SupplierReturnLine`, `RiderInventoryIssue`, `RiderInventoryIssueLine`, `InventorySupplyRequest`, `InventorySupplyRequestLine`, `MaintenanceWorkOrder`, `ExternalVehicleSnapshot`, `MaintenanceMaterialUsage`, `MaintenanceLaborEntry`, `MaintenancePlan`, `VehicleMaintenanceSchedule`, `OilChangeOperation`, `VehicleExpense`, `ExternalPartSaleLine`, `ExternalMaintenanceFinancialEntry`, `ExternalCustomerPayment`

### Telecom

`PhoneSimCard`, `RiderPhoneSimAssignment`, `PhoneSimResponsibilityChange`

### Explicit exclusions

- `AuditEntry` is not audited again; this prevents recursive audit records.
- `DatasetVersion` is an internal cache/version marker and is deliberately excluded from the audit timeline.
- Identity-context records—such as `ApplicationUser`, `ApplicationRole`, role grants, sessions, temporary credentials, and access scopes—are not currently emitted into this application audit feed. Their changes have their own identity persistence rules.

## Recommended UI behavior

- Timeline row: `occurredAtUtc`, `actor.displayNameAr` (fallback to `displayNameEn`, then `userName`, then `actorType`), `action`, and `record.displayLabel`/`record.displayCode`.
- Detail panel: show `changes` as a table: field, previous value, new value. Use `—` for a missing value.
- Creation entries normally have only `after` values; soft deletions normally have only `before` values; updates have both.
- Put `request.ipAddress`, `request.userAgent`, IDs, correlation ID, raw JSON, and source in a collapsible **Technical details** section.
- Never assume `record.displayLabel`, `displayCode`, `reason`, `actor`, or `request` is populated.
- Values are deliberately filtered server-side to exclude passwords, tokens, encrypted credentials, hashes, and similar secrets. Do not attempt to reconstruct those values in the UI.

## Errors

| Status | Meaning |
| --- | --- |
| `401 Unauthorized` | Missing, invalid, expired, or revoked access token. |
| `403 Forbidden` | Authenticated user does not have `audit.read`. |
| `404 Not Found` | The requested `eventId` does not exist. |
| `400 Bad Request` | Invalid query, including `pageSize` outside `1`–`200` or `toUtc` earlier than `fromUtc`. |

Errors use the API's Problem Details format. The frontend should display the returned `detail` where it is safe to do so and preserve the server-provided correlation ID for support.
