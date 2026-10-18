# Chefz response examples

These are complete synthetic JSON responses for frontend development, not hosted records or files to import into production. Identifiers, people, sponsors and tokens are illustrative. Ratings assume the default settings and no other recorded performance data.

Use the [implementation handoff](../chefz-accounts-reports-frontend-handoff-20261015.md) and keep both [TypeScript entry point](../chefz-accounts-and-reports-contracts.ts) and its [underlying contracts](../chefz-new-endpoints-contracts.ts).

| File | Example |
| --- | --- |
| [accounts.json](accounts.json) | Two assigned accounts on one rider; global and sponsor counts |
| [performance-import.json](performance-import.json) | One rider, two account rows on 20 September 2026 |
| [rider-performance-report.json](rider-performance-report.json) | Per-account summary/source rows plus combined daily/half summaries |
| [money-import.json](money-import.json) | Saved September second-half import; two FullTime accounts, fees 375 + 275 = 650 SAR |
| [assignment-limit.json](assignment-limit.json) | Third Chefz account rejected with maximum 2 and both current accounts |
| [examples.typecheck.ts](examples.typecheck.ts) | Same examples checked against the exported transport types |

The money example has 288 SAR net earnings and -362 SAR combined rider balance after the 650 SAR company amount. Account A has 15 fee days and account B 11; each account has its own amounts and visible row. A money preview uses empty settlement GUIDs and a null import ID; use account + assignment + period for preview row keys. Replays return `alreadyImported=true`; invalid batches return issues rather than these success examples.
