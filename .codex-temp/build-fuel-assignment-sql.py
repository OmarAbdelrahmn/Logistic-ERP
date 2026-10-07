import json, hashlib, secrets, time, uuid
from pathlib import Path
root=Path(__file__).parent
rows=json.loads((root/'fuel-reconciliation.json').read_text(encoding='utf-8'))
ready={r['card']:r for r in rows if not r['issues'] or r['issues']==['already_assigned']}
assert len(ready)==215
db=json.loads((root/'fuel-db-before.json').read_text(encoding='utf-8-sig'))
actor=db[6][0]['Id']
digest=hashlib.sha256(Path('outputs/fuel-card-assignment-20261006/التسكين.xlsx').read_bytes()).hexdigest()
def guid7():
    return str(uuid.UUID(int=(int(time.time()*1000)<<80)|(7<<76)|(secrets.randbits(12)<<64)|(2<<62)|secrets.randbits(62)))
def q(v): return "N'"+str(v).replace("'","''")+"'"
values=[]
for r in ready.values():
    p=r['employees'][0]
    values.append('('+','.join(map(q,[r['card'],r['iqama'],r['cards'][0]['Id'],p['RiderProfileId'],p['Id'],guid7(),guid7(),guid7()]))+','+str(r['row'])+')')
head="""-- Assign existing fuel cards from التسكين.xlsx, excluding both BW218 rows per user request.
-- @Commit=0 validates the entire transaction and rolls it back; @Commit=1 commits.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @actor uniqueidentifier=ACTOR;
DECLARE @effective date='2026-10-06', @month date='2026-10-01', @monthEnd date='2026-10-31';
DECLARE @now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
DECLARE @source nvarchar(255)=N'التسكين.xlsx', @checksum nvarchar(64)=CHECKSUM;
DECLARE @selected TABLE (CardNumber nvarchar(100),IqamaNo nvarchar(20),CardId uniqueidentifier PRIMARY KEY,RiderId uniqueidentifier,EmployeeId uniqueidentifier,AssignmentId uniqueidentifier,AuditId uniqueidentifier,EventId uniqueidentifier,OriginalSourceRow int);
INSERT @selected VALUES
VALUES;
DECLARE @created TABLE (Id uniqueidentifier PRIMARY KEY);
BEGIN TRY
 BEGIN TRANSACTION;
 IF (SELECT COUNT(*) FROM @selected)<>214 THROW 51500,'Unexpected assignment count.',1;
 IF NOT EXISTS (SELECT 1 FROM [identity].Users WHERE Id=@actor AND UserName=N'Omar' AND IsDeleted=0) THROW 51501,'Assignment actor no longer exists.',1;
 IF EXISTS (SELECT 1 FROM @selected s LEFT JOIN app.FuelCards c WITH(UPDLOCK,HOLDLOCK) ON c.Id=s.CardId AND c.IsDeleted=0 AND c.NormalizedCardNumber=s.CardNumber AND c.Provider=1 WHERE c.Id IS NULL) THROW 51502,'A reviewed fuel-card match changed.',1;
 IF EXISTS (SELECT 1 FROM @selected s LEFT JOIN app.Employees e WITH(UPDLOCK,HOLDLOCK) ON e.Id=s.EmployeeId AND e.IqamaNo=s.IqamaNo AND e.IsDeleted=0 AND e.IsEmployee=0 AND e.Status=3 LEFT JOIN app.RiderProfiles r WITH(UPDLOCK,HOLDLOCK) ON r.Id=s.RiderId AND r.EmployeeId=s.EmployeeId AND r.IsDeleted=0 WHERE e.Id IS NULL OR r.Id IS NULL) THROW 51503,'A reviewed rider is no longer eligible.',1;
 IF EXISTS (SELECT 1 FROM @selected s JOIN app.FuelCardRiderAssignments a WITH(UPDLOCK,HOLDLOCK) ON a.FuelCardId=s.CardId AND a.EffectiveTo IS NULL WHERE a.RiderProfileId<>s.RiderId OR a.EmployeeId<>s.EmployeeId) THROW 51504,'A selected card is assigned to a different rider.',1;
 IF EXISTS (SELECT 1 FROM @selected s JOIN app.FuelCardRiderAssignments a WITH(UPDLOCK,HOLDLOCK) ON a.FuelCardId=s.CardId WHERE a.EffectiveFrom<=@monthEnd AND (a.EffectiveTo IS NULL OR a.EffectiveTo>=@month) AND a.RiderProfileId<>s.RiderId) THROW 51505,'A selected card has a conflicting rider this month.',1;
 IF EXISTS (SELECT 1 FROM @selected s JOIN app.FuelCardMonthlyUsages u WITH(UPDLOCK,HOLDLOCK) ON u.FuelCardId=s.CardId WHERE u.IsDeleted=0 AND u.ReportMonth=@month AND u.RiderProfileId IS NOT NULL AND u.RiderProfileId<>s.RiderId) THROW 51506,'A selected card has conflicting monthly usage.',1;
 IF EXISTS (SELECT 1 FROM @selected s JOIN app.FuelCardRiderAssignments a ON a.FuelCardId=s.CardId WHERE a.EffectiveTo>=@effective) THROW 51507,'Assignment would overlap a closed period.',1;
 INSERT app.FuelCardRiderAssignments (Id,FuelCardId,RiderProfileId,EmployeeId,EffectiveFrom,AssignedByUserId,AssignmentReason,Notes,CreatedAtUtc,CreatedByUserId)
 OUTPUT inserted.Id INTO @created
 SELECT s.AssignmentId,s.CardId,s.RiderId,s.EmployeeId,@effective,@actor,N'Fuel-card assignment according to rider spreadsheet',
   @source+N'; original row '+CONVERT(nvarchar(10),s.OriginalSourceRow)+N'; SHA-256 '+@checksum,@now,@actor
 FROM @selected s WHERE NOT EXISTS (SELECT 1 FROM app.FuelCardRiderAssignments a WHERE a.FuelCardId=s.CardId AND a.EffectiveTo IS NULL);
 INSERT audit.AuditEntries (Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 SELECT s.AuditId,s.EventId,@actor,N'User',N'Created',N'Fuel',N'FuelCardRiderAssignment',s.AssignmentId,@now,N'FuelCardAssignments20261006',
   N'Rider spreadsheet import; '+@source+N'; SHA-256 '+@checksum,
   (SELECT a.Id,a.FuelCardId,a.RiderProfileId,a.EmployeeId,a.EffectiveFrom,a.AssignedByUserId,a.AssignmentReason,a.Notes FROM app.FuelCardRiderAssignments a WHERE a.Id=s.AssignmentId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),N'FuelCardRiderSpreadsheet',1,@now,@actor
 FROM @selected s JOIN @created n ON n.Id=s.AssignmentId;
 IF EXISTS (SELECT 1 FROM @selected s WHERE NOT EXISTS (SELECT 1 FROM app.FuelCardRiderAssignments a WHERE a.FuelCardId=s.CardId AND a.RiderProfileId=s.RiderId AND a.EmployeeId=s.EmployeeId AND a.EffectiveTo IS NULL)) THROW 51508,'Assignment reconciliation failed.',1;
 SELECT @Commit AS Committed,COUNT(*) AS CreatedAssignments,214-COUNT(*) AS AlreadyAssigned,@effective AS EffectiveFrom,@checksum AS SourceSha256 FROM @created;
 SELECT s.CardNumber,s.IqamaNo,s.RiderId,s.EmployeeId,a.Id AS AssignmentId,a.EffectiveFrom FROM @selected s JOIN app.FuelCardRiderAssignments a ON a.FuelCardId=s.CardId AND a.RiderProfileId=s.RiderId AND a.EmployeeId=s.EmployeeId AND a.EffectiveTo IS NULL ORDER BY s.CardNumber;
 IF @Commit=1 COMMIT TRANSACTION; ELSE ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
"""
sql=head.replace('ACTOR',q(actor)).replace('CHECKSUM',q(digest)).replace('VALUES;',',\n'.join(values)+';').replace('@selected)<>214','@selected)<>215').replace('214-COUNT(*)','215-COUNT(*)')
Path('database/scripts/assign-fuel-cards-20261006-refresh.sql').write_text(sql,encoding='utf-8')
print(json.dumps({'selectedCards':len(ready),'uniqueRiders':len({r['employees'][0]['RiderProfileId'] for r in ready.values()}),'sourceSha256':digest}))
