# PetroApp card number import

Upload an `.xlsx` workbook with one column headed `number` in cell A1. Put one card number per row, such as `BAWABA255`. Blank rows are ignored. The cards are created for PetroApp as internal card numbers, without rider assignments.

- `POST /api/fuel-cards/card-number-imports/validate` previews the upload without saving.
- `POST /api/fuel-cards/card-number-imports` creates the new cards.

Both endpoints require `fuel.import` and `multipart/form-data` with a `file` field and a `sponsorId` UUID for an existing sponsor. New cards are linked to that sponsor; existing PetroApp card numbers are counted and skipped. The file limit is 10 MB. Duplicate or invalid numbers in the sheet produce row issues and prevent the whole upload from saving.

Both endpoints also accept optional `operatingCityId` for new cards. Omit it to use Jeddah (`019c18d5-62e1-7000-8000-000000000003`). Supplied IDs must identify an existing, non-deleted operating city. Existing cards keep their current city. See the [city handoff](fuel-card-city-frontend-handoff.md).
