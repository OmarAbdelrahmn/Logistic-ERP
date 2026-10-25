import openpyxl,json,re,collections,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
out=root/'outputs/ninja-comparison-20261022'
source=Path(r'C:\Users\omarf\Downloads\رفع نينجا.xlsx')
db=json.loads((out/'database-snapshot.json').read_text(encoding='utf-8-sig'))
digits=str.maketrans('٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹','01234567890123456789')
def ident(v,fmt=''):
    if v is None:return ''
    if isinstance(v,(int,float)):
        assert int(v)==v and 0<=v<10**15
        s=str(int(v))
        return s.zfill(len(fmt)) if re.fullmatch('0+',fmt) else s
    return str(v).strip().translate(digits)
def norm(v):return ' '.join(str(v or '').split()).casefold()
w=openpyxl.load_workbook(source,read_only=True,data_only=False)
rows=[]
for s in w:
    for n,c in enumerate(s.iter_rows(min_row=2),2):
        if not any(x.value is not None for x in c):continue
        assert all(c[i].data_type!='f' for i in [0,2,3,4,6,7,9,10]),'Formula in compared field'
        v=[x.value for x in c]
        r=dict(sheet=s.title,row=n,id=ident(v[0],c[0].number_format),name=v[2],city=v[3],iqama=ident(v[4],c[4].number_format),status=v[6],suspended=v[7],vehicle=v[9],registry=ident(v[10],c[10].number_format))
        assert re.fullmatch(r'\d+',r['id'])
        rows.append(r)
assert all(n==1 for n in collections.Counter(r['id'] for r in rows).values()),'Duplicate sheet IDs'
accounts=[a for a in db['accounts'] if not a['IsDeleted']]
idx={ident(a['ExternalAccountId']):a for a in accounts}
assert len(idx)==len(accounts),'Duplicate database IDs'
sheetids={r['id'] for r in rows}
onlydb=[a for a in accounts if ident(a['ExternalAccountId']) not in sheetids]
detail=[]
counts=collections.Counter();fields=collections.Counter();sheets={}
states={1:'Available',2:'Assigned',3:'Suspended',4:'Retired',5:'Archived'}
for r in rows:
    a=idx.get(r['id']);diff=[]
    if not re.fullmatch(r'[12]\d{9}',r['iqama']):diff.append('Invalid sheet owner ID (must be 10 digits)')
    if a:
        if r['iqama']!=ident(a['IqamaNo']):diff.append('Owner ID')
        if norm(r['name']) not in [norm(a['FullNameAr']),norm(a['FullNameEn'])]:diff.append('Owner name')
        if norm(r['city']) not in [norm(a[k]) for k in ['CityCode','CityAr','CityEn']]:diff.append('City')
        if r['registry']!=ident(a['SponsorRegistry']):diff.append('Sponsor registry')
        result='Different details' if diff else 'Matching'
    else:result='Sheet only'
    counts[result]+=1;fields.update(diff)
    sc=sheets.setdefault(r['sheet'],collections.Counter());sc['Rows']+=1;sc[result]+=1
    candidates=[x['ExternalAccountId'] for x in onlydb if r['iqama']==ident(x['IqamaNo'])] if not a else []
    detail.append([result,r['sheet'],r['row'],r['id'],'; '.join(diff),r['name'],(a['FullNameEn'] or a['FullNameAr']) if a else None,r['iqama'],a['IqamaNo'] if a else None,r['city'],(a['CityEn'] or a['CityAr']) if a else None,r['registry'],a['SponsorRegistry'] if a else None,r['status'],r['suspended'],states.get(a['Status']) if a else None,r['vehicle'],a['Code'] if a else None,', '.join(candidates)])
for a in onlydb:
    detail.append(['Database only',None,None,a['ExternalAccountId'],'Absent from workbook',None,a['FullNameEn'] or a['FullNameAr'],None,a['IqamaNo'],None,a['CityEn'] or a['CityAr'],None,a['SponsorRegistry'],None,None,states.get(a['Status']),None,a['Code'],None])
counts['Database only']=len(onlydb)
assert len(rows)==counts['Matching']+counts['Different details']+counts['Sheet only']
assert len(accounts)==counts['Matching']+counts['Different details']+counts['Database only']
result=dict(source=str(source),sha256=hashlib.sha256(source.read_bytes()).hexdigest(),snapshot=db['snapshot'][0],sheetRows=len(rows),dbRows=len(accounts),counts=dict(counts),fields=dict(fields),sheets=sheets,statuses=dict(collections.Counter(r['status'] for r in rows)),suspended=sum(r['suspended'] is True for r in rows),headers=['Result','Source sheet','Source row','Captain ID','Differences','Sheet owner name','DB owner name','Sheet owner ID','DB owner ID','Sheet city','DB city','Sheet registry','DB registry','Provider status','Provider suspended','ERP account status','Sheet vehicle','ERP code','Possible old Captain IDs'],detail=detail)
(out/'comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ['detail','headers','source','sha256']},ensure_ascii=False))
