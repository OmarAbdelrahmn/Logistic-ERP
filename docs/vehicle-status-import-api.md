# Vehicle status import

Upload an `.xlsx` workbook as multipart form data with a `file` field (maximum 20 MB).

| Method | Route | Action | Authentication |
| --- | --- | --- | --- |
| POST | `/api/import/vehicles/statuses/validate` | Preview status matches and errors without saving | Anonymous allowed |
| POST | `/api/import/vehicles/statuses` | Validate again, then update every matched vehicle | Anonymous allowed |

The first worksheet must include `serial` and `status` columns. The optional `اعطال السياره` column is included in the status history reason. The headers `Serial Number` / `الرقم التسلسلي`, `الحالة` / `حالة المركبة`, and `Issue Note` are also accepted.

| Spreadsheet status | Vehicle status |
| --- | --- |
| `ready` | `Available` (1) |
| `صيانه` or `صيانة` | `OutOfService` (6) |
| `حادث` | `AccidentHold` (4) |
| `تالف` | `Decommissioned` (7) |
| `تحت مسؤلية الحركة` (also `تحت مسؤليه الحركة` or `تحت مسؤولية الحركة`) | `UnderMovementResponsibility` (8) |

The response includes `validateOnly`, `canUpdate`, `updated`, `worksheet`, `totalRows`, `validRows`, `matchedVehicles`, `changedVehicles`, `unchangedVehicles`, `rows`, and `issues`. Each row preview shows the serial number, vehicle ID, asset number, current and imported statuses, original sheet status, optional issue note, and `willChange`. Issues identify the source row and field.

Rows with unknown or duplicate serial numbers, unknown statuses, active rider assignments, conflicting open status periods, or an attempt to reactivate a decommissioned vehicle block the entire upload. A vehicle with an open blocking issue cannot be marked `Available`. Validation writes nothing. Import rechecks the sheet and saves all changes in one database operation; if any row is invalid, it saves none. Reimporting an unchanged status leaves its history untouched.

For a changed status, the import closes the open status period, creates a new administrative period effective at upload time, and updates `currentOperationalStatus`. For `Decommissioned`, it also sets the decommission date and reason. The spreadsheet has no effective date, so it does not reconstruct past status changes. The issue note is preserved as part of the new period reason; the import does not create a vehicle issue record.

When no user is authenticated, status history records the system import actor. Authenticated uploads record the current user.
