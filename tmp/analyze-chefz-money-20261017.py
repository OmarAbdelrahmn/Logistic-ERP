import json, hashlib, csv, collections, datetime
from pathlib import Path
from decimal import Decimal as D, ROUND_HALF_UP
import openpyxl

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'outputs/chefz-money-audit-20261017'
db=json.loads((OUT/'database-snapshot.json').read_text(encoding='utf-8-sig'),parse_float=D)
money=lambda v:D(str(v or 0))
roundmoney=lambda v:v.quantize(D('.01'),rounding=ROUND_HALF_UP)
date=lambda s:datetime.date.fromisoformat(str(s)[:10])
fields={'DeliveryFees':'Driver Delivery Fees','Tips':'Driver Tip','Compensation':'Driver Compensate','Bonuses':'Bonus Amount','Deductions':'Deduction Amount'}
accounts=collections.defaultdict(list)
for a in db['accounts']:
 if not a['IsDeleted'] and not a['AccountDeleted']:accounts[a['ReportIdNumber']].append(a)
assignments=collections.defaultdict(list)
for a in db['assignments']:
 if not a['IsDeleted'] and a['Status'] in (2,3):assignments[a['PlatformRiderAccountId']].append(a)
files=[]; all_order_ids=collections.defaultdict(list); details=[]
for basename in ['2151034 Albawaba Almokbela (8).xlsx','2151351 Albawaabah Farei (8).xlsx']:
 path=Path(r'C:\Users\omarf\Downloads')/basename
 hash=hashlib.sha256(path.read_bytes()).hexdigest().upper()
 w=openpyxl.load_workbook(path,data_only=False,read_only=True);s=w['Sheet1'];rows=s.iter_rows(values_only=True)
 headers=[' '.join(str(h).split()) for h in next(rows)]
 source=[];issues=[];seen=set();expected={}
 for n,raw in enumerate(rows,2):
  if all(v is None for v in raw):continue
  r=dict(zip(headers,raw));driver=str(r['Driver ID']);dt=r['Date'].date() if isinstance(r['Date'],datetime.datetime) else date(r['Date'])
  oid=str(r['Chef Order ID'] or '')
  if oid and (driver,oid) in seen:issues.append({'row':n,'issue':'duplicate order','driver':driver})
  seen.add((driver,oid));all_order_ids[(driver,oid)].append(basename)
  amounts={k:money(r.get(h)) for k,h in fields.items()};amounts['Deductions']=abs(amounts['Deductions'])
  source.append({'driver':driver,'date':dt,'order':oid,**amounts,'commission':money(r.get('Driver Company Commission Fees')),'cash':money(r.get('Cash Collected By Driver'))})
  if any(isinstance(v,str) and v.startswith('=') for v in raw):issues.append({'row':n,'issue':'formula'})
  account=accounts[driver]
  if len(account)!=1:issues.append({'row':n,'issue':'account match','driver':driver});continue
  account=account[0]
  candidates=[a for a in assignments[account['PlatformRiderAccountId']] if date(a['EffectiveFrom'])<=dt and (not a['EffectiveTo'] or date(a['EffectiveTo'])>=dt)]
  if len(candidates)!=1:issues.append({'row':n,'issue':'assignment match','driver':driver});continue
  a=candidates[0];period=dt.replace(day=1 if dt.day<=15 else 16);key=(a['Id'],period.isoformat())
  if key not in expected:
   period_end=dt.replace(day=15) if dt.day<=15 else (dt.replace(day=28)+datetime.timedelta(days=4)).replace(day=1)-datetime.timedelta(days=1)
   start=max(period,date(a['EffectiveFrom']));end=min(period_end,date(a['EffectiveTo'])) if a['EffectiveTo'] else period_end
   expected[key]={'file':basename,'AssignmentId':a['Id'],'RiderProfileId':a['RiderProfileId'],'RiderName':a['RiderName'],'DriverId':driver,'AccountType':account['AccountType'],'PeriodFrom':period.isoformat(),'AssignedDays':(end-start).days+1,'SourceRows':0,**{k:D(0) for k in fields}}
  e=expected[key];e['SourceRows']+=1
  for k in fields:e[k]+=amounts[k]
 # Independent decimal calculation, grouping once per payable rider and cycle.
 for e in expected.values():
  e['VatAmount']=e['DeliveryFees']-roundmoney(e['DeliveryFees']*D('.85'))
  e['GrossEarnings']=e['DeliveryFees']-e['VatAmount']+e['Tips']+e['Compensation']+e['Bonuses']-e['Deductions']
  e['CompanyAmount']=roundmoney(e['GrossEarnings']*D('.15')) if e['AccountType']==1 else D(e['AssignedDays'])*25
 cycles=collections.defaultdict(list)
 for e in expected.values():cycles[(e['RiderProfileId'],e['PeriodFrom'])].append(e)
 for cycle in cycles.values():
  fees=sum(e['DeliveryFees'] for e in cycle);vat=fees-roundmoney(fees*D('.85'))
  largest=max(cycle,key=lambda e:abs(e['DeliveryFees']));largest['VatAmount']+=vat-sum(e['VatAmount'] for e in cycle)
  for e in cycle:
   e['GrossEarnings']=e['DeliveryFees']-e['VatAmount']+e['Tips']+e['Compensation']+e['Bonuses']-e['Deductions']
   if e['AccountType']==1:e['CompanyAmount']=roundmoney(e['GrossEarnings']*D('.15'))
  freelancers=[e for e in cycle if e['AccountType']==1]
  if freelancers:max(freelancers,key=lambda e:abs(e['GrossEarnings']))['CompanyAmount']+=roundmoney(sum(e['GrossEarnings'] for e in freelancers)*D('.15'))-sum(e['CompanyAmount'] for e in freelancers)
  for e in cycle:e['RiderAmount']=e['GrossEarnings']-e['CompanyAmount']
 batches=[i for i in db['imports'] if i['ContentHash']==hash]
 stored=[s for s in db['settlements'] if s['ImportId'] in {i['Id'] for i in batches}]
 comparisons=[];matched=set()
 for s in stored:
  key=(s['RiderClientAssignmentId'],str(s['PeriodFrom'])[:10]);e=expected.get(key)
  if e is None:comparisons.append({'unexpectedSettlement':s['Id']});continue
  matched.add(key);diff={}
  for k in list(fields)+['VatAmount','GrossEarnings','CompanyAmount','RiderAmount','SourceRows','AssignedDays','AccountType']:
   if money(s[k])!=money(e[k]):diff[k]={'expected':e[k],'stored':s[k]}
  if s['RiderProfileId']!=e['RiderProfileId']:diff['rider']='different'
  if diff:comparisons.append({'driver':s['DriverId'],'differences':diff})
  details.append({**e,'StoredSettlementId':s['Id'],'StoredVat':s['VatAmount'],'StoredNet':s['GrossEarnings'],'StoredCompany':s['CompanyAmount'],'StoredRider':s['RiderAmount'],'Matches':not diff})
 source_totals={k:sum(r[k] for r in source) for k in fields}
 totals={k:sum(money(r[k]) for r in stored) for k in list(fields)+['VatAmount','GrossEarnings','CompanyAmount','RiderAmount','SourceRows']}
 types={str(t):{'accounts':len({e['DriverId'] for e in expected.values() if e['AccountType']==t}),'settlements':sum(e['AccountType']==t for e in expected.values()),'assignedDays':sum(e['AssignedDays'] for e in expected.values() if e['AccountType']==t),**{k:sum(money(r[k]) for r in stored if r['AccountType']==t) for k in ['DeliveryFees','VatAmount','GrossEarnings','CompanyAmount','RiderAmount']}} for t in (1,2)}
 inclusive_net=sum(roundmoney(sum(e['DeliveryFees'] for e in cy)/D('1.15')) for cy in cycles.values())
 result={'file':basename,'sha256':hash,'exactHashMatches':len(batches),'importIds':[i['Id'] for i in batches],'rows':len(source),'drivers':len({r['driver'] for r in source}),'dates':[str(min(r['date'] for r in source)),str(max(r['date'] for r in source))],'sourceTotals':source_totals,'commissionColumnSum':sum(r['commission'] for r in source),'cashColumnSum':sum(r['cash'] for r in source),'storedTotals':totals,'expectedSettlementCount':len(expected),'storedSettlementCount':len(stored),'distinctRiders':len(cycles),'sourceIssues':issues,'mismatches':comparisons,'missingSettlements':[expected[k] for k in expected.keys()-matched],'accountTypes':types,'vatIfInclusive':source_totals['DeliveryFees']-inclusive_net,'netIncreaseIfVatInclusive':inclusive_net-(totals['DeliveryFees']-totals['VatAmount']),'negativeRiders':[{'driver':e['DriverId'],'name':e['RiderName'],'net':e['GrossEarnings'],'company':e['CompanyAmount'],'rider':e['RiderAmount'],'days':e['AssignedDays']} for e in expected.values() if e['RiderAmount']<0]}
 files.append(result)
 print(json.dumps(result,ensure_ascii=True,default=str))
combined={'files':files,'crossFileDuplicateOrders':sum(len(v)>1 for k,v in all_order_ids.items() if k[1]),'invalidArithmetic':db['invalidArithmetic'],'duplicateSettlements':db['duplicateSettlements'],'readAt':db['target']}
(OUT/'reconciliation.json').write_text(json.dumps(combined,ensure_ascii=False,default=str,indent=2),encoding='utf-8')
if details:
 with (OUT/'settlement-reconciliation.csv').open('w',newline='',encoding='utf-8-sig') as f:
  writer=csv.DictWriter(f,fieldnames=list(details[0]));writer.writeheader();writer.writerows(details)
print('CROSS_FILE_DUPLICATES',combined['crossFileDuplicateOrders'],'DB_ARITHMETIC',db['invalidArithmetic'])
