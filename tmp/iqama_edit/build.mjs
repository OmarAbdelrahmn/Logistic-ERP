import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile, Workbook } from '@oai/artifact-tool';

const src = 'C:/Users/omarf/Downloads/دليل_معرفات_وحسابات_التطبيقات_الموحد_2026-10-07.xlsx';
const ref = 'C:/Users/omarf/Downloads/المقيمين_ERP_مكتمل_السجلات_الثلاثة_12-09-2026.xlsx';
const outDir = 'C:/Users/omarf/OneDrive/Documents/ChatGPT/Logistic ERP/outputs/iqama-platform-review-20261007';
const master = await SpreadsheetFile.importXlsx(await FileBlob.load(src));
const residents = await SpreadsheetFile.importXlsx(await FileBlob.load(ref));
const summary = master.worksheets.getItem('لوحة التحكم وملخص التطبيقات');
const chefsz = master.worksheets.getItem('ذا شيفز - The Chefsz');
const hunger = master.worksheets.getItem('هنقرستيشن - HungerStation');
const jahez = master.worksheets.getItem('جاهز - Jahez');
const resident = residents.worksheets.getItem('المقيمين النشطين');

const oldIqama = String(chefsz.getRange('A31').values[0][0]);
const correctedIqama = String(resident.getRange('A384').values[0][0]);
const residentName = String(resident.getRange('B384').values[0][0]);
const chefszName = String(chefsz.getRange('B31').values[0][0]);
const hungerIqama = String(hunger.getRange('B349').values[0][0]);
if (residentName !== chefszName || hungerIqama !== correctedIqama || oldIqama === correctedIqama || oldIqama.length !== 10 || correctedIqama.length !== 10) {
  throw new Error('The three source checks for Chefsz row 31 did not agree');
}

const platformRanges = [
  {name:'جاهز - Jahez', sheet:jahez, start:2, last:386, iqama:1, account:0},
  {name:'هنقرستيشن - HungerStation', sheet:hunger, start:2, last:449, iqama:1, account:0},
  {name:'ذا شيفز - The Chefsz', sheet:chefsz, start:2, last:465, iqama:0, account:0},
];
const readRows = p => p.sheet.getRangeByIndexes(p.start-1,0,p.last-p.start+1,6).values.map((v,i)=>({row:p.start+i,values:v}));
const allResidents = new Set(resident.getRange('A2:A513').values.map(x=>String(x[0]??'')).filter(Boolean));
const issueRows = [[
  'ذا شيفز', '31', oldIqama, oldIqama, correctedIqama,
  'تصحيح إقامة', 'تطابق الاسم الوحيد مع سجل المقيمين، وتؤكده إقامة هنقرستيشن في الصف 349. تم التصحيح في النسخة المحدثة.'
]];
let duplicateGroups=0, duplicateExact=0, duplicateConflict=0;
const unmatched = new Map();
for (const p of platformRanges) {
  const byAccount = new Map();
  for (const item of readRows(p)) {
    const v=item.values;
    const account=String(v[p.account]??'').trim();
    const iqama=String(v[p.iqama]??'').trim();
    if (!account || !iqama) continue;
    if (!byAccount.has(account)) byAccount.set(account, []);
    byAccount.get(account).push(item);
    // The one corrected value is intentionally excluded from unmatched review.
    if (iqama !== oldIqama && !allResidents.has(iqama)) {
      if (!unmatched.has(iqama)) unmatched.set(iqama,[]);
      unmatched.get(iqama).push({platform:p.name,row:item.row,account,status:String(v[5]??'')});
    }
  }
  if (p.name.startsWith('ذا شيفز') && !allResidents.has(correctedIqama)) throw new Error('Corrected iqama absent from residents');
  for (const [account, group] of byAccount) {
    if (group.length<2) continue;
    duplicateGroups++;
    const exact=group.every(x=>JSON.stringify(x.values)===JSON.stringify(group[0].values));
    if (exact) duplicateExact++; else duplicateConflict++;
    const iqama=String(group[0].values[p.iqama]);
    issueRows.push([
      p.name, group.map(x=>x.row).join('، '), account, iqama, iqama,
      exact ? 'حساب مكرر بالكامل' : 'حساب مكرر ببيانات مختلفة',
      exact ? 'احتفظ بصف واحد عند تجهيز ملف الرفع، بعد تأكيد أن التكرار غير مقصود.' : 'تحقق من الحالة والسجل الحاليين قبل الرفع؛ لا يمكن اختيار الصف الصحيح بلا تاريخ تحديث.'
    ]);
  }
}
for (const [iqama, entries] of unmatched) {
  issueRows.push([
    [...new Set(entries.map(x=>x.platform))].join(' / '),
    entries.map(x=>x.row).join('، '),
    entries.map(x=>x.account).join('، '),
    iqama, '',
    'غير موجودة بسجل المقيمين 2026-09-12',
    `الحالات: ${[...new Set(entries.map(x=>x.status))].join(' / ')}. تحقق من الإقامة الحالية فقط إذا ستُرفع هذه الحسابات التاريخية.`
  ]);
}
if (duplicateGroups!==15 || duplicateExact!==10 || duplicateConflict!==5 || unmatched.size!==33) throw new Error(`Audit totals changed: ${duplicateGroups}/${duplicateExact}/${duplicateConflict}/${unmatched.size}`);

chefsz.getRange('A31').values=[[correctedIqama]];
summary.getRange('D4').values=[['إجمالي المناديب الفعليين\n524 مندوب فريد\n(Unique Drivers by Iqama)']];
summary.getRange('F4').values=[['مناديب مفعلين بـ 4 تطبيقات\n200 مندوب (38.2%)\nيعملون بكافة المنصات معاً']];
summary.getRange('C19').values=[[125]];
summary.getRange('C20').values=[[53]];
summary.getRange('C21').values=[[524]];
for (const [cell,value] of Object.entries({D17:'38.2%',D18:'27.9%',D19:'23.9%',D20:'10.1%'})) summary.getRange(cell).values=[[value]];
summary.getRange('K13').values=[['يلزم حل تكرار الحسابات والتحقق من السجلات التاريخية قبل الرفع']];

const report = Workbook.create();
const sheet = report.worksheets.add('مراجعة قبل الرفع');
sheet.showGridLines=false;
sheet.getRange('A1').values=[['مراجعة حسابات التطبيقات قبل الرفع']];
sheet.getRange('A2').values=[['جاهز، هنقرستيشن، ذا شيفز | المصدر: دليل الحسابات 2026-10-07 وسجل المقيمين 2026-09-12']];
sheet.getRange('A3').values=[['تصحيح واحد مطبق؛ 15 حساباً مكرراً؛ 33 إقامة تاريخية غير موجودة في سجل المقيمين المرجعي.']];
const headers=['المنصة','صفوف المصدر','رقم الحساب','الإقامة في الملف','الإقامة المصححة','نوع الملاحظة','الإجراء والمصدر'];
sheet.getRange('A5:G5').values=[headers];
sheet.getRange(`A6:G${5+issueRows.length}`).values=issueRows;
sheet.getRange('A1:G1').format.font={name:'Arial',size:16,bold:true,color:'#1F2937'};
sheet.getRange('A2:G3').format.font={name:'Arial',size:10,color:'#475569'};
sheet.getRange('A5:G5').format={fill:'#17365D',font:{name:'Arial',size:10,bold:true,color:'#FFFFFF'}};
sheet.getRange(`A6:G${5+issueRows.length}`).format.font={name:'Arial',size:10,color:'#1F2937'};
sheet.getRange(`C6:E${5+issueRows.length}`).setNumberFormat('@');
for (const [col,width] of Object.entries({A:28,B:16,C:20,D:20,E:20,F:34,G:92})) sheet.getRange(`${col}:${col}`).format.columnWidth=width;
sheet.getRange(`G6:G${5+issueRows.length}`).format.wrapText=true;
sheet.getRange(`A6:G${5+issueRows.length}`).format.rowHeight=30;
sheet.freezePanes.freezeRows(5);

await fs.mkdir(outDir,{recursive:true});
const mfile=await SpreadsheetFile.exportXlsx(master);
await mfile.save(`${outDir}/platform_accounts_iqama_corrected.xlsx`);
const rfile=await SpreadsheetFile.exportXlsx(report);
await rfile.save(`${outDir}/platform_accounts_upload_review.xlsx`);
const inspection=await master.inspect({kind:'region',sheetId:'ذا شيفز - The Chefsz',range:'A31:B31',maxChars:450});
console.log(inspection.ndjson);
const errors=await master.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:50},maxChars:800});
console.log('formula_errors',errors.ndjson);
const preview=await report.render({sheetName:'مراجعة قبل الرفع',range:'A1:G11',scale:1,format:'png'});
await fs.writeFile(`${outDir}/review-preview.png`,new Uint8Array(await preview.arrayBuffer()));
console.log(JSON.stringify({duplicateGroups,duplicateExact,duplicateConflict,unmatched:unmatched.size,reviewRows:issueRows.length,oldIqamaMasked:oldIqama.slice(0,3)+'…'+oldIqama.slice(-2),correctedIqamaMasked:correctedIqama.slice(0,3)+'…'+correctedIqama.slice(-2)}));
