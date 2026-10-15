#!/usr/bin/env python3
"""Trace Dennis reference grid into per-pose SVG for LobsyMascotArt."""
from __future__ import annotations

import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REF = Path("/home/ubuntu/.cursor/projects/workspace/uploads/kreeft-set-scharen_34c9.png")
OUT = ROOT / "tools" / "brand" / "mascot-traced"
WORK = OUT / "work"

CORAL = "#F54A1B"
CORAL_D = "#D93A12"
INK = "#0F2D5C"
VIEW = "0 0 128 128"
UPSCALE = 3

POSES = [
    ("rustig", 0, 0),
    ("uitleggen", 1, 0),
    ("nieuwsgierig", 2, 0),
    ("goed-gedaan", 0, 1),
    ("nadenken", 1, 1),
    ("juichen", 2, 1),
]

ENUM_MAP = {
    "rustig": "Rustig",
    "uitleggen": "Uitleggen",
    "nieuwsgierig": "Nieuwsgierig",
    "goed-gedaan": "GoedGedaan",
    "nadenken": "Nadenken",
    "juichen": "Juichen",
}


def cream_distance(r: int, g: int, b: int) -> float:
    return ((r - 245) ** 2 + (g - 242) ** 2 + (b - 238) ** 2) ** 0.5


def is_ink(r: int, g: int, b: int) -> bool:
    return r < 95 and b > 50 and b > r + 8 and g < 120


def is_coral(r: int, g: int, b: int) -> bool:
    return r > 140 and g < 130 and b < 130 and r > g + 20


def remove_background(im: Image.Image, tol: float = 32.0) -> Image.Image:
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if is_ink(r, g, b):
                continue
            if cream_distance(r, g, b) <= tol or (r > 238 and g > 232 and b > 225):
                px[x, y] = (r, g, b, 0)
    return im


def upscale(im: Image.Image) -> Image.Image:
    w, h = im.size
    return im.resize((w * UPSCALE, h * UPSCALE), Image.Resampling.LANCZOS)


def ink_layer(im: Image.Image) -> Image.Image:
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sp, dp = im.load(), out.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = sp[x, y]
            if a > 128 and is_ink(r, g, b):
                dp[x, y] = (15, 45, 92, 255)
    return out


def coral_layer(im: Image.Image) -> Image.Image:
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sp, dp = im.load(), out.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = sp[x, y]
            if a < 128 or is_ink(r, g, b):
                continue
            if is_coral(r, g, b):
                c = CORAL_D if g < 65 or r < 215 else CORAL
                rgb = tuple(int(c[i + 1 : i + 3], 16) for i in (0, 2, 4))
                dp[x, y] = (*rgb, 255)
    return out


def classify_color(hex_color: str) -> str:
    h = hex_color.lower().lstrip("#")
    if len(h) == 3:
        h = "".join(c * 2 for c in h)
    r = int(h[0:2], 16)
    g = int(h[2:4], 16)
    b = int(h[4:6], 16)
    if r + g + b > 720:
        return "skip"
    if is_ink(r, g, b):
        return INK
    if g < 70 and r > 170:
        return CORAL_D
    if r > 130 and g < 140:
        return CORAL
    return "skip"


def parse_svg_paths(svg_text: str) -> list[tuple[str, str]]:
    root = ET.fromstring(svg_text)
    paths: list[tuple[str, str]] = []
    for el in root.iter():
        tag = el.tag.split("}")[-1]
        if tag != "path":
            continue
        d = el.get("d")
        if not d:
            continue
        fill = el.get("fill") or ""
        m = re.search(r"fill:\s*(#[0-9a-fA-F]{3,8})", el.get("style") or "")
        color = m.group(1) if m else (fill or "#000000")
        paths.append((d, color))
    return paths


def layout_transform(src_w: int, src_h: int) -> tuple[float, float, float]:
    scale = min(118 / src_h, 118 / src_w)
    tx = (128 - src_w * scale) / 2
    ty = (128 - src_h * scale) / 2 + 1
    return tx, ty, scale


def paths_to_markup(paths: list[tuple[str, str]], src_w: int, src_h: int) -> str:
    tx, ty, scale = layout_transform(src_w, src_h)
    seen: set[str] = set()
    parts: list[str] = []
    for d, color in paths:
        token = classify_color(color)
        if token == "skip":
            continue
        key = d[:80] + token
        if key in seen:
            continue
        seen.add(key)
        d_attr = d.replace("'", " ")
        parts.append(f"<path fill='{token}' d='{d_attr}'/>")
    inner = "".join(parts)
    return (
        f"<g transform='translate({tx:.4f},{ty:.4f}) scale({scale:.6f})'>"
        f"{inner}</g>"
    )


def vtrace(png: Path, svg_out: Path, *, speckle: int) -> None:
    import vtracer

    vtracer.convert_image_to_svg_py(
        str(png),
        str(svg_out),
        colormode="color",
        hierarchical="stacked",
        mode="spline",
        filter_speckle=speckle,
        color_precision=8,
        corner_threshold=70,
        length_threshold=3.5,
        max_iterations=8,
        splice_threshold=45,
        path_precision=2,
    )


def svgo_optimize(svg_path: Path) -> None:
    try:
        subprocess.run(
            ["npx", "--yes", "svgo", str(svg_path), "-o", str(svg_path)],
            check=True,
            capture_output=True,
            timeout=90,
            cwd=str(ROOT),
        )
    except (subprocess.CalledProcessError, FileNotFoundError, subprocess.TimeoutExpired):
        pass


def trace_pose(cell: Image.Image, slug: str) -> str:
    cell = remove_background(cell)
    cell = upscale(cell)
    w, h = cell.size
    coral_png = WORK / f"{slug}-coral.png"
    ink_png = WORK / f"{slug}-ink.png"
    coral_layer(cell).save(coral_png)
    ink_layer(cell).save(ink_png)

    coral_svg = WORK / f"{slug}-coral.svg"
    ink_svg = WORK / f"{slug}-ink.svg"
    vtrace(coral_png, coral_svg, speckle=8)
    vtrace(ink_png, ink_svg, speckle=2)
    svgo_optimize(coral_svg)
    svgo_optimize(ink_svg)

    paths = parse_svg_paths(coral_svg.read_text(encoding="utf-8"))
    paths += parse_svg_paths(ink_svg.read_text(encoding="utf-8"))
    return paths_to_markup(paths, w, h)


def main() -> None:
    WORK.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    im = Image.open(REF)
    gw, gh = im.size[0] // 3, im.size[1] // 2
    traced: dict[str, str] = {}

    for slug, col, row in POSES:
        box = (col * gw, row * gh, (col + 1) * gw, (row + 1) * gh)
        inner = trace_pose(im.crop(box), slug)
        traced[slug] = inner
        (OUT / f"mascot-{slug}.svg").write_text(
            f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{VIEW}">{inner}</svg>',
            encoding="utf-8",
        )

    avatar_vb = "22 6 84 82"
    meta = {"viewBox": VIEW, "avatarViewBox": avatar_vb, "poses": list(traced.keys())}
    (OUT / "manifest.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    emit_csharp(traced, avatar_vb)

    ref_copy = ROOT / "docs" / "brand" / "kreeft-set-scharen-reference.png"
    ref_copy.parent.mkdir(parents=True, exist_ok=True)
    if not ref_copy.exists():
        ref_copy.write_bytes(REF.read_bytes())
    print(f"Traced {len(traced)} poses → {OUT}")


def cs_string_literal(value: str) -> str:
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def emit_csharp(traced: dict[str, str], avatar_vb: str) -> None:
    cs = ROOT / "Jobsy.Web" / "Components" / "Brand" / "LobsyMascotTraced.generated.cs"
    lines = [
        "// <auto-generated by tools/brand/trace_mascot_reference.py — do not edit>",
        "using Jobsy.Web.Media;",
        "namespace Jobsy.Web.Components.Brand;",
        "",
        "internal static class LobsyMascotTraced",
        "{",
        f'    public const string ViewBox = "{VIEW}";',
        f'    public const string AvatarViewBox = "{avatar_vb}";',
        "",
        "    public static string InnerMarkup(MascotPose pose) => pose switch",
        "    {",
    ]
    for slug, enum in ENUM_MAP.items():
        inner = traced[slug]
        lines.append(f"        MascotPose.{enum} => {cs_string_literal(inner)},")
    rustig = traced["rustig"]
    lines.append(f"        MascotPose.Shell => {cs_string_literal(rustig)},")
    lines.append(f"        _ => {cs_string_literal(rustig)},")
    lines.append("    };")
    lines.append("}")
    lines.append("")
    cs.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
