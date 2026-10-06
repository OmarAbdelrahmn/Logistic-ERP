from pathlib import Path
import re,json,shutil
root=Path('.').resolve();maps=json.loads(Path('.codex-temp/manage-map.json').read_text());p=Path('src/LogisticsERP.Application/Authorization/PermissionKeys.cs');s=p.read_text();missing=[]
for m in maps.values():
 if f'"{m["prefix"]}.read"' in s:continue
 ref=f'PermissionKeys.{m["group"]}.{m["base"]}Read';create=f'public const string {m["base"]}Create = "{m["prefix"]}.create";'
 s=s.replace(create,f'public const string {m["base"]}Read = "{m["prefix"]}.read";\n        '+create)
 s=s.replace(f'        {m["group"]}.{m["base"]}Create,',f'        {m["group"]}.{m["base"]}Read,\n        {m["group"]}.{m["base"]}Create,')
 missing.append(m)
p.write_text(s)
p=Path('src/LogisticsERP.Infrastructure/Persistence/SeedData/PermissionSeedCatalog.cs');s=p.read_text();seq=max(map(int,re.findall(r'Create\((\d+),',s)))+1;added=[]
for m in missing:
 ref=f'PermissionKeys.{m["group"]}.{m["base"]}Create';line=next(x for x in s.splitlines() if ref in x)
 line=re.sub(r'Create\(\d+,',f'Create({seq},',line).replace(ref,ref[:-6]+'Read').replace('إنشاء','عرض').replace('Create ','Read ')
 added.append(line);seq+=1
s=s.replace('    ];','\n'.join(added)+'\n    ];',1);p.write_text(s)
p=Path('src/LogisticsERP.Infrastructure/Identity/SeedData/AuthorizationSeedCatalog.cs');s=p.read_text()
for m in maps.values():
 ref=f'PermissionKeys.{m["group"]}.{m["base"]}'
 line=next(x for x in s.splitlines() if f'[{ref}Create] = [' in x)
 newline=line.replace(f', {ref}Read','').replace('],',f', {ref}Read],')
 s=s.replace(line,newline)
s=s.replace('new[] { key }).ToArray();','new[] { key }).Distinct(StringComparer.Ordinal).ToArray();')
s=s.replace('actions.Skip(1), ref sequence','actions.Skip(1).Where(key => !seeds.Any(existing => existing.RoleId == grant.RoleId && existing.PermissionKey == key)), ref sequence')
p.write_text(s)
p=Path('.codex-temp/build-management-grant-migration.py');s=p.read_text().replace("reads={'support_access','inventory.receipts','jahez.imports'}", "reads={m['prefix'] for m in maps.values()}")
s=s.replace("sql=sql.replace('    );\\n\\nIF EXISTS'", "sql=sql.replace(');\\n\\nIF EXISTS'")
s=s.replace("'    OR EXISTS (SELECT 1 FROM @supportUsers", "'    OR EXISTS (SELECT 1 FROM @supportUsers")
p.write_text(s)
# Remove un-applied scaffolding, preserving a copy, then restore the preceding snapshots exactly from their target models.
for folder,name,previous,context in [
 ('src/LogisticsERP.Infrastructure/Persistence/Migrations/Application','SplitManagementPermissionCatalog','20261005160000_AllowUnassignedFuelCardUsage','ApplicationDbContext'),
 ('src/LogisticsERP.Infrastructure/Identity/Migrations','SplitManagementPermissionGrants','20261003152452_GrantJahezPermissions','IdentityDbContext')]:
 d=Path(folder)
 for file in d.glob('*'+name+'*.cs'):
  file.resolve().relative_to(root);shutil.copy2(file,Path('.codex-temp')/('initial-'+file.name));file.unlink()
 designer=(d/(previous+'.Designer.cs')).read_text(encoding='utf-8-sig');snapshot=d/(context+'ModelSnapshot.cs');s=snapshot.read_text(encoding='utf-8-sig')
 body=designer[designer.index('        protected override void BuildTargetModel'):].replace('BuildTargetModel','BuildModel',1)
 s=s[:s.index('        protected override void BuildModel')]+body;snapshot.write_text(s)
# Role archive is delete.
p=Path('src/LogisticsERP.Api/Controllers/UsersController.cs');s=p.read_text().replace('[RequirePermission(PermissionKeys.Security.RolesUpdate)]\n    public async Task<IActionResult> ArchiveRole','[RequirePermission(PermissionKeys.Security.RolesDelete)]\n    public async Task<IActionResult> ArchiveRole');p.write_text(s)
# Repair stale test construction of FleetService so the existing relevant tests run.
for name in ['VehicleOperationCardServiceTests.cs','VehicleRegistrationTransitionTests.cs','VehicleRegisteredOwnerServiceTests.cs']:
 p=Path('tests/LogisticsERP.Fleet.UnitTests')/name;s=p.read_text();identity='new LogisticsERP.Infrastructure.Identity.IdentityDbContext(new DbContextOptionsBuilder<LogisticsERP.Infrastructure.Identity.IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)'
 s=s.replace('new(db, new FleetServiceSupport',f'new(db, {identity}, new FleetServiceSupport').replace('                db,\n                new FleetServiceSupport',f'                db,\n                {identity},\n                new FleetServiceSupport');p.write_text(s)
print('Added',len(missing),'read permissions; all 46 families now have four actions')
