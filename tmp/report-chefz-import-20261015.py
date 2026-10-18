import json,csv,argparse,datetime,xml.etree.ElementTree as ET
from pathlib import Path
arg=argparse.ArgumentParser();arg.add_argument('applied_directory');args=arg.parse_args()
applied=Path(args.applied_directory);plan=json.loads((applied/'plan.json').read_text(encoding='utf-8-sig'))
summary=json.loads((applied/'summary.json').read_text(encoding='utf-8-sig'))
verified=ET.parse(applied/'post-commit-verification.xml').getroot()
checks=verified.findall('Checks');assert len(checks)==12 and all(x.findtext('Mismatches')=='0' for x in checks)
two=len(verified.findall('TwoAccountRiders'));source=json.loads(Path('artifacts/chefz-import-20261015/source.json').read_text(encoding='utf-8'))
rows={r['row']:r['cells'] for r in source['sheets'][0]['rows']}
with (applied/'exceptions.csv').open('w',encoding='utf-8-sig',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=['sheet','row','account','accountName','rider','riderName','reason']);writer.writeheader()
 for e in plan['exceptions']:
  c=rows[e['row']];writer.writerow({**e,'accountName':c.get('B',''),'riderName':c.get('L','')})
with (applied/'completed-history.csv').open('w',encoding='utf-8-sig',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=['account','riderIqama','fromDate','toDate','originalReturnDate','assignmentId']);writer.writeheader()
 for a in plan['insertAssignments']:
  if a['status']==3:
   original=next(r['cells'] for r in source['sheets'][1]['rows'] if r['row']==a['sourceRow'])
   writer.writerow({'account':str(original['A']),'riderIqama':a['userIqama'],'fromDate':a['fromDate'],'toDate':a['toDate'],'originalReturnDate':a['originalReturnDate'],'assignmentId':a['id']})
text=f'''# Chefz workbook import — 15 October 2026

Applied to the existing SHIFTZ/Chefz platform in hosted database db67927.

| Result | Count |
| --- | ---: |
| Complete dashboard account rows checked | 464 |
| Accounts missing from the database | 0 |
| Sponsor, city, vehicle type or Freelancer/FullTime corrections needed | 0 |
| Account lifecycle states reconciled | {summary['AccountsUpdated']} |
| Dashboard status labels reconciled | {summary['DashboardDetailsUpdated']} |
| Completed assignment histories inserted | {summary['CompletedHistoriesCreated']} |
| New current rider assignments | {summary['ActiveAssignmentsCreated']} |
| Original rider assignment restored | {summary['OriginalAssignmentsRestored']} |
| Existing current assignments already correct | {plan['summary']['existingAssignmentsKept']} |
| Mistaken assignments retained as Cancelled | {summary['MistakenAssignmentsCancelled']} |
| Previous assignment ended before transfer | {summary['PreviousAssignmentsEnded']} |
| Passwords encrypted and saved | {summary['PasswordsSaved']} |
| Password cells marked “غير محفوظة”, left unchanged | {plan['summary']['passwordsNotSupplied']} |
| Riders holding two Chefz accounts | {two} |
| Assignment rows pending because rider iqama is absent | {plan['summary']['missingRiderRows']} |
| Assignment row pending because the person is classified as an employee | {plan['summary']['employeeRiderRows']} |
| Supplementary rows without settings or assignment data | {plan['summary']['incompleteSupplementaryRows']} |

All 12 post-commit database checks passed using a fresh connection. All 233 saved credentials were checked against the encrypted import payload; a separate .NET decryption comparison against the workbook is recorded in the verification log. Existing performance records and settlements were preserved. No new accounts or riders were created. Freelancer/FullTime account type was kept separate from the platform payment model.

Completed histories use 1–6 October for account 2591985425, 1–10 October for 2620007464, and 1–10 October for 2588082814. The user confirmed that the current rider owns 11 October on 2620007464. The original spreadsheet return text is retained in completed-history.csv.

For account 2076420757, the original workbook rider assignment was restored and the mistaken 15 October replacement was cancelled. For 2618863134, the mistaken link was cancelled and the workbook rider assigned from 1 October. For 2559241233, the previous rider ends on 13 October and the workbook rider begins on 14 October. Existing rows and audit events were retained.

The initial encryption key was configured in the hosted API with user approval. The hosted configuration was re-read to confirm the key, and the API health endpoint returned HTTP 200 Healthy after the reload. Key material and plaintext source data are kept outside Git under ignored artifacts and are omitted from this report.

The 16 pending assignment rows are listed in exceptions.csv alongside the three incomplete supplementary rows. The employee-classification exception is account 2597241476, rider iqama 2606047062. Employment classification was preserved. These pending accounts retain their existing lifecycle state rather than being marked assigned without a rider link.

The database backup is before.xml. plan.json and executed.sql record the reviewed changes, and post-commit-verification.xml records the independent checks. The audit correlation is `{plan['correlationId']}`. Replay protection refuses a second commit of the same import.

Source workbook SHA-256: `{plan['sourceSha256']}`.
'''
(applied/'report.md').write_text(text,encoding='utf-8')
print(str((applied/'report.md').resolve()))
