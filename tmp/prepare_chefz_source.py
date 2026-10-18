import json
from pathlib import Path
data=json.loads(Path('tmp/chefz-workbook.json').read_text(encoding='utf-8'))
rows=[]
for r in data[0]['cells'][1:]:
    cells={''.join(c for c in k if c.isalpha()):v.strip() for k,v in r.items()}
    rows.append(dict(id=cells['A'], accountType=1 if cells['B']=='FREELANCER' else 2,
        sponsor=cells['C'], vehicleType='Car' if cells['D']=='سياره' else 'Motorcycle',city=cells['E'],status=cells['F']))
stats=[]
for label,col,sponsor in [('البوابة','B','7015658094'),('اكسبرس','D','7034861059'),('البوابة التجارية','F','7038745530')]:
    values={}
    for row in data[2]['cells']:
        for key,v in row.items():
            if key.startswith(col) and key[1:].isdigit(): values[int(key[1:])]=v
    names=['Working','NotWorking','Blocked','PermanentlyBlocked','UpdateBlocked','Uploaded','PrivateLicense','PendingUpload','NoLicense','ManagementSuspended','Motorcycle','WorkingOutsideSponsorship']
    stats.append(dict(sponsor=sponsor,SponsorshipCount=int(values[3]),**{name:int(values[n]) for n,name in enumerate(names,4)}))
Path('tmp/chefz-update-source.json').write_text(json.dumps(dict(accounts=rows,statistics=stats),ensure_ascii=False),encoding='utf-8')
print('Prepared',len(rows),'account rows and',len(stats),'sponsor statistics rows.')
