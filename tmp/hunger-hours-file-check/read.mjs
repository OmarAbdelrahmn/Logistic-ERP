import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
import fs from 'node:fs/promises';
const path = "C:/Users/omarf/Downloads/3PL Report_Rider's Performance_Table (36).xlsx";
const book = await SpreadsheetFile.importXlsx(await FileBlob.load(path));
const sheet = book.worksheets.getItemAt(0);
const values = sheet.getUsedRange().values;
const headers = values[0];
const hourColumn = headers.indexOf('Actual Working Hours');
const averageColumn = headers.indexOf('Avg. Working Hours/ Day');
const daysColumn = headers.indexOf('Working Days');
const rows = values.slice(1).filter(r => r[0] != null && String(r[0]).trim() !== '');
const hours = rows.map(r => r[hourColumn]);
await fs.writeFile(new URL('./source-hours.json', import.meta.url), JSON.stringify(rows.map(r => ({RiderId:String(r[0]), Hours:r[hourColumn]}))));
console.log(JSON.stringify({sheet:sheet.name,rows:rows.length,hourColumn:hourColumn+1,averageColumn:averageColumn+1,
  hoursTypes:[...new Set(hours.map(v=>typeof v))], min:Math.min(...hours),max:Math.max(...hours),
  total:hours.reduce((a,b)=>a+Number(b),0), workingDays:[...new Set(rows.map(r=>r[daysColumn]))],
  sampleHours:hours.slice(0,6),differentFromAverage:rows.filter(r=>r[hourColumn]!==r[averageColumn]).length}));
console.log((await book.inspect({kind:'table',range:`'${sheet.name.replaceAll("'","''")}'!K1:L5`,include:'values,formulas',tableMaxRows:5,tableMaxCols:2,maxChars:1600})).ndjson);
