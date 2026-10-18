import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=json.loads((root/'tmp/ninja-preflight-problems-20261013.json').read_text(encoding='utf8'))
s=json.loads((root/'tmp/ninja-source-20261013.json').read_text(encoding='utf8'))
out=root/'outputs/ninja-preflight-20261013'
out.mkdir(parents=True,exist_ok=True)
lines=['# Ninja import problems — 13 October 2026','',
       'Read-only preflight. No accounts, assignments, owner profiles, or other database records have been changed.', '',
       f"The workbook contains {p['sourceRows']} accounts: 146 working, 103 not working, and 80 with an account problem. Repeated header rows 104 and 238 are excluded. There are no duplicate account IDs or duplicate actual-user iqamas.", '',
       'Of the 146 working accounts, 111 currently meet the existing ownership and assignment rules. The remaining 35 have the issues below. One additional unused account has a duplicate ownership conflict.', '',
       '| Issue | Count | Required resolution |', '|---|---:|---|',
       '| User cell contains a label rather than an iqama | 14 | Supply the actual user iqama, or keep assignment pending. |',
       '| User iqama is missing from the ERP | 10 | Correct the number or add the actual user with their real identity details, or keep assignment pending. |',
       '| Registered owner is missing | 1 | Account 255527, owner iqama 2629274197. Correct or add the owner; recording only ownership notes would leave the owner link unresolved. |',
       '| Registered owner has no rider profile | 9 | These owners are existing staff. The current Ninja workflow requires an owner rider profile to create the ownership registration. A minimal profile could be added without changing employment status. |',
       '| Actual rider already has an active account | 1 | Ninja 215746, user iqama 2492838038, already assigned to Chefz 2618863134. The current policy permits one active account per rider. Leave pending or explicitly end the Chefz assignment before moving the rider. |',
       '| Duplicate owner, city, and sponsor | 1 pair | Accounts 279581 and 66689 share owner iqama 2577660661, Jeddah, and البوابة. The database allows only one available/assigned account for this combination. Account 279581 is working; 66689 is not working. Choose an inactive status or correct the duplicate ownership data. |', '',
       '## Field limitations to decide before import', '',
       '- Vehicle type: 253 car accounts and 76 motorcycle accounts. The current Ninja account schema has no dedicated vehicle-type field. Account notes can retain the supplied type; a separate field would require a backend/schema change. The file describes the account type, so changing the owner’s employee work type would change a different record.',
       '- Status: source values are يعمل, لايعمل, and مشكلة بالحساب. ERP account statuses are Available, Assigned, Suspended, Retired, and Archived. Working accounts can become Assigned only after a valid rider assignment. Pending working accounts need their source status retained separately. Not-working and problem statuses need an agreed mapping.',
       '- Sponsor and city: all three sponsors and both cities have existing catalog records. No new sponsor or city is required. Proposed sponsor mapping: أكسبرس → اكسبرس جايت; التجارية → البوابة المقبلة للتجارة; البوابة → البوابا الموكبلا.',
       '- Assignment dates: the workbook contains no effective-start date. Today, 13 October 2026, can be used for new assignments if that is the intended date.', '',
       '## Every affected row', '',
       '| Excel row | Ninja account ID | Owner iqama | User value | Problem |','|---:|---|---|---|---|']
for r in p['issues']:
    user=(r['userRaw'] or '').replace('|','\\|')
    reason='; '.join(r['reasons']).replace('SHIFTZ','Chefz')
    lines.append(f"| {r['row']} | {r['externalId']} | {r['ownerIqama']} | {user} | {reason} |")
lines+=['',f"Source: بيانات نينجا بعد التحديث.xlsx, worksheet بيانات نينجا. Source SHA-256: `{s['sourceSha256']}`.",'',
        'This report records preflight results only. No proposed workaround or assignment change has been executed.']
(out/'problems.md').write_text('\n'.join(lines)+'\n',encoding='utf8')
print(out/'problems.md')
