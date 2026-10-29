import collections
import csv
import datetime as dt
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/ninja-workbook-sync-20261025'
source = json.loads((OUT / 'source-extracted.json').read_text(encoding='utf-8'))
db = json.loads((OUT / 'database-snapshot.json').read_text(encoding='utf-8-sig'))


def text(value):
    return '' if value is None else str(value).strip()


def date(value):
    return text(value)[:10] or None


def norm(value):
    return ' '.join(text(value).casefold().split())


def nationality(value):
    aliases = {'مصري': 'مصر', 'بنغلاديشي': 'بنجلاديش', 'باكستاني': 'باكستان',
               'هندي': 'الهند', 'يمني': 'اليمن', 'سوداني': 'السودان', 'إثيوبي': 'اثيوبيا'}
    return aliases.get(text(value), text(value))


def phone(value):
    s = re.sub(r'[^0-9]', '', text(value))
    if s.startswith('00966'):
        s = s[5:]
    elif s.startswith('966'):
        s = s[3:]
    if len(s) == 9 and s.startswith('5'):
        s = '0' + s
    return s


issues = []
rows = []


def issue(code, severity, row=None, account='', field='', expected='', actual='', detail=''):
    issues.append(dict(code=code, severity=severity, sheet=row['sheet'] if row else '',
                       row=row['row'] if row else '', account=row['account'] if row else account,
                       field=field, workbook=expected, database=actual, detail=detail))


for sheet in source['sheets']:
    historical = sheet['sheet'] == 'البدلاء'
    for raw in sheet['rows'][1:]:
        v = raw['values']
        if v[0] is None:
            issue('NON_RECORD_ROW', 'Info', dict(sheet=sheet['sheet'], row=raw['row'], account=''), detail='Summary/formula row; exclude from import.')
            continue
        row = dict(sheet=sheet['sheet'], row=raw['row'], history=historical,
                   account=text(v[0]), owner=text(v[1]), ownerName=v[2], accountStatus=v[3],
                   password=v[4 if historical else 5], rider=text(v[5 if historical else 6]),
                   start=date(v[6 if historical else 8]), end=date(v[7]) if historical else None,
                   city=v[8 if historical else 4], riderName=v[9 if historical else 7],
                   phone=phone(v[10 if historical else 9]), nationality=v[11 if historical else 10],
                   relationship=v[12 if historical else 11], vehicle=v[13 if historical else 12],
                   payment=v[14 if historical else 13], placement=v[15 if historical else 14])
        raw_phone = v[10 if historical else 9]
        row['phoneCandidates'] = [phone(part) for part in re.split(r'[\n\r,;/]+', text(raw_phone)) if text(part)]
        if len(row['phoneCandidates']) > 1:
            row['phone'] = None
        # Sponsorship and vehicle ownership are separate dimensions in this workbook.
        row['suggestedRiderType'] = ('Internal' if text(row['relationship']) == 'كفالة' else 'External') + ('Motorcycle' if text(row['vehicle']).startswith('دباب') else 'Car')
        row['paymentModel'] = {'عمولة': 1, 'راتب': 2}.get(row['payment'])
        rows.append(row)

accounts = collections.defaultdict(list)
for a in db['accounts']:
    accounts[a['ExternalAccountId']].append(a)
employees = collections.defaultdict(list)
for e in db['employees']:
    if not e['IsDeleted']:
        employees[text(e['IqamaNo'])].append(e)
riders = collections.defaultdict(list)
for r in db['riders']:
    if not r['IsDeleted']:
        riders[r['EmployeeId']].append(r)
details = {x['PlatformRiderAccountId']: x for x in db['details'] if not x['IsDeleted']}
assignments = [x for x in db['assignments'] if not x['IsDeleted']]
matched = set()
summary = collections.Counter()
for r in rows:
    if not re.fullmatch('[12][0-9]{9}', r['rider']):
        issue('NONSTANDARD_RIDER_ID', 'Review', r, field='actual rider identity', expected=r['rider'], detail='May be an intentional temporary identity; do not replace or merge automatically.')
    if r['end'] and r['end'] < r['start']:
        issue('REVERSED_DATES', 'Blocker', r, expected=r['start'], actual=r['end'])
    if len(r['phoneCandidates']) > 1:
        issue('MULTIPLE_PHONE_CHOICES', 'Decision', r, expected=' / '.join(r['phoneCandidates']), detail='Cell contains two phone numbers. Preserve both; choose which is the account login phone.')
    elif len(r['phone']) != 10 or not r['phone'].startswith('05'):
        issue('INVALID_PHONE', 'Blocker', r, expected=r['phone'])
    if not r['history'] and r['placement'] != 'تم':
        issue('CURRENT_NOT_PLACED', 'Decision', r, expected=r['placement'], detail='Current sheet names a rider but explicitly says the placement is not completed.')
    if r['history'] and r['placement'] != 'تم':
        issue('HISTORY_NOT_ACCEPTED', 'Decision', r, expected=r['placement'], detail='History row says not accepted; decide whether this represents actual work or a failed attempt.')
    if r['password'] and r['password']['numeric'] and r['password']['length'] < 6:
        issue('SHORT_NUMERIC_PASSWORD', 'Review', r, field='password', detail='Numeric password has fewer than six digits; original leading zero cannot be inferred safely.')
    aa = accounts[r['account']]
    if len(aa) != 1 or aa[0]['IsDeleted']:
        issue('ACCOUNT_MISSING_OR_DELETED', 'Blocker', r, actual=len(aa))
        continue
    a = aa[0]
    if r['owner'] != text(a['OwnerIqama']):
        issue('OWNER_ID_MISMATCH', 'Blocker', r, expected=r['owner'], actual=a['OwnerIqama'])
    if norm(r['ownerName']) not in [norm(a['OwnerNameAr']), norm(a['OwnerNameEn'])]:
        issue('OWNER_NAME_DIFFERENCE', 'Review', r, expected=r['ownerName'], actual=a['OwnerNameEn'] or a['OwnerNameAr'])
    if norm(r['city']) not in [norm(a[k]) for k in ['CityCode', 'CityNameEn', 'CityNameAr']]:
        issue('ACCOUNT_CITY_DIFFERENCE', 'Review', r, expected=r['city'], actual=a['CityNameEn'], detail='Workbook says actual work city; account registration city is a separate concept.')
    ee = employees[r['rider']]
    if len(ee) != 1:
        issue('RIDER_MISSING_OR_AMBIGUOUS', 'Blocker', r, expected=r['rider'], actual=len(ee))
    else:
        e = ee[0]
        if len(riders[e['Id']]) != 1:
            issue('RIDER_PROFILE_MISSING_OR_AMBIGUOUS', 'Blocker', r, actual=len(riders[e['Id']]))
        if e['Status'] != 3 and not r['history']:
            issue('CURRENT_RIDER_NOT_ACTIVE', 'Review', r, actual=e['Status'])
        if norm(r['riderName']) not in [norm(e['FullNameAr']), norm(e['FullNameEn'])]:
            issue('RIDER_NAME_DIFFERENCE', 'Review', r, expected=r['riderName'], actual=e['FullNameAr'], detail='Match identity first; aliases/contractor annotations may explain the difference.')
        if not r['history']:
            for field, wanted, existing in [('nationality', r['nationality'], e['Nationality']), ('rider phone', r['phone'], phone(e['PrimaryPhone']))]:
                if wanted is not None and norm(wanted) != norm(existing):
                    issue('RIDER_PROPERTY_DIFFERENCE', 'Change', r, field=field, expected=wanted, actual=existing)
    found = [x for x in assignments if x['ExternalAccountId'] == r['account'] and text(x['ActualIqama']) == r['rider'] and date(x['EffectiveFrom']) == r['start'] and date(x['EffectiveTo']) == r['end']]
    if len(found) != 1:
        issue('DUPLICATE_DATABASE_ASSIGNMENT' if len(found) > 1 else 'ASSIGNMENT_NOT_EXACT', 'Decision' if len(found) > 1 else 'Change', r, actual=len(found), detail='No unique assignment matches account, rider, start and end dates.')
        summary['unmatchedSourceAssignments'] += 1
        r['assignmentId'] = None
    else:
        x = found[0]
        matched.add(x['Id'])
        r['assignmentId'] = x['Id']
        summary['matchedHistory' if r['history'] else 'matchedCurrent'] += 1
        if x['NinjaRiderType'] != r['suggestedRiderType']:
            issue('RIDER_TYPE_DIFFERENCE', 'Change', r, expected=r['suggestedRiderType'], actual=x['NinjaRiderType'], detail='Suggested mapping uses sponsorship for internal/external and vehicle label for car/motorcycle.')
        if x['PaymentModel'] != r['paymentModel']:
            issue('ASSIGNMENT_PAYMENT_DIFFERENCE', 'Change', r, expected=r['paymentModel'], actual=x['PaymentModel'])
        if x['Status'] != (3 if r['history'] else 2):
            issue('ASSIGNMENT_STATUS_DIFFERENCE', 'Change', r, expected=3 if r['history'] else 2, actual=x['Status'])
    if not r['history']:
        opens = [x for x in assignments if x['ExternalAccountId'] == r['account'] and x['EffectiveTo'] is None]
        if opens and text(opens[0]['ActualIqama']) != r['rider']:
            issue('CURRENT_RIDER_CONFLICT', 'Blocker', r, expected=r['rider'], actual=opens[0]['ActualIqama'])
        if r['phone'] and phone(details.get(a['Id'], {}).get('PhoneNumber')) != r['phone']:
            issue('LOGIN_PHONE_DIFFERENCE', 'Change', r, expected=r['phone'], actual=details.get(a['Id'], {}).get('PhoneNumber'))
        if a['PaymentModel'] != r['paymentModel']:
            issue('ACCOUNT_PAYMENT_DIFFERENCE', 'Change', r, expected=r['paymentModel'], actual=a['PaymentModel'])

for x in assignments:
    if x['Id'] not in matched:
        issue('DATABASE_ASSIGNMENT_UNMATCHED', 'Review', account=x['ExternalAccountId'], actual=x['ActualIqama'], detail=f"DB assignment {x['Id']}: {date(x['EffectiveFrom'])} to {date(x['EffectiveTo']) or 'open'}; not an exact workbook match.")

for dimension in ['account', 'rider']:
    grouped = collections.defaultdict(list)
    for r in rows:
        grouped[r[dimension]].append(r)
    for key, group in grouped.items():
        group.sort(key=lambda r: (r['start'], r['end'] or '9999'))
        for i, first in enumerate(group):
            for second in group[i+1:]:
                if second['start'] > (first['end'] or '9999'):
                    continue
                boundary = first['end'] == second['start']
                issue(('SAME_DAY_' if boundary else 'OVERLAPPING_') + dimension.upper(), 'Review' if boundary else 'Blocker', second, expected=f"{first['sheet']} row {first['row']}", detail=f"{dimension} {key}; {first['start']} to {first['end'] or 'open'} versus {second['start']} to {second['end'] or 'open'}; inclusive end dates.")

used = {r['account'] for r in rows}
db_only = [a for a in db['accounts'] if not a['IsDeleted'] and a['ExternalAccountId'] not in used]
current = [r for r in rows if not r['history']]
history = [r for r in rows if r['history']]
summary.update(dict(historyRows=len(history), currentRows=len(current), uniqueAccounts=len(used),
                    currentPlaced=sum(r['placement'] == 'تم' for r in current), currentNotPlaced=sum(r['placement'] != 'تم' for r in current),
                    databaseAccounts=sum(not a['IsDeleted'] for a in db['accounts']), databaseOnlyAccounts=len(db_only),
                    databaseAssignments=len(assignments), unmatchedDatabaseAssignments=sum(x['Id'] not in matched for x in assignments),
                    currentSalary=sum(r['paymentModel'] == 2 for r in current), currentCommission=sum(r['paymentModel'] == 1 for r in current),
                    missingDatabaseRiderTypes=sum(x['NinjaRiderType'] is None for x in assignments),
                    exactDuplicateAssignmentGroups=sum(n > 1 for n in collections.Counter((x['ExternalAccountId'],x['ActualIqama'],date(x['EffectiveFrom']),date(x['EffectiveTo'])) for x in assignments).values()),
                    accountsWithSavedPasswords=sum(x['SavedVersionCount'] > 0 for x in db['credentialMetadata']),
                    dayUsageRows=len(db['dayUsages']),
                    actualPhoneDifferences=sum(i['code']=='RIDER_PROPERTY_DIFFERENCE' and i['field']=='rider phone' for i in issues)))
for i in issues:
    if i['code'] == 'RIDER_PROPERTY_DIFFERENCE' and i['field'] == 'nationality' and nationality(i['workbook']) == nationality(i['database']):
        i['code'] = 'NATIONALITY_LABEL_ONLY'
        i['severity'] = 'Info'
        i['detail'] = 'Same nationality using country name versus adjective; not a citizenship conflict.'
result = dict(source=source['source'], sha256=source['sha256'], snapshot=db['snapshot'], summary=dict(summary),
              issueCounts=dict(collections.Counter(i['code'] for i in issues)), severityCounts=dict(collections.Counter(i['severity'] for i in issues)),
              rows=rows, issues=issues, databaseOnlyAccounts=[a['ExternalAccountId'] for a in db_only])
(OUT / 'analysis.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
with (OUT / 'issues.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=list(issues[0]));writer.writeheader();writer.writerows(issues)
print(json.dumps({k:result[k] for k in ['summary','issueCounts','severityCounts']},ensure_ascii=False,indent=2))
for i in issues:
    if i['severity'] in ['Blocker','Decision']:
        print(json.dumps(i,ensure_ascii=False))
