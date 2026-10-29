# Ninja assignment date corrections — frontend integration

Updated 28 October 2026. **Hosted database permissions were applied and verified at 14:28 Riyadh time.** The database now contains the dedicated permission and its administrator grant. This run did not publish API binaries or verify the hosted HTTP endpoint; publish the updated API before enabling the new frontend action. See [deployment evidence](ninja-assignment-dates-hosted-deployment-20261028.md).

## Endpoint and permission

`PUT /api/ninja/accounts/{accountId}/assignments/{assignmentId}/dates`

The only required permission is **`ninja.assignments.dates.update`**, scoped to the `NINJA` client platform. Both the route and service enforce it. `ninja.accounts.update`, `ninja.assignments.create`, `ninja.assignments.delete` and `platform_assignments.update` do not substitute for it. A user with the new permission does not need those other permissions to correct dates. Existing account/history read routes retain their own read permissions.

The catalog entry is in category `Ninja`, named `تعديل تواريخ تكليفات نينجا` / `Edit Ninja assignment dates`, with client scope and sensitive-data metadata. Only the system administrator role is seeded with this new grant; other roles/users must receive it explicitly through access management.

| Operation | Permission |
| --- | --- |
| Submit a date correction | `ninja.assignments.dates.update` in the Ninja platform scope |
| Read a Ninja account/current assignment ID and token | `ninja.read` |
| Read the existing assignment history, dates and historical tokens | `platform_assignments.read` |

Read permissions support the screen's existing data loading; they are not additional checks on the correction endpoint. Evaluate permission state for the Ninja platform, rather than allowing any generic account-edit grant.

## HTTP contract

- Method: `PUT`.
- Path: `/api/ninja/accounts/{accountId}/assignments/{assignmentId}/dates`.
- Headers: `Authorization: Bearer <access-token>`, `Content-Type: application/json`, `Accept: application/json`.
- `accountId` is the ERP account GUID, not the external Captain ID. `assignmentId` is that account's assignment GUID, not a rider/employee GUID.
- The request and success response are plain JSON objects, with no `data` envelope.

| Request field | Type | Rule |
| --- | --- | --- |
| `effectiveFrom` | `YYYY-MM-DD` string | Required; not after today in Riyadh |
| `effectiveTo` | `YYYY-MM-DD` string or null | Null/omitted for current assignments; required for ended assignments and on/after the start |
| `reason` | string | Required after trimming; maximum 1000 characters |
| `rowVersion` | Base64 string | Required token from the assignment being edited |

## Requests and responses

Send both date fields for ended assignments. To edit one date, retain the other date. Current assignments require a null/omitted end and accept only a start-date change. Cancelled/planned assignments and close/reopen attempts are rejected.

Current assignment example:

```json
{
  "effectiveFrom": "2026-10-10",
  "effectiveTo": null,
  "reason": "Correct the reviewed start date",
  "rowVersion": "AAAAAAAAB9E="
}
```

Ended assignment example:

```json
{
  "effectiveFrom": "2026-10-10",
  "effectiveTo": "2026-10-20",
  "reason": "Correct the reviewed assignment period",
  "rowVersion": "AAAAAAAAB9E="
}
```

Dates are inclusive Riyadh business dates, no later than today. The end must be on/after the start. A nonblank reason of at most 1000 characters and the **assignment** Base64 rowversion are required. The current assignment token is `account.assignmentRowVersion`; historical tokens are available from `/api/platform-accounts/{accountId}/assignment-history` with its existing read permission. The account's own `rowVersion` is not the assignment token.

HTTP 200 response (illustrative IDs/token):

```json
{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "assignmentId": "00000000-0000-0000-0000-000000000002",
  "effectiveFrom": "2026-10-10",
  "effectiveTo": "2026-10-20",
  "status": "Ended",
  "rowVersion": "AAAAAAAAB9I="
}
```

Store the returned assignment token. Refresh assignment/account history after saving; refresh after a 409 before retrying. Hide the correction action unless the new permission is granted, and show an editable end only for ended assignments. The TypeScript contracts/fetch client are in `ninja-api-contracts.ts`.

## Frontend flow

1. Load the account and the assignment record through the existing read flow. `GET /api/ninja/accounts/{accountId}` supplies `currentAssignmentId` and `assignmentRowVersion`. The existing `GET /api/platform-accounts/{accountId}/assignment-history` returns a plain array containing each assignment's `id`, `accountId`, `effectiveFrom`, `effectiveTo`, `status` and `rowVersion`.
2. Show **تعديل تواريخ التكليف** only with the new scoped permission. Allow `Active` assignments with a null end and `Ended` assignments with an existing end. Hide the action for `Planned` and `Cancelled` rows.
3. Prepopulate the current dates. On a current assignment, edit only the start and send a null end. On an ended assignment, edit either/both fields and send the unchanged value for the other date. Do not use this endpoint to end an assignment or reopen one.
4. Require a reason. Send date input values as `YYYY-MM-DD` strings; avoid converting them through JavaScript UTC timestamps. Disable repeated submission while saving.
5. On HTTP 200, replace the edited row's dates and assignment rowversion with the returned values, then refresh the displayed assignment history/account. The response uses `assignmentId`; the existing history row uses `id`.
6. On a stale-token 409, reload the assignment and let the user review and resubmit. On a recorded-data/overlap conflict, show the backend's `detail` without automatically moving another assignment or deleting imported data.

TypeScript example using the provided client (`apiOrigin`, `accessToken`, `accountId`, `assignment` and `form` come from the application's existing state):

```ts
import { NinjaApiClient, NinjaApiError } from "./ninja-api-contracts";

const api = new NinjaApiClient(apiOrigin, () => accessToken);

try {
  const saved = await api.updateAssignmentDates(accountId, assignment.id, {
    effectiveFrom: form.effectiveFrom,
    effectiveTo: assignment.status === "Active" ? null : form.effectiveTo,
    reason: form.reason.trim(),
    rowVersion: assignment.rowVersion,
  });

  assignment.effectiveFrom = saved.effectiveFrom;
  assignment.effectiveTo = saved.effectiveTo;
  assignment.rowVersion = saved.rowVersion;
  // Refresh the application's account/history queries after success.
} catch (error) {
  if (error instanceof NinjaApiError) {
    // error.status is the HTTP status.
    // error.problem.errorCode identifies a business conflict.
    // error.problem.detail is the user-facing backend message.
    // error.problem.errors may contain ASP.NET binding errors.
    // Reload on ninja.conflict; do not silently retry a stale token.
  }
  throw error;
}
```

Equivalent HTTP request (replace the origin, IDs and assignment token):

```http
PUT https://YOUR-API-ORIGIN/api/ninja/accounts/00000000-0000-0000-0000-000000000001/assignments/00000000-0000-0000-0000-000000000002/dates
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "effectiveFrom": "2026-10-10",
  "effectiveTo": "2026-10-20",
  "reason": "Correct the reviewed assignment period",
  "rowVersion": "AAAAAAAAB9E="
}
```

## Data preservation and conflicts

- No other assignment is moved, and status, rider, payment model, original lifecycle reasons, phone and credentials are preserved.
- Account periods cannot overlap beyond a same-day handover boundary.
- Saved performance/deductions must remain inside their original assignment. Adding or removing assignment coverage on any account day with imported source/fact data is rejected, protecting shared-day allocations.
- Entered daily figures, including explicit zeros, cannot be excluded. Empty excluded markers are soft-deleted with audit metadata. New boundary markers do not invent figures or handover order; use the existing day-usage workflow when same-day ordering is ambiguous.
- The latest reviewed responsibility revision cannot retain lines outside the corrected period; correct responsibility first. Historical snapshots and superseded revisions remain intact.
- Every actual date change appends a `RiderAssignmentEvent` with `Action: NinjaAssignmentDatesCorrected`, old/new dates, reason and actor. Matching dates are a no-op after rowversion validation.
- The account write lock serializes the operation with lifecycle/import/day-usage writers. Dates, markers and the correction event commit through one `SaveChangesAsync`; failed writes clear pending state.

| HTTP | Meaning |
| --- | --- |
| 400 | Invalid dates/reason, unsupported status, or a close/reopen attempt |
| 401 | Missing/expired authentication; use the application's existing authentication flow |
| 403 | Dedicated permission missing or outside the Ninja platform scope |
| 404 | `ninja.not_found` for an unknown/non-Ninja account; `ninja.assignment_not_found` for an assignment outside that account |
| 409 | `ninja.conflict` for stale tokens/write conflicts; `ninja.assignment_dates_conflict` for overlap, imports, figures or responsibility conflicts |

Business errors use ProblemDetails with `errorCode`, `detail`, `status` and `correlationId`. Authorization middleware can return `api.forbidden` before the service's `ninja.forbidden`. Invalid JSON/date binding can instead expose an `errors` dictionary. Example conflict:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "تعارض في البيانات",
  "status": 409,
  "detail": "التواريخ تتداخل مع تكليف آخر للحساب.",
  "instance": "/api/ninja/accounts/00000000-0000-0000-0000-000000000001/assignments/00000000-0000-0000-0000-000000000002/dates",
  "errorCode": "ninja.assignment_dates_conflict",
  "correlationId": "<request-correlation-id>"
}
```

## Deployment

Applied application migration `20261028111233_AddNinjaAssignmentDatePermissionCatalog` and identity migration `20261028111320_GrantNinjaAssignmentDateCorrectionAccess` together on hosted database `db67927` at **14:28:28 Riyadh time on 28 October 2026**. A rollback rehearsal passed first, and a separate post-commit verification confirmed both migrations, ten active Ninja permission definitions and ten administrator Ninja grants. Publish the API separately. Reviewed incremental SQL is in `database/scripts/ninja-assignment-dates-application.sql` and `database/scripts/ninja-assignment-dates-identity.sql`.

These migrations inserted only the new definition, administrator grant and migration-history markers. Before/after transaction checks preserved 328 Ninja accounts, 235 assignments, 3390 performance records, 10 deduction records, 299 daily usage rows, and existing permission definitions/grants. The existing permission snapshot cache expires within 60 seconds; refresh effective permissions after expiry when using an already-running API.

Permission definition ID: `019c18d5-62e1-7000-a000-000000000319`. Administrator grant ID: `019c18d5-62e1-7000-b000-000000000283`. Roll back the identity migration before the application catalog migration when reverting the feature; update the API/frontend together. Runtime correction events and existing assignment data are not deleted by migration rollback.

## Verification

- Solution build: zero warnings/errors.
- Ninja workflow suite: 132 passed, 11 environment-dependent SQL/workbook cases skipped.
- Ninja authorization/HTTP suite: 59 passed, including the new route's permission isolation and JSON binding.
- Both EF contexts report no pending model changes. Comparing snapshots against their pre-change copies confirmed only one definition and one administrator grant were added; existing seed IDs/data are preserved.
- Incremental scripts were reviewed, rehearsed with rollback, committed to the hosted database and verified through a fresh connection. See [the hosted deployment report](ninja-assignment-dates-hosted-deployment-20261028.md).
- The assignment date-correction SQL Server runtime/concurrency test remains unverified: LocalDB failed to start. No production assignment was edited for testing. Hosted permission migration execution is verified; hosted API publication/HTTP testing was not performed by this run.
