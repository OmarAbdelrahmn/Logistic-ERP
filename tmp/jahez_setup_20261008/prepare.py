import argparse, collections, csv, datetime as dt, json, pathlib, uuid
from decimal import Decimal, ROUND_HALF_UP

root = pathlib.Path(__file__).parent
parser=argparse.ArgumentParser()
parser.add_argument('--additional',action='store_true')
args=parser.parse_args()
out = pathlib.Path('outputs/jahez-setup-20261008')
if args.additional: out=out/'additional'
source = json.loads((root/'source.json').read_text(encoding='utf-8'))
def load(name): return json.loads((root/f'live_{name}.json').read_text(encoding='utf-8') or '[]')
accounts = {a['ExternalAccountId']:a for a in load('accounts')}
riders = {r['IqamaNo']:r for r in load('riders')}
today = dt.date(2026,10,8)
cutoff = today - dt.timedelta(days=1)
corrections=[]; pending=[]; staged=[]
previous=json.loads((root/'plan.json').read_text(encoding='utf-8'))['rows'] if args.additional else []
applied={r['ExternalId'] for r in previous}
live_handovers={h['Id'].lower():h for h in load('handovers')}
if args.additional:
    for r in previous:
        assert r['HandoverId'].lower() in live_handovers,'Prior import missing from database'
iqama_counts = collections.Counter(str(r['iqama']) for r in source['rows'] if r['iqama'] and r['account'] not in applied)
active=collections.Counter(a['RiderProfileId'] for a in load('assignments') if a['EffectiveTo'] is None)
used_slots=collections.defaultdict(set)
for assignment in load('assignments'):
    if assignment['EffectiveTo'] is None: used_slots[assignment['RiderProfileId']].add(assignment['RiderAccountSlot'])
def normalized_date(raw,row,field):
    if raw is None: return None
    text=str(raw)
    # Correct literal extra-zero typos without changing an ambiguous year.
    fixed=text.replace('-0-2026','-2026').replace('/20026','/2026')
    value=None
    for fmt in ('%Y-%m-%dT%H:%M:%S','%d-%m-%Y','%d/%m/%Y'):
        try: value=dt.datetime.strptime(fixed,fmt).date();break
        except ValueError: pass
    if value and value>today and value.day<=12:
        swapped=dt.date(value.year,value.day,value.month)
        if swapped<=today:value=swapped
    if value and str(raw)!=value.isoformat()+'T00:00:00' and str(raw)!=value.isoformat():
        corrections.append({'row':row,'field':field,'original':str(raw),'normalized':value.isoformat()})
    return value
def amount(raw):
    return Decimal(str(raw or 0)).quantize(Decimal('0.000001'),rounding=ROUND_HALF_UP)
for r in source['rows']:
    if not r['iqama']:continue
    if r['account'] in applied:continue
    errors=[]
    a=accounts.get(r['account']); e=riders.get(str(r['iqama']))
    start=normalized_date(r['started'],r['row'],'handover')
    last=normalized_date(r['last_settlement'],r['row'],'last_settlement')
    if not a:errors.append('Account not found')
    elif a['Status']!=1:errors.append('Account is not available (status '+str(a['Status'])+')')
    if not e:errors.append('Rider Iqama not found')
    elif not e['RiderId'] or e['RiderIsDeleted'] or e['Status']!=3:errors.append('Rider is not active')
    if e and active[e['RiderId']]+iqama_counts[str(r['iqama'])]>2:errors.append('Rider exceeds two-account limit')
    if not start:errors.append('Missing or invalid handover date: '+str(r['started']))
    elif start>today:errors.append('Handover still future after approved day/month correction: '+str(start))
    if r['last_settlement'] is not None and not last:errors.append('Invalid settlement date')
    elif last and last>today:errors.append('Settlement date still future')
    anchor_overridden=False
    if start and last and last<start:
        if args.additional and start<=today:
            corrections.append({'row':r['row'],'field':'last_settlement_anchor','original':last.isoformat(),'normalized':start.isoformat()})
            last=start
            anchor_overridden=True
        else:errors.append('Last settlement precedes handover after date correction')
    if r['as_of']!='2026-10-08T00:00:00':errors.append('Different balance cutoff')
    if errors:
        pending.append({'row':r['row'],'account':r['account'],'iqama':r['iqama'],'start':str(start),'last_settlement':str(last),'reason':' | '.join(errors)})
        continue
    # Existing Jahez report convention: negative source balance is a receivable.
    debt=-amount(r['debt']);fee=amount(r['fees'])
    anchor=last or start
    commission=Decimal(max(0,(cutoff-anchor).days)*15)
    if fee<0:raise ValueError('Negative opening fee')
    # Do not accrue new commission for the handover day.
    financial_start=max(today,start+dt.timedelta(days=1))
    def new_id():return str(uuid.uuid4())
    staged.append({'SheetRow':r['row'],'ExternalId':r['account'],'Iqama':str(r['iqama']),
        'AccountId':a['Id'],'AccountRowVersion':a['RowVersion'],'RiderId':e['RiderId'],'EmployeeId':e['EmployeeId'],
        'ExpectedActiveCount':active[e['RiderId']],
        'StartedOn':start.isoformat(),'LastSettlementOn':last.isoformat() if last else None,
        'FinancialStartOn':financial_start.isoformat(),'CommissionThrough':cutoff.isoformat(),
        'OpeningDebt':str(debt),'OpeningFees':str(fee),'OpeningCommission':str(commission),
        'AssignmentId':new_id(),'HandoverId':new_id(),'FeeId':new_id(),'EventId':new_id(),
        'FeeLedgerId':new_id(),'DebtLedgerId':new_id(),'CommissionLedgerId':new_id(),
        'Evidence':json.dumps({'file':source['file'].split('\\')[-1],'sha256':source['sha256'],'sheet':source['sheet'],
            'row':r['row'],'sourceDebt':str(r['debt']),'debtConvention':'negative_source_is_receivable',
            'sourceStart':r['started'],'sourceLastSettlement':r['last_settlement'],'lastSettlementOn':str(last),
            'openingThrough':str(cutoff),'dailyRate':15,'commissionDays':max(0,(cutoff-anchor).days),
            'handoverUsedAsSettlementAnchor':anchor_overridden,
            'blankAmountsAreZero':True},ensure_ascii=False)})
staged.sort(key=lambda r:(r['RiderId'],r['StartedOn'],r['ExternalId']))
for r in staged:
    r['Slot']=next(slot for slot in (1,2) if slot not in used_slots[r['RiderId']])
    used_slots[r['RiderId']].add(r['Slot'])
out.mkdir(exist_ok=True,parents=True)
def save_csv(name,rows):
    with (out/name).open('w',encoding='utf-8-sig',newline='') as f:
        if rows:
            w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
save_csv('pending-rows.csv',pending)
save_csv('date-corrections.csv',corrections)
(root/('plan-additional.json' if args.additional else 'plan.json')).write_text(json.dumps({'sourceHash':source['sha256'],'rows':staged},ensure_ascii=False,indent=2),encoding='utf-8')
summary={'ready':len(staged),'pending':len(pending),'unassigned':sum(not r['iqama'] for r in source['rows']),
    'previouslyApplied':len(previous),
    'openingDebt':str(sum(Decimal(r['OpeningDebt']) for r in staged)),
    'openingFees':str(sum(Decimal(r['OpeningFees']) for r in staged)),
    'openingCommission':str(sum(Decimal(r['OpeningCommission']) for r in staged)),
    'pendingReasons':dict(collections.Counter(p['reason'] for p in pending))}
(out/'plan-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False))
