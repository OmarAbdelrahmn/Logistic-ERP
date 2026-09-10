import json
import sys
import unicodedata
import pdfplumber

pdf_path, out_path = sys.argv[1:3]

def has_arabic_letters(s):
    return any(("ء" <= c <= "ي") or ("\uFB50" <= c <= "\uFDFF") or ("\uFE70" <= c <= "\uFEFF") for c in s)

def clean_cell(s):
    if not s:
        return ""
    s = s.strip()
    if has_arabic_letters(s):
        s = unicodedata.normalize("NFKC", s)[::-1]
        s = s.replace("االعRSاض", "الاعتراض")
        s = s.replace("االعRSاض", "الاعتراض")
        s = s.replace("امل", "ال")
        s = s.replace("الالكي", "المالكي")
        s = s.replace("ھ", "ه")
        s = s.replace("ﺔ", "ة")
    return s.strip()

with pdfplumber.open(pdf_path) as pdf:
    first_table = pdf.pages[0].find_tables()[0]
    first_row_cells = first_table.rows[0].cells
    broad = [c for c in first_row_cells if c and (c[2] - c[0]) > 20]
    boundaries = [broad[0][0]] + [c[2] for c in broad]
    left, right = boundaries[0], boundaries[-1]
    fractions = [(x - left) / (right - left) for x in boundaries]

    rows = []
    for page_index, page in enumerate(pdf.pages):
        table = page.find_tables()[0]
        for row_index, row in enumerate(table.rows):
            if page_index == 0 and row_index < 2:
                continue
            y0, y1 = row.bbox[1], row.bbox[3]
            page_left, page_right = table.bbox[0], table.bbox[2]
            page_bounds = [page_left + f * (page_right - page_left) for f in fractions]
            cells = [[] for _ in range(8)]
            for word in page.extract_words():
                if not (y0 <= word["top"] < y1):
                    continue
                center = (word["x0"] + word["x1"]) / 2
                col = None
                for i in range(8):
                    if page_bounds[i] <= center < page_bounds[i + 1] or (i == 7 and center <= page_bounds[i + 1]):
                        col = i
                        break
                if col is not None:
                    cells[col].append((word["x0"], word["text"]))
            values = []
            for words in cells:
                words.sort(key=lambda x: x[0])
                values.append(clean_cell(" ".join(w[1] for w in words)))
            if any(values):
                rows.append(values)

headers = ["موعد الجلسة", "رقم جلسة الاستئناف", "ملاحظات", "الحكم", "من حساب المحامي", "موعد تسليم الحكم", "تاريخ الحكم", "رقم الدعوى"]
with open(out_path, "w", encoding="utf-8") as f:
    json.dump({"headers": headers, "rows": rows}, f, ensure_ascii=False, indent=2)
print(f"rows={len(rows)}")
