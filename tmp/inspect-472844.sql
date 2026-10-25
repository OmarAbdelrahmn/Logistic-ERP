SELECT a.Id,a.ExternalAccountId,p.Code,a.Status,a.RowVersion FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE ExternalAccountId='472844' AND a.IsDeleted=0;
DECLARE @a uniqueidentifier=(SELECT Id FROM app.PlatformRiderAccounts WHERE ExternalAccountId='472844' AND IsDeleted=0);
SELECT * FROM app.RiderClientAssignments WHERE PlatformRiderAccountId=@a;
SELECT * FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId=@a;
SELECT * FROM jahez_v2.AccountFees WHERE HandoverId IN(SELECT Id FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId=@a);
SELECT * FROM jahez_v2.MoneyMovements WHERE HandoverId IN(SELECT Id FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId=@a);
SELECT * FROM jahez_v2.AccountingEntries WHERE HandoverId IN(SELECT Id FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId=@a);
SELECT * FROM jahez_v2.Operations WHERE HandoverId IN(SELECT Id FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId=@a);
SELECT SCHEMA_NAME(t.schema_id) SchemaName,t.name TableName,c.name ColumnName FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id WHERE c.name IN('HandoverId','RiderClientAssignmentId','PlatformRiderAccountId','MoneyMovementId','OperationId') ORDER BY 1,2;
