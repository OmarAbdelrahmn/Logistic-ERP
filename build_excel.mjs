import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const data = JSON.parse(await fs.readFile("table_data.json", "utf8"));
const outDir = "outputs/administrative_appeals";
await fs.mkdir(outDir, { recursive: true });

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("الاعتراضات الإدارية");
const source = workbook.worksheets.add("المصدر");

const matrix = [data.headers, ...data.rows];
sheet.getRange(`A1:H${matrix.length}`).values = matrix;
sheet.showGridLines = false;
sheet.freezePanes.freezeRows(1);

const all = sheet.getRange(`A1:H${matrix.length}`);
all.format.font = { name: "Arial", size: 11, color: "#222222" };
all.format.verticalAlignment = "center";
all.format.wrapText = true;
all.format.borders = { preset: "all", style: "thin", color: "#B7B7B7" };

const header = sheet.getRange("A1:H1");
header.format = {
  fill: "#D9D9D9",
  font: { name: "Arial", size: 11, bold: true, color: "#000000" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
  wrapText: true,
  borders: { preset: "all", style: "thin", color: "#808080" },
};
header.format.rowHeight = 38;

sheet.getRange(`A2:H${matrix.length}`).format.horizontalAlignment = "center";
sheet.getRange(`A2:H${matrix.length}`).format.rowHeight = 24;
sheet.getRange(`A2:H${matrix.length}`).format.font = { name: "Arial", size: 11, color: "#222222" };
sheet.getRange(`A1:H${matrix.length}`).format.columnWidth = 18;
sheet.getRange("A:A").format.columnWidth = 18;
sheet.getRange("B:B").format.columnWidth = 20;
sheet.getRange("C:C").format.columnWidth = 24;
sheet.getRange("D:D").format.columnWidth = 22;
sheet.getRange("E:E").format.columnWidth = 22;
sheet.getRange("F:G").format.columnWidth = 18;
sheet.getRange("H:H").format.columnWidth = 19;

const table = sheet.tables.add(`A1:H${matrix.length}`, true, "AdministrativeAppealsTable");
table.style = "TableStyleMedium2";
table.showFilterButton = true;

source.getRange("A1:B4").values = [
  ["المصدر", "جدول اعتراضات الادارية محدث 21.pdf"],
  ["المسار", "C:\\Users\\omarf\\Downloads\\جدول اعتراضات الادارية محدث 21.pdf"],
  ["عدد الصفوف", data.rows.length],
  ["ملاحظة", "تم نقل البيانات كما ظهرت في الجدول، مع إبقاء التواريخ والأرقام كنصوص للحفاظ على شكلها."],
];
source.showGridLines = false;
source.getRange("A1:B4").format.font = { name: "Arial", size: 11, color: "#222222" };
source.getRange("A1:A4").format.font = { name: "Arial", size: 11, bold: true, color: "#000000" };
source.getRange("A1:B4").format.wrapText = true;
source.getRange("A1:B4").format.verticalAlignment = "center";
source.getRange("A1:B4").format.borders = { preset: "all", style: "thin", color: "#B7B7B7" };
source.getRange("A:A").format.columnWidth = 18;
source.getRange("B:B").format.columnWidth = 75;
source.getRange("A1:B4").format.rowHeight = 28;
source.freezePanes.freezeRows(1);

const check = await workbook.inspect({
  kind: "table",
  range: `الاعتراضات الإدارية!A1:H6`,
  include: "values,formulas",
  tableMaxRows: 6,
  tableMaxCols: 8,
});
console.log(check.ndjson);
const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!",
  options: { useRegex: true, maxResults: 100 },
  summary: "final formula error scan",
});
console.log(errors.ndjson);
const preview = await workbook.render({ sheetName: "الاعتراضات الإدارية", range: "A1:H18", scale: 1, format: "png" });
await fs.writeFile(`${outDir}/preview.png`, new Uint8Array(await preview.arrayBuffer()));

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(`${outDir}/جدول_الاعتراضات_الإدارية.xlsx`);
console.log(`saved ${outDir}/جدول_الاعتراضات_الإدارية.xlsx`);
