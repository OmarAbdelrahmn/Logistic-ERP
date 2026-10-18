import json, collections, datetime, uuid, argparse, hashlib
from pathlib import Path
arg=argparse.ArgumentParser();arg.add_argument('--replace-confirmed',action='store_true');args=arg.parse_args()
source=json.loads(Path('artifacts/chefz-import-20261015/source.json').read_text(encoding='utf-8'))
db=json.loads(Path('tmp/chefz-db-20261015.json').read_text(encoding='utf-8-sig'))
uid=lambda:str(uuid.uuid4())
day=lambda x:x[:10] if x else None
def date(v):
 return datetime.datetime.strptime(v,'%d/%m/%Y').date().isoformat()
accounts={a['ExternalAccountId']:a for a in db['accounts']}
employees=collections.defaultdict(list)
for e in db['employees']:
 if not e['IsDeleted'] and e['RiderId'] and not e['RiderDeleted']:employees[e['IqamaNo']].append(e)
active={a['PlatformRiderAccountId']:a for a in db['assignments'] if not a['EffectiveTo']}
plan={'sourcePath':source['path'],'sourceSha256':source['sha256'],'correlationId':'chefz-account-sheet-20261015-'+source['sha256'][:12],
 'platformId':db['platforms'][0]['Id'],'contractId':db['contracts'][0]['Id'],'actorId':db['actors'][0]['Id'],
 'accounts':[],'insertAssignments':[],'closeAssignments':[],'reopenAssignments':[],'assignmentChecks':[], 'exceptions':[], 'passwordRows':[]}
assignmentchecks={a['Id']:{'id':a['Id'],'rowVersion':a['RowVersion']} for a in db['assignments']}
plan['assignmentChecks']=list(assignmentchecks.values())
reserved=collections.defaultdict(set)
for a in active.values():reserved[a['RiderProfileId']].add(a['RiderAccountSlot'])
blocked={x['RiderProfileId'] for x in db['blockedRiders']}
def exception(row,account,rider,reason):
 plan['exceptions'].append({'sheet':'داش شيفز','row':row,'account':account,'rider':rider,'reason':reason})
for raw in source['sheets'][0]['rows'][1:]:
 c=raw['cells'];key=str(c.get('C',''));r=raw['row'];a=accounts.get(key)
 if str(c.get('D','')).strip() not in ['FREE','FT']:
  exception(r,key,None,'Incomplete supplementary row: no account settings or assignment supplied');continue
 if not a:
  exception(r,key,None,'Account not found; ignored');continue
 row={'row':r,'id':a['Id'],'externalAccountId':key,'rowVersion':a['RowVersion'],'registeredEmployeeId':a['RegisteredEmployeeId'],
 'cityId':next(x['Id'] for x in db['cities'] if x['NameAr']==c['H'].strip()),
 'sponsorId':next(x['Id'] for x in db['sponsors'] if x['UnifiedNationalNumber']==str(c['F'])),
 'accountType':1 if c['D'].strip()=='FREE' else 2,'vehicleType':'Car' if c['G'].strip()=='سياره' else 'Motorcycle',
 'detailsId':a['DetailsId'],'oldStatus':a['Status'],'desiredStatus':a['Status'],'assignmentAction':'None',
 'userIqama':str(c['K']) if c.get('K') else None,'effectiveFrom':date(c['M']) if c.get('M') else None}
 password=str(c.get('I','')).strip()
 if password and password!='غير محفوظة':plan['passwordRows'].append({'row':r,'accountId':a['Id'],'externalAccountId':key})
 cur=active.get(a['Id'])
 if c.get('K'):
  matches=employees[row['userIqama']]
  if len(matches)!=1:exception(r,key,row['userIqama'],'Rider iqama not found; assignment pending')
  else:
   e=matches[0];row['riderId']=e['RiderId'];row['employeeId']=e['Id']
   if e['Status']!=3 or e['IsEmployee'] or e['RiderId'] in blocked:
    exception(r,key,row['userIqama'],'Rider is not eligible for active assignment')
   elif cur and cur['RiderProfileId']==e['RiderId']:
    assert day(cur['EffectiveFrom'])==row['effectiveFrom']
    row['assignmentAction']='Keep';row['assignmentId']=cur['Id'];row['desiredStatus']=2
   elif cur and not args.replace_confirmed:
    exception(r,key,row['userIqama'],'Replacement awaiting date/cancellation confirmation')
   else:
    if cur:
     cancel=key in ['2076420757','2618863134']
     end=day(cur['EffectiveFrom']) if cancel else (datetime.date.fromisoformat(row['effectiveFrom'])-datetime.timedelta(days=1)).isoformat()
     assert cancel or end>=day(cur['EffectiveFrom'])
     plan['closeAssignments'].append({'id':cur['Id'],'accountId':a['Id'],'riderId':cur['RiderProfileId'],'endDate':end,'status':4 if cancel else 3,'reason':'Cancelled mistaken rider assignment; workbook correction authorized by user.' if cancel else 'Workbook rider transfer; previous rider ends the day before the new handover.'})
     reserved[cur['RiderProfileId']].remove(cur['RiderAccountSlot'])
    other=[x for x in active.values() if x['RiderProfileId']==e['RiderId'] and x['Id']!=((cur or {}).get('Id'))]
    assert all(x['PlatformCode']=='SHIFTZ' for x in other),'Mixed platform assignment'
    assert len(reserved[e['RiderId']])<2,'Third rider account'
    assert a['PaymentModel']!=2 or all(x['PaymentModel']!=2 for x in other),'Second salary account'
    slot=next(x for x in [1,2] if x not in reserved[e['RiderId']]);reserved[e['RiderId']].add(slot)
    previous=[x for x in db['assignments'] if x['PlatformRiderAccountId']==a['Id'] and x['RiderProfileId']==e['RiderId'] and day(x['EffectiveFrom'])==row['effectiveFrom'] and x['Status']==3]
    assert len(previous)<=1
    row['assignmentAction']='Reopen' if previous else 'Create';row['assignmentId']=previous[0]['Id'] if previous else uid();row['desiredStatus']=2
    target=plan['reopenAssignments'] if previous else plan['insertAssignments']
    target.append({'id':row['assignmentId'],'accountId':a['Id'],'riderId':e['RiderId'],'employeeId':e['Id'],
      'userIqama':row['userIqama'],'fromDate':row['effectiveFrom'],'toDate':None,'status':2,'slot':slot,'paymentModel':a['PaymentModel'],'sourceRow':r,'sheet':'داش شيفز','reason':'Workbook handover matched by actual rider iqama.'})
 elif not cur:row['desiredStatus']={'متاح (Available)':1,'موقوف (Suspended)':3}[c['J'].strip()]
 else:exception(r,key,None,'Workbook omits rider but account has an open assignment')
 row['dashboardStatus']='active' if row['desiredStatus'] in [1,2] else (a['DashboardStatus'] or 'The supervisor suspended')
 plan['accounts'].append(row)
for raw in source['sheets'][1]['rows'][1:]:
 c=raw['cells'];a=accounts[str(c['A'])];e=employees[str(c['M'])];assert len(e)==1;e=e[0]
 # User confirmed mixed return-date formats are all October, and the current rider owns 11 October for 2620007464.
 end='2026-10-'+str(int(c['T'].split('/')[1])).zfill(2)
 if str(c['A'])=='2620007464':end='2026-10-10'
 start=date(c['S'])
 assert not any(x['PlatformRiderAccountId']==a['Id'] and x['Status'] in [2,3] and day(x['EffectiveFrom'])<=end and (not x['EffectiveTo'] or day(x['EffectiveTo'])>=start) for x in db['assignments'])
 plan['insertAssignments'].append({'id':uid(),'accountId':a['Id'],'riderId':e['RiderId'],'employeeId':e['Id'],'userIqama':str(c['M']),
 'fromDate':start,'toDate':end,'status':3,'slot':1,'paymentModel':a['PaymentModel'],'sourceRow':raw['row'],'sheet':'بدلاء الدوره','reason':'Completed historical assignment supplied by user; October dates confirmed.','originalReturnDate':c['T']})
plan['summary']={'accountsChecked':len(plan['accounts']),'metadataChanges':sum(any([a['cityId']!=accounts[a['externalAccountId']]['OperatingCityId'],a['sponsorId']!=accounts[a['externalAccountId']]['SponsorId'],a['accountType']!=accounts[a['externalAccountId']]['AccountType'],a['vehicleType']!=accounts[a['externalAccountId']]['VehicleType']]) for a in plan['accounts']),
 'statusChanges':sum(a['desiredStatus']!=a['oldStatus'] for a in plan['accounts']), 'existingAssignmentsKept':sum(a['assignmentAction']=='Keep' for a in plan['accounts']),
 'activeAssignmentsCreated':sum(a['status']==2 for a in plan['insertAssignments']),'assignmentsReopened':len(plan['reopenAssignments']),'completedHistoriesCreated':sum(a['status']==3 for a in plan['insertAssignments']),
 'mistakenAssignmentsCancelled':sum(a['status']==4 for a in plan['closeAssignments']),'previousAssignmentsEnded':sum(a['status']==3 for a in plan['closeAssignments']),
 'passwordsReady':len(plan['passwordRows']),'passwordsNotSupplied':len(plan['accounts'])-len(plan['passwordRows']),
 'employeeRiderRows':sum(x['reason']=='Rider is not eligible for active assignment' for x in plan['exceptions']),
 'missingRiderRows':sum(x['reason'].startswith('Rider iqama not found') for x in plan['exceptions']), 'incompleteSupplementaryRows':sum(x['reason'].startswith('Incomplete') for x in plan['exceptions'])}
Path('tmp/chefz-plan-20261015.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(plan['summary'],indent=2))
