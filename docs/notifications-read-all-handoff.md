# Mark all notifications as read: frontend handoff

Implemented endpoint: **`POST /api/notifications/read-all`**. Requires a bearer token and **`notifications.read`**. It operates on the authenticated user's notifications. Deployment of this build has not been verified.

## Request

To mark all accessible notifications as read, send an empty JSON object:

```http
POST /api/notifications/read-all
Authorization: Bearer <access-token>
Content-Type: application/json

{}
```

To mark only a screen's permission audiences, send its permission filter. For Jahez:

```json
{ "permissions": ["jahez.read"] }
```

```ts
interface NotificationReadAllRequest {
  permissions?: string[] | null;
}

interface NotificationReadAllResponse {
  markedCount: number;
  readAtUtc: string; // ISO 8601 UTC timestamp assigned to newly read items
  effectivePermissions: string[];
}
```

Do not send a user ID, notification IDs, cursor, page size, row versions, or an idempotency key. A JSON body is required; use `{}` for the default operation.

## Success response

HTTP **200** returns the response directly:

```json
{
  "markedCount": 12,
  "readAtUtc": "2026-10-04T12:00:00+00:00",
  "effectivePermissions": ["jahez.read", "notifications.read"]
}
```

`markedCount` is the number changed by this request across all matching notifications, including pages the frontend has not loaded. An empty matching set returns 200 with `markedCount: 0`. The example permissions depend on the caller's actual grants; they are not a fixed list.

## Scope and behavior

- Omitted or `null` permissions include all currently authorized audiences plus personal notifications without an audience.
- A nonempty permission array matches any requested permission the user actually holds. Personal notifications are excluded in this mode.
- `permissions: []` changes nothing. Known but ungranted keys are excluded from `effectivePermissions`.
- Only currently visible, unread, unexpired, unarchived, nondeleted notifications belonging to the signed-in user are changed. Other users' and inaccessible notifications remain untouched.
- Already-read timestamps are preserved. This action does not acknowledge, archive, or delete notifications.
- Updates preserve per-notification audit records and refresh row versions. Repeating the call is safe for already-read items; notifications that become eligible between calls can be marked by a later call.

## Frontend integration

Bind this endpoint to a **Mark all as read / تحديد الكل كمقروء** button. Use the same permission filter as the displayed notification center. Disable the button while the request is pending. On success, reload the first feed page and badge count; previously loaded row versions may be stale, and new notifications can arrive during the operation. Keep read notifications in the normal feed; remove them from an unread-only feed.

```ts
async function markAllNotificationsRead(
  baseUrl: string,
  accessToken: string,
  permissions?: string[] | null,
): Promise<NotificationReadAllResponse> {
  const response = await fetch(`${baseUrl}/api/notifications/read-all`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(permissions === undefined ? {} : { permissions }),
  });
  if (!response.ok) throw await response.json();
  return response.json();
}
```

For Jahez, call `markAllNotificationsRead(baseUrl, token, ["jahez.read"])`, then refresh `POST /api/notifications/query` using that same filter and a null cursor.

## Failures and validation

Errors follow the existing ProblemDetails response. Invalid permission keys, blank keys, or more than 256 keys return **400 `system.invalid_request`**. Missing authentication or endpoint permission returns **401/403**. A concurrent modification can return **409 `system.concurrency_conflict`**; refresh and retry. With the relational database, all batches commit in one transaction, so a failed batch rolls back the operation.

The implementation needs no migration or new permission. Automated coverage verifies ownership, audience filtering, empty/invalid filters, more than 200 notifications, preserved existing read states, audit records, repeated calls, and the controller response. SQL-backed transaction verification remains unavailable because the local SQL Server instance cannot start.

See [the complete notifications handoff](notifications-frontend-handoff.md) for the feed and single-item state endpoints.
