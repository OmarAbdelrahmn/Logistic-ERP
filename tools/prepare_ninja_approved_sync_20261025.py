"""Prepare the approved, password-free hosted correction manifest."""
import collections
import base64
import datetime as dt
import hashlib
import json
import secrets
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/ninja-workbook-sync-20261025'
db = json.loads((OUT / 'database-snapshot.json').read_text(encoding='utf-8-sig'))
source = json.loads((OUT / 'source-extracted.json').read_text(encoding='utf-8'))
analysis = json.loads((OUT / 'analysis.json').read_text(encoding='utf-8'))
assert hashlib.sha256(Path(source['source']).read_bytes()).hexdigest() == source['sha256'] == analysis['sha256']


def uid():
    ms = int(dt.datetime.now(dt.timezone.utc).timestamp() * 1000)
    return str(uuid.UUID(int=(ms << 80) | (7 << 76) | (secrets.randbits(12) << 64) | (2 << 62) | secrets.randbits(62)))


def j(value):
    return json.dumps(value, ensure_ascii=False, separators=(',', ':'))


def day(value):
    return str(value)[:10] if value else None


actor = db['actors'][0]['Id']
assert len(db['actors']) == 1 and not db['actors'][0]['IsDeleted']
assert not db['triggers'] and not db['newRiderOtherAssignments']
accounts = {a['ExternalAccountId']: a for a in db['accounts'] if not a['IsDeleted']}
employees = {e['IqamaNo']: e for e in db['employees'] if not e['IsDeleted'] and e['IqamaNo']}
riders = {r['EmployeeId']: r for r in db['riders'] if not r['IsDeleted']}
cities = {c['CityEn']: c for c in db['cities'] if not c['IsDeleted'] and c['Status'] == 1}
assert len(db['contracts']) == 1 and db['contracts'][0]['Status'] == 2
contract = db['contracts'][0]['Id']
batch = uid()
plan = dict(batchId=batch, sourcePath=source['source'], sourceSha256=source['sha256'],
            actorId=actor, platformId=db['platform'][0]['Id'], expectedCurrent=151,
            snapshot=db['snapshot'], ignoredColumns=['نوع البديل'], credentialsPolicy='Unchanged: approved corrections 1–4 only',
            updates=[], inserts=[], assignmentEvents=[], mappings=[])


def patch(table, before, values):
    changed = {k: v for k, v in values.items() if before[k] != v}
    if changed:
        plan['updates'].append(dict(table=table, id=before['Id'], rowVersion=before['RowVersion'],
                                    values=changed, auditId=uid(), auditEventId=uid()))
        if table == 'app.RiderClientAssignments':
            plan['assignmentEvents'].append(dict(id=uid(), assignmentId=before['Id'], fromStatus=before['Status'],
                toStatus=values.get('Status', before['Status']), snapshot=j(dict(batchId=batch, before={k:before[k] for k in changed}, after=changed))))


def insert(table, values):
    record = dict(table=table, id=values['Id'], values=values, auditId=uid(), auditEventId=uid())
    plan['inserts'].append(record)


duplicate_id = '01a16df8-6809-71c6-b939-d53d664fe930'
canonical_id = '01a16df6-33eb-7436-b37b-b90230121a6a'
duplicate = next(x for x in db['assignments'] if x['Id'] == duplicate_id)
canonical = next(x for x in db['assignments'] if x['Id'] == canonical_id)
assert all(duplicate[k] == canonical[k] for k in ['ExternalAccountId','ActualIqama','EffectiveFrom','EffectiveTo','PaymentModel','Status'])
assert duplicate['Status'] == 3 and duplicate['ExternalAccountId'] == '215459'
patch('app.RiderClientAssignments', duplicate, dict(IsDeleted=True, DeletionReason=f'User-approved duplicate consolidation; retained assignment {canonical_id}; source {source["sha256"]}'))
duplicate_usage = [u for u in db['dayUsages'] if u['RiderClientAssignmentId'] == duplicate_id and not u['IsDeleted']]
assert len(duplicate_usage) == 1
for u in duplicate_usage:
    assert all(u[k] is None for k in ['OrdersCount','WorkingHours','CollectedAmount','DeductionsAmount','TotalValidOffers','DeclinedOffers','MissedOffers'])
    assert any(v['RiderClientAssignmentId'] == canonical_id and v['BusinessDate'] == u['BusinessDate'] and not v['IsDeleted'] for v in db['dayUsages'])
    patch('app.PlatformAccountDayUsages', u, dict(IsDeleted=True, DeletionReason=f'Duplicate assignment superseded by {canonical_id}; no metric values were present.'))

owner_account = accounts['255527']
owner = next(e for e in db['employees'] if e['Id'] == owner_account['RegisteredEmployeeId'])
assert owner['IqamaNo'] == '2620274197'
assert not any(e['IqamaNo'] == '2629274197' for e in db['employees'])
patch('app.Employees', owner, dict(IqamaNo='2629274197'))

current_riders = set()
registration_accounts = {r['PlatformRiderAccountId'] for r in db['registrations'] if not r['IsDeleted']}
for row in analysis['rows']:
    account = accounts[row['account']]
    employee = employees[row['rider']]
    rider = riders[employee['Id']]
    phone = row['phoneCandidates'][0]
    secondary_phone = row['phoneCandidates'][1] if len(row['phoneCandidates']) > 1 else None
    assert len(phone) == 10 and phone.startswith('05')
    matches = [x for x in db['assignments'] if not x['IsDeleted'] and x['Id'] != duplicate_id
               and x['ExternalAccountId'] == row['account'] and x['ActualIqama'] == row['rider']
               and day(x['EffectiveFrom']) == row['start'] and day(x['EffectiveTo']) == row['end']]
    assert len(matches) <= 1
    permitted_new = (row['account'], row['history']) in [('222872', False), ('272573', False), ('214299', True)]
    assert matches or permitted_new, (row['sheet'], row['row'])
    source_snapshot = dict(schemaVersion=1, batchId=batch, contentSha256=source['sha256'],
        fileName=Path(source['source']).name, sourceSheet=row['sheet'], sourceRow=row['row'],
        captainId=row['account'], registeredOwnerIdentity=row['owner'], registeredOwnerName=row['ownerName'],
        actualRiderIdentity=row['rider'], actualRiderName=row['riderName'],
        workCityId=cities[row['city']]['Id'], workCity=row['city'],
        phoneNumber=phone, secondaryPhoneNumber=secondary_phone, nationality=row['nationality'],
        vehicleLabel=row['vehicle'], vehicleClass='Motorcycle' if row['vehicle'].startswith('دباب') else 'Car',
        vehicleOwnership='Company' if 'شركة' in row['vehicle'] else 'Private',
        paymentModel='Salary' if row['paymentModel'] == 2 else 'PayPerOrder',
        sourceAccountStatus=row['accountStatus'], sourcePlacementLabel=row['placement'],
        placementConfirmedByUser=True, effectiveFrom=row['start'], effectiveTo=row['end'])
    notes = j(source_snapshot)
    assert len(notes) <= 4000
    reference = f'ninja-workbook:{source["sha256"]}:{row["sheet"]}:{row["row"]}'
    if matches:
        assignment = matches[0]
        assert not assignment['OperationalAgreementNotes'] and not assignment['OperationalAgreementReference']
        patch('app.RiderClientAssignments', assignment, dict(PaymentModel=row['paymentModel'],
              OperationalAgreementReference=reference, OperationalAgreementNotes=notes))
        assignment_id = assignment['Id']
    else:
        assignment_id = uid()
        insert('app.RiderClientAssignments', dict(Id=assignment_id, RiderProfileId=rider['Id'], ClientContractId=contract,
            PlatformRiderAccountId=account['Id'], PaymentModel=row['paymentModel'], RiderAccountSlot=1,
            NinjaRiderType=None, EffectiveFrom=row['start'], EffectiveTo=row['end'], Status=3 if row['history'] else 2,
            StartReason='User-confirmed actual assignment from Ninja workbook.',
            EndReason='Historical return recorded in the source workbook.' if row['history'] else None,
            OperationalAgreementReference=reference, OperationalAgreementNotes=notes,
            AssignedByUserId=actor, EndedByUserId=actor if row['history'] else None,
            WasBackdated=True, BackdatedReason='User-approved reconciliation of the October 2026 Ninja assignment workbook.'))
        plan['assignmentEvents'].append(dict(id=uid(), assignmentId=assignment_id, fromStatus=1,
            toStatus=3 if row['history'] else 2, snapshot=j(source_snapshot)))
        assert account['Id'] not in registration_accounts
        registered_owner = next(e for e in db['employees'] if e['Id'] == account['RegisteredEmployeeId'])
        owner_rider = riders.get(registered_owner['Id'])
        if owner_rider is None:
            assert not any(r['EmployeeId'] == registered_owner['Id'] for r in db['riders']), 'Deleted owner profile requires review.'
            owner_rider = dict(Id=uid(), EmployeeId=registered_owner['Id'], TShirtSize=None,
                               OperationalNotes='Owner profile required by the platform registration relationship; created for the approved Ninja workbook reconciliation.')
            insert('app.RiderProfiles', owner_rider)
            riders[registered_owner['Id']] = owner_rider
        insert('app.PlatformAccountRegistrations', dict(Id=uid(), RegisteredEmployeeId=registered_owner['Id'],
            RiderProfileId=owner_rider['Id'], ClientPlatformId=account['ClientPlatformId'], ClientContractId=contract,
            SponsorId=account['SponsorId'], OperatingCityId=account['OperatingCityId'],
            RegistrationType=1 if account['SponsorId'] else 2, Status=4,
            StatusReason=None, RequestedAtUtc=None, ActivatedAtUtc=None,
            PlatformRiderAccountId=account['Id'], Notes='Registration established for user-confirmed imported actual assignment; historical provider activation time is unknown.'))
    plan['mappings'].append(dict(sourceSheet=row['sheet'], sourceRow=row['row'], accountId=account['Id'],
        captainId=row['account'], assignmentId=assignment_id, riderId=rider['Id'], employeeId=employee['Id'],
        fromDate=row['start'], toDate=row['end'], paymentModel=row['paymentModel'],
        cityId=cities[row['city']]['Id'], phone=phone, nationality=row['nationality'],
        reference=reference, notes=notes))
    if not row['history']:
        assert rider['Id'] not in current_riders
        current_riders.add(rider['Id'])
        assert employee['Status'] == 3 and account['Status'] in [1,2]
        patch('app.PlatformRiderAccounts', account, dict(PaymentModel=row['paymentModel'],
              OperatingCityId=cities[row['city']]['Id'], Status=2))
        detail = next(x for x in db['details'] if x['PlatformRiderAccountId'] == account['Id'] and not x['IsDeleted'])
        patch('ninja.NinjaAccountDetails', detail, dict(PhoneNumber=phone))
        patch('app.Employees', employee, dict(PrimaryPhone=phone, Nationality=row['nationality']))

assert len(plan['mappings']) == 229 and len(current_riders) == 151
plan['latestAccounts'] = []
for captain, source_rows in __import__('itertools').groupby(sorted(analysis['rows'],key=lambda r:r['account']),key=lambda r:r['account']):
    source_rows=list(source_rows)
    latest=max(source_rows,key=lambda r:(not r['history'],r['end'] or '9999-12-31',r['start'],r['row']))
    account=accounts[captain]
    if latest['history']:
        patch('app.PlatformRiderAccounts',account,dict(PaymentModel=latest['paymentModel'],OperatingCityId=cities[latest['city']]['Id']))
    plan['latestAccounts'].append(dict(accountId=account['Id'],paymentModel=latest['paymentModel'],cityId=cities[latest['city']]['Id'],status=2 if not latest['history'] else account['Status']))
assert len(plan['updates']) == len({(u['table'],u['id']) for u in plan['updates']})
assert len([i for i in plan['inserts'] if i['table']=='app.RiderClientAssignments']) == 3
assert len([i for i in plan['inserts'] if i['table']=='app.PlatformAccountRegistrations']) == 3
plan['expectedVersions'] = [dict(id=x['Id'], rowVersion=x['RowVersion'], module=x['ModuleKey']) for x in db['versions'] if x['ModuleKey'] in ['ninja','platform-operations','workforce']]
plan['guards'] = []
source_account_ids = {m['accountId'] for m in plan['mappings']}
source_employee_ids = {m['employeeId'] for m in plan['mappings']} | {owner['Id']} | {i['values']['RegisteredEmployeeId'] for i in plan['inserts'] if i['table']=='app.PlatformAccountRegistrations'}
source_rider_ids = {m['riderId'] for m in plan['mappings']}
current_account_ids = {m['accountId'] for m in plan['mappings'] if m['toDate'] is None}
for table, records, ids in [
    ('app.PlatformRiderAccounts', db['accounts'], source_account_ids),
    ('app.Employees', db['employees'], source_employee_ids),
    ('app.RiderProfiles', db['riders'], source_rider_ids),
    ('app.RiderClientAssignments', db['assignments'], {x['Id'] for x in db['assignments']}),
    ('ninja.NinjaAccountDetails', db['details'], {x['Id'] for x in db['details'] if x['PlatformRiderAccountId'] in current_account_ids}),
    ('app.PlatformAccountDayUsages', db['dayUsages'], {x['Id'] for x in duplicate_usage}),
]:
    plan['guards'].extend(dict(table=table, id=x['Id'], rowVersionHex=base64.b64decode(x['RowVersion']).hex()) for x in records if x['Id'] in ids)
for record in plan['updates'] + plan['expectedVersions']:
    record['rowVersionHex'] = base64.b64decode(record['rowVersion']).hex()
for record in plan['assignmentEvents'] + plan['expectedVersions']:
    record['auditId'] = uid()
    record['auditEventId'] = uid()
plan['summary'] = dict(updateRows=dict(collections.Counter(u['table'] for u in plan['updates'])),
    insertRows=dict(collections.Counter(u['table'] for u in plan['inserts'])),
    changedFields=dict(collections.Counter(f'{u["table"]}.{k}' for u in plan['updates'] for k in u['values'])),
    sourceRows=229, expectedVisibleAssignments=229, expectedCurrentAssignments=151,
    assignmentEvents=len(plan['assignmentEvents']), passwordsModified=False, riderTypesModified=False)
(OUT/'approved-plan.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(plan['summary'],ensure_ascii=False,indent=2))
