import openpyxl, json, hashlib, collections, datetime, pathlib
src=pathlib.Path(r'C:\Users\omarf\Downloads\الايدي جاهز شهر 10.xlsx')
w=openpyxl.load_workbook(src,data_only=True)
rows=[]
def ident(x):
    if x is None:return None
    if isinstance(x,(int,float)):return str(int(x)) if int(x)==x else str(x)
    return str(x).strip()
for s in w:
    for i,v in enumerate(s.values,1):
        if i==1 or not any(x is not None for x in v):continue
        rows.append(dict(row=i,account=ident(v[0]),serial=ident(v[1]),ownerIqama=ident(v[2]),sponsorNumber=ident(v[3]),status=ident(v[4]),riderIqama=ident(v[5]),date=v[6].isoformat() if isinstance(v[6],(datetime.date,datetime.datetime)) else ident(v[6])))
p=dict(sourcePath=str(src),sourceSha256=hashlib.sha256(src.read_bytes()).hexdigest(),sheet=w.sheetnames[0],rows=rows)
pathlib.Path('tmp/jahez-source-20261017.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf-8')
groups=collections.defaultdict(list)
for r in rows:groups[r['account']].append(r)
print(json.dumps(dict(rows=len(rows),uniqueAccounts=len(groups),statuses=collections.Counter(r['status'] for r in rows),sponsors=collections.Counter(r['sponsorNumber'] for r in rows),duplicateAccounts={k:v for k,v in groups.items() if len(v)>1},missingRider=sum(not r['riderIqama'] for r in rows),dates=collections.Counter(r['date'] for r in rows),missingSerial=[r for r in rows if not r['serial']]),ensure_ascii=False,indent=2))
