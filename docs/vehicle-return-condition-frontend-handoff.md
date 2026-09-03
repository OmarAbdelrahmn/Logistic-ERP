# Vehicle Return and Switch Condition Report — Frontend Handoff

## Goal

Show a condition-report modal only when a vehicle is returned, or switched out, with a condition other than `Good`. The modal captures the frontend-selected problem category and severity, the problem description, whether the rider is responsible, the estimated repair cost, and one or two proof files.

The backend saves the vehicle return, blocking issue, responsibility assessment, repair estimate, and evidence files atomically. A failed request saves none of them.

## Frontend request changes

1. Add `category` and `severity` inside every non-good `conditionReport`; both are now required and are stored exactly as selected by the frontend.
2. For a non-good return, continue sending the report in multipart `metadata` to `/api/vehicle-assignments/return-with-condition-report` with one or two `evidenceFiles`.
3. For a switch, add optional `conditionReport` to `SwitchVehicleRequest` and add the multipart `evidenceFiles` collection alongside the existing `promissoryFiles`.
4. Trigger the switch modal from `oldVehicleCondition != Good`; the report belongs to the old vehicle being returned, not the replacement vehicle.
5. For a good return or good switch-out, omit `conditionReport` and `evidenceFiles` completely.

## Trigger rule

Use the existing `VehicleCondition` values:

| Value | Name | Show modal |
|---:|---|---|
| 1 | `Unknown` | Yes |
| 2 | `Good` | No |
| 3 | `Fair` | Yes |
| 4 | `Damaged` | Yes |
| 5 | `Unsafe` | Yes |

Frontend rule:

```ts
const returnNeedsConditionReport = endCondition !== 2;
const switchNeedsConditionReport = oldVehicleCondition !== 2;
```

Do not render, validate, or retain modal values when `endCondition` (return) or `oldVehicleCondition` (switch) is `Good`. If the user changes a non-good selection back to `Good`, close the modal and clear its state and selected files.

## Recommended modal

Title:

- English: `Vehicle condition report`
- Arabic: `تقرير حالة المركبة`

Required fields:

1. `category` — selected by the frontend from `VehicleIssueCategory`.
2. `severity` — selected by the frontend from `VehicleIssueSeverity`.
3. `problemDescription` — multiline text, 1–4000 characters.
4. `isRiderResponsible` — explicit Yes/No selection; do not use an unchecked checkbox as an implicit answer.
   - `true`: the rider is assessed as responsible.
   - `false`: the issue is assessed as natural/mechanical and not caused by the rider.
5. `estimatedRepairCost` — SAR amount, zero or greater, maximum two decimal places.
6. `evidenceFiles` — at least one and at most two files.

Category values:

| Value | Name | Suggested Arabic label |
|---:|---|---|
| 1 | `Problem` | `عطل ميكانيكي / تقني` |
| 2 | `Accident` | `حادث` |
| 3 | `Theft` | `سرقة` |
| 4 | `Damage` | `تلفيات خارجية` |
| 5 | `Administrative` | `مشكلة إدارية (أوراق، غرامات...)` |

Severity values:

| Value | Name | Suggested Arabic label |
|---:|---|---|
| 1 | `Low` | `منخفضة` |
| 2 | `Medium` | `متوسطة` |
| 3 | `High` | `عالية` |
| 4 | `Critical` | `حرجة جداً` |

Suggested Arabic labels:

- `ما هي المشكلة؟`
- `تصنيف المشكلة`
- `الأهمية / الخطورة`
- `هل السائق مسؤول عن المشكلة؟`
- `التكلفة التقديرية للإصلاح`
- `إثبات المشكلة (ملف أو ملفان)`

Accepted evidence formats are PDF, JPEG, PNG, WebP, GIF, and BMP. Each file may be at most 10 MiB. Show the selected filename and size, allow removal before submission, and explain that the files are private evidence.

Keep the main return or switch form open behind the modal. The modal confirmation should submit the complete operation, not create a separate issue first.

## API flow

### Good condition

Continue using:

`POST /api/vehicle-assignments/return`

Content type: `application/json`

```json
{
  "assignmentId": "assignment-guid",
  "endedAtUtc": "2026-09-03T12:30:00Z",
  "endOdometer": 12120,
  "endCondition": 2,
  "endFuelLevelPercentage": 65,
  "reason": "End of shift",
  "rowVersion": "base64-row-version"
}
```

Do not send `conditionReport` for a good return.

### Non-good condition

Use:

`POST /api/vehicle-assignments/return-with-condition-report`

Content type: `multipart/form-data`

Form fields:

- `metadata`: JSON string containing the full return request and `conditionReport`.
- `evidenceFiles`: repeat this key once for each selected file.

Example metadata:

```json
{
  "assignmentId": "assignment-guid",
  "endedAtUtc": "2026-09-03T12:30:00Z",
  "endOdometer": 12120,
  "endCondition": 4,
  "endFuelLevelPercentage": 65,
  "reason": "End of shift",
  "rowVersion": "base64-row-version",
  "conditionReport": {
    "category": 4,
    "severity": 3,
    "problemDescription": "Rear wheel and mudguard are damaged",
    "isRiderResponsible": true,
    "estimatedRepairCost": 450.00
  }
}
```

TypeScript example:

```ts
type VehicleConditionReport = {
  category: 1 | 2 | 3 | 4 | 5;
  severity: 1 | 2 | 3 | 4;
  problemDescription: string;
  isRiderResponsible: boolean;
  estimatedRepairCost: number;
};

async function returnVehicleWithConditionReport(
  returnRequest: Record<string, unknown>,
  report: VehicleConditionReport,
  evidenceFiles: File[],
  idempotencyKey: string,
) {
  const form = new FormData();
  form.append("metadata", JSON.stringify({
    ...returnRequest,
    conditionReport: report,
  }));

  for (const file of evidenceFiles) {
    form.append("evidenceFiles", file);
  }

  return fetch("/api/vehicle-assignments/return-with-condition-report", {
    method: "POST",
    headers: { "Idempotency-Key": idempotencyKey },
    body: form,
  });
}
```

Do not set the `Content-Type` header manually for `FormData`; the browser must add the multipart boundary.

Generate one idempotency key when the user starts submitting and reuse it only when retrying the exact same payload and exact same files. Generate a new key after the user changes any value or file.

### Switch operation

The condition report applies to the **old vehicle being switched out**. Use `oldVehicleCondition` for the modal trigger; `newVehicleCondition` describes the replacement vehicle being taken and does not trigger this report.

Continue using:

`POST /api/vehicle-assignments/switch`

Content type: `multipart/form-data`

Form fields:

- `metadata`: the complete `SwitchVehicleRequest` JSON string.
- `promissoryFiles`: zero or more existing switch promissory files, subject to the existing maximum of three active files.
- `evidenceFiles`: one or two condition evidence files when `oldVehicleCondition != Good`; otherwise omit this field.

Non-good switch metadata example:

```json
{
  "currentAssignmentId": "old-assignment-guid",
  "newVehicleId": "replacement-vehicle-guid",
  "switchedAtUtc": "2026-09-03T12:30:00Z",
  "oldVehicleOdometer": 12120,
  "newVehicleOdometer": 8400,
  "oldVehicleCondition": 4,
  "newVehicleCondition": 2,
  "oldFuelLevelPercentage": 40,
  "newFuelLevelPercentage": 90,
  "permissionReference": "PERMIT-2026-1001",
  "reason": "Vehicle replacement",
  "rowVersion": "base64-old-assignment-row-version",
  "conditionReport": {
    "category": 4,
    "severity": 3,
    "problemDescription": "Rear wheel and mudguard are damaged",
    "isRiderResponsible": true,
    "estimatedRepairCost": 450.00
  }
}
```

Switch FormData example:

```ts
const form = new FormData();
form.append("metadata", JSON.stringify(switchRequest));

for (const file of promissoryFiles) {
  form.append("promissoryFiles", file);
}

if (switchRequest.oldVehicleCondition !== 2) {
  for (const file of evidenceFiles) {
    form.append("evidenceFiles", file);
  }
}

await fetch("/api/vehicle-assignments/switch", {
  method: "POST",
  headers: { "Idempotency-Key": idempotencyKey },
  body: form,
});
```

For a good switch-out, omit both `conditionReport` and `evidenceFiles`. The switch endpoint remains multipart because it already supports `promissoryFiles`.

## Successful result

The return and switch endpoints respond with `200 OK` and `RiderVehicleAssignmentResponse`.

After a successful non-good return or switch-out:

1. Close the modal and return/switch form.
2. Refresh the vehicle and assignment data.
3. Expect the vehicle status to be `ProblemHold=3`.
4. Refresh the rider/vehicle timeline or vehicle-issues list to display the created issue.

The issue is automatically linked through `relatedAssignmentId`. The backend stores the exact valid `category` and `severity` selected and sent by the frontend; it no longer derives either value from the vehicle condition.

Every return-created `VehicleIssueSummaryResponse` includes:

```json
{
  "id": "issue-guid",
  "issueNumber": "ISS-80001234567890AB",
  "vehicleId": "vehicle-guid",
  "relatedAssignmentId": "assignment-guid",
  "rider": {
    "riderProfileId": "rider-profile-guid",
    "employeeId": "employee-guid",
    "riderName": "اسم الموظف المسند إليه",
    "isRealRider": true,
    "realRider": null
  },
  "category": 4,
  "severity": 3,
  "isRiderResponsible": true,
  "estimatedRepairCost": 450.00,
  "blocksOperation": true,
  "status": 1,
  "description": "Rear wheel and mudguard are damaged",
  "rowVersion": "base64-row-version"
}
```

`rider` is populated by the backend from `relatedAssignmentId`; the frontend must not send it. If `isRealRider=false`, `riderName` is the assigned employee's name and `realRider` contains the actual rider's `id`, `name`, `iqamaNo`, and `relationshipToAssignedRider`. Use the following display fallback:

```ts
const issueRiderName = issue.rider?.realRider?.name ?? issue.rider?.riderName ?? "—";
```

`rider` is `null` only when the issue has no related assignment or the related assignment is no longer visible in the current data scope.

## Evidence display and download

List evidence:

`GET /api/vehicle-issues/{issueId}/evidence`

Example item:

```json
{
  "id": "evidence-guid",
  "vehicleIssueId": "issue-guid",
  "originalFileName": "rear-wheel.jpg",
  "contentType": "image/jpeg",
  "fileSizeBytes": 245120,
  "sha256Checksum": "HEX-SHA256",
  "uploadedAtUtc": "2026-09-03T12:31:00Z",
  "rowVersion": "base64-row-version"
}
```

Download evidence:

`GET /api/vehicle-issues/{issueId}/evidence/{evidenceId}/download`

The download is authenticated and supports range processing. Do not construct a public storage URL; evidence is deliberately stored outside static-file hosting.

## Validation and errors

Disable the modal submit button until all required fields are valid, but still display backend `ProblemDetails` errors.

| Error code | Frontend handling |
|---|---|
| `fleet.return_condition_report_required` | Keep the modal open and highlight missing category, severity, report fields, or evidence files. |
| `fleet.return_condition_report_not_allowed` | Clear modal state and submit the good-condition JSON flow. |
| `fleet.invalid_file` | Explain accepted types and the 10 MiB per-file limit. |
| `fleet.idempotency_required` | Generate and send an idempotency key. |
| `fleet.idempotency_conflict` | Generate a new key only if the payload or files changed. |
| `fleet.concurrency_conflict` | Reload the assignment and require the user to review current data before resubmitting. |
| `fleet.not_found` | Close stale UI and refresh active assignments. |
| `fleet.forbidden` | Show an access-denied message. |

The return-report multipart request limit is 22 MiB. The switch multipart request limit is 54 MiB because it can contain both promissory files and condition evidence. The backend rejects:

- a non-good return or switch-out without a complete condition report;
- a missing or unknown category or severity;
- zero evidence files or more than two evidence files;
- an empty, unsupported, mismatched, or oversized file;
- a good return or switch-out containing report data or evidence;
- a good condition sent to the condition-report-only endpoint;
- negative repair cost, blank/oversized description, stale row version, invalid odometer, or invalid fuel percentage.

## Accessibility and RTL notes

- Use a real dialog with focus trapping, Escape handling, and focus restoration.
- Put the validation summary inside the modal and announce it with `aria-live`.
- In Arabic, use RTL layout while keeping numbers, currency, and filenames readable.
- Do not communicate rider responsibility using color alone; always show explicit text.
- Ask for confirmation before submission because the return closes the active assignment and may place the vehicle on hold.

## Acceptance checklist

- Selecting `Good` for `endCondition` or `oldVehicleCondition` never opens the modal and clears any previous report state.
- Selecting `Unknown`, `Fair`, `Damaged`, or `Unsafe` for either return condition or the switch's old-vehicle condition opens the modal.
- Category and severity selections are sent by the frontend and returned unchanged on the created issue.
- Yes/No responsibility is explicitly selected.
- One file succeeds, two files succeed, zero or three are blocked.
- Changing files or fields after a failed attempt generates a new idempotency key.
- Successful non-good return shows the returned vehicle in `ProblemHold` after refresh.
- Successful non-good switch shows the old vehicle in `ProblemHold` and the replacement vehicle in `Assigned` after refresh.
- Issue details show responsibility, estimated cost, and the linked assignment.
- Authorized users can list and download both evidence files.
- A stale row version reloads the assignment rather than silently retrying.
