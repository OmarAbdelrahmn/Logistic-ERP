# Ninja assignment rider types and payment models

Source change: 24 October 2026. The application database migration was [applied and verified on the hosted database on 25 October 2026](ninja-rider-type-hosted-deployment-20261025.md). API publication and frontend deployment are still required before this appears in the live assignment form.

Add a required single-select field named **Rider type / نوع المندوب** to the Ninja account assignment form. Start with no selection and require the operator to choose one option before submission.

| API value | English label | Arabic label |
| --- | --- | --- |
| `InternalCar` | Internal car | سيارة داخلي |
| `ExternalCar` | External car | سيارة خارجي |
| `InternalMotorcycle` | Internal motorcycle | دراجة نارية داخلي |
| `ExternalMotorcycle` | External motorcycle | دراجة نارية خارجي |

Also add a required **Payment model / طريقة الدفع** single-select with no preselected value:

| API value | Arabic label | English label |
| --- | --- | --- |
| `PayPerOrder` | عمولة | Commission |
| `Salary` | بالراتب | Salary |

Send both selected values as `riderType` and `paymentModel` in `POST /api/ninja/accounts/{accountId}/assign` alongside the existing rider, date, phone and optional password/reason fields:

```json
{
  "actualRiderProfileId": "00000000-0000-0000-0000-000000000003",
  "effectiveFrom": "2026-10-24",
  "phoneNumber": "0503333333",
  "riderType": "ExternalMotorcycle",
  "paymentModel": "Salary"
}
```

The values are case-sensitive. Missing, blank, numeric or unrecognized values return HTTP 400 with code `ninja.invalid` before any assignment, phone or password is saved. A recognized payment model disabled in the platform settings returns HTTP 409 / `platform.payment_model_not_supported`. Older clients that omit either field must be updated with the API rollout.

The selections belong to the assignment period. Assignment/return responses expose `assignment.riderType` and `assignment.paymentModel`; the existing platform account assignment-history responses also expose both fields. Ninja account list/detail/status responses expose `riderType` and `paymentModel` at the account root. Show both on the Ninja accounts page. `riderType` identifies internal/external and car/motorcycle together. Refresh account detail after assignment or return using the existing client flow.

`paymentModel` overrides the previous account default for this new assignment. The existing rider account-count and salary-account limits are evaluated using the selected model, including the requested model in conflict details. On success the account and assignment payment models are saved together with the credentials and lifecycle changes. Generic metadata edits cannot rewrite the payment model of an active Ninja assignment. Return the account and assign it again to change the model, preserving the previous period for future calculations. This change uses the existing `PaymentModel` database columns and does not add commission rates, salary amounts, or new financial formulas.

Returning an account retains both choices on the ended assignment. A subsequent assignment requires fresh choices and stores them separately. Unassigned accounts and legacy assignments without a recorded rider type return null for `riderType`; display an empty/unknown rider type for legacy data. While unassigned, `paymentModel` returns the account's saved model; while assigned, it returns the assignment's saved model. No rider type is inferred or backfilled from employees or vehicle links. This choice does not create a fleet vehicle assignment or change employee relationship/work-type records.

Apply the `20261024150306_AddNinjaAssignmentRiderType` application migration before deploying the API. The [idempotent SQL script](../database/scripts/ninja-assignment-rider-types.sql) applies only this migration and assumes the existing application migration history and preceding schema are present. It adds a nullable `NinjaRiderType` column and a four-value check constraint to `app.RiderClientAssignments`; existing rows remain null. No Identity migration or new permission is needed. The existing Ninja assignment permissions still apply. The change affects Ninja assignment input only; other platforms do not require the field.

Frontend code is maintained outside this backend workspace. The shared [TypeScript contracts](ninja-api-contracts.ts) include both required request fields and the account/assignment response fields. No additional migration is needed for the payment model extension beyond the rider type migration above.

Verification: the solution builds with zero warnings/errors. All 20 payment-model checks pass, covering every rider-type/payment-model combination, invalid or disabled choices without partial writes, salary/account limits, metadata guards, historical preservation and assignment-snapshot reads. The 12 rider-type service checks also pass. Four additional rider-type SQL Server cases are available with `NINJA_SQL_TEST_SERVER`; they could not execute here because LocalDB failed to start. These cases also check the database constraint against an invalid value. The broader Ninja/shared assignment selection reports 88 passed, 9 optional cases skipped, and one previously documented debt-block failure (`StatusValidationPermissionsConcurrencyAndDebtBlockPreventMutations`; see [existing verification notes](shared-account-day-verification.md)); the final assignment-snapshot read check was then added and passed in the 20-check focused payment suite. EF reports no pending application model changes. The migration and SQL script were subsequently applied and verified on the hosted database; all 860 existing assignment records were preserved. See the [hosted deployment evidence](ninja-rider-type-hosted-deployment-20261025.md).
