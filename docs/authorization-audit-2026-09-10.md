# Authorization audit — 2026-09-10

## Outcome

Authorization is permission-based. Controller policies do not require `Admin`, `Manager`, `SYSTEM_ADMIN`, or any other role name. Roles are permission bundles only; the dynamic permission handler resolves current grants from the Identity database for the authenticated user.

The Housing denial came from two independent data/model mismatches:

1. `housing.read` and `housing.manage` were marked as housing-scoped, while the Housing controller and service expose module-wide operations and do not perform per-housing filtering. The generic controller policy therefore rejected a role assignment without `IsAllHousingScope`, even though the role contained the permission key.
2. The inspected database contained no persisted permission grants for the custom `HR` role. Its assigned user also had neither an all-housing flag nor an individual housing scope.

Housing permissions are now module-level. A current grant from any active custom or system role is sufficient; role names are irrelevant.

## Flow reviewed

- JWTs contain `sub`, `sid`, `auth_version`, `password_change_required`, identity/display claims, and role-code claims. Permission claims are deliberately not embedded.
- JWT validation checks issuer, audience, signature, lifetime, session, user status, and the authorization version against the Identity database on every request.
- `[RequirePermission]` creates a dynamic `permission:<key>` policy. The policy requires an authenticated user with completed password change, then calls `IPermissionChecker`.
- `PermissionChecker` loads non-deprecated permission definitions, active role assignments and role grants, direct grants/denies, support-access grants, and access scopes. Applicable direct denies override grants.
- Permission keys are exact, ordinal strings and are validated against `PermissionKeys.All`.
- Role permission changes now increment the authorization version and revoke active sessions for every assigned user, preventing stale tokens and cached authorization snapshots.
- The user-profile authorization response now calculates effective permissions through the same `IPermissionChecker` used by policies, so a raw scoped grant without an applicable scope is no longer reported as effective.

## Controller findings fixed

- Removed anonymous access from the bulk employee/rider import endpoints. Validation requires `employees.read`; execution requires both `employees.create` and `employees.update`.
- Added explicit permission attributes to fleet catalog, supplier, vehicle, assignment, compliance, issue, accident, workflow, and private-file endpoints that previously relied only on fallback authentication plus service checks.
- Added explicit authenticated-user attributes to owner/self-service endpoints such as saved views and support-access self-request/revoke.
- Dynamic vehicle status and odometer commands retain service-level permission selection because the required permission depends on the command (`vehicles.manage`, `vehicles.decommission`, or `fleet.corrections.manage`). Their controller actions explicitly require authentication.
- Added a reflection regression test that fails if any controller action lacks explicit authorization intent, if anonymous access expands beyond login/refresh, if a controller introduces role-name restrictions, or if a controller permission is absent from the registered constant catalog.

## Deployment/data repair

Apply both new EF migrations before publishing the API:

- Application context: `MakeHousingPermissionsModuleScoped`
- Identity context: `FilterSoftDeletedRolePermissionUniqueness`

After deployment, save `housing.read` and/or `housing.manage` on the intended custom role again. The inspected `HR` role currently has zero rows in `identity.RolePermissions`, so code deployment alone cannot infer or recreate the administrator's intended grants. Saving the role permissions will invalidate affected sessions; the user must sign in again and obtain a token with the new `auth_version`.
