SELECT h.* FROM app.Housing h WHERE h.IsDeleted=0;
SELECT f.* FROM app.HousingFloors f JOIN app.Housing h ON h.Id=f.HousingId WHERE h.IsDeleted=0 AND h.CityId='019c18d5-62e1-7000-8000-000000000002';
SELECT r.* FROM app.HousingRooms r JOIN app.Housing h ON h.Id=r.HousingId WHERE h.IsDeleted=0 AND h.CityId='019c18d5-62e1-7000-8000-000000000002';
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('Employees','EmployeeResidencyPermits','HousingResidencePeriods','HousingEquipment','HousingPendingOccupants','HousingExternalOccupants','AuditEntries','RiderProfiles') ORDER BY TABLE_NAME,ORDINAL_POSITION;
