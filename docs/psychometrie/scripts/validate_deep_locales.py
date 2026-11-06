#!/usr/bin/env python3
"""Re-translate broken DeepItems JSON prompts (too short / wrong script)."""
from __future__ import annotations

import json
import re
from pathlib import Path

import argostranslate.package
import argostranslate.translate

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Jobsy.Core" / "Data" / "DeepItems"
CORE = ROOT / "Jobsy.Core" / "Rules"
KIND_FILES = {
    "competence": CORE / "DeepAnalysisCompetenceItems.cs",
    "career": CORE / "DeepAnalysisCareerItems.cs",
    "culture": CORE / "DeepAnalysisCultureItems.cs",
    "values": CORE / "DeepAnalysisValuesItems.cs",
}
ITEM_RE = re.compile(r"\(\s*(?:[\w.\"]+)\s*,\s*(true|false)\s*,\s*\"([^\"]+)\"\s*\)")
AR = re.compile(r"[\u0600-\u06FF]")


def parse_nl(path: Path) -> list[str]:
    return [m.group(2) for m in ITEM_RE.finditer(path.read_text(encoding="utf-8")) if len(m.group(2)) > 5]


def valid(lang: str, prompt: str) -> bool:
    p = prompt.strip()
    if len(p) < 12:
        return False
    if lang == "ar":
        return bool(AR.search(p))
    if lang in ("pl", "ro"):
        return not p.lower().startswith("ik ")
    return not p.lower().startswith("ik ")


def main() -> None:
    argostranslate.package.update_package_index()
    nl_en = argostranslate.translate.get_translation_from_codes("nl", "en")
    en_pl = argostranslate.translate.get_translation_from_codes("en", "pl")
    en_ro = argostranslate.translate.get_translation_from_codes("en", "ro")
    en_ar = argostranslate.translate.get_translation_from_codes("en", "ar")

    def chain(nl: str, lang: str) -> str:
        en = nl_en.translate(nl)
        if lang == "en":
            return en
        tr = {"pl": en_pl, "ro": en_ro, "ar": en_ar}[lang]
        return tr.translate(en)

    for slug, cs in KIND_FILES.items():
        nls = parse_nl(cs)
        for lang in ("en", "pl", "ro", "ar"):
            path = OUT / f"{slug}-{lang}.json"
            data = json.loads(path.read_text(encoding="utf-8"))
            fixed = 0
            for i, nl in enumerate(nls, start=1):
                key = str(i)
                if key not in data:
                    continue
                prompt = data[key].get("prompt", "")
                if valid(lang, prompt):
                    continue
                for _ in range(4):
                    candidate = chain(nl, lang)
                    if valid(lang, candidate):
                        data[key]["prompt"] = candidate
                        fixed += 1
                        break
                else:
                    data[key]["prompt"] = chain(nl, lang)
                    fixed += 1
            path.write_text(json.dumps(data, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
            if fixed:
                print(f"Fixed {fixed} in {path.name}")


if __name__ == "__main__":
    main()
