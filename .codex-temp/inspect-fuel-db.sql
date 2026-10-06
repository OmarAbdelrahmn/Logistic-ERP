SELECT DB_NAME() AS DatabaseName;
SELECT c.Id,c.CardNumber,c.NormalizedCardNumber,c.Provider,c.OperatingCityId,c.SponsorId,c.IsDeleted FROM app.FuelCards c WHERE c.IsDeleted=0;
SELECT e.Id,e.IqamaNo,e.FullNameAr,e.FullNameEn,e.IsEmployee,e.Status,r.Id AS RiderProfileId,r.IsDeleted AS RiderDeleted FROM app.Employees e LEFT JOIN app.RiderProfiles r ON r.EmployeeId=e.Id WHERE e.IsDeleted=0;
SELECT a.Id,a.FuelCardId,a.RiderProfileId,a.EmployeeId,a.EffectiveFrom,a.EffectiveTo FROM app.FuelCardRiderAssignments a;
SELECT FuelCardId,RiderProfileId,EmployeeId,ReportMonth FROM app.FuelCardMonthlyUsages WHERE IsDeleted=0;
SELECT COLUMN_NAME,DATA_TYPE,IS_NULLABLE,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='app' AND TABLE_NAME='FuelCardRiderAssignments';
SELECT Id,UserName,IsDeleted,IsDevelopmentOnly FROM [identity].Users WHERE UserName=N'Omar';
