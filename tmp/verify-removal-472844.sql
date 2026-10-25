SELECT a.ExternalAccountId,a.Status,
 (SELECT COUNT(*) FROM app.RiderClientAssignments r WHERE r.PlatformRiderAccountId=a.Id AND r.EffectiveFrom='20261022') TodayAssignments,
 (SELECT COUNT(*) FROM app.RiderClientAssignments r WHERE r.PlatformRiderAccountId=a.Id AND r.Id='37294ead-7144-4eb8-b80c-bf3ddf0d4904' AND r.RowVersion=0x0000000000035474) EarlierAssignmentUnchanged,
 (SELECT COUNT(*) FROM jahez_v2.AccountSessions h WHERE h.PlatformRiderAccountId=a.Id AND h.Id='6fc7f8b1-7875-4eb6-a1dd-ce853636300f' AND h.RowVersion=0x000000000003546E) EarlierSessionUnchanged
 FROM app.PlatformRiderAccounts a WHERE a.Id='49e3f36a-1bcc-41e8-b960-cea041725ffa';
SELECT COUNT(*) RemovedReceiptRemaining FROM jahez_v2.MoneyMovements WHERE Id='01a163a8-0561-7091-9ae5-57febf8c323c';
SELECT SUM(CASE Direction WHEN 1 THEN Amount ELSE -Amount END) CashTotal FROM jahez_v2.MoneyMovements WHERE Fund=1;
SELECT COUNT(*) RetiredCommandKeys FROM jahez_v2.CommandReceipts WHERE PayloadHash='retired:472844:20261022' AND ResultJson='{}';
SELECT Action,OccurredAtUtc,AfterJson FROM audit.AuditEntries WHERE CorrelationId='jahez-remove-472844-20261022';
SELECT name,is_disabled FROM sys.triggers WHERE name='TR_Ownership_Immutable' AND parent_id=OBJECT_ID('jahez_v3.Ownership');
