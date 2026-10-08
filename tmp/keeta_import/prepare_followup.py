import csv
import re
from collections import Counter, defaultdict
from pathlib import Path

import openpyxl


ROOT = Path('tmp/keeta_import')
SOURCE = Path(r'C:\Users\omarf\Downloads\Keeta.xlsx')


def read(name):
    with (ROOT / f'{name}.csv').open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))


def norm(value):
    value = str(value or '').replace('أ', 'ا').replace('إ', 'ا').replace('آ', 'ا')
    return re.sub(r'[^0-9A-Za-z\u0621-\u064a]', '', value).casefold()


vehicles_by_serial = defaultdict(list)
for vehicle in read('vehicles'):
    if vehicle['SerialNumber'] and vehicle['IsDeleted'] == 'False':
        vehicles_by_serial[norm(vehicle['SerialNumber'])].append(vehicle)
accounts = {row['ExternalAccountId']: row for row in read('keeta_accounts')}
employees = {row['IqamaNo']: row for row in read('employees') if row['IsDeleted'] == 'False'}
existing_assignments = defaultdict(list)
for row in read('vehicle_account_assignments'):
    if row['EndedAtUtc'] == '' and row['IsDeleted'] == 'False':
        existing_assignments[row['PlatformRiderAccountId']].append(row)

source = list(openpyxl.load_workbook(SOURCE, read_only=True, data_only=True).active.values)[1:]
rows = []
counts = Counter()
for sheet_row, values in enumerate(source, 2):
    external_id = str(values[0]).strip()
    iqama = str(values[4]).strip()
    vehicle_serial = str(values[13]).strip()
    source_plate = str(values[14]).strip()
    account = accounts[external_id]
    assert account['Code'] == f'KEETA-{external_id}'
    assert iqama in employees
    assert account['RegisteredEmployeeId'].casefold() == employees[iqama]['Id'].casefold()
    settlement = str(values[12]).strip()
    assert settlement in ('Slab mode', 'Per order mode')
    model = 2 if settlement == 'Slab mode' else 1
    matches = vehicles_by_serial.get(norm(vehicle_serial), []) if vehicle_serial != '0' else []
    assert len(matches) == (0 if vehicle_serial == '0' else 1), (sheet_row, vehicle_serial)
    vehicle = matches[0] if matches else None
    if vehicle:
        assert vehicle['VehicleType'] == ('2' if values[7] == 'سيارة' else '1')
    old = existing_assignments.get(account['Id'], [])
    assert len(old) <= 1, (sheet_row, 'multiple existing assignments')
    if old:
        assert vehicle and old[0]['VehicleId'].casefold() == vehicle['Id'].casefold()
        counts['already_assigned'] += 1
    elif vehicle:
        counts['new_assignment'] += 1
        if norm(source_plate) != norm(vehicle['PlateNumberAr']):
            counts['new_plate_conflict'] += 1
    else:
        counts['no_vehicle_identity'] += 1
    counts[f'payment_{model}'] += 1
    rows.append({
        'SheetRow': sheet_row,
        'ExternalId': external_id,
        'OwnerIqama': iqama,
        'PaymentModel': model,
        'SettlementMode': settlement,
        'VehicleType': str(values[7]),
        'VehicleSerial': vehicle_serial,
        'SourcePlate': source_plate,
        'VehicleId': vehicle['Id'] if vehicle else '',
        'ExpectedVehiclePlate': vehicle['PlateNumberAr'] if vehicle else '',
        'AlreadyAssigned': 1 if old else 0,
        'PlateConflict': 1 if vehicle and norm(source_plate) != norm(vehicle['PlateNumberAr']) else 0,
    })

assert len(rows) == 259 and counts['already_assigned'] == 221
assert counts['new_assignment'] == 35 and counts['no_vehicle_identity'] == 3
assert counts['new_plate_conflict'] == 12
with (ROOT / 'followup.csv').open('w', encoding='utf-8-sig', newline='') as stream:
    writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
    writer.writeheader()
    writer.writerows(rows)
print(dict(counts))
