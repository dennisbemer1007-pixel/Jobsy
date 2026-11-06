#!/usr/bin/env python3
"""Fail if any trial screen-13 HTML has non-ascending clock labels."""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "docs/mockups/leerling/werelden/data/trial-20-manifest.json"
SRC = ROOT / "docs/mockups/leerling/werelden/src"
EXPORT = ROOT / "docs/mockups/leerling/werelden/data/occupation-day-in-life.nl.json"

CLOCK = re.compile(r"<h2>(\d{1,2}:\d{2})\s")


def parse_minutes(t: str) -> int:
    h, m = t.split(":")
    return int(h) * 60 + int(m)


def main() -> int:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    export = json.loads(EXPORT.read_text(encoding="utf-8"))
    trial_ids = {o["escoId"].lower() for o in manifest["occupations"]}
    titles = {o["escoId"].lower(): o["titleNl"] for o in manifest["occupations"]}
    by_id = {r["escoId"].lower(): r for r in export.get("rows") or []}

    errors = []
    for esco, title in titles.items():
        if esco not in by_id:
            errors.append(f"missing export row: {title}")
            continue
        blocks = by_id[esco].get("blocks") or []
        times = []
        for b in blocks:
            lab = b.get("label") or ""
            m = re.match(r"^(\d{1,2}:\d{2})", lab.strip())
            if m:
                times.append(m.group(1))
        for i in range(1, len(times)):
            if parse_minutes(times[i]) <= parse_minutes(times[i - 1]):
                errors.append(f"{title}: times not ascending {times}")

    html_files = list(SRC.glob("13-*-beroep-dag-*.html"))
    for fp in html_files:
        text = fp.read_text(encoding="utf-8")
        times = CLOCK.findall(text)
        for i in range(1, len(times)):
            if parse_minutes(times[i]) <= parse_minutes(times[i - 1]):
                errors.append(f"{fp.name}: {times}")
            delta = parse_minutes(times[i]) - parse_minutes(times[i - 1])
            if delta > 120:
                errors.append(f"{fp.name}: gap {delta} min between {times[i-1]} and {times[i]}")

    if errors:
        print("\n".join(errors))
        return 1
    print(f"ok: {len(html_files)} screen-13 html files, {len(trial_ids)} trial occupations checked")
    return 0


if __name__ == "__main__":
    sys.exit(main())
