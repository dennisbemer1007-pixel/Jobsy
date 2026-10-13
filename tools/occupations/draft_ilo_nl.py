#!/usr/bin/env python3
"""Draft Dutch ILO task lines. Status stays 'concept, wacht op akkoord'.

Fills gaps with Argos Translate (offline). A human reviews tools/occupations/ilo_tasks_nl_review.csv.
Nothing here is shown to candidates until status is goedgekeurd.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

import argostranslate.translate

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
import build_outlook as outlook  # noqa: E402

CACHE = ROOT / ".cache" / "ilo_nl_drafts.json"
OUT = ROOT / "ilo_tasks_nl.json"
STATUS = "concept, wacht op akkoord"
FORBIDDEN = ("verdwijnt", "verdwijnen", "geen toekomst", "overbodig", "ontslag", "werkloos")


def tidy(text: str) -> str:
    cleaned = " ".join((text or "").split()).strip().rstrip(";").strip()
    if cleaned.endswith("."):
        cleaned = cleaned[:-1].strip()
    lowered = cleaned.casefold()
    replacements = {
        "werkloosheid": "mensen zonder werk",
        "werklozen": "mensen zonder werk",
        "werkloos": "zonder werk",
        "ontslag": "stoppen met het contract",
        "overbodig": "niet meer gevraagd",
        "verdwijnen": "wegvallen",
        "verdwijnt": "wegvalt",
        "geen toekomst": "onduidelijke vraag",
    }
    for source, target in replacements.items():
        if source in lowered:
            cleaned = cleaned.replace(source, target).replace(source.capitalize(), target)
            lowered = cleaned.casefold()
    for word in FORBIDDEN:
        if word in cleaned.casefold():
            cleaned = cleaned.replace(word, "dit punt")
    if cleaned:
        cleaned = cleaned[0].upper() + cleaned[1:]
    return cleaned


def translate_one(text: str) -> str:
    draft = argostranslate.translate.translate(text, "en", "nl")
    return tidy(draft or text)


def main() -> None:
    ilo = outlook.load_ilo()
    catalogue = outlook.load_catalogue()
    lines = outlook.shown_tasks(ilo, {row["isco"] for row in catalogue})
    if len(lines) != outlook.EXPECTED_TASK_LINES:
        raise SystemExit(f"expected {outlook.EXPECTED_TASK_LINES} lines, got {len(lines)}")
    raw_cache: dict[str, str] = {}
    if CACHE.exists():
        raw_cache = json.loads(CACHE.read_text(encoding="utf-8"))
    cache: dict[str, str] = {}
    for english, dutch in raw_cache.items():
        key = outlook.repair_text(english)
        value = tidy(outlook.repair_text(dutch))
        if re.sub(r"[^a-z0-9]+", "", key.casefold()) == re.sub(r"[^a-z0-9]+", "", value.casefold()):
            continue
        cache[key] = value
    pending = sorted({line["en"] for line in lines} - set(cache))
    print(f"cached {len(cache)} pending {len(pending)}", flush=True)
    CACHE.parent.mkdir(parents=True, exist_ok=True)
    for done, english in enumerate(pending, start=1):
        cache[english] = translate_one(english)
        if done % 100 == 0 or done == len(pending):
            CACHE.write_text(json.dumps(cache, ensure_ascii=False), encoding="utf-8")
            print(f"translated {done}/{len(pending)}", flush=True)
    rows = []
    for line in lines:
        rows.append(
            {
                "isco": line["isco"],
                "taskID": line["taskID"],
                "en": line["en"],
                "nl": cache[line["en"]],
                "status": STATUS,
                "reviewedBy": "",
                "reviewedOn": "",
            }
        )
    rows.sort(key=lambda row: (row["isco"], row["taskID"]))
    OUT.write_text(json.dumps(rows, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {OUT} ({len(rows)} lines)")


if __name__ == "__main__":
    main()
