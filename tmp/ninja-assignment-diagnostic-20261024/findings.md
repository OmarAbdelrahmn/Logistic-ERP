# Ninja assignment investigation — 24 October 2026

Request: `/api/ninja/accounts/01a13935-9d89-753e-a5b3-3afecfba099c/assign`

Correlation ID: `b96ef76a01ccbdc1757b4fb26cec985c`

## Findings

- Hosted account 215339 is available, with no assignment history, no credential versions, one visible Ninja detail record, and one visible owner rider profile.
- Assignment start validation and day allocation staging pass against the configured hosted database for 1 October 2026. Staging was discarded without saving.
- A local assignment run using an artificial encryption key reached persistence. An EF command interceptor rejected database writes before execution. No assignment, password, or other business data was saved.
- Downloaded hosted `appsettings.json` contains no `SensitiveData:EncryptionKey`. Downloaded `web.config` contains no matching environment override. No hosted `appsettings.Production.json` exists. Hosting-level environment settings outside these files were not observable.
- Calling the credential protector with the downloaded file settings reproduces `InvalidOperationException: SensitiveData:EncryptionKey must be supplied from a secret source outside Development.`
- Ninja assignment prepares a new password before saving the assignment, so missing encryption configuration explains HTTP 500 when the optional new password is supplied. Credential protection is initialized lazily, allowing other API operations to continue working.
- Hosted stdout logging is disabled and the configured logs directory does not exist. The original exception could not be retrieved for the supplied correlation ID.

## Assessment

Missing credential encryption configuration is a reproduced, likely cause of the reported error, subject to any unobserved hosting-level environment overrides. Restore the existing master encryption key from its trusted secret source. Do not invent a replacement key without considering already encrypted data.

No hosted application configuration or business data was changed. The downloaded settings file was removed locally after inspection because it contains unrelated secrets.
