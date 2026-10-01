-- Completes the three iqamas missing from the database at the initial Riyadh import.
-- Safe to rerun: a pending row is only created while the iqama remains unmatched.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

CREATE TABLE #Pending ([Code] nvarchar(32), [Room] nvarchar(100), [Iqama] nvarchar(20), [Name] nvarchar(200), [SourceRow] int);
INSERT INTO #Pending VALUES
    (N'الرياض النسيم', N'2', N'2643584044', N'عريف مريضة', 41),
    (N'RYD-NASEEM-2', N'4', N'2588987777', N'علاء بدوى مصطفى', 70),
    (N'RYD-NASEEM-3', N'1', N'2571787197', N'ابراهيم جمعه يوسف جمعه', 82);

IF EXISTS (
    SELECT 1 FROM #Pending p
    LEFT JOIN [app].[Housing] h ON h.[Code] = p.[Code] AND h.[IsDeleted] = 0
    LEFT JOIN [app].[HousingRooms] r ON r.[HousingId] = h.[Id] AND r.[Name] = p.[Room] AND r.[IsDeleted] = 0
    WHERE r.[Id] IS NULL)
    THROW 51006, 'A target Riyadh room was not found.', 1;

INSERT INTO [app].[HousingPendingOccupants]
    ([Id], [RoomId], [IqamaNo], [Name], [SourceRow], [CreatedAtUtc], [IsDeleted])
SELECT NEWID(), r.[Id], p.[Iqama], p.[Name], p.[SourceRow], SYSUTCDATETIME(), 0
FROM #Pending p
JOIN [app].[Housing] h ON h.[Code] = p.[Code]
JOIN [app].[HousingRooms] r ON r.[HousingId] = h.[Id] AND r.[Name] = p.[Room] AND r.[IsDeleted] = 0
WHERE NOT EXISTS (SELECT 1 FROM [app].[Employees] e WHERE e.[IqamaNo] = p.[Iqama] AND e.[IsDeleted] = 0)
  AND NOT EXISTS (SELECT 1 FROM [app].[HousingPendingOccupants] x WHERE x.[IqamaNo] = p.[Iqama] AND x.[IsDeleted] = 0);

UPDATE r SET [CurrentOccupancy] = x.[Occupants]
FROM [app].[HousingRooms] r
JOIN [app].[Housing] h ON h.[Id] = r.[HousingId]
JOIN #Pending p ON p.[Code] = h.[Code] AND p.[Room] = r.[Name]
CROSS APPLY (SELECT
    (SELECT COUNT(*) FROM [app].[HousingResidencePeriods] q WHERE q.[RoomId] = r.[Id] AND q.[EffectiveTo] IS NULL) +
    (SELECT COUNT(*) FROM [app].[HousingExternalOccupants] e WHERE e.[RoomId] = r.[Id] AND e.[IsDeleted] = 0) +
    (SELECT COUNT(*) FROM [app].[HousingPendingOccupants] u WHERE u.[RoomId] = r.[Id] AND u.[IsDeleted] = 0) AS [Occupants]) x;

IF EXISTS (SELECT 1 FROM [app].[HousingRooms] r WHERE r.[CurrentOccupancy] > r.[Capacity])
    THROW 51007, 'Pending occupant exceeds room capacity.', 1;
COMMIT TRANSACTION;
SELECT h.[NameAr], r.[Name] AS [Room], p.[IqamaNo], p.[Name]
FROM [app].[HousingPendingOccupants] p
JOIN [app].[HousingRooms] r ON r.[Id] = p.[RoomId]
JOIN [app].[Housing] h ON h.[Id] = r.[HousingId]
WHERE p.[IsDeleted] = 0 AND p.[SourceRow] IN (41, 70, 82);
