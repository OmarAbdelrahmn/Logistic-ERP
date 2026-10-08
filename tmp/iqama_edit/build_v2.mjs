import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const root='C:/Users/omarf/OneDrive/Documents/ChatGPT/Logistic ERP';
const input=`${root}/outputs/iqama-platform-review-20261007/platform_accounts_iqama_corrected.xlsx`;
const oldReport=`${root}/outputs/iqama-platform-review-20261007/platform_accounts_upload_review.xlsx`;
const hungerInput='C:/Users/omarf/Downloads/تمام تطبيق هنقرستيشن.xlsx';
const outputDir=`${root}/outputs/iqama-platform-review-20261007-v2`;
const master=await SpreadsheetFile.importXlsx(await FileBlob.load(input));
const report=await SpreadsheetFile.importXlsx(await FileBlob.load(oldReport));
const hungerSource=await SpreadsheetFile.importXlsx(await FileBlob.load(hungerInput));
const summary=master.worksheets.getItem('لوحة التحكم وملخص التطبيقات');
const jahez=master.worksheets.getItem('جاهز - Jahez');
const hunger=master.worksheets.getItem('هنقرستيشن - HungerStation');
const chefsz=master.worksheets.getItem('ذا شيفز - The Chefsz');
const keeta=master.worksheets.getItem('كيتا - Keeta');
const sourceSheet=hungerSource.worksheets.getItem('يعمل');
const reviewSheet=report.worksheets.getItem('مراجعة قبل الرفع');

const oldJahez=jahez.getRange('A2:F386').values;
const oldHunger=hunger.getRange('A2:F449').values;
const oldChefsz=chefsz.getRange('A2:F465').values;
const newHunger=sourceSheet.getRange('A2:G439').values;
const keetaRows=keeta.getRange('A5:Q263').values;

const jahezSeen=new Set(), jahezRows=[], jahezDuplicates=[];
for (let i=0;i<oldJahez.length;i++) {
  const row=oldJahez[i];
  const account=String(row[0]??'');
  if (!account) throw new Error(`Blank Jahez account at row ${i+2}`);
  if (jahezSeen.has(account)) {
    const kept=jahezRows.find(x=>String(x[0])===account);
    if (JSON.stringify(kept)!==JSON.stringify(row)) throw new Error(`Jahez duplicate differs: ${account}`);
    jahezDuplicates.push({account,row:i+2});
  } else {jahezSeen.add(account);jahezRows.push(row);}
}
if (jahezRows.length!==380 || jahezDuplicates.length!==5) throw new Error('Jahez counts changed');

const statusMap={'يعمل':'يعمل (نشط)','مشاكل':'موقوف (مشاكل نظامية)','ارشيف':'غير نشط (أرشيف)'};
const sponsorMap={'اكسبرس':'شركة اكسبرس جايت','البوابا':'شركة البوابا الموكبلا','التجارية':'مؤسسة البوابة التجارية','خارج الكفالة':'خارج الكفالة'};
const oldHungerByAccount=new Map();
for (const row of oldHunger) {
  const account=String(row[0]??'');
  if (!oldHungerByAccount.has(account)) oldHungerByAccount.set(account,[]);
  oldHungerByAccount.get(account).push(row);
}
const newSeen=new Set();
const hungerRows=newHunger.map((row,i)=>{
  const [iqama,name,account,city,vehicle,sponsor,status]=row;
  const id=String(account??''), iq=String(iqama??'');
  if (!id || !/^[12]\d{9}$/.test(iq) || !city || !vehicle || !sponsorMap[sponsor] || !statusMap[status]) throw new Error(`Invalid HungerStation row ${i+2}`);
  if (newSeen.has(id)) throw new Error(`Duplicate account in new HungerStation file: ${id}`);
  newSeen.add(id);
  const old=oldHungerByAccount.get(id);
  if (!old || old.some(x=>String(x[1])!==iq)) throw new Error(`HungerStation account/iqama mismatch: ${id}`);
  return [id,iq,vehicle,city,sponsorMap[sponsor],statusMap[status]];
});
if (hungerRows.length!==438 || oldHungerByAccount.size!==438 || newSeen.size!==oldHungerByAccount.size) throw new Error('HungerStation account set changed');
const chefszAccounts=oldChefsz.map(r=>String(r[0]??''));
if (chefszAccounts.length!==464 || new Set(chefszAccounts).size!==464) throw new Error('Chefsz has duplicate account IDs');

// Preserve all six column headings and the existing cell formatting. Only data rows are compacted.
jahez.getRange('A2:F381').values=jahezRows;
jahez.getRange('A382:F386').clear({applyTo:'contents'});
hunger.getRange('A2:F439').values=hungerRows;
hunger.getRange('A440:F449').clear({applyTo:'contents'});

for (let i=0;i<jahezRows.length;i++) {
  const rowNum=i+2, status=String(jahezRows[i][5]);
  const cell=jahez.getRange(`F${rowNum}`);
  if (status==='غير نشط (Inactive)') {
    cell.format.fill='#FADBD8';
    cell.format.font={name:'Arial',size:9,bold:true,color:'#922B21'};
  } else if (status==='نشط (Active)') {
    cell.format.fill=rowNum%2===0?'#FFFFFF':'#F8F9F9';
    cell.format.font={name:'Arial',size:9,bold:false,color:'#000000'};
  } else throw new Error(`Unknown Jahez status at ${rowNum}`);
}
for (let i=0;i<hungerRows.length;i++) {
  const rowNum=i+2,status=String(hungerRows[i][5]);
  const cell=hunger.getRange(`F${rowNum}`);
  if (status==='يعمل (نشط)') {
    cell.format.fill='#EAFAF1';
    cell.format.font={name:'Arial',size:9,bold:true,color:'#1E8449'};
  } else if (status==='موقوف (مشاكل نظامية)') {
    cell.format.fill='#FADBD8';
    cell.format.font={name:'Arial',size:9,bold:true,color:'#922B21'};
  } else if (status==='غير نشط (أرشيف)') {
    cell.format.fill=rowNum%2===0?'#FFFFFF':'#F8F9F9';
    cell.format.font={name:'Arial',size:9,bold:false,color:'#000000'};
  } else throw new Error(`Unknown HungerStation status at ${rowNum}`);
}

const counts=(rows,index)=>{
  const result=new Map();
  for(const row of rows){const k=String(row[index]??'');result.set(k,(result.get(k)??0)+1);}
  return result;
};
const num=(map,key)=>map.get(key)??0;
const jVehicles=counts(jahezRows,2),jCities=counts(jahezRows,3),jSponsors=counts(jahezRows,4),jStatuses=counts(jahezRows,5);
const hVehicles=counts(hungerRows,2),hCities=counts(hungerRows,3),hSponsors=counts(hungerRows,4),hStatuses=counts(hungerRows,5);
const cSponsors=counts(oldChefsz,4);
const pct=(n,d)=>`${(100*n/d).toFixed(1)}%`;
const total=keetaRows.length+jahezRows.length+hungerRows.length+oldChefsz.length;
const cars=199+num(jVehicles,'سيارة')+num(hVehicles,'سيارة')+443;
const bikes=total-cars;
const active=259+num(jStatuses,'نشط (Active)')+num(hStatuses,'يعمل (نشط)')+293;
const inactive=total-active;
const jeddah=203+num(jCities,'جدة')+num(hCities,'جدة')+457;
const riyadh=56+num(jCities,'الرياض')+num(hCities,'الرياض');
const other=7;
const allIqamas=new Set([
  ...keetaRows.map(r=>String(r[4])),
  ...jahezRows.map(r=>String(r[1])),
  ...hungerRows.map(r=>String(r[1])),
  ...oldChefsz.map(r=>String(r[0])),
]);
if(total!==1541 || cars!==1270 || bikes!==271 || active!==1150 || inactive!==391 || jeddah!==1329 || riyadh!==205 || allIqamas.size!==524 || jeddah+riyadh+other!==total) throw new Error('Summary validation failed');

const updates={
  B4:`إجمالي حسابات التطبيقات\n${total.toLocaleString('en-US')} حساب مسجل\nعبر 4 تطبيقات توصيل`,
  H4:`أسطول السيارات مقابل الدراجات\n${cars.toLocaleString('en-US')} سيارة (${pct(cars,total)})\n${bikes} دراجة نارية (${pct(bikes,total)})`,
  J4:`التوزيع الجغرافي للحسابات\n${jeddah.toLocaleString('en-US')} جدة (${pct(jeddah,total)})\n${riyadh} الرياض (${pct(riyadh,total)}) | ${other} أخرى`,
  I8:'الرياض / مدن أخرى',
  C10:jahezRows.length,D10:num(jVehicles,'سيارة'),E10:num(jVehicles,'دراجة آلية'),
  F10:`${num(jStatuses,'نشط (Active)')} (${pct(num(jStatuses,'نشط (Active)'),jahezRows.length)})`,
  G10:`${num(jStatuses,'غير نشط (Inactive)')} (${pct(num(jStatuses,'غير نشط (Inactive)'),jahezRows.length)})`,
  H10:num(jCities,'جدة'),I10:num(jCities,'الرياض'),
  J10:`اكسبرس جايت (${num(jSponsors,'شركة اكسبرس جايت')}) - البوابا الموكبلا (${num(jSponsors,'شركة البوابا الموكبلا')}) - البوابة التجارية (${num(jSponsors,'مؤسسة البوابة التجارية')})`,
  K10:'رقم اليوزر، الإقامة، نوع المركبة، مدينة التسجيل، السجل، حالة الحساب',
  C11:hungerRows.length,D11:num(hVehicles,'سيارة'),E11:num(hVehicles,'دباب'),
  F11:`${num(hStatuses,'يعمل (نشط)')} (${pct(num(hStatuses,'يعمل (نشط)'),hungerRows.length)})`,
  G11:`${hungerRows.length-num(hStatuses,'يعمل (نشط)')} (${pct(hungerRows.length-num(hStatuses,'يعمل (نشط)'),hungerRows.length)}) [أرشيف/مشاكل]`,
  H11:num(hCities,'جدة'),I11:num(hCities,'الرياض'),
  J11:`البوابا الموكبلا (${num(hSponsors,'شركة البوابا الموكبلا')}) - اكسبرس جايت (${num(hSponsors,'شركة اكسبرس جايت')}) - البوابة التجارية (${num(hSponsors,'مؤسسة البوابة التجارية')}) - خارج الكفالة (${num(hSponsors,'خارج الكفالة')})`,
  K11:'رقم الآيدي، الإقامة، نوع المركبة، المدينة، السجل، الحالة؛ الأسماء في ملف هنقرستيشن المرجعي',
  J12:`اكسبرس جايت (${num(cSponsors,'شركة اكسبرس جايت')}) - البوابة التجارية (${num(cSponsors,'مؤسسة البوابة التجارية')})`,
  K12:'الإقامة والمعرف في عمود واحد، الاسم، نوع المركبة، المدينة، السجل، الحالة',
  C13:total,D13:cars,E13:bikes,
  F13:`${active.toLocaleString('en-US')} (${pct(active,total)})`,
  G13:`${inactive} (${pct(inactive,total)})`,
  H13:jeddah,I13:`${riyadh} الرياض + ${other} بريدة`,
  J13:'يشمل 6 حسابات خارج الكفالة في هنقرستيشن',
  K13:'تمت إزالة تكرارات جاهز وهنقرستيشن؛ تحقق من 33 إقامة تاريخية وفروق بيانات المدينة والسجل قبل الرفع',
};
for (const [cell,value] of Object.entries(updates)) summary.getRange(cell).values=[[value]];

reviewSheet.getRange('A2').values=[['جاهز، هنقرستيشن، ذا شيفز | دليل الحسابات 2026-10-07، ملف هنقرستيشن المحدّث، وسجل المقيمين 2026-09-12']];
reviewSheet.getRange('A3').values=[['أزيلت 15 نسخة مكررة من جاهز وهنقرستيشن؛ شيفز بلا تكرار؛ 33 إقامة تاريخية بحاجة إلى تحقق إذا ستُرفع.']];
const priorIssues=reviewSheet.getRange('A6:G54').values;
for (let i=0;i<priorIssues.length;i++) {
  const kind=String(priorIssues[i][5]??'');
  if (kind.includes('تكرار')) {
    const platform=String(priorIssues[i][0]);
    priorIssues[i][5]='تم حل تكرار الحساب';
    priorIssues[i][6]=platform.startsWith('جاهز')
      ? 'أُبقي صف واحد مطابق، وأزيل الصف المكرر من النسخة المحدثة.'
      : 'استُخدم الحساب والحالة من ملف هنقرستيشن المحدّث، وأزيل الصف الزائد من النسخة المحدثة.';
  }
}
const historical=priorIssues.filter(r=>String(r[5]).includes('غير موجود'));
if(historical.length!==33) throw new Error(`Historical review count changed: ${historical.length}`);
priorIssues.push([
  'جاهز','ملخص المصدر','—','—','—','فرق في ملخص المدينة',
  'الملخص القديم: 368 جدة و17 الرياض. صفوف جاهز الأصلية: 315 جدة و70 الرياض؛ بعد إزالة التكرار: 311 جدة و69 الرياض. تحقق من المدينة الفعلية قبل الرفع.'
]);
priorIssues.push([
  'ذا شيفز','ملخص المصدر','—','—','—','فرق في ملخص السجل',
  'الملخص القديم: 219 اكسبرس و202 البوابا و43 التجارية. صفوف شيفز: 421 اكسبرس و43 التجارية. تحقق من سجل 202 حساب قبل الرفع.'
]);
reviewSheet.getRange('A6:G56').values=priorIssues;
reviewSheet.getRange('A55:G56').format.font={name:'Arial',size:10,color:'#1F2937'};
reviewSheet.getRange('G55:G56').format.wrapText=true;
reviewSheet.getRange('A55:G56').format.rowHeight=36;

await fs.mkdir(outputDir,{recursive:true});
const finalMaster=await SpreadsheetFile.exportXlsx(master);
await finalMaster.save(`${outputDir}/platform_accounts_deduplicated.xlsx`);
const finalReport=await SpreadsheetFile.exportXlsx(report);
await finalReport.save(`${outputDir}/platform_accounts_upload_review.xlsx`);
for (const [sheetName,range,name] of [
  ['لوحة التحكم وملخص التطبيقات','A8:K13','summary-v2.png'],
  ['جاهز - Jahez','A22:F29','jahez-v2.png'],
  ['هنقرستيشن - HungerStation','A263:F269','hunger-v2.png'],
]) {
  const blob=await master.render({sheetName,range,scale:1,format:'png'});
  await fs.writeFile(`${outputDir}/${name}`,new Uint8Array(await blob.arrayBuffer()));
}
console.log(JSON.stringify({jahez:jahezRows.length,hunger:hungerRows.length,chefsz:oldChefsz.length,keeta:keetaRows.length,total,uniqueIqamas:allIqamas.size,reviewRows:priorIssues.length,historical:historical.length}));
