# Jahez settlement reminders

`GET /api/jahez/v2/settlement-reminders?page=1&pageSize=50` returns `{ items, page, pageSize }`. Each item contains `handoverId`, `accountId`, `externalAccountId`, `riderProfileId`, `riderName`, `reminderAnchorAtUtc`, and `daysSinceSettlement`. Requires `jahez.read` scoped to Jahez. Page size is 1–100.

The list is calculated on request. It includes sessions more than 10 Riyadh calendar days after the last executed settlement, or handover start if none. Open sessions qualify; ended sessions qualify when settlement is deferred or transferred debt remains unpaid. The worker no longer generates Jahez overdue notifications. Existing notifications are not deleted. Results are ordered by handover ID.
