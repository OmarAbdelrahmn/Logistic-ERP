import fs from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';
const out=new URL('../outputs/ninja-comparison-20261022/',import.meta.url);
const d=JSON.parse(await fs.readFile(new URL('comparison.json',out),'utf8'));
const wb=Workbook.create();
const summary=wb.worksheets.add('Summary');
const detail=wb.worksheets.add('Comparison');
for(const s of [summary,detail]) {s.showGridLines=false;s.tabColor='#243B53';}
summary.getRange('A2').values=[['Ninja account comparison']];
summary.getRange('A4:B13').values=[
 ['Comparison result','Accounts'],['Workbook accounts',d.sheetRows],['Database accounts',d.dbRows],
 ['Present in both',300],['Matching compared details',d.counts.Matching],['Different details',d.counts['Different details']],
 ['Workbook only',d.counts['Sheet only']],['Database only',d.counts['Database only']],
 ['Name-only differences',216],['City differences',20]];
summary.getRange('D4:H7').values=[['Source sheet','Rows','Matching','Different','Workbook only'],...Object.entries(d.sheets).map(([k,v])=>[k,v.Rows,v.Matching||0,v['Different details']||0,v['Sheet only']||0])];
summary.getRange('D10:E14').values=[['Provider flags','Accounts'],['ACTIVE',171],['NAQL_BLOCKED',157],['Suspended',111],['Invalid owner ID',1]];
const notes=[
 ['Compared at','22 October 2026, 17:34 Riyadh'],
 ['Matching method','Captain ID; compare owner ID, Arabic/English name, account city and sponsor registry.'],
 ['Name comparison','Ignores case and repeated whitespace; spelling differences remain flagged.'],
 ['Owner IDs and registries','All 300 shared accounts match on owner ID and sponsor registry.'],
 ['Invalid source owner ID','البوابة row 131 / Captain 66689: nine-digit ID 257766066. Kept as supplied.'],
 ['Provider status','NAQL_BLOCKED and suspension are independent flags; no direct ERP status mapping.'],
 ['Vehicle comparison','Vehicle type is shown from the workbook; no equivalent ERP account field.'],
 ['Scope','All non-deleted Ninja accounts, including archived. Database and source unchanged.'],
 ['Source workbook','رفع نينجا.xlsx; all three worksheets.'],
 ['Database source','Configured ERP database db67927; snapshot 2026-10-22 14:34:08 UTC.'],
 ['Source SHA-256',d.sha256]
];
summary.getRange('A17:B27').values=notes;
summary.getRange('A2:H27').format.font={name:'Arial',size:11,color:'#243B53'};
summary.getRange('A2').format.font={name:'Arial',size:16,bold:true};
summary.getRange('A:A').format.columnWidth=34;
summary.getRange('B:B').format.columnWidth=22;
summary.getRange('C:C').format.columnWidth=3;
summary.getRange('D:D').format.columnWidth=24;
summary.getRange('E:H').format.columnWidth=15;
summary.getRange('A4:H14').format.rowHeight=25;
for(let i=17;i<=27;i++){summary.mergeCells(`B${i}:H${i}`);summary.getRange(`B${i}:H${i}`).format.wrapText=true;}
summary.getRange('A17:H27').format.rowHeight=36;
const priority={'Sheet only':0,'Database only':1,'Different details':2,'Matching':3};
const rows=[...d.detail].sort((a,b)=>priority[a[0]]-priority[b[0]]||String(a[3]).localeCompare(String(b[3])));
detail.getRange('A2').values=[['Ninja accounts — side-by-side comparison']];
detail.getRange('A4:S4').values=[d.headers];
detail.getRange(`A5:S${rows.length+4}`).values=rows;
detail.getRange(`A2:S${rows.length+4}`).format.font={name:'Arial',size:11,color:'#243B53'};
detail.getRange('A2').format.font={name:'Arial',size:16,bold:true};
detail.getRange(`A5:S${rows.length+4}`).format.rowHeight=32;
detail.getRange('A:S').format.columnWidth=20;
detail.getRange('C:C').format.columnWidth=12;
detail.getRange('E:G').format.columnWidth=42;
detail.getRange('J:K').format.columnWidth=18;
detail.getRange('N:Q').format.columnWidth=22;
detail.getRange('S:S').format.columnWidth=26;
detail.getRange(`E4:G${rows.length+4}`).format.wrapText=true;
detail.getRange('A4:S4').format.wrapText=true;
detail.getRange('A4:S4').format.rowHeight=38;
detail.freezePanes.freezeRows(4);detail.freezePanes.freezeColumns(4);
detail.tables.add(`A4:S${rows.length+4}`,true,'NinjaComparison');
for(const [s,ranges] of [[summary,['A4:B4','D4:H4','D10:E10']],[detail,['A4:S4']]])for(const r of ranges)s.getRange(r).format={fill:'#243B53',font:{name:'Arial',size:11,bold:true,color:'#FFFFFF'},horizontalAlignment:'center',verticalAlignment:'center'};
detail.getRange(`A5:A${rows.length+4}`).conditionalFormats.add('containsText',{text:'only',format:{fill:'#FFF0CC',font:{color:'#8A5300',bold:true}}});
wb.recalculate();
console.log((await wb.inspect({kind:'table',range:'Summary!A4:B13',include:'values',tableMaxRows:10,tableMaxCols:2,maxChars:1800})).ndjson);
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!',options:{useRegex:true,maxResults:20},maxChars:1000})).ndjson);
for(const [s,range,file] of [['Summary','A1:H27','summary.png'],['Comparison','A1:G12','detail.png']]){
 const png=await wb.render({sheetName:s,range,scale:1.4,format:'png'});
 await fs.writeFile(new URL(file,out),new Uint8Array(await png.arrayBuffer()));
}
await (await SpreadsheetFile.exportXlsx(wb)).save(fileURLToPath(new URL('Ninja-account-comparison.xlsx',out)));
