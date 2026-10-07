# Registered platform account owners — 7 October 2026

The live hosted application database `db67927` was updated for Jahez, HungerStation, and The Chefsz (`SHIFTZ` in the ERP catalog). Keeta was excluded. The source was the reviewed and deduplicated workbook at `outputs/iqama-platform-review-20261007-v2/platform_accounts_deduplicated.xlsx`.

Before replacement, the database held four Jahez accounts and no HungerStation or SHIFTZ accounts. The four Jahez accounts and their linked assignment and Jahez history rows were backed up, then removed inside the same transaction that inserted the replacement accounts. No assignment to a current rider was created.

| Platform | Available | Suspended | Retired | Archived | Total |
| --- | ---: | ---: | ---: | ---: | ---: |
| Jahez | 334 | 0 | 46 | 0 | 380 |
| HungerStation | 264 | 72 | 0 | 102 | 438 |
| The Chefsz (`SHIFTZ`) | 293 | 171 | 0 | 0 | 464 |
| Total | 891 | 243 | 46 | 102 | 1,282 |

Each account has its registered owner's employee ID resolved by iqama. Fifteen iqamas lacked an ERP employee record, so minimal owner records were created using names in the supplied HungerStation and Chefsz files. Their employment and sponsorship remain unverified; they have Draft employee status and no rider assignment. Existing owners without a rider profile are now displayed by their employee name in the account API.

Six archived HungerStation accounts marked `خارج الكفالة` have a null sponsor ID and display that label in account listings. Migration `20261007120737_AllowPlatformAccountOutsideSponsorship` makes account sponsorship nullable. The Buraidah global and operating city entries were created for seven source accounts. The original Arabic source status is retained in `StatusReason` and the source vehicle type in `OperationalNotes`.

The updated API was deployed to `https://gat.premiumasp.net` before the database transaction. A deployment plan after publishing reported zero file differences, and `/health/live` returned 200. The source change built with no warnings; 40 focused domain tests and 33 focused fleet tests passed. The live database replacement dry run rolled back cleanly before the final commit.

Post-import comparison found 1,282 live accounts, zero missing accounts, zero owner, city, sponsor, or status mismatches, zero linked rider assignments, and zero old Jahez account IDs or handovers. The six null-sponsor accounts and the migration history entry were present. Keeta's account count was unchanged.

The recoverable pre-import database table snapshots, SHA-256 manifest, and previous hosted API package are in `outputs/platform-account-live-20261007-backup/`. The source workbook and import scripts remain in the task workspace.
