#!/usr/bin/env bash
# Wide hero assets: keep left scene + 80px pearl pad; trim top cream bar.
set -euo pipefail
PEARL=0xf7f4f0
W="${1:-/workspace/Jobsy.Web/wwwroot/images/candidate-illustrated}"

make_wide() {
  local src="$1" out="$2" lcut="$3" tcut="${4:-0.03}"
  ffmpeg -y -hide_banner -loglevel error -i "$src" -vf "
    crop=iw*(1-${lcut}):ih*(1-${tcut}):iw*${lcut}:ih*${tcut},
    scale=-2:720,
    scale='max(1280,iw)':720,
    pad=iw+120:720:120:0:color=${PEARL}
  " "$out"
}

make_wide "$W/beschikbaarheid.jpg" "$W/beschikbaarheid-profession-wide.jpg" 0.02
make_wide "$W/werkervaring.jpg" "$W/werkervaring-profession-wide.jpg" 0
make_wide "$W/vervoer.jpg" "$W/vervoer-profession-wide.jpg" 0
make_wide "$W/wie-ben-jij.jpg" "$W/wie-ben-jij-profession-wide.jpg" 0.04
