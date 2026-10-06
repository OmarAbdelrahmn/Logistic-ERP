import json
from pathlib import Path
from openpyxl import load_workbook

source = Path(r'C:\Users\omarf\Downloads\بيانات_السكن_الموحدة_ERP_جدة.xlsx')
book = load_workbook(source, data_only=True)
data = []
for sheet in book:
    rows = []
    for row in sheet:
        values = [cell.value for cell in row]
        if any(v is not None for v in values):
            rows.append({'row': row[0].row, 'values': values})
    data.append({'name': sheet.title, 'rows': rows, 'merges': [str(v) for v in sheet.merged_cells.ranges]})
dest = Path(__file__).with_name('jeddah-workbook.json')
dest.write_text(json.dumps(data, ensure_ascii=False, default=str, indent=2), encoding='utf-8')
print(json.dumps(data, ensure_ascii=False, default=str))
