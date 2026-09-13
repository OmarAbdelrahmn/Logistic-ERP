# Employee and rider Excel import API

This API validates and imports employee and rider records from an `.xlsx` workbook. The uploaded file is processed in memory and is not retained.

## Endpoints

### Validate without writing

`POST /api/import/employees-riders/validate`

- Content type: `multipart/form-data`
- Form field: `file`
- Access: anonymous
- File rules: non-empty `.xlsx`, maximum 20 MB, maximum 5,000 data rows
- This endpoint performs no database writes.

### Import

`POST /api/import/employees-riders`

- Content type: `multipart/form-data`
- Form field: `file`
- Access: anonymous
- The import repeats validation on the server.
- If any row has an `Error`, the entire import is rolled back and `imported` is `false`.
- `Warning` issues do not block the import. Unsupported values are reported and ignored.

The recommended frontend flow is to call the validation endpoint, show the issues and planned counts, and enable the import action only when `canImport` is `true`.

## Employee and rider phone-number update

Two anonymous endpoints accept a simple two-column `.xlsx` workbook and match every row to the shared employee/rider record by Iqama:

- `POST /api/import/employees-riders/phone-numbers/validate` checks the entire workbook without writing.
- `POST /api/import/employees-riders/phone-numbers` repeats the checks and updates `Employee.PrimaryPhone` atomically.

Both endpoints use `multipart/form-data` with a `file` field. Column A must contain the 10-digit Iqama number and column B must contain the phone number. A recognized Arabic or English header row is optional. Arabic and Persian digits are supported, and valid phone numbers are stored in canonical E.164 form (for example, `0555 123 456` becomes `+966555123456`).

An invalid row, duplicate Iqama, or Iqama that does not match a current employee/rider blocks the whole update. The response reports `canUpdate`, `updated`, matched employee/rider counts, changed and unchanged counts, and row-level issues.

## Required columns

| Excel column | System field |
|---|---|
| `رقم الاقامة` | `Employee.IqamaNo`; exactly 10 digits and unique within the workbook |
| `الاسم` | `Employee.FullNameAr`; required, maximum 200 characters |

Arabic and Persian digits are normalized. Header matching also tolerates common Arabic spelling differences such as `الإقامة`/`الاقامة`, `المسمى`/`المسمي`, and `تاريخ التعيين`/`تاريخ التعين`.

## Supported optional columns

| Excel column | Import behavior |
|---|---|
| `الجنس` | Maps `ذكر`/`Male`, `أنثى`/`Female`, or `آخر`/`Other`. |
| `الجنسية` | Sets nationality. |
| `تاريخ الميلاد` | Sets birth date. Invalid dates are errors. |
| `تاريخ التعيين` | Sets hire date. Invalid dates are errors. |
| `المهنة بالإقامة` | Sets residency profession. `المهنة` is used as a fallback. |
| `المسمى الوظيفي` | Sets `WorkingForMeAs`; `العمل الفعلي` is the fallback. |
| `العمل الفعلي` | Selects employee/rider role and maps the operational work type. `اداري` creates an administrative employee; other values create a rider profile. `دباب`, `سيارة`, and `اداري` map to the seeded work types. |
| `حالة الكفالة` | `خارج الكفالة` maps a rider to `OutsideRider`; other values default to `SponsoredInternal`. Administrative employees always use `SponsoredInternal`. |
| `رقم صاحب العمل` or `هوية صاحب العمل` | Resolves the configured sponsor. An active sponsored row without a matching sponsor is imported as `Onboarding` with a warning. |
| `حالة الموظف` | Maps `Active`, `Vacation`, `Archived`, `Suspended`, `Terminated`, `Fleeing`, `Accident`, `Sick`, `Draft`, and `Onboarding`, including supported Arabic equivalents. Missing or unknown values use the safe default and unknown values produce a warning. |
| `الفرع / المدينة` or `الفرع` | Resolves an operating city by Arabic or English name. |
| `تاريخ اصدار الاقامة` | Sets residency-document issue metadata. |
| `تاريخ انتهاء الاقامة` | Sets residency-document expiry metadata. |
| `نوع الرخصة` | Creates or updates one current driver-license row per configured category. |
| `تاريخ اصدار الرخصة` | Optional issue date applied to the imported license categories. |
| `تاريخ انتهاء الرخصة` | Optional expiry date applied to the imported license categories. |
| Platform ID columns such as `ايدي كيتا` | Creates available platform accounts when the platform and city are configured. |

The importer reports every recognized source header in `importedColumns` and every unsupported header in `ignoredColumns`. Passport columns are currently ignored because the system has no passport entity or passport document type.

## License categories

`نوع الرخصة` may contain one or more values separated by `+`, Arabic/English comma, slash, ampersand, semicolon, a line break, or a standalone Arabic `و`.

Examples:

- `نقل خفيف` creates one license.
- `خصوصي + دراجة نارية` creates two licenses. The current reference-catalog migration provides the `PRIVATE` category.
- `دراجة الية` and `دباب` map to the configured `MOTORCYCLE` category.
- `خصوصي` and `نقل ثقيل` are imported when matching active catalog categories exist. Otherwise, the value is skipped with a warning; the importer does not create catalog categories automatically.
- `لا يوجد`, `بدون`, and `في انتظار الاصدار` do not create a license.

## Expiry defaults

- When a supported issue or expiry date exists in Excel, the importer uses it.
- When a new residency or driver-license record has no expiry value, the importer defaults the expiry to one year after the import date.
- When an issue date is missing, it defaults to the import date. For an already expired supplied expiry, it defaults to one year before that expiry so the date range remains valid.
- Existing non-null dates are preserved when the workbook leaves them blank.
- `defaultedExpiryDates` reports how many residency/license expiry values were generated.
- The residency record contains metadata only; no document file is created or stored by this Excel import.

## Response

The response includes:

- `validateOnly`: whether the request used the validation endpoint.
- `canImport`: `false` when at least one row has an error.
- `imported`: `true` only after a successful committed import.
- `totalRows` and `validRows`.
- planned/committed create and update counts for employees, riders, residency metadata, licenses, and platform accounts.
- `defaultedExpiryDates`.
- `importedColumns` and `ignoredColumns`.
- row-level `issues`, each with `rowNumber`, `iqamaNo`, `severity`, and `message`.
