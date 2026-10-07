SET NOCOUNT ON;
DECLARE @card uniqueidentifier='01a10b3c-19d1-709b-9b10-20b779f5e71c';
SELECT * FROM app.FuelCards WHERE Id=@card;
SELECT * FROM app.FuelCardRiderAssignments WHERE FuelCardId=@card;
SELECT * FROM app.FuelCardMonthlyUsages WHERE FuelCardId=@card;
SELECT s.name AS SchemaName,t.name AS TableName,c.name AS ColumnName,ty.name AS DataType FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id WHERE c.name LIKE N'%FuelCard%' OR c.name IN(N'EntityId',N'SourceEntityId',N'ResultEntityId',N'AggregateId') OR t.name IN(N'AuditEntries',N'Notifications',N'OperationReceipts') ORDER BY s.name,t.name,c.column_id;
SELECT * FROM audit.AuditEntries WHERE EntityId=@card OR EntityId IN(SELECT Id FROM app.FuelCardRiderAssignments WHERE FuelCardId=@card) OR EntityId IN(SELECT Id FROM app.FuelCardMonthlyUsages WHERE FuelCardId=@card) OR BeforeJson LIKE N'%01a10b3c-19d1-709b-9b10-20b779f5e71c%' OR AfterJson LIKE N'%01a10b3c-19d1-709b-9b10-20b779f5e71c%';
