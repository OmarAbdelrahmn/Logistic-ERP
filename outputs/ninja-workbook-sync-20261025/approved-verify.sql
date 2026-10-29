SET NOCOUNT ON;

DECLARE @Actor uniqueidentifier=JSON_VALUE(@Plan,'$.actorId'),@Platform uniqueidentifier=JSON_VALUE(@Plan,'$.platformId'),
 @Batch nvarchar(100)=JSON_VALUE(@Plan,'$.batchId'),@Now datetimeoffset=SYSUTCDATETIME();

SELECT * INTO #M FROM OPENJSON(@Plan,'$.mappings') WITH(
 accountId uniqueidentifier, captainId nvarchar(150),assignmentId uniqueidentifier,riderId uniqueidentifier,employeeId uniqueidentifier,
 fromDate date,toDate date,paymentModel int,cityId uniqueidentifier,phone nvarchar(32),nationality nvarchar(100),
 reference nvarchar(200),notes nvarchar(max));
SELECT * INTO #L FROM OPENJSON(@Plan,'$.latestAccounts') WITH(accountId uniqueidentifier,paymentModel int,cityId uniqueidentifier,status int);

IF (SELECT COUNT(*) FROM #M)<>229 OR (SELECT COUNT(*) FROM #M WHERE toDate IS NULL)<>151 THROW 51000,'Invalid source mapping.',1;
IF (SELECT COUNT(*) FROM #L)<>207 OR EXISTS(SELECT 1 FROM #L l JOIN app.PlatformRiderAccounts a ON a.Id=l.accountId
 WHERE a.IsDeleted=1 OR a.PaymentModel<>l.paymentModel OR a.OperatingCityId<>l.cityId OR a.Status<>l.status)
 THROW 51000,'Latest account payment/city/status does not match the source.',1;
IF EXISTS(SELECT 1 FROM #M m LEFT JOIN app.RiderClientAssignments x ON x.Id=m.assignmentId
 WHERE x.Id IS NULL OR x.IsDeleted=1 OR x.PlatformRiderAccountId<>m.accountId OR x.RiderProfileId<>m.riderId
 OR x.EffectiveFrom<>m.fromDate OR ISNULL(x.EffectiveTo,'99991231')<>ISNULL(m.toDate,'99991231')
 OR x.PaymentModel<>m.paymentModel OR x.Status<>CASE WHEN m.toDate IS NULL THEN 2 ELSE 3 END
 OR ISNULL(x.OperationalAgreementReference,'')<>m.reference OR ISNULL(x.OperationalAgreementNotes,'')<>m.notes)
 THROW 51000,'Assignment/source reconciliation failed.',1;
IF EXISTS(SELECT 1 FROM #M m JOIN app.PlatformRiderAccounts a ON a.Id=m.accountId
 LEFT JOIN ninja.NinjaAccountDetails d ON d.PlatformRiderAccountId=a.Id AND d.IsDeleted=0
 JOIN app.Employees e ON e.Id=m.employeeId WHERE m.toDate IS NULL AND
 (a.IsDeleted=1 OR a.Status<>2 OR a.PaymentModel<>m.paymentModel OR a.OperatingCityId<>m.cityId
 OR ISNULL(d.PhoneNumber,'')<>m.phone OR ISNULL(e.PrimaryPhone,'')<>m.phone OR ISNULL(e.Nationality,'')<>m.nationality))
 THROW 51000,'Current account/contact/city reconciliation failed.',1;
IF (SELECT COUNT(*) FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND x.IsDeleted=0)<>229 THROW 51000,'Visible assignment count is not 229.',1;
IF (SELECT COUNT(*) FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND x.IsDeleted=0 AND x.EffectiveTo IS NULL)<>151 THROW 51000,'Current assignment count is not 151.',1;
IF EXISTS(SELECT x.RiderProfileId FROM app.RiderClientAssignments x WHERE x.EffectiveTo IS NULL AND x.IsDeleted=0
 AND x.RiderProfileId IN(SELECT riderId FROM #M WHERE toDate IS NULL) GROUP BY x.RiderProfileId HAVING COUNT(*)>1)
 THROW 51000,'A source current rider has another active account.',1;
IF NOT EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId
 WHERE a.ClientPlatformId=@Platform AND a.ExternalAccountId=N'255527' AND e.IqamaNo='2629274197')
 THROW 51000,'Owner identity correction is missing.',1;
IF EXISTS(SELECT 1 FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND a.ExternalAccountId=N'286371' AND x.EffectiveTo IS NULL AND x.IsDeleted=0)
 THROW 51000,'Historical-only account has a current assignment.',1;
IF (SELECT COUNT(*) FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND a.ExternalAccountId=N'215459' AND x.EffectiveFrom='20261006'
 AND x.EffectiveTo='20261006' AND x.IsDeleted=0)<>1 THROW 51000,'Duplicate historical assignment remains.',1;
IF EXISTS(SELECT 1 FROM app.PlatformAccountDayUsages u JOIN app.RiderClientAssignments x ON x.Id=u.RiderClientAssignmentId
 WHERE u.IsDeleted=0 AND x.Id='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate daily allocation remains active.',1;
IF NOT EXISTS(SELECT 1 FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND a.ExternalAccountId=N'267570' AND x.EffectiveFrom='20261023' AND x.EffectiveTo IS NULL AND x.IsDeleted=0)
 THROW 51000,'Confirmed current start date is missing.',1;
IF EXISTS(SELECT 1 FROM #M m WHERE m.toDate IS NULL AND NOT EXISTS(SELECT 1 FROM app.PlatformAccountRegistrations r
 WHERE r.PlatformRiderAccountId=m.accountId AND r.IsDeleted=0 AND r.Status=4)) THROW 51000,'Current registration missing.',1;
SELECT @Batch BatchId,CAST(1 AS bit) Verified,229 MatchedSourceRows,151 CurrentAssignments,78 HistoricalAssignments,SYSUTCDATETIME() VerifiedAtUtc;