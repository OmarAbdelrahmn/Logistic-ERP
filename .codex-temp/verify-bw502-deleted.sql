SET NOCOUNT ON;
DECLARE @card uniqueidentifier='01a10b3c-19d1-709b-9b10-20b779f5e71c';
DECLARE @assignment uniqueidentifier='01a10ca5-7a1c-7038-8cd5-b594bcc5e0a3';
SELECT DB_NAME() AS DatabaseName,
 (SELECT COUNT(*) FROM app.FuelCards WHERE Id=@card OR NormalizedCardNumber=N'BW502' OR CardNumber=N'BW502') AS RemainingCards,
 (SELECT COUNT(*) FROM app.FuelCardRiderAssignments WHERE FuelCardId=@card OR Id=@assignment) AS RemainingAssignments,
 (SELECT COUNT(*) FROM app.FuelCardMonthlyUsages WHERE FuelCardId=@card) AS RemainingMonthlyUsages,
 (SELECT COUNT(*) FROM audit.AuditEntries WHERE EntityId IN(@card,@assignment)) AS RemainingDirectAuditEntries,
 (SELECT COUNT(*) FROM app.RiderProfiles WHERE Id='01a09667-03d5-7d3e-8a81-fe929033293f' AND IsDeleted=0) AS RiderProfilesPreserved,
 (SELECT COUNT(*) FROM app.Employees WHERE Id='01a09667-03d5-7b42-9a9f-2b64b16ba1fe' AND IsDeleted=0) AS EmployeesPreserved;
