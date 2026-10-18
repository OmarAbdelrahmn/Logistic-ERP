-- Read-only deployment verification. This script does not change account or upload data.
SET NOCOUNT ON;

IF OBJECT_ID(N'ninja.NinjaAccountUpdateUploads', N'U') IS NULL
    THROW 51000, 'Missing ninja.NinjaAccountUpdateUploads.', 1;
IF OBJECT_ID(N'ninja.NinjaAccountUpdateRows', N'U') IS NULL
    THROW 51000, 'Missing ninja.NinjaAccountUpdateRows.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ninja.NinjaAccountUpdateUploads')
    AND name = N'RowVersion' AND system_type_id = 189)
    THROW 51000, 'Upload rowversion column is missing or has the wrong type.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'ninja.NinjaAccountUpdateRows')
    AND name = N'IX_NinjaAccountUpdateRows_UploadId_CaptainId' AND is_unique = 1)
    THROW 51000, 'Unique upload/Captain ID index is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'ninja.NinjaAccountUpdateRows')
    AND name = N'IX_NinjaAccountUpdateRows_UploadId_SourceRowNumber' AND is_unique = 1)
    THROW 51000, 'Unique upload/source row index is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'ninja.NinjaAccountUpdateRows')
    AND name = N'CK_NinjaAccountUpdateRows_MatchStatus' AND is_disabled = 0 AND is_not_trusted = 0)
    THROW 51000, 'Trusted match status constraint is missing.', 1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN
    (OBJECT_ID(N'ninja.NinjaAccountUpdateUploads'), OBJECT_ID(N'ninja.NinjaAccountUpdateRows'))
    AND delete_referential_action = 0 AND is_disabled = 0 AND is_not_trusted = 0) <> 3
    THROW 51000, 'Expected three restrictive trusted foreign keys.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory
    WHERE MigrationId = N'20261015134151_AddNinjaAccountSheetReviews')
    THROW 51000, 'Migration history entry is missing.', 1;

SELECT N'NinjaAccountUpdateUploads' AS TableName, COUNT_BIG(*) AS Records FROM ninja.NinjaAccountUpdateUploads
UNION ALL
SELECT N'NinjaAccountUpdateRows', COUNT_BIG(*) FROM ninja.NinjaAccountUpdateRows;

SELECT MatchStatus, SheetStatus, COUNT_BIG(*) AS Records
FROM ninja.NinjaAccountUpdateRows
GROUP BY MatchStatus, SheetStatus
ORDER BY MatchStatus, SheetStatus;
