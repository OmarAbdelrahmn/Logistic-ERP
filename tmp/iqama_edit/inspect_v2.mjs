import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load('C:/Users/omarf/OneDrive/Documents/ChatGPT/Logistic ERP/outputs/iqama-platform-review-20261007/platform_accounts_iqama_corrected.xlsx'));
for (const [sheetName,range,name] of [
  ['جاهز - Jahez','A22:F30','jahez-before-v2.png'],
  ['هنقرستيشن - HungerStation','A265:F273','hunger-before-v2.png'],
]) {
 const img=await wb.render({sheetName,range,scale:1.5,format:'png'});
 await fs.writeFile(new URL(name,import.meta.url),new Uint8Array(await img.arrayBuffer()));
 console.log(name);
}
