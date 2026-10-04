# Fuel card bulk import

The Import module has two endpoints for uploading fuel cards from one Excel sheet:

| Action | Endpoint | Access |
|---|---|---|
| Validate without saving | `POST /api/import/fuel-cards/validate` | Anonymous |
| Insert all valid cards | `POST /api/import/fuel-cards` | Anonymous |

Send `multipart/form-data` with one `file` field containing a nonempty `.xlsx` workbook up to 20 MiB. No `sponsorId` form field is needed. The first worksheet must have exactly three column headers, in any order:

An optional `operatingCityId` form field selects the city for every new card in the upload; omit it to use Jeddah (`019c18d5-62e1-7000-8000-000000000003`). The city must exist and not be soft-deleted. Existing cards keep their stored city. Each valid preview row includes `operatingCityId`, showing the resulting city for that row. Send the same city during validation and import. The spreadsheet still has only the three headers below; see the [city handoff](fuel-card-city-frontend-handoff.md).

| number | sponsor 70 number | company name |
|---|---|---|
| BW203 | 7038745530 | بترو اب |
| BW203 | 7015658094 | سياره كار |

`70 number`, `رقم الكفيل 70`, and `رقم 70` are also accepted for the sponsor column; `اسم الشركة` is accepted for the company column. Header matching ignores case and spaces. `company name` means the **fuel company**, not the sponsor's registered name. `بترو اب` maps to `PetroApp`; `سياره كار` or `سيارة كار` maps to the existing `SayaraApp` provider code. The sponsor is found by matching the 70 number to `Sponsor.EmployerIdentityNumber`. Card numbers use the internal-number format and are unique per fuel provider. Blank rows are ignored.

The JSON response has `validateOnly`, `canImport`, `imported`, `totalRows`, `newCards`, `existingCards`, `rows`, and `issues`. Each valid preview row includes its Excel `rowNumber`, `cardNumber`, `sponsor70Number`, resolved `sponsorId`, `sponsorNameAr`, `companyName`, `provider`, and `willCreateCard`. Each issue includes an Excel row number, card number, and Arabic message.

Validation never writes data. The insert endpoint writes the batch only when every row is valid (`canImport: true`). An unknown 70 number, unsupported fuel company, duplicate provider/card number in the file, or an existing card linked to another sponsor produces a row issue and prevents the entire batch from saving. An existing card with the same provider, number, and sponsor is counted and skipped, making a repeated upload safe.

The separate `/api/fuel-cards/card-number-imports` endpoints keep their original one-column PetroApp format and still require one `sponsorId` form field for the whole file.
