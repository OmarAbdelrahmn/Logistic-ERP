import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
const path='C:/Users/omarf/OneDrive/Documents/ChatGPT/Logistic ERP/outputs/iqama-platform-review-20261007-v2/platform_accounts_upload_review.xlsx';
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(path));
for (const [range,name] of [['A1:G10','review-v2-top.png'],['A51:G56','review-v2-bottom.png']]) {
 const blob=await wb.render({sheetName:'مراجعة قبل الرفع',range,scale:1,format:'png'});
 await fs.writeFile(new URL(name,import.meta.url),new Uint8Array(await blob.arrayBuffer()));
 console.log(name);
}
