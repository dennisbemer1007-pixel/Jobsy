#!/usr/bin/env python3
"""Pick 20 listable ESCO occupations (3–4 per RIASEC letter) for the approved trial."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
JOBS = json.loads((ROOT / "Jobsy.Core/Data/Occupations/occupations.nl.json").read_text())
LETTERS = ["R", "I", "A", "S", "E", "C"]
CAT = {
    "R": "buiten-groen",
    "I": "onderzoek",
    "A": "creatief",
    "S": "zorg-dieren",
    "E": "helpen",
    "C": "onderzoek",
}
WORLDS = ["zee", "tuin", "ster"]


def dominant_letter(oi):
    if not oi or len(oi) < 6:
        return None
    idx = max(range(6), key=lambda i: oi[i])
    return LETTERS[idx]


def ok(job):
    oi = job.get("oi")
    if not oi or len(oi) != 6:
        return False
    conf = (job.get("confidence") or "").lower()
    if conf not in ("high", "medium"):
        return False
    tier = (job.get("tier") or "").lower()
    if tier == "none":
        return False
    desc = job.get("desc") or ""
    if len(desc) < 80:
        return False
    return True


by_letter = {L: [] for L in LETTERS}
for job in JOBS:
    if not ok(job):
        continue
    letter = dominant_letter(job.get("oi"))
    if letter:
        by_letter[letter].append(job)

picked = []
for letter in LETTERS:
    pool = sorted(by_letter[letter], key=lambda j: (-max(j["oi"]), j["nl"]))
    # skip near-duplicates by title prefix
    seen = set()
    for job in pool:
        key = job["nl"].split()[0].lower()[:12]
        if key in seen:
            continue
        seen.add(key)
        picked.append(
            {
                "escoId": job["id"],
                "titleNl": job["nl"],
                "riasec": letter,
                "categorySlug": CAT[letter],
                "world": WORLDS[len(picked) % 3],
            }
        )
        if sum(1 for p in picked if p["riasec"] == letter) >= (4 if letter in "RI" else 3):
            break
    if len(picked) >= 20:
        break

# balance to exactly 20
while len(picked) < 20:
    for letter in LETTERS:
        for job in by_letter[letter]:
            if any(p["escoId"] == job["id"] for p in picked):
                continue
            picked.append(
                {
                    "escoId": job["id"],
                    "titleNl": job["nl"],
                    "riasec": letter,
                    "categorySlug": CAT[letter],
                    "world": WORLDS[len(picked) % 3],
                }
            )
            if len(picked) >= 20:
                break
        if len(picked) >= 20:
            break

out = ROOT / "docs/mockups/leerling/werelden/data/trial-20-manifest.json"
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps({"occupations": picked[:20]}, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
print(out)
for p in picked[:20]:
    print(p["riasec"], p["titleNl"], p["escoId"])
