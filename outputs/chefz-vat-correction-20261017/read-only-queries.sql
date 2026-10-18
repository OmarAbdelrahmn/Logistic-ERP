SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN TRANSACTION;
SELECT DB_NAME() DatabaseName,SYSUTCDATETIME() ReadAtUtc;
SELECT Id,FileName,ContentHash,SourceRows,CreatedAtUtc FROM chefz.OrderSettlementImports;
SELECT s.* FROM chefz.WalletSettlements s WHERE s.PeriodFrom >= '20261001' AND s.PeriodFrom < '20261101';
SELECT d.PlatformRiderAccountId,d.ReportIdNumber,d.AccountType,d.IsDeleted,a.ExternalAccountId,a.IsDeleted AccountDeleted
 FROM chefz.AccountDetails d JOIN app.PlatformRiderAccounts a ON a.Id=d.PlatformRiderAccountId;
SELECT a.Id,a.PlatformRiderAccountId,a.RiderProfileId,a.EffectiveFrom,a.EffectiveTo,a.Status,a.IsDeleted,e.FullNameAr RiderName
 FROM app.RiderClientAssignments a JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.PlatformRiderAccountId
 LEFT JOIN app.RiderProfiles r ON r.Id=a.RiderProfileId LEFT JOIN app.Employees e ON e.Id=r.EmployeeId
 WHERE a.EffectiveFrom <= '20261031' AND (a.EffectiveTo IS NULL OR a.EffectiveTo >= '20261001');
SELECT name,definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('chefz.WalletSettlements');
SELECT COUNT(*) InvalidArithmetic FROM chefz.WalletSettlements WHERE CompanyAmount+RiderAmount<>GrossEarnings
 OR (Source='Orders' AND GrossEarnings<>DeliveryFees-VatAmount+Tips+Compensation+Bonuses-Deductions);
SELECT RiderClientAssignmentId,PeriodFrom,COUNT(*) N FROM chefz.WalletSettlements GROUP BY RiderClientAssignmentId,PeriodFrom HAVING COUNT(*)>1;
SELECT Id,RiderClientAssignmentId,OccurredAtUtc,Reason,ChangeSnapshotJson FROM app.RiderAssignmentEvents
 WHERE RiderClientAssignmentId='ce52e580-07b7-4476-9711-8a48db7039bb' ORDER BY OccurredAtUtc;
SELECT Id,Action,OccurredAtUtc,Reason,BeforeJson,AfterJson FROM audit.AuditEntries
 WHERE EntityId='ce52e580-07b7-4476-9711-8a48db7039bb' ORDER BY OccurredAtUtc;
COMMIT TRANSACTION;
