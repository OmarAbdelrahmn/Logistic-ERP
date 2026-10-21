# Jahez V2 request read responses

`GET /api/jahez/v2/requests?page=1&pageSize=50` returns `Page<ApprovalRequestRow>`.
`GET /api/jahez/v2/requests/{requestId}` returns `ApprovalDetails` with `{ request, decisions }`.

Both reads require `jahez.read` within the Jahez client-platform scope. All existing request fields and audit fields remain at their original paths. Request creation and decision submission retain their existing response contracts.

21 October 2026: create/decision responses now also return `amounts`, and each enriched read request includes the same field: `{ dashboardDebt, commission, total }`. `dashboardDebt` is the manually entered signed operation `net`; `total` is `net + commission`. Requests without a linked operation return `amounts=null`. Transfer approval no longer blocks rider operations. See [rider debt list and full repayment contract](jahez-transferred-rider-debts-20261021.md).

| Added field on each request | Contents |
|---|---|
| `userRider` | Actual rider recorded on the request's original handover: employee ID, rider ID, Arabic/English names, iqama, mobile and sponsor ID. |
| `assignment` | Original assignment/handover/account/rider IDs, start and end instants, effective dates, status and reasons, commission start date, commission posted-through date and last settlement payment time. |
| `account` | Original account ID, code, external account number, username, status/reason, operating city and sponsor names/IDs. |
| `targetAccount` | The same account summary for the requested replacement account; usually `null` outside account-switch requests. |
| `operation` | Complete linked `Operation`, including current status, payment status, net, commission, remaining amount, calculation evidence, fund, payment reference, execution details and audit/rowversion fields. `null` for requests without a linked settlement. |

The detail response retains the chronological `decisions` array, including actor IDs, decision status, reason, time and saved `decisionJson`. Pending requests return an empty array. Root-level `proposedNet` and `proposedCommission` describe the proposal; `operation` describes the linked operation's current values, and decision evidence records the decision values.

Rider iqama is `userRider.iqamaNo`, belonging to the recorded user rider. The original handover identifies the rider and assignment even after account reassignment. Archived references remain readable as history. Missing related records return `null` without removing the request from the response. Deleted requests remain excluded; a missing/deleted request detail returns 404. Pagination and all request statuses are preserved.

Abbreviated detail example (illustrative data; unchanged scalar/audit fields and some operation fields omitted):

```json
{
  "request": {
    "id": "9f37ed84-b2ae-4a69-a276-90ef36e9e001",
    "handoverId": "18c22d03-45ae-44d1-b705-d343e8fc7001",
    "kind": 5,
    "status": 1,
    "operationId": "5417f643-ab7e-42f0-ae22-dbeef1aa1001",
    "targetAccountId": null,
    "reason": "طلب تصحيح التصفية",
    "adjustmentTarget": 1,
    "proposedNet": -120,
    "proposedCommission": 50,
    "userRider": {
      "employeeId": "78d41c92-f54a-4029-a3aa-276384fd4001",
      "riderProfileId": "37e251a9-eaaa-4221-b6b9-239ce3572001",
      "nameAr": "المندوب التجريبي",
      "nameEn": "Example Rider",
      "iqamaNo": "2000000001",
      "mobile": "0500000001",
      "sponsorId": "45db2217-ad01-46ae-ab84-a980ceab5001"
    },
    "assignment": {
      "assignmentId": "0dd19182-d16c-4353-b6ab-f221a7eb6001",
      "handoverId": "18c22d03-45ae-44d1-b705-d343e8fc7001",
      "accountId": "a7015712-1862-4e81-aa14-fca795bd8001",
      "riderProfileId": "37e251a9-eaaa-4221-b6b9-239ce3572001",
      "startedAtUtc": "2026-10-01T05:00:00Z",
      "endedAtUtc": null,
      "effectiveFrom": "2026-10-01",
      "effectiveTo": null,
      "status": "Active",
      "startReason": "تسليم الحساب للمندوب",
      "endReason": null,
      "commissionStartsOn": "2026-10-02",
      "commissionPostedThrough": null,
      "lastSettlementPaymentAtUtc": null
    },
    "account": {
      "accountId": "a7015712-1862-4e81-aa14-fca795bd8001",
      "code": "J-476835",
      "externalAccountId": "476835",
      "userName": "example-account",
      "status": "Assigned",
      "statusReason": null,
      "operatingCityId": "45db2217-ad01-46ae-ab84-a980ceab6001",
      "cityNameAr": "الرياض",
      "sponsorId": "45db2217-ad01-46ae-ab84-a980ceab5001",
      "sponsorNameAr": "الكفيل التجريبي"
    },
    "targetAccount": null,
    "operation": {
      "id": "5417f643-ab7e-42f0-ae22-dbeef1aa1001",
      "handoverId": "18c22d03-45ae-44d1-b705-d343e8fc7001",
      "mode": 1,
      "status": 2,
      "paymentStatus": 1,
      "throughDate": "2026-10-06",
      "net": 100,
      "commission": 75,
      "calculatedCommission": 75,
      "remaining": 175,
      "rowVersion": "AAAAAAAAB9E="
    }
  },
  "decisions": []
}
```

The list endpoint places the same enriched request object directly under `items`, with the existing `page` and `pageSize` fields. Decision history is returned by the detail endpoint.

TypeScript contracts and the typed client are maintained in `jahez-v2-api-contracts.ts` and `jahez-v2-client.ts`.
