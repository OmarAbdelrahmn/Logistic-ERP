# Frontend handoff: Keeta sponsor vehicle leases

## Purpose

This feature models a vehicle rental agreement between two sponsors for **Keeta only**:

```text
Lessor sponsor (original vehicle owner)
  └─ selected vehicles
       └─ active Keeta lease agreement
            └─ Lessee sponsor (temporary operating sponsor)
```

While the agreement is effective, each selected vehicle can be assigned to:

1. Keeta accounts registered under its original/lessor sponsor; and
2. Keeta accounts registered under the lessee sponsor.

The vehicle's `SponsorId` is never changed. The agreement is an additional, time-bounded permission used only by vehicle-to-platform-account assignments.

## API basics

- Base path: `/api`
- Send `Authorization: Bearer <accessToken>`.
- All IDs are GUIDs.
- Lease dates are `YYYY-MM-DD` and are evaluated using the Riyadh calendar date, inclusively.
- A `rowVersion` is an opaque Base64 concurrency token. Do not generate or edit it.
- Read endpoints require `fleet.assignments.read`; create/close endpoints require `fleet.assignments.manage`.

## Recommended UI flow

1. Select the **lessor sponsor** (الكفيل المؤجّر / الكفيل الأصلي).
2. Select the **lessee sponsor** (الكفيل المستأجر). It must be different from the lessor.
3. Select `effectiveFrom` and, optionally, `effectiveTo`.
4. Request eligible vehicles for the lessor and selected period.
5. Select one or more vehicles and create the agreement.
6. On the vehicle-account assignment screen, assign the selected vehicle to a Keeta account belonging either to the lessor or to the lessee.

Do not use this feature for HungerStation or any other platform. The API creates these agreements against the active `KEETA` platform record automatically; the user does not choose a platform.

## Frontend data types

```ts
type SponsorVehicleLeaseEligibleVehicle = {
  vehicleId: string;
  assetNumber: string;
  registrationNumber: string | null;
  plateNumberAr: string | null;
  plateNumberEn: string | null;
  vehicleType: string;
  operationalStatus: string;
  operatingCityId: string | null;
};

type SponsorVehicleLeaseVehicle = {
  id: string; // agreement-vehicle relation ID
  vehicleId: string;
  assetNumber: string;
  registrationNumber: string | null;
  plateNumberAr: string | null;
  plateNumberEn: string | null;
};

type SponsorVehicleLeaseAgreement = {
  id: string;
  platformId: string;
  platformCode: "KEETA";
  platformNameAr: string;
  lessorSponsorId: string;
  lessorSponsorNameAr: string;
  lesseeSponsorId: string;
  lesseeSponsorNameAr: string;
  agreementDate: string | null;
  agreementReference: string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  status: "Scheduled" | "Active" | "Ended";
  endReason: string | null;
  notes: string | null;
  vehicles: SponsorVehicleLeaseVehicle[];
  rowVersion: string;
};
```

## 1. Load eligible vehicles

`GET /vehicle-platform-account-assignments/lease-agreements/eligible-vehicles`

Query parameters:

| Name | Required | Notes |
| --- | --- | --- |
| `lessorSponsorId` | Yes | The first/original sponsor. |
| `effectiveFrom` | No | Defaults to today's Riyadh date. |
| `effectiveTo` | No | Omit for an open-ended agreement. |

Example:

```http
GET /api/vehicle-platform-account-assignments/lease-agreements/eligible-vehicles?lessorSponsorId=11111111-1111-1111-1111-111111111111&effectiveFrom=2026-09-02&effectiveTo=2027-09-01
```

The response contains non-archived vehicles currently owned by the lessor. Vehicles with an overlapping Keeta lease are excluded.

```json
[
  {
    "vehicleId": "33333333-3333-3333-3333-333333333333",
    "assetNumber": "CAR-001",
    "registrationNumber": "VH-01A04D6E",
    "plateNumberAr": "أ ب ج 1234",
    "plateNumberEn": "ABC 1234",
    "vehicleType": "Car",
    "operationalStatus": "Available",
    "operatingCityId": "66666666-6666-6666-6666-666666666666"
  }
]
```

Disable the Create button when the result is empty or when the user has not selected at least one vehicle.

## 2. Create a lease agreement

`POST /vehicle-platform-account-assignments/lease-agreements`

```json
{
  "lessorSponsorId": "11111111-1111-1111-1111-111111111111",
  "lesseeSponsorId": "22222222-2222-2222-2222-222222222222",
  "vehicleIds": [
    "33333333-3333-3333-3333-333333333333",
    "44444444-4444-4444-4444-444444444444"
  ],
  "agreementDate": "2026-09-02",
  "agreementReference": "Keeta rental agreement 2026-09",
  "effectiveFrom": "2026-09-02",
  "effectiveTo": null,
  "notes": "Vehicles remain registered under the original sponsor."
}
```

Rules enforced by the backend:

- Both sponsors must be active and different.
- `vehicleIds` must contain at least one distinct, non-empty ID.
- Every selected vehicle must currently belong to `lessorSponsorId`.
- `effectiveTo`, when supplied, cannot precede `effectiveFrom`.
- A vehicle cannot appear in overlapping Keeta lease periods, even if the proposed lessee is different.
- `agreementReference` is optional (maximum 200 characters); `notes` is optional (maximum 4,000 characters).

Success is `201 Created` and returns `SponsorVehicleLeaseAgreement`.

## 3. List and view agreements

`GET /vehicle-platform-account-assignments/lease-agreements`

Optional query parameters:

| Name | Default | Meaning |
| --- | --- | --- |
| `lessorSponsorId` | none | Filter by original vehicle sponsor. |
| `lesseeSponsorId` | none | Filter by temporary operating sponsor. |
| `activeOnly` | `true` | Shows only agreements currently in their inclusive date range. |

Example:

```http
GET /api/vehicle-platform-account-assignments/lease-agreements?lesseeSponsorId=22222222-2222-2222-2222-222222222222&activeOnly=true
```

Read one agreement with:

```http
GET /api/vehicle-platform-account-assignments/lease-agreements/{agreementId}
```

Use the returned `vehicles` array for the agreement detail screen. `status` is:

- `Scheduled`: `effectiveFrom` is in the future.
- `Active`: today is inside the agreement period.
- `Ended`: `effectiveTo` is before today.

## 4. End an agreement

`POST /vehicle-platform-account-assignments/lease-agreements/{agreementId}/close`

```json
{
  "effectiveTo": "2027-09-01",
  "reason": "The vehicle rental agreement ended.",
  "rowVersion": "AAAAAAAAB9E="
}
```

- `effectiveTo` defaults to today's Riyadh date.
- `reason` is required, maximum 1,000 characters.
- Use the latest `rowVersion` returned from the agreement list/create/get response.
- On success (`200 OK`), replace the local agreement state with the returned response.
- If the API returns `409`, reload the agreement before allowing the user to retry.

An agreement ending today remains valid today and no longer grants the lessee permission starting tomorrow.

## 5. Vehicle-account assignment integration

The existing assignment endpoint remains:

```http
POST /api/vehicle-platform-account-assignments
```

```json
{
  "vehicleId": "33333333-3333-3333-3333-333333333333",
  "platformRiderAccountId": "55555555-5555-5555-5555-555555555555",
  "effectiveFromUtc": "2026-09-02T09:00:00Z",
  "reason": "Activate lessee Keeta account on rented vehicle"
}
```

For an active Keeta agreement where the account belongs to the lessee sponsor, the assignment response contains:

```json
{
  "vehicleSponsorId": "11111111-1111-1111-1111-111111111111",
  "accountSponsorId": "22222222-2222-2222-2222-222222222222",
  "platformCode": "KEETA",
  "usesSponsorVehicleLeaseAgreement": true,
  "sponsorVehicleLeaseAgreementId": "77777777-7777-7777-7777-777777777777",
  "hasProblems": false,
  "problems": []
}
```

Show a small badge such as `مركبة مؤجّرة` when `usesSponsorVehicleLeaseAgreement` is `true`. It identifies that the account sponsor is permitted by an agreement rather than by direct vehicle ownership.

Important:

- The assignment is still saved successfully even when `hasProblems` is `true`; warnings are not a failed request.
- The lease removes only the `SponsorMismatch` warning for the lessee's **Keeta** accounts.
- City mismatch, vehicle/account status, duplicate assignment, and capacity warnings continue to apply normally.
- The original lessor's Keeta accounts remain valid without an agreement.
- An account for any other sponsor, or any non-Keeta account, is not covered by this exception and receives `SponsorMismatch` when sponsors differ.

## 6. Assignment list and problems screen

The following existing endpoints return the new lease fields on every assignment response:

- `GET /vehicle-platform-account-assignments`
- `GET /vehicle-platform-account-assignments/{id}`
- `GET /vehicle-platform-account-assignments/problems`
- `POST /vehicle-platform-account-assignments`

Read these fields together:

| Field | UI meaning |
| --- | --- |
| `usesSponsorVehicleLeaseAgreement` | Display the leased-vehicle badge when `true`. |
| `sponsorVehicleLeaseAgreementId` | Link to the agreement detail page when non-null. |
| `vehicleSponsorNameAr` | Original vehicle sponsor. |
| `accountSponsorNameAr` | Sponsor under which the platform account is registered. |
| `hasProblems` and `problems` | Operational warnings; never interpret them as a failed assignment create. |

If an active agreement later expires or is closed, existing active assignments are recalculated and can begin returning `SponsorMismatch` in the problems endpoint. Refresh assignment data after creating or closing an agreement.

## Error handling

Expected errors use standard `ProblemDetails`. Show `detail` to the user and branch on the error code when needed.

| Situation | HTTP | Error code |
| --- | ---: | --- |
| Invalid dates, repeated/empty vehicle IDs, same sponsor | 400 | `fleet.invalid_request` |
| Selected vehicle does not belong to the lessor | 400 | `fleet.lease_vehicle_sponsor_mismatch` |
| A vehicle has an overlapping Keeta lease | 409 | `fleet.lease_period_conflict` |
| Keeta platform catalog record is unavailable | 409 | `fleet.keeta_platform_unavailable` |
| Sponsor, vehicle, or agreement not found | 404 | `fleet.not_found` |
| Stale row version or already-closed agreement | 409 | `fleet.concurrency_conflict` or `fleet.invalid_state` |
| Missing assignment permission | 403 | `fleet.forbidden` |

## Implementation checklist

- Add a Keeta vehicle-lease list screen with filters for both sponsors and active status.
- Add create and details screens using the lessor → lessee → dates → eligible-vehicles sequence.
- Store the agreement `rowVersion` with each loaded agreement for close operations.
- Refresh eligible vehicles after a successful create/close action.
- Add the rented-vehicle badge and agreement link to vehicle-account assignment cards.
- Keep the existing warning UI; a successful assignment with warnings must remain visible and usable.
