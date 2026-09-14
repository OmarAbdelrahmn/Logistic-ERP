# Archived users and restore: frontend handoff

Backend support is available for listing archived users and restoring an archived account.

## Permissions

| Operation | Required permission |
| --- | --- |
| List archived users | `users.read` |
| Restore an archived user | `users.archive` |

Use the frontend's effective-permission state to hide or disable the corresponding UI. The API still enforces both permissions.

## List archived users

`GET /api/users/archived`

Optional query parameter:

- `search`: searches username, Arabic display name, English display name, and email.

Example:

```http
GET /api/users/archived?search=ahmed
Authorization: Bearer <access-token>
```

### `200 OK`

The response is an array of the existing `ManagedUserResponse` shape. Results contain only archived users, are ordered by newest archive first, and are limited to 500 rows.

```json
[
  {
    "id": "019d0000-0000-7000-8000-000000000020",
    "employeeId": "019d0000-0000-7000-8000-000000000001",
    "userName": "ahmed.hr",
    "email": "ahmed@example.com",
    "phoneNumber": "+966500000000",
    "displayNameAr": "أحمد محمد",
    "displayNameEn": "Ahmed Mohammed",
    "profileImageUrl": null,
    "status": "Archived",
    "requiresPasswordChange": false,
    "isDevelopmentOnly": false,
    "lastLoginAtUtc": "2026-09-01T09:30:00Z",
    "lastActivityAtUtc": "2026-09-01T09:45:00Z",
    "createdAtUtc": "2026-08-20T08:00:00Z",
    "rowVersion": "AAAAAAAAB9E="
  }
]
```

The existing `GET /api/users` endpoint continues to return only non-archived users.

## Restore an archived user

`PATCH /api/users/{userId}/restore`

Send the current `rowVersion` from the archived-users response:

```json
{
  "rowVersion": "AAAAAAAAB9E="
}
```

### `200 OK`

Returns the restored `ManagedUserResponse`:

```json
{
  "id": "019d0000-0000-7000-8000-000000000020",
  "employeeId": "019d0000-0000-7000-8000-000000000001",
  "userName": "ahmed.hr",
  "email": "ahmed@example.com",
  "phoneNumber": "+966500000000",
  "displayNameAr": "أحمد محمد",
  "displayNameEn": "Ahmed Mohammed",
  "profileImageUrl": null,
  "status": "Active",
  "requiresPasswordChange": false,
  "isDevelopmentOnly": false,
  "lastLoginAtUtc": "2026-09-01T09:30:00Z",
  "lastActivityAtUtc": "2026-09-01T09:45:00Z",
  "createdAtUtc": "2026-08-20T08:00:00Z",
  "rowVersion": "AAAAAAAAB9I="
}
```

Restore behavior:

- Changes the account status to `Active`.
- Clears the soft-delete and lockout state.
- Preserves the user's profile, employee link, password, roles, and direct permission assignments.
- Increments the authorization version and invalidates server-side session validation state.
- Does not sign the user in or create a new session.

## Error handling

Errors use the API's standard `application/problem+json` response.

| Status | `errorCode` | Frontend behavior |
| --- | --- | --- |
| `401` | `UserManagement.CurrentUserUnavailable` | Return to sign-in or refresh authentication. |
| `403` | permission policy or `UserManagement.ProtectedAccount` | Show that the action is not allowed. |
| `404` | `UserManagement.NotFound` | Remove the row from the archived list; it may already have been restored. |
| `409` | `UserManagement.ConcurrencyConflict` | Refetch archived users and ask the user to retry. |

Example conflict response:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "UserManagement.ConcurrencyConflict",
  "status": 409,
  "detail": "تعذر تنفيذ العملية المطلوبة.",
  "instance": "/api/users/019d0000-0000-7000-8000-000000000020/restore",
  "errorCode": "UserManagement.ConcurrencyConflict",
  "correlationId": "..."
}
```

## Suggested TypeScript client

Reuse the existing `ManagedUserResponse` type.

```ts
export interface RestoreManagedUserRequest {
  rowVersion: string;
}

export async function getArchivedUsers(search?: string): Promise<ManagedUserResponse[]> {
  const params = search?.trim() ? `?search=${encodeURIComponent(search.trim())}` : "";
  return api.get<ManagedUserResponse[]>(`/api/users/archived${params}`);
}

export async function restoreUser(
  userId: string,
  request: RestoreManagedUserRequest,
): Promise<ManagedUserResponse> {
  return api.patch<ManagedUserResponse>(`/api/users/${userId}/restore`, request);
}
```

Adjust the `api.get` and `api.patch` return unwrapping to the frontend's HTTP-client convention.

## UI integration checklist

1. Add an “Archived users” view or tab visible to callers with `users.read`.
2. Fetch it from `GET /api/users/archived`; debounce the optional server-side search using the same behavior as the active-user list.
3. Show “Restore” only when the caller has `users.archive`.
4. Submit the row's latest `rowVersion` in the restore request.
5. Disable the action while the request is running to prevent duplicate submissions.
6. After success, remove the row from the archived-users cache/list and invalidate or refetch the active-users list.
7. On `409`, refetch instead of resubmitting the stale row version.

