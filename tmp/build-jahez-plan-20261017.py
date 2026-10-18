import json,pathlib,uuid,collections,csv
base=pathlib.Path('tmp/jahez-db-20261017')
rd=lambda n:json.loads((base/(n+'.json')).read_text(encoding='utf-8-sig'))
a=json.loads(pathlib.Path('tmp/jahez-analysis-20261017.json').read_text(encoding='utf-8-sig'))
uid=lambda:str(uuid.uuid4()).upper()
profiles=[]
for x in a['profilesNeeded']:profiles.append(dict(x,id=uid()))
profileByEmployee={r['EmployeeId']:r['Id'] for r in rd('riders') if not r['IsDeleted']}
profileByEmployee.update({r['employeeId']:r['id'] for r in profiles})
oldAccounts={r['Id']:r for r in rd('accounts')};employees={r['Id']:r for r in rd('employees')}
active=[x for x in rd('assignments') if not x['IsDeleted'] and not x.get('EffectiveTo')]
byAccount={x['PlatformRiderAccountId']:x for x in active}
sessions={s['RiderClientAssignmentId']:s for s in rd('sessions') if not s['IsDeleted']}
overIqamas={x['iqama'] for x in a['overCapacity']}
inserts=[];vehicleLinks=[];accounts=[];pending=[]
for r in a['rows']:
 r['newAccount']=not r.get('accountId')
 if r['newAccount']:
  r['accountId']=uid();r['cityId']=employees[r['ownerId']].get('OperatingCityId') or r.get('vehicleCityId');r['paymentModel']=1
 else:r['cityId']=oldAccounts[r['accountId']]['OperatingCityId']
 assert r.get('ownerId') and r.get('sponsorId') and r.get('cityId')
 r['issues']=[x for x in r['issues'] if x not in ['AccountMissingOrAmbiguous','InactiveWithRider']]
 reasons=[]
 if r['riderIqama']:
  reasons=[x for x in r['issues'] if x not in ['VehicleMissingOrAmbiguous']]
  if r['status']!='Active' and r['riderIqama']!='#N/A':reasons.append('InactiveAccountHasListedRider')
  if r['riderIqama'] in overIqamas:reasons.append('RiderAccountCapacityConflict')
  existing=byAccount.get(r['accountId'])
  if existing:reasons.append('ExistingAssignmentHasDifferentRiderAndFinancialRecords')
  if not reasons:
   rid=profileByEmployee[r['riderEmployeeId']]
   occupied={x['RiderAccountSlot'] for x in active if x['RiderProfileId']==rid}|{x['slot'] for x in inserts if x['riderId']==rid}
   slot=next(s for s in [1,2] if s not in occupied)
   inserts.append(dict(id=uid(),sessionId=uid(),accountId=r['accountId'],externalAccountId=r['account'],riderId=rid,employeeId=r['riderEmployeeId'],riderIqama=r['riderIqama'],fromDate=r['date'],slot=slot))
  else:pending.append(dict(row=r['row'],account=r['account'],riderIqama=r['riderIqama'],date=r['date'],reasons=reasons))
 r['assignmentResult']='Created' if any(x['accountId']==r['accountId'] for x in inserts) else ('Pending' if reasons else 'NoRiderListed')
 r['pendingReasons']=reasons
 r['desiredStatus']=3 if r['status']=='Inactive' else (2 if r['assignmentResult']=='Created' or r['accountId'] in byAccount else 1)
 if r.get('vehicleId'):vehicleLinks.append(dict(id=uid(),accountId=r['accountId'],vehicleId=r['vehicleId'],serial=r['serial']))
 accounts.append(r)
plan=dict(sourcePath=a['source']['sourcePath'],sourceSha256=a['source']['sourceSha256'],sourceSheet=a['source']['sheet'],correlationId='jahez-account-sheet-20261017-'+a['source']['sourceSha256'][:12],platformId=a['platform']['Id'],contractId=next(x['Id'] for x in a['contracts'] if x['ClientPlatformId']==a['platform']['Id'] and x['Status']==2),actorId='019C18D5-62E1-7000-C000-000000000001',dateCorrections=a['fixes'],accounts=accounts,profiles=profiles,insertAssignments=inserts,vehicleLinks=vehicleLinks,pendingAssignments=pending,assignmentChecks=[{'id':r['Id'],'rowVersion':r['RowVersion']} for r in rd('assignments')],sessionChecks=[{'id':r['Id'],'rowVersion':r['RowVersion']} for r in rd('sessions')],ownerSponsorMismatches=a['ownerSponsorMismatches'],duplicateRows=a['duplicateRows'])
plan['summary']=dict(sourceRows=len(a['source']['rows']),uniqueAccounts=len(accounts),duplicatesIgnored=len(a['duplicateRows']),existingAccountsUpdated=sum(not r['newAccount'] for r in accounts),newAccounts=sum(r['newAccount'] for r in accounts),ownerProfilesCreated=len(profiles),accountSponsorChanges=sum(oldAccounts[r['accountId']].get('SponsorId')!=r['sponsorId'] for r in accounts if not r['newAccount']),suspended=sum(r['desiredStatus']==3 for r in accounts),available=sum(r['desiredStatus']==1 for r in accounts),assigned=sum(r['desiredStatus']==2 for r in accounts),newRiderAssignments=len(inserts),existingRiderAssignmentsPreserved=3,pendingRiderAssignments=len(pending),vehicleLinksCreated=len(vehicleLinks),missingVehicleSerials=sum(not r['serial'] for r in accounts),unmatchedVehicleSerials=sorted({r['serial'] for r in accounts if r['serial'] and not r.get('vehicleId')}),ownerHrSponsorDifferences=len(a['ownerSponsorMismatches']))
plan['userConfirmedSuspendedAccounts']=['456454','458394','457357']
pathlib.Path('tmp/jahez-plan-20261017.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(plan['summary'],indent=2))
print(json.dumps(pending,ensure_ascii=False,indent=2))
