-- Riyadh opening stock supplied on 2026-10-04: 10 names, 326 pieces.
-- Prices were not supplied; all opening layers have zero cost.
-- Existing car catalog items are reused by exact Arabic name. Other vehicle
-- classifications and similarly spelled names are not changed.
-- Default execution validates all writes and rolls back. Pass @Commit = 1
-- through the SQL command parameter to save. The movement number prevents
-- duplicate imports, including after subsequent stock consumption.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @Commit IS NULL SET @Commit = 0;

DECLARE @locationId uniqueidentifier = '019d77f0-0000-7000-8000-000000000004';
DECLARE @actorId uniqueidentifier = '019c18d5-62e1-7000-c000-000000000001';
DECLARE @movementId uniqueidentifier = '01a10710-279c-740e-add6-4211638c1e7d';
DECLARE @movementNumber varchar(64) = 'OPEN-RIYADH-20261004';
DECLARE @now datetimeoffset(7) = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
DECLARE @rows table (
    RowNumber int PRIMARY KEY,
    NameAr nvarchar(200) NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    ItemId uniqueidentifier NULL,
    LayerId uniqueidentifier NULL,
    LineId uniqueidentifier NULL,
    BalanceId uniqueidentifier NULL,
    Sku varchar(64) NULL
);
INSERT @rows (RowNumber, NameAr, Quantity) VALUES
    (1, N'فلتر هوا', 40),
    (2, N'فحمات خلفيه', 10),
    (3, N'فحمات اماميه', 71),
    (4, N'شمعات يمين', 2),
    (5, N'شمعات يسار', 2),
    (6, N'شنطه سياره', 1),
    (7, N'فلتر زيت', 76),
    (8, N'فلتر مكيف', 33),
    (9, N'لمبات', 90),
    (10, N'ستوب خلفي', 1);

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lockResult int;
    EXEC @lockResult = sys.sp_getapplock
        @Resource = 'OpeningStock:RIYADH:20261004', @LockMode = 'Exclusive',
        @LockOwner = 'Transaction', @LockTimeout = 15000;
    IF @lockResult < 0 THROW 51040, 'Could not acquire the opening stock import lock.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM maintenance.InventoryLocations l WITH (UPDLOCK, HOLDLOCK)
        JOIN maintenance.MaintenanceLocations m ON m.Id = l.MaintenanceLocationId
        WHERE l.Id = @locationId AND l.Code = 'RIYADH_WORKSHOP_STOCK'
            AND l.IsDeleted = 0 AND l.Status = 1 AND m.IsDeleted = 0
            AND m.Status = 1 AND m.InventoryEnabled = 1
            AND m.Id = '019d77f0-0000-7000-8000-000000000002')
        THROW 51041, 'The active Riyadh stock location could not be verified.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].Users WHERE Id = @actorId AND UserName = N'Omar')
        THROW 51042, 'The import audit user could not be verified.', 1;

    IF EXISTS (SELECT 1 FROM maintenance.StockMovements WITH (UPDLOCK, HOLDLOCK)
               WHERE MovementNumber = @movementNumber)
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM maintenance.StockMovements
                       WHERE Id = @movementId AND MovementNumber = @movementNumber
                         AND DestinationLocationId = @locationId AND MovementType = 10
                         AND SourceDocumentType = N'RiyadhOpeningStock20261004')
            THROW 51043, 'The import reference is already used by a different movement.', 1;
        IF (SELECT COUNT(*) FROM maintenance.StockMovementLines WHERE StockMovementId = @movementId) <> 10
            OR EXISTS (
                SELECT 1 FROM @rows r WHERE NOT EXISTS (
                    SELECT 1 FROM maintenance.StockMovementLines ml
                    JOIN maintenance.InventoryItems i ON i.Id = ml.InventoryItemId
                    WHERE ml.StockMovementId = @movementId
                        AND i.NameAr COLLATE Latin1_General_100_BIN2 = r.NameAr COLLATE Latin1_General_100_BIN2
                        AND i.CompatibleVehicleTypesMask = 2 AND ml.Quantity = r.Quantity))
            THROW 51044, 'Existing import lines do not match the supplied list.', 1;
        COMMIT TRANSACTION;
        SELECT 'AlreadyImported' AS ImportStatus, 10 AS ImportedItems, 326 AS ImportedPieces;
        RETURN;
    END;

    IF EXISTS (
        SELECT r.RowNumber FROM @rows r
        JOIN maintenance.InventoryItems i WITH (UPDLOCK, HOLDLOCK)
          ON i.NameAr COLLATE Latin1_General_100_BIN2 = r.NameAr COLLATE Latin1_General_100_BIN2
          AND i.CompatibleVehicleTypesMask = 2 AND i.IsDeleted = 0
        GROUP BY r.RowNumber HAVING COUNT(*) > 1)
        THROW 51045, 'An exact car catalog name has multiple matches.', 1;

    UPDATE r SET ItemId = i.Id, Sku = i.Sku
    FROM @rows r JOIN maintenance.InventoryItems i WITH (UPDLOCK, HOLDLOCK)
      ON i.NameAr COLLATE Latin1_General_100_BIN2 = r.NameAr COLLATE Latin1_General_100_BIN2
      AND i.CompatibleVehicleTypesMask = 2 AND i.IsDeleted = 0;
    IF EXISTS (SELECT 1 FROM @rows r JOIN maintenance.InventoryItems i ON i.Id = r.ItemId
               WHERE i.ItemType <> 1 OR i.BaseUnitOfMeasure <> 1 OR i.Status <> 1
                  OR i.IsSerialized = 1 OR i.IsLotTracked = 1)
        THROW 51046, 'An existing item is incompatible with piece opening stock.', 1;
    IF EXISTS (SELECT 1 FROM @rows r
               JOIN maintenance.StockBalances b WITH (UPDLOCK, HOLDLOCK) ON b.InventoryItemId = r.ItemId
               WHERE b.InventoryLocationId = @locationId AND b.IsDeleted = 0)
        OR EXISTS (SELECT 1 FROM @rows r
                   JOIN maintenance.StockCostLayers l WITH (UPDLOCK, HOLDLOCK) ON l.InventoryItemId = r.ItemId
                   WHERE l.InventoryLocationId = @locationId AND l.IsDeleted = 0)
        OR EXISTS (SELECT 1 FROM @rows r
                   JOIN maintenance.StockMovementLines ml ON ml.InventoryItemId = r.ItemId
                   JOIN maintenance.StockMovements m ON m.Id = ml.StockMovementId
                   WHERE m.SourceLocationId = @locationId OR m.DestinationLocationId = @locationId)
        THROW 51047, 'Existing Riyadh stock or history requires reconciliation before this opening import.', 1;

    DECLARE @createdItems int = (SELECT COUNT(*) FROM @rows WHERE ItemId IS NULL);
    -- UUIDv7 IDs are supplied below by the checked-in staging data.
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f61-721d-a659-24d67401f046'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-01'), LayerId = '01a10712-0f62-76fe-a13e-941eaf96ae88', LineId = '01a10712-0f62-7db4-a837-6dde2c0597f5', BalanceId = '01a10712-0f62-772e-9e4c-ffb4f100fff6' WHERE RowNumber = 1;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-7d9d-82e8-56633fc2916a'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-02'), LayerId = '01a10712-0f63-78c1-9389-7bdfb734ade1', LineId = '01a10712-0f63-7309-95a8-64b1aa762e26', BalanceId = '01a10712-0f63-7ab0-9836-d8ce232d44e5' WHERE RowNumber = 2;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-7aa8-9c99-1e0defa94db1'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-03'), LayerId = '01a10712-0f63-7fe7-81f7-4a7992000e83', LineId = '01a10712-0f63-70c7-a355-843b3e543e55', BalanceId = '01a10712-0f63-7b22-82ab-14d53f549391' WHERE RowNumber = 3;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-76c3-8fef-9f978ae9d023'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-04'), LayerId = '01a10712-0f63-7b2c-be97-6edafc65897a', LineId = '01a10712-0f63-7340-9604-af32a7384536', BalanceId = '01a10712-0f63-703e-aa28-a658cdcddd6d' WHERE RowNumber = 4;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-7889-b701-28cf9d06774b'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-05'), LayerId = '01a10712-0f63-7fb9-a614-52bd83dd81f5', LineId = '01a10712-0f63-70e7-8434-d2b7b4c5e21f', BalanceId = '01a10712-0f63-7fea-8005-ab49038eb85f' WHERE RowNumber = 5;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-7a1c-849b-66ebe4b3b171'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-06'), LayerId = '01a10712-0f63-7745-acb8-72447a9ff3a8', LineId = '01a10712-0f63-747a-88f2-1c7dad8262f4', BalanceId = '01a10712-0f63-765f-bddd-2d54f078404a' WHERE RowNumber = 6;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-7b7f-82e8-177d2460f0d5'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-07'), LayerId = '01a10712-0f63-7f61-a14a-15f647a2a699', LineId = '01a10712-0f63-7042-8e89-d14dd0604727', BalanceId = '01a10712-0f63-7478-921d-1fd4ef59c8a0' WHERE RowNumber = 7;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-71b4-b708-0f2f9916a365'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-08'), LayerId = '01a10712-0f63-75ca-b7f1-e43bf18e27e3', LineId = '01a10712-0f63-7f6a-b03e-a7da0a48e120', BalanceId = '01a10712-0f63-7d6f-af7e-d36e345c5269' WHERE RowNumber = 8;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-7a3e-a82b-684af0d03282'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-09'), LayerId = '01a10712-0f63-7e15-ac87-052c162bf240', LineId = '01a10712-0f63-7bd3-b474-41e71134710a', BalanceId = '01a10712-0f63-720a-b7fd-2f9cf35e43c6' WHERE RowNumber = 9;
    UPDATE @rows SET ItemId = COALESCE(ItemId, '01a10712-0f63-71c1-9fe2-33c63ab4ab52'), Sku = COALESCE(Sku, 'SP-C-RYD-20261004-10'), LayerId = '01a10712-0f63-7ef7-8a98-f8b9eacce27a', LineId = '01a10712-0f63-705f-8911-c7db5df7ecfd', BalanceId = '01a10712-0f63-7d0d-9a93-006173d07d55' WHERE RowNumber = 10;

    INSERT maintenance.InventoryItems
        (Id, Sku, NormalizedSku, ItemType, CompatibleVehicleTypesMask, NameAr, NameEn,
         BaseUnitOfMeasure, PurchaseUnitOfMeasure, MinimumStockLevel, ReorderQuantity,
         IsSerialized, IsLotTracked, Status, CreatedAtUtc, CreatedByUserId, IsDeleted)
    SELECT r.ItemId, r.Sku, UPPER(r.Sku), 1, 2, r.NameAr, r.NameAr,
           1, 1, 0, 0, 0, 0, 1, @now, @actorId, 0
    FROM @rows r WHERE NOT EXISTS (SELECT 1 FROM maintenance.InventoryItems i WHERE i.Id = r.ItemId);

    INSERT maintenance.StockMovements
        (Id, MovementNumber, MovementType, OccurredAtUtc, DestinationLocationId,
         SourceDocumentType, SourceDocumentId, Reason, PostedByUserId, CreatedAtUtc, CreatedByUserId)
    VALUES (@movementId, @movementNumber, 10, @now, @locationId,
            N'RiyadhOpeningStock20261004', @movementId,
            N'رصيد افتتاحي لمستودع الرياض حسب قائمة المستخدم بتاريخ 2026-10-04: 10 أصناف، 326 قطعة. لم يتم تقديم أسعار؛ التكلفة صفر.',
            @actorId, @now, @actorId);
    INSERT maintenance.StockCostLayers
        (Id, InventoryItemId, InventoryLocationId, ReceivedAtUtc, OriginalSequence,
         OriginalQuantity, RemainingQuantity, BaseUnitOfMeasure, UnitCost, OriginalTotalCost,
         CreatedAtUtc, CreatedByUserId, IsDeleted)
    SELECT LayerId, ItemId, @locationId, @now,
           DATEDIFF_BIG(MICROSECOND, '2000-01-01', CAST(@now AS datetime2(7))) + RowNumber,
           Quantity, Quantity, 1, 0, 0, @now, @actorId, 0 FROM @rows;
    INSERT maintenance.StockMovementLines
        (Id, StockMovementId, InventoryItemId, Quantity, BaseUnitOfMeasure,
         CostLayerId, UnitCost, TotalCost, CreatedAtUtc, CreatedByUserId)
    SELECT LineId, @movementId, ItemId, Quantity, 1, LayerId, 0, 0, @now, @actorId FROM @rows;
    UPDATE l SET SourceMovementLineId = r.LineId
    FROM maintenance.StockCostLayers l JOIN @rows r ON r.LayerId = l.Id;
    INSERT maintenance.StockBalances
        (Id, InventoryItemId, InventoryLocationId, QuantityOnHand, QuantityReserved,
         ReportingAverageUnitCost, LastMovementAtUtc, CreatedAtUtc, CreatedByUserId, IsDeleted)
    SELECT BalanceId, ItemId, @locationId, Quantity, 0, 0, @now, @now, @actorId, 0 FROM @rows;

    IF (SELECT COUNT(*) FROM maintenance.StockMovementLines WHERE StockMovementId = @movementId) <> 10
        OR (SELECT SUM(Quantity) FROM maintenance.StockMovementLines WHERE StockMovementId = @movementId) <> 326
        OR EXISTS (
            SELECT 1 FROM @rows r
            LEFT JOIN maintenance.StockBalances b ON b.Id = r.BalanceId
            LEFT JOIN maintenance.StockCostLayers l ON l.Id = r.LayerId
            LEFT JOIN maintenance.StockMovementLines ml ON ml.Id = r.LineId
            LEFT JOIN maintenance.InventoryItems i ON i.Id = r.ItemId
            WHERE b.Id IS NULL OR l.Id IS NULL OR ml.Id IS NULL OR i.Id IS NULL
                OR b.QuantityOnHand <> r.Quantity OR b.QuantityReserved <> 0
                OR l.RemainingQuantity <> r.Quantity OR ml.Quantity <> r.Quantity
                OR l.SourceMovementLineId <> r.LineId OR ml.CostLayerId <> r.LayerId
                OR i.NameAr COLLATE Latin1_General_100_BIN2 <> r.NameAr COLLATE Latin1_General_100_BIN2)
        THROW 51048, 'Opening balances, movement lines, and FIFO layers failed reconciliation.', 1;

    SELECT r.RowNumber, i.NameAr, b.QuantityOnHand, b.ReportingAverageUnitCost, i.Sku
    FROM @rows r JOIN maintenance.InventoryItems i ON i.Id = r.ItemId
    JOIN maintenance.StockBalances b ON b.Id = r.BalanceId ORDER BY r.RowNumber;
    IF @Commit = 1 COMMIT TRANSACTION;
    ELSE ROLLBACK TRANSACTION;
    SELECT CASE WHEN @Commit = 1 THEN 'Imported' ELSE 'ValidatedAndRolledBack' END AS ImportStatus,
           10 AS ImportedItems, 326 AS ImportedPieces, @createdItems AS CreatedCatalogItems;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;


