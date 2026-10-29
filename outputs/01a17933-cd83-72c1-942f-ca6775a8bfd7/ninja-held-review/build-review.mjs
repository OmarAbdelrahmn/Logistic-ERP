import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { Workbook, SpreadsheetFile } from '@oai/artifact-tool';

const out = path.dirname(fileURLToPath(import.meta.url));
const data = JSON.parse(await fs.readFile(path.join(out, 'review-input.json'), 'utf8'));
const wb = Workbook.create();
const review = wb.worksheets.add('Pending review');
const source = wb.worksheets.add('Original 40 rows');
const navy = '#20344D', amber = '#FFF2CC', gray = '#536273';
const decode = v => v && typeof v === 'object' && v.kind === 'date' ? new Date(v.value + (v.value.includes('T') ? 'Z' : 'T00:00:00Z')) : v && typeof v === 'object' && v.kind === 'time' ? v.value : v;
const issue = r => r.status === 'SharedDay' ? 'Shared day' : 'Missing assignment';
const raw = data.rows.map(r => [...r.raw.map(decode), r.row, issue(r)]);
source.getRange('A1:U41').values = [[...data.headers, 'Original workbook row', 'Hold reason'], ...raw];
source.getRange('A1:U41').format.font = { name: 'Arial', size: 10, color: '#202D3B' };
source.getRange('A1:U1').format = { fill: navy, font: { name: 'Arial', size: 10, bold: true, color: '#FFFFFF' }, wrapText: true, rowHeight: 42, verticalAlignment: 'center' };
source.getRange('A2:A41').setNumberFormat('yyyy-mm-dd');
source.getRange('B2:C41').setNumberFormat('@');
source.getRange('G2:G41').setNumberFormat('[h]:mm:ss');
source.getRange('E2:E41').setNumberFormat('0');
source.getRange('F2:F41').setNumberFormat('0.00');
source.getRange('H2:J41').setNumberFormat('0.00000000');
source.getRange('K2:P41').setNumberFormat('0.00"%"');
source.getRange('Q2:S41').setNumberFormat('0.00');
source.getRange('T2:T41').setNumberFormat('0');
source.getRange('A1:U41').format.columnWidth = 16;
source.getRange('D1:D41').format.columnWidth = 38;
source.getRange('U1:U41').format.columnWidth = 22;
source.getRange('A2:U41').format.rowHeight = 23;
const sourceTable = source.tables.add('A1:U41', true, 'NinjaHeldSourceRows');
sourceTable.style = 'TableStyleLight9';
sourceTable.showFilterButton = true;
source.getRange('A2:C41').format.horizontalAlignment = 'center';
source.freezePanes.freezeRows(1); source.showGridLines = false;

review.getRange('A1:L49').format.font = { name: 'Arial', size: 10, color: '#202D3B' };
review.showGridLines = false; review.tabColor = navy;
review.getRange('A2').values = [['NINJA — 40 pending account-day records']];
review.getRange('A2').format.font = { name: 'Arial', size: 15, bold: true, color: navy };
review.getRange('A2:L2').format.rowHeight = 27;
review.getRange('A3').values = [['October 2026 | 19 accounts | Not imported — confirmation required']];
review.getRange('A3:L3').format = { rowHeight: 24, font: { name: 'Arial', size: 10, color: gray }, borders: { bottom: { style: 'thin', color: '#B8C3D0' } } };
review.getRange('A5').values = [['Records']]; review.getRange('B5').formulas = [['=COUNTA(B10:B49)']];
review.getRange('D5').values = [['Orders']]; review.getRange('E5').formulas = [['=SUM(E10:E49)']];
review.getRange('G5').values = [['Hours']]; review.getRange('H5').formulas = [['=SUM(F10:F49)']];
review.getRange('I5').values = [['Shared-day rows']]; review.getRange('J5').formulas = [['=COUNTIFS(G10:G49,"Shared day")']];
review.getRange('H5').setNumberFormat('#,##0.00');
for (const cell of ['B5','E5','H5','J5']) review.getRange(cell).format.font = { name: 'Arial', size: 12, bold: true, color: navy };
review.getRange('A6').values = [['Yellow cells are for your confirmations. Names in Ninja’s file are not verified historical assignments.']];
review.getRange('A6:L6').format = { rowHeight: 24, font: { name: 'Arial', size: 10, italic: true, color: gray } };
const headers = ['Source row','Captain ID','Name in Ninja file','Work date','Orders','Hours','Issue','Recorded assignment history','Information required','Confirmed rider / Iqama','Confirmed dates / earlier-holder split','Review notes'];
review.getRange('A9:L9').values = [headers];
const matrix = data.rows.map(r => {
  const history = r.history.length ? r.history.map(h => `${h.EffectiveFrom} to ${h.EffectiveTo || 'current'} | ${h.FullNameAr || h.FullNameEn || 'Name unavailable'}`).join('\n') : 'No assignment recorded';
  return [null,null,null,new Date(r.date+'T00:00:00Z'),null,null,null,history,
    r.status === 'SharedDay' ? 'Confirm earlier holder’s completed orders and hours; last holder receives remainder.' : 'Confirm actual rider and the assignment dates covering this day.',
    null,null,null];
});
review.getRange('A10:L49').values = matrix;
for (let i=0;i<40;i++) {
  const row=i+10, src=i+2;
  for (const [column,expression] of [['A',`='Original 40 rows'!T${src}`],['B',`='Original 40 rows'!C${src}`],['C',`='Original 40 rows'!D${src}`],['E',`='Original 40 rows'!E${src}`],['F',`=ROUND('Original 40 rows'!G${src}*24,8)`],['G',`='Original 40 rows'!U${src}`]]) review.getRange(`${column}${row}`).formulas = [[expression]];
}
const reviewTable = review.tables.add('A9:L49', true, 'NinjaHeldReview');
reviewTable.style = 'TableStyleLight9';
reviewTable.showFilterButton = true;
review.getRange('A9:L9').format = { fill: navy, font: { name: 'Arial', size: 10, bold: true, color: '#FFFFFF' }, rowHeight: 43, wrapText: true, horizontalAlignment: 'center', verticalAlignment: 'center' };
review.getRange('J9:L9').format = { fill: '#9A6900', font: { name: 'Arial', size: 10, bold: true, color: '#FFFFFF' } };
review.getRange('A10:L49').format = { rowHeight: 52, verticalAlignment: 'center' };
review.getRange('C10:C49').format.wrapText = true;
review.getRange('H10:L49').format.wrapText = true;
review.getRange('J10:L49').format.fill = amber;
review.getRange('J10:L49').format.font = { name: 'Arial', size: 10, color: '#1D4E89' };
review.getRange('B10:B49').setNumberFormat('@'); review.getRange('D10:D49').setNumberFormat('yyyy-mm-dd');
review.getRange('A10:B49').format.horizontalAlignment = 'center';
review.getRange('D10:D49').format.horizontalAlignment = 'center';
review.getRange('G10:G49').format.horizontalAlignment = 'center';
review.getRange('H5').format.horizontalAlignment = 'left';
review.getRange('E10:E49').setNumberFormat('#,##0'); review.getRange('F10:F49').setNumberFormat('0.00');
review.getRange('J10:L49').setNumberFormat('@');
const widths = [10,13,34,14,10,11,23,57,46,26,36,36];
for (let i=0;i<12;i++) review.getRangeByIndexes(0,i,49,1).format.columnWidth=widths[i];
for (let i=0;i<40;i++) if (data.rows[i].history.length>1) review.getRange(`A${i+10}:L${i+10}`).format.rowHeight = 76;
review.getRange('G10:G49').conditionalFormats.add('containsText',{text:'Missing assignment',format:{fill:'#FCE9E7',font:{color:'#9C3028'}}});
review.getRange('G10:G49').conditionalFormats.add('containsText',{text:'Shared day',format:{fill:'#ECE8F7',font:{color:'#594185',bold:true}}});
review.freezePanes.freezeRows(9);
review.freezePanes.freezeColumns(2);
wb.recalculate();
console.log((await wb.inspect({kind:'table',range:'\'Pending review\'!A5:J5',include:'values,formulas',tableMaxRows:1,tableMaxCols:10,maxChars:1600})).ndjson);
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A',options:{useRegex:true,maxResults:10},maxChars:1000})).ndjson);
const xlsx=await SpreadsheetFile.exportXlsx(wb);
await xlsx.save(path.join(out,'Ninja_40_Held_Performance_Review.xlsx'));
for (const [name,sheet,range] of [['review-preview','Pending review','A1:I15'],['review-inputs-preview','Pending review','H9:L15'],['source-preview','Original 40 rows','A1:J7']]) {
  const preview=await wb.render({sheetName:sheet,range,scale:1,format:'png'});
  await fs.writeFile(path.join(out,name+'.png'),new Uint8Array(await preview.arrayBuffer()));
}
console.log('Saved Ninja_40_Held_Performance_Review.xlsx');
