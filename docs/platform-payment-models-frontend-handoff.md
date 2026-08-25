# Platform payment models: frontend handoff

## Values

Use these exact case-insensitive request values and expect these exact response values:

- `PayPerOrder`
- `Salary`

Do not send translated values to the API. Translate only the displayed labels.

## Changed endpoints

Primary API:

- `GET /api/platforms`
- `POST /api/platforms`
- `PUT /api/platforms/{id}`
- `GET /api/platform-accounts` (also adds the optional `paymentModel` query parameter)
- `GET /api/platform-accounts/{id}`
- `POST /api/platform-accounts`
- `PUT /api/platform-accounts/{id}`
- `POST /api/platform-accounts/{id}/assign`
- `POST /api/platform-accounts/{id}/release`
- `GET /api/platform-accounts/{id}/assignment-history`
- `GET /api/riders/{riderProfileId}/platform-history`
- `GET /api/employees`

Compatibility API:

- `GET|POST|PUT /api/platform-operations/platforms[/{id}]`
- `GET|POST|PUT /api/platform-operations/accounts[/{id}]`
- `GET|POST /api/platform-operations/assignments`
- `POST /api/platform-operations/assignments/{id}/close`

The export download payload also changes for `moduleKey: "platform-accounts"` by adding a `PaymentModel` CSV column. Credential endpoints and their payloads are unchanged.

## Endpoint-by-endpoint changes

### `GET /api/platforms`

Response addition on every platform:

```json
{
  "supportedPaymentModels": ["PayPerOrder", "Salary"]
}
```

Use this array as the source for the account payment-model dropdown. Jahez returns `["PayPerOrder"]`.

### `POST /api/platforms`

Request addition: `supportedPaymentModels` is required.

```json
{
  "code": "JAHEZ",
  "nameAr": "جاهز",
  "nameEn": "Jahez",
  "supportedPaymentModels": ["PayPerOrder"],
  "status": "Active",
  "notes": null,
  "archiveReason": null,
  "rowVersion": null
}
```

Send one or two distinct values only. Empty arrays, duplicate values, and unknown values return `400` / `hr.invalid_request`.

### `PUT /api/platforms/{id}`

Request addition: `supportedPaymentModels` is required on every update, together with the current `rowVersion`.

```json
{
  "code": "KEETA",
  "nameAr": "كيتا",
  "nameEn": "Keeta",
  "supportedPaymentModels": ["PayPerOrder", "Salary"],
  "status": "Active",
  "notes": null,
  "archiveReason": null,
  "rowVersion": "AAAAAAAAB9E="
}
```

You cannot remove a model while non-archived accounts still use it. The API returns `409` with `platform.payment_models_in_use`.

### `GET /api/platform-accounts`

New optional query parameter:

```text
GET /api/platform-accounts?paymentModel=PayPerOrder
GET /api/platform-accounts?platformId={platformId}&paymentModel=Salary
```

Response additions:

- Each account contains `paymentModel`.
- `currentAssignment`, if present, contains `paymentModel`.

```json
{
  "id": "01993c00-0000-7000-8000-000000000010",
  "platformId": "01993c00-0000-7000-8000-000000000001",
  "externalAccountId": "JZ-98421",
  "paymentModel": "PayPerOrder",
  "status": "Assigned",
  "currentAssignment": {
    "id": "01993c00-0000-7000-8000-000000000030",
    "paymentModel": "PayPerOrder",
    "actualRiderProfileId": "01993c00-0000-7000-8000-000000000040",
    "status": "Active"
  }
}
```

All earlier filters remain supported: `accountId`, `platformId`, `operatingCityId`, `ownerRiderProfileId`, `actualRiderProfileId`, `status`, `currentOnly`, and `includeArchived`.

### `GET /api/platform-accounts/{id}`

Response additions are the same as the list endpoint: `paymentModel` on the account and on `currentAssignment` when the account is assigned. There is no request change.

### `POST /api/platform-accounts`

Request addition: `paymentModel` is required.

```json
{
  "platformId": "01993c00-0000-7000-8000-000000000001",
  "operatingCityId": "11111111-1111-1111-1111-111111111111",
  "ownerRiderProfileId": "01993c00-0000-7000-8000-000000000020",
  "code": "JAHEZ-1001",
  "externalAccountId": "JZ-98421",
  "userName": "rider.account",
  "paymentModel": "PayPerOrder",
  "status": "Available",
  "statusReason": null,
  "acquisitionDate": "2026-08-01",
  "startDate": "2026-08-01",
  "endDate": null,
  "notes": null,
  "archiveReason": null,
  "rowVersion": null
}
```

Frontend rule: load the selected platform first, then populate this field only from its `supportedPaymentModels`. An unsupported selection returns `409` / `platform.payment_model_not_supported`.

### `PUT /api/platform-accounts/{id}`

Request addition: `paymentModel` is required on every update, with the account's current `rowVersion`.

If an assigned account changes payment model, the backend validates the rider's full active-account combination. The update can return either rider-limit error below.

### `POST /api/platform-accounts/{id}/assign`

The request body is unchanged. Do **not** send `paymentModel`; the backend uses the account's saved model.

```json
{
  "actualRiderProfileId": "01993c00-0000-7000-8000-000000000040",
  "effectiveFrom": "2026-08-24",
  "reason": "Assigned to Jeddah operations",
  "wasBackdated": false,
  "backdatedReason": null
}
```

Response addition: the returned assignment includes `paymentModel`.

Validation additions:

- Third active account: `409` / `platform.rider_account_limit_reached`.
- Second active Salary account: `409` / `platform.rider_salary_account_limit_reached`.

### `POST /api/platform-accounts/{id}/release`

The request is unchanged. The closed assignment in the response now contains `paymentModel`.

### `GET /api/platform-accounts/{id}/assignment-history`

Response addition: every assignment-history item contains `paymentModel`. Use this historical value rather than assuming the account's current model.

### `GET /api/riders/{riderProfileId}/platform-history`

Response addition: every object in `assignments` contains `paymentModel`. This is the payment model used for that historical assignment.

### `GET /api/employees`

New field: `currentWorkPlatforms`, an array with zero, one, or two accounts for a rider. Each item contains `paymentModel`.

```json
{
  "currentWorkPlatforms": [
    {
      "id": "01993c00-0000-7000-8000-000000000001",
      "code": "KEETA",
      "nameAr": "كيتا",
      "nameEn": "Keeta",
      "platformRiderAccountId": "01993c00-0000-7000-8000-000000000010",
      "externalAccountId": "KT-98421",
      "paymentModel": "Salary"
    },
    {
      "id": "01993c00-0000-7000-8000-000000000002",
      "code": "JAHEZ",
      "nameAr": "جاهز",
      "nameEn": "Jahez",
      "platformRiderAccountId": "01993c00-0000-7000-8000-000000000011",
      "externalAccountId": "JZ-1033",
      "paymentModel": "PayPerOrder"
    }
  ]
}
```

`currentWorkPlatform` remains temporarily, but it contains only the first current account. New UI must use `currentWorkPlatforms`.

### `GET /api/platform-operations/platforms`

Compatibility endpoint response addition: `supportedPaymentModels` on every platform.

### `POST /api/platform-operations/platforms`

### `PUT /api/platform-operations/platforms/{id}`

Compatibility endpoint request addition: `supportedPaymentModels` is required. The accepted values and validation are identical to `/api/platforms`.

### `GET /api/platform-operations/accounts`

Compatibility endpoint response addition: `paymentModel` on every account.

### `POST /api/platform-operations/accounts`

### `PUT /api/platform-operations/accounts/{id}`

Compatibility endpoint request addition: `paymentModel` is required. This API uses `clientPlatformId` and optionally `registeredEmployeeId` instead of the simpler API's `platformId` and `ownerRiderProfileId`.

### `GET /api/platform-operations/assignments`

Response addition: each assignment contains `paymentModel`.

### `POST /api/platform-operations/assignments`

The request body is unchanged. The backend reads the payment model from `platformRiderAccountId`, applies the two-account/one-Salary rules, and returns `paymentModel` in the assignment.

### `POST /api/platform-operations/assignments/{id}/close`

The request body is unchanged. The returned closed assignment includes `paymentModel`.

### `GET /api/platform-accounts/{id}/credential-history`

### `POST /api/platform-accounts/{id}/rotate-credential`

No changes to either credential endpoint or payload.

### `GET /api/exports/{id}/download`

When the export request uses `moduleKey: "platform-accounts"`, the downloaded CSV adds a `PaymentModel` column immediately before `Status`.
