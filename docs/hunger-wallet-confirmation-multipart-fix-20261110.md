# Hunger wallet confirmation: missing day-usage version tokens

## Findings

The reported wallet reviews contain a previous-rider group version and `ordersCount: 50`, but their persisted `assignment.previousDayUsages[].usageRowVersions` dictionary is null. The selected contribution has an existing daily-usage marker. `PrepareAssignment` requires both the group token and per-assignment usage tokens; a missing dictionary rejects confirmation with a concurrency conflict.

HTTP regression tests reproduced the loss with both dotted and bracketed multipart dictionary field names. The nested `IReadOnlyDictionary<Guid,string?>` on the record was not populated by MVC model binding. This occurs with and without an attached image.

The deployed frontend's stale-review classifier returns true for any HTTP 409. It therefore replaces an assignment/day-usage conflict with an expiry/supersession message. The two supplied review IDs are superseded now, but that later status is not proof of their status at the original failed confirmation. Repeatedly creating another review does not repair missing version tokens.

## Backend fix

`HungerWalletReviewForm.ToRequest` now normalizes explicitly supplied multipart usage-version fields before calling the service. It supports the deployed dotted syntax and canonical bracket syntax. It preserves version strings exactly and maps explicitly empty tokens to null for usage rows that did not exist when context was read. Conflicting values, duplicate values, malformed keys, or assignment IDs outside the selected group are rejected with 400.

No tokens are fetched from the database or invented to make a request succeed. Existing concurrency checks remain active. Missing tokens still fail confirmation. No live financial confirmation was executed during diagnosis, and no database migration is required for this fix.

Publish the updated API and create a fresh review. Already stored reviews with missing token dictionaries must not be reused.

## Frontend changes

Read a fresh assignment context and retain its complete `usageRowVersions` map. Prefer these form names:

```text
assignment.previousDayUsages[0].assignmentIds[0] = <assignment-guid>
assignment.previousDayUsages[0].rowVersion = <group-version>
assignment.previousDayUsages[0].figures.ordersCount = 50
assignment.previousDayUsages[0].usageRowVersions[<assignment-guid>] = <usage-version>
```

Include an entry for every assignment in the group, including explicit empty values for null tokens. Do not use a truthiness check that omits null entries. Confirm the newest successful `reviewId`; prevent overlapping review requests from overwriting newer state with an older response.

Remove the blanket `status === 409` stale-review classification. Display the server's actual ProblemDetails for assignment, day-usage, and other business conflicts. Reserve the expiry/replacement message for an explicit expiry/replacement response.

## Hours investigation

Read-only hosted checks found 407 active daily-operation records with saved hours, including 37 with unresolved rider allocation. Among resolved records, sums of rider working-hour allocations match source hours within 0.000001. This verifies internal consistency, not agreement with the original Excel cells. The exact reported uploaded workbook is still needed to establish a parsing or display discrepancy.

Explicit decimal-hour, duration, and fraction-of-day tests pass. Do not change saved hours or multiply fractional values by 24 without checking the source cell type, number format, and declared unit.

The user subsequently supplied the workbook. Its 103 rows parse without any hours discrepancy; matching hosted previews were never confirmed. See the [completed workbook diagnosis](hunger-hours-workbook-diagnosis-20261110.md).
