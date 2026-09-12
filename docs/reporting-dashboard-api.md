# Reporting dashboard API

All endpoints are read-only and require the `reports.read` permission. They return aggregate operational data only; employee names, account usernames, credentials, and other sensitive values are deliberately excluded.

| Endpoint | Dashboard content |
| --- | --- |
| `GET /api/reports/dashboard` | Whole-system overview: HR, people/compliance, fleet, operations, and maintenance/inventory. |
| `GET /api/reports/hr/dashboard` | Detailed headcount, sponsor headcount, active-platform account coverage, and missing-account gaps. |
| `GET /api/reports/people-compliance/dashboard` | Payroll, employee documents, licences, medical insurance, leave requests, and absence-compliance cases. |
| `GET /api/reports/fleet/dashboard` | Vehicle availability, assignments, holds, open issues, and unclosed accidents. |
| `GET /api/reports/operations/dashboard` | Platforms, platform accounts, housing capacity/residents, phone SIMs, and fuel-card assignment. |
| `GET /api/reports/maintenance-inventory/dashboard` | Active locations/items, stock records and value, low stock, work orders, and pending supply requests. |

## HR platform coverage definitions

`activeRidersWithAccount` is the number of active rider profiles with a current active assignment to an assigned account at that platform. `activeRidersWithoutAccount` is every active rider who does not have that current assignment. `activeRiderPlatformCoverageGaps` is the total of those gaps across all active platforms.

The report service uses server-side aggregate queries with `AsNoTracking`; it does not load entity lists into memory. The `AddHrDashboardReportingIndexes` migration adds indexes that support the active-rider and sponsor breakdowns. Apply that migration before relying on the reports in production.
