# Jeddah housing update

Applied to the configured ERP database on 2026-10-05 from `بيانات_السكن_الموحدة_ERP_جدة.xlsx`, sheet `بيانات الرفع لنظام ERP`.

Source SHA-256: `63a9d01296e68f85709a5892496a8ab0177e99b3911b526e32a09962627ce6b3`.

| Housing code | Housing | Rooms | Beds | Residents | Vacancies |
| --- | --- | ---: | ---: | ---: | ---: |
| safa | سكن الصفا | 12 | 84 | 56 | 28 |
| SAMER | سكن السامر فيلا الاجواد | 9 | 85 | 53 | 32 |
| MECANIC | سكن السامر ميكانيكي (سكن جديد) | 6 | 46 | 30 | 16 |
| Total | | 27 | 215 | 139 | 76 |

The three existing housing IDs and all 26 existing room IDs were retained. Housing codes, Arabic/English names, districts, room names, capacities, and floor assignments now match the upload sheet. The existing three floor IDs were renamed to their corresponding workbook floors; five floors and Mechanic room 6 were added. The resulting eight floor records preserve apartment/workshop labels from the upload sheet.

124 residents matched existing employees by exact iqama. The five existing assignments in Samer room 1 were retained unchanged, and 119 missing assignments were added with effective date 2026-10-05. Existing residence history was preserved. No employees, employment classifications, platform assignments, or equipment were changed.

All 139 listed residents are recorded in their specified rooms. Vacancy rows create no resident records. Following the user's subsequent instruction, all 15 unmatched residents are now external renters in `HousingExternalOccupants`: eight in Safa, two in Samer, and five in Mechanic. The ten existing external occupants were preserved, and the five pending iqama records were soft-deleted and replaced with external occupants in the same rooms. Their iqamas remain in the archived pending records and conversion audit. Jeddah has no active pending occupants. No identities were inferred from short or similar names. The original import audit retains the original resident name, classification, work, notes, source row, and assigned room.

## External renters

Every row below is now an external renter and counts toward occupancy in the listed room. The historical workbook iqamas are shown for reference. There are no pending employee links for these residents.

| Upload row | Housing | Floor | Room | Iqama | Name | Recorded as |
| ---: | --- | --- | --- | --- | --- | --- |
| 5 | safa | الدور الأول | غرفة 1 | 2619453430 | هارون الرشيد اشرف | External renter |
| 7 | safa | الدور الأول | غرفة 1 | 2494774173 | قمر ال حسن | External renter |
| 16 | safa | الدور الأول | غرفة 2 | — | محمد حسنين | External renter |
| 17 | safa | الدور الأول | غرفة 2 | — | محمد دانيال | External renter |
| 55 | safa | الدور الثاني | غرفة 7 | 2630354393 | اسلام سمير | External renter |
| 56 | safa | الدور الثاني | غرفة 7 | — | ابراهيم زعلوق | External renter |
| 60 | safa | الدور الثالث | غرفة 9 | — | معوض | External renter |
| 71 | safa | الدور الثالث | غرفة 10 | 2643360973 | باسم وجيه | External renter |
| 108 | SAMER | الدور الأول - شقة 1 | غرفة 3 | — | مصطفى عارف | External renter |
| 119 | SAMER | الدور الثاني - شقة 2 | غرفة 5 | — | التماس | External renter |
| 187 | MECANIC | الدور الأرضي | غرفة 2 | 2553353455 | عاطف علي عبدالستار | External renter |
| 205 | MECANIC | الدور الأرضي | غرفة 5 | — | monirujjaman | External renter |
| 213 | MECANIC | الدور الأرضي - الورشة | غرفة 6 | — | محمد صبري | External renter |
| 214 | MECANIC | الدور الأرضي - الورشة | غرفة 6 | — | عبدالرحمن السمكري | External renter |
| 215 | MECANIC | الدور الأرضي - الورشة | غرفة 6 | — | نادر الميكانيكي | External renter |

## Validation and execution

The transaction was first executed with rollback. Its housing, room, capacity, occupancy, and per-person assignment checks passed, and the original database state was confirmed unchanged after rollback. The committed records were then independently checked against all 27 rooms and all 139 residents. All 60 pre-existing active residence periods, other housing records, employee records, and existing external/pending residents were preserved. A repeat trial reported zero inserts or updates.

The SQL runs atomically and refuses conflicting room assignments, extra floors/rooms, or existing residents absent from the workbook. It appends housing audit entries containing before/after values and the workbook hash when changes occur. Pre-import snapshots and execution results are retained under the ignored `artifacts/jeddah-housing-20261005` directory.

The original workbook runner below is now superseded and deliberately refuses execution after the external-renter override, preventing it from restoring pending classifications:

```powershell
.\database\scripts\run-jeddah-housing-import-20261005.ps1
.\database\scripts\run-jeddah-housing-import-20261005.ps1 -Commit
```

The runner uses the existing configured database connection without displaying credentials. The checked-in SQL contains the reviewed housing, floor, and room IDs for this database. Future changes in the workbook or housing records require a new reconciliation.

The override is recorded in `database/scripts/classify-jeddah-external-renters-20261005.sql`. It was validated with rollback, committed, and independently checked: all 15 are external, all housing/room records and employee assignments are unchanged, occupancy remains 139, and repeating the conversion creates no duplicates. The conversion appends an audit event and preserves the archived pending records.

