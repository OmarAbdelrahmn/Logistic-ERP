import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
const book = await SpreadsheetFile.importXlsx(await FileBlob.load('C:/Users/omarf/Downloads/بيانات_السكن_الموحدة_ERP_جدة.xlsx'));
console.log((await book.inspect({kind:'table',range:"'بيانات الرفع لنظام ERP'!A1:O10",tableMaxRows:10,tableMaxCols:15,maxChars:5000})).ndjson);
const preview = await book.render({sheetName:'ملخص وتقارير الإشغال',range:'A4:J8',scale:1.5,format:'png'});
await fs.writeFile('.codex-temp/jeddah-source-summary.png',new Uint8Array(await preview.arrayBuffer()));
