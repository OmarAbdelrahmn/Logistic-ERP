import json
from collections import Counter, defaultdict
from pathlib import Path
root = Path(__file__).parent
source = json.loads((root/'fuel-assignment-source.json').read_text(encoding='utf-8'))
db = json.loads((root/'fuel-db-before.json').read_text(encoding='utf-8-sig'))
cards, people, assignments, usage = db[1:5]
print('DATABASE', db[0], 'SCHEMA', db[5], 'ACTOR', db[6])
rows = source[0]['rows'][1:]
by_card = defaultdict(list)
for row in rows:
    by_card[row['values'][0]].append(row)
results = []
for row in rows:
    card, iqama, name = row['values']
    matches = [c for c in cards if c['NormalizedCardNumber']==card]
    employees = [p for p in people if p['IqamaNo']==str(iqama)]
    active = [a for a in assignments if matches and a['FuelCardId']==matches[0]['Id'] and a['EffectiveTo'] is None]
    issues = []
    if len({r['values'][1] for r in by_card[card]})>1: issues.append('source_card_conflict')
    if len(matches)!=1: issues.append('card_matches_'+str(len(matches)))
    if len(employees)!=1: issues.append('employee_matches_'+str(len(employees)))
    if len(employees)==1:
        if not employees[0]['RiderProfileId'] or employees[0]['RiderDeleted']: issues.append('no_active_rider_profile')
        if employees[0]['IsEmployee'] or employees[0]['Status']!=1: issues.append('rider_ineligible')
    if active:
        issues.append('already_assigned' if len(employees)==1 and active[0]['RiderProfileId']==employees[0]['RiderProfileId'] else 'card_assigned_to_other')
    result = dict(row=row['row'],card=card,iqama=str(iqama),name=name,issues=issues,cards=matches,employees=employees,active=active)
    results.append(result)
(root/'fuel-reconciliation.json').write_text(json.dumps(results, ensure_ascii=False, indent=2),encoding='utf-8')
print('SUMMARY',dict(Counter(tuple(r['issues']) for r in results)))
print('SOURCE_DUPLICATES', [dict(card=card,rows=[r['row'] for r in group],iqamas=[r['values'][1] for r in group]) for card,group in by_card.items() if len(group)>1])
print('EXCEPTIONS',json.dumps([r for r in results if r['issues']],ensure_ascii=False))
print('ROWS',len(rows),'UNIQUE_CARD_IQAMA',len({(r['values'][0],r['values'][1]) for r in rows}))
