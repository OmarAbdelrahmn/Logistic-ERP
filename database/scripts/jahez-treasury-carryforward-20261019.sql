-- Executed only by the guarded maintenance tool in its serializable transaction.
-- @Plan and @PlanSha256 are parameters. No existing V2 financial row is updated or deleted.
SET NOCOUNT ON;
IF EXISTS(SELECT 1 FROM jahez_v3.State WHERE IsActive=1)
 THROW 51000,'Carryforward is only allowed before treasury activation.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CashboxHandovers WHERE Status=1)
 THROW 51000,'Complete or cancel pending handovers before historical carryforward.',1;

SELECT * INTO #CarryOwners FROM OPENJSON(@Plan,'$.ownership') WITH(
 Id uniqueidentifier '$.id',Source int '$.source',SourceId uniqueidentifier '$.sourceId',
 PlatformAccountId uniqueidentifier '$.platformAccountId',SponsorId uniqueidentifier '$.sponsorId',
 SponsorIdentifier nvarchar(100) '$.sponsorIdentifier',SponsorNameAr nvarchar(200) '$.sponsorNameAr',Evidence nvarchar(2000) '$.evidence');
SELECT * INTO #CarryPostings FROM OPENJSON(@Plan,'$.postings') WITH(
 Id uniqueidentifier '$.id',SubAccountId uniqueidentifier '$.subAccountId',SponsorId uniqueidentifier '$.sponsorId',
 MoneyMovementId uniqueidentifier '$.moneyMovementId',CashboxHandoverId uniqueidentifier '$.cashboxHandoverId',
 BatchId uniqueidentifier '$.batchId',Revision int '$.revision',ReversesPostingId uniqueidentifier '$.reversesPostingId',Kind int '$.kind',
 Amount decimal(18,2) '$.amount',OccurredAtUtc datetimeoffset '$.occurredAtUtc',ActorUserId uniqueidentifier '$.actorUserId',
 ActorNameAr nvarchar(200) '$.actorNameAr',Reference nvarchar(500) '$.reference',Reason nvarchar(2000) '$.reason');
SELECT * INTO #CarryLines FROM OPENJSON(@Plan,'$.handoverLines') WITH(
 Id uniqueidentifier '$.id',HandoverId uniqueidentifier '$.handoverId',SubAccountId uniqueidentifier '$.subAccountId',
 AccountCode nvarchar(100) '$.accountCode',AccountNameAr nvarchar(200) '$.accountNameAr',SponsorId uniqueidentifier '$.sponsorId',
 SponsorNameAr nvarchar(200) '$.sponsorNameAr',SponsorIdentifier nvarchar(100) '$.sponsorIdentifier',
 Amount decimal(18,2) '$.amount',MovementCount int '$.movementCount');
SELECT * INTO #CarryMemberships FROM OPENJSON(@Plan,'$.handoverPostings') WITH(
 Id uniqueidentifier '$.id',HandoverId uniqueidentifier '$.handoverId',PostingId uniqueidentifier '$.postingId');

IF (SELECT COUNT(*) FROM #CarryOwners WHERE Source=1)<>(SELECT COUNT(*) FROM jahez_v2.AccountFees)
 OR (SELECT COUNT(*) FROM #CarryOwners WHERE Source=2)<>(SELECT COUNT(*) FROM jahez_v2.Operations WHERE Status=3)
 OR (SELECT COUNT(*) FROM #CarryOwners WHERE Source=3)<>(SELECT COUNT(*) FROM jahez_v2.TransferredDebts)
 OR (SELECT COUNT(*) FROM #CarryOwners WHERE Source=4)<>(SELECT COUNT(*) FROM jahez_v2.MoneyMovements)
 THROW 51000,'The reviewed source population is incomplete or changed.',1;
IF EXISTS(SELECT Source,SourceId FROM #CarryOwners GROUP BY Source,SourceId HAVING COUNT(*)<>1)
 THROW 51000,'Duplicate ownership source in reviewed plan.',1;
IF EXISTS(SELECT 1 FROM #CarryOwners p JOIN jahez_v3.Ownership o ON o.Source=p.Source AND o.SourceId=p.SourceId
 WHERE o.PlatformAccountId<>p.PlatformAccountId OR o.SponsorId<>p.SponsorId OR o.SponsorIdentifier<>p.SponsorIdentifier)
 THROW 51000,'The plan conflicts with preserved ownership.',1;
IF EXISTS(SELECT 1 FROM #CarryOwners p LEFT JOIN app.Sponsors s ON s.Id=p.SponsorId
 WHERE s.Id IS NULL OR s.RegistryNameAr<>p.SponsorNameAr OR s.EmployerIdentityNumber<>p.SponsorIdentifier)
 THROW 51000,'Sponsor evidence no longer matches the registry.',1;
IF EXISTS(SELECT 1 FROM #CarryOwners d JOIN jahez_v2.TransferredDebts src ON d.Source=3 AND d.SourceId=src.Id
 LEFT JOIN #CarryOwners o ON o.Source=2 AND o.SourceId=src.OperationId
 WHERE o.SourceId IS NULL OR o.SponsorId<>d.SponsorId OR o.PlatformAccountId<>d.PlatformAccountId)
 THROW 51000,'Debt attribution must inherit its originating operation.',1;
IF EXISTS(SELECT 1 FROM #CarryOwners ow JOIN jahez_v2.MoneyMovements m ON ow.Source=4 AND ow.SourceId=m.Id
 LEFT JOIN #CarryOwners origin ON
 (m.TransferredDebtId IS NOT NULL AND origin.Source=3 AND origin.SourceId=m.TransferredDebtId)
 OR (m.TransferredDebtId IS NULL AND m.OperationId IS NOT NULL AND origin.Source=2 AND origin.SourceId=m.OperationId)
 OR (m.TransferredDebtId IS NULL AND m.OperationId IS NULL AND m.Purpose=3 AND origin.Source=1
     AND EXISTS(SELECT 1 FROM jahez_v2.AccountFees f WHERE f.Id=origin.SourceId AND f.HandoverId=m.HandoverId))
 WHERE origin.SourceId IS NULL OR origin.SponsorId<>ow.SponsorId OR origin.PlatformAccountId<>ow.PlatformAccountId)
 THROW 51000,'Movement attribution must inherit its original obligation.',1;
IF EXISTS(SELECT 1 FROM #CarryPostings p LEFT JOIN jahez_v3.TreasurySubAccounts a ON a.Id=p.SubAccountId
 WHERE a.Id IS NULL OR a.IsPosting=0 OR a.IsActive=0 OR a.SponsorId<>p.SponsorId OR p.Kind NOT IN(1,2) OR p.Amount=0)
 THROW 51000,'Posting ownership, classification or amount is invalid.',1;
IF EXISTS(SELECT 1 FROM #CarryPostings p LEFT JOIN jahez_v2.MoneyMovements m ON m.Id=p.MoneyMovementId
 LEFT JOIN #CarryOwners ow ON ow.Source=4 AND ow.SourceId=m.Id
 JOIN jahez_v3.TreasurySubAccounts a ON a.Id=p.SubAccountId
 WHERE p.Kind=1 AND (m.Id IS NULL OR ow.SponsorId<>p.SponsorId
 OR p.Amount<>CASE WHEN m.Direction=1 THEN m.Amount ELSE -m.Amount END OR p.OccurredAtUtc<>m.OccurredAtUtc
 OR p.ActorUserId<>m.ActorUserId OR p.CashboxHandoverId IS NOT NULL
 OR a.Location<>CASE m.Fund WHEN 1 THEN 1 WHEN 2 THEN 4 WHEN 3 THEN 3 END
 OR a.Purpose NOT IN(CASE WHEN m.Purpose=3 THEN 2 ELSE 1 END,3)))
 THROW 51000,'Business postings differ from their recorded source movements.',1;
IF EXISTS(SELECT MoneyMovementId FROM #CarryPostings WHERE Kind=1 GROUP BY MoneyMovementId HAVING COUNT(*)<>1)
 OR (SELECT COUNT(*) FROM #CarryPostings WHERE Kind=1)<>(SELECT COUNT(*) FROM jahez_v2.MoneyMovements)
 THROW 51000,'Every existing movement needs exactly one initial posting.',1;
IF EXISTS(SELECT BatchId FROM #CarryPostings WHERE Kind=2 GROUP BY BatchId
 HAVING COUNT(*)<>2 OR SUM(Amount)<>0 OR MIN(CONVERT(varchar(36),SponsorId))<>MAX(CONVERT(varchar(36),SponsorId)))
 THROW 51000,'Internal custody transfers do not conserve each sponsor balance.',1;
IF EXISTS(SELECT 1 FROM #CarryPostings p JOIN jahez_v3.TreasurySubAccounts a ON a.Id=p.SubAccountId
 LEFT JOIN jahez_v2.CashboxHandovers h ON h.Id=p.CashboxHandoverId
 WHERE p.Kind=2 AND (h.Id IS NULL OR h.Status<>2 OR p.MoneyMovementId IS NOT NULL
 OR p.ActorUserId<>h.AccountantUserId OR p.OccurredAtUtc<>h.ConfirmedAtUtc
 OR NOT((a.Location=1 AND p.Amount<0) OR (a.Location=2 AND p.Amount>0))))
 THROW 51000,'Historical cash transfers must preserve the confirmed custody event.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CashboxHandovers h
 CROSS APPLY(SELECT COALESCE(SUM(p.Amount),0) AS Received FROM #CarryPostings p
 JOIN jahez_v3.TreasurySubAccounts a ON a.Id=p.SubAccountId WHERE p.Kind=2 AND p.CashboxHandoverId=h.Id AND a.Location=2) x
 WHERE h.Status=2 AND h.CashboxAmount<>x.Received)
 THROW 51000,'Confirmed cash was not reconstructed exactly once.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CashboxHandovers h
 CROSS APPLY(SELECT COALESCE(SUM(CASE WHEN a.Location=1 THEN l.Amount ELSE 0 END),0) AS Cash,
 COALESCE(SUM(CASE WHEN a.Location=3 THEN l.Amount ELSE 0 END),0) AS Bank,
 COALESCE(SUM(CASE WHEN a.Location=4 THEN l.Amount ELSE 0 END),0) AS Petro,COALESCE(SUM(l.MovementCount),0) AS Entries
 FROM #CarryLines l JOIN jahez_v3.TreasurySubAccounts a ON a.Id=l.SubAccountId WHERE l.HandoverId=h.Id) x
 WHERE h.Status=2 AND (h.CashboxAmount<>x.Cash OR h.BankAmount<>x.Bank OR h.PetroAppAmount<>x.Petro OR h.MovementCount<>x.Entries))
 THROW 51000,'The frozen handover totals or membership count differ.',1;
IF EXISTS(SELECT l.CashboxHandoverId,p.Id FROM jahez_v2.CashboxHandoverMovements l
 JOIN #CarryPostings p ON p.MoneyMovementId=l.MoneyMovementId
 JOIN jahez_v2.CashboxHandovers h ON h.Id=l.CashboxHandoverId AND h.Status=2
 EXCEPT SELECT HandoverId,PostingId FROM #CarryMemberships)
 OR EXISTS(SELECT HandoverId,PostingId FROM #CarryMemberships EXCEPT
 SELECT l.CashboxHandoverId,p.Id FROM jahez_v2.CashboxHandoverMovements l
 JOIN #CarryPostings p ON p.MoneyMovementId=l.MoneyMovementId
 JOIN jahez_v2.CashboxHandovers h ON h.Id=l.CashboxHandoverId AND h.Status=2)
 THROW 51000,'The reviewed posting membership is not the exact legacy membership.',1;

DECLARE @now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
INSERT jahez_v3.Ownership(Id,Source,SourceId,PlatformAccountId,SponsorId,SponsorIdentifier,SponsorNameAr,Evidence,CreatedAtUtc,CreatedByUserId)
 SELECT p.Id,p.Source,p.SourceId,p.PlatformAccountId,p.SponsorId,p.SponsorIdentifier,p.SponsorNameAr,p.Evidence,@now,NULL
 FROM #CarryOwners p WHERE NOT EXISTS(SELECT 1 FROM jahez_v3.Ownership o WHERE o.Source=p.Source AND o.SourceId=p.SourceId);
INSERT jahez_v3.Postings(Id,SubAccountId,SponsorId,MoneyMovementId,CashboxHandoverId,BatchId,Revision,ReversesPostingId,
 Kind,Amount,OccurredAtUtc,ActorUserId,ActorNameAr,Reference,Reason,CreatedAtUtc,CreatedByUserId)
 SELECT Id,SubAccountId,SponsorId,MoneyMovementId,CashboxHandoverId,BatchId,Revision,ReversesPostingId,
 Kind,Amount,OccurredAtUtc,ActorUserId,ActorNameAr,Reference,Reason,@now,NULL FROM #CarryPostings;
INSERT jahez_v3.HandoverLines(Id,HandoverId,SubAccountId,AccountCode,AccountNameAr,SponsorId,SponsorNameAr,SponsorIdentifier,Amount,MovementCount,CreatedAtUtc,CreatedByUserId)
 SELECT Id,HandoverId,SubAccountId,AccountCode,AccountNameAr,SponsorId,SponsorNameAr,SponsorIdentifier,Amount,MovementCount,@now,NULL FROM #CarryLines;
INSERT jahez_v3.HandoverPostings(Id,HandoverId,PostingId,CreatedAtUtc,CreatedByUserId)
 SELECT Id,HandoverId,PostingId,@now,NULL FROM #CarryMemberships;

DECLARE @after nvarchar(max)=(SELECT @PlanSha256 AS PlanSha256,
 (SELECT COUNT(*) FROM #CarryOwners) AS ProvenSources,(SELECT COUNT(*) FROM #CarryPostings WHERE Kind=1) AS OriginalMovements,
 (SELECT COUNT(*) FROM #CarryPostings WHERE Kind=2) AS TransferEntries,
 CAST(0 AS bit) AS TreasuryActivated,CAST(0 AS bit) AS FundingInvented,
 JSON_VALUE(@Plan,'$.rosterSha256') AS OriginalRosterSha256 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,OccurredAtUtc,CorrelationId,
 Reason,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 VALUES(NEWID(),NEWID(),NULL,N'System',N'JahezV2TreasuryCarryforward',N'jahez',N'TreasuryCarryforward',@now,
 N'jahez-v2-treasury-carryforward-20261019',N'User requested correcting and carrying V2 custody into V3. Historical ownership proven by the committed roster and sponsor audit chain; original movements and confirmed handovers preserved. Bank funding remains unverified.',
 @after,N'CodexAuthorizedMaintenance',1,@now,NULL);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE ModuleKey=N'jahez';
