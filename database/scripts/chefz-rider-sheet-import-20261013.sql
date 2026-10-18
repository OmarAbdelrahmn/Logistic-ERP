-- @PlanJson and @CorrelationId are parameters. The runner owns a SERIALIZABLE transaction.
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @PlatformId uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId'),
 @ContractId uniqueidentifier=JSON_VALUE(@PlanJson,'$.contractId'),
 @Actor uniqueidentifier=JSON_VALUE(@PlanJson,'$.actorId'),@Now datetimeoffset=SYSUTCDATETIME();
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WHERE Id=@PlatformId AND Code='SHIFTZ' AND IsDeleted=0)
 THROW 51000,'Unexpected Chefz platform.',1;
IF NOT EXISTS(SELECT 1 FROM app.ClientContracts WHERE Id=@ContractId AND ClientPlatformId=@PlatformId AND Status=2 AND IsDeleted=0)
 THROW 51000,'Chefz contract unavailable.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId AND Action='ChefzRiderSheetImport')
 THROW 51000,'This import has already committed.',1;
SELECT * INTO #Source FROM OPENJSON(@PlanJson,'$.rows') WITH
 (SheetRow int '$.row',OwnerIqama varchar(10) '$.ownerIqama',RiderIqama varchar(10) '$.userIqama',
 AccountId uniqueidentifier '$.accountId',EmployeeId uniqueidentifier '$.employeeId',RiderId uniqueidentifier '$.riderId',
 FromDate date '$.effectiveFrom',PaymentModel int '$.paymentModel',AssignmentId uniqueidentifier '$.assignmentId',
 EventId uniqueidentifier '$.eventId',Action varchar(20) '$.assignmentAction');
SELECT * INTO #Remove FROM OPENJSON(@PlanJson,'$.removeJahez') WITH
 (SheetRow int '$.row',AssignmentId uniqueidentifier '$.assignmentId',AccountId uniqueidentifier '$.accountId',HandoverId uniqueidentifier '$.handoverId');
SELECT * INTO #Move FROM OPENJSON(@PlanJson,'$.moves') WITH
 (SheetRow int '$.row',AssignmentId uniqueidentifier '$.assignmentId',AccountId uniqueidentifier '$.accountId',HandoverId uniqueidentifier '$.handoverId',EndDate date '$.endDate');
SELECT * INTO #Iban FROM OPENJSON(@PlanJson,'$.ibanUpdates') WITH
 (EmployeeId uniqueidentifier '$.employeeId',Iqama varchar(10) '$.iqama',Iban varchar(34) '$.iban',OldIban varchar(34) '$.oldIban',Action varchar(10) '$.action');
IF (SELECT COUNT(*) FROM #Source)<>305 THROW 51000,'Source row count changed.',1;
IF EXISTS(SELECT AccountId FROM #Source WHERE Action='Assign' GROUP BY AccountId HAVING COUNT(*)>1)
 OR EXISTS(SELECT RiderId FROM #Source WHERE Action='Assign' GROUP BY RiderId HAVING COUNT(*)>1)
 THROW 51000,'Ambiguous assignment selection.',1;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK) ON a.Id=s.AccountId
 LEFT JOIN app.Employees owner ON owner.Id=a.RegisteredEmployeeId
 LEFT JOIN app.Employees rider WITH(UPDLOCK,HOLDLOCK) ON rider.Id=s.EmployeeId
 LEFT JOIN app.RiderProfiles rp ON rp.Id=s.RiderId
 WHERE s.Action='Assign' AND (a.Id IS NULL OR a.ClientPlatformId<>@PlatformId OR a.ExternalAccountId<>s.OwnerIqama
 OR a.IsDeleted=1 OR a.Status NOT IN(1,2) OR a.PaymentModel<>s.PaymentModel
 OR owner.IqamaNo<>s.OwnerIqama OR rider.Id IS NULL OR rider.IqamaNo<>s.RiderIqama OR rider.Status<>3 OR rider.IsEmployee=1 OR rider.IsDeleted=1
 OR rp.Id IS NULL OR rp.EmployeeId<>rider.Id OR rp.IsDeleted=1 OR s.FromDate IS NULL OR s.FromDate>'2026-10-13'))
 THROW 51000,'Assignment matches changed since preflight.',1;
IF EXISTS(SELECT 1 FROM #Iban i LEFT JOIN app.Employees e WITH(UPDLOCK,HOLDLOCK) ON e.Id=i.EmployeeId
 WHERE e.Id IS NULL OR e.IqamaNo<>i.Iqama OR e.IsDeleted=1 OR ISNULL(e.Iban,'')<>ISNULL(i.OldIban,'')
 OR LEN(i.Iban)<>24 OR i.Iban NOT LIKE 'SA%' OR SUBSTRING(i.Iban,3,22) LIKE '%[^0-9]%')
 THROW 51000,'IBAN preflight changed or invalid normalized source.',1;
-- Verify the whole source against its preflight snapshot, including rows left pending.
IF EXISTS(SELECT 1 FROM app.RiderClientAssignments r WITH(UPDLOCK,HOLDLOCK) JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@PlatformId)
 THROW 51000,'Chefz assignments appeared since preflight; rerun dependency review before clearing.',1;
SELECT r.* INTO #OtherAssignments FROM app.RiderClientAssignments r WITH(UPDLOCK,HOLDLOCK)
 WHERE NOT EXISTS(SELECT 1 FROM #Remove x WHERE x.AssignmentId=r.Id) AND NOT EXISTS(SELECT 1 FROM #Move x WHERE x.AssignmentId=r.Id);
SELECT a.* INTO #OtherAccounts FROM app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK)
 WHERE NOT EXISTS(SELECT 1 FROM #Source x WHERE x.Action='Assign' AND x.AccountId=a.Id)
 AND NOT EXISTS(SELECT 1 FROM #Remove x WHERE x.AccountId=a.Id) AND NOT EXISTS(SELECT 1 FROM #Move x WHERE x.AccountId=a.Id);
SELECT e.* INTO #OtherEmployees FROM app.Employees e WITH(UPDLOCK,HOLDLOCK) WHERE NOT EXISTS(SELECT 1 FROM #Iban x WHERE x.EmployeeId=e.Id);
IF EXISTS(SELECT 1 FROM (SELECT * FROM #Remove UNION ALL SELECT SheetRow,AssignmentId,AccountId,HandoverId FROM #Move) x
 LEFT JOIN app.RiderClientAssignments a ON a.Id=x.AssignmentId
 LEFT JOIN app.PlatformRiderAccounts p ON p.Id=x.AccountId
 LEFT JOIN platform.ClientPlatforms cp ON cp.Id=p.ClientPlatformId
 LEFT JOIN jahez.JahezAccountHandover h WITH(UPDLOCK,HOLDLOCK) ON h.Id=x.HandoverId
 WHERE a.Id IS NULL OR a.IsDeleted=1 OR a.EffectiveTo IS NOT NULL OR cp.Code<>'JAHEZ' OR h.Id IS NULL
 OR h.RiderClientAssignmentId<>a.Id OR h.EndedAtUtc IS NOT NULL OR h.IsDeleted=1 OR h.DebtStatus=4)
 THROW 51000,'Jahez assignment changed since preflight.',1;
-- Do not remove receipts, actual transactions, settlements or approval records.
IF EXISTS(SELECT 1 FROM jahez.JahezTransaction t JOIN #Remove x ON x.HandoverId=t.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez.JahezDailyDispatch t JOIN #Remove x ON x.HandoverId=t.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez.JahezRiderSettlement t JOIN #Remove x ON x.HandoverId=t.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez.JahezCashboxEntry t JOIN #Remove x ON x.HandoverId=t.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez.JahezApprovalRequest t JOIN #Remove x ON x.HandoverId=t.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez.JahezEarningsStatement t JOIN #Remove x ON x.HandoverId=t.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez.JahezCommissionPolicyPeriod t JOIN #Remove x ON x.HandoverId=t.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez.JahezReminderState t JOIN #Remove x ON x.HandoverId=t.HandoverId)
 THROW 51000,'Jahez histories have additional dependencies requiring review.',1;
IF EXISTS(SELECT 1 FROM jahez.JahezLedgerEntry t JOIN #Remove x ON x.HandoverId=t.HandoverId WHERE t.Kind<>5)
 THROW 51000,'A removable Jahez ledger has non-opening entries.',1;
-- The one conflict-free transfer follows the Jahez close workflow and daily commission rule.
IF EXISTS(SELECT 1 FROM #Move x JOIN jahez.JahezAccountHandover h ON h.Id=x.HandoverId JOIN app.RiderClientAssignments a ON a.Id=x.AssignmentId
 WHERE x.EndDate<a.EffectiveFrom OR x.EndDate<CONVERT(date,SWITCHOFFSET(h.StartedAtUtc,'+03:00')) OR x.EndDate<h.CommissionPostedThrough)
 OR EXISTS(SELECT 1 FROM jahez.JahezTransaction t JOIN #Move x ON x.HandoverId=t.HandoverId
 WHERE t.OccurredAtUtc>=TODATETIMEOFFSET(CONVERT(datetime2,DATEADD(day,1,x.EndDate)),'+03:00')
 AND NOT EXISTS(SELECT 1 FROM jahez.JahezImportBatch b WHERE b.ReplacesBatchId=t.BatchId AND b.CommittedAtUtc IS NOT NULL))
 OR EXISTS(SELECT 1 FROM jahez.JahezDailyDispatch t JOIN #Move x ON x.HandoverId=t.HandoverId WHERE t.Date>x.EndDate
 AND NOT EXISTS(SELECT 1 FROM jahez.JahezImportBatch b WHERE b.ReplacesBatchId=t.BatchId AND b.CommittedAtUtc IS NOT NULL))
 OR EXISTS(SELECT 1 FROM jahez.JahezRiderSettlement t JOIN #Move x ON x.HandoverId=t.HandoverId WHERE t.CountsAsSettlement=1 AND t.ThroughDate>x.EndDate)
 OR EXISTS(SELECT 1 FROM jahez.JahezCommissionPolicyPeriod t JOIN #Move x ON x.HandoverId=t.HandoverId WHERE t.ToDate>=x.EndDate)
 THROW 51000,'Jahez close date or commission needs additional review.',1;
SELECT x.HandoverId,x.EndDate,CASE WHEN h.CommissionPostedThrough>=h.CommissionStartsOn
 THEN DATEADD(day,1,h.CommissionPostedThrough) ELSE h.CommissionStartsOn END FromDate INTO #Commission
 FROM #Move x JOIN jahez.JahezAccountHandover h ON h.Id=x.HandoverId;
IF EXISTS(SELECT 1 FROM jahez.JahezCommissionPolicyPeriod p JOIN #Commission c ON c.HandoverId=p.HandoverId WHERE p.ToDate>=c.FromDate AND p.FromDate<=c.EndDate)
 THROW 51000,'Percentage commission requires the Jahez service.',1;
DECLARE @DeletedLedger int=0,@DeletedFees int=0;
DELETE l FROM jahez.JahezLedgerEntry l JOIN #Remove x ON x.HandoverId=l.HandoverId; SET @DeletedLedger=@@ROWCOUNT;
DELETE f FROM jahez.JahezAccountFee f JOIN #Remove x ON x.HandoverId=f.HandoverId; SET @DeletedFees=@@ROWCOUNT;
DELETE h FROM jahez.JahezAccountHandover h JOIN #Remove x ON x.HandoverId=h.Id;
DELETE e FROM app.RiderAssignmentEvents e JOIN #Remove x ON x.AssignmentId=e.RiderClientAssignmentId;
DELETE a FROM app.RiderClientAssignments a JOIN #Remove x ON x.AssignmentId=a.Id;
INSERT jahez.JahezLedgerEntry (Id,HandoverId,Bucket,Kind,Amount,SourceId,OccurredAtUtc,Reason,FromDate,ThroughDate,CalculationJson,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),c.HandoverId,3,1,(DATEDIFF(day,c.FromDate,c.EndDate)+1)*15,NEWID(),@Now,
 N'User authorized Jahez closure for Chefz sheet import.',c.FromDate,c.EndDate,
 (SELECT 'Daily' Kind,c.FromDate,c.EndDate ToDate,15 DailyRate,(DATEDIFF(day,c.FromDate,c.EndDate)+1)*15 Amount FOR JSON PATH),@Now,@Actor
 FROM #Commission c WHERE c.FromDate<=c.EndDate;
UPDATE h SET EndedAtUtc=DATEADD(millisecond,-1,TODATETIMEOFFSET(CONVERT(datetime2,DATEADD(day,1,x.EndDate)),'+03:00')),
 DebtStatus=2,DebtTransferred=0,CommissionPostedThrough=CASE WHEN c.FromDate<=c.EndDate THEN c.EndDate ELSE h.CommissionPostedThrough END,
 UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM jahez.JahezAccountHandover h JOIN #Move x ON x.HandoverId=h.Id JOIN #Commission c ON c.HandoverId=h.Id;
INSERT app.RiderAssignmentEvents (Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),a.Id,a.Status,3,@Now,@Actor,N'User authorized move from Jahez to Chefz.',@Now,@Actor
 FROM app.RiderClientAssignments a JOIN #Move x ON x.AssignmentId=a.Id;
UPDATE a SET EffectiveTo=x.EndDate,Status=3,EndReason=N'User authorized move from Jahez to Chefz.',EndedByUserId=@Actor,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM app.RiderClientAssignments a JOIN #Move x ON x.AssignmentId=a.Id;
UPDATE a SET Status=1,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM app.PlatformRiderAccounts a
 WHERE EXISTS(SELECT 1 FROM #Move x WHERE x.AccountId=a.Id) OR EXISTS(SELECT 1 FROM #Remove x WHERE x.AccountId=a.Id);
IF EXISTS(SELECT 1 FROM #Source s JOIN app.RiderClientAssignments a ON a.RiderProfileId=s.RiderId AND a.EffectiveTo IS NULL AND a.IsDeleted=0 WHERE s.Action='Assign')
 THROW 51000,'Rider still has an active assignment.',1;
INSERT app.RiderClientAssignments (Id,RiderProfileId,ClientContractId,PlatformRiderAccountId,PaymentModel,RiderAccountSlot,EffectiveFrom,Status,StartReason,
 AssignedByUserId,WasBackdated,BackdatedReason,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT AssignmentId,RiderId,@ContractId,AccountId,PaymentModel,1,FromDate,2,N'Chefz rider sheet import matched by owner and user iqama.',
 @Actor,CASE WHEN FromDate<'2026-10-13' THEN 1 ELSE 0 END,
 CASE WHEN FromDate<'2026-10-13' THEN N'Sheet handover date supplied by user.' END,@Now,@Actor,0 FROM #Source WHERE Action='Assign';
INSERT app.RiderAssignmentEvents (Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
 SELECT EventId,AssignmentId,1,2,@Now,@Actor,N'Chefz rider sheet import.',
 (SELECT s.SheetRow,s.OwnerIqama,s.RiderIqama,s.FromDate FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now,@Actor FROM #Source s WHERE Action='Assign';
UPDATE a SET Status=2,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM app.PlatformRiderAccounts a JOIN #Source s ON s.AccountId=a.Id WHERE s.Action='Assign';
UPDATE e SET Iban=i.Iban,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM app.Employees e JOIN #Iban i ON i.EmployeeId=e.Id;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.RiderClientAssignments a ON a.Id=s.AssignmentId
 WHERE s.Action='Assign' AND (a.Id IS NULL OR a.RiderProfileId<>s.RiderId OR a.PlatformRiderAccountId<>s.AccountId OR a.EffectiveFrom<>s.FromDate OR a.EffectiveTo IS NOT NULL OR a.Status<>2))
 OR EXISTS(SELECT 1 FROM #Iban i JOIN app.Employees e ON e.Id=i.EmployeeId WHERE e.Iban<>i.Iban)
 THROW 51000,'Import verification failed.',1;
IF EXISTS(SELECT * FROM #OtherAssignments EXCEPT SELECT r.* FROM app.RiderClientAssignments r WHERE NOT EXISTS(SELECT 1 FROM #Remove x WHERE x.AssignmentId=r.Id) AND NOT EXISTS(SELECT 1 FROM #Move x WHERE x.AssignmentId=r.Id) AND NOT EXISTS(SELECT 1 FROM #Source s WHERE s.Action='Assign' AND s.AssignmentId=r.Id))
 OR EXISTS(SELECT * FROM #OtherAccounts EXCEPT SELECT a.* FROM app.PlatformRiderAccounts a WHERE NOT EXISTS(SELECT 1 FROM #Source x WHERE x.Action='Assign' AND x.AccountId=a.Id) AND NOT EXISTS(SELECT 1 FROM #Remove x WHERE x.AccountId=a.Id) AND NOT EXISTS(SELECT 1 FROM #Move x WHERE x.AccountId=a.Id))
 OR EXISTS(SELECT * FROM #OtherEmployees EXCEPT SELECT e.* FROM app.Employees e WHERE NOT EXISTS(SELECT 1 FROM #Iban x WHERE x.EmployeeId=e.Id))
 THROW 51000,'An unrelated record changed.',1;
IF EXISTS(SELECT a.RiderProfileId FROM app.RiderClientAssignments a WHERE a.EffectiveTo IS NULL AND a.IsDeleted=0
 AND EXISTS(SELECT 1 FROM #Source s WHERE s.Action='Assign' AND s.RiderId=a.RiderProfileId) GROUP BY a.RiderProfileId HAVING COUNT(*)>1)
 THROW 51000,'An active rider has multiple accounts.',1;
DECLARE @Summary nvarchar(max)=(SELECT (SELECT COUNT(*) FROM #Source WHERE Action='Assign') AssignmentsCreated,
 (SELECT COUNT(*) FROM #Source WHERE Action='Pending') AssignmentsPending,(SELECT COUNT(*) FROM #Remove) JahezHistoriesRemoved,
 (SELECT COUNT(*) FROM #Move) JahezAssignmentsClosed,@DeletedFees JahezFeesRemoved,@DeletedLedger JahezOpeningLedgerEntriesRemoved,
 (SELECT COUNT(*) FROM #Iban WHERE Action='Add') IbansAdded,(SELECT COUNT(*) FROM #Iban WHERE Action='Update') IbansUpdated FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries (Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 VALUES(NEWID(),NEWID(),@Actor,'User','ChefzRiderSheetImport','chefz','ClientPlatform',@PlatformId,@Now,@CorrelationId,
 N'User requested iqama-based Chefz assignments and rider IBAN updates; pending exceptions retained; conflicting Jahez histories removed as authorized.',
 (SELECT JSON_QUERY(@PlanJson,'$.removeJahez') RemovedJahez,JSON_QUERY(@PlanJson,'$.moves') ClosedJahez,JSON_QUERY(@PlanJson,'$.ibanUpdates') IbanChanges FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 @Summary,'CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey IN('chefz','jahez','platform-operations','workforce');
INSERT app.DatasetVersions (Id,ModuleKey,Version,LastChangedAtUtc,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT NEWID(),k.ModuleKey,1,@Now,@Now,@Actor,0 FROM(VALUES('chefz'),('jahez'),('platform-operations'),('workforce')) k(ModuleKey)
 WHERE NOT EXISTS(SELECT 1 FROM app.DatasetVersions d WHERE d.ModuleKey=k.ModuleKey);
SELECT @Summary SummaryJson;
