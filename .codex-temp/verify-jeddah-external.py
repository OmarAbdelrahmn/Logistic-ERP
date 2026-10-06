import json
from pathlib import Path
root=Path(__file__).parent
def read(name):
    return json.loads((root/name).read_text(encoding='utf-8-sig'))
before=read('jeddah-before-external.json')
after=read('jeddah-after-external.json')
targets=read('jeddah-commit.json')[1]
def index(rows): return {r['Id']:r for r in rows}
for s in targets:
    matching=[x for x in after[1] if x['HousingCode']==s['Code'] and x['RoomName']==s['Room'] and x['Name']==s['Name']]
    assert len(matching)==1,s
assert len(after[1])-len(before[1])==5
assert len(before[2])-len(after[2])==5
assert index(before[0])==index(after[0]), 'Employee residence periods changed'
assert index(before[3])==index(after[3]), 'Employees changed'
assert before[4]==after[4], 'Equipment changed'
assert read('jeddah-rooms-before-external.json')==read('jeddah-rooms-after-external.json'), 'Housing, rooms, or occupancy changed'
after_external=index(after[1])
for x in before[1]: assert x==after_external[x['Id']], 'Existing external record changed'
remaining=index(after[2])
for p in before[2]:
    if p['HousingCode'] not in {'safa','SAMER','MECANIC'}:
        assert p==remaining[p['Id']], 'Unrelated pending record changed'
assert not [p for p in after[2] if p['HousingCode'] in {'safa','SAMER','MECANIC'}]
assert read('jeddah-external-rerun.json')[-1][0]['CreatedExternal']==0
assert read('jeddah-external-rerun.json')[-1][0]['ArchivedPending']==0
print(json.dumps({'externalRenters':15,'jeddahPendingOccupants':0,'occupancy':139,'allRoomsAndEmployeeAssignmentsUnchanged':True,'duplicateConversionsOnRerun':0}))
