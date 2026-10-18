-- Read-only verification; may run inside the transaction or on a fresh connection.
DECLARE @Platform uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId'),@Contract uniqueidentifier=JSON_VALUE(@PlanJson,'$.contractId'),
 @Actor uniqueidentifier=JSON_VALUE(@PlanJson,'$.actorId'),@From date=JSON_VALUE(@PlanJson,'$.effectiveFrom');
SELECT * INTO #VerifySource FROM OPENJSON(@PlanJson,'$.rows') WITH(
 ExternalId nvarchar(150) '$.externalId',OwnerIqama nvarchar(10) '$.ownerIqama',UserIqama nvarchar(10) '$.userIqama',
 AccountAction varchar(12) '$.accountAction',AssignmentAction varchar(20) '$.assignmentAction',
 AccountId uniqueidentifier '$.accountId',DetailsId uniqueidentifier '$.detailsId',OwnerEmployeeId uniqueidentifier '$.ownerEmployeeId',OwnerRiderId uniqueidentifier '$.ownerRiderId',
 OperatingCityId uniqueidentifier '$.operatingCityId',SponsorId uniqueidentifier '$.sponsorId',Code nvarchar(32) '$.code',PaymentModel int '$.paymentModel',FinalStatus int '$.finalStatus',
 Phone nvarchar(32) '$.savedPhone',OwnershipNotes nvarchar(4000) '$.ownershipNotes',Notes nvarchar(4000) '$.notes',StatusReason nvarchar(500) '$.statusReason',
 RiderId uniqueidentifier '$.riderId',AssignmentId uniqueidentifier '$.assignmentId',RegistrationId uniqueidentifier '$.registrationId',EventId uniqueidentifier '$.eventId');
SELECT 'Account count' CheckName,ABS(COUNT(*)-301) Mismatches FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@Platform
UNION ALL SELECT 'Account values',COUNT(*) FROM #VerifySource s LEFT JOIN app.PlatformRiderAccounts a ON a.Id=s.AccountId
 WHERE s.AccountAction='Add' AND (a.Id IS NULL OR EXISTS(
 SELECT @Platform,s.ExternalId,s.Code,s.OwnerEmployeeId,s.OperatingCityId,s.SponsorId,s.PaymentModel,s.FinalStatus,
 s.OwnershipNotes,s.Notes,CASE WHEN s.AssignmentAction='Assign' THEN NULL ELSE s.StatusReason END,CONVERT(bit,0),@Actor
 EXCEPT SELECT a.ClientPlatformId,a.ExternalAccountId,a.Code,a.RegisteredEmployeeId,a.OperatingCityId,a.SponsorId,a.PaymentModel,a.Status,
 a.OwnershipNotes,a.OperationalNotes,a.StatusReason,a.IsDeleted,a.CreatedByUserId))
UNION ALL SELECT 'Owner iqama links',COUNT(*) FROM #VerifySource s LEFT JOIN app.Employees e ON e.Id=s.OwnerEmployeeId
 LEFT JOIN app.RiderProfiles p ON p.Id=s.OwnerRiderId
 WHERE s.AccountAction='Add' AND (e.Id IS NULL OR e.IqamaNo<>s.OwnerIqama OR e.IsDeleted<>0 OR e.IsEmployee<>0 OR p.Id IS NULL OR p.EmployeeId<>e.Id OR p.IsDeleted<>0)
UNION ALL SELECT 'Phone/detail values',COUNT(*) FROM #VerifySource s LEFT JOIN ninja.NinjaAccountDetails d ON d.Id=s.DetailsId
 WHERE s.AccountAction='Add' AND (d.Id IS NULL OR EXISTS(SELECT s.AccountId,s.Phone,CONVERT(bit,0),@Actor EXCEPT SELECT d.PlatformRiderAccountId,d.PhoneNumber,d.IsDeleted,d.CreatedByUserId))
UNION ALL SELECT 'Detail count',ABS(COUNT(*)-301) FROM ninja.NinjaAccountDetails d JOIN app.PlatformRiderAccounts a ON a.Id=d.PlatformRiderAccountId WHERE a.ClientPlatformId=@Platform
UNION ALL SELECT 'Assignment count',ABS(COUNT(*)-107) FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId WHERE a.ClientPlatformId=@Platform
UNION ALL SELECT 'Assignment values',COUNT(*) FROM #VerifySource s LEFT JOIN app.RiderClientAssignments r ON r.Id=s.AssignmentId
 WHERE s.AssignmentAction='Assign' AND (r.Id IS NULL OR EXISTS(
 SELECT s.AccountId,s.RiderId,@Contract,@From,CONVERT(date,NULL),2,1,1,@Actor,CONVERT(bit,0),CONVERT(bit,0)
 EXCEPT SELECT r.PlatformRiderAccountId,r.RiderProfileId,r.ClientContractId,r.EffectiveFrom,r.EffectiveTo,r.Status,r.PaymentModel,r.RiderAccountSlot,r.AssignedByUserId,r.WasBackdated,r.IsDeleted))
UNION ALL SELECT 'User iqama links',COUNT(*) FROM #VerifySource s LEFT JOIN app.RiderProfiles p ON p.Id=s.RiderId
 LEFT JOIN app.Employees e ON e.Id=p.EmployeeId
 WHERE s.AssignmentAction='Assign' AND (p.Id IS NULL OR p.IsDeleted<>0 OR e.Id IS NULL OR e.IqamaNo<>s.UserIqama OR e.IsDeleted<>0 OR e.Status<>3 OR e.IsEmployee<>0)
UNION ALL SELECT 'Ownership registration values',COUNT(*) FROM #VerifySource s LEFT JOIN app.PlatformAccountRegistrations r ON r.Id=s.RegistrationId
 WHERE s.AssignmentAction='Assign' AND (r.Id IS NULL OR EXISTS(
 SELECT s.AccountId,s.OwnerEmployeeId,s.OwnerRiderId,@Platform,@Contract,s.SponsorId,s.OperatingCityId,1,4,CONVERT(bit,0)
 EXCEPT SELECT r.PlatformRiderAccountId,r.RegisteredEmployeeId,r.RiderProfileId,r.ClientPlatformId,r.ClientContractId,r.SponsorId,r.OperatingCityId,r.RegistrationType,r.Status,r.IsDeleted))
UNION ALL SELECT 'Registration count',ABS(COUNT(*)-107) FROM app.PlatformAccountRegistrations WHERE ClientPlatformId=@Platform
UNION ALL SELECT 'Assignment events',COUNT(*) FROM #VerifySource s LEFT JOIN app.RiderAssignmentEvents e ON e.Id=s.EventId
 WHERE s.AssignmentAction='Assign' AND (e.Id IS NULL OR e.RiderClientAssignmentId<>s.AssignmentId OR e.FromStatus<>1 OR e.ToStatus<>2 OR e.ActorUserId<>@Actor)
UNION ALL SELECT 'Skipped accounts absent',COUNT(*) FROM #VerifySource s JOIN app.PlatformRiderAccounts a ON a.ClientPlatformId=@Platform AND a.ExternalAccountId=s.ExternalId WHERE s.AccountAction='Skip'
UNION ALL SELECT 'Non-assigned rows remain unassigned',COUNT(*) FROM #VerifySource s JOIN app.RiderClientAssignments r ON r.PlatformRiderAccountId=s.AccountId WHERE s.AssignmentAction<>'Assign'
UNION ALL SELECT 'One active account per selected rider',COUNT(*) FROM #VerifySource s
 WHERE s.AssignmentAction='Assign' AND (SELECT COUNT(*) FROM app.RiderClientAssignments r WHERE r.RiderProfileId=s.RiderId AND r.EffectiveTo IS NULL AND r.IsDeleted=0)<>1
UNION ALL SELECT 'Contract',COUNT(*) FROM(SELECT @Contract Id) x LEFT JOIN app.ClientContracts c ON c.Id=x.Id
 WHERE c.Id IS NULL OR c.ClientPlatformId<>@Platform OR c.Status<>2 OR c.IsDeleted<>0 OR c.Code<>JSON_VALUE(@PlanJson,'$.contractCode')
UNION ALL SELECT 'Summary audit receipt',ABS(COUNT(*)-1) FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId AND Action='NinjaSafeAccountImport'
UNION ALL SELECT 'Account audit count',ABS(COUNT(*)-301) FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId AND EntityType='PlatformRiderAccount' AND Action='Created'
UNION ALL SELECT 'Assignment audit count',ABS(COUNT(*)-107) FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId AND EntityType='RiderClientAssignment' AND Action='Created'
UNION ALL SELECT 'No Ninja credentials created',COUNT(*) FROM app.PlatformAccountCredentialVersions v JOIN app.PlatformRiderAccounts a ON a.Id=v.PlatformRiderAccountId WHERE a.ClientPlatformId=@Platform
UNION ALL SELECT 'No Ninja daily reports imported',COUNT(*) FROM ninja.NinjaDailyImports;
DROP TABLE #VerifySource;
