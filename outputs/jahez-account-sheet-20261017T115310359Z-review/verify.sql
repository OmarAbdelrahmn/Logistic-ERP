SET NOCOUNT ON;
WITH S AS(SELECT * FROM OPENJSON(@PlanJson,'$.accounts') WITH
 (Id uniqueidentifier '$.accountId',ExternalId varchar(100) '$.account',OwnerId uniqueidentifier '$.ownerId',OwnerIqama varchar(10) '$.ownerIqama',
 SponsorId uniqueidentifier '$.sponsorId',SponsorNumber varchar(20) '$.sponsorNumber',DesiredStatus int '$.desiredStatus'))
SELECT 'Account owner, sponsor number and status' CheckName,COUNT(*) Mismatches FROM S
 LEFT JOIN app.PlatformRiderAccounts a ON a.Id=S.Id LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId LEFT JOIN app.Sponsors p ON p.Id=a.SponsorId
 WHERE a.Id IS NULL OR a.ExternalAccountId<>S.ExternalId OR a.RegisteredEmployeeId<>S.OwnerId OR a.RegisteredEmployeeId IS NULL
 OR e.IqamaNo<>S.OwnerIqama OR a.SponsorId<>S.SponsorId OR a.SponsorId IS NULL OR p.EmployerIdentityNumber<>S.SponsorNumber OR a.Status<>S.DesiredStatus OR a.IsDeleted=1
UNION ALL
SELECT 'New owner rider profiles',COUNT(*) FROM OPENJSON(@PlanJson,'$.profiles') WITH(Id uniqueidentifier '$.id',EmployeeId uniqueidentifier '$.employeeId') s
 LEFT JOIN app.RiderProfiles r ON r.Id=s.Id WHERE r.Id IS NULL OR r.EmployeeId<>s.EmployeeId OR r.IsDeleted=1
UNION ALL
SELECT 'Rider, assignment and both clearance dates',COUNT(*) FROM OPENJSON(@PlanJson,'$.insertAssignments') WITH
 (Id uniqueidentifier '$.id',SessionId uniqueidentifier '$.sessionId',AccountId uniqueidentifier '$.accountId',RiderId uniqueidentifier '$.riderId',
 RiderIqama varchar(10) '$.riderIqama',FromDate date '$.fromDate',Slot int '$.slot') s
 LEFT JOIN app.RiderClientAssignments a ON a.Id=s.Id LEFT JOIN jahez_v2.AccountSessions h ON h.Id=s.SessionId
 LEFT JOIN app.RiderProfiles r ON r.Id=a.RiderProfileId LEFT JOIN app.Employees e ON e.Id=r.EmployeeId
 WHERE a.Id IS NULL OR h.Id IS NULL OR a.PlatformRiderAccountId<>s.AccountId OR a.RiderProfileId<>s.RiderId
 OR a.EffectiveFrom<>s.FromDate OR a.RiderAccountSlot<>s.Slot OR e.IqamaNo<>s.RiderIqama OR a.Status<>2 OR a.EffectiveTo IS NOT NULL
 OR h.RiderClientAssignmentId<>a.Id OR h.PlatformRiderAccountId<>a.PlatformRiderAccountId OR h.RiderProfileId<>a.RiderProfileId
 OR h.StartedAtUtc<>TODATETIMEOFFSET(CONVERT(datetime2,s.FromDate),'+03:00') OR h.LastSettlementPaymentAtUtc IS NULL
 OR h.LastSettlementPaymentAtUtc<>h.StartedAtUtc OR h.CommissionStartsOn<>DATEADD(day,1,s.FromDate) OR h.EndedAtUtc IS NOT NULL
UNION ALL
SELECT 'Vehicle serial links',COUNT(*) FROM OPENJSON(@PlanJson,'$.vehicleLinks') WITH
 (Id uniqueidentifier '$.id',AccountId uniqueidentifier '$.accountId',VehicleId uniqueidentifier '$.vehicleId',Serial varchar(100) '$.serial') s
 LEFT JOIN app.VehiclePlatformAccountAssignments a ON a.Id=s.Id LEFT JOIN app.Vehicles v ON v.Id=a.VehicleId
 WHERE a.Id IS NULL OR a.VehicleId<>s.VehicleId OR a.PlatformRiderAccountId<>s.AccountId OR v.SerialNumber<>s.Serial OR a.Status<>1 OR a.EndedAtUtc IS NOT NULL OR a.IsDeleted=1
UNION ALL
SELECT 'Two account capacity for imported riders',COUNT(*) FROM(SELECT a.RiderProfileId FROM app.RiderClientAssignments a
 WHERE a.EffectiveTo IS NULL AND a.IsDeleted=0 AND a.RiderProfileId IN(SELECT RiderId FROM OPENJSON(@PlanJson,'$.insertAssignments') WITH(RiderId uniqueidentifier '$.riderId'))
 GROUP BY a.RiderProfileId HAVING COUNT(*)>2) x
UNION ALL
SELECT 'No imported fee charges or receipts',COUNT(*) FROM jahez_v2.AccountSessions h
 WHERE h.Id IN(SELECT Id FROM OPENJSON(@PlanJson,'$.insertAssignments') WITH(Id uniqueidentifier '$.sessionId'))
 AND (EXISTS(SELECT 1 FROM jahez_v2.AccountFees f WHERE f.HandoverId=h.Id)
 OR EXISTS(SELECT 1 FROM jahez_v2.AccountingEntries f WHERE f.HandoverId=h.Id)
 OR EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements f WHERE f.HandoverId=h.Id))
UNION ALL
SELECT 'Import audit record',ABS(1-COUNT(*)) FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId;
