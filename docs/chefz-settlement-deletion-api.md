# Delete Chefz settlements for a half-month

`DELETE /api/chefz/settlements?year=2026&month=11&half=1` deletes 1–15 November; `half=2` selects 16 through the last day of the month. The caller needs the new client-scoped, high-trust `chefz.settlements.delete` permission for all Chefz platforms. Deploy the permission migration and grant the permission through the normal authorization process.

The operation permanently deletes all wallet and order settlements in the chosen period, their imported order rows, and import records whose settlements are entirely in that period. The response reports period dates and deleted counts. The original workbook can then be uploaded and recalculated. Repeating the request for an empty period returns zero counts. A period-level audit entry records the actor, dates, and counts.

If an imported workbook spans multiple periods, the endpoint returns 409 without deleting anything. Such a workbook cannot currently be reset one period at a time; retaining the other period while replaying the original full workbook requires additional import support. Invalid periods return 400 and missing permission returns 403.
