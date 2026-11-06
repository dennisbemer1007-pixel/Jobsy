#!/usr/bin/env python3
"""Validate MBO/HBO CSV banks and emit question-count manifest for tests."""
import csv
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "Jobsy.Core" / "Data" / "Scholen" / "MboHbo"

FILES = {
    "vakken_mbo": ("1a-vakkenpakket-mbo.csv", 40),
    "vakken_hbo": ("1b-vakkenpakket-hbo.csv", 40),
    "interesse": ("2-interessetest-riasec.csv", 48),
    "competentie": ("3-competentietest.csv", 40),
    "beroepen": ("4-beroepentest.csv", 50),
    "werkwaarden": ("5-werkwaarden-werkcultuur.csv", 41),
}


def read_rows(path: Path):
    text = path.read_text(encoding="utf-8-sig")
    lines = text.splitlines()
    if not lines:
        return []
    reader = csv.DictReader(lines, delimiter=";")
    return list(reader)


def main() -> int:
    ok = True
    for key, (name, expected) in FILES.items():
        path = DATA / name
        if not path.is_file():
            print(f"MISSING {path}", file=sys.stderr)
            ok = False
            continue
        rows = read_rows(path)
        if len(rows) != expected:
            print(f"COUNT {key}: expected {expected}, got {len(rows)}", file=sys.stderr)
            ok = False
        for i, row in enumerate(rows, start=1):
            q = (row.get("vraag") or "").strip()
            s = (row.get("voorbeeldscenario") or "").strip()
            if not q or not s:
                print(f"EMPTY {name} row {i}", file=sys.stderr)
                ok = False
    if ok:
        print("mbo-hbo CSV banks OK")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
