#!/usr/bin/env bash
# Fill missing ESCO "Een dag als…" texts via the Jobsy API (OpenAI runs server-side).
# Resumable: each occupation is saved before the next; restart the same command to continue.
#
# Prerequisites:
#   - Jobsy API running with Postgres and a valid OpenAI key (Integration tile or OpenAI__ApiKey).
#   - Admin JWT in JOBSY_ADMIN_BEARER (or pass --cookie for browser session).
#
# Examples:
#   export JOBSY_API=https://acc-api.lobsy.nl
#   export JOBSY_ADMIN_BEARER='eyJ...'
#   ./scripts/occupation-day-in-life-fill.sh              # fill all missing (background batch)
#   ./scripts/occupation-day-in-life-fill.sh --limit 50   # pilot 50
#   ./scripts/occupation-day-in-life-fill.sh --status     # progress only
#   ./scripts/occupation-day-in-life-fill.sh --stop
set -euo pipefail

API="${JOBSY_API:-http://localhost:5000}"
AUTH=()
LIMIT=""
ACTION=start

while [[ $# -gt 0 ]]; do
  case "$1" in
    --limit) LIMIT="$2"; shift 2 ;;
    --status) ACTION=status; shift ;;
    --stop) ACTION=stop; shift ;;
    --api) API="$2"; shift 2 ;;
    -h|--help)
      sed -n '1,20p' "$0"
      exit 0
      ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

if [[ -n "${JOBSY_ADMIN_BEARER:-}" ]]; then
  AUTH=(-H "Authorization: Bearer ${JOBSY_ADMIN_BEARER}")
fi

case "$ACTION" in
  status)
    curl -fsS "${AUTH[@]}" "${API}/api/admin/occupation-day-in-life/status" | jq .
    ;;
  stop)
    curl -fsS -X POST "${AUTH[@]}" "${API}/api/admin/occupation-day-in-life/stop"
    echo "Batch stop requested."
    ;;
  start)
    URL="${API}/api/admin/occupation-day-in-life/start"
    if [[ -n "$LIMIT" ]]; then
      URL="${URL}?limit=${LIMIT}"
    fi
    curl -fsS -X POST "${AUTH[@]}" "$URL" | jq .
    echo "Poll: curl -fsS ${AUTH[*]} ${API}/api/admin/occupation-day-in-life/status | jq ."
    ;;
esac
