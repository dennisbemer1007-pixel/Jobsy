#!/usr/bin/env python3
"""Generate G78 v2 bank + NL strings from approved voorstel xlsx."""
from __future__ import annotations

import re
import sys
from pathlib import Path

from openpyxl import load_workbook

ROOT = Path(__file__).resolve().parents[1]
XLSX = Path(
    sys.argv[1]
    if len(sys.argv) > 1
    else "/home/ubuntu/.cursor/projects/agent-repos-Jobsy/uploads/G78-vragen-voorstel_3b00.xlsx"
)

# Same display order as PupilQuestionBank.cs (v1)
V1_ORDER = list(range(9001, 9061))

CAT_MAP = {
    "Samenwerken": "Rules.CompetencyTestCatalog.Samenwerken",
    "Resultaatgerichtheid": "Rules.CompetencyTestCatalog.Resultaatgerichtheid",
    "Stressbestendigheid": "Rules.CompetencyTestCatalog.Stressbestendigheid",
    "Innovatie": "Rules.CompetencyTestCatalog.Innovatie",
    "Extraversie": "Rules.CompetencyTestCatalog.Extraversie",
    "Realistic": "Rules.CareerTestCatalog.Realistic",
    "Investigative": "Rules.CareerTestCatalog.Investigative",
    "Artistic": "Rules.CareerTestCatalog.Artistic",
    "Social": "Rules.CareerTestCatalog.Social",
    "Enterprising": "Rules.CareerTestCatalog.Enterprising",
    "Conventional": "Rules.CareerTestCatalog.Conventional",
    "Autonomy": "Rules.SchwartzValuesCatalog.Autonomy",
    "Connection": "Rules.SchwartzValuesCatalog.Connection",
    "Achievement": "Rules.SchwartzValuesCatalog.Achievement",
    "Stability": "Rules.SchwartzValuesCatalog.Stability",
    "Impact": "Rules.SchwartzValuesCatalog.Impact",
    "Culture Autonomy": "Rules.CulturePersonalityCatalog.Autonomy",
    "Collaboration": "Rules.CulturePersonalityCatalog.Collaboration",
    "Culture Collaboration": "Rules.CulturePersonalityCatalog.Collaboration",
    "PeopleFirst": "Rules.CulturePersonalityCatalog.PeopleFirst",
    "Informal (hoog)": "Rules.CulturePersonalityCatalog.Informal",
    "Informal (laag)": "Rules.CulturePersonalityCatalog.Informal",
    "Flexibility (hoog)": "Rules.CulturePersonalityCatalog.Flexibility",
    "Flexibility (laag)": "Rules.CulturePersonalityCatalog.Flexibility",
    "Culture Innovation (hoog)": "Rules.CulturePersonalityCatalog.Innovation",
    "Culture Innovation (laag)": "Rules.CulturePersonalityCatalog.Innovation",
}

WORLD_MODEL = {
    range(9001, 9016): ("PupilWorld.Koraalrif", "AssessmentKind.Competence"),
    range(9016, 9031): ("PupilWorld.Schatgrot", "AssessmentKind.Career"),
    range(9031, 9046): ("PupilWorld.Vuurtoren", "AssessmentKind.Values"),
    range(9046, 9061): ("PupilWorld.Lagune", "AssessmentKind.Culture"),
}


def world_model(qid: int) -> tuple[str, str]:
    for r, wm in WORLD_MODEL.items():
        if qid in r:
            return wm
    raise ValueError(qid)


def parse_ab(text: str) -> tuple[str, str]:
    text = text.replace("—", "—").strip()
    m = re.match(r"A:\s*(.+?)\s*[—\-]\s*B:\s*(.+)", text, re.S)
    if not m:
        raise ValueError(f"Cannot parse A/B: {text[:80]}")
    return m.group(1).strip(), m.group(2).strip()


def parse_category(token: str) -> str | None:
    token = token.strip()
    for key in sorted(CAT_MAP.keys(), key=len, reverse=True):
        if token.startswith(key):
            return CAT_MAP[key]
    if token.startswith("A → "):
        token = token[4:]
        for key in sorted(CAT_MAP.keys(), key=len, reverse=True):
            if token.startswith(key):
                return CAT_MAP[key]
    return None


def load_rows() -> dict[int, dict]:
    wb = load_workbook(XLSX, data_only=True)
    ws = wb["Voorstel vragen"]
    by_id: dict[int, dict] = {}
    for row in ws.iter_rows(min_row=2, values_only=True):
        if not row or row[0] is None:
            continue
        try:
            qid = int(row[0])
        except (TypeError, ValueError):
            continue
        a_text, b_text = parse_ab(str(row[4]))
        ab_dims = str(row[9] or "")
        cat_a = cat_b = None
        foil = "geen tweede" in ab_dims.lower() or "foil" in ab_dims.lower()
        same_dim = "zelfde dimensie" in ab_dims.lower()
        if "A →" in ab_dims and "B →" in ab_dims:
            parts = ab_dims.split("B →")
            left = parts[0].replace("A →", "").strip().rstrip("/").strip()
            right = parts[1].split("/")[0].strip()
            cat_a = parse_category(left)
            if not foil and not same_dim:
                cat_b = parse_category(right)
        if cat_a is None:
            # fallback from dimensie column
            dim = str(row[1])
            for key in CAT_MAP:
                if key in dim:
                    cat_a = CAT_MAP[key]
                    break
        by_id[qid] = {
            "a_text": a_text,
            "b_text": b_text,
            "voorbeeld": str(row[7] or "").strip(),
            "cat_a": cat_a,
            "cat_b": cat_b,
        }
    if len(by_id) != 60:
        raise SystemExit(f"Expected 60 questions, got {len(by_id)}")
    return by_id


def emit_strings(by_id: dict[int, dict], out: Path) -> None:
    lines = [
        "namespace Jobsy.Core.Scholen;",
        "",
        "/// <summary>Dutch contrast strings for G78 bank v2 (g78-2). Merged when contrast mode is active.</summary>",
        "public static class PupilQuestionBankNlStringsG78V2",
        "{",
        "    private static readonly Dictionary<string, string> All = Build();",
        "",
        "    public static void MergeInto(IDictionary<string, string> target)",
        "    {",
        "        foreach (var (k, v) in All)",
        "            target[k] = v;",
        "    }",
        "",
        "    public static bool TryGet(string key, out string value) => All.TryGetValue(key, out value!);",
        "",
        "    private static Dictionary<string, string> Build()",
        "    {",
        '        var nl = new Dictionary<string, string>(StringComparer.Ordinal);',
    ]
    for qid in V1_ORDER:
        r = by_id[qid]
        a = r["a_text"].replace('"', '\\"')
        b = r["b_text"].replace('"', '\\"')
        ex = r["voorbeeld"].replace('"', '\\"')
        lines.append(f'        nl["LeerlingQ.{qid}.A"] = "{a}";')
        lines.append(f'        nl["LeerlingQ.{qid}.B"] = "{b}";')
        if ex:
            lines.append(f'        nl["LeerlingQ.{qid}.Voorbeeld"] = "{ex}";')
    lines += [
        "        return nl;",
        "    }",
        "}",
        "",
    ]
    out.write_text("\n".join(lines), encoding="utf-8")


def emit_bank(by_id: dict[int, dict], out: Path) -> None:
    lines = [
        "using Jobsy.Core.Enums;",
        "",
        "namespace Jobsy.Core.Scholen;",
        "",
        "/// <summary>G78 contrast question bank (scoring version g78-2). Same ids 9001–9060 as v1.</summary>",
        "public sealed class PupilQuestionBankG78V2 : PupilQuestionBankBase",
        "{",
        "    public const int ScoringVersion = 2;",
        "    public const string ScoringVersionKey = \"g78-2\";",
        "",
        "    public PupilQuestionBankG78V2()",
        "        : base(BuildQuestions(), ScoringVersion)",
        "    {",
        "    }",
        "",
        "    private static IReadOnlyList<PupilQuestion> BuildQuestions()",
        "        =>",
        "        [",
    ]
    for qid in V1_ORDER:
        r = by_id[qid]
        world, model = world_model(qid)
        cat_a = r["cat_a"] or "UNKNOWN"
        cat_b = r["cat_b"]
        if cat_b:
            lines.append(
                f"            Q({qid}, {world}, {model}, {cat_a}, {cat_b}),"
            )
        else:
            lines.append(f"            Q({qid}, {world}, {model}, {cat_a}),")
    lines += [
        "        ];",
        "",
        "    private static PupilQuestion Q(",
        "        int id,",
        "        PupilWorld world,",
        "        AssessmentKind model,",
        "        string categoryA,",
        "        string? categoryB = null)",
        "        => new(",
        "            id,",
        "            world,",
        "            model,",
        "            categoryA,",
        "            Reverse: false,",
        "            TextKeyA: $\"LeerlingQ.{id}.A\",",
        "            TextKeyB: $\"LeerlingQ.{id}.B\",",
        "            ExampleKey: $\"LeerlingQ.{id}.Voorbeeld\",",
        "            CategoryB: categoryB);",
        "}",
        "",
    ]
    out.write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    by_id = load_rows()
    core = ROOT / "Jobsy.Core" / "Scholen"
    emit_strings(by_id, core / "PupilQuestionBankNlStringsG78V2.cs")
    emit_bank(by_id, core / "PupilQuestionBankG78V2.cs")
    print("Wrote PupilQuestionBankG78V2.cs and PupilQuestionBankNlStringsG78V2.cs")


if __name__ == "__main__":
    main()
