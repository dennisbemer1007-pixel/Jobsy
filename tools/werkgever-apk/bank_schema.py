"""Shared schema and validation for hand-authored Loopbaan-APK A/B banks (no template mixing)."""
from __future__ import annotations

import csv
import re
from collections import Counter
from dataclasses import dataclass
from datetime import date
from pathlib import Path

BRON = "werkgever-loopbaan-apk-concept"
KOP_AB = "Wat past het best bij jou?"
KOP_CHECK = "Hoe goed past deze zin bij jou?"
GENERIC_SITUATION = re.compile(
    r"^Denk aan (je |een |uw )",
    re.IGNORECASE,
)

RIASEC = {
    "R": "R (Praktisch)",
    "I": "I (Onderzoekend)",
    "A": "A (Creatief)",
    "S": "S (Sociaal)",
    "E": "E (Ondernemend)",
    "C": "C (Geordend)",
}

SCHWARTZ = {
    "autonomy": "Werkwaarde – Zelfstandigheid (Autonomy)",
    "connection": "Werkwaarde – Verbinding (Connection)",
    "achievement": "Werkwaarde – Prestatie (Achievement)",
    "stability": "Werkwaarde – Zekerheid (Stability)",
    "impact": "Werkwaarde – Betekenis (Impact)",
}

BASIS_COMPETENCES = [
    "Samenwerken",
    "Resultaatgerichtheid",
    "Stressbestendigheid",
    "Creativiteit / vernieuwen",
    "Contact maken",
    "Communiceren",
    "Plannen & organiseren",
    "Klantgerichtheid",
    "Leren & ontwikkelen",
]

UITGEBREID_COMPETENCES = BASIS_COMPETENCES + [
    "Zelfstandig afronden",
    "Nauwkeurig werken",
    "Flexibel aanpassen",
]


@dataclass
class Item:
    blok: str
    display_blok: int
    soort: str
    situatie: str
    kop: str
    optie_a: str
    dimensie_a: str
    optie_b: str
    dimensie_b: str
    sector: str = ""


def word_count(text: str) -> int:
    return len(re.findall(r"\S+", text))


def assert_b1(text: str, label: str) -> None:
    if word_count(text) > 15:
        raise ValueError(f"{label}: meer dan 15 woorden: {text!r}")


def validate_item_realism(it: Item, nr: int, bank: str) -> None:
    assert_b1(it.situatie, f"{bank} nr{nr} situatie")
    if it.soort == "ab":
        assert_b1(it.optie_a, f"{bank} nr{nr} optie_a")
        assert_b1(it.optie_b, f"{bank} nr{nr} optie_b")
        if it.dimensie_a == it.dimensie_b:
            raise ValueError(f"{bank} nr{nr}: gelijke dimensies")
    else:
        assert_b1(it.optie_a, f"{bank} nr{nr} check")
    if GENERIC_SITUATION.match(it.situatie.strip()):
        raise ValueError(f"{bank} nr{nr}: generieke situatie")
    if not it.situatie.strip().endswith((".", "!", "?")):
        raise ValueError(f"{bank} nr{nr}: situatie moet eindigen op leesteken")


def count_coverage(items: list[Item]) -> dict[str, Counter]:
    riasec = Counter()
    comp = Counter()
    schwartz = Counter()
    ri_pairs = Counter()
    checks = 0
    ab = 0

    for it in items:
        if it.soort == "check":
            checks += 1
            comp[it.dimensie_a] += 1
            continue
        ab += 1
        if it.blok == "interesse":
            riasec[it.dimensie_a] += 1
            riasec[it.dimensie_b] += 1
            k_a = it.dimensie_a.split()[0]
            k_b = it.dimensie_b.split()[0]
            key = tuple(sorted([k_a, k_b]))
            ri_pairs[key] += 1
        elif it.blok == "competentie":
            comp[it.dimensie_a] += 1
            comp[it.dimensie_b] += 1
        elif it.blok == "werkwaarden":
            schwartz[it.dimensie_a] += 1
            schwartz[it.dimensie_b] += 1

    return {
        "riasec": riasec,
        "competentie": comp,
        "schwartz": schwartz,
        "riasec_pairs": ri_pairs,
        "meta": Counter({"ab": ab, "check": checks, "total": len(items)}),
    }


def validate_bank(name: str, items: list[Item], min_ri: int, min_comp: int, min_sw: int, comp_dims: list[str]) -> None:
    situaties = [it.situatie.strip().lower() for it in items]
    if len(situaties) != len(set(situaties)):
        dupes = [s for s, c in Counter(situaties).items() if c > 1][:5]
        raise SystemExit(f"{name}: dubbele situaties: {dupes}")

    for nr, it in enumerate(items, start=1):
        validate_item_realism(it, nr, name)

    cov = count_coverage(items)
    for dim in RIASEC.values():
        n = cov["riasec"].get(dim, 0)
        if n < min_ri:
            raise SystemExit(f"{name}: RIASEC {dim} heeft {n}, min {min_ri}")
    for dim in comp_dims:
        n = cov["competentie"].get(dim, 0)
        if n < min_comp:
            raise SystemExit(f"{name}: competentie {dim} heeft {n}, min {min_comp}")
    for dim in SCHWARTZ.values():
        n = cov["schwartz"].get(dim, 0)
        if n < min_sw:
            raise SystemExit(f"{name}: Schwartz {dim} heeft {n}, min {min_sw}")


def write_csv(path: Path, items: list[Item]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, delimiter=";", lineterminator="\n", quoting=csv.QUOTE_MINIMAL)
        w.writerow(
            [
                "nr",
                "blok",
                "display_blok",
                "soort",
                "situatie",
                "kop",
                "optie_a",
                "dimensie_a",
                "optie_b",
                "dimensie_b",
                "bron",
            ]
        )
        for nr, it in enumerate(items, start=1):
            w.writerow(
                [
                    nr,
                    it.blok,
                    it.display_blok,
                    it.soort,
                    it.situatie,
                    it.kop,
                    it.optie_a,
                    it.dimensie_a,
                    it.optie_b,
                    it.dimensie_b,
                    BRON,
                ]
            )


def write_dekking_section(name: str, items: list[Item], cov: dict[str, Counter]) -> str:
    lines = [
        f"# Dekking {name} (CONCEPT)",
        "",
        f"Handmatig geschreven items; export op {date.today().isoformat()} via `tools/werkgever-apk/export_handcrafted_banks.py`.",
        "",
        "## Samenvatting",
        "",
        f"- Totaal items: **{len(items)}**",
        f"- Forced choice (ab): **{cov['meta']['ab']}**",
        f"- Competentie-check (check): **{cov['meta']['check']}**",
        "",
        "## RIASEC (interesse)",
        "",
        "| Dimensie | Aantal meetpunten |",
        "| --- | ---: |",
    ]
    for dim, n in sorted(cov["riasec"].items()):
        lines.append(f"| {dim} | {n} |")

    lines.extend(["", "## RIASEC-paren (forced choice)", "", "| Paar | Aantal |", "| --- | ---: |"])
    for key, n in sorted(cov["riasec_pairs"].items()):
        lines.append(f"| {' – '.join(key)} | {n} |")

    lines.extend(["", "## Competenties", "", "| Dimensie | Aantal meetpunten |", "| --- | ---: |"])
    for dim, n in sorted(cov["competentie"].items()):
        lines.append(f"| {dim} | {n} |")

    lines.extend(
        ["", "## Werkwaarden (Schwartz)", "", "| Dimensie | Aantal meetpunten |", "| --- | ---: |"]
    )
    for dim, n in sorted(cov["schwartz"].items()):
        lines.append(f"| {dim} | {n} |")

    lines.extend(["", "## Blokken (display_blok)", "", "| display_blok | Aantal |", "| ---: | ---: |"])
    by_display = Counter(it.display_blok for it in items)
    for db in sorted(by_display):
        lines.append(f"| {db} | {by_display[db]} |")

    return "\n".join(lines) + "\n"