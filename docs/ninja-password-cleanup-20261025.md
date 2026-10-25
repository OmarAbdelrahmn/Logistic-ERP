# Ninja old-key password cleanup

The user reported changing the hosting encryption key on 25 October 2026, could not provide a compatible original Ninja key, and authorized removal of old-key passwords, explicitly limited to Ninja.

Applied on 25 October 2026 at **12:06:59 Riyadh time** (`09:06:59 UTC`). Removed **89 saved Ninja credential versions belonging to 89 accounts**. All were saved before the key-change date; the latest was 24 October at `16:56:51 UTC`. No credentials had been saved in the preceding 30 minutes at preflight. Classification used the known key-change date, not the `KeyVersion` rotation counter or an unavailable hosting key. The current hosting key was not requested or changed by the cleanup.

Independent post-commit reads verified:

- All **328 Ninja master accounts** remain, with zero accounts containing saved credentials.
- All **233 SHIFTZ/Chefz credential versions** remain unchanged.
- No legacy Ninja credential versions remain.

An exact preflight manifest with row fingerprints restricted deletion to the reviewed 89 records. The cleanup acquired the application's account write locks and used one serializable transaction. Before deletion, it saved an encrypted-row backup under ignored `artifacts/` storage and read it back to verify its contents. Bidirectional SQL comparisons verified all retained credential columns and every Ninja account column before commit. The account and credential ciphertext values were not printed. An initial attempt stopped before deletion because temporary tables created in a parameterized SQL scope did not persist; its transaction rolled back. The corrected attempt completed and verified successfully.

A second invocation returned `AlreadyRemoved` with zero database writes. The backup restore path has not been exercised. The backup contains encrypted values, not readable passwords, and remains available for rollback. Database schema, account assignments, phones, hosting settings and API binaries were not changed.

These accounts now return `hasPassword: false` and null password/cooldown metadata until passwords are entered again. New passwords use the API's effective hosting encryption key. The cleanup removes only values saved in the ERP; it does not change passwords at the Ninja provider. An authenticated hosted account request was not available for end-to-end verification.

- [Sanitized report](../outputs/ninja-password-cleanup-20261025/report.json)
- [Cleanup tool](../tools/Remove-LegacyNinjaPasswords20261025.ps1): read-only plan by default; apply requires the exact reviewed manifest and refuses changed rows, unexpected references or enabled credential triggers.
