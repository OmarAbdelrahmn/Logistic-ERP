import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const source = 'C:/Users/omarf/Downloads/دليل_معرفات_وحسابات_التطبيقات_الموحد_2026-10-07.xlsx';
const wb = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
for (const [sheetName, range, name] of [
  ['لوحة التحكم وملخص التطبيقات', 'A15:F21', 'summary-before.png'],
  ['ذا شيفز - The Chefsz', 'A27:F35', 'chefsz-before.png'],
]) {
  const p = await wb.render({sheetName, range, scale: 1.5, format: 'png'});
  await fs.writeFile(new URL(name, import.meta.url), new Uint8Array(await p.arrayBuffer()));
  console.log(`rendered ${name}`);
}
const o = await wb.inspect({kind:'region',sheetId:'ذا شيفز - The Chefsz',range:'A29:F33',maxChars:1300});
console.log(o.ndjson);
