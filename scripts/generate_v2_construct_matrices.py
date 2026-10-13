#!/usr/bin/env python3
"""Generate construct matrix markdown for v2 instruments (NL stems mirroring UiStrings merge order)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
KEY_VAL = re.compile(r'\["(AssessmentV2\.[^"]+)"\]\s*=\s*"((?:\\.|[^"\\])*)"')


def extract_lang_method(text: str, method: str) -> str:
    """Return body of `private static Dictionary<string, string> {method}()` block."""
    needle = f"private static Dictionary<string, string> {method}()"
    start = text.find(needle)
    if start < 0:
        return ""
    brace = text.find("{", start)
    if brace < 0:
        return ""
    depth = 0
    for i in range(brace, len(text)):
        ch = text[i]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1 : i]
    return ""


def parse_key_values(block: str) -> dict[str, str]:
    out: dict[str, str] = {}
    for m in KEY_VAL.finditer(block):
        key = m.group(1)
        val = m.group(2).encode("utf-8").decode("unicode_escape") if "\\" in m.group(2) else m.group(2)
        out[key] = val
    return out


def load_nl_stems() -> dict[str, str]:
    """Same merge order as UiStringsAssessmentV2.MergeAll (base → supplement → bank)."""
    stems: dict[str, str] = {}

    base = (ROOT / "Jobsy.Web/Localization/UiStringsAssessmentV2.cs").read_text(encoding="utf-8")
    stems.update(parse_key_values(extract_lang_method(base, "Nl")))

    supplement = (ROOT / "Jobsy.Web/Localization/UiStringsAssessmentV2Supplement.cs").read_text(encoding="utf-8")
    stems.update(parse_key_values(extract_lang_method(supplement, "Nl")))

    bank = (ROOT / "Jobsy.Web/Localization/UiStringsAssessmentV2ValuesCareerBank.cs").read_text(encoding="utf-8")
    stems.update(parse_key_values(extract_lang_method(bank, "Nl")))

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
        "**Catalogus:** `CompetencyTestCatalogV2.cs`. **Teksten:** base + supplement merge (NL).",
        parse_competence_rows(),
        stems,
    )
    write_matrix(
        docs / "CULTURE_V2_CONSTRUCT_MATRIX.md",
        "Cultuur-scan v2 — construct matrix",
        "**Catalogus:** `CulturePersonalityCatalogV2.cs` (12 dimensies × 4). **Curiosity** scored as Openness. **Teksten:** base + supplement + bank (Q20).",
        parse_culture_rows(),
        stems,
    )
    write_matrix(
        docs / "RIASEC_V2_CONSTRUCT_MATRIX.md",
        "Carrière-scan v2 (RIASEC) — construct matrix",
        "**Catalogus:** `CareerTestCatalogV2.cs`. **Teksten:** `UiStringsAssessmentV2ValuesCareerBank.cs` (NL).",
        parse_career_rows(),
        stems,
    )


if __name__ == "__main__":
    main()
