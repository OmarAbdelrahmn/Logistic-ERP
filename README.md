# Logistics ERP — البوابة للخدمات اللوجستية

Clean Architecture foundation for a single-company logistics ERP owned by **البوابة للخدمات اللوجستية**. HungerStation, Keeta, Ninja, and similar organizations are client platforms, not tenants.

## Current stage

This stage contains domain models, EF Core configurations, database migrations, Identity foundations, and API/Worker startup configuration only. It intentionally contains no business services or controllers. Automated testing is deferred by project decision.

## Projects

- `LogisticsERP.Domain`: pure domain entities, enums, and common base types; no EF Core, Identity, or HTTP dependencies.
- `LogisticsERP.Application`: neutral abstractions and immutable result types; no business services yet.
- `LogisticsERP.Infrastructure`: SQL Server, EF Core configurations/migrations, Identity, soft-delete/audit interceptors, and dependency registration.
- `LogisticsERP.Api`: JWT validation, permission-ready authorization foundation, Problem Details, open development Swagger, CORS, rate limiting, health checks, forwarded headers, correlation IDs, and security headers.
- `LogisticsERP.Worker`: separate worker composition root; no scheduled business jobs yet.

## Current business domains

- Company profile and operating cities.
- Employees, relationship/status/job-title history, sponsored and outside-rider details.
- Rider profiles, housing, supervisors, and residence history.
- Client platforms, contracts, platform rider accounts, encrypted credential versions, and rider-client assignment history.
- Document types, requirements, employee document versions, and tags.
- Roles, permissions, direct grants/denies, scoped access, sessions, temporary credentials, and support access.
- Leave types, configurable approval workflows, requests, immutable decisions, amendments, cancellations, and document versions.
- Employee absence compliance cases and immutable case events.
- Employee status-change requests integrated with status periods.
- Notifications, exports, saved views, dataset versions, and append-only audit entries.

Vehicle, maintenance, inventory, spare-parts, and fuel models are intentionally held as an approval plan in [docs/vehicle-maintenance-v2-plan.md](docs/vehicle-maintenance-v2-plan.md) before they enter a migration.

## Data rules

- No multi-tenancy and no `TenantId`.
- No application hard deletes. `AuditableEntity` records use `IsDeleted` plus actor/time/reason metadata.
- History records are append-only; modifying or deleting them is rejected by the persistence interceptor.
- All timestamps are UTC. Riyadh/Jeddah localization belongs at system boundaries.
- UUIDv7 identifiers and SQL Server `rowversion` are used where appropriate.
- Important temporal relationships use filtered unique indexes to permit only one active row.
- Application and Identity use separate DbContexts, schemas, snapshots, migrations, and history tables.

## Seed data

- Singleton company profile: `البوابة للخدمات اللوجستية` (`ALBAWABA`).
- Global city: `جدة / Jeddah`.
- Active operating city: Jeddah.

Future operating cities are ordinary `GlobalCity` + `OperatingCity` records and will be managed through CRUD when that feature is implemented. They are not hard-coded to Jeddah only.

## Database

Configure `ConnectionStrings:LogisticsDatabase` through environment or deployment configuration. The checked-in LocalDB value is development-only and contains no credentials.

Apply migrations in this order:

```powershell
dotnet ef database update --context ApplicationDbContext --project src/LogisticsERP.Infrastructure/LogisticsERP.Infrastructure.csproj
dotnet ef database update --context IdentityDbContext --project src/LogisticsERP.Infrastructure/LogisticsERP.Infrastructure.csproj
```

For deployment pipelines, idempotent scripts are available at:

- `database/scripts/application.sql`
- `database/scripts/identity.sql`

Do not seed users or passwords. Provision the first administrator later through a secure one-time operation.

## Local startup

```powershell
dotnet restore LogisticsERP.slnx
dotnet build LogisticsERP.slnx --no-restore
dotnet run --project src/LogisticsERP.Api/LogisticsERP.Api.csproj
```

Development endpoints:

- Swagger UI: `https://localhost:7112/swagger`
- Health: `https://localhost:7112/health/live`

The `LogisticsERP.Api` launch profile opens Swagger automatically. Swagger is intentionally unauthenticated in development at this stage and does not advertise a Bearer security scheme.

Development generates an ephemeral JWT signing key if no secret is configured. Production startup fails closed unless `Authentication:SigningKey` comes from a secret source.

## Design documentation

- [Arabic system model diagram](docs/system-models-ar.md)
- [Vehicle, maintenance, inventory, and fuel V2 plan](docs/vehicle-maintenance-v2-plan.md)
- [Leave, absence compliance, and employee status V2 plan](docs/leave-absence-status-v2-plan.md)
