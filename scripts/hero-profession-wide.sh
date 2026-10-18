#!/usr/bin/env bash
# Profession-band wide heroes (#732): right-aligned scene in 2560×720 with pearl pad left.
# Per-asset left crop keeps figures out of the CSS fade zone (62–78% viewport).
set -euo pipefail
PEARL=0xf7f4f0
W="${1:-/workspace/Jobsy.Web/wwwroot/images/candidate-illustrated}"

make_wide() {
  local src="$1" out="$2" lcut="$3" tcut="${4:-0.03}"
  ffmpeg -y -hide_banner -loglevel error -i "$src" -vf "
    crop=iw*(1-${lcut}):ih*(1-${tcut}):iw*${lcut}:ih*${tcut},
    scale=-2:720,
    pad=2560:720:(2560-iw):0:color=${PEARL}
  " -q:v 2 "$out"
}

# lcut = fraction removed from left of source (higher → figures further right in frame)
make_wide "$W/werkervaring.jpg" "$W/werkervaring-profession-wide.jpg" 0.56
make_wide "$W/beschikbaarheid.jpg" "$W/beschikbaarheid-profession-wide.jpg" 0.48
make_wide "$W/vervoer.jpg" "$W/vervoer-profession-wide.jpg" 0.52
make_wide "$W/wie-ben-jij.jpg" "$W/wie-ben-jij-profession-wide.jpg" 0.58
