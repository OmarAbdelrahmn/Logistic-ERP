# Reactivate an employee or rider

`POST /api/employees/{employeeId}/reactivate` requires the workforce employees update permission. Use the employee ID for both employees and riders.

```json
{
  "effectiveDate": "2026-11-01",
  "reason": "Return to work"
}
```

Only a non-archived person currently in `Terminated` status can be reactivated. A reason and effective date are required. The existing activation checks apply: a valid iqama, a sponsor for sponsored people, and a rider profile for riders. On success, the response is the employee details; status becomes `Active`, the current termination date and status reason are cleared, and a status history entry is recorded. This does not recreate assignments. Missing employees return not found; a different current status returns a conflict; invalid activation data returns a validation error.

The existing `POST /api/employees/{employeeId}/status-transitions` can also transition a terminated person to `Active`; it now clears the termination date as well.
