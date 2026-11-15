#!/usr/bin/env python3
"""Export hand-authored A/B banks to CSV, dekking, and voorbeeldvragen for Dennis."""
from __future__ import annotations

import random
import textwrap
from pathlib import Path

from bank_schema import (
    BASIS_COMPETENCES,
    UITGEBREID_COMPETENCES,
    count_coverage,
    validate_bank,
    write_csv,
    write_dekking_section,
)
from handcrafted_basis import load_basis_items
from handcrafted_uitgebreid import load_uitgebreid_items

ROOT = Path(__file__).resolve().parents[2]
DATA_DIR = ROOT / "Jobsy.Core" / "Data" / "WerkgeverApk"
DOCS_DIR = ROOT / "docs" / "werkgever-apk"
ARTIFACT_DIR = Path("/opt/cursor/artifacts/pakketten")


def write_voorbeeldvragen(path: Path, basis: list, uit: list) -> None:
    """Pick 5 RIASEC, 5 competentie, 5 werkwaarden from combined banks."""
    pool = [( "Basis", it) for it in basis] + [("Uitgebreid", it) for it in uit]
    ri = [x for x in pool if x[1].blok == "interesse" and x[1].soort == "ab"]
    comp = [x for x in pool if x[1].blok == "competentie" and x[1].soort == "ab"]
    werk = [x for x in pool if x[1].blok == "werkwaarden"]
    rng = random.Random(20261116)

    def pick_three(groups: list, n: int) -> list:
        by_sector: dict[str, list] = {}
        for g in groups:
            sec = g[1].sector or "algemeen"
            by_sector.setdefault(sec, []).append(g)
        chosen: list = []
        sectors = sorted(by_sector.keys())
        rng.shuffle(sectors)
        for sec in sectors:
            if len(chosen) >= n:
                break
            chosen.append(rng.choice(by_sector[sec]))
        while len(chosen) < n and groups:
            cand = rng.choice(groups)
            if cand not in chosen:
                chosen.append(cand)
        return chosen[:n]

    picks = pick_three(ri, 5) + pick_three(comp, 5) + pick_three(werk, 5)
    lines = [
        "# Voorbeeld A/B-vragen (CONCEPT)",
        "",
        "15 representatieve paren: 5 RIASEC, 5 competenties, 5 werkwaarden. "
        "Elke situatie is een concrete mini-scène; sector komt uit het bank-item.",
        "",
    ]
    for i, (bank, it) in enumerate(picks, 1):
        kind = {"interesse": "RIASEC", "competentie": "Competentie", "werkwaarden": "Werkwaarde"}[it.blok]
        sec = it.sector or "—"
        lines.append(f"## {i}. {kind} · {bank} · sector {sec}")
        lines.append(f"**Situatie:** {it.situatie}")
        lines.append("")
        lines.append(f"**{it.kop}**")
        lines.append("")
        lines.append(f"- **A** ({it.dimensie_a}): {it.optie_a}")
        lines.append(f"- **B** ({it.dimensie_b}): {it.optie_b}")
        lines.append("")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    basis = load_basis_items()
    uit = load_uitgebreid_items()

    validate_bank("Basis", basis, min_ri=4, min_comp=4, min_sw=4, comp_dims=BASIS_COMPETENCES)
    validate_bank(
        "Uitgebreid",
        uit,
        min_ri=12,
        min_comp=12,
        min_sw=12,
        comp_dims=UITGEBREID_COMPETENCES,
    )

    basis_path = DATA_DIR / "ab-basis-CONCEPT.csv"
    uit_path = DATA_DIR / "ab-uitgebreid-CONCEPT.csv"
    write_csv(basis_path, basis)
    write_csv(uit_path, uit)

    cov_basis = count_coverage(basis)
    cov_uit = count_coverage(uit)
    dekking_body = [
        "# Loopbaan-APK A/B question banks — dekking (CONCEPT)",
        "",
        "Handmatig geschreven items (geen sjabloon-combinaties). "
        "Export via `tools/werkgever-apk/export_handcrafted_banks.py`.",
        "",
        write_dekking_section("Basis (~40)", basis, cov_basis).strip(),
        write_dekking_section("Uitgebreid (~200)", uit, cov_uit).strip(),
    ]
    dekking_path = DOCS_DIR / "ab-dekking-CONCEPT.md"
    dekking_path.write_text("\n".join(dekking_body) + "\n", encoding="utf-8")

    ARTIFACT_DIR.mkdir(parents=True, exist_ok=True)
    (ARTIFACT_DIR / "dekking.md").write_text(dekking_path.read_text(encoding="utf-8"), encoding="utf-8")
    write_voorbeeldvragen(ARTIFACT_DIR / "voorbeeldvragen.md", basis, uit)

    print(f"Basis: {len(basis)} -> {basis_path}")
    print(f"Uitgebreid: {len(uit)} -> {uit_path}")
    print(f"Dekking: {dekking_path}")
    print(f"Voorbeeld: {ARTIFACT_DIR / 'voorbeeldvragen.md'}")


if __name__ == "__main__":
    main()