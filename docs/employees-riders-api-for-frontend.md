# Employees and Riders API

Frontend integration reference for the current workforce API.

## Base rules

- Base URL: `/api`
- Send `Authorization: Bearer <accessToken>` on every request.
- All route identifiers are GUIDs.
- JSON dates use `YYYY-MM-DD`. Date-time values use ISO 8601 UTC.
- `RowVersion` is an opaque concurrency token. Return the latest value in update and archive requests.
- Successful reads and mutations return JSON unless stated otherwise.
- Validation, permission, not-found, conflict, and concurrency failures are returned as `ProblemDetails`.

## Model meaning / معنى النموذج

| Property | English | العربية |
|---|---|---|
| `IsEmployee = true` | Administrative/staff person. | الشخص إداري/موظف مكتبي. |
| `IsEmployee = false` | Operational rider. | الشخص رايدر تشغيلي. |
| `EngagementType = SponsoredInternal` | Rider or employee connected to the company sponsorship. | موظف أو رايدر على كفالة الشركة. |
| `EngagementType = OutsideRider` | External rider. Must have `IsEmployee = false`; `SponsorId` may be null. | رايدر خارجي. يجب أن يكون `IsEmployee = false` ويمكن أن يكون `SponsorId` فارغاً. |
| `RiderProfile` | Optional one-to-one rider extension. A rider must have one when `IsEmployee = false`. | امتداد رايدر اختياري بعلاقة واحد لواحد، ويجب وجوده للرايدر. |
| `CurrentWorkPlatform` | Current platform assignment for a rider. It is `null` when the rider has no open platform assignment. It includes the platform ID and the rider's platform-account ID. | المنصة التي يعمل عليها الرايدر حالياً. تكون `null` إذا لم يكن للرايدر إسناد منصة مفتوح، وتتضمن معرّف المنصة ومعرّف حساب الرايدر على المنصة. |
| `IqamaNo` | One plain 10-digit string containing digits only when supplied. It is required for `Active` employees. | رقم إقامة واحد كنص من 10 أرقام فقط عند إدخاله، ويكون مطلوباً للموظف النشط. |
| `Status` | `Draft`, `Onboarding`, `Active`, `Suspended`, `OnLeave`, `Terminated`, `Archived`, `Fleeing`, `Accident`, `Sick`. | حالات دورة الحياة: مسودة، تهيئة، نشط، موقوف، إجازة، منتهٍ، مؤرشف، متغيب/هارب، حادث، مرضي. |

`ResidencyProfession` and `WorkingForMeAs` are direct employee fields. There is no separate employee residency-permit entity. An Iqama scan or other proof is stored through the employee document APIs.

## Permissions

| Permission | Allows |
|---|---|
| `employees.read` | List, view, and view work history. |
| `employees.create` | Create an employee or rider record. |
| `employees.update` | Edit employee fields and perform status/role transitions. |
| `employees.archive` | Archive an employee. |
| `riders.read` | List riders and outside riders. |
| `riders.manage` | Edit rider-profile fields. |

## Employee endpoints

### 1. List employees

`GET /api/employees`

Permission: `employees.read`

English: Returns all non-deleted employee records with the main role, engagement, sponsor, and rider-profile summary.

العربية: يعيد جميع سجلات الموظفين غير المؤرشفة مع ملخص الدور والارتباط والكفيل وملف الرايدر.

Response `200 OK`:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "iqamaNo": "1234567890",
    "fullNameAr": "أحمد محمد",
    "fullNameEn": "Ahmed Mohammed",
    "nationality": "Saudi",
    "primaryPhone": "0500000000",
    "isEmployee": false,
    "engagementType": "OutsideRider",
    "status": "Active",
    "workingForMeAs": "Delivery Rider",
    "residencyProfession": "عامل توصيل",
    "sponsorId": null,
    "sponsorNameAr": null,
    "riderProfileId": "00000000-0000-0000-0000-000000000001",
    "rowVersion": "AAAAAAAAAAA="
  }
]
```

### 2. Get employee details

`GET /api/employees/{employeeId}`

Permission: `employees.read`

English: Returns the complete employee record, optional rider profile, and work-history records.

العربية: يعيد بيانات الموظف كاملة، وملف الرايدر إن وجد، وسجل تغييرات العمل.

Response `200 OK`:

```json
{
  "employee": {
    "id": "00000000-0000-0000-0000-000000000000",
    "iqamaNo": "1234567890",
    "residencyProfession": "عامل توصيل",
    "workingForMeAs": "Delivery Rider",
    "fullNameAr": "أحمد محمد",
    "fullNameEn": "Ahmed Mohammed",
    "nationality": "Saudi",
    "birthDate": "1995-01-20",
    "gender": "Male",
    "primaryPhone": "0500000000",
    "secondaryPhone": null,
    "email": "ahmed@example.com",
    "profilePhotoDocumentId": null,
    "maritalStatus": "Married",
    "emergencyContactName": "محمد أحمد",
    "emergencyContactRelationship": "Brother",
    "emergencyContactPhone": "0511111111",
    "isEmployee": false,
    "engagementType": "OutsideRider",
    "status": "Active",
    "statusReason": null,
    "hireDate": "2026-08-23",
    "operationalWorkTypeId": null,
    "operatingCityId": null,
    "sponsorId": null,
    "contractStartDate": null,
    "contractEndDate": null,
    "probationEndDate": null,
    "terminationDate": null,
    "alternateContactName": null,
    "alternateContactPhone": null,
    "notes": null,
    "rowVersion": "AAAAAAAAAAA="
  },
  "rider": {
    "id": "00000000-0000-0000-0000-000000000001",
    "employeeId": "00000000-0000-0000-0000-000000000000",
    "iqamaNo": "1234567890",
    "fullNameAr": "أحمد محمد",
    "fullNameEn": "Ahmed Mohammed",
    "engagementType": "OutsideRider",
    "status": "Active",
    "tShirtSize": "Large",
    "operationalNotes": null,
    "rowVersion": "AAAAAAAAAAA="
  },
  "workHistory": []
}
```

`rider` is `null` for an administrative employee. `workHistory` contains `ChangeType`, `OldValue`, `NewValue`, `EffectiveDate`, `Reason`, `ChangedByUserId`, and `CreatedAtUtc`.

### 3. Create employee or rider

`POST /api/employees`

Permission: `employees.create`

English: Creates one main employee record. Set `isEmployee` to choose administrative employee or rider. For a rider, include the `rider` object.

العربية: ينشئ سجل موظف رئيسي. استخدم `isEmployee` لتحديد موظف إداري أو رايدر. يجب إرسال كائن `rider` عند إنشاء رايدر.

Response: `201 Created` with the same `EmployeeDetailsResponse` shape as the details endpoint.

Request body:

```json
{
  "iqamaNo": "1234567890",
  "residencyProfession": "عامل توصيل",
  "workingForMeAs": "Delivery Rider",
  "fullNameAr": "أحمد محمد",
  "fullNameEn": "Ahmed Mohammed",
  "nationality": "Saudi",
  "birthDate": "1995-01-20",
  "gender": "Male",
  "primaryPhone": "0500000000",
  "secondaryPhone": null,
  "email": "ahmed@example.com",
  "profilePhotoDocumentId": null,
  "maritalStatus": "Married",
  "emergencyContactName": null,
  "emergencyContactRelationship": null,
  "emergencyContactPhone": null,
  "isEmployee": false,
  "engagementType": "OutsideRider",
  "status": "Onboarding",
  "statusReason": null,
  "hireDate": "2026-08-23",
  "operationalWorkTypeId": null,
  "operatingCityId": null,
  "sponsorId": null,
  "contractStartDate": null,
  "contractEndDate": null,
  "probationEndDate": null,
  "terminationDate": null,
  "alternateContactName": null,
  "alternateContactPhone": null,
  "notes": null,
  "rider": {
    "tShirtSize": "Large",
    "operationalNotes": null,
    "rowVersion": null
  },
  "rowVersion": null
}
```

For an administrative employee, send `isEmployee: true` and `rider: null`.

### 4. Update employee details

`PUT /api/employees/{employeeId}`

Permission: `employees.update`

English: Updates employee fields, lifecycle status, and role (`isEmployee`). Status and role changes are recorded in work history; `statusReason` is used for a status change, or the history entry uses "Employee status updated." if omitted. When changing an administrative employee to a rider, include `rider`; a rider with an active platform or vehicle assignment cannot be changed to an administrative employee. `Archived` must use the archive endpoint.

العربية: يحدث بيانات الموظف وحالة دورة الحياة والدور (`isEmployee`). تُسجل تغييرات الحالة والدور في سجل العمل؛ ويستخدم `statusReason` عند تغيير الحالة، أو الرسالة الافتراضية "Employee status updated." إذا لم تُرسل. عند تحويل الموظف الإداري إلى رايدر أرسل كائن `rider`؛ ولا يمكن تحويل الرايدر إلى موظف إداري إذا كان لديه إسناد منصة أو مركبة نشط. حالة `Archived` تتم من خلال نقطة الأرشفة.

Request: same shape as create. Include the current employee `rowVersion`. If the employee is a rider and rider fields are being changed, include the current rider `rowVersion`.

Response `200 OK`: `EmployeeDetailsResponse`.

### 5. Change employee lifecycle status

`POST /api/employees/{employeeId}/status-transitions`

Permission: `employees.update`

English: Changes the lifecycle status and records a status entry in work history. `Archived` is handled by the archive endpoint. Use this endpoint for `Fleeing`, `Accident`, and `Sick` as well.

العربية: يغير حالة دورة الحياة ويسجل التغيير في سجل العمل. حالة `Archived` تتم من خلال نقطة الأرشفة. استخدم هذه النقطة أيضاً لحالات `Fleeing` و`Accident` و`Sick`.

Request:

```json
{
  "status": "Active",
  "effectiveDate": "2026-08-23",
  "reason": "All onboarding requirements completed"
}
```

To become `Active`, the record must have a valid 10-digit `iqamaNo`; a `SponsoredInternal` record must have `sponsorId`; and a rider must have a rider profile.

Response `200 OK`: `EmployeeDetailsResponse`.

### 6. Change administrative/rider role

`POST /api/employees/{employeeId}/role-transitions`

Permission: `employees.update`

English: Converts an administrative employee to a rider or a rider to an administrative employee. The employee record and Iqama remain the same. Rider history is not deleted.

العربية: يحول الموظف الإداري إلى رايدر أو يحول الرايدر إلى موظف إداري. يبقى سجل الموظف ورقم الإقامة كما هما ولا يتم حذف تاريخ الرايدر.

Request for administrative employee → rider:

```json
{
  "isEmployee": false,
  "effectiveDate": "2026-08-23",
  "reason": "Moved to delivery operations",
  "rider": {
    "tShirtSize": "Large",
    "operationalNotes": null,
    "rowVersion": null
  }
}
```

Request for rider → administrative employee:

```json
{
  "isEmployee": true,
  "effectiveDate": "2026-08-23",
  "reason": "Promoted to administration",
  "rider": null
}
```

The conversion to administrative employee is rejected while the rider has an active platform assignment or active vehicle assignment. The rider profile is retained and simply no longer appears in rider lists.

Response `200 OK`: `EmployeeDetailsResponse`.

### 7. Get employee work history

`GET /api/employees/{employeeId}/work-history`

Permission: `employees.read`

English: Returns role, status, engagement, profession, operational work, city, and sponsor changes.

العربية: يعيد تغييرات الدور والحالة والارتباط والمهنة والعمل التشغيلي والمدينة والكفيل.

Response `200 OK`:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000010",
    "changeType": "Role",
    "oldValue": "Administrative",
    "newValue": "Rider",
    "effectiveDate": "2026-08-23",
    "reason": "Moved to delivery operations",
    "changedByUserId": "00000000-0000-0000-0000-000000000020",
    "createdAtUtc": "2026-08-23T10:00:00Z"
  }
]
```

### 8. Archive employee

`PATCH /api/employees/{employeeId}/archive`

Permission: `employees.archive`

English: Soft-deletes the employee and marks the lifecycle status as `Archived`. It is rejected when the rider has an active platform or vehicle assignment.

العربية: يؤرشف الموظف حذفاً منطقياً ويضع الحالة `Archived`. يرفض الطلب إذا كان للرايدر إسناد منصة أو مركبة نشط.

Request:

```json
{
  "reason": "Record closed",
  "rowVersion": "AAAAAAAAAAA="
}
```

Response: `204 No Content`.

## Rider endpoints

### 9. List riders

`GET /api/riders`

Permission: `riders.read`

Optional query:

- `outsideOnly=true`: return only riders with `engagementType = OutsideRider`.
- `outsideOnly=false`: return all riders.

English: Returns records where `isEmployee = false` and a rider profile exists.

العربية: يعيد السجلات التي يكون فيها `isEmployee = false` ويوجد لها ملف رايدر.

Response `200 OK`:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000001",
    "employeeId": "00000000-0000-0000-0000-000000000000",
    "iqamaNo": "1234567890",
    "fullNameAr": "أحمد محمد",
    "fullNameEn": "Ahmed Mohammed",
    "engagementType": "OutsideRider",
    "status": "Active",
    "tShirtSize": "Large",
    "operationalNotes": null,
    "rowVersion": "AAAAAAAAAAA="
  }
]
```

### 10. List outside riders

`GET /api/riders/outside`

Permission: `riders.read`

English: Shortcut for `GET /api/riders?outsideOnly=true`.

العربية: اختصار للنقطة `GET /api/riders?outsideOnly=true` لإظهار الرايدرز الخارجيين فقط.

Response: same array shape as the rider list endpoint.

### 11. Update rider profile

`PUT /api/riders/{riderProfileId}`

Permission: `riders.manage`

English: Updates rider-only fields. Employee identity, Iqama, sponsorship, engagement, and lifecycle status are updated through employee endpoints.

العربية: يحدث بيانات الرايدر فقط. بيانات الهوية والإقامة والكفيل والارتباط والحالة يتم تحديثها من خلال نقاط الموظفين.

Request:

```json
{
  "tShirtSize": "Large",
  "operationalNotes": "Uses motorcycle for evening operations",
  "rowVersion": "AAAAAAAAAAA="
}
```

Allowed `tShirtSize` values: `ExtraSmall`, `Small`, `Medium`, `Large`, `ExtraLarge`, `DoubleExtraLarge`, `TripleExtraLarge`. Send `null` or an empty value when no size is selected.

Response `200 OK`: `RiderDetailsResponse`.

## Minimal external-rider endpoints

Use these endpoints to create an external rider from the essential identity and operating information. The API creates and maintains the required `Employee` and `RiderProfile` rows automatically. New records are created with `isEmployee = false`, `engagementType = OutsideRider`, and `status = Active`.

### List external riders

`GET /api/external-riders`

Permission: `riders.read`

Response `200 OK`:

```json
[
  {
    "employeeId": "00000000-0000-0000-0000-000000000000",
    "riderProfileId": "00000000-0000-0000-0000-000000000001",
    "iqamaNo": "1234567890",
    "fullNameAr": "أحمد محمد",
    "primaryPhone": "0500000000",
    "operatingCityId": "00000000-0000-0000-0000-000000000003",
    "operationalWorkTypeId": "00000000-0000-0000-0000-000000000004",
    "status": "Active",
    "rowVersion": "AAAAAAAAAAA="
  }
]
```

### Get one external rider

`GET /api/external-riders/{employeeId}`

Permission: `riders.read`

Response `200 OK`: one item with the same shape as the list response.

### Create an external rider

`POST /api/external-riders`

Permission: `employees.create`

Required fields:

```json
{
  "iqamaNo": "1234567890",
  "fullNameAr": "أحمد محمد",
  "primaryPhone": "0500000000",
  "operatingCityId": "00000000-0000-0000-0000-000000000003",
  "operationalWorkTypeId": "00000000-0000-0000-0000-000000000004"
}
```

`iqamaNo` must be a unique 10-digit value. `primaryPhone` cannot exceed 32 characters. `operatingCityId` and `operationalWorkTypeId` must reference existing catalog records. Load their selectable values from `GET /api/hr-catalogs/operating-cities` and `GET /api/hr-catalogs/operational-work-types`. Response: `201 Created` with the external-rider response and a `Location` header for the get-one endpoint.

### Update an external rider

`PUT /api/external-riders/{employeeId}`

Permission: `employees.update`

Send the latest `rowVersion` returned by a create, get, or update request:

```json
{
  "iqamaNo": "1234567890",
  "fullNameAr": "أحمد محمد المحدث",
  "rowVersion": "AAAAAAAAAAA="
}
```

The update changes only the Iqama number and Arabic full name. All other employee and rider data is preserved. Response: `200 OK` with a refreshed `rowVersion`.

## Frontend workflow

1. Load `GET /api/employees` or `GET /api/riders` for lists.
2. Open details with `GET /api/employees/{employeeId}`.
3. Use the returned `rowVersion` for updates and archive operations.
4. Create an administrative employee with `isEmployee: true`.
5. Create a rider with `isEmployee: false` and a `rider` object.
6. Use `/role-transitions` for administrative/rider conversion.
7. Use `/status-transitions` for lifecycle changes.
8. Refresh the details response after every successful mutation because the server returns updated row-version values.

## Related APIs

Iqama scans, licenses, rider cards, health cards, and other documents are managed by separate controllers. The employee/rider core endpoints above store the links and identity; they do not upload binary files.

### Upload an Ajeer contract

`POST /api/riders/{riderProfileId}/documents/ajeer-contract`

Permission: `documents.upload`

Content type: `multipart/form-data`

Form fields:

| Field | Type | Required |
|---|---|---|
| `documentNumber` | string | Yes |
| `issueDate` | `YYYY-MM-DD` | Yes |
| `expiryDate` | `YYYY-MM-DD` | Yes |
| `notes` | string | No |
| `file` | PDF or image | Yes |

The frontend does not send `documentTypeId` to this dedicated rider endpoint. The API assigns the seeded `AJEER_CONTRACT` type (`019c18d5-62e1-7000-8000-000000000036`) automatically.

Response `200 OK`: `EmployeeDocumentResponse`, including `documentTypeCode: "AJEER_CONTRACT"` and `documentTypeNameAr: "عقود اجير"`.
