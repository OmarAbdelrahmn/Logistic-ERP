import json,pathlib,csv,sys,collections,xml.etree.ElementTree as ET
out=pathlib.Path(sys.argv[1]);plan=json.loads((out/'plan.json').read_text(encoding='utf-8-sig'))
base=pathlib.Path('tmp/jahez-db-20261017');read=lambda n:json.loads((base/(n+'.json')).read_text(encoding='utf-8-sig'))
employees={x['Id']:x for x in read('employees')};vehicles={x['Id']:x for x in read('vehicles')};sponsors={x['Id']:x for x in read('sponsors')};riders={x['Id']:x for x in read('riders')}
accounts=plan['accounts'];oldAccounts={x['Id']:x for x in read('accounts')}
status={1:'Available',2:'Assigned',3:'Suspended',4:'Retired',5:'Archived'}
reasonText={'RiderAccountCapacityConflict':'Exceeds the global two-account rider limit; account selection needed.',
 'InactiveAccountHasListedRider':'User confirmed: keep the account Suspended and leave the listed rider assignment pending.',
 'ExistingAssignmentHasDifferentRiderAndFinancialRecords':'Current rider differs and existing fee receipts/settlement requests are linked. Current assignment and financial history preserved.',
 'RiderMissingOrAmbiguous':'Source rider is missing or invalid (#N/A).','MissingDate':'No handover/last clearance date supplied.'}
def csvwrite(name,rows):
 if not rows:return
 with (out/name).open('w',newline='',encoding='utf-8-sig') as f:
  w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
roster=[];vehicleIssues=[];capacity=collections.defaultdict(list)
for r in accounts:
 e=employees[r['ownerId']];v=vehicles.get(r.get('vehicleId'));old=oldAccounts.get(r['accountId'])
 roster.append(dict(sourceRow=r['row'],account=r['account'],accountAction='Created' if r['newAccount'] else 'Updated',ownerIqama=r['ownerIqama'],ownerName=e['FullNameAr'],dashboardSponsor70=r['sponsorNumber'],dashboardSponsorName=sponsors[r['sponsorId']]['RegistryNameAr'],ownerHrSponsor70=sponsors.get(e.get('SponsorId'),{}).get('EmployerIdentityNumber',''),ownerHrSponsorDiffers=e.get('SponsorId')!=r['sponsorId'],previousStatus=status.get(old.get('Status')) if old else '',appliedStatus=status[r['desiredStatus']],sourceVehicleSerial=r['serial'],vehicleResult='Linked' if v else ('Serial not found' if r['serial'] else 'No serial supplied'),sourceRiderIqama=r['riderIqama'],riderResult=r['assignmentResult'],handoverAndLastClearanceDate=r['date'] if r['assignmentResult']=='Created' else '',suppliedDate=r['date'],dateCorrected=r['account'] in plan['dateCorrections'],pendingReason=' '.join(reasonText.get(x,x) for x in r['pendingReasons'])))
 if v:
  reasons=[]
  if v.get('SponsorId')!=r['sponsorId']:reasons.append('Sponsor mismatch')
  if v.get('OperatingCityId')!=r['cityId']:reasons.append('Operating city mismatch')
  if r['desiredStatus'] not in [1,2]:reasons.append('Suspended account linked for future use')
  if v['CurrentOperationalStatus'] not in [1,2]:reasons.append('Vehicle operational status is not Available/Assigned')
  capacity[(v['Id'],r['cityId'])].append(r)
  if reasons:vehicleIssues.append(dict(account=r['account'],serial=r['serial'],issues='; '.join(reasons),accountSponsor70=r['sponsorNumber'],vehicleSponsor70=sponsors.get(v.get('SponsorId'),{}).get('EmployerIdentityNumber',''),accountCityId=r['cityId'],vehicleCityId=v.get('OperatingCityId','')))
caps=[]
for (vid,city),rr in capacity.items():
 maximum={1:3,2:2}.get(vehicles[vid]['VehicleType'])
 if maximum and len(rr)>maximum:caps.append(dict(serial=vehicles[vid].get('SerialNumber'),cityId=city,linkedAccountCount=len(rr),capacity=maximum,accounts=', '.join(r['account'] for r in rr)))
csvwrite('account-results.csv',roster);csvwrite('vehicle-link-warnings.csv',vehicleIssues);csvwrite('vehicle-capacity-warnings.csv',caps)
csvwrite('pending-rider-assignments.csv',[dict(row=x['row'],account=x['account'],riderIqama=x['riderIqama'],sourceDate=x['date'],reason=' '.join(reasonText.get(r,r) for r in x['reasons'])) for x in plan['pendingAssignments']])
csvwrite('owner-sponsor-differences.csv',[r for r in roster if r['ownerHrSponsorDiffers']])
csvwrite('created-owner-rider-profiles.csv',[dict(ownerIqama=x['iqama'],ownerName=employees[x['employeeId']]['FullNameAr'],riderProfileId=x['id'],employeeStatus=employees[x['employeeId']]['Status']) for x in plan['profiles']])
s=plan['summary'];checks=ET.parse(out/'post-commit-verification.xml').getroot().findall('Checks');assert checks and all(x.findtext('Mismatches')=='0' for x in checks)
extra=ET.parse(out/'final-owner-and-date-verification.xml').getroot().findall('Checks');assert len(extra)==2 and all(x.findtext('Mismatches')=='0' for x in extra)
lines=['# Jahez October roster update — 17 October 2026','',
'Applied to the hosted application database `db67927` and verified using a fresh connection. All seven reconciliation checks and two additional owner/date checks returned zero mismatches. Every employee dashboard owner now has a rider profile, and all nine date corrections were independently verified. Before-image backups and the exact source workbook are retained in this folder.','',
'| Change | Result |','| --- | ---: |']
for label,key in [('Workbook data rows','sourceRows'),('Unique source accounts','uniqueAccounts'),('Identical duplicate rows ignored','duplicatesIgnored'),('Existing account records reconciled','existingAccountsUpdated'),('New accounts created','newAccounts'),('Employee-owner rider profiles created','ownerProfilesCreated'),('Account sponsors corrected by 70-number','accountSponsorChanges'),('Inactive accounts set to Suspended','suspended'),('New rider assignments with both dates set','newRiderAssignments'),('Existing assignments preserved','existingRiderAssignmentsPreserved'),('Rider entries pending resolution','pendingRiderAssignments'),('Vehicle-to-account links created','vehicleLinksCreated'),('Owner HR sponsor differences reported','ownerHrSponsorDifferences')]:lines.append(f'| {label} | {s[key]} |')
lines+=['','The source covers 380 accounts. Two missing accounts (405057 and 405009) were created against their exact owner Iqamas and dashboard sponsors, using the owners’ existing operating city (also matching the listed vehicle city). Existing accounts 472477 and 472847 are absent from the source and were preserved, leaving 382 Jahez master accounts.','',
f"Owners were matched by Iqama and existing owner links were validated. {s['ownerProfilesCreated']} employee owners received new rider profiles without changing their employment classification or HR status. One additional missing profile was created by another operation during preparation and was reused. Account sponsor IDs were resolved through the full `Sponsor.EmployerIdentityNumber` values 7034861059 and 7015658094. The 87 differences between owner HR sponsor and dashboard sponsor are recorded in `owner-sponsor-differences.csv`; the HR sponsor records were preserved.",'',
'Imported rider `EffectiveFrom`, session `StartedAtUtc` and `LastSettlementPaymentAtUtc` use the same source/corrected date at midnight Riyadh time. Commission starts on the following day. No historical payment receipt, account fee, opening balance, ledger entry or settlement operation was created. Existing financial records were compared before commit and preserved.','',
'The source’s Active status maps to Assigned when an assignment exists, otherwise Available. Inactive maps to Suspended for future reactivation. The complete result for each account is in `account-results.csv`.','',
'## Date corrections supplied in chat','', '| Account | Handover and last تصفية |','| --- | --- |']
for k,v in plan['dateCorrections'].items():lines.append(f'| {k} | {v} |')
lines+=['','## Pending rider entries','', 'Three inactive accounts are intentionally held Suspended at the user’s instruction. The other ten entries need account selection, corrected rider data or reconciliation with existing payment history.','', '| Account | Listed rider Iqama | Source date | Reason |','| --- | --- | --- | --- |']
for x in plan['pendingAssignments']:lines.append(f"| {x['account']} | {x['riderIqama']} | {x['date'] or 'Missing'} | {' '.join(reasonText.get(r,r) for r in x['reasons'])} |")
lines+=['','Capacity is shared across platforms. Iqama 2501676890 has three source Jahez accounts (476885, 491306, 458167); select two. Iqama 2215204666 already has one Chefz account, so only one of Jahez 420509/459049 fits. Iqama 2462687274 already has two Chefz accounts, so Jahez 456420 remains pending. All other-platform assignments were preserved.','',
'For accounts 399990, 404654 and 404719, current riders differ from the sheet and current sessions already have account-fee receipts and unexecuted settlement requests. Their assignments, sessions, dates and payments were retained. Resolve those records before replacing the riders; this import did not reattribute money to another person.','',
'## Vehicle matching and warnings','',
'Vehicle-to-platform-account links were created using exact serial-number matches. These are direct account links, independent of physical rider-to-vehicle handover records. Their effective date is the import time because the sheet supplies no historical vehicle-link date. Existing physical vehicle assignments were preserved.','',
'Serial 720438120 was not found (accounts 472969 and 472484). Serial 400604220 was not found (account 472526). Accounts 405144, 405059 and 408243 have no serial supplied. No vehicle was guessed or created.','',
f'The existing fleet policy permits links with warnings. `vehicle-link-warnings.csv` lists {len(vehicleIssues)} links with sponsor, city, account-status or vehicle-status warnings. `vehicle-capacity-warnings.csv` lists {len(caps)} vehicle/city groups exceeding the configured capacity (three motorcycle accounts or two car accounts per platform and city). These warnings do not change sponsor ownership, city, odometer or physical vehicle handover history.','',
'## Recovery and verification','',
'The import was first executed and verified in a transaction that rolled back. It was then committed after another verification. A fresh database connection confirmed owner/sponsor/status matches, created profiles, actual rider Iqamas, assignment dates, both clearance dates, vehicle serial links, rider capacity, absence of imported financial charges/receipts and the audit record.','',
'Every pre-existing rider profile, rider assignment, account session, vehicle-account assignment and all Jahez financial/import tables were preserved byte-for-byte. Employees, sponsors, vehicles, physical vehicle assignments, credentials, platform definitions, contracts and accounts outside the source were also checked for preservation inside the transaction.','',
'Before images are saved as `*-before.xml`; `source.xlsx`, `plan.json`, `executed.sql`, `verify.sql`, `transaction-verification.xml` and `post-commit-verification.xml` identify the exact input, executed changes and checks. A unique audit correlation ID blocks accidental replay. This import adds records and updates only source account sponsor/status metadata; no rows were deleted. Recover only the affected records from the backups after reviewing subsequent live activity.','']
(out/'report.md').write_text('\n'.join(lines),encoding='utf-8')
print(json.dumps(dict(report=str(out/'report.md'),vehicleWarnings=len(vehicleIssues),vehicleCapacityGroups=len(caps),summary=s),ensure_ascii=False,indent=2))
