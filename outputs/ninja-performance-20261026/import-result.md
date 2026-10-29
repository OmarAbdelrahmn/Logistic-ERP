# Ninja October performance import — applied and verified

Applied on 26 October 2026 at 21:33 Riyadh time. Independent readback completed at 21:35.

| Result | Count |
|---|---:|
| Source workbook rows, 1–24 October | 3,072 |
| Approved rows imported | 3,032 |
| Completed orders imported | 42,042 |
| Working hours imported | 34,855.86666662 |
| Rows held for review | 40 |
| Existing 25–26 October sources preserved | 183 |
| Sources/shares after import | 3,215 / 3,215 |

Every imported row was checked against its account, business date, historical assignment and actual rider. The full original workbook and the separate validated selection are retained in database artifacts with fingerprints and original row references. No account owner fallback, invented assignment, equal split or missing-as-zero conversion was used.

The user explicitly authorized the 3,032-row subset. Thirty-seven rows lack a covering assignment and three shared-day rows lack earlier-holder declarations. All 40 remain unimported; see [the complete review report](unresolved-rows-review.md).

Existing accounts, assignment periods, handover usages and the original 183 performance sources/shares were independently compared before and after and are unchanged. No payroll, payment, debt or account-suspension entries were created by the import.

The hosted schema migrations are applied: nine evaluation tables, the v1 policy and two permission definitions. Both scripts were rehearsed with rollback before application; a server COPY_ONLY checksum backup was created. The host denies RESTORE VERIFYONLY, so restore verification is not claimed.

The evaluation service was exercised read-only against hosted data: 209 account rows and 203 rider rows; the account report's 44,077 observed orders exactly equal all October source totals, including the preserved 25–26 October data. There were no database writes during this evaluation check.

## Documentation

[Performance and suggested-money API/service documentation](../../docs/ninja-evaluation/README.md) includes the endpoint reference, calculation rules, import workflow, Arabic frontend handoff, service/data model, deployment guide and TypeScript client.

The new API binary has not been published to the host. Database migration and performance import are complete; activate the new routes only after deploying that binary and enabling Features:NinjaEvaluation. All monetary results remain suggestions pending Ninja's official monthly invoice.

## Evidence

- `rehearsal.json`: staged import verified, rolled back and checked for preservation.
- `apply.json`: committed import receipt.
- `verify.json`: independent complete-row readback.
- `preservation-verification.json`: original source/share and history comparisons.
- `hosted-evaluation-verification.json`: source/report totals and complete-filter summaries.
- `unresolved-rows-review.md`: the exact 40 held source rows.
