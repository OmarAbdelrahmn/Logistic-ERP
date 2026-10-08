import csv
from pathlib import Path


ROOT = Path('tmp/keeta_import')
OUTPUT = Path('outputs/keeta-unassigned-20261007.md')


def read(name):
    with (ROOT / f'{name}.csv').open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))


vehicles = {r['Id']: r for r in read('vehicles')}
by_serial = {r['SerialNumber']: r for r in read('vehicles') if r['SerialNumber']}
cities = {
    '019c18d5-62e1-7000-8000-000000000003': 'Jeddah',
    '019c18d5-62e1-7000-8000-000000000005': 'Riyadh',
}
sponsors = {
    '019c18d5-62e1-7000-8000-000000000042': 'EXPRESS GATE Company',
    '019c18d5-62e1-7000-8000-000000000041': 'AlBawwabah AlMuqbilah',
    '019c18d5-62e1-7000-8000-000000000040': 'Albawaaba Trading EST',
}
statuses = {
    '3': 'Problem hold', '4': 'Accident hold',
    '6': 'Out of service', '7': 'Under movement responsibility',
}
reasons = {
    'review_not_accepted': 'Review rejected or pending',
    'vehicle_plate_conflict': 'Source plate conflicts with ERP plate',
    'vehicle_serial_missing': 'No source vehicle identity',
}
held = []
for row in read('staging'):
    vehicle = vehicles.get(row['VehicleId']) or by_serial.get(row['VehicleSerial'])
    reason = reasons.get(row['AssignmentHoldReason'], '')
    if not reason and vehicle and vehicle['CurrentOperationalStatus'] not in ('1', '2'):
        reason = statuses[vehicle['CurrentOperationalStatus']]
    if reason:
        held.append((row, vehicle, reason))

assert len(held) == 38
lines = [
    '# Keeta accounts without vehicle assignments',
    '',
    'Source: `Keeta.xlsx`, worksheet `ورقة1`. All 38 accounts were created in the ERP on 7 October 2026. The row number refers to the source worksheet.',
    '',
    '| Row | Keeta ID | Iqama | Name | City | Source sponsor | Vehicle serial | Source plate | ERP plate | Reason |',
    '| ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- |',
]
for row, vehicle, reason in held:
    cells = [
        row['SheetRow'], row['ExternalId'], row['OwnerIqama'], row['OwnerNameEn'],
        cities[row['OperatingCityId']], sponsors[row['SponsorId']],
        row['VehicleSerial'], row['VehiclePlate'],
        vehicle['PlateNumberAr'] if vehicle else '—', reason,
    ]
    lines.append('| ' + ' | '.join(str(x or '—').replace('|', '\\|') for x in cells) + ' |')
lines += [
    '',
    'The two pending-review accounts also have `0` for vehicle serial and plate. They appear once in the review category. The `ERP plate` column is provided for comparison and may reflect a later vehicle registration change.',
    '',
]
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
OUTPUT.write_text('\n'.join(lines), encoding='utf-8')
print(OUTPUT, len(held))
