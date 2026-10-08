import csv
import json
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path('tmp/keeta_import')
BACKUP = Path('outputs/keeta-followup-20261008-backup')


def read(name):
    with (ROOT / f'{name}.csv').open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))


source = read('followup')
accounts = {row['ExternalAccountId']: row for row in read('keeta_accounts')}
vehicles = {row['Id']: row for row in read('vehicles')}
assignments = read('vehicle_account_assignments')
by_account = defaultdict(list)
for row in assignments:
    if row['EndedAtUtc'] == '' and row['IsDeleted'] == 'False':
        by_account[row['PlatformRiderAccountId']].append(row)

errors = []
warnings = Counter()
for row in source:
    account = accounts.get(row['ExternalId'])
    if not account:
        errors.append((row['SheetRow'], 'account missing'))
        continue
    if account['PaymentModel'] != row['PaymentModel']:
        errors.append((row['SheetRow'], 'payment model'))
    linked = by_account.get(account['Id'], [])
    if len(linked) != (1 if row['VehicleId'] else 0):
        errors.append((row['SheetRow'], 'assignment count'))
    elif linked and linked[0]['VehicleId'].casefold() != row['VehicleId'].casefold():
        errors.append((row['SheetRow'], 'vehicle mismatch'))
    if linked:
        vehicle = vehicles[row['VehicleId']]
        if vehicle['CurrentOperationalStatus'] not in ('1', '2'):
            warnings['vehicle_operational_status'] += 1
        if account['Status'] not in ('1', '2'):
            warnings['platform_account_status'] += 1
        if vehicle['SponsorId'].casefold() != account['SponsorId'].casefold():
            warnings['sponsor_mismatch'] += 1
        if vehicle['OperatingCityId'].casefold() != account['OperatingCityId'].casefold():
            warnings['city_mismatch'] += 1
        if row['PlateConflict'] == '1':
            warnings['source_plate_conflict_not_in_tracker'] += 1

old_accounts = json.loads((BACKUP / 'accounts.json').read_text(encoding='utf-8'))
old_assignments = json.loads((BACKUP / 'assignments.json').read_text(encoding='utf-8'))
assert {item['Id'].casefold() for item in old_accounts} == {item['Id'].casefold() for item in accounts.values()}
assert {item['Id'].casefold() for item in old_assignments}.issubset({item['Id'].casefold() for item in assignments})

capacity = Counter(row['VehicleId'] for row in assignments if row['EndedAtUtc'] == '' and row['IsDeleted'] == 'False')
warnings['vehicles_over_capacity'] = sum(
    count > (2 if vehicles[vehicle_id]['VehicleType'] == '2' else 3)
    for vehicle_id, count in capacity.items()
)
print('accounts', len(accounts), 'assignments', len(assignments),
      'salary', sum(a['PaymentModel'] == '2' for a in accounts.values()),
      'pay_per_order', sum(a['PaymentModel'] == '1' for a in accounts.values()))
print('errors', len(errors), errors[:10])
print('warnings', dict(warnings))
assert len(accounts) == 259 and len(assignments) == 256 and not errors
