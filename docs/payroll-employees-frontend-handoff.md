# Frontend handoff: payroll employees

## Delivery status

- Backend CRUD implementation: complete in this repository.
- Hosted application database: migrations `20260901125629_AddPayrollEmployees` and `20260901131037_LinkPayrollEmployeesToSponsors` applied and verified on 2026-09-01.
- Seed data: nine active rows were inserted because the supplied source contains numbers 1–8 and 10; number 9 was not supplied.
- Hosted API binary: this handoff does not claim the updated API has been published. Deploy the current API build before enabling the frontend page against the hosted API.
- Base route: `/api/payroll-employees`.

## Screen and field model

Suggested Arabic page title: `موظفو الرواتب`.

| API property | Arabic label | Type | Notes |
| --- | --- | --- | --- |
| `number` | م | integer | Positive and unique among active rows. |
| `sponsorId` | الكفيل | UUID | Required. Must reference an active sponsor. |
| `name` | الاسم | string | Required, maximum 200 characters. |
| `nationalId` | رقم الهوية | string | Required, exactly 10 digits; keep as a string. |
| `country` | البلد | string | Required, maximum 100 characters. |
| `joiningDate` | تاريخ الانضمام | `YYYY-MM-DD` | Required calendar date. |
| `personalIban` | الايبان الشخصي | string | Required Saudi IBAN: `SA` followed by 22 digits. |
| `salary` | الراتب | decimal | Required and greater than or equal to zero. |
| `status` | الحالة | string | Maximum 100 characters. Empty string is currently valid because the supplied rows have blank statuses. |

The API also returns `id` and `rowVersion`. They are infrastructure fields required for routing and safe updates; do not show them as editable columns.

## Authorization

Send `Authorization: Bearer <accessToken>` on every request.

| Permission | UI capability |
| --- | --- |
| `employees.read` | Show the list and record details. |
| `employees.create` | Show and submit the create action. |
| `employees.update` | Show and submit the edit action. |
| `employees.archive` | Show and submit the delete action. |
| `sponsors.read` | Load the sponsor selector with `GET /api/sponsors`. |

Hide or disable each action when its permission is unavailable. A permission failure is returned as `403 ProblemDetails`.

## Suggested TypeScript types

```ts
export type PayrollEmployeeSponsor = {
  id: string;
  employerIdentityNumber: string;
  registryNameAr: string;
  registryNameEn: string | null;
};

export type PayrollEmployee = {
  id: string;
  number: number;
  sponsorId: string;
  sponsor: PayrollEmployeeSponsor;
  name: string;
  nationalId: string;
  country: string;
  joiningDate: string; // YYYY-MM-DD
  personalIban: string; // normalized, without spaces
  salary: number;
  status: string;
  rowVersion: string; // opaque Base64 token
};

export type CreatePayrollEmployeeRequest = {
  number: number;
  sponsorId: string;
  name: string;
  nationalId: string;
  country: string;
  joiningDate: string;
  personalIban: string;
  salary: number;
  status: string;
};

export type UpdatePayrollEmployeeRequest = CreatePayrollEmployeeRequest & {
  rowVersion: string;
};
```

## Sponsor selector

Load sponsor options from:

```http
GET /api/sponsors
```

Permission: `sponsors.read`.

Display `registryNameAr` as the primary label and `employerIdentityNumber` as supporting text. Only offer rows whose `status === "Active"`; send the selected sponsor's `id` as `sponsorId`. Do not send the embedded `sponsor` response object in create or update requests.

The payroll relation is required and many-to-one: every payroll employee has exactly one sponsor, while one sponsor may be linked to many payroll employees. A sponsor linked to any payroll employee cannot be archived until those employees are reassigned or archived.

## List and search

```http
GET /api/payroll-employees
GET /api/payroll-employees?search=جمانه
```

Permission: `employees.read`.

The optional `search` value matches name, national ID, country, normalized IBAN, or status. Results are returned as a plain JSON array ordered by `number`; this endpoint is not paginated.

Response `200 OK`:

```json
[
  {
    "id": "01990000-0000-7000-8000-000000000001",
    "number": 1,
    "sponsorId": "019c18d5-62e1-7000-8000-000000000040",
    "sponsor": {
      "id": "019c18d5-62e1-7000-8000-000000000040",
      "employerIdentityNumber": "7038745530",
      "registryNameAr": "مؤسسة البوابة التجارية",
      "registryNameEn": null
    },
    "name": "جمانه عبدالكريم بن حسن القحطاني",
    "nationalId": "1125236081",
    "country": "السعودية",
    "joiningDate": "2025-09-24",
    "personalIban": "SA6980000107608016495857",
    "salary": 1000.00,
    "status": "",
    "rowVersion": "AAAAAAAAAAA="
  }
]
```

Recommended table columns follow the supplied order and add the relation: `number`, `name`, `nationalId`, `country`, `joiningDate`, `personalIban`, `salary`, `sponsor.registryNameAr`, and `status`, followed by row actions.

For Arabic RTL presentation:

- Render `nationalId` and `personalIban` with `dir="ltr"` so digits remain readable.
- Format `salary` with the Saudi Riyal currency presentation, but keep the submitted value as an unformatted JSON number.
- Display `joiningDate` using the product locale while retaining `YYYY-MM-DD` in API state.

## Get one record

```http
GET /api/payroll-employees/{id}
```

Permission: `employees.read`.

- `200 OK`: one `PayrollEmployee` object.
- `404 payroll_employee.not_found`: remove stale list state or return to the list.

Use this endpoint before opening an edit/delete dialog when the list data may be stale.

## Create

```http
POST /api/payroll-employees
Content-Type: application/json
```

Permission: `employees.create`.

```json
{
  "number": 9,
  "sponsorId": "019c18d5-62e1-7000-8000-000000000040",
  "name": "اسم الموظف",
  "nationalId": "1000000009",
  "country": "السعودية",
  "joiningDate": "2026-09-01",
  "personalIban": "SA0000000000000000000000",
  "salary": 1000.00,
  "status": ""
}
```

Success: `201 Created` with the created `PayrollEmployee`. Insert the returned object into local state; do not manufacture an `id` or `rowVersion` in the frontend.

The backend removes whitespace from the IBAN and uppercases `sa`. Replace the form/list value with the returned normalized IBAN after saving.

## Update

```http
PUT /api/payroll-employees/{id}
Content-Type: application/json
```

Permission: `employees.update`.

Send every business field plus the latest `rowVersion`:

```json
{
  "number": 1,
  "sponsorId": "019c18d5-62e1-7000-8000-000000000040",
  "name": "جمانه عبدالكريم بن حسن القحطاني",
  "nationalId": "1125236081",
  "country": "السعودية",
  "joiningDate": "2025-09-24",
  "personalIban": "SA6980000107608016495857",
  "salary": 1200.00,
  "status": "نشط",
  "rowVersion": "latest opaque rowVersion"
}
```

Success: `200 OK` with the refreshed record. Replace the complete local row so the next edit uses the new `rowVersion`.

## Delete

Deletion is a recoverable backend soft delete. The record disappears from ordinary reads but remains in the database audit history.

```http
DELETE /api/payroll-employees/{id}?rowVersion={encodedRowVersion}&reason={encodedReason}
```

Permission: `employees.archive`.

- `rowVersion` is required.
- `reason` is optional; provide an Arabic user-entered reason when the product flow collects one.
- URL-encode both values with `URLSearchParams`. Base64 `rowVersion` values can contain `+`, `/`, and `=` and must not be concatenated into the URL manually.
- Success: `204 No Content`; remove the row from local state.

Example client call:

```ts
const query = new URLSearchParams({ rowVersion: item.rowVersion });
if (reason.trim()) query.set("reason", reason.trim());

await api.delete(`/api/payroll-employees/${item.id}?${query.toString()}`);
```

## Validation and field errors

Apply matching client-side rules for responsiveness, while keeping backend responses authoritative:

- `number`: integer greater than zero.
- `sponsorId`: required UUID selected from the active sponsor lookup.
- `name`: trimmed, required, maximum 200 characters.
- `nationalId`: `/^[0-9]{10}$/`.
- `country`: trimmed, required, maximum 100 characters.
- `joiningDate`: required.
- `personalIban`: remove spaces and validate `/^SA[0-9]{22}$/i`.
- `salary`: numeric and at least zero.
- `status`: send `""` rather than `null`; maximum 100 characters.

## Concurrency

`rowVersion` is an opaque Base64 token. Never decode, edit, or reuse an older value.

- Update uses the token in the JSON body.
- Delete uses the token in the URL query.
- For `409 payroll_employee.concurrency_conflict`, close or reset the form, fetch the latest record, and tell the user that another change was saved first. Do not retry automatically.

## ProblemDetails handling

Failures use the existing response shape:

```ts
export type ApiProblem = {
  type?: string;
  title: string;       // same value as errorCode
  status: number;
  detail: string;
  instance?: string;
  errorCode: string;
  field?: string;
  correlationId: string;
};
```

| Error code | HTTP | Frontend action |
| --- | ---: | --- |
| `payroll_employee.invalid_request` | 400 | Show the general validation message and retain form values. |
| `payroll_employee.invalid_national_id` | 400 | Mark `nationalId` invalid. |
| `payroll_employee.invalid_iban` | 400 | Mark `personalIban` invalid. |
| `payroll_employee.not_found` | 404 | Remove stale state and refresh the list. |
| `payroll_employee.sponsor_not_found` | 400 | Mark `sponsorId` invalid and refresh the sponsor selector. |
| `payroll_employee.duplicate_number` | 409 | Mark `number` as already used. |
| `payroll_employee.duplicate_national_id` | 409 | Mark `nationalId` as already used. |
| `payroll_employee.duplicate_iban` | 409 | Mark `personalIban` as already used. |
| `payroll_employee.concurrency_conflict` | 409 | Reload the record before another edit/delete. |
| `payroll_employee.persistence_conflict` | 409 | Refresh the list and display a conflict message. |

Use `field` when present to bind a server failure to the form control. Include `correlationId` in support/error logs.

## Seeded hosted data

The hosted database contains these active row numbers after the migration:

```text
1, 2, 3, 4, 5, 6, 7, 8, 10
```

Status is an empty string for every seeded row. The frontend should render this as a neutral placeholder such as `—`, while continuing to preserve the empty API value until a real status vocabulary is agreed.

All nine seeded payroll employees are initially linked to `مؤسسة البوابة التجارية` (`019c18d5-62e1-7000-8000-000000000040`). Users with update permission can reassign an employee by selecting another active sponsor and submitting the ordinary `PUT` request.

## Frontend delivery checklist

- Add an RTL payroll-employees list page and navigation permission guard.
- Add API client functions for all five endpoints.
- Load active sponsors and add a required sponsor selector to create/edit forms.
- Show the embedded sponsor name in list and detail views without making another lookup request per row.
- Add create and edit forms using the shared validation rules above.
- Preserve national IDs and IBANs as strings.
- Add a confirmation dialog for delete and URL-encode `rowVersion`.
- Replace local records with every successful create/update response.
- Implement field mapping for `ProblemDetails` errors.
- Implement the `409` reload flow instead of automatic retries.
- Show empty status as `—` without converting it to `null`.
- Enable the hosted route only after the updated API binary is deployed.
