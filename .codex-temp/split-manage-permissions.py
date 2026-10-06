from pathlib import Path
import re,json
root=Path('.')
p=root/'src/LogisticsERP.Application/Authorization/PermissionKeys.cs'
s=p.read_text(encoding='utf-8-sig'); group=None; maps={}
for line in s.splitlines():
 m=re.search(r'public static class (\w+)',line)
 if m:group=m[1]
 m=re.search(r'public const string (\w*Manage) = "([^"]+)";',line)
 if m:
  name,key=m.groups(); base=name[:-6]; ref=f'PermissionKeys.{group}.{name}'
  maps[ref]=dict(ref=ref,group=group,base=base,old=key,prefix=key[:-7])
# Only these families lacked a read permission but already protected GET endpoints using manage.
read_refs={'PermissionKeys.Security.SupportAccessManage','PermissionKeys.Inventory.ReceiptsManage','PermissionKeys.Jahez.ImportsManage'}
for ref,m in maps.items():
 name=m['base']+'Manage'; declaration=f'public const string {name} = "{m["old"]}";'
 actions=['Create','Update','Delete']+(['Read'] if ref in read_refs else [])
 s=s.replace(declaration,'\n        '.join(f'public const string {m["base"]+a} = "{m["prefix"]}.{a.lower()}";' for a in actions))
 s=s.replace(f'{m["group"]}.{name},','\n        '.join(f'{m["group"]}.{m["base"]+a},' for a in actions))
 # final entry has no comma
 s=s.replace(f'{m["group"]}.{name}\n', ',\n        '.join(f'{m["group"]}.{m["base"]+a}' for a in actions)+'\n')
p.write_text(s,encoding='utf-8')
# Seed definitions keep existing IDs for create; append IDs for update/delete/read.
p=root/'src/LogisticsERP.Infrastructure/Persistence/SeedData/PermissionSeedCatalog.cs';s=p.read_text(encoding='utf-8-sig');added=[];seq=200
for ref,m in maps.items():
 line=next(x for x in s.splitlines() if ref in x)
 parts=re.match(r'\s*Create\((\d+), ([^,]+), "([^"]+)", "([^"]+)", "([^"]+)", "([^"]+)", "([^"]+)"(.*)\),',line).groups()
 _,_,cat,ar,en,_,_,flags=parts
 ar=ar.removeprefix('إدارة ');en=en.removeprefix('Manage ')
 for a in ['Create','Update','Delete']+(['Read'] if ref in read_refs else []):
  action_ar={'Create':'إنشاء','Update':'تعديل','Delete':'حذف','Read':'عرض'}[a]
  new=f'        Create({parts[0] if a=="Create" else seq}, PermissionKeys.{m["group"]}.{m["base"]+a}, "{cat}", "{action_ar} {ar}", "{a if a!="Update" else "Edit"} {en}", "{action_ar} {ar} ضمن النطاق المصرح به.", "{a} {en} within the authorized scope."{flags}),'
  if a=='Create':s=s.replace(line,new)
  else:added.append(new);seq+=1
s=s.replace('    ];','\n'.join(added)+'\n    ];',1);p.write_text(s,encoding='utf-8')
# Update endpoints by the operation, not just the HTTP verb.
rows=json.loads((root/'.codex-temp/manage-inventory.json').read_text());changes=[]
read={'Get':'Read','GetSuppliers':'Read','GetReceipts':'Read','GetReceipt':'Read','DownloadReceiptFile':'Read','ImportPreview':'Read','Imports':'Read','DownloadImportFile':'Read'}
create={'AssignResident','AssignSupervisor','AssignEmployee','AssignRider','AssignByIqama','Assign','AssignAccount','Take','Approve','Registration','Insurance','Inspection','OperationCard','Handover','Earnings','Payment','Adjustment','Adopt','Upload','PostReceipt','PostSupplierReturn','PostPartSale','PostCustomerLaborCharge','PostMechanicLaborPayment','PostOtherFinancialEntry','PostCustomerPayment','UploadFile','CreateVersion'}
delete={'Archive','ArchiveCompany','ArchivePlan','ArchivePolicy','ArchiveCard','ArchiveHealthCard','ArchiveHearing','ArchiveFile','ArchiveItem','RequestCancellation','RemoveOccupant','DeleteFloor','DeleteEquipment','DeleteExternalOccupant','DeletePendingOccupant','CloseResidence','CloseSupervisor','CloseAssignment','CloseLeaseAgreement','Release','Return','ReturnWithConditionReport'}
for f in (root/'src/LogisticsERP.Api/Controllers').glob('*.cs'):
 s=f.read_text(encoding='utf-8-sig');repls=[]
 for row in [r for r in rows if Path(r['file'])==f]:
  method,ref=row['method'],row['ref'];m=maps[ref]
  action='Create' if method.startswith('Create') or method in create else 'Delete' if method.startswith('Delete') or method in delete else read.get(method,'Update')
  if f.name=='UsersController.cs' and method=='Create':action='Create'
  if f.name=='JahezController.cs' and method=='Close':action='Delete'
  new=f'PermissionKeys.{m["group"]}.{m["base"]+action}'
  pos=sum(len(x) for x in s.splitlines(keepends=True)[:row['line']-1]);line_end=s.index('\n',pos) if '\n' in s[pos:] else len(s)
  line=s[pos:line_end];assert ref in line,(f,row)
  extra=[]
  if method in {'ReplaceRoles','ReplaceRolePermissions','ReplacePermissions','Switch','AcceptSwitch'}:
   extra=[f'PermissionKeys.{m["group"]}.{m["base"]+a}' for a in ['Create','Delete']]
  newline=line.replace(ref,new)+''.join('\n    [RequirePermission('+x+')]' for x in extra)
  repls.append((pos,line_end,newline));changes.append(dict(**row,new=new,additional=extra))
 for start,end,newline in reversed(repls):s=s[:start]+newline+s[end:]
 if repls:f.write_text(s,encoding='utf-8')
# Service permission checks follow their callers. Upserts resolve create/update from the requested record ID.
service_actions={'ArchiveSupplierAsync':'Delete','CreateCardAsync':'Create','SetSponsorAsync':'Update','SetCityAsync':'Update','AssignRiderAsync':'Update','StopRiderAsync':'Delete','CorrectIdentityAsync':'Create','TransitionToPublicTransportAsync':'Create','ChangeAdministrativeStatusAsync':'Update','RecordOdometerAsync':'Create','ExecuteTakeAsync':'Create','ReturnAsync':'Delete','SwitchAsync':'Update','RenewPermissionAsync':'Update','AttachPromissoryFilesAsync':'Update','RenewRegistrationAsync':'Create','RenewInsuranceAsync':'Create','RenewInspectionAsync':'Create','RenewOperationCardAsync':'Create','CreateIssueAsync':'Create','ActOnIssueAsync':'Update','ResolveIssueAsync':'Update','ApproveAsync':'Create','CloseAsync':'Delete','AcceptSwitchAsync':'Update','CreateLeaseAgreementAsync':'Create','CloseLeaseAgreementAsync':'Delete','HandoverAsync':'Create','AdoptLegacyAsync':'Create','RecordEarningsAsync':'Create','PayAsync':'Create','AdjustAsync':'Create','GetImportBatchesAsync':'Read','UploadAsync':'Create','PreviewImportAsync':'Read','CommitImportAsync':'Update','GetImportFileAsync':'Read'}
for f in (root/'src/LogisticsERP.Infrastructure').rglob('*.cs'):
 if 'Migrations' in f.parts or 'SeedData' in f.parts or f.name=='SupportAccessService.cs':continue
 s=f.read_text(encoding='utf-8-sig');repls=[]
 for row in [r for r in rows if Path(r['file'])==f]:
  ref=row['ref'];m=maps[ref];method=row['method']
  if method.startswith('Upsert') and method!='UpsertManualAsync':new=f'(id.HasValue ? PermissionKeys.{m["group"]}.{m["base"]}Update : PermissionKeys.{m["group"]}.{m["base"]}Create)'
  elif method=='UpsertManualAsync':new=f'PermissionKeys.Fleet.DailyDistancesUpdate' # handled separately
  else:
   assert method in service_actions,(f,method)
   a=service_actions[method]
   if method=='RecordOdometerAsync' and 'Corrections' in ref:a='Create'
   new=f'PermissionKeys.{m["group"]}.{m["base"]+a}'
  pos=sum(len(x) for x in s.splitlines(keepends=True)[:row['line']-1]);start=s.index(ref,pos);repls.append((start,start+len(ref),new))
 for start,end,new in sorted(repls,reverse=True):s=s[:start]+new+s[end:]
 if repls:f.write_text(s,encoding='utf-8')
# Preserve historical role-grant IDs, then append the newly split action grants.
p=root/'src/LogisticsERP.Infrastructure/Identity/SeedData/AuthorizationSeedCatalog.cs';s=p.read_text(encoding='utf-8-sig')
for ref,m in maps.items():s=s.replace(ref,f'PermissionKeys.{m["group"]}.{m["base"]}Create')
s=s.replace('SystemAdminPermissions','OriginalSystemAdminPermissions').replace('ManagerPermissions','OriginalManagerPermissions')
insert='''    public static IReadOnlyList<string> SystemAdminPermissions { get; } = ExpandPermissions(OriginalSystemAdminPermissions);
    public static IReadOnlyList<string> ManagerPermissions { get; } = ExpandPermissions(OriginalManagerPermissions);

    private static IReadOnlyList<string> ExpandPermissions(IEnumerable<string> permissions) =>
        permissions.SelectMany(key => SplitActions.TryGetValue(key, out var actions) ? actions : new[] { key }).ToArray();

    private static readonly IReadOnlyDictionary<string, string[]> SplitActions = new Dictionary<string, string[]>
    {
'''
for ref,m in maps.items():
 actions=['Create','Update','Delete']+(['Read'] if ref in read_refs else [])
 insert+=f'        [PermissionKeys.{m["group"]}.{m["base"]}Create] = ['+', '.join(f'PermissionKeys.{m["group"]}.{m["base"]+a}' for a in actions)+'],\n'
insert+='    };\n\n'
# Field ordering: dictionary must initialize before expanded properties.
marker='    public static IReadOnlyList<RolePermissionSeed> RolePermissions';s=s.replace(marker,insert+marker)
s=s.replace('        return seeds;','''        foreach (var grant in seeds.ToArray())
        {
            if (SplitActions.TryGetValue(grant.PermissionKey, out var actions))
                AddRolePermissions(seeds, grant.RoleId, actions.Skip(1), ref sequence);
        }
        return seeds;''')
# Move dictionary ahead of properties initialization.
s=s.replace('    public static IReadOnlyList<string> SystemAdminPermissions { get; } = ExpandPermissions(OriginalSystemAdminPermissions);\n    public static IReadOnlyList<string> ManagerPermissions { get; } = ExpandPermissions(OriginalManagerPermissions);\n\n','')
s=s.replace(marker,'    public static IReadOnlyList<string> SystemAdminPermissions { get; } = ExpandPermissions(OriginalSystemAdminPermissions);\n    public static IReadOnlyList<string> ManagerPermissions { get; } = ExpandPermissions(OriginalManagerPermissions);\n\n'+marker)
p.write_text(s,encoding='utf-8')
(root/'.codex-temp/manage-map.json').write_text(json.dumps(maps,indent=2));(root/'.codex-temp/manage-endpoint-changes.json').write_text(json.dumps(changes,indent=2))
print('Split',len(maps),'families; changed',len(changes),'controller permission checks')
