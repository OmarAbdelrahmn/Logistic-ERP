# Create a user with roles and permissions

The user-creation endpoint now creates the account, role assignments, direct permission assignments, access scopes, and initial temporary credential in one Identity-database transaction.

## Prerequisite catalog endpoints

Use these before showing the create-user form:

- `GET /api/users/roles` returns active roles and the permission keys inherited from each role. Requires `roles.read`.
- `GET /api/users/permissions` returns the permission catalog, Arabic/English labels, sensitivity, high-trust status, and scope requirements. Requires `permissions.read`.

## Create endpoint

`POST /api/users`

The caller must have all three permissions:

- `users.create`
- `roles.manage`
- `permissions.manage`

### Request

```json
{
  "userName": "ahmed.hr",
  "initialPassword": "TemporaryP@ss123",
  "displayNameAr": "أحمد محمد",
  "displayNameEn": "Ahmed Mohammed",
  "email": "ahmed@example.com",
  "phoneNumber": "+966500000000",
  "employeeId": "019d0000-0000-7000-8000-000000000001",
  "roleAssignments": [
    {
      "roleId": "019c18d5-62e1-7000-9000-000000000002",
      "startsAtUtc": null,
      "expiresAtUtc": null,
      "reason": "HR manager",
      "isAllHousingScope": false,
      "isAllClientScope": false,
      "includesFuturePlatformContracts": false,
      "scopes": []
    }
  ],
  "directPermissionAssignments": [
    {
      "permissionKey": "hr_forms.templates.manage",
      "effect": "Grant",
      "startsAtUtc": null,
      "expiresAtUtc": null,
      "reason": "Creates and publishes HR forms",
      "isAllHousingScope": false,
      "isAllClientScope": false,
      "includesFuturePlatformContracts": false,
      "scopes": []
    }
  ]
}
```

`roleAssignments` must contain at least one unique active role. For compatibility, omitting the property assigns the protected minimal `USER` role. Sending an empty array is rejected because it would create an account without a role.

`directPermissionAssignments` is optional. Each permission key must exist in the permission catalog. `effect` accepts `Grant` or `Deny`.

Use `startsAtUtc`/`expiresAtUtc` for temporary access. When an expiry is supplied, a start time must also be supplied and the expiry must be later.

For scoped access, send entries such as:

```json
{
  "type": "Housing",
  "targetId": "019d0000-0000-7000-8000-000000000010"
}
```

Supported scope types are `Housing`, `ClientPlatform`, and `ClientContract`. Do not combine an individual scope with the matching `isAllHousingScope` or `isAllClientScope` flag.

### `201 Created` response

```json
{
  "user": {
    "id": "019d0000-0000-7000-8000-000000000020",
    "employeeId": "019d0000-0000-7000-8000-000000000001",
    "userName": "ahmed.hr",
    "email": "ahmed@example.com",
    "phoneNumber": "+966500000000",
    "displayNameAr": "أحمد محمد",
    "displayNameEn": "Ahmed Mohammed",
    "status": "PendingTemporaryPassword",
    "requiresPasswordChange": true,
    "isDevelopmentOnly": false,
    "lastLoginAtUtc": null,
    "lastActivityAtUtc": null,
    "createdAtUtc": "2026-08-29T10:00:00Z",
    "rowVersion": "AAAAAAAAB9E="
  },
  "authorization": {
    "authorizationVersion": 1,
    "roles": [
      {
        "assignmentId": "019d0000-0000-7000-8000-000000000021",
        "roleId": "019c18d5-62e1-7000-9000-000000000002",
        "roleCode": "MANAGER",
        "startsAtUtc": "2026-08-29T10:00:00Z",
        "expiresAtUtc": null,
        "reason": "HR manager",
        "isAllHousingScope": false,
        "isAllClientScope": false,
        "includesFuturePlatformContracts": false,
        "scopes": []
      }
    ],
    "directPermissions": [
      {
        "assignmentId": "019d0000-0000-7000-8000-000000000022",
        "permissionKey": "hr_forms.templates.manage",
        "effect": "Grant",
        "startsAtUtc": "2026-08-29T10:00:00Z",
        "expiresAtUtc": null,
        "reason": "Creates and publishes HR forms",
        "isAllHousingScope": false,
        "isAllClientScope": false,
        "includesFuturePlatformContracts": false,
        "scopes": []
      }
    ]
  }
}
```

### Errors

- `400 UserManagement.InvalidRequest`: malformed role, permission, effect, time window, or scope.
- `400 UserManagement.PasswordRejected`: initial password fails the configured policy.
- `404 UserManagement.NotFound`: a selected role or scope target does not exist or is inactive.
- `409 UserManagement.Duplicate`: username, email, or employee is already assigned.
- `401/403`: the caller is unauthenticated or lacks one of the three required management permissions.

If any database write fails, the user and all authorization assignments are rolled back together.
