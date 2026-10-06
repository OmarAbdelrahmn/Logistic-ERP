# Jahez response names

Successful JSON endpoints under `/api/jahez` include display data alongside their existing IDs. Original field names, financial values, pagination, and nested approval/import structures remain compatible. Downloads still return the original file.

Handovers, balances, debts, dispatches, import rows, and import account summaries have the additional fields below. Fees, ledger entries, earnings, commission policies, settlements, approval requests/decisions, and cashbox entries resolve the same data through their handover.

| Fields | Meaning |
|---|---|
| `account: { id, code, externalAccountId }` | Related Jahez account with its display code and platform number |
| `ownerRiderProfileId`, `ownerEmployeeId`, `ownerRiderNameAr`, `ownerRiderNameEn` | Account's registered owner |
| `actualRiderProfileId`, `actualEmployeeId`, `actualRiderNameAr`, `actualRiderNameEn` | Rider for this usage period; `actualRiderProfileId` matches the existing `riderProfileId` where present |
| `targetAccount: { id, code, externalAccountId }` | Approval request's target account when present |

Every returned user ID has Arabic and English name companions:

| Existing ID | Additional names |
|---|---|
| `createdByUserId` | `createdByUserNameAr`, `createdByUserNameEn` |
| `updatedByUserId` | `updatedByUserNameAr`, `updatedByUserNameEn` |
| `deletedByUserId` | `deletedByUserNameAr`, `deletedByUserNameEn` |
| `requestedByUserId` | `requestedByUserNameAr`, `requestedByUserNameEn` |
| `collectedByUserId` | `collectedByUserNameAr`, `collectedByUserNameEn` |
| `actorUserId` | `actorUserNameAr`, `actorUserNameEn` |
| `uploadedByUserId` | `uploadedByUserNameAr`, `uploadedByUserNameEn` |
| `accountantUserId` | `accountantUserNameAr`, `accountantUserNameEn` |
| `approvedByUserId` | `approvedByUserNameAr`, `approvedByUserNameEn` |

These additions apply inside paged `items`, approval `request` and `decisions`, and import preview `rows` and `accounts`, including POST responses. Cashbox handovers and import batches expose user names without identifying a single account or rider. Technical references such as source, settlement, approval, batch, and correction IDs retain their values; their containing record supplies account/rider context rather than an invented record name.

For example, each item from `GET /api/jahez/handovers` contains these identity fields in addition to the existing dates, flags, and assignment ID:

```json
{
  "id": "33333333-3333-3333-3333-333333333333",
  "accountId": "11111111-1111-1111-1111-111111111111",
  "externalAccountId": "456469",
  "riderProfileId": "22222222-2222-2222-2222-222222222222",
  "account": {
    "id": "11111111-1111-1111-1111-111111111111",
    "code": "J-456469",
    "externalAccountId": "456469"
  },
  "ownerRiderProfileId": "55555555-5555-5555-5555-555555555555",
  "ownerEmployeeId": "66666666-6666-6666-6666-666666666666",
  "ownerRiderNameAr": "صاحب الحساب",
  "ownerRiderNameEn": "Account owner",
  "actualRiderProfileId": "22222222-2222-2222-2222-222222222222",
  "actualEmployeeId": "77777777-7777-7777-7777-777777777777",
  "actualRiderNameAr": "المندوب الفعلي",
  "actualRiderNameEn": "Actual rider"
}
```

Names reflect current employee/user display data, including archived people referenced by financial history. The actual rider comes from the historical handover, never the current account assignment. Owner fields reflect the account's current registered owner because handovers do not store owner snapshots.

Unavailable names and associations return `null`. Owners without a rider profile still have their employee ID and names. Unresolved import rows and multi-rider account summaries leave actual-rider fields null. Name mapping runs after existing authorization, uses batched lookups, and applies to idempotent replays without changing stored financial receipts.
