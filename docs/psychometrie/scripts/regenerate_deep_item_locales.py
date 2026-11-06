#!/usr/bin/env python3
"""Regenerate DeepItems/*.json from Dutch stems in DeepAnalysis*Items.cs (Argos translate nl→en→pl/ro/ar)."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
CORE = ROOT / "Jobsy.Core" / "Rules"
OUT = ROOT / "Jobsy.Core" / "Data" / "DeepItems"

KIND_FILES = {
    "competence": CORE / "DeepAnalysisCompetenceItems.cs",
    "career": CORE / "DeepAnalysisCareerItems.cs",
    "culture": CORE / "DeepAnalysisCultureItems.cs",
    "values": CORE / "DeepAnalysisValuesItems.cs",
}

ITEM_RE = re.compile(
    r"\(\s*(?:[\w.]+|\"[^\"]+\")\s*,\s*(true|false)\s*,\s*\"([^\"]+)\"\s*\)",
    re.MULTILINE,
)


def parse_items(path: Path) -> list[dict]:
    text = path.read_text(encoding="utf-8")
    items: list[dict] = []
    for m in ITEM_RE.finditer(text):
        rev, prompt = m.group(1) == "true", m.group(2).strip()
        if not prompt or prompt.startswith("//"):
            continue
        if "Domains" in prompt or "All" in prompt:
            continue
        items.append({"reverse": rev, "prompt_nl": prompt})
    return items


def install_packages():
    import argostranslate.package

    argostranslate.package.update_package_index()
    available = argostranslate.package.get_available_packages()
    needed = [("nl", "en"), ("en", "pl"), ("en", "ro"), ("en", "ar")]
    installed = {(p.from_code, p.to_code) for p in argostranslate.package.get_installed_packages()}
    for fr, to in needed:
        if (fr, to) in installed:
            continue
        pkg = next((p for p in available if p.from_code == fr and p.to_code == to), None)
        if pkg is None:
            raise RuntimeError(f"No Argos package for {fr}->{to}")
        argostranslate.package.install_from_path(pkg.download())


def get_translator(from_code: str, to_code: str):
    import argostranslate.translate

    return argostranslate.translate.get_translation_from_codes(from_code, to_code)


def translate_safe(translator, text: str, min_len: int = 8) -> str:
    best = text
    for _ in range(3):
        out = translator.translate(text)
        if out and len(out.strip()) >= min_len:
            return out.strip()
        best = out or best
    return best.strip() or text


def example_en(domain_hint: str, reverse: bool) -> str:
    angle = (
        "This is about situations where you tend to stick to what you know — or not."
        if reverse
        else "This is about how you usually react when it matters at work."
    )
    return f"Example: something at work goes differently than planned. {angle}"


def main() -> int:
    install_packages()
    nl_en = get_translator("nl", "en")
    en_pl = get_translator("en", "pl")
    en_ro = get_translator("en", "ro")
    en_ar = get_translator("en", "ar")

    for slug, cs_path in KIND_FILES.items():
        items = parse_items(cs_path)
        if not items:
            print(f"WARN: no items parsed from {cs_path}", file=sys.stderr)
            continue
        for lang in ("en", "pl", "ro", "ar"):
            payload: dict[str, dict] = {}
            for i, item in enumerate(items, start=1):
                nl = item["prompt_nl"]
                en = translate_safe(nl_en, nl)
                if lang == "en":
                    prompt = en
                elif lang == "pl":
                    prompt = translate_safe(en_pl, en)
                elif lang == "ro":
                    prompt = translate_safe(en_ro, en)
                else:
                    prompt = translate_safe(en_ar, en)
                payload[str(i)] = {
                    "prompt": prompt,
                    "example": example_en("", item["reverse"]),
                    "reverse": item["reverse"],
                    "domain": "",
                }
            out_path = OUT / f"{slug}-{lang}.json"
            out_path.write_text(json.dumps(payload, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
            print(f"Wrote {out_path} ({len(payload)} items)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
