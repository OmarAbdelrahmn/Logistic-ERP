# Chefz workbook import — 15 October 2026

Applied to the existing SHIFTZ/Chefz platform in hosted database db67927.

| Result | Count |
| --- | ---: |
| Complete dashboard account rows checked | 464 |
| Accounts missing from the database | 0 |
| Sponsor, city, vehicle type or Freelancer/FullTime corrections needed | 0 |
| Account lifecycle states reconciled | 66 |
| Dashboard status labels reconciled | 33 |
| Completed assignment histories inserted | 3 |
| New current rider assignments | 66 |
| Original rider assignment restored | 1 |
| Existing current assignments already correct | 214 |
| Mistaken assignments retained as Cancelled | 2 |
| Previous assignment ended before transfer | 1 |
| Passwords encrypted and saved | 233 |
| Password cells marked “غير محفوظة”, left unchanged | 231 |
| Riders holding two Chefz accounts | 18 |
| Assignment rows pending because rider iqama is absent | 15 |
| Assignment row pending because the person is classified as an employee | 1 |
| Supplementary rows without settings or assignment data | 3 |

All 12 post-commit database checks passed using a fresh connection. All 233 saved credentials were checked against the encrypted import payload; a separate .NET decryption comparison against the workbook is recorded in the verification log. Existing performance records and settlements were preserved. No new accounts or riders were created. Freelancer/FullTime account type was kept separate from the platform payment model.

Completed histories use 1–6 October for account 2591985425, 1–10 October for 2620007464, and 1–10 October for 2588082814. The user confirmed that the current rider owns 11 October on 2620007464. The original spreadsheet return text is retained in completed-history.csv.

For account 2076420757, the original workbook rider assignment was restored and the mistaken 15 October replacement was cancelled. For 2618863134, the mistaken link was cancelled and the workbook rider assigned from 1 October. For 2559241233, the previous rider ends on 13 October and the workbook rider begins on 14 October. Existing rows and audit events were retained.

The initial encryption key was configured in the hosted API with user approval. The hosted configuration was re-read to confirm the key, and the API health endpoint returned HTTP 200 Healthy after the reload. Key material and plaintext source data are kept outside Git under ignored artifacts and are omitted from this report.

The 16 pending assignment rows are listed in exceptions.csv alongside the three incomplete supplementary rows. The employee-classification exception is account 2597241476, rider iqama 2606047062. Employment classification was preserved. These pending accounts retain their existing lifecycle state rather than being marked assigned without a rider link.

The database backup is before.xml. plan.json and executed.sql record the reviewed changes, and post-commit-verification.xml records the independent checks. The audit correlation is `chefz-account-sheet-20261015-c0e2521f1cdc`. Replay protection refuses a second commit of the same import.

Source workbook SHA-256: `c0e2521f1cdc1a86aa5bbe1571d9c38b788b43a6deb80bf5b589687d9fde79af`.
