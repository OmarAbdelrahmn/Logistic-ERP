# Ninja account password reads

Source change: 24 October 2026. Publish the updated API to make this available; this extension needs no database migration.

Both `GET /api/ninja/accounts` (inside each `items` entry) and `GET /api/ninja/accounts/{accountId}` return a nullable `password` property on the account object:

```json
{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "captainId": "214235",
  "phoneNumber": "0578756197",
  "hasPassword": true,
  "password": "EXAMPLE_SAVED_PASSWORD",
  "passwordChangeAllowedAtUtc": "2026-10-27T12:14:31Z"
}
```

The example password is a placeholder. The API returns the actual readable password, not ciphertext or a masked value. It reads the latest credential by `KeyVersion`; `hasPassword` and the 72-hour deadline come from that same version. Credentials remain encrypted in storage. The password is the value saved in the ERP; the endpoint does not contact the Ninja provider.

Account access still requires `ninja.read`. The password field additionally requires the existing `platform_credentials.read` permission for the account's Ninja client platform. Without it, the account is readable with `password: null`, even when `hasPassword` is true. A credential-read grant for another platform or a rotation grant alone does not reveal the password. If no password has been saved, `password` and the deadline are null and `hasPassword` is false.

During the password cooldown, rejected replacements are not returned: the retained saved password remains visible. After a successful rotation, the next account GET returns the new saved value and its new deadline. Refresh account detail after credential writes. Status writes and other mutation responses do not include the readable password.

The GET routes disable response caching with `Cache-Control: no-store, no-cache`. Read `password` directly from the account object using the updated [TypeScript contract](ninja-api-contracts.ts). Keep the existing `hasPassword` and deadline display behavior; null password does not necessarily mean no saved credential.

Verification: the solution builds with zero warnings/errors. All 14 new password-read service cases and the SQL Server query-translation check pass, as do all 40 Ninja authorization checks. Tests cover both GET service paths, missing credentials, cooldown/rotation, scoped credential access, no decryption without permission, paging and omission from status writes. SQL translation is verified without a live database; this change has not been deployed to the hosted API.
