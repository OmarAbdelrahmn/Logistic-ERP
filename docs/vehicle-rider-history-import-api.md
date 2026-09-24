# Vehicle and rider authorization history import

Upload the September Tamm authorization workbook to create completed vehicle and rider history records. The import uses the five Arabic columns in `سجل سبتمبر.xlsx`: serial number, authorization number, authorized person's ID, authorization start date, and authorization end or cancellation date. Blank rows are ignored.

## Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/api/import/vehicle-rider-history/validate` | Preview matches and row issues without saving |
| POST | `/api/import/vehicle-rider-history` | Save valid historical rows |

Both endpoints allow anonymous access and accept `multipart/form-data` with a `file` field containing a non-empty `.xlsx` file up to 20 MB. The response includes `totalRows`, `validRows`, `createdAssignments`, `alreadyImported`, matched `rows`, and row-level `issues`. Importing valid rows is partial: invalid rows are reported and skipped. Each authorization number can be imported once; an exact repeat is reported as `AlreadyImported`, while a reused number with different details is an error.

Only existing vehicles matched by normalized serial number and employees matched by Iqama are used. If an employee has no rider profile, import creates one minimal rider profile for that employee, as the current assignment import does. Multiple history rows for the same employee reuse that profile. Unknown or ambiguous matches are reported as row errors. Validation previews the planned profile ID without saving it. The authorization end date must be before the current Saudi date, so the import cannot create an ongoing assignment. The stored `permissionStartsOn` and `permissionEndsOn` reproduce the sheet dates. `startedAtUtc` is the start date at 00:00 Saudi time and `endedAtUtc` is 00:00 on the next day, giving the end date an inclusive interpretation. Rows with the same start and end date cover that single day.

Imported assignments have `Completed` status and retain the sheet's authorization number. The sheet contains no odometer or vehicle condition readings, so the record marks condition as `Unknown` and uses the schema's required zero start odometer. The import does not update `Vehicle.CurrentAssignmentId`, vehicle operational status, operational status periods, existing active assignments, or rider availability. Existing historical periods may overlap because the source contains overlapping authorizations; they are retained as recorded.
