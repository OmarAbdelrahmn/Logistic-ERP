# HungerStation wallet evidence image — frontend handoff

Date: 8 November 2026.

## Deployment status

Backend source is implemented and tested locally. This task did not deploy API binaries or frontend files. Coordinate API deployment before switching the frontend to multipart requests: the previous API expects JSON.

The hosted database was checked on 8 November 2026 using a read-only connection to the configured `db67927` database. No schema or data modification was required or performed for this feature. Existing `hunger.WalletReviews.RequestJson` stores the image link in the review payload; `hunger.FinancialEvents.EvidenceReference` stores it upon confirmation. No migration, permission grant, or database image column is needed.

Verified hosted migration heads:

- Application: `20261106143740_HungerSourceCaptainClassification`.
- Identity: `20261106094011_GrantFleetVehicleIdentityFieldPermissions`.

Verified schema: `WalletReviews.RequestJson` is `nvarchar(max)`; `ResultJson` is nullable `nvarchar(max)`; `Status` is `nvarchar(20)`; `FinancialEvents.EvidenceReference` is non-null `nvarchar(1000)`. An omitted image uses an empty evidence string in the existing database column and returns `walletEvidence: null` in the review response.

## Review request

`POST /api/hunger/accounts/{accountId}/wallet-review`

Permission: `hunger.finance.wallet.review`, with existing platform scope and feature checks.

The request now uses **multipart/form-data**, replacing JSON at this endpoint. Use `FormData`; do not manually set `Content-Type`, since the browser supplies the multipart boundary. Keep the existing authentication mechanism.

| Field | Type | Notes |
|---|---|---|
| `action` | string | `Deliver` or `Return` |
| `riderProfileId` | GUID | Actual rider |
| `supervisorEmployeeId` | GUID | Supervisor employee |
| `businessDate` | date | `yyyy-MM-dd` |
| `mobileBalance` | decimal | Current observed balance; use a decimal point, e.g. `0.00` |
| `closingBalance` | nullable decimal | Send `0` for a confirmed first-period account; omit when unknown |
| `provenAdjustments` | decimal | Send `0` when there are no adjustments |
| `walletEvidence` | optional file | Omit the part when no image is selected |
| `closingEvidence` | optional string | Existing closing evidence rules remain in effect |
| `adjustmentEvidence` | optional string | Existing adjustment evidence rules remain in effect |

Do not append `null`, `undefined`, or a text reference as the `walletEvidence` part. It accepts a file only. Do not serialize nested assignment/return objects as JSON strings or append `[object Object]`.

For Deliver, send nested form fields:

- `assignment.actualRiderProfileId`
- `assignment.effectiveFrom`
- `assignment.reason`, when supplied
- `assignment.wasBackdated` (`true` or `false`)
- `assignment.backdatedReason`, when supplied
- `assignment.compensationModel`, when supplied
- Existing previous-day usage collections, when needed, using indexed dotted keys such as `assignment.previousDayUsages[0].<property>`.

For Return, omit assignment and send:

- `return.effectiveTo`
- `return.status` = `Ended`
- `return.reason`
- `return.rowVersion`
- Existing day-usage properties, when needed, as `return.dayUsage.<property>`.

## Delivery example

```ts
type WalletDelivery = {
  accountId: string;
  riderProfileId: string;
  supervisorEmployeeId: string;
  businessDate: string;
  mobileBalance: number;
  closingBalance: number | null;
  provenAdjustments: number;
  closingEvidence?: string | null;
  adjustmentEvidence?: string | null;
  walletEvidence?: File | null;
  assignment: {
    effectiveFrom: string;
    reason?: string | null;
    wasBackdated: boolean;
    backdatedReason?: string | null;
    compensationModel?: string | null;
  };
};

async function reviewWalletDelivery(
  apiOrigin: string,
  accessToken: string,
  input: WalletDelivery,
) {
  const form = new FormData();
  const append = (key: string, value: string | number | boolean | null | undefined) => {
    if (value !== null && value !== undefined) form.append(key, String(value));
  };
  append('action', 'Deliver');
  append('riderProfileId', input.riderProfileId);
  append('supervisorEmployeeId', input.supervisorEmployeeId);
  append('businessDate', input.businessDate);
  append('mobileBalance', input.mobileBalance);
  append('closingBalance', input.closingBalance);
  append('provenAdjustments', input.provenAdjustments);
  append('closingEvidence', input.closingEvidence);
  append('adjustmentEvidence', input.adjustmentEvidence);
  append('assignment.actualRiderProfileId', input.riderProfileId);
  append('assignment.effectiveFrom', input.assignment.effectiveFrom);
  append('assignment.reason', input.assignment.reason);
  append('assignment.wasBackdated', input.assignment.wasBackdated);
  append('assignment.backdatedReason', input.assignment.backdatedReason);
  append('assignment.compensationModel', input.assignment.compensationModel);
  if (input.walletEvidence) form.append('walletEvidence', input.walletEvidence);

  const response = await fetch(
    new URL(`/api/hunger/accounts/${input.accountId}/wallet-review`, apiOrigin),
    { method: 'POST', headers: { Authorization: `Bearer ${accessToken}` }, body: form },
  );
  const body = await response.json();
  if (!response.ok) {
    // Pass the complete ProblemDetails to the existing UI error handler.
    // Preserve errorCode, field, assignmentLimit and correlationId.
    throw body;
  }
  return body;
}
```

This example covers a delivery without previous-day usage input. Preserve the existing backdated usage workflow when that input is needed.

## Image field and response

Replace the required text box `مرجع إثبات المحفظة *` with an optional file input labeled **صورة إثبات رصيد المحفظة (اختياري)**. Accept JPEG (`.jpg`/`.jpeg`), PNG, WebP, GIF and BMP (`.bmp`/`.dib`), up to 5 MiB (5,242,880 bytes). Provide selection, preview, replacement and removal before submission. Removing the selection omits the upload part. Client validation is for usability; the server validates extension, declared type and content signature. SVG, PDF, empty files and mismatched file content are rejected.

The total request limit is 6 MiB. Keep scalar/nested input within that limit. A newly selected image replaces only the pending form selection; there is no API for deleting evidence from previously saved reviews.

The review response retains its existing fields and adds:

```json
{
  "walletEvidence": "/private/hunger/wallets/ACCOUNT_GUID/GENERATED_NAME.png"
}
```

Without an image, this field is `null`. Resolve returned links against the API origin, for example `new URL(review.walletEvidence, apiOrigin).href`. The server generates filenames and stores images under `wwwroot/private/hunger/wallets/{accountId}/`. Use the response link when displaying the saved evidence. Confirmation copies that link into the financial event's `evidenceReference`; wallet observation/report evidence then retains it. Existing historical text references can still appear in reporting; render those as text, not as image URLs.

## Confirmation and error handling

`POST /api/hunger/wallet-reviews/{reviewId}/confirm` remains unchanged, with no body and no file upload. Confirm only a successful review with `canConfirm: true`. Show success only after confirmation succeeds. Preserve form values on failure.

- `files.invalid_file`: show an Arabic file validation error and allow replacement. Oversized total requests may return HTTP 413 before a structured application error; handle that separately.
- Closing/adjustment conflict: a confirmed first-period account must send `closingBalance=0`, `provenAdjustments=0`. Do not infer a first period from a zero current balance and do not globally convert unknown closing balances to zero. Accounts with prior activity retain the existing settlement requirements.
- `platform.rider_account_limit_reached`: read `assignmentLimit.maximumActiveAccounts` and `activeAccounts`; list the existing platform/account assignments and explain the rider has reached the allowed limit. Do not show only the generic error detail. Do not release assignments, increase limits, change payment models, or retry automatically. When `allowedPaymentModels` is empty, changing the payment model is not an available remedy. After the assignment conflict is resolved, request a fresh review.
- Superseded, expired or changed-data review: create a fresh review before confirming. Reattach the selected file if a new review is submitted. Already saved reviews retain their images for history.

The rider-swap review endpoint continues to accept its existing JSON body. This multipart change applies to the standalone wallet-review endpoint only.

## Verification completed

- Solution build passed with zero warnings and errors.
- Finance/storage checks: 59 passed; 3 SQL integration tests skipped because SQL opt-in was absent.
- Multipart HTTP checks: 2 passed, covering nested assignment binding with and without an image.
- Real filesystem tests verify image bytes and link persistence through confirmation, optional omission, and spoofed-image rejection without a saved review or file.
- Hosted database schema and migration heads verified read-only. API deployment and hosted upload/static-image retrieval were not performed or verified.
