# Spare part catalog import

Both actions are in `ImportController`, accept a multipart form upload named `file`, and allow anonymous access. The file must be a nonempty `.xlsx` workbook no larger than 10 MB.

| Method | Route | Action |
| --- | --- | --- |
| POST | `/api/maintenance-inventory/items/import/validate` | Check the workbook, existing catalog matches, and row issues without saving |
| POST | `/api/maintenance-inventory/items/import` | Check again and add the missing catalog items if the entire workbook is valid |

The first worksheet needs two columns:

| Header in the supplied workbook | Meaning |
| --- | --- |
| `اسم الصنف / القطعة` | Arabic spare part name |
| `التصنيف` | `سيارة` for car or `دباب` for motorcycle |

The endpoint creates active `SparePart` catalog items. Each item gets the supplied name as `NameAr`; `NameEn` receives the same text because the workbook has no English translation. The system generates a stable SKU and uses `Piece` for the required base and purchase units. The compatibility mask contains only the workbook's car or motorcycle type. No quantity, stock balance, price, supplier, barcode, or other catalog details are imported.

Uploading the same name and vehicle type again skips that catalog item. A name already present with a different compatibility setting remains unchanged, and the import creates a separate item limited to the workbook's type. Blank rows are ignored. Duplicate name/type pairs, unsupported classifications, missing names, and names over 200 characters are reported by row; any such error prevents all writes from that workbook.

The response includes `validateOnly`, `canImport`, `imported`, `totalRows`, `wouldCreateItems`, `createdItems`, `alreadyExistingItems`, row actions, and row issues. Validation reports `WouldCreate` rows and `createdItems: 0`; the commit route reports `Created` rows after saving. If any row has an issue, both routes report `canImport: false` and save nothing. A malformed workbook or missing required columns returns a validation error. Anonymous imports have no user ID recorded as their creator.
