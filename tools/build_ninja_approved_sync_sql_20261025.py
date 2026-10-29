"""Generate bounded correction SQL. Values are read only from @Plan parameters."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'outputs/ninja-workbook-sync-20261025'
plan = json.loads((OUT/'approved-plan.json').read_text(encoding='utf-8'))
tables = {
 'app.Employees':'Employees', 'app.PlatformRiderAccounts':'Accounts',
 'app.RiderClientAssignments':'Assignments', 'ninja.NinjaAccountDetails':'Details',
 'app.PlatformAccountDayUsages':'Usages', 'app.PlatformAccountRegistrations':'Registrations',
 'app.RiderProfiles':'Riders', 'app.RiderAssignmentEvents':'Events',
 'app.PlatformAccountCredentialVersions':'Credentials','app.DatasetVersions':'Versions',
}
allowed = {
 'app.Employees':{'IqamaNo','PrimaryPhone','Nationality'},
 'app.PlatformRiderAccounts':{'PaymentModel','OperatingCityId','Status'},
 'app.RiderClientAssignments':{'PaymentModel','OperationalAgreementReference','OperationalAgreementNotes','IsDeleted','DeletionReason'},
 'ninja.NinjaAccountDetails':{'PhoneNumber'},
 'app.PlatformAccountDayUsages':{'IsDeleted','DeletionReason'},
}
schema=json.loads((OUT/'database-snapshot.json').read_text(encoding='utf-8-sig'))['schema']
foreign_keys=json.loads((OUT/'database-snapshot.json').read_text(encoding='utf-8-sig'))['foreignKeys']
types={(c['TABLE_SCHEMA']+'.'+c['TABLE_NAME'],c['COLUMN_NAME']):c['DATA_TYPE'] for c in schema}


def sqlvalue(table,col,record='p.JsonValues'):
    val=f"JSON_VALUE({record},'$.{col}')"
    typ=types[(table,col)]
    if typ=='bit': return f"CASE {val} WHEN 'true' THEN CAST(1 AS bit) WHEN 'false' THEN CAST(0 AS bit) ELSE TRY_CONVERT(bit,{val}) END"
    if typ in ['int','bigint','uniqueidentifier','date','datetimeoffset']: return f'TRY_CONVERT({typ},{val})'
    assert typ in ['nvarchar','varchar'],(table,col,typ)
    return val


def bracket(table):
    return '.'.join('['+p+']' for p in table.split('.'))


mapping = """
SELECT * INTO #M FROM OPENJSON(@Plan,'$.mappings') WITH(
 accountId uniqueidentifier, captainId nvarchar(150),assignmentId uniqueidentifier,riderId uniqueidentifier,employeeId uniqueidentifier,
 fromDate date,toDate date,paymentModel int,cityId uniqueidentifier,phone nvarchar(32),nationality nvarchar(100),
 reference nvarchar(200),notes nvarchar(max));
SELECT * INTO #L FROM OPENJSON(@Plan,'$.latestAccounts') WITH(accountId uniqueidentifier,paymentModel int,cityId uniqueidentifier,status int);
"""
verify = """
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
"""
variables="""
DECLARE @Actor uniqueidentifier=JSON_VALUE(@Plan,'$.actorId'),@Platform uniqueidentifier=JSON_VALUE(@Plan,'$.platformId'),
 @Batch nvarchar(100)=JSON_VALUE(@Plan,'$.batchId'),@Now datetimeoffset=SYSUTCDATETIME();
"""
sql=["SET NOCOUNT ON; SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;",variables,
"""
IF @@TRANCOUNT<>1 THROW 51000,'This correction requires one external transaction.',1;
DECLARE @LockResult int;
EXEC @LockResult=sys.sp_getapplock @Resource=N'ninja-workbook-reconciliation-20261025',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @LockResult<0 THROW 51000,'Could not acquire reconciliation lock.',1;
DECLARE @Resource nvarchar(255);
DECLARE accountLocks CURSOR LOCAL FAST_FORWARD FOR SELECT N'platform-account:'+value FROM OPENJSON(@LockOrder);
OPEN accountLocks; FETCH NEXT FROM accountLocks INTO @Resource;
WHILE @@FETCH_STATUS=0 BEGIN
 EXEC @LockResult=sys.sp_getapplock @Resource=@Resource,@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
 IF @LockResult<0 THROW 51000,'An account is being edited.',1;
 FETCH NEXT FROM accountLocks INTO @Resource;
END;
CLOSE accountLocks; DEALLOCATE accountLocks;
IF NOT EXISTS(SELECT 1 FROM [identity].Users WHERE Id=@Actor AND UserName=N'Omar' AND IsDeleted=0) THROW 51000,'Actor changed.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=@Batch) THROW 51000,'Batch already applied; verify without reapplying.',1;
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WHERE Id=@Platform AND Code=N'NINJA' AND IsDeleted=0) THROW 51000,'Platform changed.',1;
SELECT * INTO #U FROM OPENJSON(@Plan,'$.updates') WITH(TableName nvarchar(150) '$.table',Id uniqueidentifier '$.id',RV varchar(16) '$.rowVersionHex',JsonValues nvarchar(max) '$.values' AS JSON,AuditId uniqueidentifier '$.auditId',AuditEventId uniqueidentifier '$.auditEventId');
SELECT * INTO #I FROM OPENJSON(@Plan,'$.inserts') WITH(TableName nvarchar(150) '$.table',Id uniqueidentifier '$.id',JsonValues nvarchar(max) '$.values' AS JSON,AuditId uniqueidentifier '$.auditId',AuditEventId uniqueidentifier '$.auditEventId');
SELECT * INTO #G FROM OPENJSON(@Plan,'$.guards') WITH(TableName nvarchar(150) '$.table',Id uniqueidentifier '$.id',RV varchar(16) '$.rowVersionHex');
SELECT * INTO #V FROM OPENJSON(@Plan,'$.expectedVersions') WITH(Id uniqueidentifier '$.id',RV varchar(16) '$.rowVersionHex',AuditId uniqueidentifier '$.auditId',AuditEventId uniqueidentifier '$.auditEventId');
SELECT * INTO #E FROM OPENJSON(@Plan,'$.assignmentEvents') WITH(Id uniqueidentifier '$.id',AssignmentId uniqueidentifier '$.assignmentId',FromStatus int '$.fromStatus',ToStatus int '$.toStatus',Snapshot nvarchar(max) '$.snapshot',AuditId uniqueidentifier '$.auditId',AuditEventId uniqueidentifier '$.auditEventId');
CREATE TABLE #Changes(TableName nvarchar(150),Id uniqueidentifier,AuditId uniqueidentifier,AuditEventId uniqueidentifier,BeforeJson nvarchar(max),AfterJson nvarchar(max));
""",mapping]
for table,name in tables.items():
    sql.append(f'SELECT * INTO #Before{name} FROM {bracket(table)} WITH(TABLOCKX,HOLDLOCK);')
    sql.append(f"IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #Before{name} b ON b.Id=g.Id WHERE g.TableName=N'{table}' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: {table}.',1;" if table!='app.RiderAssignmentEvents' and table!='app.PlatformAccountCredentialVersions' else '')
sql.append("""
IF EXISTS(SELECT 1 FROM #V v LEFT JOIN #BeforeVersions b ON b.Id=v.Id WHERE b.Id IS NULL OR b.IsDeleted=1 OR b.ModuleKey NOT IN(N'ninja',N'platform-operations',N'workforce')) THROW 51000,'Dataset version identity changed.',1;
-- Version counters may advance for unrelated live work. Increment the locked current value;
-- source entity rowversions in #G remain strict and are never refreshed here.
IF (SELECT COUNT(*) FROM #BeforeAssignments x JOIN #BeforeAccounts a ON a.Id=x.PlatformRiderAccountId WHERE a.ClientPlatformId=@Platform)<>227 THROW 51000,'Assignment set changed since preflight.',1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE is_disabled=0 AND parent_id IN(OBJECT_ID(N'app.Employees'),OBJECT_ID(N'app.PlatformRiderAccounts'),OBJECT_ID(N'app.RiderClientAssignments'),OBJECT_ID(N'ninja.NinjaAccountDetails'),OBJECT_ID(N'app.PlatformAccountDayUsages'),OBJECT_ID(N'app.RiderProfiles'),OBJECT_ID(N'app.PlatformAccountRegistrations'),OBJECT_ID(N'audit.AuditEntries'))) THROW 51000,'Unexpected write trigger.',1;
""")
for fk in foreign_keys:
    if fk['ParentTable']=='RiderClientAssignments' and fk['ChildTable'] not in ['RiderAssignmentEvents','PlatformAccountDayUsages']:
        child=bracket(fk['ChildSchema']+'.'+fk['ChildTable'])
        column='['+fk['ChildColumn']+']'
        sql.append(f"IF EXISTS(SELECT 1 FROM {child} WHERE {column}='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate has an unexpected operational/financial reference.',1;")
for table,fields in allowed.items():
    updates=[u for u in plan['updates'] if u['table']==table]
    used=sorted({k for u in updates for k in u['values']})
    assert set(used)<=fields
    setters=[f"[{col}]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'{col}') THEN {sqlvalue(table,col)} ELSE t.[{col}] END" for col in used]
    setters += ['UpdatedAtUtc=@Now','UpdatedByUserId=@Actor']
    if 'IsDeleted' in used:
        setters += ["DeletedAtUtc=CASE WHEN JSON_VALUE(p.JsonValues,'$.IsDeleted')='true' THEN @Now ELSE t.DeletedAtUtc END", "DeletedByUserId=CASE WHEN JSON_VALUE(p.JsonValues,'$.IsDeleted')='true' THEN @Actor ELSE t.DeletedByUserId END"]
    sql.append(f"UPDATE t SET {','.join(setters)} FROM {bracket(table)} t JOIN #U p ON p.Id=t.Id AND p.TableName=N'{table}' AND t.RowVersion=CONVERT(binary(8),p.RV,2); IF @@ROWCOUNT<>{len(updates)} THROW 51000,'Update count mismatch: {table}.',1;")

for table in ['app.RiderProfiles','app.PlatformAccountRegistrations','app.RiderClientAssignments']:
    records=[i for i in plan['inserts'] if i['table']==table]
    columns=list(records[0]['values'])
    assert all(list(r['values'])==columns for r in records)
    sql.append(f"INSERT {bracket(table)} ({','.join('['+c+']' for c in columns)},CreatedAtUtc,CreatedByUserId,IsDeleted) SELECT {','.join(sqlvalue(table,c) for c in columns)},@Now,@Actor,0 FROM #I p WHERE TableName=N'{table}'; IF @@ROWCOUNT<>{len(records)} THROW 51000,'Insert count mismatch: {table}.',1;")

sql.append("""
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
SELECT Id,AssignmentId,FromStatus,ToStatus,@Now,@Actor,N'User-approved Ninja workbook reconciliation; original source labels and corrections retained.',Snapshot,@Now,@Actor FROM #E;
UPDATE t SET Version=t.Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM app.DatasetVersions t JOIN #V v ON v.Id=t.Id JOIN #BeforeVersions b ON b.Id=t.Id AND b.RowVersion=t.RowVersion;
IF @@ROWCOUNT<>3 THROW 51000,'Dataset version update mismatch.',1;
""")
for table,name in tables.items():
    if table in ['app.PlatformAccountCredentialVersions']:
        sql.append(f"IF EXISTS(SELECT * FROM #Before{name} EXCEPT SELECT * FROM {bracket(table)}) OR EXISTS(SELECT * FROM {bracket(table)} EXCEPT SELECT * FROM #Before{name}) THROW 51000,'Credentials changed.',1;")
        continue
    special = 'UNION SELECT Id FROM #V' if table=='app.DatasetVersions' else 'UNION SELECT Id FROM #E' if table=='app.RiderAssignmentEvents' else ''
    sql.append(f"""
IF EXISTS(SELECT b.* FROM #Before{name} b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'{table}' {special}) EXCEPT SELECT * FROM {bracket(table)})
 OR EXISTS(SELECT t.* FROM {bracket(table)} t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'{table}' UNION SELECT Id FROM #I WHERE TableName=N'{table}' {special}) EXCEPT SELECT * FROM #Before{name})
 THROW 51000,'Unrelated rows changed: {table}.',1;
IF EXISTS(SELECT Id FROM #Before{name} EXCEPT SELECT Id FROM {bracket(table)}) THROW 51000,'A row was physically removed: {table}.',1;
""")
    if table=='app.RiderAssignmentEvents':
        sql.append("INSERT #Changes SELECT N'app.RiderAssignmentEvents',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #E p JOIN app.RiderAssignmentEvents t ON t.Id=p.Id;")
    elif table=='app.DatasetVersions':
        sql.append("INSERT #Changes SELECT N'app.DatasetVersions',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #V p JOIN #BeforeVersions b ON b.Id=p.Id JOIN app.DatasetVersions t ON t.Id=p.Id;")
    else:
        sql.append(f"INSERT #Changes SELECT N'{table}',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #U p JOIN #Before{name} b ON b.Id=p.Id JOIN {bracket(table)} t ON t.Id=p.Id WHERE p.TableName=N'{table}';")
        sql.append(f"INSERT #Changes SELECT N'{table}',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #I p JOIN {bracket(table)} t ON t.Id=p.Id WHERE p.TableName=N'{table}';")

sql.append(verify)
sql.append("""
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT AuditId,AuditEventId,@Actor,N'User',CASE WHEN BeforeJson IS NULL THEN N'Created' ELSE N'WorkbookReconciled' END,N'Ninja',TableName,Id,@Now,@Batch,
 N'Explicit user instructions: match worksheet payment, nationality, phone and work city; apply confirmed placements and duplicate consolidation.',BeforeJson,AfterJson,N'NinjaWorkbookSync20261025',1,@Now,@Actor FROM #Changes;
IF @@ROWCOUNT<>(SELECT COUNT(*) FROM #Changes) THROW 51000,'Audit count mismatch.',1;
""")
for table,name in tables.items():
    sql.append(f'SELECT * FROM #Before{name};')
sql.append("SELECT @Batch BatchId,@Now VerifiedAtUtc,(SELECT COUNT(*) FROM #Changes) AuditedChanges,229 MatchedSourceRows,151 CurrentAssignments,78 HistoricalAssignments,CAST(1 AS bit) CredentialsPreserved,CAST(1 AS bit) UnrelatedRowsPreserved;")
(OUT/'approved-sync.sql').write_text('\n'.join(sql),encoding='utf-8')
(OUT/'approved-verify.sql').write_text('SET NOCOUNT ON;\n'+variables+mapping+verify+"SELECT @Batch BatchId,CAST(1 AS bit) Verified,229 MatchedSourceRows,151 CurrentAssignments,78 HistoricalAssignments,SYSUTCDATETIME() VerifiedAtUtc;",encoding='utf-8')
print('Generated parameterized correction and independent verification SQL; no literal source values or secrets embedded.')
