#!/usr/bin/env bash
set -euo pipefail
W="${1:-/workspace/Jobsy.Web/wwwroot/images/candidate-illustrated}"

make_scene() {
  local src="$1" out="$2" lcut="${3:-0}" tcut="${4:-0.03}" rcut="${5:-0}"
  ffmpeg -y -hide_banner -loglevel error -i "$src" -vf "crop=iw*(1-${lcut}-${rcut}):ih*(1-${tcut}):iw*${lcut}:ih*${tcut},scale=2560:720" -q:v 2 "$out"
}

make_scene "$W/werkervaring.jpg" "$W/werkervaring-profession-wide.jpg" 0.20 0.14 0
make_scene "$W/beschikbaarheid.jpg" "$W/beschikbaarheid-profession-wide.jpg" 0.18 0.14 0
make_scene "$W/vervoer.jpg" "$W/vervoer-profession-wide.jpg" 0.16 0.14 0
make_scene "$W/wie-ben-jij.jpg" "$W/wie-ben-jij-profession-wide.jpg" 0.22 0.14 0
make_scene "$W/wie-ben-jij.jpg" "$W/kompas-hero-wide.jpg" 0.50 0.23 0

# After regenerating *-profession-wide.jpg / kompas-hero-wide.jpg:
#   python3 scripts/hero-mobile-figures.py
