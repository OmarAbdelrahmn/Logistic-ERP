import json,csv,collections
from decimal import Decimal as D, ROUND_HALF_UP
from pathlib import Path

root=Path(__file__).resolve().parents[1]
out=root/'outputs/chefz-vat-correction-20261017'
db=json.loads((out/'database-snapshot.json').read_text(encoding='utf-8-sig'),parse_float=D)
m=lambda v:D(str(v or 0))
r=lambda v:v.quantize(D('.01'),rounding=ROUND_HALF_UP)
rows=[dict(s) for s in db['settlements'] if s['Source']=='Orders']
groups=collections.defaultdict(list)
for s in rows:
 for k in ['DeliveryFees','VatAmount','GrossEarnings','Tips','Compensation','Bonuses','Deductions','CompanyAmount','RiderAmount']:s[k]=m(s[k])
 s['OldVatAmount']=s['VatAmount'];s['OldGrossEarnings']=s['GrossEarnings'];s['OldCompanyAmount']=s['CompanyAmount'];s['OldRiderAmount']=s['RiderAmount']
 s['VatAmount']=s['DeliveryFees']-r(s['DeliveryFees']/D('1.15'))
 groups[(s['ImportId'],s['RiderProfileId'],s['PeriodFrom'])].append(s)
for g in groups.values():
 g.sort(key=lambda s:(-abs(s['DeliveryFees']),s['PlatformRiderAccountId'],s['RiderClientAssignmentId']))
 fees=sum(s['DeliveryFees'] for s in g)
 g[0]['VatAmount']+=fees-r(fees/D('1.15'))-sum(s['VatAmount'] for s in g)
 for s in g:
  s['GrossEarnings']=s['DeliveryFees']-s['VatAmount']+s['Tips']+s['Compensation']+s['Bonuses']-s['Deductions']
  s['CompanyAmount']=r(s['GrossEarnings']*D('.15')) if s['AccountType']==1 else m(s['AssignedDays'])*25
 fs=sorted((s for s in g if s['AccountType']==1),key=lambda s:(-abs(s['GrossEarnings']),s['PlatformRiderAccountId'],s['RiderClientAssignmentId']))
 if fs:fs[0]['CompanyAmount']+=r(sum(s['GrossEarnings'] for s in fs)*D('.15'))-sum(s['CompanyAmount'] for s in fs)
 for s in g:s['RiderAmount']=s['GrossEarnings']-s['CompanyAmount']
summaries=[]
for batch in db['imports']:
 g=[s for s in rows if s['ImportId']==batch['Id']]
 if not g:continue
 summary={'importId':batch['Id'],'file':batch['FileName'],'settlements':len(g),'sourceRows':batch['SourceRows']}
 for k in ['VatAmount','GrossEarnings','CompanyAmount','RiderAmount']:
  summary['old'+k]=sum(s['Old'+k] for s in g);summary['new'+k]=sum(s[k] for s in g);summary['change'+k]=summary['new'+k]-summary['old'+k]
 summaries.append(summary)
plan={'rule':'VAT-inclusive delivery fees / 1.15; reconcile per original import/rider/cycle; retain original assigned-day snapshots','databaseRead':db['target'],'imports':summaries,'settlements':rows}
(out/'correction-plan.json').write_text(json.dumps(plan,ensure_ascii=False,default=str,indent=2),encoding='utf-8')
columns=['Id','ImportId','DriverId','RiderName','AccountType','AssignedDays','DeliveryFees','OldVatAmount','VatAmount','OldGrossEarnings','GrossEarnings','OldCompanyAmount','CompanyAmount','OldRiderAmount','RiderAmount']
with (out/'settlement-corrections.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=columns,extrasaction='ignore');w.writeheader();w.writerows(rows)
print(json.dumps(summaries,ensure_ascii=True,default=str,indent=2))

# Review mode performs SELECTs only. Applying requires explicit switches and an
# accountable existing user, and rejects any changed source snapshot atomically.
guards={'ImportId':'uniqueidentifier','RiderClientAssignmentId':'uniqueidentifier','RiderProfileId':'uniqueidentifier',
 'AccountType':'int','AssignedDays':'int','SourceRows':'int','PeriodFrom':'date','ChargedFrom':'date','ChargedTo':'date',
 'DeliveryFees':'decimal(18,2)','Tips':'decimal(18,2)','Compensation':'decimal(18,2)','Bonuses':'decimal(18,2)','Deductions':'decimal(18,2)',
 'OldVatAmount':'decimal(18,2)','OldGrossEarnings':'decimal(18,2)','OldCompanyAmount':'decimal(18,2)','OldRiderAmount':'decimal(18,2)'}
new_fields=['VatAmount','GrossEarnings','CompanyAmount','RiderAmount']
def literal(value,kind):
 if kind=='date':return "'"+str(value)[:10]+"'"
 if kind=='uniqueidentifier':return "'"+str(value)+"'"
 if kind=='int':return str(value)
 return format(m(value),'.2f')
declarations=['Id uniqueidentifier PRIMARY KEY']+[f'{k} {v} NOT NULL' for k,v in guards.items()]+[f'New{k} decimal(18,2) NOT NULL' for k in new_fields]
values=[]
for s in rows:
 values.append('('+','.join([literal(s['Id'],'uniqueidentifier')]+[literal(s[k],t) for k,t in guards.items()]+[literal(s[k],'decimal(18,2)') for k in new_fields])+')')
conditions=['s.Source=\'Orders\'']+[f's.{k[3:] if k.startswith("Old") else k}=p.{k}' for k in guards]
script="""-- Prepared for review. No production financial updates have been executed.
-- Default scope: the two supplied imports, 231 settlements. Third import is opt-in.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @Apply bit=0, @IncludeThirdImport bit=0, @ActorId uniqueidentifier=NULL;
IF DB_NAME()<>'db67927' THROW 51000,'Unexpected database target.',1;
DECLARE @Plan TABLE (
"""+',\n'.join(declarations)+"\n);\nINSERT @Plan VALUES\n"+',\n'.join(values)+";\n"
script+="""IF @IncludeThirdImport=0 DELETE @Plan WHERE ImportId='01a14a74-223a-767e-95d4-fa5657214e83';
IF @Apply=0
BEGIN
 SELECT ImportId,COUNT(*) SettlementCount,SUM(OldVatAmount) OldVat,SUM(NewVatAmount) NewVat,
  SUM(OldGrossEarnings) OldNet,SUM(NewGrossEarnings) NewNet,
  SUM(OldCompanyAmount) OldCompany,SUM(NewCompanyAmount) NewCompany,
  SUM(OldRiderAmount) OldRider,SUM(NewRiderAmount) NewRider
 FROM @Plan GROUP BY ImportId;
 SELECT COUNT(*) MatchingUnchangedSnapshots FROM @Plan p JOIN chefz.WalletSettlements s ON s.Id=p.Id WHERE """+' AND '.join(conditions)+""";
 RETURN;
END;
IF @ActorId IS NULL OR NOT EXISTS(SELECT 1 FROM [identity].Users WHERE Id=@ActorId AND IsDeleted=0)
 THROW 51000,'Set an authorized audit actor before applying.',1;
DECLARE @Expected int=(SELECT COUNT(*) FROM @Plan),@Matched int,@Now datetimeoffset=SYSUTCDATETIME(),
 @Correlation nvarchar(100)=CONCAT('chefz-vat-inclusive-',CONVERT(varchar(36),NEWID()));
BEGIN TRY
 BEGIN TRANSACTION;
 SELECT @Matched=COUNT(*) FROM @Plan p JOIN chefz.WalletSettlements s WITH(UPDLOCK,HOLDLOCK) ON s.Id=p.Id
 WHERE """+' AND '.join(conditions)+""";
 IF @Matched<>@Expected THROW 51000,'A saved financial snapshot changed. Rebuild the correction plan.',1;
 UPDATE s SET VatAmount=p.NewVatAmount,GrossEarnings=p.NewGrossEarnings,
  CompanyAmount=p.NewCompanyAmount,RiderAmount=p.NewRiderAmount
 FROM chefz.WalletSettlements s JOIN @Plan p ON p.Id=s.Id;
 IF @@ROWCOUNT<>@Expected THROW 51000,'Unexpected correction count.',1;
 INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,
  OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),NEWID(),@ActorId,'User','ChefzVatInclusiveCorrection','chefz','ChefzWalletSettlement',p.Id,
  @Now,@Correlation,N'User confirmed delivery fees include VAT: divide by 1.15. Preserve original charged-day snapshots.',
  (SELECT p.OldVatAmount VatAmount,p.OldGrossEarnings GrossEarnings,p.OldCompanyAmount CompanyAmount,p.OldRiderAmount RiderAmount FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
  (SELECT p.NewVatAmount VatAmount,p.NewGrossEarnings GrossEarnings,p.NewCompanyAmount CompanyAmount,p.NewRiderAmount RiderAmount FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
  'AuthorizedFinancialCorrection',1,@Now,@ActorId FROM @Plan p;
 UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@ActorId WHERE ModuleKey='chefz';
 IF EXISTS(SELECT 1 FROM @Plan p JOIN chefz.WalletSettlements s ON s.Id=p.Id
  WHERE s.VatAmount<>p.NewVatAmount OR s.GrossEarnings<>p.NewGrossEarnings
   OR s.CompanyAmount<>p.NewCompanyAmount OR s.RiderAmount<>p.NewRiderAmount
   OR s.GrossEarnings<>s.DeliveryFees-s.VatAmount+s.Tips+s.Compensation+s.Bonuses-s.Deductions
   OR s.RiderAmount<>s.GrossEarnings-s.CompanyAmount)
  THROW 51000,'Financial verification failed.',1;
 COMMIT TRANSACTION;
 SELECT @Correlation CorrelationId,@Expected CorrectedSettlements;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
"""
(out/'review-and-apply-vat-correction.sql').write_text(script,encoding='utf-8')
