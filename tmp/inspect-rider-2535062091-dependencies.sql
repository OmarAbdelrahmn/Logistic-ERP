SELECT e.IqamaNo,e.FullNameAr,r.Id RiderProfileId FROM app.Employees e JOIN app.RiderProfiles r ON r.EmployeeId=e.Id WHERE r.Id='01a09667-03c9-76f5-867d-3b9a98165fca';
SELECT COUNT(*) AccountingEntries FROM jahez_v2.AccountingEntries WHERE HandoverId='01a15875-a043-7f1b-8ae9-191fc6779f69';
SELECT SCHEMA_NAME(t.schema_id) SchemaName,t.name TableName,c.name ColumnName FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id WHERE (c.name IN('RiderClientAssignmentId','HandoverId','AccountSessionId','RiderProfileId')) AND (SCHEMA_NAME(t.schema_id) LIKE 'jahez%' OR t.name IN('PlatformAccountDayUsages','RiderAssignmentEvents')) ORDER BY SchemaName,TableName,ColumnName;
SELECT ModuleKey,Version FROM app.DatasetVersions WHERE ModuleKey IN('jahez','platform-operations');
