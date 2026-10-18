import collections, csv, datetime as dt, json, pathlib, re

root = pathlib.Path(__file__).parent
source = json.loads((root / 'source.json').read_text(encoding='utf-8'))
def load(name):
    return json.loads((root / ('live_' + name + '.json')).read_text(encoding='utf-8') or '[]')
accounts = collections.defaultdict(list)
riders = collections.defaultdict(list)
for a in load('accounts'): accounts[a['ExternalAccountId']].append(a)
for r in load('riders'): riders[r['IqamaNo']].append(r)
assignments = load('assignments')
active = collections.defaultdict(list)
for a in assignments:
    if a['EffectiveTo'] is None: active[a['RiderProfileId']].append(a)
counts = collections.Counter()
issues = []
today = dt.date(2026, 10, 8)
iqama_counts = collections.Counter(str(r['iqama']) for r in source['rows'] if r['iqama'])
def date(value):
    if value is None: return None
    for fmt in ('%Y-%m-%dT%H:%M:%S','%d-%m-%Y','%d/%m/%Y'):
        try: return dt.datetime.strptime(str(value),fmt).date()
        except ValueError: pass
    return None
for row in source['rows']:
    if not row['iqama']:
        counts['unassigned_source_rows'] += 1
        continue
    counts['assigned_source_rows'] += 1
    def issue(kind,detail):
        counts[kind] += 1
        issues.append({'sheet_row':row['row'],'account':row['account'],'iqama':row['iqama'],'issue':kind,'detail':detail})
    aa = accounts[row['account']]
    rr = riders[str(row['iqama'])]
    if len(aa) != 1: issue('account_match',str(len(aa)))
    else:
        if aa[0]['Status'] != 1: issue('account_not_available','status=' + str(aa[0]['Status']))
        if not aa[0]['SponsorId']: issue('account_missing_sponsor','')
        if aa[0]['PaymentModel'] != 1: issue('account_payment_model','model=' + str(aa[0]['PaymentModel']))
    if len(rr) != 1: issue('rider_match',str(len(rr)))
    else:
        rider = rr[0]
        if not rider['RiderId'] or rider['RiderIsDeleted']: issue('rider_profile_missing',rider['FullNameAr'])
        if rider['Status'] != 3: issue('rider_not_active','status=' + str(rider['Status']))
        if rider['RiderId'] and len(active[rider['RiderId']]) + iqama_counts[str(row['iqama'])] > 2:
            issue('rider_account_limit','source=' + str(iqama_counts[str(row['iqama'])]) + '; existing=' + str(len(active[rider['RiderId']])))
    start = date(row['started'])
    last = date(row['last_settlement'])
    if start is None: issue('invalid_handover_date',str(row['started']))
    elif start > today: issue('future_handover_date',str(start))
    if row['last_settlement'] is not None and last is None: issue('invalid_settlement_date',str(row['last_settlement']))
    elif last and last > today: issue('future_settlement_date',str(last))
    if start and last and last < start: issue('settlement_before_handover',str(last) + ' < ' + str(start))
    if row['debt'] is None: issue('blank_debt','Zero or unknown needs clarification')
    if row['fees'] is None: issue('blank_fees','Zero or unknown needs clarification')
    if isinstance(row['cached_commission'],(int,float)) and row['cached_commission'] < 0:
        issue('negative_cached_commission',str(row['cached_commission']))
    if row['as_of'] != '2026-10-08T00:00:00': issue('balance_date',str(row['as_of']))
out = pathlib.Path('outputs/jahez-setup-20261008')
out.mkdir(parents=True,exist_ok=True)
with (out/'validation-issues.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=['sheet_row','account','iqama','issue','detail'])
    writer.writeheader();writer.writerows(issues)
(out/'validation-summary.json').write_text(json.dumps(dict(counts),indent=2),encoding='utf-8')
print(json.dumps(dict(counts),ensure_ascii=False))
for k in ('account_match','account_not_available','rider_match','rider_profile_missing','rider_not_active','rider_account_limit','invalid_handover_date'):
    print(k,json.dumps([i for i in issues if i['issue']==k],ensure_ascii=False))
print('Existing assignments:',len(assignments),'Existing Jahez handovers:',len(load('handovers')))
