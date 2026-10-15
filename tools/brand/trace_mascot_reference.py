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
REF = ROOT / "docs" / "brand" / "kreeft-set-scharen-reference.png"
if not REF.exists():
    REF = Path("/home/ubuntu/.cursor/projects/workspace/uploads/kreeft-set-scharen_34c9.png")
OUT = ROOT / "tools" / "brand" / "mascot-traced"
WORK = OUT / "work"

CORAL = "#F54A1B"
CORAL_D = "#D93A12"
INK = "#0F2D5C"
VIEW = "0 0 128 128"
UPSCALE = 1
FIT = 88  # max side in 128 viewBox (padding against clip)
VIEW_PAD = 4.0

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

# Face geometry in 128×128 space (after layout; not scaled with body trace).
FACE: dict[str, dict] = {
    "rustig": {"eyes": ((48, 37, -8), (80, 37, 8)), "mouth": "smile"},
    "uitleggen": {"eyes": ((48, 37, -8), (80, 37, 8)), "mouth": "open"},
    "nieuwsgierig": {"eyes": ((46, 37, -10), (78, 37, -10)), "mouth": "line"},
    "goed-gedaan": {"eyes": ((48, 37, -8), (80, 37, 8)), "mouth": "smile"},
    "nadenken": {"eyes": ((50, 35, 6), (82, 35, 6)), "mouth": "frown"},
    "juichen": {"eyes": ((47, 36, -8), (81, 36, 8)), "mouth": "open-wide"},
}


def cream_distance(r: int, g: int, b: int) -> float:
    return ((r - 245) ** 2 + (g - 242) ** 2 + (b - 238) ** 2) ** 0.5


def is_ink(r: int, g: int, b: int) -> bool:
    return r < 95 and b > 50 and b > r + 8 and g < 120


def is_coral(r: int, g: int, b: int) -> bool:
    return r > 140 and g < 130 and b < 130 and r > g + 20


def is_segment_shadow(r: int, g: int, b: int, y: int, h: int) -> bool:
    if not is_coral(r, g, b):
        return False
    if y < h * 0.38 or y > h * 0.92:
        return False
    return g < 58 or (r < 200 and g < 68)


def cell_crop_box(col: int, row: int, gw: int, gh) -> tuple[int, int, int, int]:
    inset_l = 62 if col == 2 else (32 if col > 0 else 10)
    inset_r = 32 if col < 2 else 10
    inset_t = 14 if row > 0 else 10
    inset_b = 14 if row < 1 else 10
    return (
        col * gw + inset_l,
        row * gh + inset_t,
        (col + 1) * gw - inset_r,
        (row + 1) * gh - inset_b,
    )


def remove_background(im: Image.Image, tol: float = 32.0) -> Image.Image:
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if cream_distance(r, g, b) <= tol or (r > 238 and g > 232 and b > 225):
                px[x, y] = (r, g, b, 0)
    return im


def in_face_oval(x: int, y: int, w: int, h: int) -> bool:
    cx, cy = w * 0.5, h * 0.27
    rx, ry = w * 0.22, h * 0.1
    return ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0


def in_head_shell(x: int, y: int, w: int, h: int) -> bool:
    """Keep segment traces off the head (face overlays sit on light coral body)."""
    return y < h * 0.42 or in_face_oval(x, y, w, h)


def trim_alpha(im: Image.Image, pad: int = 6) -> Image.Image:
    bbox = im.getbbox()
    if not bbox:
        return im
    x0, y0, x1, y1 = bbox
    x0 = max(0, x0 - pad)
    y0 = max(0, y0 - pad)
    x1 = min(im.width, x1 + pad)
    y1 = min(im.height, y1 + pad)
    return im.crop((x0, y0, x1, y1))


def upscale(im: Image.Image) -> Image.Image:
    w, h = im.size
    return im.resize((w * UPSCALE, h * UPSCALE), Image.Resampling.LANCZOS)


def body_layer(im: Image.Image) -> Image.Image:
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sp, dp = im.load(), out.load()
    w, h = im.size
    coral_rgb = (0xF5, 0x4A, 0x1B)
    for y in range(h):
        for x in range(w):
            r, g, b, a = sp[x, y]
            if a < 128 or is_ink(r, g, b):
                continue
            if is_coral(r, g, b):
                dp[x, y] = (*coral_rgb, 255)
    return out


def segment_layer(im: Image.Image) -> Image.Image:
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sp, dp = im.load(), out.load()
    w, h = im.size
    dark = (0xD9, 0x3A, 0x12)
    for y in range(h):
        for x in range(w):
            r, g, b, a = sp[x, y]
            if a < 128:
                continue
            if is_segment_shadow(r, g, b, y, h) and not in_head_shell(x, y, w, h):
                dp[x, y] = (*dark, 255)
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
        return "skip"
    if g < 72 and r > 160:
        return CORAL_D
    if r > 130 and g < 150:
        return CORAL
    return "skip"


def shift_path_d(d: str, ox: float, oy: float) -> str:
    if not ox and not oy:
        return d
    tokens = re.findall(r"[MLZ]|[-]?\d+\.?\d*", d)
    out: list[str] = []
    i = 0
    while i < len(tokens):
        t = tokens[i]
        if t in ("M", "L"):
            out.append(t)
            i += 1
            if i + 1 >= len(tokens):
                break
            x = float(tokens[i])
            y = float(tokens[i + 1])
            out.append(f"{int(round(x + ox))},{int(round(y + oy))}")
            i += 2
        elif t == "Z":
            out.append(t)
            i += 1
        else:
            i += 1
    return "".join(out)


def path_bbox(d: str, ox: float = 0.0, oy: float = 0.0) -> tuple[float, float, float, float] | None:
    nums = [float(n) for n in re.findall(r"[-]?\d+\.?\d*", d)]
    if len(nums) < 4:
        return None
    xs = nums[0::2][:200]
    ys = nums[1::2][:200]
    if not xs or not ys:
        return None
    return min(xs) + ox, min(ys) + oy, max(xs) + ox, max(ys) + oy


PathEntry = tuple[str, str, float, float]


def filter_paths(
    paths: list[PathEntry], slug: str, src_w: int, src_h: int
) -> list[PathEntry]:
    kept: list[PathEntry] = []
    area = src_w * src_h
    for d, color, ox, oy in paths:
        bb = path_bbox(d, ox, oy)
        if bb is None:
            continue
        x0, y0, x1, y1 = bb
        w, h = x1 - x0, y1 - y0
        if slug == "juichen" and x1 < src_w * 0.22:
            continue
        if (
            slug == "juichen"
            and y1 < src_h * 0.28
            and x0 > src_w * 0.52
            and w * h < area * 0.02
        ):
            continue
        if (
            slug == "juichen"
            and x0 < src_w * 0.14
            and w < src_w * 0.08
            and h < src_h * 0.35
        ):
            continue
        if (
            x0 < src_w * 0.1
            and x1 < src_w * 0.28
            and w * h < area * 0.02
        ):
            continue
        if x0 < src_w * 0.02 and w < src_w * 0.08 and slug in ("juichen", "nieuwsgierig"):
            continue
        if y1 < src_h * 0.2 and w * h < area * 0.04:
            continue
        if y0 < src_h * 0.05 and x0 < src_w * 0.35 and w * h < area * 0.04:
            continue
        kept.append((d, color, ox, oy))
    return kept


def parse_svg_paths(svg_text: str) -> list[PathEntry]:
    root = ET.fromstring(svg_text)
    paths: list[PathEntry] = []
    for el in root.iter():
        if el.tag.split("}")[-1] != "path":
            continue
        d = el.get("d")
        if not d or d.strip() in ("M0,0 Z", "M0,0 L0,0 Z"):
            continue
        fill = el.get("fill") or ""
        m = re.search(r"fill:\s*(#[0-9a-fA-F]{3,8})", el.get("style") or "")
        color = m.group(1) if m else (fill or "#000000")
        ox = oy = 0.0
        tr = el.get("transform") or ""
        tm = re.search(r"translate\(([\d.-]+)[,\s]+([\d.-]+)\)", tr)
        if tm:
            ox, oy = float(tm.group(1)), float(tm.group(2))
        paths.append((d, color, ox, oy))
    return paths


def layout_transform(src_w: int, src_h: int, fit: float = FIT) -> tuple[float, float, float]:
    scale = min(fit / src_h, fit / src_w)
    tx = (128 - src_w * scale) / 2
    ty = (128 - src_h * scale) / 2 + 2
    return tx, ty, scale


def body_bbox_128(
    paths: list[PathEntry], src_w: int, src_h: int, fit: float
) -> tuple[float, float, float, float]:
    tx, ty, scale = layout_transform(src_w, src_h, fit)
    minx = miny = 1e9
    maxx = maxy = -1e9
    for d, _color, ox, oy in paths:
        bb = path_bbox(d, ox, oy)
        if bb is None:
            continue
        x0, y0, x1, y1 = bb
        for x, y in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
            px = tx + x * scale
            py = ty + y * scale
            minx = min(minx, px)
            miny = min(miny, py)
            maxx = max(maxx, px)
            maxy = max(maxy, py)
    if maxx < 0:
        return 0.0, 0.0, 128.0, 128.0
    return minx, miny, maxx, maxy


def auto_fit_transform(
    paths: list[PathEntry], src_w: int, src_h: int, *, slug: str
) -> tuple[float, float, float]:
    fit = float(FIT * (0.82 if slug == "juichen" else 1.0))
    for _ in range(10):
        tx, ty, scale = layout_transform(src_w, src_h, fit)
        minx, miny, maxx, maxy = body_bbox_128(paths, src_w, src_h, fit)
        if (
            minx >= VIEW_PAD
            and miny >= VIEW_PAD
            and maxx <= 128 - VIEW_PAD
            and maxy <= 128 - VIEW_PAD
        ):
            return tx, ty, scale
        fit *= 0.93
    return layout_transform(src_w, src_h, fit)


def eye_markup(cx: float, cy: float, rot: float, left: bool) -> str:
    flip = -1 if left else 1
    pupil_x = -1.2 if left else 1.2
    return (
        f"<g transform='translate({cx:.2f},{cy:.2f}) rotate({rot}) scale({flip},1)'>"
        f"<ellipse cx='0' cy='0' rx='5.5' ry='4.2' fill='#FFFFFF'/>"
        f"<ellipse cx='{pupil_x:.1f}' cy='0.5' rx='2.5' ry='3' fill='{INK}'/>"
        f"</g>"
    )


def mouth_markup(kind: str) -> str:
    if kind == "smile":
        return (
            f"<path fill='none' stroke='{INK}' stroke-width='2.2' "
            f"stroke-linecap='round' d='M58 50 Q64 53 70 50'/>"
        )
    if kind == "line":
        return f"<path fill='none' stroke='{INK}' stroke-width='2' stroke-linecap='round' d='M58 50 H70'/>"
    if kind == "frown":
        return f"<path fill='none' stroke='{INK}' stroke-width='2' stroke-linecap='round' d='M58 52 Q64 49 70 52'/>"
    if kind == "open":
        return (
            f"<ellipse cx='64' cy='51' rx='5.5' ry='4' fill='{INK}'/>"
            f"<ellipse cx='64' cy='52.5' rx='2.8' ry='2' fill='{CORAL}'/>"
        )
    if kind == "open-wide":
        return (
            f"<ellipse cx='64' cy='50.5' rx='7' ry='5' fill='{INK}'/>"
            f"<ellipse cx='64' cy='52' rx='3.5' ry='2.5' fill='{CORAL}'/>"
        )
    return ""


def face_overlay(slug: str) -> str:
    spec = FACE[slug]
    parts = ["<g class='lobsy-face'>"]
    for i, (cx, cy, rot) in enumerate(spec["eyes"]):
        parts.append(eye_markup(cx, cy, rot, left=(i == 0)))
    parts.append(mouth_markup(spec["mouth"]))
    parts.append("</g>")
    return "".join(parts)


def paths_to_body_markup(paths: list[PathEntry], src_w: int, src_h: int, slug: str) -> str:
    tx, ty, scale = auto_fit_transform(paths, src_w, src_h, slug=slug)
    by_fill: dict[str, list[str]] = {}
    seen: set[str] = set()
    for d, color, ox, oy in paths:
        token = classify_color(color)
        if token == "skip":
            continue
        key = d[:64] + token + str(ox)
        if key in seen:
            continue
        seen.add(key)
        flat = shift_path_d(d, ox, oy).replace("'", " ")
        if not flat.endswith("Z"):
            flat += "Z"
        by_fill.setdefault(token, []).append(flat)
    parts = [f"<path fill='{fill}' d='{' '.join(segs)}'/>" for fill, segs in by_fill.items()]
    body = "".join(parts)
    return f"<g transform='translate({tx:.4f},{ty:.4f}) scale({scale:.6f})'>{body}</g>"


def vtrace(png: Path, svg_out: Path, *, speckle: int, mode: str = "polygon") -> None:
    import vtracer

    vtracer.convert_image_to_svg_py(
        str(png),
        str(svg_out),
        colormode="color",
        hierarchical="stacked",
        mode=mode,
        filter_speckle=speckle,
        color_precision=6,
        corner_threshold=85,
        length_threshold=12.0,
        max_iterations=6,
        splice_threshold=55,
        path_precision=1,
    )


def svgo_optimize(svg_path: Path) -> None:
    config = svg_path.with_suffix(".svgo.yml")
    config.write_text(
        "multipass: true\n"
        "plugins:\n"
        "  - removeViewBox: false\n"
        "  - convertPathData:\n"
        "      floatPrecision: 0\n"
        "  - mergePaths:\n"
        "      force: true\n",
        encoding="utf-8",
    )
    try:
        subprocess.run(
            ["npx", "--yes", "svgo", str(svg_path), "-o", str(svg_path), "--config", str(config)],
            check=True,
            capture_output=True,
            timeout=120,
            cwd=str(ROOT),
        )
    except (subprocess.CalledProcessError, FileNotFoundError, subprocess.TimeoutExpired):
        pass


def trace_pose(cell: Image.Image, slug: str) -> str:
    cell = remove_background(cell)
    cell = trim_alpha(cell)
    cell = upscale(cell)
    w, h = cell.size

    body_png = WORK / f"{slug}-body.png"
    seg_png = WORK / f"{slug}-seg.png"
    body_layer(cell).save(body_png)
    segment_layer(cell).save(seg_png)

    body_svg = WORK / f"{slug}-body.svg"
    seg_svg = WORK / f"{slug}-seg.svg"
    vtrace(body_png, body_svg, speckle=32)
    vtrace(seg_png, seg_svg, speckle=28)
    svgo_optimize(body_svg)
    svgo_optimize(seg_svg)

    paths = parse_svg_paths(body_svg.read_text(encoding="utf-8"))
    paths += parse_svg_paths(seg_svg.read_text(encoding="utf-8"))
    paths = filter_paths(paths, slug, w, h)
    return paths_to_body_markup(paths, w, h, slug) + face_overlay(slug)


def main() -> None:
    WORK.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    im = Image.open(REF)
    gw, gh = im.size[0] // 3, im.size[1] // 2
    traced: dict[str, str] = {}

    for slug, col, row in POSES:
        box = cell_crop_box(col, row, gw, gh)
        inner = trace_pose(im.crop(box), slug)
        traced[slug] = inner
        svg_path = OUT / f"mascot-{slug}.svg"
        svg_path.write_text(
            f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{VIEW}">{inner}</svg>',
            encoding="utf-8",
        )
        svgo_optimize(svg_path)
        kb = svg_path.stat().st_size / 1024
        print(f"  {slug}: {kb:.1f} KB")

    avatar_vb = "22 6 84 82"
    meta = {"viewBox": VIEW, "avatarViewBox": avatar_vb, "poses": list(traced.keys())}
    (OUT / "manifest.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    emit_csharp(traced, avatar_vb)

    ref_copy = ROOT / "docs" / "brand" / "kreeft-set-scharen-reference.png"
    ref_copy.parent.mkdir(parents=True, exist_ok=True)
    if not ref_copy.exists() and REF != ref_copy:
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
