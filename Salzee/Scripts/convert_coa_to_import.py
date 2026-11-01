"""Convert COA.csv (client format) to coa-import.csv (AMS AmsCoa import format)."""
import csv
import re
import sys
from pathlib import Path

ZERO_BY_INDEX = {1: "00", 2: "000", 3: "0000", 4: "00000"}
# Leading digit -> acc.AccountCategoryID (seeded 1–5). Suspense (9) mapped to Liability.
CAT_MAP = {"1": 1, "2": 2, "3": 3, "4": 4, "5": 5, "9": 2}
CAT_NAME = {1: "Asset", 2: "Liability", 3: "Equity", 4: "Revenue", 5: "Expense"}


def parent_code(code: str, level: int) -> str:
    if level <= 1:
        return ""
    parts = code.strip().split("-")
    if len(parts) != 5:
        return ""
    idx = level - 1
    if idx < 1 or idx > 4:
        return ""
    parts[idx] = ZERO_BY_INDEX[idx]
    for i in range(idx + 1, 5):
        parts[i] = ZERO_BY_INDEX[i]
    return "-".join(parts)


def category_id(code: str) -> int:
    m = re.match(r"^(\d)", code.strip())
    if not m:
        return 1
    return CAT_MAP.get(m.group(1), 1)


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    src = root / "COA.csv"
    dst = root / "wwwroot" / "templates" / "ams" / "coa-import.csv"
    dst_ams = root / "wwwroot" / "templates" / "ams" / "coa-import-ams.csv"
    if len(sys.argv) >= 2:
        src = Path(sys.argv[1])
    if len(sys.argv) >= 3:
        dst = Path(sys.argv[2])

    rows_client: list[dict[str, str]] = []
    rows_ams: list[dict[str, str]] = []
    errors: list[str] = []

    with src.open(newline="", encoding="utf-8-sig") as f:
        reader = csv.DictReader(f)
        for line_no, row in enumerate(reader, start=2):
            sno = (row.get("S No") or "").strip()
            code = (row.get("COA Code") or "").strip()
            name = (row.get("Narration") or "").strip()
            lvl_s = (row.get("Level #") or "").strip()
            if not code or not lvl_s:
                errors.append(f"Line {line_no}: missing COA Code or Level #")
                continue
            try:
                level = int(lvl_s)
            except ValueError:
                errors.append(f"Line {line_no}: invalid level {lvl_s!r}")
                continue
            if level < 1 or level > 5:
                errors.append(f"Line {line_no}: level {level} out of range 1–5")
                continue

            cat = category_id(code)
            cat_name = CAT_NAME.get(cat, "Asset")
            parent = parent_code(code, level)

            rows_client.append(
                {
                    "S No": sno or str(len(rows_client)),
                    "COA Code": code,
                    "Narration": name,
                    "Level #": str(level),
                    "Account Category": cat_name,
                }
            )
            rows_ams.append(
                {
                    "AccountCode": code,
                    "AccountName": name,
                    "AccountCategoryID": str(cat),
                    "ParentAccountCode": parent,
                    "AccountLevel": str(level),
                    "IsControlAccount": "1" if level < 5 else "0",
                    "AllowDirectPosting": "1" if level >= 5 else "0",
                }
            )

    parent_codes = {r["AccountCode"] for r in rows_ams}
    missing_parents = [
        (r["AccountCode"], r["ParentAccountCode"])
        for r in rows_ams
        if r["ParentAccountCode"] and r["ParentAccountCode"] not in parent_codes
    ]

    dst.parent.mkdir(parents=True, exist_ok=True)

    client_fields = ["S No", "COA Code", "Narration", "Level #", "Account Category"]
    with dst.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=client_fields, lineterminator="\n")
        w.writeheader()
        w.writerows(rows_client)

    ams_fields = [
        "AccountCode",
        "AccountName",
        "AccountCategoryID",
        "ParentAccountCode",
        "AccountLevel",
        "IsControlAccount",
        "AllowDirectPosting",
    ]
    with dst_ams.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=ams_fields, lineterminator="\n")
        w.writeheader()
        w.writerows(rows_ams)

    print(f"Wrote {len(rows_client)} rows -> {dst}")
    print(f"Wrote {len(rows_ams)} rows -> {dst_ams} (AMS upload)")
    if errors:
        print(f"Warnings ({len(errors)}):")
        for e in errors[:20]:
            print(f"  {e}")
    if missing_parents:
        print(f"Missing parent codes ({len(missing_parents)}) — check COA hierarchy:")
        for c, p in missing_parents[:20]:
            print(f"  {c} parent {p}")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
