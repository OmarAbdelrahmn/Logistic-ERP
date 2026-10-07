# Vehicle return KM — frontend handoff

## Required behavior

Allow a vehicle return with any nonnegative whole-number KM reading, including zero and a reading below the handover KM, the vehicle's current KM, or any historical reading.

Save the entered reading exactly as `endOdometer`. The backend records it on the completed assignment and in odometer history. A lower reading does not decrease the vehicle's current odometer or tracked mileage.

This applies to both ordinary returns and returns with a condition report. This change concerns the return flow; the vehicle-switch flow has separate validation.

## Frontend changes

File: `C:/Users/omarf/Videos/FrontProject/src/app/admin/fleet/assignments/components/ReturnVehicleModal.tsx`.

These changes are already implemented in the local frontend checkout:

1. Remove `minOdometer` state and comparisons against `startOdometer`, current KM, or historical KM.
2. Remove the minimum-KM helper text and any error saying the return reading must exceed an earlier reading.
3. Use `min={0}` and `step={1}` on the return KM input. The current reading may remain the initial suggested value, but users can edit it to a lower value.
4. Validate only that the reading is a nonnegative safe integer:

```tsx
if (!Number.isSafeInteger(formData.endOdometer) || formData.endOdometer < 0) {
  toast.error(
    "قراءة عداد غير صحيحة",
    "أدخل قراءة عداد صحيحة تساوي صفرًا أو أكثر."
  );
  return null;
}
```

5. Submit the entered value directly:

```ts
endOdometer: formData.endOdometer,
```

Do not replace it with `Math.max(currentOdometer, endOdometer)` or require a correction reason solely because it is lower. Apply the same validation to the normal-return submit and condition-report submit; both currently use `validateMainForm()`.

Keep the existing assignment ID, assignment row version, return date, fuel, and condition-report validation. The return date still cannot precede the handover date.

## API contract

No request-field or route change is needed in `src/lib/fleet/api.ts`.

| Return type | Request |
| --- | --- |
| Good condition | `POST /api/vehicle-assignments/return`, JSON body |
| Condition report | `POST /api/vehicle-assignments/return-with-condition-report`, multipart form |

For an ordinary return, continue sending `assignmentId`, `endedAtUtc`, `endOdometer`, `endCondition`, `endFuelLevelPercentage`, `reason`, and `rowVersion` using the existing DTO.

For a return with a report, put the same return fields and `conditionReport` in the multipart `metadata` JSON, alongside the existing `evidenceFiles`. Let the browser set the multipart Content-Type boundary. Keep the existing `Idempotency-Key` handling for both routes.

After success, refresh the assignment list and any displayed vehicle details or rider timeline. Display the completed assignment's `endOdometer` as the return KM. The vehicle's current KM can remain higher than the submitted return KM.

## Acceptance checks

With handover KM = 10,000, current vehicle KM = 15,000, and an older reading = 20,000:

| Entered return KM | Expected |
| ---: | --- |
| 0 | Accepted; actual return KM saved; vehicle KM does not decrease |
| 5,000 | Accepted despite being below all three readings |
| 12,000 | Accepted despite being below current and historical readings |
| 17,000 | Accepted; vehicle mileage updates if it also exceeds tracked mileage |
| -1 | Rejected |
| Fractional/non-numeric input | Rejected |

Check both ordinary returns and returns with a condition report. A stale assignment row version or a return date before handover must still fail.

## Deployment requirement

The frontend change alone is insufficient. Deploy the updated backend and frontend, and apply application migration `20261006155322_AllowLowerVehicleReturnOdometer`.

The incremental idempotent deployment script is:

`database/scripts/allow-lower-vehicle-return-odometer.sql`

It changes the assignment odometer database constraint to allow any nonnegative end reading. Without this migration, the old database constraint can still reject a reading below handover KM even when API validation accepts it.

The local code changes and deployment script are prepared. This handoff does not confirm deployment to `gat.premiumasp.net` or application of the migration to its database. Restoring the previous constraint requires reconciling any newly accepted readings below handover KM first.
