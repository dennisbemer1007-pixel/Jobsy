#!/usr/bin/env python3
"""
Extract assessment inventory from Lobsy source (read-only).
Output: docs/psychometrie/data/inventory.json
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "docs/psychometrie/data/inventory.json"


def parse_competency_questions(cs: str) -> list[dict]:
    pattern = re.compile(
        r'new\((\d+),\s*(\w+),\s*Reverse:\s*(true|false),\s*"([^"]+)"',
        re.IGNORECASE,
    )
    const_map = {
        "Samenwerken": "Samenwerken",
        "Resultaatgerichtheid": "Resultaatgerichtheid",
        "Stressbestendigheid": "Stressbestendigheid",
        "Innovatie": "Innovatie",
        "Extraversie": "Extraversie",
        "Realistic": "Realistic",
        "Investigative": "Investigative",
        "Artistic": "Artistic",
        "Social": "Social",
        "Enterprising": "Enterprising",
        "Conventional": "Conventional",
        "Autonomy": "Autonomy",
        "Informal": "Informal",
        "Collaboration": "Collaboration",
        "Flexibility": "Flexibility",
        "Innovation": "Innovation",
        "PeopleFirst": "PeopleFirst",
        "Openness": "Openness",
        "Conscientiousness": "Conscientiousness",
        "Extraversion": "Extraversion",
        "Agreeableness": "Agreeableness",
        "EmotionalStability": "EmotionalStability",
        "Connection": "Connection",
        "Achievement": "Achievement",
        "Stability": "Stability",
        "Impact": "Impact",
    }
    items = []
    for m in pattern.finditer(cs):
        cat_token = m.group(2)
        items.append(
            {
                "id": int(m.group(1)),
                "scale": const_map.get(cat_token, cat_token),
                "reverse": m.group(3).lower() == "true",
                "text_key": m.group(4),
            }
        )
    return items


def load_ui_strings_competencies() -> tuple[dict[str, str], dict[str, str]]:
    path = ROOT / "Jobsy.Web/Localization/UiStringsCompetencies.cs"
    raw = path.read_text(encoding="utf-8")
    nl, en = {}, {}

    def extract_block(name: str) -> dict[str, str]:
        m = re.search(rf"private static Dictionary<string, string> {name}\(\).*?\{{", raw, re.S)
        if not m:
            return {}
        start = m.end()
        depth = 1
        i = start
        while i < len(raw) and depth:
            if raw[i] == "{":
                depth += 1
            elif raw[i] == "}":
                depth -= 1
            i += 1
        block = raw[start : i - 1]
        out = {}
        for km in re.finditer(r'\["([^"]+)"\]\s*=\s*"((?:\\.|[^"\\])*)"', block):
            key = km.group(1)
            val = km.group(2).encode().decode("unicode_escape")
            out[key] = val.replace('\\"', '"')
        return out

    nl = extract_block("Nl")
    en = extract_block("En")
    return nl, en


def deep_competence_items() -> list[dict]:
    path = ROOT / "Jobsy.Core/Rules/DeepAnalysisCompetenceItems.cs"
    raw = path.read_text(encoding="utf-8")
    items = []
    for m in re.finditer(
        r'\(\s*(\w+),\s*(true|false),\s*"((?:\\.|[^"\\])*)"\s*\)',
        raw,
    ):
        domain = m.group(1)
        if domain not in (
            "Openheid",
            "Consciëntieusheid",
            "Extraversie",
            "Vriendelijkheid",
            "EmotioneleStabiliteit",
        ):
            continue
        items.append(
            {
                "scale": domain,
                "reverse": m.group(2).lower() == "true",
                "text_nl": m.group(3),
            }
        )
    for i, it in enumerate(items, 1):
        it["id"] = i
    return items


def onboarding_mini() -> dict:
    path = ROOT / "Jobsy.Core/Rules/OnboardingWizardCatalog.cs"
    raw = path.read_text(encoding="utf-8")

    def int_array(name: str) -> list[int]:
        m = re.search(rf"{name}\s*=\s*\[([^\]]+)\]", raw)
        if not m:
            return []
        return [int(x.strip()) for x in m.group(1).split(",") if x.strip().isdigit()]

    return {
        "competence_item_ids": int_array("CompetencyQuestionIds"),
        "career_item_ids": int_array("CareerQuestionIds"),
        "culture_item_ids": int_array("CultureQuestionIds"),
        "values_item_ids": int_array("ValuesQuestionIds"),
        "total_mini_questions": 21,
    }


def main() -> None:
    nl, en = load_ui_strings_competencies()
    comp_cs = (ROOT / "Jobsy.Core/Rules/CompetencyTestCatalog.cs").read_text(encoding="utf-8")
    career_cs = (ROOT / "Jobsy.Core/Rules/CareerTestCatalog.cs").read_text(encoding="utf-8")
    culture_cs = (ROOT / "Jobsy.Core/Rules/CulturePersonalityCatalog.cs").read_text(encoding="utf-8")
    values_cs = (ROOT / "Jobsy.Core/Rules/SchwartzValuesCatalog.cs").read_text(encoding="utf-8")

    def attach_text(items: list[dict]) -> None:
        for it in items:
            key = it.get("text_key", "")
            it["text_nl"] = nl.get(key, "")
            it["text_en"] = en.get(key, "")
            it["text_pl"] = nl.get(key, "")  # MergeAll copies Nl for pl/ro/ar
            it["text_ro"] = nl.get(key, "")
            it["text_ar"] = nl.get(key, "")

    competence = parse_competency_questions(comp_cs)
    career = parse_competency_questions(career_cs)
    culture = parse_competency_questions(culture_cs)
    values = parse_competency_questions(values_cs)
    for batch in (competence, career, culture, values):
        attach_text(batch)

    inventory = {
        "source": "Jobsy repo (code + UiStringsCompetencies)",
        "response_format": {"likert_min": 1, "likert_max": 5, "labels_key_prefix": "Competency.Likert."},
        "scoring": {
            "scale_percent": "mean of reverse-scored Likert items, mapped to 0-100 via LikertAnswerJson.ToPercent",
            "tag_cutoff_percent": 70,
            "dna_high_mid_low": {"high_min": 70, "mid_min": 45},
            "culture_dna_threshold": 60,
            "provisional_neutral_fill": 55,
        },
        "tests": {
            "competence_quickscan": {
                "model_claim": "Big Five / OCEAN workplace (Mini-IPIP inspired)",
                "item_count": len(competence),
                "items_per_scale": 5,
                "matching_scales": ["Samenwerken", "Resultaatgerichtheid", "Stressbestendigheid", "Innovatie"],
                "items": competence,
            },
            "career_quickscan": {
                "model_claim": "Holland RIASEC",
                "item_count": len(career),
                "items": career,
            },
            "culture_quickscan": {
                "model_claim": "OCP culture dims + abbreviated Big Five facets",
                "item_count": len(culture),
                "items": culture,
            },
            "values_quickscan": {
                "model_claim": "Schwartz workplace drivers (5 facets)",
                "item_count": len(values),
                "items": values,
            },
            "deep_competence": {
                "items": (deep_items := deep_competence_items()),
                "item_count": len(deep_items),
                "items_per_domain": 30,
                "domains": sorted({x["scale"] for x in deep_items}),
            },
        },
        "onboarding_mini": onboarding_mini(),
        "deep_lengths": {
            "competence": 150,
            "career": 200,
            "culture": 150,
            "values": 150,
        },
        "translation_note": {
            "nl": "primary",
            "en": "full test item translations in UiStringsCompetencies.En",
            "pl_ro_ar": "test items fall back to Dutch (UiStringsCompetencies.MergeAll)",
        },
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(inventory, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {OUT} ({OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
