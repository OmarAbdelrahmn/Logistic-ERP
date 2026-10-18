import json,uuid,collections,sys
from pathlib import Path
from datetime import date,timedelta
p=Path('tmp/chefz-assignment-plan-20261013.json')
plan=json.loads(p.read_text(encoding='utf-8'))
db=json.loads(Path('tmp/chefz-assignment-db-20261013.json').read_text(encoding='utf-8-sig'))
change_dates='--move-today' in sys.argv
remove_conflicts='--remove-conflicts' in sys.argv
ibans={};moves=[];removals=[]
for r in plan['rows']:
    r['assignmentAction']='Pending'
    issues=[x for x in r['issues'] if x!='Rider has another platform assignment']
    if not issues and r.get('otherAssignments'):
        other=r['otherAssignments'][0]
        h=next((x for x in db['handovers'] if x['RiderClientAssignmentId']==other['Id']),None)
        stop=(date.fromisoformat(r['effectiveFrom'])-timedelta(days=1)).isoformat()
        if h is None:issues.append('Jahez handover missing')
        elif h['DebtStatus']==4:issues.append('Jahez account frozen')
        elif stop < other['EffectiveFrom'][:10] or stop < (h['CommissionPostedThrough'] or '')[:10]:
            if remove_conflicts:
                removals.append(dict(row=r['row'],assignmentId=other['Id'],accountId=other['PlatformRiderAccountId'],handoverId=h['Id']))
                r['removeJahezAssignmentId']=other['Id']
            elif change_dates:r['effectiveFrom']='2026-10-13';stop='2026-10-12';r['dateAdjusted']=True
            else:issues.append('Sheet date conflicts with Jahez assignment or posted commission')
        if not issues and not r.get('removeJahezAssignmentId'):
            r['jahezAssignmentId']=other['Id'];r['jahezAccountId']=other['PlatformRiderAccountId'];r['jahezHandoverId']=h['Id'];r['jahezEndDate']=stop
            moves.append(dict(row=r['row'],assignmentId=other['Id'],accountId=other['PlatformRiderAccountId'],handoverId=h['Id'],endDate=stop))
    r['finalIssues']=issues
    if not issues:r['assignmentAction']='Assign';r['assignmentId']=str(uuid.uuid4());r['eventId']=str(uuid.uuid4())
    if r.get('employeeId') and r['ibanValid']:
        if r['employeeId'] in ibans and ibans[r['employeeId']]['iban']!=r['iban']:raise ValueError('Conflicting source IBAN')
        ibans[r['employeeId']]=dict(employeeId=r['employeeId'],iqama=r['userIqama'],iban=r['iban'],oldIban=r['oldIban'],action=r['ibanAction'])
plan['ibanUpdates']=[x for x in ibans.values() if x['action'] in ('Add','Update')]
plan['ibanChecks']=list(ibans.values());plan['moves']=moves
plan['removeJahez']=removals
p.write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print('Assignments',sum(r['assignmentAction']=='Assign' for r in plan['rows']),'pending',sum(r['assignmentAction']=='Pending' for r in plan['rows']),'Jahez closures',len(moves),'IBAN writes',len(plan['ibanUpdates']),'IBAN checks',len(plan['ibanChecks']))
print('Pending reasons',dict(collections.Counter(x for r in plan['rows'] for x in r['finalIssues'])))
