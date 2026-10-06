# Management permissions: frontend handoff

Updated 6 October 2026. This document is authoritative for the management-permission split; older feature handoffs may still mention `.manage`.

46 former management permissions now have independent Read, Create, Edit, and Delete keys. Edit is named `.update` in API payloads. 209 affected endpoints are listed below with every required permission. Paths, HTTP verbs, request/response bodies, row versions, and file formats are unchanged.

## Frontend changes

- Remove checks and submitted values ending in `.manage`. Use the exact keys below; do not infer permission from the user’s role name.
- Render four separate permission controls per family: Read (`.read`), Create (`.create`), Edit (`.update`), Delete (`.delete`). A grant for one action does not imply another.
- Fetch the catalog from `GET /api/users/permissions` (`permissions.read`). Fetch the signed-in user’s authorization from `GET /api/user-profile/me/authorization` after login, token refresh, and access changes. Use `effectivePermissionKeys` for visibility, and retain `roles`, `directPermissions`, scope flags, `scopes`, and validity windows for resource-specific checks.
- Multiple required permissions in an endpoint row are AND checks: all must be granted. Shared workflows still require their additional stock, work-order, approval, import, credential, or file permissions.
- Delete buttons include archive, remove, assignment return/close, support revocation, and cancellation operations shown in the table. Preserve existing soft-delete/history behavior and confirmations.
- Some families have no separate endpoint for an action yet. The permission is available for assignment, but does not add a new CRUD API. In particular, Jahez shared financial/history views continue to use `jahez.read`; do not replace it with a family read key unless the endpoint table says so.
- Keep the existing client scopes and sensitive/high-trust grant rules. Housing permissions remain module-level. A globally visible action can still be refused for a resource outside the user’s scope.
- On 403, refresh authorization and show the API Problem Details message. After the database authorization-version update, refresh tokens or ask the user to log in if the session can no longer refresh.

## Permission keys

| Previous key | Read | Create | Edit | Delete | Client scope | High trust |
|---|---|---|---|---|---|---|
| `roles.manage` | `roles.read` | `roles.create` | `roles.update` | `roles.delete` | No | Yes |
| `permissions.manage` | `permissions.read` | `permissions.create` | `permissions.update` | `permissions.delete` | No | Yes |
| `support_access.manage` | `support_access.read` | `support_access.create` | `support_access.update` | `support_access.delete` | No | Yes |
| `company_profile.manage` | `company_profile.read` | `company_profile.create` | `company_profile.update` | `company_profile.delete` | No | Yes |
| `operating_cities.manage` | `operating_cities.read` | `operating_cities.create` | `operating_cities.update` | `operating_cities.delete` | No | No |
| `tags.manage` | `tags.read` | `tags.create` | `tags.update` | `tags.delete` | No | No |
| `riders.manage` | `riders.read` | `riders.create` | `riders.update` | `riders.delete` | No | No |
| `sponsors.manage` | `sponsors.read` | `sponsors.create` | `sponsors.update` | `sponsors.delete` | No | No |
| `residency.manage` | `residency.read` | `residency.create` | `residency.update` | `residency.delete` | No | No |
| `licenses.manage` | `licenses.read` | `licenses.create` | `licenses.update` | `licenses.delete` | No | No |
| `rider_cards.manage` | `rider_cards.read` | `rider_cards.create` | `rider_cards.update` | `rider_cards.delete` | No | No |
| `health_cards.manage` | `health_cards.read` | `health_cards.create` | `health_cards.update` | `health_cards.delete` | No | No |
| `insurance.manage` | `insurance.read` | `insurance.create` | `insurance.update` | `insurance.delete` | No | No |
| `promissory_notes.manage` | `promissory_notes.read` | `promissory_notes.create` | `promissory_notes.update` | `promissory_notes.delete` | No | Yes |
| `documents.catalog.manage` | `documents.catalog.read` | `documents.catalog.create` | `documents.catalog.update` | `documents.catalog.delete` | No | No |
| `platform_accounts.manage` | `platform_accounts.read` | `platform_accounts.create` | `platform_accounts.update` | `platform_accounts.delete` | Yes | No |
| `platform_assignments.manage` | `platform_assignments.read` | `platform_assignments.create` | `platform_assignments.update` | `platform_assignments.delete` | Yes | No |
| `housing.manage` | `housing.read` | `housing.create` | `housing.update` | `housing.delete` | No | No |
| `phone_sims.manage` | `phone_sims.read` | `phone_sims.create` | `phone_sims.update` | `phone_sims.delete` | No | No |
| `notifications.manage` | `notifications.read` | `notifications.create` | `notifications.update` | `notifications.delete` | No | No |
| `fleet.vehicles.manage` | `fleet.vehicles.read` | `fleet.vehicles.create` | `fleet.vehicles.update` | `fleet.vehicles.delete` | No | No |
| `fleet.assignments.manage` | `fleet.assignments.read` | `fleet.assignments.create` | `fleet.assignments.update` | `fleet.assignments.delete` | No | No |
| `fleet.issues.manage` | `fleet.issues.read` | `fleet.issues.create` | `fleet.issues.update` | `fleet.issues.delete` | No | No |
| `fleet.compliance.manage` | `fleet.compliance.read` | `fleet.compliance.create` | `fleet.compliance.update` | `fleet.compliance.delete` | No | No |
| `fleet.corrections.manage` | `fleet.corrections.read` | `fleet.corrections.create` | `fleet.corrections.update` | `fleet.corrections.delete` | No | Yes |
| `fleet.registration_transitions.manage` | `fleet.registration_transitions.read` | `fleet.registration_transitions.create` | `fleet.registration_transitions.update` | `fleet.registration_transitions.delete` | No | Yes |
| `fleet.daily_distances.manage` | `fleet.daily_distances.read` | `fleet.daily_distances.create` | `fleet.daily_distances.update` | `fleet.daily_distances.delete` | No | No |
| `jahez.handovers.manage` | `jahez.handovers.read` | `jahez.handovers.create` | `jahez.handovers.update` | `jahez.handovers.delete` | Yes | No |
| `jahez.collections.manage` | `jahez.collections.read` | `jahez.collections.create` | `jahez.collections.update` | `jahez.collections.delete` | Yes | No |
| `jahez.earnings.manage` | `jahez.earnings.read` | `jahez.earnings.create` | `jahez.earnings.update` | `jahez.earnings.delete` | Yes | No |
| `jahez.imports.manage` | `jahez.imports.read` | `jahez.imports.create` | `jahez.imports.update` | `jahez.imports.delete` | Yes | No |
| `jahez.adjustments.manage` | `jahez.adjustments.read` | `jahez.adjustments.create` | `jahez.adjustments.update` | `jahez.adjustments.delete` | Yes | Yes |
| `fuel.manage` | `fuel.read` | `fuel.create` | `fuel.update` | `fuel.delete` | No | No |
| `maintenance.locations.manage` | `maintenance.locations.read` | `maintenance.locations.create` | `maintenance.locations.update` | `maintenance.locations.delete` | No | Yes |
| `maintenance.external_jobs.manage` | `maintenance.external_jobs.read` | `maintenance.external_jobs.create` | `maintenance.external_jobs.update` | `maintenance.external_jobs.delete` | No | No |
| `maintenance.part_sales.manage` | `maintenance.part_sales.read` | `maintenance.part_sales.create` | `maintenance.part_sales.update` | `maintenance.part_sales.delete` | No | No |
| `maintenance.customer_labor_charges.manage` | `maintenance.customer_labor_charges.read` | `maintenance.customer_labor_charges.create` | `maintenance.customer_labor_charges.update` | `maintenance.customer_labor_charges.delete` | No | No |
| `maintenance.mechanic_labor_payments.manage` | `maintenance.mechanic_labor_payments.read` | `maintenance.mechanic_labor_payments.create` | `maintenance.mechanic_labor_payments.update` | `maintenance.mechanic_labor_payments.delete` | No | No |
| `inventory.items.manage` | `inventory.items.read` | `inventory.items.create` | `inventory.items.update` | `inventory.items.delete` | No | No |
| `inventory.receipts.manage` | `inventory.receipts.read` | `inventory.receipts.create` | `inventory.receipts.update` | `inventory.receipts.delete` | No | No |
| `inventory.returns.manage` | `inventory.returns.read` | `inventory.returns.create` | `inventory.returns.update` | `inventory.returns.delete` | No | No |
| `leave_requests.manage` | `leave_requests.read` | `leave_requests.create` | `leave_requests.update` | `leave_requests.delete` | No | No |
| `absence_cases.manage` | `absence_cases.read` | `absence_cases.create` | `absence_cases.update` | `absence_cases.delete` | No | No |
| `employee_status_changes.manage` | `employee_status_changes.read` | `employee_status_changes.create` | `employee_status_changes.update` | `employee_status_changes.delete` | No | No |
| `legal_cases.manage` | `legal_cases.read` | `legal_cases.create` | `legal_cases.update` | `legal_cases.delete` | No | No |
| `hr_forms.templates.manage` | `hr_forms.templates.read` | `hr_forms.templates.create` | `hr_forms.templates.update` | `hr_forms.templates.delete` | No | Yes |

## Affected endpoints

All keys in a row must be granted. For conditional rows, the service chooses the key from the request and current database state.

### CompanyProfile

| Method | Endpoint | Required permissions |
|---|---|---|
| PUT | `/api/company-profile` | `company_profile.update` |

### FuelCards

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/fuel-cards` | `fuel.create` |
| PUT | `/api/fuel-cards/{id:guid}/sponsor` | `fuel.update` |
| PUT | `/api/fuel-cards/{id:guid}/city` | `fuel.update` |
| POST | `/api/fuel-cards/{id:guid}/assignments` | `fuel.update` |
| POST | `/api/fuel-cards/{id:guid}/stop-rider` | `fuel.delete` |

### Housing

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/housing` | `housing.create` |
| PUT | `/api/housing/{id:guid}` | `housing.update` |
| PATCH | `/api/housing/{id:guid}/archive` | `housing.delete` |
| POST | `/api/housing/{id:guid}/rooms` | `housing.create` |
| POST | `/api/housing/{id:guid}/floors` | `housing.create` |
| PUT | `/api/housing/{id:guid}/floors/{floorId:guid}` | `housing.update` |
| DELETE | `/api/housing/floors/{floorId:guid}` | `housing.delete` |
| POST | `/api/housing/floors/{floorId:guid}/equipment` | `housing.create` |
| PUT | `/api/housing/floors/{floorId:guid}/equipment/{equipmentId:guid}` | `housing.update` |
| DELETE | `/api/housing/equipment/{equipmentId:guid}` | `housing.delete` |
| POST | `/api/housing/{id:guid}/residents` | `housing.create` |
| POST | `/api/housing/residence-periods/{periodId:guid}/close` | `housing.delete` |
| POST | `/api/housing/{id:guid}/supervisors` | `housing.create` |
| POST | `/api/housing/supervisor-periods/{periodId:guid}/close` | `housing.delete` |

### HousingWarehouse

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/housing/{housingId:guid}/warehouse/items` | `housing.create` |
| PUT | `/api/housing/{housingId:guid}/warehouse/items/{itemId:guid}` | `housing.update` |
| PATCH | `/api/housing/{housingId:guid}/warehouse/items/{itemId:guid}/statuses/{status}/quantity` | `housing.update` |
| POST | `/api/housing/{housingId:guid}/warehouse/items/{itemId:guid}/status-transfers` | `housing.update` |
| POST | `/api/housing/{housingId:guid}/warehouse/items/{itemId:guid}/housing-transfers` | `housing.update` |
| DELETE | `/api/housing/{housingId:guid}/warehouse/items/{itemId:guid}` | `housing.delete` |

### HrCatalogs

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/hr-catalogs/global-cities` | `operating_cities.create` |
| PUT | `/api/hr-catalogs/global-cities/{id:guid}` | `operating_cities.update` |
| POST | `/api/hr-catalogs/residency-professions` | `residency.create` |
| PUT | `/api/hr-catalogs/residency-professions/{id:guid}` | `residency.update` |
| POST | `/api/hr-catalogs/driver-license-categories` | `licenses.create` |
| PUT | `/api/hr-catalogs/driver-license-categories/{id:guid}` | `licenses.update` |
| POST | `/api/hr-catalogs/document-types` | `documents.catalog.create` |
| PUT | `/api/hr-catalogs/document-types/{id:guid}` | `documents.catalog.update` |
| POST | `/api/hr-catalogs/document-requirements` | `documents.catalog.create` |
| PUT | `/api/hr-catalogs/document-requirements/{id:guid}` | `documents.catalog.update` |
| POST | `/api/hr-catalogs/operating-cities` | `operating_cities.create` |
| PUT | `/api/hr-catalogs/operating-cities/{id:guid}` | `operating_cities.update` |

### HrFormTemplates

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/hr-form-templates` | `hr_forms.templates.create` |
| PUT | `/api/hr-form-templates/{id:guid}` | `hr_forms.templates.update` |
| POST | `/api/hr-form-templates/{id:guid}/versions` | `hr_forms.templates.create` |
| POST | `/api/hr-form-templates/{id:guid}/versions/{versionId:guid}/publish` | `hr_forms.templates.update` |
| PATCH | `/api/hr-form-templates/{id:guid}/archive` | `hr_forms.templates.delete` |

### HrWorkflows

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/hr-workflows/leave-types` | `leave_requests.create` |
| PUT | `/api/hr-workflows/leave-types/{id:guid}` | `leave_requests.update` |
| POST | `/api/hr-workflows/leave-approval-workflows` | `leave_requests.create` |
| PUT | `/api/hr-workflows/leave-approval-workflows/{id:guid}` | `leave_requests.update` |
| POST | `/api/hr-workflows/leave-requests` | `leave_requests.create` |
| PUT | `/api/hr-workflows/leave-requests/{id:guid}` | `leave_requests.update` |
| POST | `/api/hr-workflows/leave-requests/{id:guid}/transitions` | `leave_requests.update` |
| POST | `/api/hr-workflows/leave-requests/{id:guid}/date-change-requests` | `leave_requests.update` |
| POST | `/api/hr-workflows/leave-requests/{id:guid}/cancellation-requests` | `leave_requests.delete` |
| POST | `/api/hr-workflows/absence-cases` | `absence_cases.create` |
| PUT | `/api/hr-workflows/absence-cases/{id:guid}` | `absence_cases.update` |
| POST | `/api/hr-workflows/absence-cases/{id:guid}/transitions` | absence_cases.delete when status is Cancelled; absence_cases.update for all other transitions. |
| POST | `/api/hr-workflows/employee-status-change-requests` | `employee_status_changes.create` |

### Insurance

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/insurance/companies` | `insurance.create` |
| PUT | `/api/insurance/companies/{id:guid}` | `insurance.update` |
| POST | `/api/insurance/companies/{companyId:guid}/plans` | `insurance.create` |
| PUT | `/api/insurance/companies/{companyId:guid}/plans/{id:guid}` | `insurance.update` |
| POST | `/api/insurance/employees/{employeeId:guid}/policies` | `insurance.create` |
| PUT | `/api/insurance/employees/{employeeId:guid}/policies/{id:guid}` | `insurance.update` |
| PATCH | `/api/insurance/companies/{id:guid}/archive` | `insurance.delete` |
| PATCH | `/api/insurance/plans/{id:guid}/archive` | `insurance.delete` |
| PATCH | `/api/insurance/policies/{id:guid}/archive` | `insurance.delete` |

### Jahez

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/jahez/handovers` | `jahez.handovers.create` |
| POST | `/api/jahez/legacy-adoptions` | `jahez.adjustments.create` |
| POST | `/api/jahez/handovers/{id:guid}/close` | `jahez.handovers.delete` |
| POST | `/api/jahez/earnings` | `jahez.earnings.create` |
| POST | `/api/jahez/settlements` | `jahez.collections.create` |
| POST | `/api/jahez/adjustments` | `jahez.adjustments.create` |
| POST | `/api/jahez/imports` | `jahez.imports.create` |
| GET | `/api/jahez/imports/{id:guid}` | `jahez.imports.read` |
| GET | `/api/jahez/imports` | `jahez.imports.read` |
| POST | `/api/jahez/imports/{id:guid}/commit` | `jahez.imports.update` |
| GET | `/api/jahez/import-files/{id:guid}` | `jahez.imports.read` |

### LeaveDocuments

| Method | Endpoint | Required permissions |
|---|---|---|
| PUT | `/api/hr-workflows/leave-requests/{leaveRequestId:guid}/documents/{documentId:guid}` | `leave_requests.update` |
| PATCH | `/api/hr-workflows/leave-requests/{leaveRequestId:guid}/documents/{documentId:guid}/archive` | `leave_requests.delete` |

### LegalCases

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/hr/legal-cases` | `legal_cases.create` |
| PUT | `/api/hr/legal-cases/{id:guid}` | `legal_cases.update` |
| DELETE | `/api/hr/legal-cases/{id:guid}` | `legal_cases.delete` |
| POST | `/api/hr/legal-cases/{caseId:guid}/hearings` | `legal_cases.create` |
| PUT | `/api/hr/legal-cases/{caseId:guid}/hearings/{hearingId:guid}` | `legal_cases.update` |
| DELETE | `/api/hr/legal-cases/{caseId:guid}/hearings/{hearingId:guid}` | `legal_cases.delete` |
| POST | `/api/hr/legal-cases/{caseId:guid}/hearings/{hearingId:guid}/files` | `legal_cases.create` |
| DELETE | `/api/hr/legal-cases/{caseId:guid}/hearings/{hearingId:guid}/files/{fileId:guid}` | `legal_cases.delete` |

### MaintenanceInventory

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/maintenance-inventory/items` | `inventory.items.create` |
| PUT | `/api/maintenance-inventory/items/{id:guid}` | `inventory.items.update` |
| GET | `/api/maintenance-inventory/suppliers` | `inventory.receipts.read` |
| POST | `/api/maintenance-inventory/suppliers` | `inventory.receipts.create` |
| PUT | `/api/maintenance-inventory/suppliers/{id:guid}` | `inventory.receipts.update` |
| POST | `/api/maintenance-inventory/receipts` | `inventory.receipts.create` |
| GET | `/api/maintenance-inventory/receipts` | `inventory.receipts.read` |
| GET | `/api/maintenance-inventory/receipts/{id:guid}` | `inventory.receipts.read` |
| GET | `/api/maintenance-inventory/receipts/{id:guid}/bill-file` | `inventory.receipts.read` |
| POST | `/api/maintenance-inventory/supplier-returns` | `inventory.returns.create` |

### MaintenanceLocations

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/maintenance-locations` | `maintenance.locations.create` |
| PUT | `/api/maintenance-locations/{id:guid}` | `maintenance.locations.update` |

### MaintenanceWorkOrders

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/maintenance-work-orders/external` | `maintenance.work_orders.create` + `maintenance.external_jobs.create` |
| POST | `/api/maintenance-work-orders/{id:guid}/part-sales` | `maintenance.work_orders.update` + `maintenance.part_sales.create` + `inventory.stock.move` |
| POST | `/api/maintenance-work-orders/{id:guid}/customer-labor-charges` | `maintenance.work_orders.update` + `maintenance.customer_labor_charges.create` |
| POST | `/api/maintenance-work-orders/{id:guid}/mechanic-labor-payments` | `maintenance.work_orders.update` + `maintenance.mechanic_labor_payments.create` |
| POST | `/api/maintenance-work-orders/{id:guid}/other-financial-entries` | `maintenance.work_orders.update` + `maintenance.external_jobs.create` |
| POST | `/api/maintenance-work-orders/{id:guid}/customer-payments` | `maintenance.work_orders.update` + `maintenance.external_jobs.create` |

### Notifications

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/notifications` | `notifications.create` |

### PhoneSims

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/phone-sims` | `phone_sims.create` |
| PUT | `/api/phone-sims/{id:guid}` | `phone_sims.update` |
| PATCH | `/api/phone-sims/{id:guid}/responsible-employee` | `phone_sims.update` |
| PATCH | `/api/phone-sims/{id:guid}/status` | `phone_sims.update` |
| PATCH | `/api/phone-sims/{id:guid}/archive` | `phone_sims.delete` |
| POST | `/api/phone-sims/{id:guid}/assignments` | `phone_sims.create` |
| POST | `/api/phone-sims/{id:guid}/assignments/{assignmentId:guid}/close` | `phone_sims.delete` |

### Places

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/places` | `phone_sims.create` |
| PUT | `/api/places/{id:guid}` | `phone_sims.update` |

### PlatformAccounts

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/platform-accounts` | `platform_accounts.create` |
| PUT | `/api/platform-accounts/{id:guid}` | `platform_accounts.update` |
| POST | `/api/platform-accounts/{id:guid}/assign` | `platform_assignments.create` |
| POST | `/api/platform-accounts/{id:guid}/release` | `platform_assignments.delete` |

### PlatformOperations

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/platform-operations/platforms` | `platform_accounts.create` |
| PUT | `/api/platform-operations/platforms/{id:guid}` | `platform_accounts.update` |
| POST | `/api/platform-operations/contracts` | `platform_accounts.create` |
| PUT | `/api/platform-operations/contracts/{id:guid}` | `platform_accounts.update` |
| POST | `/api/platform-operations/accounts` | `platform_accounts.create` |
| PUT | `/api/platform-operations/accounts/{id:guid}` | `platform_accounts.update` |
| POST | `/api/platform-operations/registrations` | `platform_accounts.create` |
| PUT | `/api/platform-operations/registrations/{id:guid}` | `platform_accounts.update` |
| POST | `/api/platform-operations/assignments` | `platform_assignments.create` |
| POST | `/api/platform-operations/assignments/{id:guid}/close` | `platform_assignments.delete` |
| PATCH | `/api/platform-operations/{resource}/{id:guid}/archive` | `platform_accounts.delete` |

### Platforms

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/platforms` | `platform_accounts.create` |
| PUT | `/api/platforms/{id:guid}` | `platform_accounts.update` |

### PromissoryNotes

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/promissory-notes/employee/{employeeId:guid}` | `promissory_notes.create` |
| PUT | `/api/promissory-notes/employee/{employeeId:guid}/{id:guid}` | `promissory_notes.update` |
| PATCH | `/api/promissory-notes/{id:guid}/archive` | `promissory_notes.delete` |

### ResidencyAndLicenses

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/compliance/employees/{employeeId:guid}/driver-licenses` | `licenses.create` |
| PUT | `/api/compliance/employees/{employeeId:guid}/driver-licenses/{id:guid}` | `licenses.update` |
| PATCH | `/api/compliance/driver-licenses/{id:guid}/archive` | `licenses.update` |

### RiderCards

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/riders/{riderProfileId:guid}/cards` | `rider_cards.create` |
| PUT | `/api/riders/{riderProfileId:guid}/cards/{id:guid}` | `rider_cards.update` |
| POST | `/api/riders/{riderProfileId:guid}/health-cards` | `health_cards.create` |
| PUT | `/api/riders/{riderProfileId:guid}/health-cards/{id:guid}` | `health_cards.update` |
| PATCH | `/api/riders/{riderProfileId:guid}/cards/{id:guid}/archive` | `rider_cards.delete` |
| PATCH | `/api/riders/{riderProfileId:guid}/health-cards/{id:guid}/archive` | `health_cards.delete` |

### Riders

| Method | Endpoint | Required permissions |
|---|---|---|
| PUT | `/api/riders/{riderProfileId:guid}` | `riders.update` |

### Rooms

| Method | Endpoint | Required permissions |
|---|---|---|
| PUT | `/api/rooms/{id:guid}` | `housing.update` |
| DELETE | `/api/rooms/{id:guid}` | `housing.delete` |
| POST | `/api/rooms/{id:guid}/occupants/employees` | `housing.create` |
| POST | `/api/rooms/{id:guid}/occupants/riders` | `housing.create` |
| POST | `/api/rooms/{id:guid}/occupants/iqama` | `housing.create` |
| POST | `/api/rooms/{id:guid}/equipment` | `housing.create` |
| PUT | `/api/rooms/{id:guid}/equipment/{equipmentId:guid}` | `housing.update` |
| POST | `/api/rooms/{id:guid}/occupants/external` | `housing.create` |
| PUT | `/api/rooms/occupants/external/{occupantId:guid}` | `housing.update` |
| DELETE | `/api/rooms/occupants/external/{occupantId:guid}` | `housing.delete` |
| POST | `/api/rooms/occupants/pending/{pendingId:guid}/resolve` | `housing.update` |
| DELETE | `/api/rooms/occupants/pending/{pendingId:guid}` | `housing.delete` |
| POST | `/api/rooms/occupants/{occupancyPeriodId:guid}/move` | `housing.update` |
| POST | `/api/rooms/occupants/{occupancyPeriodId:guid}/remove` | `housing.delete` |

### Sponsors

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/sponsors` | `sponsors.create` |
| PUT | `/api/sponsors/{id:guid}` | `sponsors.update` |
| PATCH | `/api/sponsors/{id:guid}/archive` | `sponsors.delete` |

### SupportAccess

| Method | Endpoint | Required permissions |
|---|---|---|
| GET | `/api/support-access` | `support_access.read` |
| POST | `/api/support-access` | `support_access.create` |
| POST | `/api/support-access/{id:guid}/resolve` | `support_access.update` |
| POST | `/api/support-access/{id:guid}/revoke` | `support_access.delete` |

### Tags

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/tags` | `tags.create` |
| PUT | `/api/tags/{id:guid}` | `tags.update` |
| PATCH | `/api/tags/{id:guid}/archive` | `tags.delete` |
| PUT | `/api/tags/assignments/{resource}/{resourceId:guid}` | `tags.update` |

### Users

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/users` | `users.create` + `roles.create` + `permissions.create` |
| POST | `/api/users/roles` | `roles.create` |
| PUT | `/api/users/roles/{roleId:guid}` | `roles.update` |
| PUT | `/api/users/roles/{roleId:guid}/permissions` | `roles.update` + `roles.create` + `roles.delete` |
| PATCH | `/api/users/roles/{roleId:guid}/archive` | `roles.delete` |
| PUT | `/api/users/{userId:guid}/roles` | `roles.update` + `roles.create` + `roles.delete` |
| PUT | `/api/users/{userId:guid}/permissions` | `permissions.update` + `permissions.create` + `permissions.delete` |

### VehicleAssignments

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/vehicle-assignments/{assignmentId:guid}/promissory-files` | `fleet.assignments.update` |
| POST | `/api/vehicle-assignments/take` | `fleet.assignments.create` |
| POST | `/api/vehicle-assignments/return` | `fleet.assignments.delete` |
| POST | `/api/vehicle-assignments/return-with-condition-report` | `fleet.assignments.delete` |
| POST | `/api/vehicle-assignments/switch` | `fleet.assignments.update` + `fleet.assignments.create` + `fleet.assignments.delete` |
| POST | `/api/vehicle-assignments/{assignmentId:guid}/renew-permission` | `fleet.assignments.update` |

### VehicleCatalogs

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/vehicle-catalogs/manufacturers` | `fleet.vehicles.create` |
| PUT | `/api/vehicle-catalogs/manufacturers/{id:guid}` | `fleet.vehicles.update` |
| POST | `/api/vehicle-catalogs/models` | `fleet.vehicles.create` |
| PUT | `/api/vehicle-catalogs/models/{id:guid}` | `fleet.vehicles.update` |

### VehicleCompliance

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/vehicles/{vehicleId:guid}/registrations` | `fleet.compliance.create` |
| POST | `/api/vehicles/{vehicleId:guid}/insurance-policies` | `fleet.compliance.create` |
| POST | `/api/vehicles/{vehicleId:guid}/inspections` | `fleet.compliance.create` |
| POST | `/api/vehicles/{vehicleId:guid}/operation-cards` | `fleet.compliance.create` |

### VehicleDailyDistances

| Method | Endpoint | Required permissions |
|---|---|---|
| PUT | `/api/vehicle-daily-distances/{vehicleId:guid}/{workDate}` | fleet.daily_distances.create when no manual reading exists (including GPS-only rows); fleet.daily_distances.update when a manual reading already exists. |

### VehicleIssues

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/vehicle-issues` | `fleet.issues.create` |
| POST | `/api/vehicle-issues/{id:guid}/{operation:regex(^(review&#124;close&#124;reject)$)}` | `fleet.issues.update` |
| POST | `/api/vehicle-issues/{id:guid}/resolve` | `fleet.issues.update` |

### VehiclePlatformAccountAssignments

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/vehicle-platform-account-assignments` | `fleet.assignments.create` |
| POST | `/api/vehicle-platform-account-assignments/{id:guid}/close` | `fleet.assignments.delete` |
| POST | `/api/vehicle-platform-account-assignments/{id:guid}/switch` | `fleet.assignments.update` + `fleet.assignments.create` + `fleet.assignments.delete` |
| POST | `/api/vehicle-platform-account-assignments/switches/{switchId:guid}/accept` | `fleet.assignments.update` + `fleet.assignments.create` + `fleet.assignments.delete` |
| POST | `/api/vehicle-platform-account-assignments/lease-agreements` | `fleet.assignments.create` |
| POST | `/api/vehicle-platform-account-assignments/lease-agreements/{agreementId:guid}/close` | `fleet.assignments.delete` |

### Vehicles

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/vehicles` | `fleet.vehicles.create` |
| PUT | `/api/vehicles/{id:guid}` | `fleet.vehicles.update` |
| POST | `/api/vehicles/{id:guid}/{statusAction:regex(^(stolen&#124;recover&#124;out-of-service&#124;restore&#124;decommission&#124;under-movement-responsibility)$)}` | fleet.vehicles.decommission for decommission; fleet.vehicles.update for all other statusAction values. |
| POST | `/api/vehicles/{id:guid}/odometer` | fleet.corrections.create when isCorrection=true; fleet.vehicles.create otherwise. |
| POST | `/api/vehicles/{id:guid}/identity-corrections` | `fleet.corrections.create` |
| POST | `/api/vehicles/{id:guid}/registration-transitions/private-to-public` | `fleet.registration_transitions.create` |

### VehicleSuppliers

| Method | Endpoint | Required permissions |
|---|---|---|
| POST | `/api/vehicle-suppliers` | `fleet.vehicles.create` |
| PUT | `/api/vehicle-suppliers/{id:guid}` | `fleet.vehicles.update` |
| PATCH | `/api/vehicle-suppliers/{id:guid}/archive` | `fleet.vehicles.delete` |

## Related read endpoints

These existing read permissions still apply to list/detail screens. These routes are included so the frontend can pair each action with its screen’s data permissions.

| Method | Endpoint | Required permissions |
|---|---|---|
| GET | `/api/company-profile` | `company_profile.read` |
| GET | `/api/fuel-cards` | `fuel.read` |
| GET | `/api/fuel-cards/{id:guid}` | `fuel.read` |
| GET | `/api/fuel-cards/{id:guid}/assignments` | `fuel.read` |
| GET | `/api/fuel-cards/monthly-usage` | `fuel.read` |
| GET | `/api/fuel-cards/period-usage` | `fuel.read` |
| GET | `/api/fuel-cards/unassigned-usage` | `fuel.read` |
| GET | `/api/fuel-cards/imports` | `fuel.read` |
| GET | `/api/housing` | `housing.read` |
| GET | `/api/housing/stay-report` | `housing.read` |
| GET | `/api/housing/{id:guid}` | `housing.read` |
| GET | `/api/housing/{id:guid}/rooms` | `housing.read` |
| GET | `/api/housing/{id:guid}/floors` | `housing.read` |
| GET | `/api/housing/{id:guid}/residents` | `housing.read` |
| GET | `/api/housing/{id:guid}/supervisors` | `housing.read` |
| GET | `/api/housing/{housingId:guid}/warehouse` | `housing.read` |
| GET | `/api/housing/{housingId:guid}/warehouse/items` | `housing.read` |
| GET | `/api/housing/{housingId:guid}/warehouse/items/{itemId:guid}` | `housing.read` |
| GET | `/api/hr-catalogs/global-cities` | `operating_cities.read` |
| GET | `/api/hr-catalogs/residency-professions` | `residency.read` |
| GET | `/api/hr-catalogs/driver-license-categories` | `licenses.read` |
| GET | `/api/hr-catalogs/operating-cities` | `operating_cities.read` |
| GET | `/api/hr-form-templates` | `hr_forms.templates.read` |
| GET | `/api/hr-form-templates/{id:guid}` | `hr_forms.templates.read` |
| GET | `/api/hr-form-templates/by-code/{code}` | `hr_forms.templates.read` |
| GET | `/api/hr-form-templates/{id:guid}/versions` | `hr_forms.templates.read` |
| GET | `/api/hr-workflows/leave-types` | `leave_requests.read` |
| GET | `/api/hr-workflows/leave-approval-workflows` | `leave_requests.read` |
| GET | `/api/hr-workflows/leave-requests` | `leave_requests.read` |
| GET | `/api/hr-workflows/leave-requests/{id:guid}/date-change-requests` | `leave_requests.read` |
| GET | `/api/hr-workflows/leave-requests/{id:guid}/cancellation-requests` | `leave_requests.read` |
| GET | `/api/hr-workflows/absence-cases` | `absence_cases.read` |
| GET | `/api/hr-workflows/employee-status-change-requests` | `employee_status_changes.read` |
| GET | `/api/insurance/companies` | `insurance.read` |
| GET | `/api/insurance/companies/{companyId:guid}/plans` | `insurance.read` |
| GET | `/api/insurance/policies` | `insurance.read` |
| GET | `/api/jahez/handovers` | `jahez.read` |
| GET | `/api/jahez/handovers/{id:guid}/balance` | `jahez.read` |
| GET | `/api/jahez/debts` | `jahez.read` |
| GET | `/api/jahez/ledger` | `jahez.read` |
| GET | `/api/jahez/handovers/{id:guid}/fee` | `jahez.read` |
| GET | `/api/jahez/handovers/{id:guid}/settlements` | `jahez.read` |
| GET | `/api/jahez/handovers/{id:guid}/earnings` | `jahez.read` |
| GET | `/api/jahez/handovers/{id:guid}/commission-policies` | `jahez.read` |
| GET | `/api/jahez/requests` | `jahez.read` |
| GET | `/api/jahez/requests/{id:guid}` | `jahez.read` |
| GET | `/api/jahez/dispatches` | `jahez.read` |
| GET | `/api/jahez/cashbox` | `jahez.cashbox.read` |
| GET | `/api/jahez/cashbox/handovers` | `jahez.cashbox.read` |
| GET | `/api/jahez/cashbox/entries` | `jahez.cashbox.read` |
| GET | `/api/hr-workflows/leave-requests/{leaveRequestId:guid}/documents` | `leave_requests.read` |
| GET | `/api/hr/legal-cases` | `legal_cases.read` |
| GET | `/api/hr/legal-cases/{id:guid}` | `legal_cases.read` |
| GET | `/api/hr/legal-cases/{id:guid}/history` | `legal_cases.read` |
| GET | `/api/maintenance-inventory/items` | `inventory.items.read` |
| GET | `/api/maintenance-locations` | `maintenance.locations.read` |
| GET | `/api/maintenance-work-orders/external` | `maintenance.work_orders.read` + `maintenance.external_jobs.read` |
| GET | `/api/notifications` | `notifications.read` |
| GET | `/api/notifications/unread-count` | `notifications.read` |
| POST | `/api/notifications/query` | `notifications.read` |
| POST | `/api/notifications/read-all` | `notifications.read` |
| POST | `/api/notifications/{id:guid}/state` | `notifications.read` |
| GET | `/api/phone-sims` | `phone_sims.read` |
| GET | `/api/phone-sims/{id:guid}` | `phone_sims.read` |
| GET | `/api/phone-sims/{id:guid}/receipt-form` | `phone_sims.read` |
| GET | `/api/phone-sims/{id:guid}/responsibility-history` | `phone_sims.read` |
| GET | `/api/phone-sims/{id:guid}/assignments` | `phone_sims.read` |
| GET | `/api/places` | `phone_sims.read` |
| GET | `/api/platform-accounts` | `platform_accounts.read` |
| GET | `/api/platform-accounts/{id:guid}` | `platform_accounts.read` |
| GET | `/api/platform-accounts/{id:guid}/assignment-history` | `platform_assignments.read` |
| GET | `/api/platform-operations/platforms` | `platform_accounts.read` |
| GET | `/api/platform-operations/contracts` | `platform_accounts.read` |
| GET | `/api/platform-operations/accounts` | `platform_accounts.read` |
| GET | `/api/platform-operations/registrations` | `platform_accounts.read` |
| GET | `/api/platform-operations/assignments` | `platform_assignments.read` |
| GET | `/api/platforms` | `platform_accounts.read` |
| GET | `/api/promissory-notes` | `promissory_notes.read` |
| GET | `/api/reports/fleet/vehicle-assignments` | `reports.read` + `fleet.assignments.read` + `fleet.vehicles.read` + `riders.read` |
| GET | `/api/reports/fleet/rider-assignments` | `reports.read` + `fleet.assignments.read` + `fleet.vehicles.read` + `riders.read` |
| GET | `/api/compliance/driver-licenses` | `licenses.read` |
| GET | `/api/riders/{riderProfileId:guid}/cards` | `rider_cards.read` |
| GET | `/api/riders/{riderProfileId:guid}/health-cards` | `health_cards.read` |
| GET | `/api/riders/{riderProfileId:guid}/platform-history` | `platform_assignments.read` |
| GET | `/api/riders` | `riders.read` |
| GET | `/api/riders/outside` | `riders.read` |
| GET | `/api/rooms/{id:guid}` | `housing.read` |
| GET | `/api/sponsors` | `sponsors.read` |
| GET | `/api/sponsors/{id:guid}` | `sponsors.read` |
| GET | `/api/tags` | `tags.read` |
| GET | `/api/tags/assignments/{resource}/{resourceId:guid}` | `tags.read` |
| GET | `/api/users/roles` | `roles.read` |
| GET | `/api/users/permissions` | `permissions.read` |
| GET | `/api/users/{userId:guid}/authorization` | `permissions.read` |
| GET | `/api/vehicle-assignments` | `fleet.assignments.read` |
| GET | `/api/vehicle-assignments/{assignmentId:guid}` | `fleet.assignments.read` |
| GET | `/api/riders/{riderProfileId:guid}/complete-history` | `fleet.assignments.read` + `fleet.issues.read` + `fleet.accidents.read` + `maintenance.work_orders.read` + `inventory.stock.read` |
| GET | `/api/vehicle-assignments` | `fleet.assignments.read` |
| GET | `/api/vehicle-assignments` | `fleet.assignments.read` |
| GET | `/api/vehicle-catalogs/manufacturers` | `fleet.vehicles.read` |
| GET | `/api/vehicle-catalogs/models` | `fleet.vehicles.read` |
| GET | `/api/vehicles/{vehicleId:guid}/{type:regex(^(registrations&#124;insurance-policies&#124;inspections&#124;operation-cards)$)}` | `fleet.compliance.read` |
| GET | `/api/vehicles/{vehicleId:guid}/due` | `fleet.compliance.read` |
| GET | `/api/vehicle-daily-distances/reports/vehicles/{vehicleId:guid}` | `fleet.daily_distances.read` |
| GET | `/api/vehicle-daily-distances/reports/missing-records` | `fleet.daily_distances.read` |
| GET | `/api/vehicle-daily-distances` | `fleet.daily_distances.read` |
| GET | `/api/vehicle-daily-distances/gps-imports` | `fleet.daily_distances.read` |
| GET | `/api/vehicle-issues` | `fleet.issues.read` |
| GET | `/api/vehicle-issues/{id:guid}/evidence` | `fleet.issues.read` |
| GET | `/api/vehicle-issues/{id:guid}/evidence/{evidenceId:guid}/download` | `fleet.issues.read` |
| GET | `/api/vehicle-platform-account-assignments` | `fleet.assignments.read` |
| GET | `/api/vehicle-platform-account-assignments/problems` | `fleet.assignments.read` |
| GET | `/api/vehicle-platform-account-assignments/{id:guid}` | `fleet.assignments.read` |
| GET | `/api/vehicle-platform-account-assignments/switches` | `fleet.assignments.read` |
| GET | `/api/vehicle-platform-account-assignments/switches/{switchId:guid}` | `fleet.assignments.read` |
| GET | `/api/vehicle-platform-account-assignments/lease-agreements` | `fleet.assignments.read` |
| GET | `/api/vehicle-platform-account-assignments/lease-agreements/eligible-vehicles` | `fleet.assignments.read` |
| GET | `/api/vehicle-platform-account-assignments/lease-agreements/{agreementId:guid}` | `fleet.assignments.read` |
| GET | `/api/vehicles/{id:guid}/complete-history` | `fleet.vehicles.read` + `fleet.assignments.read` + `fleet.issues.read` + `fleet.accidents.read` + `maintenance.work_orders.read` + `inventory.stock.read` |
| GET | `/api/vehicles` | `fleet.vehicles.read` |
| GET | `/api/vehicles/lookup` | `fleet.vehicles.read` |
| GET | `/api/vehicles/{id:guid}` | `fleet.vehicles.read` |
| GET | `/api/vehicles/{id:guid}/status-history` | `fleet.vehicles.read` |
| GET | `/api/vehicles/{id:guid}/odometer` | `fleet.vehicles.read` |
| GET | `/api/vehicles/{id:guid}/rider-timeline` | `fleet.assignments.read` |
| GET | `/api/vehicles/{id:guid}/readiness` | `fleet.vehicles.read` |
| GET | `/api/vehicles/{id:guid}/identity-corrections` | `fleet.vehicles.read` |
| GET | `/api/vehicles/{id:guid}/registration-transitions` | `fleet.vehicles.read` |
| GET | `/api/vehicle-suppliers` | `fleet.vehicles.read` |
| GET | `/api/vehicle-suppliers/{id:guid}` | `fleet.vehicles.read` |

## Authorization editing payloads

`PUT /api/users/{userId}/permissions` requires `permissions.create`, `permissions.update`, and `permissions.delete` because it replaces the full assignment set. Send the complete desired set, not only the changed permission. Removing an entry revokes it.

```json
{"assignments":[{"permissionKey":"fuel.create","effect":"Grant","startsAtUtc":null,"expiresAtUtc":null,"reason":"Create fuel cards","isAllHousingScope":false,"isAllClientScope":false,"includesFuturePlatformContracts":false,"scopes":[]}]}
```

`PUT /api/users/{userId}/roles` and `PUT /api/users/roles/{roleId}/permissions` require all three role action permissions. Role permission replacement keeps its existing `{ "permissionKeys": [...], "rowVersion": "..." }` body. Read permission is independently assigned; do not silently add it when saving one write permission.

## Database and rollout

Every active legacy management role grant and direct assignment is expanded to all four keys. Direct denies, expiry/start dates, reasons, client/housing flags, and exact scopes are copied; temporary support-access permission arrays are expanded and deduplicated. Existing explicit denies still override grants. Original assignment and seeded IDs remain on Create wherever possible.

Compatibility `.manage` definitions and assignments remain in the hosted database while the older API is running. The updated backend rejects `.manage`, hides compatibility entries from permission editing responses, and uses only the new action keys. Publish the updated backend before the frontend starts sending the new permissions. Backend publication is a separate deployment step.

Application and Identity migrations are applied together through the transaction script. Rollback requires restoring the authorization backup: independently edited permissions cannot safely be collapsed back into management access.

Machine-readable files: [endpoint map](management-permissions-endpoints.json) and [TypeScript constants and endpoint guards](management-permissions.ts). Database execution and verification results are recorded in [deployment notes](management-permissions-db-update.md).
