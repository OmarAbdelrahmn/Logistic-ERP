import contextlib
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

spec = importlib.util.spec_from_file_location('carry', Path(__file__).parents[1] / 'Build-JahezTreasuryCarryforwardPlan.py')
carry = importlib.util.module_from_spec(spec)
spec.loader.exec_module(carry)


def xml(path, groups):
    root = ET.Element('NewDataSet')
    for index, rows in enumerate(groups):
        for row in rows:
            node = ET.SubElement(root, 'Table' + (str(index) if index else ''))
            for name, value in row.items():
                if value is not None:
                    ET.SubElement(node, name).text = str(value)
    ET.ElementTree(root).write(path, encoding='utf-8')


class CarryforwardPlanTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.roster = self.folder / 'roster'
        self.roster.mkdir()
        self.old, self.new, self.account, self.session, self.user = [carry.new_id() for _ in range(5)]
        self.fee, self.operation, self.debt, self.cash, self.bank, self.handover = [carry.new_id() for _ in range(6)]
        (self.roster / 'source.xlsx').write_bytes(b'original reviewed source fixture')
        digest = hashlib.sha256((self.roster / 'source.xlsx').read_bytes()).hexdigest()
        self.plan = dict(sourceSha256=digest, correlationId='approved-roster', accounts=[dict(accountId=self.account, sponsorId=self.old)])
        self.sources = [
            [dict(Id=self.account, ExternalAccountId='Account A', SponsorId=self.new)],
            [dict(Id=self.session, PlatformRiderAccountId=self.account)],
            [dict(Id=self.fee, HandoverId=self.session, CreatedAtUtc='2026-10-17T13:00:00Z')],
            [dict(Id=self.operation, HandoverId=self.session, Status='3', ExecutedAtUtc='2026-10-18T14:00:00Z')],
            [dict(Id=self.cash, HandoverId=self.session, Purpose='3', Fund='1', Direction='1', Amount='200.00',
                  ActorUserId=self.user, OccurredAtUtc='2026-10-17T14:00:00Z', Reason='fee'),
             dict(Id=self.bank, HandoverId=self.session, OperationId=self.operation, Purpose='1', Fund='3', Direction='2', Amount='50.00',
                  ActorUserId=self.user, OccurredAtUtc='2026-10-18T14:00:00Z', Reason='payout')],
            [dict(Id=self.debt, OperationId=self.operation)],
            [dict(EntityId=self.account, Sequence='10', OccurredAtUtc='2026-10-18T12:00:00Z',
                  BeforeJson=json.dumps(dict(SponsorId=self.old)), AfterJson=json.dumps(dict(SponsorId=self.new)))],
            [],
            [dict(Id=self.handover, Status='2', BusinessDate='2026-10-18', CashboxAmount='200.00', BankAmount='0.00', PetroAppAmount='0.00',
                  MovementCount='1', ConfirmedAtUtc='2026-10-18T10:00:00Z', AccountantUserId=self.user)],
            [dict(CashboxHandoverId=self.handover, MoneyMovementId=self.cash)],
            [dict(Id=self.old, RegistryNameAr='الشركة الأصلية', EmployerIdentityNumber='old'), dict(Id=self.new, RegistryNameAr='الشركة الجديدة', EmployerIdentityNumber='new')],
        ]
        routes = []
        for sponsor, location, purpose in [(self.old, 1, 2), (self.old, 2, 2), (self.new, 3, 3)]:
            routes.append(dict(Id=carry.new_id(), SponsorId=sponsor, Location=location, Purpose=purpose,
                               IsDefault='true', IsActive='true', IsPosting='true', Code='code', NameAr='حساب الاختبار'))
        self.metadata = [[dict(Id=carry.new_id(), CorrelationId='approved-roster', OccurredAtUtc='2026-10-17T12:00:00Z',
                               BeforeJson=json.dumps(dict(SourceSha256=digest)))],
                         [dict(Id=self.user, DisplayNameAr='المحاسب')], routes, []]

    def build(self):
        (self.roster / 'plan.json').write_text(json.dumps(self.plan), encoding='utf-8')
        xml(self.folder / 'source-evidence.xml', self.sources)
        xml(self.folder / 'metadata-evidence.xml', self.metadata)
        xml(self.roster / 'app.Sponsors-before.xml', [self.sources[10]])
        with contextlib.redirect_stdout(io.StringIO()):
            carry.main(self.folder, self.roster)
        return json.loads((self.folder / 'carryforward-plan.json').read_text(encoding='utf-8'))

    def test_source_dates_preserve_fee_and_inherit_operation_and_debt(self):
        result = self.build()
        owners = {(o['source'], o['sourceId']): o['sponsorId'] for o in result['ownership']}
        self.assertEqual(self.old, owners[(1, self.fee)])
        self.assertEqual(self.old, owners[(4, self.cash)])
        self.assertEqual(self.new, owners[(2, self.operation)])
        self.assertEqual(self.new, owners[(3, self.debt)])
        self.assertEqual(self.new, owners[(4, self.bank)])

    def test_confirmed_cash_is_transferred_once_without_new_receipt_or_invented_funding(self):
        result = self.build()
        self.assertEqual(2, len([p for p in result['postings'] if p['kind'] == 1]))
        transfers = [p for p in result['postings'] if p['kind'] == 2]
        self.assertEqual(2, len(transfers))
        self.assertEqual(0, sum(carry.Decimal(p['amount']) for p in transfers))
        self.assertEqual(1, len({p['batchId'] for p in transfers}))
        self.assertEqual({'1', '2'}, {str(p['kind']) for p in result['postings']})
        self.assertTrue(any(carry.Decimal(b['balance']) == -50 for b in result['balances']))
        self.assertEqual(1, len(result['handoverPostings']))

    def test_broken_sponsor_chain_blocks_reconstruction(self):
        self.sources[6][0]['BeforeJson'] = json.dumps(dict(SponsorId=self.new))
        with self.assertRaisesRegex(ValueError, 'Incomplete sponsor audit chain'):
            self.build()

    def test_source_before_known_baseline_requires_review(self):
        self.sources[2][0]['CreatedAtUtc'] = '2026-10-16T00:00:00Z'
        with self.assertRaisesRegex(ValueError, 'Source predates verified roster'):
            self.build()

    def test_existing_frozen_owner_is_never_overwritten(self):
        self.metadata[3].append(dict(Id=carry.new_id(), Source='1', SourceId=self.fee, SponsorId=self.new, PlatformAccountId=self.account))
        with self.assertRaisesRegex(ValueError, 'Existing frozen ownership differs'):
            self.build()

    def test_handover_discrepancy_blocks_plan(self):
        self.sources[8][0]['CashboxAmount'] = '199.00'
        with self.assertRaisesRegex(AssertionError, 'frozen handover differs'):
            self.build()


if __name__ == '__main__':
    unittest.main()
