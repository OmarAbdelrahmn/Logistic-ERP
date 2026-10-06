import json
from collections import Counter, defaultdict
from pathlib import Path

root = Path(__file__).parent
book = json.loads((root / 'jeddah-workbook.json').read_text(encoding='utf-8'))
details = json.loads((root / 'jeddah-details.json').read_text(encoding='utf-8-sig'))
employees = details[3]
by_iqama = {e['IqamaNo']: e for e in employees if e['IqamaNo']}
periods = {p['EmployeeId']: p for p in details[0]}
room_groups = defaultdict(list)
people = []
for row in book[-1]['rows'][1:]:
    v = row['values']
    key = (v[0], v[5], str(v[6]))
    room_groups[key].append(v)
    if v[13] != 'مشغول':
        assert v[13] == 'شاغر', row
        continue
    iqama = str(v[9]) if v[9] is not None else None
    assert iqama is None or len(iqama) == 10 and iqama.isdigit(), row
    match = by_iqama.get(iqama) if iqama else None
    p = {'row': row['row'], 'code': v[0], 'floor': v[5], 'room': str(v[6]), 'iqama': iqama, 'name': v[10], 'type': v[11], 'work': v[12], 'notes': v[14], 'employee': match, 'current': periods.get(match['Id']) if match else None}
    if not match:
        tokens = v[10].casefold().split()
        p['candidates'] = [e for e in employees if any(all(t in (e[f] or '').casefold() for t in tokens) for f in ('FullNameAr','FullNameEn'))]
    people.append(p)
assert not [q for q,c in Counter(p['iqama'] for p in people if p['iqama']).items() if c>1]
for key, rows in room_groups.items():
    assert len({r[8] for r in rows}) == 1
    assert len(rows) == rows[0][8], (key, len(rows), rows[0][8])
    assert len([p for p in people if (p['code'],p['floor'],p['room']) == key]) <= rows[0][8]
summary = {code: {'rooms': sum(k[0] == code for k in room_groups), 'capacity': sum(rows[0][8] for k,rows in room_groups.items() if k[0]==code), 'occupants': sum(p['code']==code for p in people)} for code in ('safa','SAMER','MECANIC')}
print(json.dumps({'summary':summary,'matchedIqama':sum(p['employee'] is not None for p in people),'missingIqama':sum(p['iqama'] is None for p in people),'unmatched':[{k:v for k,v in p.items() if k!='current'} for p in people if not p['employee']], 'currentlyAssigned':[{k:v for k,v in p.items() if k not in ('employee','candidates')} for p in people if p['current']], 'existingJeddahAssignments': [p for p in details[0] if p['HousingCode'] in ('SAMER VILA1','MECANIC','JEDDAH SAFA')]}, ensure_ascii=False, indent=2))
(root / 'jeddah-reconciliation.json').write_text(json.dumps({'summary':summary,'people':people,'rooms':[{'code':k[0],'floor':k[1],'room':k[2],'capacity':v[0][8], 'name':v[0][7]} for k,v in room_groups.items()]}, ensure_ascii=False,indent=2),encoding='utf-8')
