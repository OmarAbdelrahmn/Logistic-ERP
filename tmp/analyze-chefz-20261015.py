import json,collections
from pathlib import Path
d=json.loads(Path('tmp/chefz-db-20261015.json').read_text(encoding='utf-8-sig'))
s=json.loads(Path('tmp/chefz-source-20261015.json').read_text(encoding='utf-8'))
accounts={a['ExternalAccountId']:a for a in d['accounts']}
emps=collections.defaultdict(list)
for e in d['employees']:
    if not e['IsDeleted'] and e['RiderId'] and not e['RiderDeleted']: emps[e['IqamaNo']].append(e)
valid=[r for r in s['sheets'][0]['rows'][1:] if str(r['cells'].get('D','')).strip() in ['FREE','FT']]
missing=[]; issues=[]; changes=collections.Counter(); seen=collections.Counter(); active={a['PlatformRiderAccountId']:a for a in d['assignments'] if a['EffectiveTo'] is None}
for r in valid:
 c=r['cells']; key=str(c.get('C')); a=accounts.get(key); seen[key]+=1
 if not a: missing.append({'row':r['row'],'account':key});continue
 desiredtype=1 if c['D'].strip()=='FREE' else 2
 changes['type']+=a['AccountType']!=desiredtype
 sponsor=next(x for x in d['sponsors'] if str(c['F'])==x['UnifiedNationalNumber'])
 changes['sponsor']+=a['SponsorId']!=sponsor['Id']
 changes['vehicle']+=a['VehicleType']!=('Car' if c['G'].strip()=='سياره' else 'Motorcycle')
 changes['city']+=a['OperatingCityId']!=next(x['Id'] for x in d['cities'] if x['NameAr']==c['H'].strip())
 if c.get('K'):
  iq=str(c['K']); e=emps.get(iq,[]); cur=active.get(a['Id'])
  if len(e)!=1: issues.append({'row':r['row'],'account':key,'rider':iq,'issue':'missing/ambiguous rider','matches':len(e)});continue
  e=e[0]
  if e['Status']!=3: issues.append({'row':r['row'],'account':key,'rider':iq,'issue':'rider inactive','status':e['Status']})
  if cur and cur['RiderProfileId']!=e['RiderId']: issues.append({'row':r['row'],'account':key,'rider':iq,'issue':'different current rider','currentAssignment':cur['Id'],'from':cur['EffectiveFrom']})
  elif cur: changes['existing same rider']+=1;changes['start date']+=cur['EffectiveFrom'][:10]!='-'.join(reversed(c['M'].split('/')))
  else:
   changes['new assignment']+=1
   other=[x for x in active.values() if x['RiderProfileId']==e['RiderId']]
   if other: issues.append({'row':r['row'],'account':key,'rider':iq,'issue':'other current accounts','platforms':[x['PlatformCode'] for x in other]})
 elif a['Id'] in active: issues.append({'row':r['row'],'account':key,'issue':'source no rider; current assignment exists'})
hist=[]
for r in s['sheets'][1]['rows'][1:]:
 c=r['cells'];hist.append({'row':r['row'],'account':str(c['A']),'rider':str(c['M']),'riderMatch':[(x['RiderId'],x['Status']) for x in emps[str(c['M'])]],'accountMatch':str(c['A']) in accounts})
print(json.dumps({'valid':len(valid),'missing':missing,'duplicateAccounts':[k for k,v in seen.items() if v>1],'changes':dict(changes),'issues':issues,'history':hist},ensure_ascii=False,indent=2))
