import json, pathlib, collections, datetime
root=pathlib.Path('tmp/jahez-db-20261017')
def read(n):return json.loads((root/(n+'.json')).read_text(encoding='utf-8-sig'))
src=json.loads(pathlib.Path('tmp/jahez-source-20261017.json').read_text(encoding='utf-8'))
fix={'460555':'2026-10-17','404899':'2026-09-12','472431':'2026-10-15','492241':'2026-10-11',
     '472844':'2026-10-13','474642':'2026-10-13','475162':'2026-10-17','470544':'2026-10-04','460711':'2026-10-16'}
def group(items,key):
 d=collections.defaultdict(list)
 for x in items:d[x.get(key)].append(x)
 return d
platform=next(x for x in read('platforms') if x['Code']=='JAHEZ')
accounts=[x for x in read('accounts') if x['ClientPlatformId']==platform['Id'] and not x['IsDeleted']]
ag=group(accounts,'ExternalAccountId'); employees=read('employees');eg=group([x for x in employees if not x['IsDeleted']],'IqamaNo'); eb={x['Id']:x for x in employees}
riders=read('riders');rg=group([x for x in riders if not x['IsDeleted']],'EmployeeId')
sg=group([x for x in read('sponsors') if not x['IsDeleted']],'EmployerIdentityNumber')
vg=group([x for x in read('vehicles') if not x['IsDeleted']],'SerialNumber')
rows=[];dupes=[];seen={};issues=[];missingprofiles={};sponsorMismatch=[]
for r0 in src['rows']:
 r=r0.copy()
 if r['account'] in seen:
  assert {k:v for k,v in r.items() if k!='row'}=={k:v for k,v in seen[r['account']].items() if k!='row'},'Conflicting duplicate'
  dupes.append(r['row']);continue
 seen[r['account']]=r0
 r['originalDate']=r['date'];r['date']=fix.get(r['account'],r['date']); r['date']=r['date'][:10] if r['date'] else None
 ai=ag[r['account']]; ow=eg[r['ownerIqama']]; sp=sg[r['sponsorNumber']]; ve=vg[r['serial']] if r['serial'] else []
 r['issues']=[]
 if len(ai)!=1:r['issues'].append('AccountMissingOrAmbiguous')
 if len(ow)!=1:r['issues'].append('OwnerMissingOrAmbiguous')
 if len(sp)!=1:r['issues'].append('SponsorMissingOrAmbiguous')
 if r['serial'] and len(ve)!=1:r['issues'].append('VehicleMissingOrAmbiguous')
 if len(ai)==1:r['accountId']=ai[0]['Id'];r['accountRowVersion']=ai[0]['RowVersion'];r['paymentModel']=ai[0]['PaymentModel'];r['currentStatus']=ai[0]['Status']
 if len(ow)==1:
  o=ow[0];r['ownerId']=o['Id'];r['ownerIsEmployee']=o['IsEmployee'];r['ownerStatus']=o['Status']
  if len(ai)==1 and ai[0].get('RegisteredEmployeeId')!=o['Id']:r['issues'].append('OwnerLinkMismatch')
  if not rg[o['Id']]:missingprofiles[o['Id']]={'employeeId':o['Id'],'iqama':o['IqamaNo'],'isEmployee':o['IsEmployee'],'status':o['Status'],'reason':'RegisteredOwner'}
  if len(sp)==1 and o.get('SponsorId')!=sp[0]['Id']:sponsorMismatch.append({'row':r['row'],'account':r['account'],'ownerIqama':r['ownerIqama'],'ownerSponsorId':o.get('SponsorId'),'dashboardSponsorId':sp[0]['Id']})
 if len(sp)==1:r['sponsorId']=sp[0]['Id']
 if len(ve)==1:r['vehicleId']=ve[0]['Id'];r['vehicleType']=ve[0]['VehicleType'];r['vehicleSponsorId']=ve[0].get('SponsorId');r['vehicleCityId']=ve[0].get('OperatingCityId')
 if r['riderIqama']:
  ri=eg[r['riderIqama']]
  if len(ri)!=1:r['issues'].append('RiderMissingOrAmbiguous')
  else:
   e=ri[0];r['riderEmployeeId']=e['Id'];r['riderStatus']=e['Status'];r['riderIsEmployee']=e['IsEmployee']
   if e['Status']!=3:r['issues'].append('RiderNotActive')
   if not rg[e['Id']]:missingprofiles[e['Id']]={'employeeId':e['Id'],'iqama':e['IqamaNo'],'isEmployee':e['IsEmployee'],'status':e['Status'],'reason':'ActualRider'}
   else:r['riderId']=rg[e['Id']][0]['Id']
  if not r['date']:r['issues'].append('MissingDate')
  elif r['date']>'2026-10-17':r['issues'].append('FutureDate')
  if r['status']!='Active':r['issues'].append('InactiveWithRider')
 rows.append(r)
assignments=[x for x in read('assignments') if not x['IsDeleted'] and x.get('EffectiveTo') is None]
activeByRider=group(assignments,'RiderProfileId')
sourceRiders=group([r for r in rows if r.get('riderEmployeeId')],'riderEmployeeId')
over=[]
for e,rr in sourceRiders.items():
 old=activeByRider.get(rr[0].get('riderId'),[])
 if len(rr)+len(old)>2:over.append({'iqama':rr[0]['riderIqama'],'sourceAccounts':[r['account'] for r in rr],'otherAssignments':[{'account':x['PlatformRiderAccountId'],'platform':next(a for a in read('accounts') if a['Id']==x['PlatformRiderAccountId'])['ClientPlatformId'],'slot':x['RiderAccountSlot']} for x in old]})
for r in rows:
 if r['issues']:issues.append({k:r.get(k) for k in ['row','account','ownerIqama','riderIqama','date','serial','riderIsEmployee','riderStatus','issues']})
out=dict(source=src,fixes=fix,platform=platform,contracts=read('contracts'),rows=rows,profilesNeeded=list(missingprofiles.values()),duplicateRows=dupes,ownerSponsorMismatches=sponsorMismatch,overCapacity=over)
pathlib.Path('tmp/jahez-analysis-20261017.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(accounts=len(rows),statuses=collections.Counter(r['status'] for r in rows),ownerLinkMismatches=sum('OwnerLinkMismatch' in r['issues'] for r in rows),existingJahezAssignments=sum(x['PlatformRiderAccountId'] in {a['Id'] for a in accounts} for x in assignments),existingVehicleLinks=sum(x['PlatformRiderAccountId'] in {a['Id'] for a in accounts} and not x['IsDeleted'] and x.get('EndedAtUtc') is None for x in read('vehicleAssignments')),ownerSponsorMismatchCount=len(sponsorMismatch),profilesNeeded=len(missingprofiles),profileStatuses=collections.Counter(str((x['isEmployee'],x['status'])) for x in missingprofiles.values()),overCapacity=over,issues=issues,contracts=read('contracts'),sponsors=read('sponsors')),ensure_ascii=False,indent=2,default=str))

