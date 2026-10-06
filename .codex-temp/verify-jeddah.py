import json
from collections import Counter
from pathlib import Path

root = Path(__file__).parent
def read(name):
    return json.loads((root / name).read_text(encoding='utf-8-sig'))
source = read('jeddah-reconciliation.json')
before, after = read('jeddah-current.json'), read('jeddah-final.json')
old_details, details = read('jeddah-details.json'), read('jeddah-details-final.json')
housing_ids = {'safa':'01a0a427-51c1-7477-b93e-cee215d22268', 'SAMER':'01a0a428-5ea7-710e-8e7f-2e8550e2c8be','MECANIC':'01a0a5e0-535c-7236-afa6-3708df20f662'}
def index(rows):
    return {r['Id']:r for r in rows}
def without(row,excluded):
    return {k:v for k,v in row.items() if k not in excluded}
housing = index(after[0])
floors = index(after[1])
rooms = index(after[2])
active = [r for r in rooms.values() if not r['IsDeleted']]
assert len(active) == 27
assert len([f for f in floors.values() if not f['IsDeleted']]) == 8
for code, expected in source['summary'].items():
    actual = [r for r in active if r['HousingId']==housing_ids[code]]
    assert housing[housing_ids[code]]['Code'] == code
    assert len(actual) == expected['rooms']
    assert sum(r['Capacity'] for r in actual) == expected['capacity']
    assert sum(r['CurrentOccupancy'] for r in actual) == expected['occupants']
by_key = {(housing[r['HousingId']]['Code'],floors[r['FloorId']]['Name'],r['Name']): r for r in active}
for expected in source['rooms']:
    room = by_key[(expected['code'],expected['floor'],expected['name'])]
    assert room['Capacity'] == expected['capacity']
    target = [p for p in source['people'] if (p['code'],p['floor'],p['room']) == (expected['code'],expected['floor'],expected['room'])]
    assert room['CurrentOccupancy'] == len(target)
    for p in target:
        if p['employee']:
            matching = [x for x in details[0] if x['EmployeeId']==p['employee']['Id'] and x['RoomId']==room['Id']]
        elif p['iqama']:
            matching = [x for x in details[2] if x['IqamaNo']==p['iqama'] and x['Name']==p['name'] and x['RoomId']==room['Id']]
        else:
            matching = [x for x in details[1] if x['Name']==p['name'] and x['RoomId']==room['Id']]
        assert len(matching)==1, p
    count = sum(x['RoomId']==room['Id'] for records in details[:3] for x in records)
    assert count==len(target)
for h in before[0]:
    if h['Id'] not in housing_ids.values():
        assert housing[h['Id']]==h
new_periods = index(details[0])
for p in old_details[0]:
    assert without(p,{'HousingCode','RoomName'}) == without(new_periods[p['Id']],{'HousingCode','RoomName'})
for i in (1,2):
    new = index(details[i])
    for p in old_details[i]:
        assert new[p['Id']]==p
assert index(details[3])==index(old_details[3]), 'Employee records changed'
assert details[4]==old_details[4], 'Equipment changed'
allowed = {'FloorId','Name','Capacity','CurrentOccupancy','UpdatedAtUtc','UpdatedByUserId','RowVersion'}
for r in before[2]:
    assert without(r,allowed)==without(rooms[r['Id']],allowed), 'Existing room metadata changed outside scope'
rerun = read('jeddah-rerun.json')[-1][0]
assert all(v==0 for k,v in rerun.items() if k!='ImportStatus'), rerun
result = {'housing':3,'floors':8,'rooms':27,'capacity':215,'occupants':139,'vacancies':76,'employeeAssignments':124,'newEmployeeAssignments':119,'pendingIqamas':5,'nameOnlyOccupants':10,'originalPeriodsPreserved':len(old_details[0]),'originalRoomIdsPreserved':len(before[2]),'otherHousingAndEmployeesUnchanged':True,'rerunWrites':0,'summary':source['summary']}
(root / 'jeddah-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
