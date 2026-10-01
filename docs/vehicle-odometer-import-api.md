# Vehicle odometer import

Upload a `.xlsx` workbook as multipart form data with a `file` field (maximum 20 MB).

| Method | Route | Action |
| --- | --- | --- |
| POST | `/api/import/vehicles/odometer/validate` | Preview vehicle matches and errors without saving |
| POST | `/api/import/vehicles/odometer` | Validate again and update the matched vehicle records |

Both endpoints allow anonymous requests. No bearer token or fleet permission is required. The commit endpoint can replace a higher current reading with the value in the workbook.

The first worksheet needs these two columns. Arabic or English headers are accepted:

| Arabic header | English header | Value |
| --- | --- | --- |
| `الرقم التسلسلي` | `Serial Number` | Existing vehicle serial number |
| `الكيلومترات الحالية` | `Current KM` | Nonnegative whole number of kilometers |

The response includes `canUpdate`, `updated`, row previews showing current and imported readings, counts, and row issues. The commit endpoint saves nothing if any row has an invalid reading, an unknown serial number, or a duplicate serial number. Blank rows are ignored. Serial matching uses the same normalization as vehicle identity fields.

On success, the import updates `CurrentOdometer`, `TrackedDistanceKm`, and `LastOdometerAtUtc` on each changed vehicle. It writes no odometer history entry. The supplied reading replaces the current value, including when it is lower. An unchanged reading does not modify the vehicle.
