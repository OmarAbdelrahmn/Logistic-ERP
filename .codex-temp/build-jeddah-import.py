"""Generate the transaction from validated workbook rows and reviewed ERP IDs."""
import hashlib
import json
import secrets
import time
import uuid
from pathlib import Path

root = Path(__file__).parent
data = json.loads((root / 'jeddah-reconciliation.json').read_text(encoding='utf-8'))
current = json.loads((root / 'jeddah-current.json').read_text(encoding='utf-8-sig'))
book = json.loads((root / 'jeddah-workbook.json').read_text(encoding='utf-8'))
source = Path(r'C:\Users\omarf\Downloads\بيانات_السكن_الموحدة_ERP_جدة.xlsx')
source_hash = hashlib.sha256(source.read_bytes()).hexdigest()

def uid():
    bits = (int(time.time() * 1000) << 80) | (7 << 76) | (secrets.randbits(12) << 64) | (2 << 62) | secrets.randbits(62)
    return str(uuid.UUID(int=bits))

def lit(value):
    if value is None:
        return 'NULL'
    if isinstance(value, int):
        return str(value)
    return "N'" + str(value).replace("'", "''") + "'"

def values(rows):
    return ',\n'.join('    (' + ', '.join(lit(v) for v in row) + ')' for row in rows) + ';\n'

housing_ids = {'safa':'01a0a427-51c1-7477-b93e-cee215d22268', 'SAMER':'01a0a428-5ea7-710e-8e7f-2e8550e2c8be','MECANIC':'01a0a5e0-535c-7236-afa6-3708df20f662'}
housing_rows = []
for code, hid in housing_ids.items():
    v = next(r['values'] for r in book[-1]['rows'][1:] if r['values'][0] == code)
    old = next(h for h in current[0] if h['Id'] == hid)
    s = data['summary'][code]
    housing_rows.append((hid, old['Code'], code, v[1], v[2], v[4], s['rooms'],s['capacity'],s['occupants']))
floor_ids = {}
floor_rows = []
for code, hid in housing_ids.items():
    floors = list(dict.fromkeys(r['floor'] for r in data['rooms'] if r['code'] == code))
    original = next(f for f in current[1] if f['HousingId'] == hid and not f['IsDeleted'])
    for i, name in enumerate(floors):
        fid = original['Id'] if i == 0 else uid()
        floor_ids[(code,name)] = fid
        floor_rows.append((fid, hid, name, original['Name'] if i == 0 else None))
room_ids = {}
room_rows = []
for r in data['rooms']:
    hid = housing_ids[r['code']]
    old = next((v for v in current[2] if v['HousingId'] == hid and v['Name'] == r['room'] and not v['IsDeleted']),None)
    rid = old['Id'] if old else uid()
    room_ids[(r['code'],r['floor'],r['room'])] = rid
    count = sum((p['code'],p['floor'],p['room']) == (r['code'],r['floor'],r['room']) for p in data['people'])
    room_rows.append((rid,hid,floor_ids[(r['code'],r['floor'])],r['room'],r['name'],r['capacity'],count))
person_rows = [(p['row'],room_ids[(p['code'],p['floor'],p['room'])],p['iqama'],p['name'],p['type'],p['work'],p['notes'],uid(),uid()) for p in data['people']]
sql = f"""-- Jeddah housing reconciliation, supplied workbook: {source.name}
-- SHA-256: {source_hash}
-- Source sheet: بيانات الرفع لنظام ERP. 3 housing records, 27 rooms, 215 beds, 139 occupants.
-- Reuses existing housing, floor and room IDs. Keeps existing residence periods unchanged.
-- No employee records are created or changed; missing identities use pending/name-only records.
-- Requires a bit command parameter @Commit. False executes and verifies, then rolls back.
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
DECLARE @effective date='2026-10-05';
DECLARE @actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001';
DECLARE @source nvarchar(200)=N'Jeddah housing workbook 2026-10-05';
DECLARE @housing table (Id uniqueidentifier PRIMARY KEY, OldCode nvarchar(32), Code nvarchar(32), NameAr nvarchar(200), NameEn nvarchar(200), District nvarchar(200), Rooms int, Capacity int, Occupants int, BeforeJson nvarchar(max));
INSERT @housing (Id,OldCode,Code,NameAr,NameEn,District,Rooms,Capacity,Occupants) VALUES
{values(housing_rows)}
DECLARE @floors table (Id uniqueidentifier PRIMARY KEY,HousingId uniqueidentifier,Name nvarchar(100),OldName nvarchar(100));
INSERT @floors VALUES
{values(floor_rows)}
DECLARE @rooms table (Id uniqueidentifier PRIMARY KEY,HousingId uniqueidentifier,FloorId uniqueidentifier,OldName nvarchar(100),Name nvarchar(100),Capacity int,Occupants int);
INSERT @rooms VALUES
{values(room_rows)}
DECLARE @people table (SourceRow int PRIMARY KEY,RoomId uniqueidentifier,IqamaNo varchar(10),Name nvarchar(200),OccupantType nvarchar(100),PlatformWork nvarchar(200),Notes nvarchar(1000),PeriodId uniqueidentifier,OccupantId uniqueidentifier,EmployeeId uniqueidentifier);
INSERT @people (SourceRow,RoomId,IqamaNo,Name,OccupantType,PlatformWork,Notes,PeriodId,OccupantId) VALUES
{values(person_rows)}
DECLARE @updatedHousing int=0,@updatedFloors int=0,@createdFloors int=0,@updatedRooms int=0,@createdRooms int=0,@assigned int=0,@createdPending int=0,@createdNameOnly int=0,@resolvedPending int=0,@occupancyUpdates int=0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lockResult int;
    EXEC @lockResult=sys.sp_getapplock @Resource='Housing:Jeddah:Workbook20261005',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
    IF @lockResult<0 THROW 51100,'Could not lock the Jeddah housing import.',1;
    IF EXISTS (SELECT 1 FROM @housing s LEFT JOIN app.Housing h WITH (UPDLOCK,HOLDLOCK) ON h.Id=s.Id
               WHERE h.Id IS NULL OR h.IsDeleted=1 OR h.CityId<>'019c18d5-62e1-7000-8000-000000000002' OR h.Status<>2 OR h.Code NOT IN (s.Code,s.OldCode))
        THROW 51101,'A reviewed Jeddah housing record changed or is unavailable.',1;
    IF EXISTS (SELECT 1 FROM @housing s JOIN app.Housing h WITH (UPDLOCK,HOLDLOCK) ON h.Code=s.Code AND h.Id<>s.Id)
        THROW 51102,'A workbook housing code is already used by another housing.',1;
    IF EXISTS (SELECT 1 FROM app.HousingRooms r WITH (UPDLOCK,HOLDLOCK) JOIN @housing h ON h.Id=r.HousingId WHERE r.IsDeleted=0 AND NOT EXISTS (SELECT 1 FROM @rooms s WHERE s.Id=r.Id))
        THROW 51103,'An additional Jeddah room needs reconciliation before importing.',1;
    IF EXISTS (SELECT 1 FROM app.HousingFloors f WITH (UPDLOCK,HOLDLOCK) JOIN @housing h ON h.Id=f.HousingId WHERE f.IsDeleted=0 AND NOT EXISTS (SELECT 1 FROM @floors s WHERE s.Id=f.Id))
        THROW 51104,'An additional Jeddah floor needs reconciliation before importing.',1;
    IF EXISTS (SELECT 1 FROM @rooms s JOIN app.HousingRooms r ON r.Id=s.Id WHERE r.HousingId<>s.HousingId OR r.IsDeleted=1 OR r.Name NOT IN (s.OldName,s.Name))
        THROW 51105,'A reviewed room identity or name changed.',1;
    IF EXISTS (SELECT 1 FROM @floors s JOIN app.HousingFloors f ON f.Id=s.Id WHERE f.HousingId<>s.HousingId OR f.IsDeleted=1 OR (f.Name<>s.Name AND (s.OldName IS NULL OR f.Name<>s.OldName)))
        THROW 51106,'A reviewed floor identity or name changed.',1;
    UPDATE p SET EmployeeId=e.Id FROM @people p JOIN app.Employees e WITH (UPDLOCK,HOLDLOCK) ON e.IqamaNo=p.IqamaNo AND e.IsDeleted=0;
    IF EXISTS (SELECT 1 FROM @people p JOIN app.HousingResidencePeriods x WITH (UPDLOCK,HOLDLOCK) ON x.EmployeeId=p.EmployeeId AND x.EffectiveTo IS NULL WHERE x.RoomId<>p.RoomId)
        THROW 51107,'A listed employee already occupies a different room; no assignments were moved.',1;
    IF EXISTS (SELECT 1 FROM app.HousingResidencePeriods x WITH (UPDLOCK,HOLDLOCK) JOIN app.HousingRooms r ON r.Id=x.RoomId JOIN @housing h ON h.Id=r.HousingId WHERE x.EffectiveTo IS NULL AND NOT EXISTS (SELECT 1 FROM @people p WHERE p.EmployeeId=x.EmployeeId AND p.RoomId=x.RoomId))
        THROW 51108,'An existing Jeddah resident is missing from the workbook.',1;
    IF EXISTS (SELECT 1 FROM app.HousingExternalOccupants x WITH (UPDLOCK,HOLDLOCK) JOIN app.HousingRooms r ON r.Id=x.RoomId JOIN @housing h ON h.Id=r.HousingId WHERE x.IsDeleted=0 AND NOT EXISTS (SELECT 1 FROM @people p WHERE p.IqamaNo IS NULL AND p.RoomId=x.RoomId AND p.Name=x.Name))
        THROW 51109,'An existing name-only Jeddah resident is missing from the workbook.',1;
    IF EXISTS (SELECT 1 FROM app.HousingPendingOccupants x WITH (UPDLOCK,HOLDLOCK) JOIN app.HousingRooms r ON r.Id=x.RoomId JOIN @housing h ON h.Id=r.HousingId WHERE x.IsDeleted=0 AND NOT EXISTS (SELECT 1 FROM @people p WHERE p.IqamaNo=x.IqamaNo AND p.RoomId=x.RoomId))
        THROW 51110,'An existing pending Jeddah resident is missing from the workbook.',1;
    IF EXISTS (SELECT 1 FROM @people p JOIN app.HousingPendingOccupants x WITH (UPDLOCK,HOLDLOCK) ON x.IqamaNo=p.IqamaNo AND x.IsDeleted=0 WHERE x.RoomId<>p.RoomId)
        THROW 51111,'A listed pending iqama occupies another room.',1;

    UPDATE s SET BeforeJson=(SELECT h.Code,h.NameAr,h.NameEn,h.AddressDistrict,h.AddressCity,
        JSON_QUERY((SELECT f.Id,f.Name FROM app.HousingFloors f WHERE f.HousingId=h.Id AND f.IsDeleted=0 FOR JSON PATH)) AS Floors,
        JSON_QUERY((SELECT r.Id,r.FloorId,r.Name,r.Capacity,r.CurrentOccupancy FROM app.HousingRooms r WHERE r.HousingId=h.Id AND r.IsDeleted=0 FOR JSON PATH)) AS Rooms
        FROM app.Housing h WHERE h.Id=s.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM @housing s;

    UPDATE h SET Code=s.Code,NameAr=s.NameAr,NameEn=s.NameEn,AddressCity=N'جدة',AddressDistrict=s.District,UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.Housing h JOIN @housing s ON s.Id=h.Id
    WHERE h.Code COLLATE Latin1_General_100_BIN2<>s.Code COLLATE Latin1_General_100_BIN2 OR h.NameAr<>s.NameAr OR h.NameEn COLLATE Latin1_General_100_BIN2<>s.NameEn COLLATE Latin1_General_100_BIN2 OR ISNULL(h.AddressCity,N'')<>N'جدة' OR ISNULL(h.AddressDistrict,N'')<>s.District;
    SET @updatedHousing=@@ROWCOUNT;
    UPDATE f SET Name=s.Name,UpdatedAtUtc=@now,UpdatedByUserId=@actor FROM app.HousingFloors f JOIN @floors s ON s.Id=f.Id WHERE f.Name<>s.Name;
    SET @updatedFloors=@@ROWCOUNT;
    INSERT app.HousingFloors (Id,HousingId,Name,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT s.Id,s.HousingId,s.Name,@now,@actor,0 FROM @floors s WHERE NOT EXISTS (SELECT 1 FROM app.HousingFloors f WHERE f.Id=s.Id);
    SET @createdFloors=@@ROWCOUNT;
    UPDATE r SET FloorId=s.FloorId,Name=s.Name,Capacity=s.Capacity,UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.HousingRooms r JOIN @rooms s ON s.Id=r.Id WHERE r.FloorId<>s.FloorId OR r.Name<>s.Name OR r.Capacity<>s.Capacity;
    SET @updatedRooms=@@ROWCOUNT;
    INSERT app.HousingRooms (Id,HousingId,FloorId,Name,Capacity,CurrentOccupancy,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT s.Id,s.HousingId,s.FloorId,s.Name,s.Capacity,0,@now,@actor,0 FROM @rooms s WHERE NOT EXISTS (SELECT 1 FROM app.HousingRooms r WHERE r.Id=s.Id);
    SET @createdRooms=@@ROWCOUNT;
    -- Pending iqamas are resolved only when a real matching employee exists.
    UPDATE x SET IsDeleted=1,DeletedAtUtc=@now,DeletedByUserId=@actor,DeletionReason=N'Resolved by '+@source,UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.HousingPendingOccupants x JOIN @people p ON p.IqamaNo=x.IqamaNo AND p.RoomId=x.RoomId WHERE p.EmployeeId IS NOT NULL AND x.IsDeleted=0;
    SET @resolvedPending=@@ROWCOUNT;
    INSERT app.HousingResidencePeriods (Id,EmployeeId,RoomId,EffectiveFrom,AssignedByUserId,CreatedAtUtc,CreatedByUserId,SourceReference,MoveInReason,CapacityOverrideUsed)
    SELECT p.PeriodId,p.EmployeeId,p.RoomId,@effective,@actor,@now,@actor,@source+N', ERP row '+CONVERT(nvarchar(10),p.SourceRow),
        N'حسب كشف السكن: '+p.Name+N'؛ التصنيف: '+p.OccupantType+N'؛ العمل: '+p.PlatformWork+N'؛ الملاحظات: '+p.Notes,0
    FROM @people p WHERE p.EmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM app.HousingResidencePeriods x WHERE x.EmployeeId=p.EmployeeId AND x.EffectiveTo IS NULL);
    SET @assigned=@@ROWCOUNT;
    INSERT app.HousingPendingOccupants (Id,RoomId,IqamaNo,Name,SourceRow,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT p.OccupantId,p.RoomId,p.IqamaNo,p.Name,p.SourceRow,@now,@actor,0 FROM @people p
    WHERE p.IqamaNo IS NOT NULL AND p.EmployeeId IS NULL AND NOT EXISTS (SELECT 1 FROM app.HousingPendingOccupants x WHERE x.IqamaNo=p.IqamaNo AND x.IsDeleted=0);
    SET @createdPending=@@ROWCOUNT;
    INSERT app.HousingExternalOccupants (Id,RoomId,Name,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT p.OccupantId,p.RoomId,p.Name,@now,@actor,0 FROM @people p WHERE p.IqamaNo IS NULL
    AND NOT EXISTS (SELECT 1 FROM app.HousingExternalOccupants x WHERE x.RoomId=p.RoomId AND x.Name=p.Name AND x.IsDeleted=0);
    SET @createdNameOnly=@@ROWCOUNT;
    UPDATE r SET CurrentOccupancy=q.Occupants,UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.HousingRooms r JOIN @rooms s ON s.Id=r.Id
    CROSS APPLY (SELECT (SELECT COUNT(*) FROM app.HousingResidencePeriods x WHERE x.RoomId=r.Id AND x.EffectiveTo IS NULL)
        +(SELECT COUNT(*) FROM app.HousingPendingOccupants x WHERE x.RoomId=r.Id AND x.IsDeleted=0)
        +(SELECT COUNT(*) FROM app.HousingExternalOccupants x WHERE x.RoomId=r.Id AND x.IsDeleted=0) AS Occupants) q
    WHERE r.CurrentOccupancy<>q.Occupants;
    SET @occupancyUpdates=@@ROWCOUNT;

    IF EXISTS (SELECT 1 FROM @rooms s LEFT JOIN app.HousingRooms r ON r.Id=s.Id WHERE r.Id IS NULL OR r.IsDeleted=1 OR r.HousingId<>s.HousingId OR r.FloorId<>s.FloorId OR r.Name<>s.Name OR r.Capacity<>s.Capacity OR r.CurrentOccupancy<>s.Occupants)
        THROW 51112,'Room identity, capacity or occupancy failed workbook reconciliation.',1;
    IF EXISTS (SELECT 1 FROM @people p WHERE (p.EmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM app.HousingResidencePeriods x WHERE x.EmployeeId=p.EmployeeId AND x.RoomId=p.RoomId AND x.EffectiveTo IS NULL))
        OR (p.EmployeeId IS NULL AND p.IqamaNo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM app.HousingPendingOccupants x WHERE x.IqamaNo=p.IqamaNo AND x.RoomId=p.RoomId AND x.Name=p.Name AND x.IsDeleted=0))
        OR (p.IqamaNo IS NULL AND NOT EXISTS (SELECT 1 FROM app.HousingExternalOccupants x WHERE x.RoomId=p.RoomId AND x.Name=p.Name AND x.IsDeleted=0)))
        THROW 51113,'An occupant was not recorded in the exact workbook room.',1;
    IF EXISTS (SELECT 1 FROM @housing s CROSS APPLY (SELECT COUNT(*) AS Rooms,SUM(r.Capacity) AS Capacity,SUM(r.CurrentOccupancy) AS Occupants FROM app.HousingRooms r WHERE r.HousingId=s.Id AND r.IsDeleted=0) q WHERE q.Rooms<>s.Rooms OR q.Capacity<>s.Capacity OR q.Occupants<>s.Occupants)
        THROW 51114,'Housing totals failed workbook reconciliation.',1;

    IF @updatedHousing+@updatedFloors+@createdFloors+@updatedRooms+@createdRooms+@assigned+@createdPending+@createdNameOnly+@resolvedPending+@occupancyUpdates>0
    INSERT audit.AuditEntries (Id,EventId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc)
    SELECT NEWID(),NEWID(),N'System',N'Imported',N'Housing',N'Housing',s.Id,@now,N'JeddahHousing20261005',
        N'Workbook reconciliation; SHA-256 {source_hash}',s.BeforeJson,
        (SELECT h.Code,h.NameAr,h.NameEn,h.AddressDistrict,h.AddressCity,
            JSON_QUERY((SELECT f.Id,f.Name FROM app.HousingFloors f WHERE f.HousingId=h.Id AND f.IsDeleted=0 FOR JSON PATH)) AS Floors,
            JSON_QUERY((SELECT r.Id,r.FloorId,r.Name,r.Capacity,r.CurrentOccupancy FROM app.HousingRooms r WHERE r.HousingId=h.Id AND r.IsDeleted=0 FOR JSON PATH)) AS Rooms,
            JSON_QUERY((SELECT p.SourceRow,p.IqamaNo,p.Name,p.OccupantType,p.PlatformWork,p.Notes,p.RoomId,p.EmployeeId FROM @people p JOIN @rooms r ON r.Id=p.RoomId WHERE r.HousingId=h.Id FOR JSON PATH)) AS Occupants
        FROM app.Housing h WHERE h.Id=s.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),N'JeddahHousingWorkbookImport',1,@now FROM @housing s;

    SELECT h.Code,h.NameAr,COUNT(r.Id) AS Rooms,SUM(r.Capacity) AS Capacity,SUM(r.CurrentOccupancy) AS Occupants,SUM(r.Capacity-r.CurrentOccupancy) AS Vacancies
    FROM @housing s JOIN app.Housing h ON h.Id=s.Id JOIN app.HousingRooms r ON r.HousingId=h.Id AND r.IsDeleted=0 GROUP BY h.Code,h.NameAr ORDER BY h.Code;
    SELECT p.SourceRow,h.Code,f.Name AS Floor,r.Name AS Room,p.IqamaNo,p.Name,
        CASE WHEN p.IqamaNo IS NULL THEN 'NameOnly' ELSE 'PendingIqama' END AS RecordType
    FROM @people p JOIN app.HousingRooms r ON r.Id=p.RoomId JOIN app.Housing h ON h.Id=r.HousingId JOIN app.HousingFloors f ON f.Id=r.FloorId WHERE p.EmployeeId IS NULL ORDER BY p.SourceRow;
    IF @Commit=1 COMMIT TRANSACTION; ELSE ROLLBACK TRANSACTION;
    SELECT CASE WHEN @Commit=1 THEN 'Imported' ELSE 'ValidatedAndRolledBack' END AS ImportStatus,
        @updatedHousing AS UpdatedHousing,@updatedFloors AS UpdatedFloors,@createdFloors AS CreatedFloors,@updatedRooms AS UpdatedRooms,@createdRooms AS CreatedRooms,
        @assigned AS NewEmployeeAssignments,@createdPending AS NewPendingOccupants,@createdNameOnly AS NewNameOnlyOccupants,@resolvedPending AS ResolvedPending,@occupancyUpdates AS UpdatedRoomOccupancy;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
"""
dest = root.parent / 'database/scripts/import-jeddah-housing-20261005.sql'
dest.write_text(sql,encoding='utf-8')
print(json.dumps({'sql':str(dest),'sourceSha256':source_hash,'housing':len(housing_rows),'floors':len(floor_rows),'rooms':len(room_rows),'occupants':len(person_rows)},ensure_ascii=False))
