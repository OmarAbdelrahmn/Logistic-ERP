import csv
import re
from collections import Counter
from pathlib import Path

import openpyxl


ROOT = Path('tmp/keeta_import')
SOURCE = Path(r'C:\Users\omarf\Downloads\Keeta.xlsx')
SPONSORS = {
    'EXPRESS GATE Company': '019c18d5-62e1-7000-8000-000000000042',
    'AlBawwabah AlMuqbilah': '019c18d5-62e1-7000-8000-000000000041',
    'Albawaaba Trading EST': '019c18d5-62e1-7000-8000-000000000040',
}
CITIES = {
    'Jeddah': '019c18d5-62e1-7000-8000-000000000003',
    'Riyad': '019c18d5-62e1-7000-8000-000000000005',
}


def read_csv(name):
    with (ROOT / f'{name}.csv').open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))


def normalized(value):
    value = str(value or '').replace('أ', 'ا').replace('إ', 'ا').replace('آ', 'ا')
    return re.sub(r'[^0-9A-Za-z\u0621-\u064a]', '', value).casefold()


employees = {row['IqamaNo']: row for row in read_csv('employees') if row['IsDeleted'] == 'False'}
vehicles = {normalized(row['SerialNumber']): row for row in read_csv('vehicles')
            if normalized(row['SerialNumber']) and row['IsDeleted'] == 'False'}
source = list(openpyxl.load_workbook(SOURCE, read_only=True, data_only=True).active.values)[1:]
rows = []
reasons = Counter()
warning_counts = Counter()

for sheet_row, row in enumerate(source, start=2):
    external_id, _, first, last, iqama, _, _, vehicle_type, _, review, _, _, settlement, serial, plate, city, sponsor = row
    external_id, iqama = str(external_id).strip(), str(iqama).strip()
    serial, plate = str(serial).strip(), str(plate).strip()
    assert iqama in employees and sponsor in SPONSORS and city in CITIES
    vehicle = vehicles.get(normalized(serial)) if serial != '0' else None
    reason = ''
    if review != 'مقبول':
        reason = 'review_not_accepted'
    elif vehicle is None:
        reason = 'vehicle_serial_missing'
    elif normalized(plate) != normalized(vehicle['PlateNumberAr']):
        reason = 'vehicle_plate_conflict'
    elif vehicle['VehicleType'] != ('2' if vehicle_type == 'سيارة' else '1'):
        reason = 'vehicle_type_conflict'
    else:
        if vehicle['SponsorId'].casefold() != SPONSORS[sponsor]:
            warning_counts['vehicle_sponsor_mismatch'] += 1
        if vehicle['OperatingCityId'].casefold() != CITIES[city]:
            warning_counts['vehicle_city_mismatch'] += 1
        if vehicle['CurrentOperationalStatus'] not in ('1', '2'):
            warning_counts['vehicle_operational_status'] += 1
    reasons[reason or 'assign'] += 1
    rows.append({
        'SheetRow': sheet_row,
        'ExternalId': external_id,
        'OwnerIqama': iqama,
        'OwnerNameEn': f'{first} {last}'.strip(),
        'OperatingCityId': CITIES[city],
        'SponsorId': SPONSORS[sponsor],
        'ReviewStatus': str(review),
        'ServiceStatus': str(row[8]),
        'SettlementMode': str(settlement),
        'VehicleType': str(vehicle_type),
        'VehicleSerial': serial,
        'VehiclePlate': plate,
        'VehicleId': vehicle['Id'] if vehicle and not reason else '',
        'ExpectedVehiclePlate': vehicle['PlateNumberAr'] if vehicle and not reason else '',
        'AssignmentHoldReason': reason,
    })

assert len(rows) == 259
assert len({r['ExternalId'] for r in rows}) == 259
assert len({r['OwnerIqama'] for r in rows}) == 259
with (ROOT / 'staging.csv').open('w', encoding='utf-8-sig', newline='') as stream:
    writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
    writer.writeheader()
    writer.writerows(rows)
print('accounts', len(rows), 'assignment_decisions', dict(reasons), 'warnings', dict(warning_counts))
