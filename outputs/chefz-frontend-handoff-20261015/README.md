# Chefz frontend integration package — 15 October 2026

Start with [the complete implementation handoff](docs/chefz-accounts-reports-frontend-handoff-20261015.md).

The backend implements admin lifecycle controls, overall/per-sponsor counts, two Chefz accounts per rider, and individual rider/account breakdowns in performance and money reports. FullTime fees are 25 SAR per account per assigned day. Other platforms retain their existing policies.

The hosted database passed all 19 readiness checks; no migration or production data update was needed. **The updated API was not deployed by this task. Deploy it before expecting the new hosted HTTP behavior.** No frontend source was changed.

Included:

- [TypeScript entry point](docs/chefz-accounts-and-reports-contracts.ts) and [underlying contracts](docs/chefz-new-endpoints-contracts.ts); keep both files together.
- [Five complete synthetic JSON response examples](docs/chefz-account-updates-examples/README.md), plus their TypeScript validation file.
- [Hosted database readiness record](docs/chefz-hosted-readiness-20261015.md) and the exact read-only SQL/JSON/XML evidence under `outputs/`.
- Supporting Chefz module, status, assignment, report and financial integration guides.

Validation: solution build succeeded with zero warnings/errors; 71 Chefz workflow checks passed. TypeScript contracts and all example literals passed strict TypeScript 5.9.3 compilation. Synthetic example counts, grouping and financial arithmetic were checked. Hosted API smoke tests remain dependent on publishing the updated API.

Examples are for development only. They contain no real riders or credentials and must not be imported into production. Where an older module guide differs, use the dated complete implementation handoff above.
