import json
import secrets
import time
import uuid
from pathlib import Path

root=Path(__file__).parent
details=json.loads((root/'jeddah-before-external.json').read_text(encoding='utf-8-sig'))
source=json.loads((root/'jeddah-commit.json').read_text(encoding='utf-8-sig'))[1]
def lit(v):
    if v is None: return 'NULL'
    if isinstance(v,int): return str(v)
    return "N'"+str(v).replace("'","''")+"'"
def uid():
    return str(uuid.UUID(int=(int(time.time()*1000)<<80)|(7<<76)|(secrets.randbits(12)<<64)|(2<<62)|secrets.randbits(62)))
rows=[]
for s in source:
    records=details[2] if s['IqamaNo'] else details[1]
    found=[x for x in records if x['Name']==s['Name'] and x['HousingCode']==s['Code'] and x['RoomName']==s['Room']]
    assert len(found)==1
    x=found[0]
    rows.append((s['SourceRow'],x['RoomId'],s['Name'],s['IqamaNo'],x['Id'] if s['IqamaNo'] else None,uid() if s['IqamaNo'] else x['Id']))
staging=',\n'.join('    ('+', '.join(lit(v) for v in r)+')' for r in rows)
sql=f"""-- User-requested classification of the 15 unmatched Jeddah residents as external renters.
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
{staging};
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
"""
dest=root.parent/'database/scripts/classify-jeddah-external-renters-20261005.sql'
dest.write_text(sql,encoding='utf-8')
print(json.dumps({'targets':len(rows),'pendingToConvert':sum(r[4] is not None for r in rows),'alreadyExternal':sum(r[4] is None for r in rows)}))
