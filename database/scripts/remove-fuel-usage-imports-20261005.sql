-- Remove the two October monthly usage imports and the cards created by the first import.
-- Existing audit entries remain as evidence of the original operations.
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @firstImportId uniqueidentifier = '01a10b8a-ef87-7dfc-82c3-431b39cd2430';
    DECLARE @secondImportId uniqueidentifier = '01a10bce-e4fe-7686-9abe-06580ebd772f';
    DECLARE @deletedUsageRows int;
    DECLARE @deletedCards int;
    DECLARE @deletedImports int;

    IF (SELECT COUNT(*) FROM [app].[FuelCardImports]
        WHERE [Id] IN (@firstImportId, @secondImportId)) <> 2
        THROW 51000, 'Expected two fuel usage imports; no data was removed.', 1;

    IF NOT EXISTS (SELECT 1 FROM [app].[FuelCardImports]
        WHERE [Id] = @firstImportId AND [CreatedCards] = 22)
        OR NOT EXISTS (SELECT 1 FROM [app].[FuelCardImports]
        WHERE [Id] = @secondImportId AND [CreatedCards] = 0)
        THROW 51000, 'Fuel import creation counts changed; no data was removed.', 1;

    SELECT card.[Id]
    INTO #usageCreatedCards
    FROM [app].[FuelCards] AS card
    JOIN [app].[FuelCardImports] AS fuelImport
        ON fuelImport.[Id] IN (@firstImportId, @secondImportId)
        AND card.[Provider] = fuelImport.[Provider]
        AND card.[CreatedAtUtc] = fuelImport.[CreatedAtUtc]
        AND card.[CreatedByUserId] = fuelImport.[CreatedByUserId];

    IF (SELECT COUNT(*) FROM #usageCreatedCards) <> 22
        THROW 51000, 'Fuel cards created by the imports no longer match the expected 22; no data was removed.', 1;

    IF EXISTS (SELECT 1 FROM [app].[FuelCards] AS card
        JOIN #usageCreatedCards AS candidate ON candidate.[Id] = card.[Id]
        WHERE card.[UpdatedAtUtc] IS NOT NULL OR card.[IsDeleted] = 1)
        THROW 51000, 'An imported card was subsequently changed; no data was removed.', 1;

    IF EXISTS (SELECT 1 FROM [app].[FuelCardMonthlyUsages] AS usage
        JOIN #usageCreatedCards AS candidate ON candidate.[Id] = usage.[FuelCardId])
        OR EXISTS (SELECT 1 FROM [app].[FuelCardRiderAssignments] AS assignment
        JOIN #usageCreatedCards AS candidate ON candidate.[Id] = assignment.[FuelCardId])
        THROW 51000, 'An imported card has usage or a rider assignment; no data was removed.', 1;

    IF (SELECT COUNT(*) FROM [app].[FuelCardMonthlyUsages]
        WHERE [LastImportId] IN (@firstImportId, @secondImportId)) <> 47
        OR (SELECT COALESCE(SUM([TotalAmount]), 0) FROM [app].[FuelCardMonthlyUsages]
            WHERE [LastImportId] IN (@firstImportId, @secondImportId)) <> 1715.00
        THROW 51000, 'Usage linked to the imports changed; no data was removed.', 1;

    DELETE FROM [app].[FuelCardMonthlyUsages]
    WHERE [LastImportId] IN (@firstImportId, @secondImportId);
    SET @deletedUsageRows = @@ROWCOUNT;

    DELETE card
    FROM [app].[FuelCards] AS card
    JOIN #usageCreatedCards AS candidate ON candidate.[Id] = card.[Id];
    SET @deletedCards = @@ROWCOUNT;

    DELETE FROM [app].[FuelCardImports]
    WHERE [Id] IN (@firstImportId, @secondImportId);
    SET @deletedImports = @@ROWCOUNT;

    IF @deletedUsageRows <> 47 OR @deletedCards <> 22 OR @deletedImports <> 2
        THROW 51000, 'Fuel cleanup row counts changed; all deletions were rolled back.', 1;

    COMMIT TRANSACTION;

    SELECT @deletedUsageRows AS [DeletedUsageRows],
        @deletedCards AS [DeletedCards],
        @deletedImports AS [DeletedImports];
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
