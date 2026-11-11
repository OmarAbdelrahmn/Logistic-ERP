# Delete Ninja performance for a day

`DELETE /api/ninja/performance`

Requires `ninja.performance.delete` in the Ninja client-platform scope. Import and assignment-date permissions do not authorize deletion. This sensitive, high-trust permission is seeded for the system administrator; custom roles require an explicit grant.

| Query parameter | Required | Meaning |
| --- | --- | --- |
| `reportDate` | Yes | One report date, `YYYY-MM-DD`, no later than today in Riyadh. |
| `reason` | Yes | Nonblank correction reason, at most 1,000 characters. URL-encode it. |
| `sponsorId` | No | Accounts currently linked to this sponsor through `PlatformRiderAccount.SponsorId`. Omitting it selects all permitted Ninja accounts, including accounts without a sponsor. |

```http
DELETE /api/ninja/performance?reportDate=2026-11-09&reason=Wrong%20assignment
DELETE /api/ninja/performance?reportDate=2026-11-09&sponsorId=019c18d5-62e1-7000-8000-000000000040&reason=Wrong%20assignment
```

Successful response (`200 OK`):

```json
{
  "reportDate": "2026-11-09",
  "sponsorId": null,
  "performanceRowsDeleted": 85,
  "sourcesDeleted": 80,
  "provenanceRowsDeleted": 85
}
```

The selected performance sources, rider allocations, and allocation provenance are deleted with an audit entry in one transaction. Allocation counts can exceed source counts when riders share an account/day. Archived and soft-deleted accounts are included. Repeating a deletion with no remaining matching data returns zero counts.

Other dates, other platforms, and (when filtered) other sponsors remain intact. Deductions, handover figures, reviewed responsibility, evaluation context, and saved evaluation snapshots are not deleted. These can independently block assignment correction and require their own correction workflows.

Original import metadata and uploaded workbook artifacts remain as historical evidence. Live reports and attendance use the remaining performance, so deletion can change their results. Saved snapshots remain historical; create a new evaluation after correction and reimport.

## Correction flow

1. Show the selected date and whether deletion covers all Ninja accounts or one sponsor; collect a reason.
2. Call the endpoint once for each blocking date.
3. Correct the assignment periods and resolve any other reported blockers.
4. Reimport performance with the corrected assignment coverage. The same workbook restores deleted account/days and skips surviving identical records. Conflicting surviving performance still rejects the import.
5. Refresh performance, attendance, and evaluation. With preview/confirm imports, create a new preview; an already confirmed preview returns its original confirmation rather than restoring deleted data.

Deletion and assignment correction are separate operations. Until reimport succeeds, deleted performance is absent from live reporting. An empty or unknown sponsor is rejected rather than treated as an unfiltered request.

## Deployment

The hosted database permission migrations were [applied and independently verified on 10 November 2026](ninja-performance-deletion-hosted-deployment-20261110.md). API publication is separate.

Apply the new Application and Identity permission migrations with the normal deployment workflow. Adding this code does not change the live database or delete any live performance.
