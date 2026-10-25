# Ninja account read HTTP 500 investigation

Reported route: `/api/ninja/accounts`.

Initially reported correlation ID: `138c01d269582149790e300608f19f98`.

The API already supports the hosting variable `SensitiveData__EncryptionKey`: `WebApplication.CreateBuilder` loads environment configuration, and the credential protector resolves `SensitiveData:EncryptionKey`. The user confirms the variable is configured in the hosting control panel. Its actual value in the hosted worker process was not available to this investigation.

Fresh remote configuration retrieval found a valid key in `appsettings.json`, no key override inside `web.config`, and no Production JSON file. The downloaded web configuration had stdout logging disabled. These facts do not rule out a control-panel environment override.

The user subsequently supplied a server error for correlation ID `0f48106ff9410c26958c067191b52e8d`. It confirms `System.Security.Cryptography.AuthenticationTagMismatchException: The computed authentication tag did not match the input authentication tag.` in `PlatformCredentialProtector.Unprotect`, called by `NinjaService.AccountResponses`. SQL account, rider, employee and sheet queries completed before the failure. This proves a credential authentication failure for that logged request, consistent with a different encryption key or altered encrypted material. It does not prove the environment variable was ignored. The separate ASP.NET Core Data Protection warnings in the log refer to a different key mechanism; platform credentials use the custom AES-GCM protector.

Read-only database checks found 89 Ninja credential versions. None authenticated with the downloaded JSON key or the available local configuration/development fallback candidates. No passwords or keys were printed, and no database writes were made. This demonstrates a mismatch with those candidates, not with the unobserved hosted environment key.

The source change isolates cryptographic credential failures during list/detail GET reads: the affected account returns `password: null`, retains its metadata, and logs event `3100` with account/correlation IDs. Other account passwords remain readable when their credentials authenticate. It does not change key resolution, stored credentials, schemas, password rotation, or deployment configuration.

The failing logged request is covered by the source change. The exact originally reported correlation ID was not present in the supplied excerpt. This source change has not been deployed and does not establish that the hosted HTTP 500 is resolved. Recovering the affected passwords requires compatible original key material; returning null does not recover them.

Validation: the full solution rebuilt with zero warnings/errors. The account workflow run passed 95 cases and skipped nine requiring unavailable SQL Server/workbook fixtures. One existing test, `StatusValidationPermissionsConcurrencyAndDebtBlockPreventMutations`, failed during the initial run because it expects transferred Jahez debt to block operations, while the current `RiderOperationalBlock` implementation explicitly disables that rule. It was excluded from the subsequent account workflow run; this unrelated behavior was not changed.

The additional `PlatformCredentialConfigurationTests.CredentialProtectorUsesEnvironmentKeyOverJsonStyleConfiguration` test passed through the actual infrastructure registration: a double-underscore environment setting takes precedence over a different JSON-style key and decrypts a credential made with the environment key. The test uses a unique environment prefix and removes its temporary setting afterward.

Subsequent resolution: the user confirmed a recent hosting key change and authorized deleting old-key passwords for Ninja only. On 25 October at 12:06:59 Riyadh time, all 89 pre-change Ninja credential versions were removed with an encrypted backup and preservation checks. All 328 Ninja accounts and 233 other-platform credentials remain. See the [cleanup record](ninja-password-cleanup-20261025.md). No API publication was required for that database cleanup; the source resilience change above remains unpublished.
