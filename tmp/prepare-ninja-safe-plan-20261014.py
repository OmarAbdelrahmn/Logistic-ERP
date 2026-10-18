import json, re, secrets, time, uuid, collections, hashlib
from pathlib import Path
import openpyxl

root=Path(__file__).resolve().parents[1]
source=json.loads((root/'tmp/ninja-source-20261013.json').read_text(encoding='utf8'))
db=json.loads((root/'tmp/ninja-db-20261013.json').read_text(encoding='utf-8-sig'))
assert hashlib.sha256(Path(source['sourcePath']).read_bytes()).hexdigest()==source['sourceSha256']
raw=openpyxl.load_workbook(source['sourcePath'],read_only=True,data_only=False)
assert not any(c.data_type=='f' for row in raw.active for c in row), 'Source formulas require review'
def new_id():
    return str(uuid.UUID(int=(int(time.time()*1000)<<80)|(7<<76)|(secrets.randbits(12)<<64)|(2<<62)|secrets.randbits(62)))
def unique_index(rows,key):
    result={}
    for row in rows:
        if row['IsDeleted']: continue
        assert row[key] not in result, f'Ambiguous {key}'
        result[row[key]]=row
    return result
employees=unique_index(db['employees'],'IqamaNo')
platforms=[p for p in db['platforms'] if not p['IsDeleted'] and p['Status']==1]
assert len(platforms)==1 and not db['accounts'] and not db['contracts'], 'Ninja state requires a fresh reconciliation'
actors=[a for a in db['actors'] if not a['IsDeleted']]
assert len(actors)==1
cities={c['NameEn']:c for c in db['cities'] if not c['IsDeleted'] and c['Status']==1}
sponsor_ids={'أكسبرس':'019c18d5-62e1-7000-8000-000000000042','البوابة':'019c18d5-62e1-7000-8000-000000000041','التجارية':'019c18d5-62e1-7000-8000-000000000040'}
sponsors={s['Id']:s for s in db['sponsors'] if not s['IsDeleted'] and s['Status']==1}
active=collections.defaultdict(list)
for a in db['assignments']:
    if a['EffectiveTo'] is None: active[a['RiderProfileId']].append(a)
owner_counts=collections.Counter((r['ownerIqama'],r['city'],r['sponsor']) for r in source['rows'] if r['sourceStatus']!='مشكلة بالحساب')
rows=[]
for r in source['rows']:
    r=dict(r)
    assert re.fullmatch(r'\d+',r['externalId']) and re.fullmatch(r'[12]\d{9}',r['ownerIqama'])
    assert r['city'] in cities and r['sponsor'] in sponsor_ids and r['vehicleType'] in ('سيارة','دباب')
    assert r['sourceStatus'] in ('يعمل','لايعمل','مشكلة بالحساب')
    owner=employees.get(r['ownerIqama']); reasons=[]
    if owner is None: reasons.append('صاحب الحساب غير موجود في النظام')
    else:
        if owner['IsEmployee']: reasons.append('صاحب الحساب مسجل كموظف مكتب؛ إنشاء الحساب يتطلب مالكًا مسجلًا كمندوب')
        if not owner['RiderId'] or owner['RiderDeleted']: reasons.append('صاحب الحساب ليس لديه ملف مندوب متاح')
    if owner_counts[r['ownerIqama'],r['city'],r['sponsor']]>1 and r['sourceStatus']=='لايعمل':
        reasons.append('تكرار المالك والمدينة والكفيل مع حساب يعمل؛ يحتاج إلى تحديد حالة الحساب أو تصحيح بيانات الملكية')
    r['accountAction']='Skip' if reasons else 'Add'
    r['accountIssues']=reasons
    r['assignmentAction']='NotWorking' if r['sourceStatus']!='يعمل' else 'Pending'
    r['assignmentIssues']=[]
    if r['accountAction']=='Skip':
        r['assignmentAction']='SkippedAccount'
    elif r['sourceStatus']=='يعمل':
        rider=employees.get(r['userIqama'])
        if not r['userIqama']: r['assignmentIssues'].append('خانة المستخدم تحتوي على وصف وليس رقم إقامة')
        elif rider is None: r['assignmentIssues'].append('رقم إقامة المستخدم غير موجود في النظام')
        elif rider['IsEmployee'] or rider['Status']!=3 or not rider['RiderId'] or rider['RiderDeleted']:
            r['assignmentIssues'].append('المستخدم غير مؤهل للتعيين أو ملف المندوب غير متاح')
        elif active[rider['RiderId']]:
            a=active[rider['RiderId']][0]
            r['assignmentIssues'].append(f"المستخدم لديه حساب نشط على {'شيفز' if a['PlatformCode']=='SHIFTZ' else a['PlatformCode']}: {a['ExternalAccountId']}")
        else:
            assert r['phone'] and re.fullmatch(r'05\d{8}',r['phone'])
            r.update(assignmentAction='Assign',userEmployeeId=rider['Id'],riderId=rider['RiderId'],assignmentId=new_id(),registrationId=new_id(),eventId=new_id())
    if r['accountAction']=='Add':
        r.update(accountId=new_id(),detailsId=new_id(),ownerEmployeeId=owner['Id'],ownerRiderId=owner['RiderId'],
                 operatingCityId=cities[r['city']]['Id'],sponsorId=sponsor_ids[r['sponsor']],code='NINJA-'+r['externalId'],paymentModel=1,
                 initialStatus=3 if r['sourceStatus']=='مشكلة بالحساب' else 1,
                 finalStatus=2 if r['assignmentAction']=='Assign' else (3 if r['sourceStatus']=='مشكلة بالحساب' else 1),
                 savedPhone=r['phone'] if r['phone'] and re.fullmatch(r'05\d{8}',r['phone']) else None,
                 ownershipNotes='إقامة صاحب الحساب حسب ملف نينجا: '+r['ownerIqama'],
                 notes=f"نوع مركبة الحساب: {r['vehicleType']}\nحالة الحساب في ملف نينجا: {r['sourceStatus']}\nقيمة مستخدم الحساب في الملف: {r['userRaw']}\nمصدر البيانات: بيانات نينجا بعد التحديث.xlsx، الصف {r['row']}",
                 statusReason='مشكلة بالحساب حسب ملف نينجا' if r['sourceStatus']=='مشكلة بالحساب' else ('التعيين معلق: '+'؛ '.join(r['assignmentIssues']) if r['assignmentAction']=='Pending' else 'لا يعمل حسب ملف نينجا' if r['sourceStatus']=='لايعمل' else None))
    rows.append(r)
summary=dict(sourceAccounts=len(rows),accountsToAdd=sum(r['accountAction']=='Add' for r in rows),accountsSkipped=sum(r['accountAction']=='Skip' for r in rows),
             assignmentsToCreate=sum(r['assignmentAction']=='Assign' for r in rows),assignmentsPending=sum(r['assignmentAction']=='Pending' for r in rows),
             statusCounts=dict(collections.Counter(r['finalStatus'] for r in rows if r['accountAction']=='Add')))
plan=dict(sourcePath=source['sourcePath'],sourceSha256=source['sourceSha256'],effectiveFrom='2026-10-14',platformId=platforms[0]['Id'],actorId=actors[0]['Id'],
          contractId=new_id(),contractCode=('SYS-'+platforms[0]['Id'].replace('-',''))[:32],correlationId='ninja-safe-import-20261014-'+source['sourceSha256'][:12],summary=summary,rows=rows)
assert summary['accountsToAdd']==301 and summary['assignmentsToCreate']==107 and summary['assignmentsPending']==25
(root/'tmp/ninja-safe-plan-20261014.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps(summary,ensure_ascii=False))
