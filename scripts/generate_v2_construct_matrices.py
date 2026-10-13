#!/usr/bin/env python3
"""Generate construct matrix markdown for v2 instruments (NL stems from merged UiStrings via regex from bank+supplement+base)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def load_nl_stems() -> dict[str, str]:
    stems: dict[str, str] = {}
    for path in [
        ROOT / "Jobsy.Web/Localization/UiStringsAssessmentV2.cs",
        ROOT / "Jobsy.Web/Localization/UiStringsAssessmentV2Supplement.cs",
        ROOT / "Jobsy.Web/Localization/UiStringsAssessmentV2ValuesCareerBank.cs",
    ]:
        text = path.read_text(encoding="utf-8")
        for m in re.finditer(r'\["(AssessmentV2\.[^"]+)"\]\s*=\s*"([^"]*)"', text):
            stems[m.group(1)] = m.group(2)
    return stems


def parse_competence_rows() -> list[tuple[int, str, bool, str]]:
    text = (ROOT / "Jobsy.Core/Rules/CompetencyTestCatalogV2.cs").read_text(encoding="utf-8")
    rows = []
    for m in re.finditer(
        r"new\((\d+),\s*(\w+),\s*Reverse:\s*(true|false),\s*\"(AssessmentV2\.Competency\.Q\d+)\"\)",
        text,
    ):
        rows.append((int(m.group(1)), m.group(2), m.group(3) == "true", m.group(4)))
    return rows


def parse_culture_rows() -> list[tuple[int, str, bool, str]]:
    text = (ROOT / "Jobsy.Core/Rules/CulturePersonalityCatalogV2.cs").read_text(encoding="utf-8")
    rows = []
    for m in re.finditer(
        r"\(\s*(\w+),\s*(true|false),\s*\"(AssessmentV2\.Culture\.Q\d+)\"\s*\)",
        text,
    ):
        cat, rev, key = m.group(1), m.group(2) == "true", m.group(3)
        rows.append((0, cat, rev, key))
    # assign ids 1..48 in file order
    return [(i + 1, cat, rev, key) for i, (_, cat, rev, key) in enumerate(rows)]


def parse_career_rows() -> list[tuple[int, str, bool, str]]:
    text = (ROOT / "Jobsy.Core/Rules/CareerTestCatalogV2.cs").read_text(encoding="utf-8")
    rows = []
    for m in re.finditer(
        r"new\((\d+),\s*(\w+),\s*Reverse:\s*(true|false),\s*\"(AssessmentV2\.Career\.Q\d+)\"\)",
        text,
    ):
        rows.append((int(m.group(1)), m.group(2), m.group(3) == "true", m.group(4)))
    return rows


def write_matrix(path: Path, title: str, intro: str, rows: list[tuple[int, str, bool, str]], stems: dict[str, str]) -> None:
    lines = [
        f"# {title}",
        "",
        intro,
        "",
        "| Id | Key | Construct | Keying | NL stem (referentie) |",
        "|----|-----|-----------|--------|----------------------|",
    ]
    for id_, cat, rev, key in rows:
        k = "R" if rev else "F"
        stem = stems.get(key, "—")
        stem = stem.replace("|", "\\|")
        lines.append(f"| {id_} | {key} | {cat} | {k} | {stem} |")
    lines.extend(["", "**Legenda:** F = forward; R = reverse (`6 −` ruwe score).", ""])
    path.write_text("\n".join(lines), encoding="utf-8")
    print("Wrote", path)


def main() -> None:
    stems = load_nl_stems()
    docs = ROOT / "docs/psychometrie"
    write_matrix(
        docs / "COMPETENCE_V2_CONSTRUCT_MATRIX.md",
        "Competentie-scan v2 — construct matrix",
        "**Catalogus:** `CompetencyTestCatalogV2.cs`. **Teksten:** supplement + base merge.",
        parse_competence_rows(),
        stems,
    )
    write_matrix(
        docs / "CULTURE_V2_CONSTRUCT_MATRIX.md",
        "Cultuur-scan v2 — construct matrix",
        "**Catalogus:** `CulturePersonalityCatalogV2.cs` (12 dimensies × 4). **Curiosity** scored as Openness.",
        parse_culture_rows(),
        stems,
    )
    write_matrix(
        docs / "RIASEC_V2_CONSTRUCT_MATRIX.md",
        "Carrière-scan v2 (RIASEC) — construct matrix",
        "**Catalogus:** `CareerTestCatalogV2.cs`. **Teksten:** `UiStringsAssessmentV2ValuesCareerBank.cs`.",
        parse_career_rows(),
        stems,
    )


if __name__ == "__main__":
    main()
