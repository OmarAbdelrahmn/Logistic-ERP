# Simple Platform API

This backend-only API replaces contracts and registrations as public resources with a simpler model:

`Platform -> owner account -> current actual rider -> assignment history`

Contracts and registrations are still maintained internally for compatibility and audit history.

## Business rules

- An owner rider can have at most one non-archived account on each platform.
- The owner rider is the person under whose platform identity the account is registered.
- The actual rider may be the owner or another rider using the owner's platform identity.
- An account can have only one active actual rider.
- An actual rider can have only one active platform account.
- Releasing an account closes the assignment; it never deletes assignment history.
- Updates and releases use Base64 `rowVersion` values for optimistic concurrency.
- Credential secrets are encrypted, never returned, and excluded from audit payloads.

## Shared responses

### Platform response

```json
{
  "id": "01993c00-0000-7000-8000-000000000001",
  "code": "KEETA",
  "nameAr": "كيتا",
  "nameEn": "Keeta",
  "status": "Active",
  "notes": null,
  "rowVersion": "AAAAAAAAB9E="
}
```

### Account response

```json
{
  "id": "01993c00-0000-7000-8000-000000000010",
  "platformId": "01993c00-0000-7000-8000-000000000001",
  "platformCode": "KEETA",
  "platformNameAr": "كيتا",
  "platformNameEn": "Keeta",
  "operatingCityId": "11111111-1111-1111-1111-111111111111",
  "operatingCityNameAr": "جدة",
  "operatingCityNameEn": "Jeddah",
  "ownerRiderProfileId": "01993c00-0000-7000-8000-000000000020",
  "ownerEmployeeId": "01993c00-0000-7000-8000-000000000021",
  "ownerRiderNameAr": "اسم صاحب الحساب",
  "ownerRiderNameEn": "Account Owner",
  "code": "KEETA-1001",
  "externalAccountId": "KT-98421",
  "userName": "rider.account",
  "status": "Assigned",
  "statusReason": null,
  "acquisitionDate": "2026-08-01",
  "startDate": "2026-08-01",
  "endDate": null,
  "notes": null,
  "currentAssignment": {
    "id": "01993c00-0000-7000-8000-000000000030",
    "accountId": "01993c00-0000-7000-8000-000000000010",
    "actualRiderProfileId": "01993c00-0000-7000-8000-000000000040",
    "actualEmployeeId": "01993c00-0000-7000-8000-000000000041",
    "actualRiderNameAr": "اسم المندوب الفعلي",
    "actualRiderNameEn": "Actual Rider",
    "effectiveFrom": "2026-08-24",
    "effectiveTo": null,
    "status": "Active",
    "startReason": "Assigned to Jeddah operations",
    "endReason": null,
    "wasBackdated": false,
    "backdatedReason": null,
    "assignedByUserId": "01993c00-0000-7000-8000-000000000050",
    "endedByUserId": null,
    "rowVersion": "AAAAAAAAB9I="
  },
  "rowVersion": "AAAAAAAAB9M="
}
```

## 1. List platforms

`GET /api/platforms?includeArchived=false`

Permission: `platform_accounts.read`

Request body: none.

Response: `200 OK` with an array of Platform responses.

## 2. Create platform

`POST /api/platforms`

Permission: `platform_accounts.manage`

```json
{
  "code": "KEETA",
  "nameAr": "كيتا",
  "nameEn": "Keeta",
  "status": "Active",
  "notes": null,
  "archiveReason": null,
  "rowVersion": null
}
```

Response: `200 OK` with the created Platform response.

## 3. Update or archive platform

`PUT /api/platforms/{id}`

Permission: `platform_accounts.manage`

```json
{
  "code": "KEETA",
  "nameAr": "كيتا",
  "nameEn": "Keeta",
  "status": "Disabled",
  "notes": "Temporarily disabled",
  "archiveReason": null,
  "rowVersion": "AAAAAAAAB9E="
}
```

To archive, use `status: "Archived"` and provide `archiveReason`. A platform cannot be archived while it has non-archived accounts.

Response: `200 OK` with the updated Platform response.

## 4. List and filter platform accounts

`GET /api/platform-accounts`

Permission: `platform_accounts.read`

Optional query parameters:

- `accountId`
- `platformId`
- `operatingCityId`
- `ownerRiderProfileId`
- `actualRiderProfileId`
- `status`
- `currentOnly`
- `includeArchived`

`ownerRiderProfileId` searches by the registered owner. `actualRiderProfileId` searches accounts that the rider actually used. When `currentOnly=true`, only active assignments match.

Response: `200 OK` with an array of Account responses. Each item includes its current assignment when one exists.

## 5. Get one platform account

`GET /api/platform-accounts/{id}`

Permission: `platform_accounts.read`

Request body: none.

Response: `200 OK` with one Account response.

## 6. Create platform account

`POST /api/platform-accounts`

Permission: `platform_accounts.manage`

```json
{
  "platformId": "01993c00-0000-7000-8000-000000000001",
  "operatingCityId": "11111111-1111-1111-1111-111111111111",
  "ownerRiderProfileId": "01993c00-0000-7000-8000-000000000020",
  "code": "KEETA-1001",
  "externalAccountId": "KT-98421",
  "userName": "rider.account",
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

`ownerRiderProfileId` is required. Creating a second non-archived account for the same owner and platform returns `409 Conflict`.

Response: `200 OK` with the created Account response.

## 7. Update or archive platform account

`PUT /api/platform-accounts/{id}`

Permission: `platform_accounts.manage`

Request: the same fields as Create, with the current `rowVersion`.

```json
{
  "platformId": "01993c00-0000-7000-8000-000000000001",
  "operatingCityId": "11111111-1111-1111-1111-111111111111",
  "ownerRiderProfileId": "01993c00-0000-7000-8000-000000000020",
  "code": "KEETA-1001",
  "externalAccountId": "KT-98421",
  "userName": "updated.account",
  "status": "Available",
  "statusReason": null,
  "acquisitionDate": "2026-08-01",
  "startDate": "2026-08-01",
  "endDate": null,
  "notes": "Updated operational note",
  "archiveReason": null,
  "rowVersion": "AAAAAAAAB9M="
}
```

An actively assigned account must remain `Assigned`; its platform, owner, and city cannot be changed. To archive, release it first, then use `status: "Archived"` with an `archiveReason`.

Response: `200 OK` with the updated Account response.

## 8. Assign an actual rider

`POST /api/platform-accounts/{id}/assign`

Permission: `platform_assignments.manage`

```json
{
  "actualRiderProfileId": "01993c00-0000-7000-8000-000000000040",
  "effectiveFrom": "2026-08-24",
  "reason": "Assigned to Jeddah operations",
  "wasBackdated": false,
  "backdatedReason": null
}
```

The actual rider may differ from the account owner. The operation changes the account to `Assigned`, creates append-only assignment history, and automatically maintains the internal registration/contract compatibility records.

Response: `200 OK` with an Assignment response.

## 9. Release the actual rider

`POST /api/platform-accounts/{id}/release`

Permission: `platform_assignments.manage`

```json
{
  "effectiveTo": "2026-09-30",
  "status": "Ended",
  "reason": "Rider moved to another account",
  "rowVersion": "AAAAAAAAB9I="
}
```

`status` must be `Ended` or `Cancelled`. `rowVersion` is the assignment row version returned by the account or assignment response.

Response: `200 OK` with the closed Assignment response. The account becomes `Available`.

## 10. Get account assignment history

`GET /api/platform-accounts/{id}/assignment-history`

Permission: `platform_assignments.read`

Request body: none.

Response: `200 OK` with every Assignment response for the account, newest first. The history shows every actual rider, start/end dates, status, and reasons.

## 11. Get credential history

`GET /api/platform-accounts/{id}/credential-history`

Permission: `platform_credentials.read`

Request body: none.

```json
[
  {
    "id": "01993c00-0000-7000-8000-000000000060",
    "version": 2,
    "rotatedAtUtc": "2026-08-24T12:00:00Z",
    "rotatedByUserId": "01993c00-0000-7000-8000-000000000050",
    "reason": "Scheduled credential rotation"
  }
]
```

The response contains metadata only, never the credential secret.

## 12. Rotate account credential

`POST /api/platform-accounts/{id}/rotate-credential`

Permission: `platform_credentials.rotate`

```json
{
  "secret": "new-platform-secret",
  "reason": "Scheduled credential rotation"
}
```

Response: `200 OK` with one credential-history item. The submitted `secret` is encrypted and is not returned.

## 13. Get a rider's complete platform history

`GET /api/riders/{riderProfileId}/platform-history`

Permission: `platform_assignments.read`

Request body: none.

```json
{
  "riderProfileId": "01993c00-0000-7000-8000-000000000040",
  "employeeId": "01993c00-0000-7000-8000-000000000041",
  "riderNameAr": "اسم المندوب الفعلي",
  "riderNameEn": "Actual Rider",
  "assignments": [
    {
      "assignmentId": "01993c00-0000-7000-8000-000000000030",
      "platformId": "01993c00-0000-7000-8000-000000000001",
      "platformCode": "KEETA",
      "platformNameAr": "كيتا",
      "platformNameEn": "Keeta",
      "accountId": "01993c00-0000-7000-8000-000000000010",
      "accountCode": "KEETA-1001",
      "externalAccountId": "KT-98421",
      "ownerRiderProfileId": "01993c00-0000-7000-8000-000000000020",
      "ownerRiderNameAr": "اسم صاحب الحساب",
      "ownerRiderNameEn": "Account Owner",
      "effectiveFrom": "2026-08-24",
      "effectiveTo": "2026-09-30",
      "status": "Ended",
      "startReason": "Assigned to Jeddah operations",
      "endReason": "Rider moved to another account",
      "wasBackdated": false,
      "backdatedReason": null
    }
  ]
}
```

Archived accounts and platforms remain visible in this history so historical work is not lost.

## Error response

Validation, not-found, conflict, authorization, and concurrency failures use `ProblemDetails`:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "hr.conflict",
  "status": 409,
  "detail": "The operation conflicts with the current record state.",
  "instance": "/api/platform-accounts/.../assign",
  "errorCode": "hr.conflict",
  "correlationId": "..."
}
```
