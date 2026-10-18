SELECT Id,PlateNumberAr,NormalizedPlateNumberAr,PlateNumberEn,PlateDigits,VehicleType,CurrentOdometer FROM app.Vehicles
WHERE IsDeleted=0 AND PlateDigits IN('1027','1389','1423','1609','2362','2986','3048','5811','6162') ORDER BY PlateDigits;
SELECT v.PlateNumberAr,o.* FROM app.Vehicles v CROSS APPLY(SELECT TOP(4) * FROM maintenance.OilChangeOperations o WHERE o.VehicleId=v.Id ORDER BY o.PerformedAtUtc DESC,o.Id DESC) o
WHERE v.IsDeleted=0 AND v.PlateDigits IN('1027','1389','1423','1609','2362','2986','3048','5811','6162') ORDER BY v.PlateDigits,o.PerformedAtUtc DESC,o.Id DESC;
SELECT OBJECT_SCHEMA_NAME(f.parent_object_id) SchemaName,OBJECT_NAME(f.parent_object_id) TableName,c.name ColumnName,
OBJECT_SCHEMA_NAME(f.referenced_object_id) ReferencedSchema,OBJECT_NAME(f.referenced_object_id) ReferencedTable
FROM sys.foreign_key_columns f JOIN sys.columns c ON c.object_id=f.parent_object_id AND c.column_id=f.parent_column_id
WHERE OBJECT_NAME(f.referenced_object_id) IN('OilChangeOperations','MaterialUsages','StockMovements','StockMovementLines','StockCostAllocations','OilBarrelUsageAllocations');
