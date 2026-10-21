# Edit a Chefz assignment start date

**21 October 2026:** Freelancer assignments may share a boundary day when their usage order and report figures can be resolved. FullTime assignments remain exclusive by day. Date corrections also protect saved usage/allocation evidence. See [shared-day handovers](shared-account-day-handovers.md).

`PUT /api/chefz/assignments/{assignmentId}/start-date`

Permission: `chefz.accounts.update`, scoped to the Chefz platform. Supported platform codes: `CHEFZ`, `THECHEFZ`, `CHEFSZ`, and `SHIFTZ`.

Use the assignment ID and its `rowVersion` from `GET /api/platform-accounts/{accountId}/assignment-history`. This lets the caller select the current assignment or a historical assignment explicitly. The token belongs to the assignment, not the account or Chefz account details.

Request:

```json
{
  "effectiveFrom": "2026-10-18",
  "rowVersion": "<assignment rowVersion>"
}
```

`effectiveFrom` is the required start date in `YYYY-MM-DD` format. Dates retain the existing business calendar semantics.

Success: HTTP 200 with:

```json
{
  "assignmentId": "<assignment GUID>",
  "accountId": "<account GUID>",
  "riderProfileId": "<rider GUID>",
  "effectiveFrom": "2026-10-18",
  "effectiveTo": null,
  "status": "Active",
  "rowVersion": "<updated assignment rowVersion>"
}
```

Only `EffectiveFrom` changes on the assignment, along with the standard update audit metadata and SQL Server rowversion. End date, status, assigned rider, payment model, account slot, reasons, and account data stay as they are. Each date change records an assignment history event with the previous and new start dates. An unchanged date returns the current result without adding another event.

Validation and errors:

- HTTP 400 / `Chefz.Invalid`: missing/default start date, or a start date after the assignment's end date.
- HTTP 403 / `Chefz.Forbidden`: missing Chefz update permission.
- HTTP 404 / `Chefz.NotFound`: unknown assignment or assignment belonging to a different platform.
- HTTP 409 / `Chefz.Conflict`: missing, invalid, or stale assignment rowversion; cancelled assignment; overlap with another non-cancelled assignment for the same account; or a date that excludes saved daily performance or charged settlement days.

Assignment ranges include both start and end dates. A previous assignment ending on October 15 prevents the next assignment from starting on October 15. Another Chefz account held by the same rider is unaffected.

Saved settlement snapshots are not recalculated. Future previews and imports use the corrected assignment dates. No database migration is required; deploy the updated API to expose the endpoint.
