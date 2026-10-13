#!/usr/bin/env python3
"""Generate per-item wording review for all UI languages (manual heuristics + source lines)."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT_JSON = ROOT / "docs/psychometrie/data/item_wording_review.json"
OUT_MD = ROOT / "docs/psychometrie/ITEMteksten_BEOORDELING.md"


def extract_dict_block(cs_text: str, method: str) -> dict[str, str]:
    m = re.search(rf"private static Dictionary<string, string> {method}\(\).*?\{{", cs_text, re.S)
    if not m:
        return {}
    start = m.end()
    depth = 1
    i = start
    while i < len(cs_text) and depth:
        if cs_text[i] == "{":
            depth += 1
        elif cs_text[i] == "}":
            depth -= 1
        i += 1
    block = cs_text[start : i - 1]
    out = {}
    for km in re.finditer(r'\["([^"]+)"\]\s*=\s*"((?:\\.|[^"\\])*)"', block):
        out[km.group(1)] = km.group(2).replace('\\"', '"')
    return out


def effective_strings(base_nl: dict, en_overlay: dict, lang: str) -> dict[str, str]:
    if lang == "nl":
        return dict(base_nl)
    if lang == "en":
        merged = dict(base_nl)
        merged.update(en_overlay)
        return merged
    return dict(base_nl)


def flags_nl(text: str) -> list[str]:
    f = []
    low = text.lower()
    if " en " in low and (" terwijl " in low or " voordat " in low or " zonder " in low):
        f.append("mogelijk_dubbel_concept")
    if any(w in low for w in ("altijd", "nooit", "helemaal", "iedereen")):
        f.append("absoluut_taal")
    if any(w in low for w in ("diagnose", "disc", "ipip", "schwartz", "riasec", "ocean")):
        f.append("jargon")
    wc = len(text.split())
    if wc > 22:
        f.append("lang_voor_b1")
    if wc <= 22 and not f:
        f.append("b1_ok")
    return f


def b1_en(text: str) -> list[str]:
    f = []
    if len(text.split()) > 22:
        f.append("long_for_plain_english")
    if "—" in text:
        f.append("em_dash")
    if not f:
        f.append("plain_ok")
    return f


def main() -> None:
    comp_cs = (ROOT / "Jobsy.Web/Localization/UiStringsCompetencies.cs").read_text(encoding="utf-8")
    cult_cs = (ROOT / "Jobsy.Web/Localization/UiStringsCulture.cs").read_text(encoding="utf-8")
    val_cs = (ROOT / "Jobsy.Web/Localization/UiStringsValues.cs").read_text(encoding="utf-8")

    comp_nl = extract_dict_block(comp_cs, "Nl")
    comp_en = extract_dict_block(comp_cs, "En")
    cult_nl = extract_dict_block(cult_cs, "Nl")
    cult_en = extract_dict_block(cult_cs, "En")
    val_nl = extract_dict_block(val_cs, "Nl")
    val_en = extract_dict_block(val_cs, "En")

    tests = [
        ("competence", "Competency.Q", comp_nl, comp_en, "UiStringsCompetencies.cs"),
        ("career", "Career.Q", comp_nl, comp_en, "UiStringsCompetencies.cs"),
        ("culture", "CultureScan.Q", cult_nl, cult_en, "UiStringsCulture.cs"),
        ("values", "ValuesScan.Q", val_nl, val_en, "UiStringsValues.cs"),
    ]

    languages = ["nl", "en", "pl", "ro", "ar"]
    items = []
    md_lines = [
        "# Itemteksten — review per taal",
        "",
        "**Type bewijs:** (a) geverifieerd uit broncode; geen responsdata.",
        "",
        "Taalregels in product:",
        "- `UiStringsCompetencies.MergeAll`: pl/ro/ar ← `Nl()` (`UiStringsCompetencies.cs` L16–18)",
        "- `UiStringsCulture.MergeAll`: pl/ro/ar ← `Nl()` (`UiStringsCulture.cs` L16–18)",
        "- `UiStringsValues.MergeAll`: pl/ro/ar ← `Nl()` (`UiStringsValues.cs` L16–18)",
        "- Cultuur EN: `En()` vertaalt **geen** `CultureScan.Q**` (`UiStringsCulture.cs` L154–188)",
        "- Waarden EN: `En()` vertaalt **geen** `ValuesScan.Q**` (`UiStringsValues.cs` L87–114)",
        "",
    ]

    for test_key, prefix, nl_d, en_d, source_file in tests:
        keys = sorted([k for k in nl_d if k.startswith(prefix)], key=lambda k: int(re.search(r"\d+", k).group()))
        md_lines.append(f"## {test_key} ({len(keys)} items)")
        md_lines.append("")
        for key in keys:
            row = {"test": test_key, "key": key, "source_file": source_file, "languages": {}}
            for lang in languages:
                if test_key in ("competence", "career"):
                    text = effective_strings(nl_d, en_d, lang if lang in ("nl", "en") else "nl")
                elif test_key == "culture":
                    text = effective_strings(cult_nl, cult_en, lang if lang in ("nl", "en") else "nl")
                else:
                    text = effective_strings(val_nl, val_en, lang if lang in ("nl", "en") else "nl")
                txt = text.get(key, "")
                same_as_nl = lang != "nl" and txt == nl_d.get(key, "")
                row["languages"][lang] = {
                    "text": txt,
                    "same_as_nl": same_as_nl,
                    "flags_nl_review" if lang == "nl" else "flags_en_review": (
                        flags_nl(txt) if lang == "nl" else (b1_en(txt) if lang == "en" else ["not_translated_uses_nl"])
                    ),
                }
            items.append(row)
            nl_text = row["languages"]["nl"]["text"]
            fl = row["languages"]["nl"]["flags_nl_review"]
            md_lines.append(f"### {key}")
            md_lines.append(f"- **NL ({source_file}):** {nl_text}")
            md_lines.append(f"- **NL-heuristiek:** {', '.join(fl)}")
            en_t = row["languages"]["en"]["text"]
            md_lines.append(f"- **EN:** {en_t}" + (" *(identiek aan NL — niet vertaald)*" if en_t == nl_text else ""))
            for lang in ("pl", "ro", "ar"):
                t = row["languages"][lang]["text"]
                md_lines.append(f"- **{lang.upper()}:** {t[:80]}{'…' if len(t)>80 else ''} *(NL-fallback)*")
            md_lines.append("")

    deep_count = len(re.findall(r'\(\s*Openheid|\(\s*Consci', (ROOT / "Jobsy.Core/Rules/DeepAnalysisCompetenceItems.cs").read_text()))
    summary = {
        "evidence_type": "code_verified_a",
        "quick_scan_items_reviewed": len(items),
        "deep_competence_nl_only": "DeepAnalysisCompetenceItems.cs (150 NL strings, no UiStrings keys)",
        "translation_gaps": {
            "culture_q_en": "CultureScan.Q01-Q18 remain Dutch in En()",
            "values_q_en": "ValuesScan.Q01-Q25 remain Dutch in En()",
            "pl_ro_ar_all_quickscan": "MergeAll copies Nl for competencies, culture, values",
        },
    }

    OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    OUT_JSON.write_text(json.dumps({"summary": summary, "items": items}, ensure_ascii=False, indent=2), encoding="utf-8")
    OUT_MD.write_text("\n".join(md_lines), encoding="utf-8")
    print(f"Wrote {OUT_JSON} and {OUT_MD}")


if __name__ == "__main__":
    main()
