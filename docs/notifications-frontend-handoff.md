# Notifications frontend handoff

## Scope and access

The backend routes are under `/api/notifications`. Send the normal bearer access token and use camel-case JSON properties. Notification display text is Arabic: every item returns `title` and `body` from the stored Arabic message. English title/body fields are not returned. These are the current repository contracts; deployment of this build is not verified here.

| Permission | Frontend capability |
| --- | --- |
| `notifications.read` | Load the current user's feed and unread count; change the state of that user's notifications. |
| `notifications.manage` | Create a notification for a specified user. Reserve this for an administrative or service UI. |

The server uses the authenticated user for feed ownership. The frontend does not send a user ID when reading or changing state.

## Endpoint map

| Method | Route | Request | Success response |
| --- | --- | --- | --- |
| `POST` | `/api/notifications/query` | JSON filter and page request | `NotificationFeed` with `items`, `nextCursor`, `unreadCount`, `effectivePermissions` |
| `GET` | `/api/notifications` | Query string | `{ items, nextCursor }` |
| `GET` | `/api/notifications/unread-count` | Optional permission query string | `{ count: number }` |
| `POST` | `/api/notifications/{id}/state` | JSON `action` and `rowVersion` | Updated `NotificationItem` |
| `POST` | `/api/notifications` | JSON create request | Created `NotificationItem` |

Use `POST /query` for the notification center when the list and badge count need the same permission filter. The GET routes remain available for callers that need them separately.

## Feed request and response

```ts
type NotificationSeverity =
  | "Information" | "Success" | "Warning" | "Error" | "Critical";

interface NotificationItem {
  id: string;                         // GUID
  eventType: string;                  // producer-defined, not a fixed enum
  severity: NotificationSeverity;
  title: string;                      // Arabic display title
  body: string;                       // Arabic display message
  sourceEntityType: string | null;
  sourceEntityId: string | null;      // GUID
  deepLink: string | null;
  visibleAtUtc: string;               // ISO 8601 timestamp
  expiresAtUtc: string | null;
  readAtUtc: string | null;
  acknowledgedAtUtc: string | null;
  archivedAtUtc: string | null;
  rowVersion: string;                 // opaque Base64 version
  permissionKeys: string[];           // [] for a personal notification
}

interface NotificationQueryRequest {
  permissions?: string[] | null;
  unreadOnly?: boolean;               // default false
  pageSize?: number;                  // default 50, valid range 1–200
  cursor?: string | null;
}

interface NotificationFeed {
  items: NotificationItem[];
  nextCursor: string | null;
  unreadCount: number;
  effectivePermissions: string[];
}
```

Example first page:

```http
POST /api/notifications/query
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "permissions": ["fleet.accidents.read", "fleet.vehicles.read"],
  "unreadOnly": false,
  "pageSize": 50,
  "cursor": null
}
```

`items` contains only notifications addressed to the signed-in user that are already visible, have not expired, are not archived, and match the effective permission audience. Sort order is newest `visibleAtUtc` first, with a stable ID tie-breaker. `unreadCount` counts all matching visible unread notifications, independent of the current page or cursor. A read notification remains in the full feed; an archived notification does not.

An accident item now has Arabic display text such as `"title": "متابعة الحادث ACC-123"` and `"body": "تم تسجيل حادث للمركبة."`. The frontend renders these strings directly and does not need to select a language-specific field.

Use `nextCursor` unchanged for the next page. It is opaque and can contain a colon; URL-encode it for GET requests. Reset the cursor when `permissions` or `unreadOnly` changes. A `null` cursor means there is no next page.

### Permission filter semantics

- Omit `permissions` or send `null` to use every permission the user currently holds and include personal notifications with no permission audience.
- Send a nonempty array to show notifications matching **any** requested permission the user actually holds. This excludes personal notifications with no audience.
- Send `[]` to request an empty feed and count of zero.
- Unknown keys, blank keys, or more than 256 requested keys return `400 system.invalid_request`. Known keys the user lacks are removed from `effectivePermissions`.
- Permissions are checked again on every request. A revoked permission hides its audience's notifications and prevents state changes; it does not erase stored history.

For GET, repeat the parameter for multiple keys:

```text
GET /api/notifications?permissions=fleet.accidents.read&permissions=fleet.vehicles.read&unreadOnly=false&pageSize=50
GET /api/notifications/unread-count?permissions=fleet.vehicles.read
```

Omitting the GET parameter means all currently authorized audiences plus personal notifications. The GET feed returns `{ items, nextCursor }`; it does not include `unreadCount` or `effectivePermissions`. The count endpoint returns `{ count }`.

## State actions

```http
POST /api/notifications/{id}/state
Content-Type: application/json

{ "action": "read", "rowVersion": "<latest item.rowVersion>" }
```

| `action` | Effect |
| --- | --- |
| `read` | Sets `readAtUtc` if it is still null. |
| `unread` | Clears `readAtUtc`. |
| `acknowledge` | Sets acknowledgement time and marks the item read. |
| `archive` | Sets archive time and removes the item from normal feeds and counts. |

The action is case-insensitive. Replace the local item with the returned object, including its new `rowVersion`, then refresh the count if the badge is shown. A `409 system.concurrency_conflict` means the supplied version is stale: reload the feed before retrying. The endpoint applies ownership and current permission checks; inaccessible items return `404 system.not_found`. There is no unarchive action.

## Create notification (administrative UI)

`POST /api/notifications` requires `notifications.manage` and a JSON body:

```ts
interface CreateNotificationRequest {
  recipientUserId: string;            // existing user GUID
  eventType: string;
  severity: NotificationSeverity;     // parsed case-insensitively
  titleAr: string;
  titleEn: string;
  bodyAr: string;
  bodyEn: string;
  sourceEntityType?: string | null;
  sourceEntityId?: string | null;
  deepLink?: string | null;
  scopeSnapshotJson?: string | null;  // JSON encoded as a string, when needed
  deduplicationKey: string;
  visibleAtUtc?: string | null;       // defaults to server time
  expiresAtUtc?: string | null;
  permissionKeys?: string[] | null;
}
```

The titles, bodies, event type, and deduplication key must contain text. `permissionKeys: null` or omission creates a personal notification. A supplied audience must be a nonempty array of known keys; matching uses any one key. The `(recipientUserId, deduplicationKey)` pair is unique: attempting to create it again returns `409 system.conflict`. An unknown recipient returns `404 system.not_found`. The response is the created `NotificationItem`.

## Frontend behavior

1. Gate the center and badge on `notifications.read`; gate creation on `notifications.manage`.
2. Load the first page with `/query`, render `title` and `body` directly in the Arabic interface, and format UTC timestamps for the user's locale. These fields replace the former `titleAr`, `titleEn`, `bodyAr`, and `bodyEn` response fields.
3. Show the returned `unreadCount` as the badge. Do not derive the total from the current page length.
4. Keep `rowVersion` with each item. Use the latest value for each state action.
5. Navigate with `deepLink` when present and supported by the frontend route map; otherwise render the notification without a destination.
6. Treat `eventType`, `severity`, and `sourceEntityType` as technical codes. Do not render them as English prose; map any visible severity label to Arabic.
7. Notifications are delivered when the producer event occurs; granting a permission later does not backfill old deliveries.

## Error handling

Expected service failures use HTTP Problem Details with `title` and `errorCode` set to the same code, plus `detail`, `status`, `instance`, and `correlationId`.

| Status | Code | Meaning for the UI |
| --- | --- | --- |
| `400` | `system.invalid_request` | Invalid page size, cursor, permission filter, create fields, or state action. |
| `401` / `403` | Authentication or permission failure | Refresh sign-in state or hide the unauthorized capability. |
| `404` | `system.not_found` | Recipient or notification is unavailable to this caller. |
| `409` | `system.concurrency_conflict` | Reload the item and use its latest `rowVersion`. |
| `409` | `system.conflict` | Duplicate notification deduplication key for the recipient. |

Source contracts: `NotificationsController`, `SystemContracts`, `NotificationService`, and `NotificationPermissionFilter`. The existing audience reference is [notification-permissions-api.md](notification-permissions-api.md).
