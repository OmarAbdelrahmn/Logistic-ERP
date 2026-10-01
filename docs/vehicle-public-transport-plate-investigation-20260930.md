# Public transport conversion: plate investigation

Verified against the configured hosted database `db67927` on 2026-09-30. Times below use Saudi time (UTC+3).

## Finding

The conversion stored the old full Arabic and English plate values as the new full plate values. The separate plate components contain different values, and the English letters and digits appear swapped. The current vehicle matches that saved conversion. There is no later identity correction recorded for this vehicle.

The screenshot's heading `1987 أ ع س` comes from a vehicle-file event's original filename, `1987 أ ع س.pdf`. The plate badge displays the vehicle's full plate field, which remains `س ب ن 5201`. Those two labels have different sources.

## Record identifiers

| Record | Value |
| --- | --- |
| Vehicle serial | `821504220` |
| Vehicle ID | `01a0aed7-0d9b-7ff4-af2e-fc74b20f20f9` |
| Asset number | `VEH-FC74B20F20F9` |
| Conversion ID | `01a0f2f7-70f4-7819-a844-d75c0af69ed9` |
| Actor user ID | `b1de8de7-7ad4-4f08-f387-08df1173e0a7` |
| Conversion effective time | 2026-09-30 18:36:00 |
| Conversion saved time | 2026-09-30 18:38:17.752 |
| Conversion reason | تحويل المركبة إلى النقل العام |
| Registration change | `Private` (1) → `PublicTransport` (5) |
| Current assignment | None |

## Plate values traced

| Field | Before conversion | Saved conversion's new value | Current vehicle |
| --- | --- | --- | --- |
| Full Arabic plate | `س ب ن 5201` | `س ب ن 5201` | `س ب ن 5201` |
| Full English plate | `S B N 5201` | `S B N 5201` | `S B N 5201` |
| Arabic letters | `س ب ن` | `أ ع س` | `أ ع س` |
| English letters | `S B N` | `1987` | `1987` |
| Digits | `5201` | `S E A` | `S E A` |

The conversion snapshot independently preserves these same before/after values. The vehicle audit at 18:38:17.752 records changes to the three split fields and registration type. It does not record a change to either full plate field because the submitted conversion's full values were unchanged.

This establishes a mismatch in the saved field values. The original HTTP request body was not retained in the inspected audit records, and frontend source is not present in this repository, so the exact frontend assignment that produced the mismatch still needs inspection.

## Files and timeline

| Saudi time | Event |
| --- | --- |
| 2026-09-17 13:08:44.607 | Vehicle created with full plates `س ب ن 5201` / `S B N 5201`, letters `س ب ن` / `S B N`, digits `5201`, registration type 1. |
| 2026-09-19 13:37:34.378 | Istimara version 1 uploaded: `س ب ن 5201.jpg`, 128,577 bytes. |
| 2026-09-30 18:38:17.661 | Istimara version 2 uploaded during conversion: `WhatsApp Image 2026-09-17 at 4.04.26 PM.jpeg`, 114,744 bytes. This is the current Istimara. |
| 2026-09-30 18:38:17.676 | Operation-card version 1 uploaded during conversion: `1987 أ ع س.pdf`, 89,176 bytes. |
| 2026-09-30 18:38:17.752 | Conversion, snapshot, and vehicle changes saved. |
| 2026-09-30 18:38:51.160 | Operation-card version 2 uploaded with the same filename and size. This is the current operation-card file. |

The conversion references the operation-card version uploaded during conversion, while the vehicle's current operation-card attachment points to the later version. Historical versions remain available. Document contents were not inspected; the trace above uses metadata and database records.

## Frontend mapping to correct

Endpoint: `POST /api/vehicles/{id}/registration-transitions/private-to-public`, using `multipart/form-data`.

Build all five fields from the edited new-plate form values:

| Request field | Expected value for the reported new plate |
| --- | --- |
| `plateNumberAr` | Complete new Arabic plate, for example `أ ع س 1987` |
| `plateNumberEn` | Complete new English plate; confirm the exact letter order on the document |
| `plateLettersAr` | `أ ع س` |
| `plateLettersEn` | Actual English letters; `S E A` is stored in the digits field and needs confirmation against the document |
| `plateDigits` | `1987` |

The full plate fields must reflect the edited new plate. The backend copies `plateNumberAr` and `plateNumberEn` directly to the vehicle's current full plate fields and separately copies the three component fields. It does not rebuild the full plates from the components. Current validation checks presence, lengths, and duplicate full plates, but does not check consistency between full plates, letters, and digits.

After a successful conversion, refresh vehicle detail, inventory, and lookup data. `GET /api/vehicles/{id}` exposes the current full plates as `summary.plateNumberAr` and `summary.plateNumberEn`. The conversion response is a history record; it is not a complete updated vehicle response.

The intended English full plate must be confirmed before correcting the hosted vehicle. This investigation made no changes to this vehicle or its immutable conversion history.

## Source locations

- Conversion request binding: `src/LogisticsERP.Api/Controllers/VehiclesController.cs`, `TransitionToPublic`.
- Vehicle plate assignment: `src/LogisticsERP.Infrastructure/Fleet/FleetService.cs`, `TransitionToPublicTransportAsync`.
- Full plate response mapping: `FleetService.BuildSummariesAsync`.
- File event heading: `src/LogisticsERP.Infrastructure/Fleet/FleetCompleteHistoryService.cs`, `vehicle_file` events use `OriginalFileName` as their summary.
