# Frontend handoff: account handover and monthly usage

**Status, 29 October 2026:** The hosted database migration has been applied and verified. The matching API and frontend still need to be deployed. Do not assume the hosted API already exposes the contracts below.

## What changes in the UI

1. Remove day-split fields from the account return/withdrawal form. The existing return APIs continue accepting the old optional `dayUsage` for compatibility.
2. On the account assignment form, after selecting an account, the incoming rider, and the effective date, fetch the assignment context. Fetch it again whenever any of these inputs changes.
3. Display prior riders and their saved figures. If `requiresPreviousUsage` is true, require the missing `OrdersCount` values before submission. A confirmed zero is valid; blank and null mean unknown.
4. Keep the other existing figures optional. Preserve every previously returned figure when completing a missing one. Do not treat a missing optional field as zero.
5. Include new or completed prior figures in `previousDayUsages` on the assignment request itself. If all required figures are already saved, omit the array. Do not save the figures in a separate request before assignment.
6. Show `monthlyUsageStatus` and `usageMonth` on account lists and details. Keep the existing operational `status` separately.

These rules apply to generic accounts, Ninja, Jahez, and Chefz. Chefz FullTime retains its existing same-day exclusivity rule.

## Read assignment context

`GET /api/platform-accounts/{accountId}/assignment-context?date=2026-10-29&riderProfileId={incomingRiderId}`

The date is the effective Riyadh business date. The call is read-only, requires authentication and the existing platform-scoped read permission. Example response:

```json
{
  "accountId": "11111111-1111-1111-1111-111111111111",
  "businessDate": "2026-10-29",
  "riderProfileId": "22222222-2222-2222-2222-222222222222",
  "requiresPreviousUsage": true,
  "contributions": [
    {
      "assignmentIds": ["33333333-3333-3333-3333-333333333333"],
      "riderProfileId": "44444444-4444-4444-4444-444444444444",
      "riderNameAr": "المندوب السابق",
      "figures": { "ordersCount": null, "workingHours": 3, "collectedAmount": null, "deductionsAmount": null, "totalValidOffers": null, "declinedOffers": null, "missedOffers": null },
      "rowVersion": "AAAAAAAAB9E=",
      "usageRowVersions": { "33333333-3333-3333-3333-333333333333": "AAAAAAAAB9E=" },
      "missingRequiredFields": ["OrdersCount"],
      "missingOptionalFields": ["CollectedAmount", "DeductionsAmount"]
    }
  ]
}
```

The missing-field names are PascalCase; figure properties are camelCase. Optional fields differ by platform: Ninja uses working hours, collected amount and deductions; Chefz uses total valid, declined and missed offers. Jahez and generic accounts use orders. `assignmentIds` may contain several IDs for consecutive periods of the same rider. Preserve their order and send the entire array and all tokens exactly as returned.

`requiresPreviousUsage` indicates that required data is missing, not that an earlier rider necessarily exists. A nonempty contribution with a saved order count can require no further entry. If the last rider and incoming rider are the same, no new split is required for their consecutive periods. For `A → A → B`, capture A's combined figures; for `A → B → A`, the separate A and B contributions remain separate. Ambiguous historical order returns a conflict and must be resolved through the existing day-usage correction workflow.

## Submit with the assignment

Add `previousDayUsages` to the existing request for:

| Flow | Endpoint |
| --- | --- |
| Generic and Chefz | `POST /api/platform-accounts/{id}/assign` |
| Legacy platform operations | `POST /api/platform-operations/assignments` |
| Ninja | `POST /api/ninja/accounts/{accountId}/assign` |
| Jahez | `POST /api/jahez/v2/handovers` |
| Jahez FreeSwitch approval | `POST /api/jahez/v2/requests/{id}/decision` |

Jahez commands continue to require the existing `Idempotency-Key` header. Supply a new key for a new logical request and follow the existing retry rules.

Example Ninja request, using the context above:

```json
{
  "actualRiderProfileId": "22222222-2222-2222-2222-222222222222",
  "effectiveFrom": "2026-10-29",
  "phoneNumber": "0501234567",
  "reason": "تسليم الحساب",
  "wasBackdated": false,
  "riderType": "InternalCar",
  "paymentModel": "PayPerOrder",
  "previousDayUsages": [
    {
      "assignmentIds": ["33333333-3333-3333-3333-333333333333"],
      "figures": { "ordersCount": 0, "workingHours": 3 },
      "rowVersion": "AAAAAAAAB9E=",
      "usageRowVersions": { "33333333-3333-3333-3333-333333333333": "AAAAAAAAB9E=" }
    }
  ]
}
```

`figures` replaces the supplied contribution's full figure object. Copy the current object before editing it; omitting an existing non-null field would attempt to clear it and can be rejected. For Jahez, use `accountId`, `riderProfileId`, `effectiveAtUtc` and `reason` in its normal handover body, with the same `previousDayUsages` array. For FreeSwitch, put that array on the approval decision body for the target account and effective business date.

The server rechecks the history and tokens and saves the contribution and assignment together. On a missing order count, changed membership, stale token, frozen import, or other conflict, display the returned Problem Details, reload the context, and ask the operator to review before retrying. Do not silently resubmit stale numbers. Existing saved values are corrected through day usage rather than overwritten through assignment.

## Correct day figures

`GET /api/platform-accounts/{accountId}/day-usage?date=YYYY-MM-DD` returns the existing `assignments` plus `contributions`. For `PUT` to the same URL, send all eligible assignments once in handover order, their usage rowversions, a `reason`, and the contributions being corrected. Set each unchanged assignment's `figures` to null. For a grouped contribution, send the complete group membership, figures and tokens from the latest GET. Saved imported figures and order are protected; use the existing import correction workflow where applicable.

## Monthly account usage

Account responses, including account objects nested in supported comparison and detail responses, add:

```json
{ "monthlyUsageStatus": "Used", "usageMonth": "2026-10" }
```

Use the exact values `Used` and `Unused`. The month is the current Gregorian month in Riyadh, formatted `YYYY-MM`. An actual non-cancelled assignment overlapping the month through now counts as Used, including an assignment that began in a previous month or ended earlier this month. Planned or future assignments do not count. A Jahez session ending exactly at the month's start does not count in the new month. This is calculated on read; no frontend reset or scheduler is needed. It is independent of the account's operational availability and actual order count.

## Frontend acceptance scenarios

- Return an account without entering split figures.
- Assign to another rider on the same date: show the previous rider and prevent submission until orders are filled; accept zero.
- Return and reassign to the same rider that day without requesting a new split; then assign to a different rider and enter the combined total.
- Assign on the following date without requesting the prior day's figures.
- Preserve saved optional values, allow missing optional values, and handle later import errors that identify missing metrics.
- Reload context after 409 and preserve the operator's unsaved input for review.
- Display Used through the month after a return, and show the new month's correct state after a refresh.
- Verify the existing Chefz FullTime rule and Jahez idempotency behavior.

The shared TypeScript types are in [shared-account-day-usage-contracts.ts](shared-account-day-usage-contracts.ts). Detailed Arabic background is in [assignment-day-contributions-20261029.md](assignment-day-contributions-20261029.md).
