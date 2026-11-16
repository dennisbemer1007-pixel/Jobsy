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
    r"^(Denk aan |Plots komt|Even later blijkt)",
    re.IGNORECASE,
)
FORBIDDEN_TEXT = re.compile(
    r"\bplots|denk aan|wethouder|burgemeester|rampgebied|presentator|tv-programma|tv-show",
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

RIASEC_OPPOSITES = {
    frozenset({"R", "S"}),
    frozenset({"I", "E"}),
    frozenset({"A", "C"}),
}

SCHWARTZ_OPPOSITES = {
    frozenset({SCHWARTZ["stability"], SCHWARTZ["autonomy"]}),
    frozenset({SCHWARTZ["achievement"], SCHWARTZ["connection"]}),
    frozenset({SCHWARTZ["impact"], SCHWARTZ["stability"]}),
}

ALLOWED_SECTORS = {
    "magazijn", "zorg", "winkel", "bouw", "schoonmaak", "horeca", "kantoor",
    "logistiek", "productie", "transport", "techniek", "groen", "onderwijs",
    "kinderopvang", "agrarisch", "ict", "financiën", "sport", "garage",
    "beveiliging", "evenementen", "overheid",
}

MAX_OPTION_WORD_DIFF = 6

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

ALL_COMPETENCES = set(UITGEBREID_COMPETENCES)


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


def riasec_letter(dim: str) -> str:
    return dim.strip().split()[0]


def validate_item_realism(it: Item, nr: int, bank: str) -> None:
    where = f"{bank} nr{nr}"
    assert_b1(it.situatie, f"{where} situatie")
    if it.sector not in ALLOWED_SECTORS:
        raise ValueError(f"{where}: sector {it.sector!r} niet toegestaan")
    texts = [it.situatie, it.optie_a, it.optie_b]
    for text in texts:
        if FORBIDDEN_TEXT.search(text) or GENERIC_SITUATION.match(text.strip()):
            raise ValueError(f"{where}: verboden of generieke tekst: {text!r}")
    if not it.situatie.strip().endswith((".", "!", "?")):
        raise ValueError(f"{where}: situatie moet eindigen op leesteken")

    if it.soort == "check":
        assert_b1(it.optie_a, f"{where} check")
        if it.blok != "competentie" or it.dimensie_a not in ALL_COMPETENCES:
            raise ValueError(f"{where}: check hoort bij een competentie: {it.dimensie_a!r}")
        return

    assert_b1(it.optie_a, f"{where} optie_a")
    assert_b1(it.optie_b, f"{where} optie_b")
    if word_count(it.optie_a) < 3 or word_count(it.optie_b) < 3:
        raise ValueError(f"{where}: opties moeten een volledige handeling zijn")
    if it.optie_a.strip().lower() == it.optie_b.strip().lower():
        raise ValueError(f"{where}: gelijke opties")
    if abs(word_count(it.optie_a) - word_count(it.optie_b)) > MAX_OPTION_WORD_DIFF:
        raise ValueError(f"{where}: opties niet even zwaar qua lengte: {it.optie_a!r} / {it.optie_b!r}")

    if it.blok == "interesse":
        if frozenset({riasec_letter(it.dimensie_a), riasec_letter(it.dimensie_b)}) not in RIASEC_OPPOSITES:
            raise ValueError(f"{where}: RIASEC niet tegengesteld: {it.dimensie_a} / {it.dimensie_b}")
    elif it.blok == "werkwaarden":
        if frozenset({it.dimensie_a, it.dimensie_b}) not in SCHWARTZ_OPPOSITES:
            raise ValueError(f"{where}: Schwartz niet tegengesteld: {it.dimensie_a} / {it.dimensie_b}")
    elif it.blok == "competentie":
        if it.dimensie_a != it.dimensie_b or it.dimensie_a not in ALL_COMPETENCES:
            raise ValueError(f"{where}: competentie-AB moet één dimensie hebben: {it.dimensie_a} / {it.dimensie_b}")
    else:
        raise ValueError(f"{where}: onbekend blok {it.blok!r}")


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
    opties = Counter(
        o.strip().lower() for it in items if it.soort == "ab" for o in (it.optie_a, it.optie_b)
    )
    reused = [o for o, c in opties.items() if c > 1][:5]
    if reused:
        raise SystemExit(f"{name}: hergebruikte opties: {reused}")

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