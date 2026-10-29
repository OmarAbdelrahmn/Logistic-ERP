SELECT i.Id,i.ReportDate,i.FileName,i.ImportedRows,i.SkippedRows,i.CreatedAtUtc,i.CreatedByUserId,
 u.UserName,u.DisplayNameAr
FROM ninja.NinjaDailyImports i LEFT JOIN [identity].Users u ON u.Id=i.CreatedByUserId
WHERE i.ReportDate='20261026' AND i.IsDeductions=0;
SELECT 'Performance' TableName,COUNT(*) Rows FROM ninja.NinjaDailyPerformance WHERE ReportDate='20261026'
UNION ALL SELECT 'Sources',COUNT(*) FROM ninja.NinjaDailyPerformanceSources WHERE ReportDate='20261026'
UNION ALL SELECT 'Deductions',COUNT(*) FROM ninja.NinjaDailyDeductions WHERE ReportDate='20261026';
SELECT SCHEMA_NAME(t.schema_id) SchemaName,t.name TableName,c.name ColumnName,TYPE_NAME(c.user_type_id) TypeName
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id
WHERE (t.name LIKE '%DayUsage%' OR t.name='DatasetVersions') ORDER BY t.name,c.column_id;
SELECT fk.name,OBJECT_SCHEMA_NAME(fk.parent_object_id) ChildSchema,OBJECT_NAME(fk.parent_object_id) ChildTable,
 COL_NAME(fc.parent_object_id,fc.parent_column_id) ChildColumn,
 OBJECT_SCHEMA_NAME(fk.referenced_object_id) ParentSchema,OBJECT_NAME(fk.referenced_object_id) ParentTable,
 fk.delete_referential_action_desc
FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=fk.object_id
WHERE fk.referenced_object_id IN(OBJECT_ID('ninja.NinjaDailyImports'),OBJECT_ID('ninja.NinjaDailyPerformanceSources'),OBJECT_ID('ninja.NinjaDailyPerformance'));
SELECT OBJECT_SCHEMA_NAME(parent_id) SchemaName,OBJECT_NAME(parent_id) TableName,name,is_disabled
FROM sys.triggers WHERE parent_id IN(OBJECT_ID('ninja.NinjaDailyImports'),OBJECT_ID('ninja.NinjaDailyPerformanceSources'),OBJECT_ID('ninja.NinjaDailyPerformance'),OBJECT_ID('ninja.AllocationProvenance'),OBJECT_ID('ninja.PerformanceImportArtifacts'));
SELECT * FROM app.DatasetVersions WHERE ModuleKey LIKE '%ninja%' OR ModuleKey LIKE '%platform%';
