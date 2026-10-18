SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRY
 BEGIN TRANSACTION;
 IF DB_NAME() <> N'db67927' THROW 51200, 'Unexpected database', 1;
 DECLARE @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001';
 DECLARE @Now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
 DECLARE @Platform uniqueidentifier=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ' AND Status=1 AND IsDeleted=0);
 DECLARE @Contract uniqueidentifier=(SELECT TOP(1) Id FROM app.ClientContracts WHERE ClientPlatformId=@Platform AND Status=2 AND IsDeleted=0 ORDER BY Id);
 IF @Platform IS NULL OR @Contract IS NULL THROW 51201, 'Active Jahez platform or contract missing', 1;
 IF NOT EXISTS(SELECT 1 FROM [identity].Users WHERE Id=@Actor AND UserName='Omar' AND IsDeleted=0 AND IsDevelopmentOnly=0)
   THROW 51202, 'Import actor unavailable', 1;
 SELECT *,CONVERT(datetimeoffset,StartedOn+'T00:00:00+03:00') AS StartedAt,
   CASE WHEN LastSettlementOn IS NULL THEN NULL ELSE CONVERT(datetimeoffset,LastSettlementOn+'T00:00:00+03:00') END AS LastSettlementAt
 INTO #Stage
 FROM OPENJSON(@Plan,'$.rows') WITH (
   SheetRow int, ExternalId nvarchar(150), Iqama nvarchar(20), AccountId uniqueidentifier,
   AccountRowVersion nvarchar(100), RiderId uniqueidentifier, EmployeeId uniqueidentifier,
   StartedOn varchar(10), LastSettlementOn varchar(10), FinancialStartOn date, CommissionThrough date,
   OpeningDebt decimal(18,6), OpeningFees decimal(18,6), OpeningCommission decimal(18,6),
   AssignmentId uniqueidentifier,HandoverId uniqueidentifier,FeeId uniqueidentifier,EventId uniqueidentifier,
   FeeLedgerId uniqueidentifier,DebtLedgerId uniqueidentifier,CommissionLedgerId uniqueidentifier,
   Evidence nvarchar(max), Slot int, ExpectedActiveCount int
 );
 IF @ExpectedCount<=0 OR (SELECT COUNT(*) FROM #Stage)<>@ExpectedCount THROW 51203,'Unexpected approved row count',1;
 IF EXISTS(SELECT AccountId FROM #Stage GROUP BY AccountId HAVING COUNT(*)>1) THROW 51204,'Duplicate staged account',1;
 IF EXISTS(SELECT RiderId FROM #Stage GROUP BY RiderId HAVING COUNT(*)>2) THROW 51205,'Rider exceeds account limit',1;
 IF EXISTS(SELECT 1 FROM #Stage WHERE OpeningFees<0 OR OpeningCommission<0 OR Slot NOT IN (1,2)
   OR StartedOn>'2026-10-08' OR LastSettlementOn> '2026-10-08' OR LastSettlementOn<StartedOn
   OR CommissionThrough<>'2026-10-07' OR FinancialStartOn< '2026-10-08')
   THROW 51206,'Invalid staged dates or opening balances',1;
 IF EXISTS(SELECT 1 FROM #Stage s LEFT JOIN app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK) ON a.Id=s.AccountId
   WHERE a.Id IS NULL OR a.ClientPlatformId<>@Platform OR a.IsDeleted=1 OR a.Status<>1 OR a.PaymentModel<>1
   OR a.ExternalAccountId<>s.ExternalId OR a.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("s.AccountRowVersion"))','varbinary(8)'))
   THROW 51207,'Account changed since validation',1;
 IF EXISTS(SELECT 1 FROM #Stage s JOIN app.PlatformRiderAccounts a ON a.Id=s.AccountId
   LEFT JOIN app.Sponsors p ON p.Id=a.SponsorId WHERE p.Id IS NULL OR p.IsDeleted=1 OR p.Status<>1)
   THROW 51208,'Account sponsor missing or inactive',1;
 IF EXISTS(SELECT 1 FROM #Stage s LEFT JOIN app.RiderProfiles r WITH(UPDLOCK,HOLDLOCK) ON r.Id=s.RiderId
   LEFT JOIN app.Employees e WITH(UPDLOCK,HOLDLOCK) ON e.Id=r.EmployeeId
   WHERE r.Id IS NULL OR r.IsDeleted=1 OR e.Id<>s.EmployeeId OR e.IqamaNo<>s.Iqama OR e.IsDeleted=1 OR e.Status<>3)
   THROW 51209,'Rider identity or status changed',1;
 IF EXISTS(SELECT 1 FROM #Stage s JOIN app.RiderClientAssignments a WITH(UPDLOCK,HOLDLOCK)
   ON a.PlatformRiderAccountId=s.AccountId WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL)
   THROW 51210,'Account already assigned',1;
 IF EXISTS(SELECT 1 FROM #Stage s JOIN jahez.JahezAccountHandover h WITH(UPDLOCK,HOLDLOCK)
   ON h.PlatformRiderAccountId=s.AccountId WHERE h.IsDeleted=0)
   THROW 51211,'Account already has financial history; reconcile instead of reimporting',1;
 IF EXISTS(SELECT 1 FROM #Stage s OUTER APPLY(SELECT COUNT(*) AS N FROM app.RiderClientAssignments a WITH(UPDLOCK,HOLDLOCK)
   WHERE a.RiderProfileId=s.RiderId AND a.IsDeleted=0 AND a.EffectiveTo IS NULL) existing
   WHERE existing.N<>COALESCE(s.ExpectedActiveCount,0)
   OR existing.N+(SELECT COUNT(*) FROM #Stage peers WHERE peers.RiderId=s.RiderId)>2)
   THROW 51212,'Rider assignments changed or exceed limit',1;
 IF EXISTS(SELECT 1 FROM #Stage s JOIN app.RiderClientAssignments a ON a.RiderProfileId=s.RiderId AND a.RiderAccountSlot=s.Slot
   WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL)
   OR EXISTS(SELECT RiderId,Slot FROM #Stage GROUP BY RiderId,Slot HAVING COUNT(*)>1)
   THROW 51217,'Assignment slot already occupied',1;

 INSERT app.RiderClientAssignments
   (Id,RiderProfileId,ClientContractId,PlatformRiderAccountId,EffectiveFrom,Status,StartReason,
    OperationalAgreementReference,OperationalAgreementNotes,AssignedByUserId,WasBackdated,BackdatedReason,
    PaymentModel,RiderAccountSlot,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT AssignmentId,RiderId,@Contract,AccountId,StartedOn,2,
   N'Opening import: جاهز 8-10.xlsx; row '+CONVERT(nvarchar(10),SheetRow),
   N'JAHEZ-OPENING-20261008',Evidence,@Actor,CASE WHEN StartedOn<'2026-10-08' THEN 1 ELSE 0 END,
   CASE WHEN StartedOn<'2026-10-08' THEN N'Historical handover supplied in opening workbook' ELSE NULL END,
   1,Slot,@Now,@Actor,0 FROM #Stage;

 INSERT app.RiderAssignmentEvents
   (Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
 SELECT EventId,AssignmentId,1,2,@Now,@Actor,N'Historical assignment opening import',Evidence,@Now,@Actor FROM #Stage;

 UPDATE a SET Status=2,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM app.PlatformRiderAccounts a JOIN #Stage s ON s.AccountId=a.Id;

 INSERT jahez.JahezAccountHandover
   (Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,CommissionStartsOn,
    CommissionPostedThrough,LastSettlementPaymentAtUtc,IsLegacy,DebtTransferred,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT HandoverId,AccountId,RiderId,AssignmentId,SWITCHOFFSET(StartedAt,'+00:00'),FinancialStartOn,
   CASE WHEN CONVERT(date,StartedOn)<=CommissionThrough THEN CommissionThrough ELSE NULL END,
   SWITCHOFFSET(LastSettlementAt,'+00:00'),1,0,
   N'Opening import: جاهز 8-10.xlsx; row '+CONVERT(nvarchar(10),SheetRow)+N'; balances through 2026-10-07',@Now,@Actor,0 FROM #Stage;

 INSERT jahez.JahezAccountFee (Id,HandoverId,Amount,WaivedAmount,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT FeeId,HandoverId,OpeningFees,0,@Now,@Actor,0 FROM #Stage;

 INSERT jahez.JahezLedgerEntry
   (Id,HandoverId,Bucket,Kind,Amount,SourceId,OccurredAtUtc,Reason,CreatedAtUtc,CreatedByUserId,CalculationJson,FromDate,ThroughDate)
 SELECT ids.EntryId,s.HandoverId,ids.Bucket,5,ids.Amount,s.HandoverId,@Now,
   N'Opening import: جاهز 8-10.xlsx; row '+CONVERT(nvarchar(10),s.SheetRow),@Now,@Actor,s.Evidence,
   CASE WHEN ids.Bucket=3 AND s.OpeningCommission>0 THEN DATEADD(day,1,CONVERT(date,COALESCE(s.LastSettlementOn,s.StartedOn))) ELSE NULL END,
   CASE WHEN ids.Bucket=3 THEN s.CommissionThrough ELSE NULL END
 FROM #Stage s CROSS APPLY(VALUES(s.FeeLedgerId,1,s.OpeningFees),(s.DebtLedgerId,2,s.OpeningDebt),(s.CommissionLedgerId,3,s.OpeningCommission)) ids(EntryId,Bucket,Amount);

 IF (SELECT COUNT(*) FROM app.RiderClientAssignments a JOIN #Stage s ON s.AssignmentId=a.Id)<>@ExpectedCount
   OR (SELECT COUNT(*) FROM jahez.JahezAccountHandover h JOIN #Stage s ON s.HandoverId=h.Id)<>@ExpectedCount
   OR (SELECT COUNT(*) FROM jahez.JahezLedgerEntry l JOIN #Stage s ON s.HandoverId=l.HandoverId)<>@ExpectedCount*3
   THROW 51213,'Import row counts failed',1;
 IF EXISTS(SELECT 1 FROM #Stage s JOIN jahez.JahezAccountHandover h ON h.Id=s.HandoverId
   JOIN app.RiderClientAssignments a ON a.Id=s.AssignmentId JOIN app.PlatformRiderAccounts p ON p.Id=s.AccountId
   WHERE h.RiderProfileId<>s.RiderId OR h.PlatformRiderAccountId<>s.AccountId OR h.RiderClientAssignmentId<>s.AssignmentId
   OR h.StartedAtUtc<>s.StartedAt OR ISNULL(h.LastSettlementPaymentAtUtc,'1900-01-01')<>ISNULL(s.LastSettlementAt,'1900-01-01')
   OR h.CommissionStartsOn<>s.FinancialStartOn OR a.RiderAccountSlot<>s.Slot OR a.Status<>2 OR p.Status<>2)
   THROW 51214,'Assignment or reminder date verification failed',1;
 IF EXISTS(SELECT 1 FROM #Stage s CROSS APPLY(VALUES(1,s.OpeningFees),(2,s.OpeningDebt),(3,s.OpeningCommission)) expected(Bucket,Amount)
   CROSS APPLY(SELECT SUM(Amount) Amount FROM jahez.JahezLedgerEntry WHERE HandoverId=s.HandoverId AND Bucket=expected.Bucket) actual
   WHERE actual.Amount IS NULL OR actual.Amount<>expected.Amount)
   THROW 51215,'Opening balance reconciliation failed',1;
 IF EXISTS(SELECT 1 FROM jahez.JahezCashboxEntry c JOIN #Stage s ON s.HandoverId=c.HandoverId)
   OR EXISTS(SELECT 1 FROM jahez.JahezRiderSettlement p JOIN #Stage s ON s.HandoverId=p.HandoverId)
   THROW 51216,'Opening import must not create new receipts',1;

 SELECT COUNT(*) AS Assignments,SUM(OpeningDebt) AS OpeningDebt,SUM(OpeningFees) AS OpeningFees,SUM(OpeningCommission) AS OpeningCommission
 FROM #Stage;
 SELECT @Commit AS Committed;
 IF @Commit=1 COMMIT TRANSACTION; ELSE ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
