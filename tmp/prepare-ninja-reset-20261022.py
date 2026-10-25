import json,uuid,collections,sys,hashlib,openpyxl
from pathlib import Path
root=Path(__file__).resolve().parents[1];out=root/'outputs/ninja-reset-20261022'
d=json.loads((out/'preflight.json').read_text(encoding='utf-8-sig'))
b=json.loads((root/'outputs/ninja-comparison-20261022/database-snapshot.json').read_text(encoding='utf-8-sig'))
source=Path(r'C:\Users\omarf\Downloads\رفع نينجا.xlsx')
correction=None
uid=lambda:str(uuid.uuid4())
employees={e['IqamaNo']:e for e in d['employees'] if not e['IsDeleted']}
accounts={a['ExternalAccountId']:a for a in b['accounts']}
citymap={c['NameEn'].casefold():c['Id'] for c in d['cities'] if not c['IsDeleted']}
cities=[]
for name,code,ar,regionar,regionen in [('TAIF','TAIF','الطائف','منطقة مكة المكرمة','Makkah Region'),('Madinah','MADINAH','المدينة المنورة','منطقة المدينة المنورة','Madinah Region')]:
    if name.casefold() not in citymap:
        c=dict(id=uid(),globalId=uid(),code=code,nameEn=name.title(),nameAr=ar,regionAr=regionar,regionEn=regionen)
        cities.append(c);citymap[name.casefold()]=c['id']
sponsors={s['EmployerIdentityNumber']:s['Id'] for s in d['sponsors'] if not s['IsDeleted']}
rows=[];newemployees={}
for s in openpyxl.load_workbook(source,read_only=True,data_only=True):
 for n,v in enumerate(s.values,1):
    if n==1 or not any(x is not None for x in v):continue
    captain=str(v[0]);iqama=str(v[4]);raw=iqama
    if captain=='66689':continue
    if captain=='66689' and correction:iqama=correction
    e=employees.get(iqama);a=accounts.get(captain)
    if not e and captain!='66689':raise ValueError('Missing owner '+captain)
    ownerid=e['Id'] if e else newemployees.setdefault(iqama,uid())
    rows.append(dict(id=a['Id'] if a else uid(),detailsId=uid(),captainId=captain,ownerId=ownerid,iqama=iqama,name=v[2],cityId=citymap[v[3].casefold()],sponsorId=sponsors[str(v[10])],isNew=not bool(a),newOwner=not bool(e),metadata=json.dumps(dict(source='رفع نينجا.xlsx',sheet=s.title,row=n,captainId=captain,name=v[2],city=v[3],ownerIqama=iqama,originalOwnerIqama=raw,email=v[5],status=v[6],suspended=v[7],createdAt=v[8],vehicleType=v[9],sponsorRegistry=str(v[10])),ensure_ascii=False)))
assert len(rows)==327 and len(set(r['captainId'] for r in rows))==327
assert all(v==1 for v in collections.Counter((r['ownerId'],r['cityId'],r['sponsorId']) for r in rows).values())
plan=dict(sourcePath=str(source),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),correlationId='ninja-reset-'+uid(),actorId=d['actors'][0]['Id'],platformId=json.loads((root/'tmp/ninja-safe-plan-20261014.json').read_text(encoding='utf-8'))['platformId'],excludedCaptainId='66689',ready=True,cities=cities,rows=rows)
(out/'plan.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(ready=plan['ready'],rows=len(rows),existing=sum(not r['isNew'] for r in rows),new=sum(r['isNew'] for r in rows),newOwners=len(newemployees),cities=len(cities))))
