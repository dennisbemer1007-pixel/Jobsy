#!/usr/bin/env python3
"""Normalize Stijl A CSS: radii, shadows, gradients (Jobsy.Web/wwwroot/css)."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Jobsy.Web" / "wwwroot" / "css"

EXCLUDED_NAMES = frozenset(
    {
        "tokens.css",
        "app.min.css",
    }
)

SHADOW_KEYWORDS = re.compile(
    r"modal|dialog|dropdown|drawer|backdrop|popover|menu|toast|"
    r"cookie-consent|share-modal|lobsy-dialog|filter-sheet|admin-drawer",
    re.I,
)

GRADIENT_ALLOW = re.compile(
    r"hero|finale|gold-gradient|map|ontdek|journey-hero|carriere-hero|"
    r"career-scene|passport-hero|ob-finale|pub-|wl-hero|err-blob|illustration",
    re.I,
)

# Never flatten radii on illustrated / organic surfaces (warm humanistisch).
RADIUS_SKIP = re.compile(
    r"career-scene|err-blob|au-blob|pub-landing__blob|illustration|mascot",
    re.I,
)

RE_BORDER_RADIUS = re.compile(r"border-radius\s*:\s*([^;}{]+)", re.I)
RE_BOX_SHADOW = re.compile(r"box-shadow\s*:\s*([^;}{]+)", re.I)
RE_LINEAR_GRADIENT = re.compile(r"linear-gradient\s*\(", re.I)


def should_process(path: Path) -> bool:
    return path.name not in EXCLUDED_NAMES and "lib/" not in str(path)


def collect_files() -> list[Path]:
    return sorted(p for p in ROOT.glob("**/*.css") if should_process(p))


def count_metrics(text: str) -> tuple[int, int, int]:
    radii = re.findall(r"border-radius\s*:\s*([^;}{]+)", text, re.I)
    shadows = re.findall(r"box-shadow\s*:\s*([^;}{]+)", text, re.I)
    distinct_radii = len({r.strip() for r in radii})
    non_none_shadows = sum(1 for s in shadows if "none" not in s.lower())
    gradients = len(RE_LINEAR_GRADIENT.findall(text))
    return distinct_radii, non_none_shadows, gradients


def replace_legacy_radius_vars(text: str) -> str:
    subs = [
        (r"var\(\s*--radius-sm\s*(?:,\s*[^)]+)?\)", "var(--r-sm)"),
        (r"var\(\s*--radius-lg\s*(?:,\s*[^)]+)?\)", "var(--r-lg)"),
        (r"var\(\s*--radius-md\s*(?:,\s*[^)]+)?\)", "var(--r-md)"),
        (r"var\(\s*--radius\s*(?:,\s*[^)]+)?\)", "var(--r-md)"),
        (r"var\(\s*--radius-pill\s*(?:,\s*[^)]+)?\)", "var(--radius-pill)"),
        (r"var\(\s*--wa-radius\s*\)", "var(--r-lg)"),
        (r"var\(\s*--form-card-radius\s*\)", "var(--r-lg)"),
        (r"var\(\s*--au-radius-card\s*\)", "var(--r-lg)"),
        (r"var\(\s*--pub-radius-pill\s*\)", "var(--radius-pill)"),
        (r"var\(\s*--pub-radius-card\s*(?:,\s*[^)]+)?\)", "var(--r-lg)"),
        (r"var\(\s*--pub-radius-scene\s*(?:,\s*[^)]+)?\)", "var(--r-lg)"),
        (r"var\(\s*--pub-radius-sm\s*(?:,\s*[^)]+)?\)", "var(--r-sm)"),
        (r"var\(\s*--radius-sm\s*\)", "var(--r-sm)"),
    ]
    for pat, repl in subs:
        text = re.sub(pat, repl, text, flags=re.I)
    text = re.sub(
        r"border-radius\s*:\s*calc\(\s*var\(\s*--form-card-radius\s*\)\s*\+\s*\d+px\s*\)",
        "border-radius:var(--r-lg)",
        text,
        flags=re.I,
    )
    text = re.sub(
        r"border-radius\s*:\s*calc\(\s*var\(\s*--r-lg\s*\)\s*\+\s*\d+px\s*\)",
        "border-radius:var(--r-lg)",
        text,
        flags=re.I,
    )
    text = re.sub(r"border-radius\s*:\s*99px\b", "border-radius:var(--radius-pill)", text, flags=re.I)
    text = re.sub(r"border-radius\s*:\s*28px\b", "border-radius:var(--r-lg)", text, flags=re.I)
    text = re.sub(r"border-radius\s*:\s*1\.75rem\b", "border-radius:var(--r-lg)", text, flags=re.I)
    text = re.sub(
        r"--pub-radius-card\s*:\s*[^;]+;",
        "--pub-radius-card: var(--r-lg);",
        text,
        flags=re.I,
    )
    text = re.sub(
        r"--pub-radius-scene\s*:\s*[^;]+;",
        "--pub-radius-scene: var(--r-lg);",
        text,
        flags=re.I,
    )
    text = re.sub(
        r"--form-card-radius\s*:\s*var\([^)]+\)\s*;",
        "--form-card-radius: var(--r-lg);",
        text,
        flags=re.I,
    )
    text = re.sub(
        r"--wa-radius\s*:\s*[^;]+;",
        "--wa-radius: var(--r-lg);",
        text,
        flags=re.I,
    )
    text = re.sub(
        r"--au-radius-card\s*:\s*[^;]+;",
        "--au-radius-card: var(--r-lg);",
        text,
        flags=re.I,
    )
    return text


def px_from_token(num: float, unit: str) -> float:
    unit = unit.lower()
    if unit in ("rem", "em"):
        return num * 16.0
    return num


def normalize_radius_component(raw: str) -> str:
    val = raw.strip()
    if not val:
        return val
    low = val.lower()
    if low == "0":
        return "0"
    if low in ("50%", "999px", "9999px", "99px"):
        return "var(--radius-pill)" if low != "50%" else "50%"
    if low.startswith("var("):
        return val

    m = re.fullmatch(r"([\d.]+)\s*(px|rem|em)", val, re.I)
    if m:
        px = px_from_token(float(m.group(1)), m.group(2))
        if px == 0:
            return "0"
        if px <= 8:
            return "var(--r-sm)"
        if px <= 12:
            return "var(--r-md)"
        if px <= 24:
            return "var(--r-lg)"
        if px <= 36:
            return "var(--r-lg)"
        return val

    # label-sized rem shorthand (~6px at 16px root)
    m = re.fullmatch(r"([\d.]+)\s*rem", val, re.I)
    if m and float(m.group(1)) <= 0.5:
        return "var(--r-sm)"

    return val


def normalize_border_radius_value(value: str) -> str:
    value = value.strip()
    if not value:
        return value
    parts = value.split("/")
    normalized_parts = []
    for part in parts:
        tokens = part.split()
        normalized_parts.append(" ".join(normalize_radius_component(t) for t in tokens))
    return " / ".join(normalized_parts)


def skip_comment(text: str, i: int) -> int:
    end = text.find("*/", i + 2)
    return end + 2 if end != -1 else len(text)


def skip_ws(text: str, i: int) -> int:
    n = len(text)
    while i < n:
        if text[i : i + 2] == "/*":
            i = skip_comment(text, i)
        elif text[i] in " \t\n\r\f":
            i += 1
        else:
            break
    return i


def read_until(text: str, i: int, stop: str) -> tuple[str, int]:
    n = len(text)
    start = i
    while i < n:
        if text[i : i + 2] == "/*":
            i = skip_comment(text, i)
        elif text[i] in stop:
            break
        else:
            i += 1
    return text[start:i], i


def read_block(text: str, i: int) -> tuple[str, int]:
    """i points at '{'; returns inner body and index after closing '}'."""
    assert text[i] == "{"
    depth = 1
    i += 1
    start = i
    n = len(text)
    while i < n and depth:
        if text[i : i + 2] == "/*":
            i = skip_comment(text, i)
            continue
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                break
        i += 1
    body = text[start:i]
    return body, i + 1


def remove_linear_gradients_from_value(value: str) -> str:
    out: list[str] = []
    i = 0
    n = len(value)
    while i < n:
        chunk = value[i : i + 16]
        if chunk.lower().startswith("linear-gradient("):
            j = i + len("linear-gradient(")
            depth = 1
            while j < n and depth:
                if value[j] == "(":
                    depth += 1
                elif value[j] == ")":
                    depth -= 1
                j += 1
            i = j
            continue
        out.append(value[i])
        i += 1
    cleaned = "".join(out)
    cleaned = re.sub(r"\s+", " ", cleaned).strip()
    cleaned = re.sub(r",\s*,+", ", ", cleaned)
    cleaned = re.sub(r"^\s*,\s*", "", cleaned)
    cleaned = re.sub(r"\s*,\s*$", "", cleaned)
    if not cleaned:
        return "none"
    return cleaned


def process_declarations(decls: str, selector: str) -> str:
    if not RADIUS_SKIP.search(selector):
        def br_repl(m: re.Match[str]) -> str:
            return "border-radius:" + normalize_border_radius_value(m.group(1))

        decls = RE_BORDER_RADIUS.sub(br_repl, decls)

    pop = bool(SHADOW_KEYWORDS.search(selector))

    def shadow_repl(_: re.Match[str]) -> str:
        return "box-shadow:var(--shadow-pop)" if pop else "box-shadow:none"

    decls = re.sub(r"box-shadow\s*:\s*[^;]+", shadow_repl, decls, flags=re.I)

    if not GRADIENT_ALLOW.search(selector):

        def bg_repl(m: re.Match[str]) -> str:
            prop = m.group(1)
            val = remove_linear_gradients_from_value(m.group(2))
            return f"{prop}:{val}"

        decls = re.sub(
            r"(background(?:-image)?)\s*:\s*([^;]+)",
            bg_repl,
            decls,
            flags=re.I,
        )

    return decls


def process_rule_list(text: str, parent_selectors: str = "") -> str:
    n = len(text)
    i = 0
    out: list[str] = []

    while i < n:
        i = skip_ws(text, i)
        if i >= n:
            break

        if text[i] == "@":
            at_start = i
            prelude, i = read_until(text, i, "{;")
            i = skip_ws(text, i)
            if i >= n:
                out.append(text[at_start:])
                break
            if text[i] == ";":
                out.append(text[at_start : i + 1])
                i += 1
                continue
            if text[i] == "{":
                body, i = read_block(text, i)
                inner = process_rule_list(body, parent_selectors)
                out.append(prelude + "{" + inner + "}")
                continue
            out.append(text[at_start:i])
            continue

        sel_start = i
        selector, i = read_until(text, i, "{")
        i = skip_ws(text, i)
        if i >= n or text[i] != "{":
            out.append(text[sel_start:i])
            continue

        full_selector = selector.strip()
        if parent_selectors:
            full_selector = f"{parent_selectors} {full_selector}".strip()

        body, i = read_block(text, i)
        if "{" in body and "}" in body and not re.search(
            r"^\s*[a-z-]+\s*:", body, re.I | re.M
        ):
            inner = process_rule_list(body, full_selector)
            out.append(selector + "{" + inner + "}")
        elif "{" in body:
            inner = process_rule_list(body, full_selector)
            out.append(selector + "{" + inner + "}")
        else:
            new_body = process_declarations(body, full_selector)
            out.append(selector + "{" + new_body + "}")

    return "".join(out)


def normalize_file_content(text: str) -> str:
    text = replace_legacy_radius_vars(text)
    return process_rule_list(text)


def main() -> int:
    files = collect_files()
    originals = [p.read_text(encoding="utf-8", errors="ignore") for p in files]
    before = count_metrics("\n".join(originals))

    updated_files: list[str] = []
    for path, original in zip(files, originals):
        updated = normalize_file_content(original)
        updated_files.append(updated)
        if updated != original:
            path.write_text(updated, encoding="utf-8", newline="\n")

    after = count_metrics("\n".join(updated_files))

    print(f"Processed {len(files)} CSS files (excluded tokens.css + app.min.css).")
    print(f"Distinct border-radius values: before={before[0]} after={after[0]}")
    print(f"Non-none box-shadow declarations: before={before[1]} after={after[1]}")
    print(f"linear-gradient( occurrences: before={before[2]} after={after[2]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
