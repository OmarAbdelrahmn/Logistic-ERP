import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load('C:/Users/omarf/OneDrive/Documents/ChatGPT/Logistic ERP/outputs/iqama-platform-review-20261007/platform_accounts_iqama_corrected.xlsx'));
console.log(wb.help('worksheet.deleteRows',{include:'index,examples,notes',maxChars:2500}).ndjson);
