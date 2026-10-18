IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions')
    THROW 51000, 'Ninja migration history is missing.', 1;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('ninja') AND name IN
    ('NinjaAccountDetails','NinjaDailyImports','NinjaDailyPerformance','NinjaDailyDeductions')) <> 4
    THROW 51000, 'Ninja tables are incomplete.', 1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE SCHEMA_NAME(schema_id)='ninja' AND is_disabled=0 AND is_not_trusted=0) <> 9
    THROW 51000, 'Ninja foreign keys are missing, disabled or untrusted.', 1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id IN
    (OBJECT_ID('ninja.NinjaDailyPerformance'),OBJECT_ID('ninja.NinjaDailyDeductions')) AND is_disabled=0 AND is_not_trusted=0) <> 2
    THROW 51000, 'Ninja value constraints are missing, disabled or untrusted.', 1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE name IN
    ('IX_NinjaAccountDetails_PlatformRiderAccountId','IX_NinjaDailyImports_ReportDate_IsDeductions_ContentHash',
     'IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate','IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate')
    AND is_unique=1 AND is_disabled=0) <> 4
    THROW 51000, 'Ninja uniqueness indexes are incomplete.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ninja.NinjaAccountDetails') AND name='RowVersion' AND TYPE_NAME(user_type_id)='timestamp')
    THROW 51000, 'Ninja account details rowversion is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('app.PlatformRiderAccounts') AND name='RowVersion' AND TYPE_NAME(user_type_id)='timestamp')
    THROW 51000, 'Shared account rowversion is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ninja.NinjaDailyPerformance') AND name='TotalWorkingHours' AND precision=18 AND scale=8)
    THROW 51000, 'Ninja hours precision is incorrect.', 1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('ninja.NinjaDailyDeductions') AND name IN ('TotalDeductions','TotalCollectedAmountAtDelivery') AND precision=18 AND scale=2) <> 2
    THROW 51000, 'Ninja money precision is incorrect.', 1;

SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions';
SELECT OBJECT_SCHEMA_NAME(object_id) SchemaName,OBJECT_NAME(object_id) TableName,name ColumnName,TYPE_NAME(user_type_id) SqlType,max_length,precision,scale,is_nullable
    FROM sys.columns WHERE object_id IN (OBJECT_ID('ninja.NinjaAccountDetails'),OBJECT_ID('ninja.NinjaDailyImports'),OBJECT_ID('ninja.NinjaDailyPerformance'),OBJECT_ID('ninja.NinjaDailyDeductions')) ORDER BY TableName,column_id;
SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints WHERE SCHEMA_NAME(schema_id)='ninja' ORDER BY name;
SELECT OBJECT_NAME(object_id) TableName,name,is_unique,is_disabled FROM sys.indexes WHERE object_id IN
    (OBJECT_ID('ninja.NinjaAccountDetails'),OBJECT_ID('ninja.NinjaDailyImports'),OBJECT_ID('ninja.NinjaDailyPerformance'),OBJECT_ID('ninja.NinjaDailyDeductions')) AND name IS NOT NULL ORDER BY TableName,name;
SELECT name,is_disabled,is_not_trusted FROM sys.foreign_keys WHERE SCHEMA_NAME(schema_id)='ninja' ORDER BY name;
SELECT 'NinjaAccountDetails' TableName,COUNT_BIG(*) RecordCount FROM ninja.NinjaAccountDetails
UNION ALL SELECT 'NinjaDailyImports',COUNT_BIG(*) FROM ninja.NinjaDailyImports
UNION ALL SELECT 'NinjaDailyPerformance',COUNT_BIG(*) FROM ninja.NinjaDailyPerformance
UNION ALL SELECT 'NinjaDailyDeductions',COUNT_BIG(*) FROM ninja.NinjaDailyDeductions;
SELECT SYSUTCDATETIME() VerifiedAtUtc;
