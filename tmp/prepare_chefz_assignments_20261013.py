import json,re,collections
from pathlib import Path
from datetime import datetime
raw=json.loads(Path('tmp/chefz-assignment-workbook-20261013.json').read_text(encoding='utf-8'))
db=json.loads(Path('tmp/chefz-assignment-db-20261013.json').read_text(encoding='utf-8-sig'))
def norm(v):
    return re.sub(r'\s+','',str(v or '')).upper()
def valid_iban(s):
    if not re.fullmatch(r'SA\d{22}',s):return False
    x=s[4:]+s[:4]
    return int(''.join(str(ord(c)-55) if c.isalpha() else c for c in x))%97==1
rows=[]
for cells in raw[0]['cells'][1:]:
    n=int(re.search(r'\d+',next(iter(cells))).group())
    vals={re.sub(r'\d+','',k):v for k,v in cells.items()}
    owner=norm(vals.get('A'));user=norm(vals.get('B'));iban=norm(vals.get('E'))
    dt=vals.get('D');parsed=None
    if dt:
        for fmt in ('%d/%m/%Y','%Y-%m-%dT%H:%M:%S','%Y-%m-%d'):
            try:parsed=datetime.strptime(str(dt),fmt).date().isoformat();break
            except ValueError:pass
    rows.append(dict(row=n,ownerIqama=owner,userIqama=user,effectiveFrom=parsed,sourceDate=dt,iban=iban,ibanValid=valid_iban(iban)))
accounts=collections.defaultdict(list);employees=collections.defaultdict(list)
for a in db['accounts']:
    if not a['IsDeleted']:accounts[a['ExternalAccountId']].append(a)
for e in db['employees']:
    if not e['IsDeleted']:employees[e['IqamaNo']].append(e)
usercounts=collections.Counter(r['userIqama'] for r in rows)
ownercounts=collections.Counter(r['ownerIqama'] for r in rows)
for r in rows:
    issues=[];aa=accounts[r['ownerIqama']];ee=employees[r['userIqama']]
    if not re.fullmatch(r'\d{10}',r['ownerIqama']):issues.append('Invalid owner iqama')
    if len(aa)!=1:issues.append('Owner Chefz account missing or ambiguous')
    else:
        a=aa[0];r.update(accountId=a['Id'],accountStatus=a['Status'],paymentModel=a['PaymentModel'],registeredOwnerIqama=a['OwnerIqama'])
        if a['OwnerIqama'] and a['OwnerIqama']!=r['ownerIqama']:issues.append('Registered owner iqama mismatch')
        if a['Status'] in (3,4,5):issues.append('Chefz account unavailable')
    if len(ee)!=1:issues.append('Rider employee missing or ambiguous')
    else:
        e=ee[0];r.update(employeeId=e['Id'],riderId=e['RiderProfileId'],employeeStatus=e['Status'],isEmployee=e['IsEmployee'],oldIban=e['Iban'])
        if not e['RiderProfileId'] or e['RiderDeleted']:issues.append('Rider profile missing')
        if e['Status']!=3 or e['IsEmployee']:issues.append('Rider unavailable')
        others=[a for a in db['assignments'] if not a['IsDeleted'] and not a['EffectiveTo'] and a['Code']!='SHIFTZ' and a['RiderProfileId']==e['RiderProfileId']]
        if others:issues.append('Rider has another platform assignment');r['otherAssignments']=others
        r['ibanAction']='Missing in sheet' if not r['iban'] else 'Invalid in sheet' if not r['ibanValid'] else 'Unchanged' if norm(e['Iban'])==r['iban'] else 'Add' if not norm(e['Iban']) else 'Update'
    if not r['effectiveFrom']:issues.append('Missing or invalid handover date')
    elif r['effectiveFrom']>'2026-10-13':issues.append('Future handover date')
    if ownercounts[r['ownerIqama']]>1:issues.append('Duplicate owner iqama in sheet')
    if r['userIqama'] and usercounts[r['userIqama']]>1:issues.append('Duplicate rider iqama in sheet')
    r['issues']=issues
out=dict(rows=rows,sourceSha256='88db9f8fbfdfbe77c4cbf94e22f27c20423a91cd7d63b22672ffed2f2939f116',platformId=db['platforms'][0]['Id'],contractId=db['contracts'][0]['Id'],actorId=db['actors'][0]['Id'])
Path('tmp/chefz-assignment-plan-20261013.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
print('Rows',len(rows),'Clean assignments',sum(not r['issues'] for r in rows))
print('Issues',dict(collections.Counter(x for r in rows for x in r['issues'])))
print('IBAN actions',dict(collections.Counter(r.get('ibanAction','No rider') for r in rows)))
print('Assignment platforms',dict(collections.Counter(a['Code'] for a in db['assignments'])))
print('Dates',dict(collections.Counter(r['effectiveFrom'] for r in rows)))
print('Unique matching riders',len({r['employeeId'] for r in rows if r.get('employeeId')}))
print('Invalid IBAN rows',[r['row'] for r in rows if r['iban'] and not r['ibanValid']])
