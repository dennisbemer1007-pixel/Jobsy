#!/usr/bin/env python3
"""Side-by-side screen-13 zee (1440) gpt-4o vs cheap-model export for trial occupations."""
import importlib.util
import os
import sys

ROOT = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ROOT)

# Load gen.py without running its main block
spec = importlib.util.spec_from_file_location("gen", os.path.join(ROOT, "gen.py"))
gen = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)

DATA = os.path.join(ROOT, "data")
EXPORT_4O = os.path.join(DATA, "occupation-day-in-life.nl.json")
EXPORT_MINI = os.path.join(DATA, "occupation-day-in-life.mini.nl.json")

COMPARE_SLUGS = [
    "bioloog",
    "accumonteur",
    "advertentiemanager",
    "docent-alfabetisering",
]



def trial_by_slug(export_path):
    days = gen.load_trial_manifest(export_path)
    return {d["slug"]: d for d in days}


def day_column(day, label):
    title = day.get("dreamTitle", "Beroep")
    blocks = ""
    for b in day["blocks"]:
        blocks += f'<li class="day-block" data-moment="{b["key"]}"><div class="day-block-inner"><div><h2>{b["label"]}</h2><p>{b["text"]}</p></div></div></li>'
    skills = day.get("skills") or []
    facts = ""
    if skills:
        pills = "".join(f'<li class="day-pill">{s}</li>' for s in skills[:6])
        facts = f'<section class="day-facts"><h2>Wat je vaak doet</h2><ul class="day-cloud">{pills}</ul></section>'
    return f'''<div class="model-col"><p class="model-label">{label}</p>
<h2 class="model-title">Een dag als {title}</h2>
<ol class="day-timeline">{blocks}</ol>{facts}</div>'''


def compare_page(slug, left, right, left_label, right_label):
    w = gen.W["zee"]
    body = f'''{gen.top(w["small"])}
<div class="wrap compare-wrap"><div class="compare-head"><h1>Vergelijk · {left.get("dreamTitle","")} · Zee 1440</h1></div>
<div class="compare-grid">{day_column(left, left_label)}{day_column(right, right_label)}</div></div>
<style>
.compare-wrap{{max-width:2800px;margin:0 auto;padding:24px}}
.compare-head h1{{font-family:Fraunces,serif;color:#10284a;margin:0 0 16px}}
.compare-grid{{display:grid;grid-template-columns:1fr 1fr;gap:24px;align-items:start}}
.model-col{{background:#fffcfa;border:1px solid #ddd5cc;border-radius:12px;padding:20px}}
.model-label{{font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.06em;color:#d4532f;margin:0 0 8px}}
.model-title{{font-family:Fraunces,serif;margin:0 0 12px;color:#10284a}}
</style>{gen.FOOT}'''
    gen.page(f"compare-zee-{slug}", body, w["cls"])


def main():
    if not os.path.isfile(EXPORT_MINI):
        print("Missing mini export:", EXPORT_MINI, file=sys.stderr)
        sys.exit(1)
    by4o = trial_by_slug(EXPORT_4O)
    bymini = trial_by_slug(EXPORT_MINI)
    for slug in COMPARE_SLUGS:
        left = by4o.get(slug)
        right = bymini.get(slug)
        if not left or not right:
            print("skip", slug, "missing row")
            continue
        compare_page(slug, left, right, "gpt-4o (database export)", "gpt-4o-mini (trial export)")
        print("wrote compare-zee-" + slug)
    print("ok")


if __name__ == "__main__":
    main()
