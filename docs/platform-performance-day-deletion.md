# Delete daily performance by platform

All routes accept the same query parameters:

- `reportDate`: required, one `YYYY-MM-DD` date no later than today in Riyadh.
- `reason`: required, nonblank, at most 1,000 characters; URL-encode it.
- `sponsorId`: optional; filters by the account's current `SponsorId`, not its rider's sponsor. Omit for all accounts, including accounts without a sponsor. Empty or malformed values are rejected.

| Platform | DELETE route | Dedicated client-platform permission |
| --- | --- | --- |
| Ninja | `/api/ninja/performance` | `ninja.performance.delete` |
| Hunger | `/api/hunger/performance` | `hunger.performance.delete` |
| Chefz / Shiftz | `/api/chefz/performance` | `chefz.performance.delete` |
| Jahez daily orders | `/api/jahez/v2/performance` | `jahez.performance.delete` |

```http
DELETE /api/hunger/performance?reportDate=2026-11-09&reason=Wrong%20assignment
DELETE /api/hunger/performance?reportDate=2026-11-09&sponsorId=019c18d5-62e1-7000-8000-000000000040&reason=Wrong%20assignment
```

Each operation targets one date in its own platform and returns `200 OK` with `reportDate`, `sponsorId`, and `performanceRowsDeleted`. Repeating the call returns zero matching rows. Other dates, sponsors outside the filter, financial postings, and account/assignment records remain unchanged. Available original import evidence and audit records are retained. The new Hunger and Jahez permissions are sensitive, high-trust, and granted by default only to the system administrator; other roles require explicit grants.

## Platform behavior

**Ninja:** deletes daily performance sources, rider allocations, and provenance. Response also includes `sourcesDeleted` and `provenanceRowsDeleted`. Original import metadata and workbook artifacts remain. Deductions and reviewed responsibility are separate and may still block date corrections. See [Ninja details](ninja-performance-day-deletion.md).

**Hunger:** soft-deletes daily operations, their embedded allocations/manual comparisons, and unresolved potential fines. Response includes `potentialFinesDeleted`. Original rows remain archived with deletion metadata and are excluded from live reads. If a related fine was reviewed, waived, or invoiced, the entire deletion is rejected with 409 until the financial review is corrected. Cumulative quality snapshots, quality revisions, invoices, and wallet records are not removed by this daily-operations endpoint.

**Chefz / Shiftz:** deletes daily performance sources and rider allocations, retaining import metadata even when a batch covers several sponsors. Response retains `sourcesDeleted` and `importsDeleted` (now zero) for compatibility, and adds `sponsorId`. The HTTP endpoint now requires a reason. Wallet settlements and imported settlement order rows are unaffected.

**Jahez:** deletes daily dispatch/order rows by account session, preserving originals in the deletion audit and retaining uploaded files, parsed source rows, batches, and command receipts. This does not delete transactions, earnings statements, fees, commission postings, or treasury entries. Archived accounts/sessions are included within the authorized platform.

## Correction and recovery

1. Preview the selected date and sponsor/all-account scope in the UI and collect a reason.
2. Delete only the blocking date(s).
3. Correct the assignment/handover and any independent blockers.
4. Reimport and refresh performance/evaluation. Deleted data is absent from live reports until recovery succeeds.

For Hunger, create a **new preview** from the same file and confirm it. An old confirmed preview returns its original receipt. Surviving rows are skipped and deleted rows are rebuilt using current assignments.

For Chefz and Ninja, reupload the same file; missing account/days are rebuilt and surviving identical data is skipped.

For Jahez, upload/preview the same file to retrieve its batch, then call `POST /api/jahez/v2/imports/{batchId}/commit` with a **fresh `Idempotency-Key`**. A committed daily batch can restore missing source rows using corrected eligible sessions; surviving rows retain their attribution. Resolve any allocation/overlap issues first. A superseded batch cannot restore missing rows: use the current replacement batch. Replaying an old command key returns its original receipt and does not perform recovery.

Apply the Application/Identity permission migrations before enabling Hunger or Jahez deletion. The Identity migration also adds the previously missing system administrator grant for Chefz deletion. API publication is separate from updating the database.

The hosted database updates were [applied and independently verified on 10 November 2026](platform-performance-deletion-hosted-deployment-20261110.md). API publication remains separate.
