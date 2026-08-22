# Logistics ERP — البوابة للخدمات اللوجستية

Clean Architecture foundation for a single-company logistics ERP owned by **البوابة للخدمات اللوجستية**. HungerStation, Keeta, Ninja, and similar organizations are client platforms, not tenants.

## Current stage

This stage contains domain models, EF Core configurations/migrations, Identity, authentication/session services and controllers, current-user profile APIs, and API/Worker startup configuration. Business services/controllers for the operational domains and automated tests remain deferred by project decision.

## Projects

- `LogisticsERP.Domain`: pure domain entities, enums, and common base types; no EF Core, Identity, or HTTP dependencies.
- `LogisticsERP.Application`: neutral abstractions, immutable results, and authentication/user-profile contracts and service interfaces.
- `LogisticsERP.Infrastructure`: SQL Server, EF Core configurations/migrations, Identity, authentication/session and user-profile service implementations, soft-delete/audit interceptors, and dependency registration.
- `LogisticsERP.Api`: authentication and user-profile controllers, JWT/session validation, permission-ready authorization foundation, Problem Details, open development Swagger, CORS, rate limiting, health checks, forwarded headers, correlation IDs, and security headers.
- `LogisticsERP.Worker`: separate worker composition root; no scheduled business jobs yet.
- `LogisticsERP.Bootstrap`: one-time console tool for provisioning the first production `SYSTEM_ADMIN` without a bootstrap HTTP endpoint or stored password.

## Current business domains

- Company profile and operating cities.
- Employees, relationship/status history, operational assignments, sponsors, sponsorship history, and residency permits.
- Rider profiles, housing, supervisors, and residence history.
- Driver licenses, rider/health cards, medical insurance, promissory notes, and their renewal history.
- Client platforms, contracts, official platform-account ownership, encrypted credential versions, registrations, and actual rider-use history.
- Document types, requirements, private employee document versions, and tags.
- Roles, permissions, direct grants/denies, scoped access, sessions, temporary credentials, and support access.
- Leave types, configurable approval workflows, requests, immutable decisions, amendments, cancellations, and document versions.
- Employee absence compliance cases and immutable case events.
- Employee status-change requests integrated with status periods.
- Notifications, exports, saved views, dataset versions, and append-only audit entries.

Vehicle, maintenance, inventory, spare-parts, and fuel models are intentionally held as an approval plan in [docs/vehicle-maintenance-v2-plan.md](docs/vehicle-maintenance-v2-plan.md) before they enter a migration.

## Data rules

- No multi-tenancy and no `TenantId`.
- No application hard deletes. `AuditableEntity` records use `IsDeleted` plus actor/time/reason metadata.
- Event/file history is append-only. Temporal periods may be closed once but cannot be reopened, rewritten, or deleted.
- All timestamps are UTC. Riyadh/Jeddah localization belongs at system boundaries.
- UUIDv7 identifiers and SQL Server `rowversion` are used where appropriate.
- Important temporal relationships use filtered unique indexes to permit only one active row.
- Application and Identity use separate DbContexts, schemas, snapshots, migrations, and history tables.

## Seed data

- Singleton company profile: `البوابة للخدمات اللوجستية` (`ALBAWABA`).
- Global cities: `جدة / Jeddah` and `الرياض / Riyadh`.
- Active operating cities: Jeddah and Riyadh.
- Operational work types: administrative, car, and motorcycle.
- Driver-license categories: light transport and motorcycle.
- Employee document types: residency, driver license, rider card, health card, promissory note, and medical insurance (10 MB default limit).
- Protected roles: `SYSTEM_ADMIN`, `MANAGER`, and `USER`.
- Permission catalog: 55 granular permission definitions with minimal role baselines, direct grant/deny support, and client/housing scopes.

Future operating cities are ordinary `GlobalCity` + `OperatingCity` records and will be managed through CRUD when that feature is implemented. They are not hard-coded to Jeddah only.

## Database

Configure `ConnectionStrings:LogisticsDatabase` through a deployment secret source. Never commit a production database password to `appsettings.json`; rotate any credential that has already been placed there.

Apply migrations in this order:

```powershell
$env:ConnectionStrings__LogisticsDatabase = "<from-secret-store>"
dotnet ef database update --context ApplicationDbContext --project src/LogisticsERP.Infrastructure/LogisticsERP.Infrastructure.csproj
dotnet ef database update --context IdentityDbContext --project src/LogisticsERP.Infrastructure/LogisticsERP.Infrastructure.csproj
Remove-Item Env:ConnectionStrings__LogisticsDatabase
```

For deployment pipelines, idempotent scripts are available at:

- `database/scripts/application.sql`
- `database/scripts/identity.sql`

No production user or password is seeded. Provision the first production administrator after the Identity migrations with the interactive tool:

```powershell
$env:ConnectionStrings__LogisticsDatabase = "<from-secret-store>"
dotnet run --project tools/LogisticsERP.Bootstrap/LogisticsERP.Bootstrap.csproj
Remove-Item Env:ConnectionStrings__LogisticsDatabase
```

The tool refuses to create a second active production `SYSTEM_ADMIN` and requires the temporary password to be changed on first login.

## Local startup

```powershell
dotnet restore LogisticsERP.slnx
dotnet build LogisticsERP.slnx --no-restore
dotnet run --project src/LogisticsERP.Api/LogisticsERP.Api.csproj
```

After applying the Identity migrations, local Development startup creates this development-only account once:

- Username: `Omar`
- Temporary password: `P@ssword1234`
- Role: `SYSTEM_ADMIN`
- Direct access: all 55 permissions with all client and housing scopes
- Mandatory password change: enabled

The database record is marked `IsDevelopmentOnly`; production login and session validation reject it even if environments accidentally share a database. The password is never reset after the first creation.

Development endpoints:

- Swagger UI: `https://localhost:7112/swagger`
- Health: `https://localhost:7112/health/live`

The `LogisticsERP.Api` launch profile opens Swagger automatically. Swagger is intentionally unauthenticated in development at this stage and does not advertise a Bearer security scheme.

Development generates an ephemeral JWT signing key if no secret is configured. Production startup fails closed unless `Authentication:SigningKey` comes from a secret source.

Authentication defaults to 10-minute access tokens, 7-day refresh idle expiry, 30-day absolute refresh-family expiry, 10 active sessions, and a 15-second session-validation cache. See the authorization document below for endpoint and security details.

`DeviceLabel` on login is optional. It is a user-facing session label such as `Omar Laptop`, limited to 200 characters; it is not a trusted device identifier.

The private document root is `src/LogisticsERP.Api/wwwroot/private/employee-documents/{employeeId}/{documentId}/{versionId}` and is git-ignored. `UseStaticFiles` is intentionally not enabled. Upload/download endpoints are not implemented in this stage.

## Design documentation

- [Arabic system model diagram](docs/system-models-ar.md)
- [Arabic production-readiness and secrets checklist](docs/production-readiness-ar.md)
- [Current authentication and authorization status (Arabic)](docs/current-authorization-ar.md)
- [Authorization roles and 55-permission catalog (Arabic)](docs/authorization-permission-catalog-ar.md)
- [Vehicle, maintenance, inventory, and fuel V2 plan](docs/vehicle-maintenance-v2-plan.md)
- [Leave, absence compliance, and employee status V2 plan](docs/leave-absence-status-v2-plan.md)
