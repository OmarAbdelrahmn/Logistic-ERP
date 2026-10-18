import openpyxl, json, hashlib, re, collections
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = Path(r'C:\Users\omarf\Downloads\بيانات نينجا بعد التحديث.xlsx')
w = openpyxl.load_workbook(source, read_only=True, data_only=True)
rows, skipped = [], []
def identifier(value):
    if value is None: return None
    if isinstance(value, (int, float)) and int(value) == value: return str(int(value))
    return str(value).strip().translate(str.maketrans('٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹','01234567890123456789'))
for n, cells in enumerate(w.active.values, 1):
    if identifier(cells[0]) == 'ID':
        skipped.append(n)
        continue
    if not any(cells): continue
    account, owner, sponsor, city, vehicle, status, user, phone = cells
    user, phone = identifier(user), identifier(phone)
    rows.append(dict(row=n, externalId=identifier(account), ownerIqama=identifier(owner),
                     sponsor=sponsor.strip(), city=city.strip(), vehicleType=vehicle.strip(), sourceStatus=status.strip(),
                     userIqama=user if user and re.fullmatch(r'[12]\d{9}',user) else None,
                     userRaw=user, phone=('0'+phone if phone and re.fullmatch(r'5\d{8}',phone) else phone)))
result=dict(sourcePath=str(source), sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
            effectiveFrom='2026-10-13', skippedHeaderRows=skipped, rows=rows)
(root/'tmp/ninja-source-20261013.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(rows=len(rows),skippedHeaders=skipped,
    categories={key:dict(collections.Counter(r[key] for r in rows)) for key in ['sponsor','city','vehicleType','sourceStatus']},
    activeInvalidUsers=[r for r in rows if r['sourceStatus']=='يعمل' and r['userIqama'] is None],
    duplicateIds=[k for k,v in collections.Counter(r['externalId'] for r in rows).items() if v>1],
    duplicateUsers=[k for k,v in collections.Counter(r['userIqama'] for r in rows if r['userIqama']).items() if v>1]),ensure_ascii=False))
