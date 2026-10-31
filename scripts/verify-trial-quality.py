#!/usr/bin/env python3
"""Quality gate for trial-20 export + screen-13 HTML (B1, chips, times)."""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "docs/mockups/leerling/werelden/data/trial-20-manifest.json"
EXPORT = ROOT / "docs/mockups/leerling/werelden/data/occupation-day-in-life.nl.json"
SRC = ROOT / "docs/mockups/leerling/werelden/src"

FORBIDDEN = [
    "specimens",
    "specimen",
    "organismen",
    "organisme",
    "wetenschappelijke papers",
    "wetenschappelijke gemeenschap",
    "hypothese",
    "methodologie",
    "stakeholder",
    "implementeren",
    "faciliteren",
    "compliance",
]

CLOCK = re.compile(r"<h2>(\d{1,2}:\d{2})\s")


def parse_minutes(t: str) -> int:
    h, m = t.split(":")
    return int(h) * 60 + int(m)


def scan_text(text: str, ctx: str, errors: list) -> None:
    low = text.lower()
    for w in FORBIDDEN:
        if w in low:
            errors.append(f"{ctx}: forbidden '{w}'")


def main() -> int:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    export = json.loads(EXPORT.read_text(encoding="utf-8"))
    trial = manifest["occupations"]
    by_id = {r["escoId"].lower(): r for r in export.get("rows") or []}
    errors = []

    for occ in trial:
        eid = occ["escoId"].lower()
        title = occ["titleNl"]
        row = by_id.get(eid)
        if not row:
            errors.append(f"missing export row: {title}")
            continue
        for field in ("morning", "midday", "afternoon", "closing", "variesNote"):
            scan_text(str(row.get(field) or ""), f"{title}.{field}", errors)
        for b in row.get("blocks") or []:
            scan_text(str(b.get("text") or ""), f"{title}.block.{b.get('key')}", errors)
        skills = row.get("skills") or row.get("highlights") or []
        for s in skills[:6]:
            scan_text(str(s), f"{title}.skill", errors)
            if len(str(s)) > 55:
                errors.append(f"{title}.skill too long: {s[:60]}…")

    slug_map = {occ["escoId"].lower(): re.sub(r"[^a-z0-9]+", "-", occ["titleNl"].lower()).strip("-") for occ in trial}
    for world in ("zee", "eilandtuin", "sterrenkust"):
        for eid, slug in slug_map.items():
            fp = SRC / f"13-{world}-beroep-dag-{slug}.html"
            if not fp.exists():
                errors.append(f"missing html {fp.name}")
                continue
            text = fp.read_text(encoding="utf-8")
            if "Lobsy<small>" not in text and ">Lobsy<" not in text:
                errors.append(f"{fp.name}: header not Lobsy")
            if "Jobsy<small>" in text:
                errors.append(f"{fp.name}: header wrongly Jobsy")
            times = CLOCK.findall(text)
            for i in range(1, len(times)):
                if parse_minutes(times[i]) <= parse_minutes(times[i - 1]):
                    errors.append(f"{fp.name}: times {times}")
            scan_text(text, fp.name, errors)

    if errors:
        print("\n".join(errors[:80]))
        if len(errors) > 80:
            print(f"… and {len(errors) - 80} more")
        return 1
    print(f"ok: {len(trial)} occupations, quality checks passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
