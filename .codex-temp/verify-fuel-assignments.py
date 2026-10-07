import json
from pathlib import Path
root=Path(__file__).parent
before=json.loads((root/'fuel-db-before.json').read_text(encoding='utf-8-sig'))
after=json.loads((root/'fuel-db-after.json').read_text(encoding='utf-8-sig'))
committed=json.loads((root/'fuel-assignment-commit.json').read_text(encoding='utf-8-sig'))
rerun=json.loads((root/'fuel-assignment-refresh-dry.json').read_text(encoding='utf-8-sig'))
rows=json.loads((root/'fuel-reconciliation.json').read_text(encoding='utf-8'))
ready={r['card']:r for r in rows if not r['issues'] or r['issues']==['already_assigned']}
assert committed[0][0]['Committed'] and committed[0][0]['CreatedAssignments']==213
assert rerun[0][0]['CreatedAssignments']==0 and rerun[0][0]['AlreadyAssigned']==215
assert len(committed[1])==214
for record in rerun[1]:
    source=ready[record['CardNumber']]
    assert record['IqamaNo']==source['iqama'] and record['RiderId']==source['employees'][0]['RiderProfileId']
    matches=[a for a in after[3] if a['FuelCardId']==source['cards'][0]['Id'] and a['EffectiveTo'] is None]
    assert len(matches)==1 and matches[0]['EmployeeId']==record['EmployeeId'] and matches[0]['RiderProfileId']==record['RiderId']
after_by_id={a['Id']:a for a in after[3]}
selected_ids={r['cards'][0]['Id'] for r in ready.values()}
assert all(after_by_id[a['Id']]==a for a in before[3] if a['FuelCardId'] in selected_ids)
assert before[4]==after[4]
blocked={r['card']:r for r in rows if r['issues'] and r['issues']!=['already_assigned']}
assert len(blocked)==8
report=Path('outputs/fuel-card-assignment-20261006/assignment-results.md')
lines=['Fuel card assignments — 6 October 2026','', '213 new assignments saved in this task. Final verification confirmed 215 cards assigned to 214 riders.','', 'Both BW218 rows were removed from the corrected workbook. Identical duplicate entries for BW356 and BW357 were processed once.','', 'Pending cards','', '| Card | Iqama | Spreadsheet name | Reason |','|---|---|---|---|']
for r in blocked.values():
    reason='No matching employee/rider record for this Iqama' if not r['employees'] else 'Classified as staff; ERP requires a rider'
    lines.append(f"| {r['card']} | {r['iqama']} | {r['name']} | {reason} |")
report.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(json.dumps({'verifiedAssignedCards':215,'newAssignmentsInThisTask':213,'pendingCards':8,'selectedHistoricalAssignmentsPreserved':True}))
