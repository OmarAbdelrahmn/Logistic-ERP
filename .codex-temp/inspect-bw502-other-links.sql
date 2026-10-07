SET NOCOUNT ON;
DECLARE @targets TABLE(Id uniqueidentifier PRIMARY KEY);
INSERT @targets SELECT Id FROM app.FuelCards WHERE Id='01a10b3c-19d1-709b-9b10-20b779f5e71c';
INSERT @targets SELECT Id FROM app.FuelCardRiderAssignments WHERE FuelCardId='01a10b3c-19d1-709b-9b10-20b779f5e71c';
INSERT @targets SELECT Id FROM app.FuelCardMonthlyUsages WHERE FuelCardId='01a10b3c-19d1-709b-9b10-20b779f5e71c';
SELECT N'FleetCommandReceipts' AS TableName,COUNT_BIG(*) AS RelatedRows FROM app.FleetCommandReceipts WHERE ResultEntityId IN(SELECT Id FROM @targets)
UNION ALL SELECT N'Notifications',COUNT_BIG(*) FROM app.Notifications WHERE SourceEntityId IN(SELECT Id FROM @targets) OR DeepLink LIKE N'%01a10b3c-19d1-709b-9b10-20b779f5e71c%'
UNION ALL SELECT N'VehicleOdometerReadings',COUNT_BIG(*) FROM app.VehicleOdometerReadings WHERE SourceEntityId IN(SELECT Id FROM @targets)
UNION ALL SELECT N'VehicleOperationalStatusPeriods',COUNT_BIG(*) FROM app.VehicleOperationalStatusPeriods WHERE SourceEntityId IN(SELECT Id FROM @targets)
UNION ALL SELECT N'ExternalFinancialEntries',COUNT_BIG(*) FROM maintenance.ExternalFinancialEntries WHERE SourceEntityId IN(SELECT Id FROM @targets)
UNION ALL SELECT N'VehicleExpenses',COUNT_BIG(*) FROM maintenance.VehicleExpenses WHERE SourceEntityId IN(SELECT Id FROM @targets)
UNION ALL SELECT N'FuelCardImports errors',COUNT_BIG(*) FROM app.FuelCardImports WHERE RowErrorsJson LIKE N'%BW502%' OR RowErrorsJson LIKE N'%01a10b3c-19d1-709b-9b10-20b779f5e71c%';
SELECT fk.name AS ForeignKeyName,OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,OBJECT_NAME(fk.parent_object_id) AS ChildTable,pc.name AS ChildColumn,OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ParentSchema,OBJECT_NAME(fk.referenced_object_id) AS ParentTable,rc.name AS ParentColumn FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=fk.object_id JOIN sys.columns pc ON pc.object_id=fc.parent_object_id AND pc.column_id=fc.parent_column_id JOIN sys.columns rc ON rc.object_id=fc.referenced_object_id AND rc.column_id=fc.referenced_column_id WHERE OBJECT_NAME(fk.referenced_object_id) IN(N'AuditEntries',N'Notifications',N'FleetCommandReceipts',N'FuelCardRiderAssignments',N'FuelCardMonthlyUsages');
SELECT OBJECT_SCHEMA_NAME(parent_id) AS SchemaName,OBJECT_NAME(parent_id) AS TableName,name AS TriggerName,is_disabled FROM sys.triggers WHERE parent_id IN(OBJECT_ID(N'app.FuelCards'),OBJECT_ID(N'app.FuelCardRiderAssignments'),OBJECT_ID(N'app.FuelCardMonthlyUsages'),OBJECT_ID(N'audit.AuditEntries'));
SELECT COUNT_BIG(*) AS AuditRowsWithOtherCardText FROM audit.AuditEntries WHERE (BeforeJson LIKE N'%BW502%' OR AfterJson LIKE N'%BW502%') AND EntityId NOT IN(SELECT Id FROM @targets);
SELECT COUNT_BIG(*) AS ActiveRiderProfiles FROM app.RiderProfiles WHERE Id='01a09667-03d5-7d3e-8a81-fe929033293f' AND IsDeleted=0;
