# Reports dashboards — frontend handoff

## Purpose

Build read-only dashboard pages from the Reports API. Every endpoint returns a direct JSON object (there is no `{ data: ... }` wrapper), uses camel-case property names, and requires the authenticated user to hold `reports.read`.

The reports intentionally contain aggregate operational data only. Do not expect employee names, personal identifiers, account usernames, or credentials in these responses.

## Routes

| Page | Request | Response type | Recommended use |
| --- | --- | --- | --- |
| System overview | `GET /api/reports/dashboard` | `SystemDashboardReportResponse` | Landing dashboard; summary cards from every module. |
| HR | `GET /api/reports/hr/dashboard` | `HrDashboardReportResponse` | HR dashboard and sponsor/platform charts. |
| People & compliance | `GET /api/reports/people-compliance/dashboard` | `PeopleComplianceDashboardReportResponse` | Compliance and HR-workflow dashboard. |
| Fleet | `GET /api/reports/fleet/dashboard` | `FleetDashboardReportResponse` | Fleet operations dashboard. |
| Operations | `GET /api/reports/operations/dashboard` | `OperationsDashboardReportResponse` | Platform, housing, telecom, and fuel operations dashboard. |
| Maintenance & inventory | `GET /api/reports/maintenance-inventory/dashboard` | `MaintenanceInventoryDashboardReportResponse` | Workshop and warehouse dashboard. |

Send the normal access token:

```http
Authorization: Bearer <access-token>
Accept: application/json
```

There are no route or query parameters and no pagination. The dashboard values are generated from the current database state.

## System overview

`GET /api/reports/dashboard` returns the summaries needed for a landing page in one request.

```ts
type SystemDashboardReport = {
  generatedAtUtc: string;
  hr: HrHeadcountSummary;
  peopleCompliance: PeopleComplianceDashboard;
  fleet: FleetDashboard;
  operations: OperationsDashboard;
  maintenanceInventory: MaintenanceInventoryDashboard;
};
```

Use `generatedAtUtc` as the “last updated” timestamp. Render it in the user’s locale/time zone; do not use it as a cache/version key.

## HR dashboard

`GET /api/reports/hr/dashboard`

```ts
type HrDashboardReport = {
  generatedAtUtc: string;
  headcount: HrHeadcountSummary;
  sponsors: SponsorHeadcount[];
  activePlatforms: PlatformCoverage[];
};

type HrHeadcountSummary = {
  totalPeople: number;
  totalEmployees: number;
  totalRiders: number;
  activePeople: number;
  activeEmployees: number;
  activeRiders: number;
  peopleWithoutSponsor: number;
  activeRidersWithoutAnyPlatformAccount: number;
  activeRiderPlatformCoverageGaps: number;
};

type SponsorHeadcount = {
  sponsorId: string;
  sponsorNameAr: string;
  sponsorNameEn: string | null;
  status: "Active" | "Disabled" | "Archived";
  totalPeople: number;
  employees: number;
  riders: number;
  activePeople: number;
  activeEmployees: number;
  activeRiders: number;
};

type PlatformCoverage = {
  platformId: string;
  platformCode: string;
  platformNameAr: string;
  platformNameEn: string;
  operationalAccountCount: number;
  assignedAccountCount: number;
  activeRidersWithAccount: number;
  activeRidersWithoutAccount: number;
};
```

Recommended HR UI:

- Summary cards: `totalPeople`, `totalEmployees`, `totalRiders`, `activeRiders`, `peopleWithoutSponsor`, and `activeRidersWithoutAnyPlatformAccount`.
- Sponsor table or stacked bar chart: use `sponsors`, displaying Arabic names by default and falling back to `sponsorNameEn` when needed.
- Platform coverage table: show operational/assigned account counts and rider coverage. Highlight `activeRidersWithoutAccount` when it is greater than zero.
- Do not treat `totalPeople` as `employees + riders` from a filtered table; use the returned fields as the source of truth.

### Coverage definitions

An “account” for rider coverage means a currently active `RiderClientAssignment` on an account whose account status is `Assigned`.

- `activeRidersWithAccount`: active riders currently assigned to an account at that specific active platform.
- `activeRidersWithoutAccount`: active riders without such an assignment at that platform.
- `activeRidersWithoutAnyPlatformAccount`: active riders without an assigned account on any platform.
- `activeRiderPlatformCoverageGaps`: total missing rider/platform pairs across all active platforms. A rider missing two platforms contributes `2`.

## People and compliance dashboard

`GET /api/reports/people-compliance/dashboard`

```ts
type PeopleComplianceDashboard = {
  payrollEmployees: number;
  activeEmployeeDocuments: number;
  expiredEmployeeDocuments: number;
  activeDriverLicenses: number;
  expiredDriverLicenses: number;
  activeMedicalInsurancePolicies: number;
  pendingLeaveRequests: number;
  activeLeaveRequests: number;
  openAbsenceComplianceCases: number;
};
```

Use warning styling for `expiredEmployeeDocuments`, `expiredDriverLicenses`, `pendingLeaveRequests`, and `openAbsenceComplianceCases` when non-zero.

## Fleet dashboard

`GET /api/reports/fleet/dashboard`

```ts
type FleetDashboard = {
  totalVehicles: number;
  availableVehicles: number;
  assignedVehicles: number;
  heldVehicles: number;
  decommissionedVehicles: number;
  activeRiderVehicleAssignments: number;
  openVehicleIssues: number;
  unclosedAccidents: number;
};
```

`heldVehicles` is the total in `ProblemHold`, `AccidentHold`, `Stolen`, or `OutOfService`. It does not include decommissioned vehicles. Use alert styling for open issues and unclosed accidents.

## Operations dashboard

`GET /api/reports/operations/dashboard`

```ts
type OperationsDashboard = {
  activePlatforms: number;
  operationalPlatformAccounts: number;
  assignedPlatformAccounts: number;
  activeHousingLocations: number;
  totalActiveHousingCapacity: number;
  currentHousingResidents: number;
  totalPhoneSims: number;
  availablePhoneSims: number;
  assignedPhoneSims: number;
  phoneSimsNeedingAttention: number;
  totalFuelCards: number;
  assignedFuelCards: number;
};
```

`operationalPlatformAccounts` includes only `Available` and `Assigned` accounts. `phoneSimsNeedingAttention` includes `Suspended` and `Lost` SIMs.

## Maintenance and inventory dashboard

`GET /api/reports/maintenance-inventory/dashboard`

```ts
type MaintenanceInventoryDashboard = {
  activeMaintenanceLocations: number;
  activeInventoryItems: number;
  stockBalanceRecords: number;
  lowStockItems: number;
  inventoryValue: number;
  openWorkOrders: number;
  inProgressWorkOrders: number;
  pendingSupplyRequests: number;
};
```

Format `inventoryValue` as Saudi Riyals using the application’s existing currency helper. It is the current inventory valuation calculated from `quantityOnHand × reportingAverageUnitCost`. It may be decimal, so never cast it to an integer.

## Frontend behavior

- Fetch only the focused endpoint needed by a page. Fetch `/api/reports/dashboard` for the system landing page rather than making all focused requests in parallel.
- These endpoints are read-only. Do not send an idempotency header, `rowVersion`, or request body.
- Disable or hide dashboard navigation when the user does not have `reports.read`.
- Display skeleton cards/tables while loading. A manual Refresh action is appropriate; do not poll aggressively.
- A successful request is `200 OK`. Standard API problem responses apply on failure, especially `401` for expired/invalid authentication and `403` for missing `reports.read`.
- Treat every returned count as an integer except `inventoryValue`, which is decimal. Empty datasets return zero values and empty `sponsors`/`activePlatforms` arrays—not `null`.

## Example HR response

```json
{
  "generatedAtUtc": "2026-09-12T08:30:00Z",
  "headcount": {
    "totalPeople": 250,
    "totalEmployees": 40,
    "totalRiders": 210,
    "activePeople": 225,
    "activeEmployees": 36,
    "activeRiders": 189,
    "peopleWithoutSponsor": 5,
    "activeRidersWithoutAnyPlatformAccount": 23,
    "activeRiderPlatformCoverageGaps": 71
  },
  "sponsors": [
    {
      "sponsorId": "00000000-0000-0000-0000-000000000001",
      "sponsorNameAr": "شركة مثال",
      "sponsorNameEn": "Example Company",
      "status": "Active",
      "totalPeople": 100,
      "employees": 15,
      "riders": 85,
      "activePeople": 94,
      "activeEmployees": 14,
      "activeRiders": 80
    }
  ],
  "activePlatforms": [
    {
      "platformId": "00000000-0000-0000-0000-000000000010",
      "platformCode": "KEETA",
      "platformNameAr": "كيتا",
      "platformNameEn": "Keeta",
      "operationalAccountCount": 150,
      "assignedAccountCount": 120,
      "activeRidersWithAccount": 120,
      "activeRidersWithoutAccount": 69
    }
  ]
}
```
