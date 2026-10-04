# External rider and work order permissions — frontend handoff

Updated October 3, 2026. The configured database has been updated. Deploy the updated backend code together with the frontend permission changes. Existing request and response bodies are unchanged; external rider deletion is a new endpoint.

## Permission keys and labels

Both modules now have exactly four permissions. Use the keys below; the UI may label `read` as **Show** and `update` as **Edit**.

| Module | UI action | Permission key | Arabic label |
| --- | --- | --- | --- |
| External riders | Show | `external_riders.read` | عرض المناديب الخارجيين |
| External riders | Create | `external_riders.create` | إنشاء المناديب الخارجيين |
| External riders | Edit | `external_riders.update` | تعديل المناديب الخارجيين |
| External riders | Delete | `external_riders.delete` | حذف المناديب الخارجيين |
| Work orders | Show | `maintenance.work_orders.read` | عرض أوامر الصيانة |
| Work orders | Create | `maintenance.work_orders.create` | إنشاء أوامر الصيانة |
| Work orders | Edit / lifecycle actions | `maintenance.work_orders.update` | تعديل أوامر الصيانة |
| Work orders | Delete / cancel | `maintenance.work_orders.delete` | إلغاء أوامر الصيانة |

Remove checks and checkboxes for `external_riders.manage` and `maintenance.work_orders.manage`. These keys are no longer registered. Each action is independent: create, edit, or delete does not imply show, and show does not imply any write permission. Do not grant access based on a role name.

## Read permissions for the current user

After login, request `GET /api/user-profile/me/authorization` with `Authorization: Bearer <accessToken>`. Use its `effectivePermissionKeys` array to gate routes and actions. Its response shape is unchanged, including `authorizationVersion`, `roles`, `directPermissions`, and `deniedPermissionKeys`.

```ts
const permissions = new Set(authorization.effectivePermissionKeys);
const can = (key: string) => permissions.has(key);

const externalRiders = {
  show: can("external_riders.read"),
  create: can("external_riders.create"),
  edit: can("external_riders.update"),
  delete: can("external_riders.delete"),
  import: can("external_riders.create") && can("external_riders.update"),
};

const workOrders = {
  show: can("maintenance.work_orders.read"),
  create: can("maintenance.work_orders.create"),
  edit: can("maintenance.work_orders.update"),
  cancel: can("maintenance.work_orders.delete"),
};
```

Use show permission for the module list/details. Gate every button separately. Avoid fetching protected lists/details for a user who has only a write permission. All permissions listed for an endpoint are required together.

For the permission editor, fetch `GET /api/users/permissions` with `permissions.read`. Render the database catalog rather than a cached two-permission list. Save the new exact keys through the existing user/role authorization APIs; their contracts have not changed.

The migration increments the authorization version for affected users. Their existing sessions and refresh tokens can become invalid. On `401`, clear the old session and ask the user to sign in again, then reload authorization. On `403`, show an access-denied message and refresh the displayed authorization; do not treat it as permission to retry with another key.

## External rider endpoints

| Method | Route | Required permission(s) | Success |
| --- | --- | --- | --- |
| GET | `/api/external-riders` | `external_riders.read` | `200`, existing array |
| GET | `/api/external-riders/{employeeId}` | `external_riders.read` | `200`, existing rider response |
| POST | `/api/external-riders` | `external_riders.create` | `201`, existing rider response |
| PUT | `/api/external-riders/{employeeId}` | `external_riders.update` | `200`, existing rider response |
| DELETE | `/api/external-riders/{employeeId}` | `external_riders.delete` | `204`, empty body |
| POST | `/api/import/external-riders/validate` | `external_riders.create` + `external_riders.update` | `200`, existing import preview |
| POST | `/api/import/external-riders` | `external_riders.create` + `external_riders.update` | `200`, existing import result |

Imports may create new riders and update existing riders, so both permissions are required for validation and import. The upload remains multipart form data with the existing `file` field. A single create/edit form requires only its respective action permission.

### Delete an external rider

Use **employeeId**, not `riderProfileId`. Send a JSON body with the reason and the latest `rowVersion` from the existing external rider response:

```http
DELETE /api/external-riders/{employeeId}
Authorization: Bearer <accessToken>
Content-Type: application/json
```

```json
{
  "reason": "External rider record is no longer needed",
  "rowVersion": "<latest base64 rowVersion>"
}
```

On `204`, remove the row from the displayed list; do not try to parse JSON. Deletion archives the associated external rider employee record and retains history. The record disappears from the normal external rider list/details. The endpoint cannot delete an ordinary employee or a sponsored internal rider.

An active vehicle assignment or active client assignment blocks deletion with `409`. A stale/invalid `rowVersion`, or a missing reason, also returns `409` under the existing archive rules. Reload the record when appropriate and display the returned `ProblemDetails`. A missing, already deleted, or non-external record returns `404`.

## Work order endpoints

The base route is `/api/maintenance-work-orders`. Existing URLs and payloads remain available. There is no new general-purpose PUT endpoint or permanent DELETE endpoint; edit controls the existing lifecycle and posting actions, and delete controls cancellation.

| Method | Route | Required permission(s) |
| --- | --- | --- |
| GET | `/api/maintenance-work-orders` | `maintenance.work_orders.read` |
| GET | `/api/maintenance-work-orders/{id}` | `maintenance.work_orders.read` |
| GET | `/api/maintenance-work-orders/external` | `maintenance.work_orders.read` + `maintenance.external_jobs.read` |
| POST | `/api/maintenance-work-orders` | `maintenance.work_orders.create` |
| POST | `/api/maintenance-work-orders/external` | `maintenance.work_orders.create` + `maintenance.external_jobs.manage` |
| POST | `/api/maintenance-work-orders/{id}/start` | `maintenance.work_orders.update` |
| POST | `/api/maintenance-work-orders/{id}/complete` | `maintenance.work_orders.update` |
| POST | `/api/maintenance-work-orders/{id}/close` | `maintenance.work_orders.update` |
| POST | `/api/maintenance-work-orders/{id}/cancel` | `maintenance.work_orders.delete` |
| POST | `/api/maintenance-work-orders/{id}/materials` | `maintenance.work_orders.update` + `inventory.stock.move` |
| POST | `/api/maintenance-work-orders/materials/{usageId}/reverse` | `maintenance.work_orders.update` + `inventory.stock.adjust` |
| POST | `/api/maintenance-work-orders/{id}/oil-change` | `maintenance.work_orders.update` + `maintenance.oil.complete` + `inventory.stock.move` |
| POST | `/api/maintenance-work-orders/{id}/part-sales` | `maintenance.work_orders.update` + `maintenance.part_sales.manage` + `inventory.stock.move` |
| POST | `/api/maintenance-work-orders/{id}/customer-labor-charges` | `maintenance.work_orders.update` + `maintenance.customer_labor_charges.manage` |
| POST | `/api/maintenance-work-orders/{id}/mechanic-labor-payments` | `maintenance.work_orders.update` + `maintenance.mechanic_labor_payments.manage` |
| POST | `/api/maintenance-work-orders/{id}/other-financial-entries?income=true` | `maintenance.work_orders.update` + `maintenance.external_jobs.manage` |
| POST | `/api/maintenance-work-orders/{id}/customer-payments` | `maintenance.work_orders.update` + `maintenance.external_jobs.manage` |

The existing company list query parameters (`maintenanceLocationId`, `vehicleId`, `status`) and external list query parameters (`maintenanceLocationId`, `status`) are unchanged.

The related maintenance plan endpoints now use create/edit separately: `POST /api/maintenance/plans` requires `maintenance.work_orders.create`; `PUT /api/maintenance/plans/{id}` requires `maintenance.work_orders.update`; `GET /api/maintenance/plans` retains `maintenance.work_orders.read`. Batch spare parts at `POST /api/SparePart/spare-parts?date=<date>` require `maintenance.work_orders.update` plus `inventory.stock.move`. Work order picker endpoints that previously used `maintenance.work_orders.read` keep that permission.

### Cancel a work order

Label the button **Cancel** / **إلغاء**. Gate it with `maintenance.work_orders.delete` and the existing state rules. Only an `Open` order with no posted material usage can be cancelled. A cancelled order remains in history; do not permanently remove it from the database or call a DELETE route.

```http
POST /api/maintenance-work-orders/{id}/cancel
Authorization: Bearer <accessToken>
Content-Type: application/json
```

```json
{
  "occurredAtUtc": "2026-10-03T12:00:00Z",
  "workPerformed": null,
  "qualityCheckNotes": null,
  "notes": "Order cancelled by the administrator",
  "rowVersion": "<latest work order rowVersion>"
}
```

Use the actual action timestamp rather than the example. Success is `200` with the existing work order response, including its updated status and `rowVersion`. Replace the displayed row with that response. Start/complete/close also retain their existing action body and response. A permission does not override state or inventory constraints: handle `409` for invalid transitions/stale versions and `404` for missing records.

## Database and rollout notes

Both migrations were applied to the configured database: `20261003095839_SplitExternalRiderAndWorkOrderPermissions` and `20261003095953_SplitExternalRiderAndWorkOrderGrants`. There are four active definitions for each module. Legacy role/direct manage assignments became create, update, and delete assignments; read assignments were retained. Direct grants/denials, validity windows, and scopes are preserved. System Admin receives all four external rider permissions; the default System Admin and Manager work order grants are updated.

Deploy this backend revision before using the new frontend controls. Reload the permission catalog and require affected users to sign in again. Existing create/edit/read response shapes have not changed.

Validation: API build passed with no warnings; 68 focused permission, controller, and external rider checks passed in an isolated test project. SQL was verified in a rolled-back transaction, including an expired denial with an explicit scope, before the database update was committed. An unrelated existing anonymous-endpoint audit expectation and existing main fleet test-project compilation issues prevented treating the whole test suite as passing.

The SQL scripts in `docs/sql/20261003-external-rider-work-order-permission-definitions.sql` and `docs/sql/20261003-external-rider-work-order-permission-grants.sql` are idempotent migration scripts. They intentionally contain no transaction wrappers; execute both in one transaction. Automatic rollback of the grant split is blocked because independent permissions may be edited after migration; rollback requires review of the saved pre-update permission data.
