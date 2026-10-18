import json,sys,collections
from pathlib import Path
plan=json.loads(Path('tmp/chefz-assignment-plan-20261013.json').read_text(encoding='utf-8'))
db=json.loads(Path('tmp/chefz-assignment-db-20261013.json').read_text(encoding='utf-8-sig'))
out=Path(sys.argv[1]);applied='--applied' in sys.argv
lines=['# Chefz rider sheet import — 13 October 2026','',
       ('Status: applied and verified.' if applied else 'Status: reviewed with a successful rolled-back transaction. No permanent changes yet.'),'',
       'Source: `تقرير سيستم  داتا بدلاء .xlsx`, worksheet `ورقة1`, rows 2–306. Match column A to Chefz account ID and column B to the actual rider iqama. Names are ignored. Column D is day/month/year. IBAN whitespace is removed and letters are uppercased. Arabic notes in the IBAN column are treated as data exceptions, not instructions.','',
       '| Result | Count |','| --- | ---: |','| Sheet rows | 305 |','| Owner iqamas matching Chefz accounts | 304 |',
       '| Chefz assignments created | 217 |','| Assignment rows pending | 88 |','| Previous Chefz assignments found | 0 |',
       '| Conflicting Jahez assignment histories removed | 12 |','| Jahez assignments closed normally | 1 |',
       '| Jahez opening ledger entries removed with conflicting histories | 36 |','| Jahez fee records removed with conflicting histories | 12 |',
       '| Rider IBANs added | 254 |','| Rider IBANs replaced | 0 |','| Matching rider IBANs already correct | 5 |','',
       'Counts below can overlap. Duplicate riders remain pending on every affected source row. Suspended Chefz accounts remain suspended. Riders and accounts were not created for unmatched iqamas.','',
       '## Pending assignments','', '| Sheet row | Owner iqama | Rider iqama | Reason |','| ---: | --- | --- | --- |']
for r in plan['rows']:
    if r['assignmentAction']=='Pending':lines.append(f"| {r['row']} | {r['ownerIqama']} | {r['userIqama'] or 'Missing'} | {'; '.join(r['finalIssues'])} |")
lines+=['','## IBAN exceptions for existing riders','','| Sheet row | Rider iqama | Reason |','| ---: | --- | --- |']
for r in plan['rows']:
    if r.get('employeeId') and r['ibanAction'] in ('Missing in sheet','Invalid in sheet'):
        reason='Missing IBAN in sheet' if not r['iban'] else 'Invalid Saudi IBAN length/checksum' if r['iban'].startswith('SA') else 'Text note instead of IBAN; no action taken from note'
        lines.append(f"| {r['row']} | {r['userIqama']} | {reason} |")
lines+=['','## Jahez histories selected for removal','','The linked records consist of opening ledger entries and fee setup records. No imported transactions, dispatches, payment receipts, settlements, approval requests, or earnings statements are attached to these 12 handovers. The full affected rows are saved in `before.xml`.','',
        '| Sheet row | Rider iqama | Jahez account ID | Sheet Chefz start |','| ---: | --- | --- | --- |']
for x in plan['removeJahez']:
    r=next(r for r in plan['rows'] if r['row']==x['row'])
    a=next(a for a in db['assignments'] if a['Id']==x['assignmentId'])
    lines.append(f"| {r['row']} | {r['userIqama']} | {a['ExternalAccountId']} | {r['effectiveFrom']} |")
lines+=['','## Normal Jahez closure','','Rider `2514944897` (sheet row 220) ends Jahez on 12 October 2026 and starts Chefz on 13 October 2026. The existing Jahez close rule adds SAR 75 commission for 8–12 October, five days at SAR 15/day. Its earlier financial history is retained.','',
        '## Verification','','The transaction checks every imported account/rider/date and changed IBAN, enforces one active account for every imported rider, and confirms unrelated account, employee, and assignment records remain unchanged. The rolled-back run verified all planned changes against SQL Server. A post-commit read repeats the comparison for the committed import.','']
out.write_text('\n'.join(lines),encoding='utf-8')
print(out)
