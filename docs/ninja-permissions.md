# Dedicated Ninja permissions

Updated 28 October 2026. The `/api/ninja/*` endpoints and their service methods use dedicated Ninja permission keys, checked against the `NINJA` client-platform scope.

| Permission | Allowed operations |
| --- | --- |
| `ninja.read` | Read accounts, performance, deductions, reports and saved sheet reviews; compare account sheets |
| `ninja.accounts.update` | Change account status/phone, upload account sheet reviews and edit review notes |
| `ninja.assignments.create` | Assign an account; also requires `ninja.accounts.update` |
| `ninja.assignments.delete` | Return an account; also requires `ninja.accounts.update` |
| `ninja.assignments.dates.update` | Correct current assignment start dates or ended assignment start/end dates; this permission alone is sufficient |
| `ninja.credentials.rotate` | Supply a new password during assignment, return or credential update; also requires the operation's other permissions |
| `ninja.performance.import` | Import daily performance |
| `ninja.deductions.import` | Import daily deductions |
| `ninja.evaluation.manage` | Manage reviewed evaluation context, responsibility and snapshots |
| `ninja.evaluation.export` | Export permitted evaluation results |

Password rotation permission is required whenever `newPassword` is supplied, including during cooldown. Performance/deduction import permissions are independent of account-update permission and of each other.

Account list/detail GET responses now include a nullable `password` field. Returning its readable saved value requires the existing `platform_credentials.read` permission in the same Ninja client-platform scope, in addition to `ninja.read`. Readers without credential-read access still receive account metadata, with `password: null`. A rotation grant does not imply credential-read access. No new permission definition or migration is needed for this field. See the [password read handoff](ninja-account-password-reads.md).

The permission catalog lists these under category `Ninja` with Arabic/English labels, client scope and sensitive-data metadata. The system administrator role receives all ten keys. Existing manager/custom role grants and user grants are not automatically copied from shared platform permissions. Assign the required Ninja keys explicitly through access management with the Ninja platform scope. Shared platform, Jahez and Chefz grants alone do not authorize Ninja endpoints.

Generic `/api/platform-accounts` setup, metadata and history routes retain their own platform permissions. Ninja lifecycle and credential changes continue to use the dedicated Ninja workflow.

## Deployment

The assignment-date correction permission migrations `AddNinjaAssignmentDatePermissionCatalog` and `GrantNinjaAssignmentDateCorrectionAccess` were applied and verified on the hosted database on **28 October 2026 at 14:28 Riyadh time**. These register only `ninja.assignments.dates.update` and its administrator grant; they preserve existing grant IDs and do not update assignment data. API publication remains a separate step and was not performed by this database deployment. See [assignment date integration](ninja-assignment-date-corrections.md) and [hosted deployment evidence](ninja-assignment-dates-hosted-deployment-20261028.md).

Apply application migration `AddNinjaPermissionCatalog` and identity migration `GrantNinjaPermissions`, then publish the updated API and update frontend permission checks. Reviewed SQL scripts are `database/scripts/ninja-permissions-application.sql` and `database/scripts/ninja-permissions-identity.sql`. These migrations add permission definitions and administrator grants without changing account, rider, assignment or report data. Rollback removes only the added seed definitions/grants; revert the API and frontend together if rolling back.

Both permission migrations were applied to the hosted database on 17 October 2026 at 18:15 Riyadh time. A fresh connection verified seven active Ninja definitions, seven administrator grants and Omar's active all-client administrator scope with no applicable Ninja denial. See [hosted permission deployment](ninja-permissions-hosted-deployment-20261017.md) for evidence and repeatable tooling. Database application and API publication are separate deployment steps.

## Verification

The solution builds with zero warnings/errors. The focused Ninja workflow/catalog and Ninja/Chefz/Jahez authorization suites pass 100 tests; four SQL/sample-file tests require external fixtures and are skipped. Both EF contexts report no pending model changes. The broader controller authorization audit passes two checks and fails two unrelated checks for Import anonymous endpoints and a Maintenance role restriction. Those initial checks preceded the hosted database deployment recorded above; API publication was not performed by the permission database deployment.
