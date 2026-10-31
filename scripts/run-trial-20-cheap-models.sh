#!/usr/bin/env bash
# Generate trial-20 occupation days with gpt-4o-mini into a separate JSON file (does not touch Acc DB).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DATA="$ROOT/docs/mockups/leerling/werelden/data"
export OPENAI_API_KEY="${OPENAI_API_KEY:-${OpenAI__ApiKey:-}}"
if [[ -z "$OPENAI_API_KEY" ]]; then
  echo "Set OPENAI_API_KEY (Acceptatie Integrations OpenAI key is fine locally)." >&2
  exit 2
fi
dotnet run --project "$ROOT/tools/OccupationDayTrialFill/OccupationDayTrialFill.csproj" -- \
  --model gpt-4o-mini \
  --replace \
  --export-out occupation-day-in-life.mini.nl.json \
  --cost-out trial-20-cost-mini.json
export OCCUPATION_DAY_EXPORT="$DATA/occupation-day-in-life.mini.nl.json"
python3 "$ROOT/scripts/verify-trial-timeline.py"
python3 "$ROOT/scripts/verify-trial-quality.py"
cd "$ROOT/docs/mockups/leerling/werelden"
python3 gen_model_compare.py
node render_compare.js
python3 "$ROOT/scripts/write-trial-20-model-compare-report.py"
