#!/usr/bin/env python3
"""Emit DEEP_*_CONSTRUCT_MATRIX.md from DeepAnalysis*Items.cs (NL reference stems)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
CORE = ROOT / "Jobsy.Core" / "Rules"
DOCS = ROOT / "docs" / "psychometrie"

SPECS = [
    (
        "COMPETENCE_DEEP_CONSTRUCT_MATRIX.md",
        "DeepAnalysisCompetenceItems.cs",
        "Competentie diepteanalyse (Big Five / IPIP-NEO facetten)",
        "Holland/IPIP-NEO workplace adaptatie; 150 items, 5 traits × 6 facetten × 5 stems. "
        "Bron inspiratie: publieke IPIP/O*NET domain descriptions (geen gekopieerde items). "
        "Betrouwbaarheid/normen: **nog te bepalen met pilotdata (n≈300)**.",
    ),
    (
        "RIASEC_DEEP_CONSTRUCT_MATRIX.md",
        "DeepAnalysisCareerItems.cs",
        "RIASEC diepteanalyse (Holland)",
        "200 items, ~33–34 per type (R-I-A-S-E-C). Theorie: Holland (1997) vocational interests. "
        "Betrouwbaarheid/normen: **nog te bepalen met pilotdata (n≈300)**.",
    ),
    (
        "VALUES_DEEP_CONSTRUCT_MATRIX.md",
        "DeepAnalysisValuesItems.cs",
        "Werkwaarden diepteanalyse (Schwartz workplace drivers)",
        "150 items, 30 per driver (Autonomie, Verbinding, Prestatie, Stabiliteit, Impact). "
        "Betrouwbaarheid/normen: **nog te bepalen met pilotdata (n≈300)**.",
    ),
    (
        "CULTURE_DEEP_CONSTRUCT_MATRIX.md",
        "DeepAnalysisCultureItems.cs",
        "Cultuur & persoonlijkheid diepteanalyse",
        "150 items: 6 cultuurdimensies × 15 + 5 persoonlijkheidsfacetten × 12 (OCP-achtig + Big Five werkfacetten). "
        "Betrouwbaarheid/normen: **nog te bepalen met pilotdata (n≈300)**.",
    ),
]

ITEM_RE = re.compile(
    r"\(\s*(?:[\w.\"]+)\s*,\s*(true|false)\s*,\s*\"([^\"]+)\"\s*\)",
    re.MULTILINE,
)
DOMAIN_RE = re.compile(r"[\w.]+Catalog\.(\w+)|\"([^\"]+)\"|\b(Openheid|Consciëntieusheid|Extraversie|Vriendelijkheid|EmotioneleStabiliteit|Autonomy|Connection|Achievement|Stability|Impact)\b")


def domain_from_token(token: str) -> str:
    m = re.search(r"\.(\w+)\s*$", token)
    if m:
        return m.group(1)
    return token.strip('"')


def parse_items(path: Path) -> list[tuple[str, bool, str]]:
    text = path.read_text(encoding="utf-8")
    current_domain = "?"
    out: list[tuple[str, bool, str]] = []
    for line in text.splitlines():
        dm = re.search(r"//\s*(\w[\w\s&]+)\s*—", line)
        if dm:
            current_domain = dm.group(1).strip()
        for rev, prompt in ITEM_RE.findall(line):
            if "Domains" in prompt:
                continue
            out.append((current_domain, rev == "true", prompt))
    return out


def write_matrix(filename: str, cs_name: str, title: str, intro: str) -> None:
    items = parse_items(CORE / cs_name)
    lines = [
        f"# {title}",
        "",
        f"**Catalogus:** `Jobsy.Core/Rules/{cs_name}` · **Runtime:** `DeepAnalysisCatalog`",
        f"**Vertalingen:** `Jobsy.Core/Data/DeepItems/{{kind}}-{{lang}}.json` (nl = embedded stems)",
        "",
        intro,
        "",
        "**v3 free tier (25):** blijft v2-subset (`QuickScanFreeTierV3`); diep-scores zijn **andere itembank** "
        "maar zelfde domeinen/schalen → vergelijkbare interpretatie, geen stille herberekening Quick-Scan.",
        "",
        "| Id | Domein | Keying | NL stem (referentie) |",
        "|----|--------|--------|----------------------|",
    ]
    for i, (domain, rev, prompt) in enumerate(items, start=1):
        key = "R" if rev else "F"
        safe = prompt.replace("|", "\\|")
        lines.append(f"| {i} | {domain} | {key} | {safe} |")
    lines.extend(["", "**Legenda:** F = forward; R = reverse (`6 −` ruwe score).", ""])
    (DOCS / filename).write_text("\n".join(lines), encoding="utf-8")
    print(f"Wrote {(DOCS / filename)} ({len(items)} rows)")


def main() -> None:
    for spec in SPECS:
        write_matrix(*spec)


if __name__ == "__main__":
    main()
