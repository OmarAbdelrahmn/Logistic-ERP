const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = 'C:/Users/omarf/Videos/FrontProject';
const ts = require(path.join(root, 'node_modules/typescript'));
const file = path.join(root, 'src/app/admin/jahez/accounts/page.tsx');
const source = fs.readFileSync(file, 'utf8');
const ast = ts.createSourceFile(file, source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
const component = ast.statements.find(s => ts.isFunctionDeclaration(s) && s.name?.text === 'JahezAccountsPage');
const names = new Set(['opNeedsPayment', 'trimmedOpPaymentRef', 'opPaymentReferenceError',
  'opPaymentFundInvalid', 'savedOperationNeedsPayment', 'savedPaymentReferenceInvalid',
  'opPaymentDetailsChanged', 'opSaveDisabled', 'handleSaveOperationDraft', 'handleExecuteOperation']);
const selected = component.body.statements.filter(s => ts.isVariableStatement(s)
  && s.declarationList.declarations.some(d => names.has(d.name.getText(ast))));
assert.equal(selected.length, names.size);
const dependencies = ['OperationMode', 'OperationStatus', 'Purpose', 'opMode', 'opPreview', 'opTransferToRider',
  'opPaymentRef', 'opFund', 'savedOperation', 'opSubmitting', 'previewLoading', 'opAccount',
  'opEffectiveAt', 'opThroughDate', 'opNet', 'opReason', 'toast', 'setOpSubmitting', 'setOpPreview',
  'previewJahezOperation', 'updateJahezOperation', 'createJahezOperation', 'setSavedOperation',
  'executeJahezOperation', 'setIsOpModalOpen', 'loadData', 'getJahezAccountDetails'];
const code = `const {${dependencies.join(',')}} = state;\n${selected.map(s => s.getText(ast)).join('\n')}\n`
  + 'return {handleSaveOperationDraft,handleExecuteOperation,opSaveDisabled,savedPaymentReferenceInvalid,opPaymentDetailsChanged};';
const makeHandlers = new Function('state', ts.transpileModule(code,
  {compilerOptions: {target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.None}}).outputText);
const mode = {Settle: 1, SettleAndWithdraw: 2, Withdraw: 3};
const results = [];
function setup(overrides = {}, serverPreview) {
  const calls = {created: [], updated: [], executed: [], warnings: [], previews: []};
  const preview = {remaining: 135.94, allowedFunds: [1, 2]};
  const state = {
    OperationMode: mode, OperationStatus: {Ready: 1, UnderReview: 2, Executed: 3}, Purpose: {Settlement: 1},
    opMode: mode.Settle, opPreview: preview, opTransferToRider: false, opPaymentRef: '  receipt-actual  ',
    opFund: 1, savedOperation: null, opSubmitting: false, previewLoading: false,
    opAccount: {currentHandoverId: 'session', accountId: 'account'}, opEffectiveAt: '2026-10-18T12:00',
    opThroughDate: '2026-10-18', opNet: 75.94, opReason: 'settlement',
    toast: {warning: (...v) => calls.warnings.push(v), error: (...v) => { throw new Error(v.join(': ')); }, success() {}, info() {}},
    setOpSubmitting() {}, setOpPreview() {}, setSavedOperation() {}, setIsOpModalOpen() {}, loadData() {},
    previewJahezOperation: async v => {calls.previews.push(v); return serverPreview ?? state.opPreview;},
    createJahezOperation: async v => {calls.created.push(v); return {id: 'created', ...v};},
    updateJahezOperation: async (...v) => {calls.updated.push(v); return {id: v[0], ...v[1].values};},
    executeJahezOperation: async id => calls.executed.push(id),
    getJahezAccountDetails: async () => { throw new Error('Unexpected recovery call'); },
    ...overrides,
  };
  return {calls, ...makeHandlers(state)};
}
async function check(name, fn) { await fn(); results.push(name); }
(async () => {
  for (const reference of ['', ' \t ', 'x'.repeat(501)]) {
    await check(`Reject invalid reference length ${reference.length}`, async () => {
      const s = setup({opPaymentRef: reference}); assert.equal(s.opSaveDisabled, true);
      await s.handleSaveOperationDraft(); assert.equal(s.calls.created.length, 0); assert.equal(s.calls.updated.length, 0);
    });
  }
  await check('Send trimmed real reference on create', async () => {
    const s = setup(); await s.handleSaveOperationDraft(); assert.equal(s.calls.created[0].paymentReference, 'receipt-actual');
    assert.equal(s.calls.created[0].effectiveAtUtc, '2026-10-18T09:00:00.000Z');
  });
  await check('Load saved UTC timestamps as Riyadh local time', async () => {
    const helper = ast.statements.find(s => ts.isFunctionDeclaration(s) && s.name?.text === 'riyadhDateTimeInput');
    const convert = new Function(ts.transpileModule(helper.getText(ast) + '\nreturn riyadhDateTimeInput;',
      {compilerOptions: {target: ts.ScriptTarget.ES2020}}).outputText)();
    assert.equal(convert('2026-10-18T08:40:00Z'), '2026-10-18T11:40');
    assert.equal(convert('2026-10-18T11:40:00+03:00'), '2026-10-18T11:40');
  });
  await check('Accept 500-character reference and bank disbursement', async () => {
    const s = setup({opPaymentRef: 'x'.repeat(500), opFund: 3, opPreview: {remaining: -50, allowedFunds: [1, 3]}});
    await s.handleSaveOperationDraft(); assert.equal(s.calls.created[0].paymentReference.length, 500); assert.equal(s.calls.created[0].fund, 3);
  });
  await check('No save without preview', async () => {
    const s = setup({opPreview: null}); await s.handleSaveOperationDraft(); assert.equal(s.calls.created.length, 0);
  });
  await check('Fresh preview catches new money requirement', async () => {
    const s = setup({opPaymentRef: '', opPreview: {remaining: 0, allowedFunds: []}}, {remaining: 135.94, allowedFunds: [1, 2]});
    await s.handleSaveOperationDraft(); assert.equal(s.calls.created.length, 0); assert.equal(s.calls.warnings.length, 1);
  });
  for (const [label, overrides] of [
    ['zero settlement', {opPreview: {remaining: 0, allowedFunds: []}}],
    ['withdrawal', {opMode: mode.Withdraw, opNet: 75.94, opPreview: {remaining: 0, allowedFunds: []}}],
    ['transfer', {opMode: mode.SettleAndWithdraw, opTransferToRider: true, opPreview: {remaining: 135.94, allowedFunds: []}}],
  ]) await check(`No reference required for ${label}`, async () => {
    const s = setup({...overrides, opPaymentRef: ''}); await s.handleSaveOperationDraft();
    assert.equal(s.calls.created[0].paymentReference, null); assert.equal(s.calls.created[0].fund, null);
    if (label === 'withdrawal') assert.equal(s.calls.created[0].net, 0);
  });
  const draft = {id: 'existing', mode: mode.Settle, status: 1, handoverId: 'session', remaining: 135.94,
    transferToRider: false, paymentReference: null, fund: 1, rowVersion: 'current-version'};
  await check('Repair existing draft with reference and current row version', async () => {
    const s = setup({savedOperation: draft}); await s.handleSaveOperationDraft();
    assert.equal(s.calls.created.length, 0); assert.equal(s.calls.updated[0][1].values.paymentReference, 'receipt-actual');
    assert.equal(s.calls.updated[0][1].rowVersion, 'current-version');
  });
  await check('Block execution when saved reference is missing', async () => {
    const s = setup({savedOperation: draft}); await s.handleExecuteOperation(); assert.equal(s.calls.executed.length, 0);
  });
  await check('Block execution until changed reference is saved', async () => {
    const s = setup({savedOperation: {...draft, paymentReference: 'old-reference'}});
    await s.handleExecuteOperation(); assert.equal(s.calls.executed.length, 0);
  });
  await check('Execute only with matching saved payment details', async () => {
    const s = setup({savedOperation: {...draft, paymentReference: 'receipt-actual'}});
    await s.handleExecuteOperation(); assert.deepEqual(s.calls.executed, ['existing']);
  });
  fs.writeFileSync(path.join(__dirname, 'frontend-verification.json'), JSON.stringify({passed: results.length, checks: results}, null, 2));
  console.log(`Passed ${results.length} frontend handler/payload checks. No network calls or financial writes were made.`);
})().catch(error => { console.error(error); process.exitCode = 1; });
