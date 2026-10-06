from pathlib import Path
import json,re
root=Path('.')
def edit(path,old,new):
 p=root/path;s=p.read_text(encoding='utf-8-sig');assert old in s,(path,old);p.write_text(s.replace(old,new),encoding='utf-8')
# Align remaining operation-specific endpoints.
overrides={('FuelCardsController.cs','AssignRider'):'Update',('FuelCardsController.cs','StopRider'):'Delete',('VehiclesController.cs','CorrectIdentity'):'Create',('VehiclesController.cs','TransitionToPublic'):'Create',('VehiclePlatformAccountAssignmentsController.cs','Close'):'Delete'}
changes=json.loads((root/'.codex-temp/manage-endpoint-changes.json').read_text())
for r in changes:
 if (Path(r['file']).name,r['method']) not in overrides:continue
 a=overrides[(Path(r['file']).name,r['method'])];new=re.sub(r'(Create|Update|Delete)$',a,r['new'])
 p=Path(r['file']);s=p.read_text();pattern=r'\[RequirePermission\('+re.escape(r['new'])+r'\)\]((?:\s+\[[^\n]+\])*\s+public async Task<IActionResult> '+r['method']+r'\()'
 s,n=re.subn(pattern,'[RequirePermission('+new+r')]\1',s);assert n==1 or new in s,(p,r['method']);p.write_text(s);r['new']=new
(root/'.codex-temp/manage-endpoint-changes.json').write_text(json.dumps(changes,indent=2))
# Dynamic upsert: creating a manual reading on a GPS-only row is also create.
p=root/'src/LogisticsERP.Infrastructure/Fleet/VehicleDailyDistanceService.cs';s=p.read_text();old='''        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.DailyDistancesUpdate, null, cancellationToken))
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.Forbidden);
        }

''';assert old in s;s=s.replace(old,'',1)
marker='''        if (current is not null && !FleetServiceSupport.MatchesRowVersion'''
s=s.replace(marker,'''        var permission = current?.ManualOdometerReading is null
            ? PermissionKeys.Fleet.DailyDistancesCreate : PermissionKeys.Fleet.DailyDistancesUpdate;
        if (!await support.HasPermissionAsync(permission, null, cancellationToken))
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.Forbidden);

'''+marker,1);p.write_text(s)
edit('src/LogisticsERP.Api/Controllers/VehicleDailyDistancesController.cs','[RequirePermission(PermissionKeys.Fleet.DailyDistancesUpdate)]','[Microsoft.AspNetCore.Authorization.Authorize]')
# Service checks for mixed assignment operations.
p=root/'src/LogisticsERP.Infrastructure/Fleet/FleetService.cs';s=p.read_text().replace('PermissionKeys.Fleet.AssignmentsManage','PermissionKeys.Fleet.AssignmentsCreate')
s=s.replace('!await support.HasVehiclePermissionAsync(oldVehicle, PermissionKeys.Fleet.AssignmentsUpdate, cancellationToken)', '!await support.HasVehiclePermissionAsync(oldVehicle, PermissionKeys.Fleet.AssignmentsUpdate, cancellationToken) || !await support.HasVehiclePermissionAsync(oldVehicle, PermissionKeys.Fleet.AssignmentsDelete, cancellationToken)')
p.write_text(s)
p=root/'src/LogisticsERP.Infrastructure/Fleet/VehiclePlatformAccountAssignmentService.cs';s=p.read_text()
needle='if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsUpdate, null, cancellationToken))'
s=s.replace(needle,'''if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsUpdate, null, cancellationToken)
            || !await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsCreate, null, cancellationToken)
            || !await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsDelete, null, cancellationToken))''');p.write_text(s)
# Support access now uses independent keys, retaining the operator self-request/revoke rules in the service.
p=root/'src/LogisticsERP.Infrastructure/Authentication/SupportAccessService.cs';s=p.read_text()
s=s.replace('CanManageAsync(actorId, cancellationToken)', 'CanActAsync(actorId, PermissionKeys.Security.SupportAccessCreate, cancellationToken)',1)
s=s.replace('CanManageAsync(actorId, cancellationToken)', 'CanActAsync(actorId, PermissionKeys.Security.SupportAccessUpdate, cancellationToken)',1)
s=s.replace('CanManageAsync(actorId, cancellationToken)', 'CanActAsync(actorId, PermissionKeys.Security.SupportAccessDelete, cancellationToken)',1)
s=s.replace('CanManageAsync(Guid userId, CancellationToken cancellationToken)', 'CanActAsync(Guid userId, string permissionKey, CancellationToken cancellationToken)').replace('PermissionKeys.Security.SupportAccessManage,','permissionKey,');p.write_text(s)
p=root/'src/LogisticsERP.Api/Controllers/SupportAccessController.cs';s=p.read_text();s=s.replace('[HttpPost]\n    [Authorize]','[HttpPost]\n    [RequirePermission(PermissionKeys.Security.SupportAccessCreate)]').replace('[HttpPost("{id:guid}/revoke")]\n    [Authorize]','[HttpPost("{id:guid}/revoke")]\n    [RequirePermission(PermissionKeys.Security.SupportAccessDelete)]');p.write_text(s)
# Update pre-existing surface tests to assert the action now assigned to each endpoint.
for f in (root/'tests').rglob('*.cs'):
 s=f.read_text(encoding='utf-8-sig');new=[]
 for line in s.splitlines():
  for ref in re.findall(r'PermissionKeys\.\w+\.\w*Manage\b',line):
   matches=[r for r in changes if r['ref']==ref and (f"nameof({Path(r['file']).stem}.{r['method']})" in line)]
   replacement=matches[0]['new'] if len(matches)==1 else ref[:-6]+'Create'
   if 'VehicleDailyDistancesController.UpsertManual' in line: replacement=ref[:-6]+'Create'
   line=line.replace(ref,replacement)
  new.append(line)
 if '\n'.join(new)+'\n'!=s:f.write_text('\n'.join(new)+'\n',encoding='utf-8')
print('Aligned dynamic and service authorization checks')
