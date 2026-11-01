import re
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "Views"
pat = re.compile(r'<partial name="_AmsExportCsv" model="(\w+)"')
for p in root.rglob("*.cshtml"):
    text = p.read_text(encoding="utf-8")
    new = pat.sub(r'<partial name="_AmsExportCsv" model="@("\1")"', text)
    if new != text:
        p.write_text(new, encoding="utf-8")
        print(p.relative_to(root))
