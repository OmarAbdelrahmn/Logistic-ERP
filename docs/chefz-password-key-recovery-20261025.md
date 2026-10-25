# Chefz password HTTP 500 — encryption key recovery

Reported account: `2dee6b7f-a813-4429-a9bc-3b3d0c1330fe`.

Reported route: `/api/chefz/accounts/2dee6b7f-a813-4429-a9bc-3b3d0c1330fe/password`.

Reported correlation ID: `dd9a9c50340046f0a1986c916dac3d5e`.

On 25 October 2026, fresh read-only retrieval showed the hosted `appsettings.json` missing `SensitiveData:EncryptionKey`. The downloaded `web.config` did not declare an encryption-key override; hosted Development settings also lacked it and Production settings were absent. Hosting-level environment variables outside these files were not observable. Hosted stdout logging was disabled, so the original request's exception could not be recovered by correlation ID. The production credential protector requires the missing setting; failure is consistent with the reported 500.

The trusted key retained from the 15 October Chefz import successfully decrypted all **233 Chefz credential versions**, including the reported account's credential. No passwords or keys were printed. Four other encrypted-field columns had no stored values. A separate **89 Ninja credential versions use a different key** and were not recovered by this operation.

At **11:35 Riyadh time** (`08:35 UTC`), the original Chefz key was restored to hosted configuration. Existing settings were backed up and compared, and the proposed change was checked to add only that setting. A comment-only `web.config` change requested an API recycle. Fresh remote read-back confirmed the restored setting; the live health endpoint returned HTTP 200 after the recycle. No database writes, password resets, credential re-encryption or schema changes were performed.

The account's authenticated HTTP password request was not replayed because an authenticated user session was not available. The saved credential's decryption and the hosted configuration/health were verified; an authenticated retry is still needed to confirm that request end to end. The separate Ninja key mismatch remains unresolved and requires its original key rather than password resets or a newly invented key.

The Web Deploy publish profile now skips `appsettings*.json` to preserve hosted secrets on subsequent publishes. MSBuild evaluation confirms the skip rule, and its pattern was checked against the base, Development and Production settings filenames while excluding API binaries. No API binaries were published by this recovery.

Evidence:

- [Sanitized recovery report](../outputs/chefz-password-key-recovery-20261025/report.json)
- [Read-only key compatibility summary](../outputs/chefz-password-key-recovery-20261025/key-compatibility.json)
- [Recovery tool](../tools/Restore-ChefzCredentialEncryptionKey.ps1): default is plan mode; `-Apply` restores only the trusted original key if the hosted setting is absent and requests a recycle. It refuses to replace a different configured key.

Private configuration backups and the original key remain in ignored `artifacts/` folders. They are not included in these reports. Backup configuration was retained; the recovery rollback path was not exercised during the successful update.
