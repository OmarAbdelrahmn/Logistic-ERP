import csv
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path('tmp/keeta_import')


def read(name):
    with (ROOT / f'{name}.csv').open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))


source = read('staging')
employees = {r['IqamaNo']: r for r in read('employees') if r['IsDeleted'] == 'False'}
vehicles = {r['Id']: r for r in read('vehicles')}
accounts = {r['ExternalAccountId']: r for r in read('keeta_accounts')}
assignments = read('vehicle_account_assignments')
by_account = defaultdict(list)
for assignment in assignments:
    by_account[assignment['PlatformRiderAccountId']].append(assignment)

errors = []
for row in source:
    account = accounts.get(row['ExternalId'])
    if account is None:
        errors.append((row['SheetRow'], 'missing account'))
        continue
    expected = {
        'Code': 'KEETA-' + row['ExternalId'],
        'RegisteredEmployeeId': employees[row['OwnerIqama']]['Id'],
        'OperatingCityId': row['OperatingCityId'],
        'SponsorId': row['SponsorId'],
        'Status': '1' if row['ReviewStatus'] == 'مقبول' else '3',
        'IsDeleted': 'False',
    }
    for field, value in expected.items():
        if account[field].casefold() != value.casefold():
            errors.append((row['SheetRow'], field))
    expected_vehicle = row['VehicleId']
    if expected_vehicle and vehicles[expected_vehicle]['CurrentOperationalStatus'] not in ('1', '2'):
        expected_vehicle = ''
    actual = by_account.get(account['Id'], [])
    if len(actual) != (1 if expected_vehicle else 0):
        errors.append((row['SheetRow'], 'assignment count'))
    elif expected_vehicle and actual[0]['VehicleId'].casefold() != expected_vehicle.casefold():
        errors.append((row['SheetRow'], 'assignment vehicle'))

counts = Counter(row['Status'] for row in accounts.values())
print('accounts', len(accounts), 'status', dict(counts), 'assignments', len(assignments))
print('mapping errors', len(errors), errors[:10])
assert len(accounts) == 259 and len(assignments) == 221 and not errors
