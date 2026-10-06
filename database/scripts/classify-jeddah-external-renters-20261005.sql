-- User-requested classification of the 15 unmatched Jeddah residents as external renters.
-- Five pending records are archived and replaced with external occupants. Ten are already external.
-- Requires bit parameter @Commit. Default false validates and rolls back. Safe to repeat.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
IF @Commit IS NULL SET @Commit=0;
DECLARE @now datetimeoffset(7)=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
DECLARE @actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001';
DECLARE @targets table (SourceRow int PRIMARY KEY,RoomId uniqueidentifier,Name nvarchar(200),IqamaNo varchar(10),PendingId uniqueidentifier,ExternalId uniqueidentifier);
INSERT @targets VALUES
    (5, N'964529df-bda7-413c-8270-10e8e6a1d701', N'هارون الرشيد اشرف', N'2619453430', N'01a10b15-8061-730e-a227-0ce587e39c7c', N'01a10b26-4fa6-7545-b6ce-491af730fe19'),
    (7, N'964529df-bda7-413c-8270-10e8e6a1d701', N'قمر ال حسن', N'2494774173', N'01a10b15-8061-775c-a548-9b2074bce5a3', N'01a10b26-4fa6-7260-a232-845ba388d651'),
    (16, N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', N'محمد حسنين', NULL, NULL, N'01a10b15-8061-7166-be35-6cc13d380d5c'),
    (17, N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', N'محمد دانيال', NULL, NULL, N'01a10b15-8061-7fe3-bb1b-d978d98df228'),
    (55, N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', N'اسلام سمير', N'2630354393', N'01a10b15-8061-7c3f-bd36-3509fff40d9c', N'01a10b26-4fa6-77c1-b5ea-0668c5078149'),
    (56, N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', N'ابراهيم زعلوق', NULL, NULL, N'01a10b15-8061-76b6-98a5-34bd501f5f4b'),
    (60, N'01a0bf32-4d95-76ab-8c7e-ea7f79afb5f2', N'معوض', NULL, NULL, N'01a10b15-8061-79bb-88ee-e3359300c35c'),
    (71, N'01a0bf32-6803-7ead-82a5-654c4cd61396', N'باسم وجيه', N'2643360973', N'01a10b15-8061-7723-b728-3079ad5f294f', N'01a10b26-4fa6-7d5a-a42a-40e1362fe589'),
    (108, N'01a0b04e-157f-7ab4-8c68-e04a1070b506', N'مصطفى عارف', NULL, NULL, N'01a10b15-8061-7200-a54a-45bba8adf3f6'),
    (119, N'01a0b04e-5201-79fd-8e1c-20034f66fe32', N'التماس', NULL, NULL, N'01a10b15-8061-7d82-9436-16293d0e35af'),
    (187, N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'عاطف علي عبدالستار', N'2553353455', N'01a10b15-8061-70b2-ba08-1c8e9d5d3501', N'01a10b26-4fa6-7509-b401-5f9480e603db'),
    (205, N'01a0bf35-3d3e-75b0-847b-ccdd7897cf9d', N'monirujjaman', NULL, NULL, N'01a10b15-8061-77eb-b258-b154e1f1ee49'),
    (213, N'01a10b15-8061-71ae-8368-fb3cac974794', N'محمد صبري', NULL, NULL, N'01a10b15-8061-7cf9-bf9a-137ee1757aa8'),
    (214, N'01a10b15-8061-71ae-8368-fb3cac974794', N'عبدالرحمن السمكري', NULL, NULL, N'01a10b15-8061-769a-a942-de2bce480271'),
    (215, N'01a10b15-8061-71ae-8368-fb3cac974794', N'نادر الميكانيكي', NULL, NULL, N'01a10b15-8061-779f-a8de-4241b07ab037');
DECLARE @rooms table (Id uniqueidentifier PRIMARY KEY,Occupants int);
DECLARE @before nvarchar(max),@archived int=0,@created int=0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lockResult int;
    EXEC @lockResult=sys.sp_getapplock @Resource='Housing:Jeddah:Workbook20261005',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
    IF @lockResult<0 THROW 51200,'Could not lock the Jeddah resident conversion.',1;
    INSERT @rooms SELECT r.Id,r.CurrentOccupancy FROM app.HousingRooms r WITH (UPDLOCK,HOLDLOCK)
    WHERE r.IsDeleted=0 AND r.HousingId IN ('01a0a427-51c1-7477-b93e-cee215d22268','01a0a428-5ea7-710e-8e7f-2e8550e2c8be','01a0a5e0-535c-7236-afa6-3708df20f662');
    IF (SELECT COUNT(*) FROM @rooms)<>27 OR (SELECT SUM(Occupants) FROM @rooms)<>139
        THROW 51201,'Jeddah room totals changed since the workbook import.',1;
    IF EXISTS (SELECT 1 FROM @targets t WHERE NOT EXISTS (SELECT 1 FROM @rooms r WHERE r.Id=t.RoomId))
        THROW 51202,'A target room is unavailable.',1;
    IF EXISTS (SELECT 1 FROM @targets t WHERE NOT EXISTS (
        SELECT 1 FROM app.HousingExternalOccupants x WITH (UPDLOCK,HOLDLOCK) WHERE x.Id=t.ExternalId AND x.RoomId=t.RoomId AND x.Name=t.Name AND x.IsDeleted=0)
        AND NOT EXISTS (SELECT 1 FROM app.HousingPendingOccupants p WITH (UPDLOCK,HOLDLOCK) WHERE p.Id=t.PendingId AND p.RoomId=t.RoomId AND p.Name=t.Name AND p.IqamaNo=t.IqamaNo AND p.IsDeleted=0))
        THROW 51203,'An expected pending or external occupant changed.',1;
    IF EXISTS (SELECT 1 FROM @targets t JOIN app.HousingExternalOccupants x WITH (UPDLOCK,HOLDLOCK) ON x.RoomId=t.RoomId AND x.Name=t.Name AND x.IsDeleted=0 WHERE x.Id<>t.ExternalId)
        THROW 51204,'A duplicate external resident requires reconciliation.',1;
    SET @before=(SELECT t.SourceRow,t.RoomId,t.Name,t.IqamaNo,t.PendingId,t.ExternalId,
        CASE WHEN EXISTS (SELECT 1 FROM app.HousingPendingOccupants p WHERE p.Id=t.PendingId AND p.IsDeleted=0) THEN 'PendingIqama' ELSE 'ExternalRenter' END AS Classification
        FROM @targets t FOR JSON PATH);
    UPDATE p SET IsDeleted=1,DeletedAtUtc=@now,DeletedByUserId=@actor,
        DeletionReason=N'User requested external renter classification on 2026-10-05',UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.HousingPendingOccupants p JOIN @targets t ON t.PendingId=p.Id
    WHERE p.IsDeleted=0 AND p.RoomId=t.RoomId AND p.IqamaNo=t.IqamaNo AND p.Name=t.Name;
    SET @archived=@@ROWCOUNT;
    INSERT app.HousingExternalOccupants (Id,RoomId,Name,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT t.ExternalId,t.RoomId,t.Name,@now,@actor,0 FROM @targets t
    WHERE NOT EXISTS (SELECT 1 FROM app.HousingExternalOccupants x WHERE x.Id=t.ExternalId);
    SET @created=@@ROWCOUNT;
    IF EXISTS (SELECT 1 FROM @targets t WHERE NOT EXISTS (SELECT 1 FROM app.HousingExternalOccupants x WHERE x.Id=t.ExternalId AND x.RoomId=t.RoomId AND x.Name=t.Name AND x.IsDeleted=0))
        OR EXISTS (SELECT 1 FROM @targets t JOIN app.HousingPendingOccupants p ON p.Id=t.PendingId WHERE p.IsDeleted=0)
        THROW 51205,'Not all 15 target occupants are external renters.',1;
    IF EXISTS (SELECT 1 FROM @rooms s JOIN app.HousingRooms r ON r.Id=s.Id
        CROSS APPLY (SELECT (SELECT COUNT(*) FROM app.HousingResidencePeriods p WHERE p.RoomId=r.Id AND p.EffectiveTo IS NULL)
            +(SELECT COUNT(*) FROM app.HousingExternalOccupants x WHERE x.RoomId=r.Id AND x.IsDeleted=0)
            +(SELECT COUNT(*) FROM app.HousingPendingOccupants p WHERE p.RoomId=r.Id AND p.IsDeleted=0) AS Actual) a
        WHERE r.CurrentOccupancy<>s.Occupants OR a.Actual<>s.Occupants)
        THROW 51206,'Resident conversion changed room occupancy.',1;
    -- This marker also protects the earlier workbook importer from reversing this explicit override.
    IF @archived+@created>0
    INSERT audit.AuditEntries (Id,EventId,ActorType,Action,Category,EntityType,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc)
    VALUES (NEWID(),NEWID(),N'System',N'Reclassified',N'Housing',N'HousingExternalOccupant',@now,N'JeddahExternalRenters20261005',
        N'User requested all 15 unmatched Jeddah residents be external renters. Rooms and occupancy preserved; archived pending records retain iqamas.',@before,
        (SELECT t.SourceRow,t.RoomId,t.Name,t.IqamaNo,t.PendingId,t.ExternalId,N'ExternalRenter' AS Classification FROM @targets t FOR JSON PATH),
        N'JeddahHousingExternalRenters20261005',1,@now);
    SELECT h.Code,COUNT(*) AS ExternalRenters FROM @targets t JOIN app.HousingRooms r ON r.Id=t.RoomId JOIN app.Housing h ON h.Id=r.HousingId GROUP BY h.Code;
    IF @Commit=1 COMMIT TRANSACTION; ELSE ROLLBACK TRANSACTION;
    SELECT CASE WHEN @Commit=1 THEN 'Applied' ELSE 'ValidatedAndRolledBack' END AS Status,@archived AS ArchivedPending,@created AS CreatedExternal,15 AS ExternalRenters,139 AS JeddahOccupants;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
