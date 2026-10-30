"""Watercolor-style inline SVGs for groep 7/8 mockups (world tint via CSS classes main/accent/shade)."""

def _svg(inner: str, vb: str = "0 0 48 48") -> str:
    return (
        f'<svg viewBox="{vb}" class="painted" aria-hidden="true" xmlns="http://www.w3.org/2000/svg">'
        f'<ellipse class="shade" cx="24" cy="28" rx="17" ry="14"/>'
        f"{inner}</svg>"
    )

CHIP = {
    "sport": _svg(
        '<path class="main" d="M24 8c-6 0-11 4-12 10 0 5 3 9 8 11l-2 9 6-4 6 4-2-9c5-2 8-6 8-11-1-6-6-10-12-10z"/>'
        '<path class="accent" d="M24 8v32M12 18h24M14 28h20" fill="none" stroke-width="2" stroke-linecap="round"/>'
    ),
    "buiten": _svg(
        '<circle class="accent" cx="24" cy="18" r="7"/>'
        '<path class="main" d="M10 36c4-10 24-10 28 0" fill="none" stroke-width="3" stroke-linecap="round"/>'
        '<path class="main" d="M8 38h32" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "dieren": _svg(
        '<path class="main" d="M14 22c0-6 4-10 10-10s10 4 10 10-4 14-10 14-10-8-10-14z"/>'
        '<circle class="accent" cx="19" cy="20" r="2"/><circle class="accent" cx="29" cy="20" r="2"/>'
        '<path class="main" d="M20 26c2 2 6 2 8 0" fill="none" stroke-width="2" stroke-linecap="round"/>'
        '<path class="accent" d="M12 18l-4-3M36 18l4-3" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "gamen": _svg(
        '<rect class="main" x="10" y="16" width="28" height="18" rx="6"/>'
        '<path class="accent" d="M18 22v8M14 26h8" stroke-width="2.5" stroke-linecap="round"/>'
        '<circle class="accent" cx="30" cy="24" r="2"/><circle class="accent" cx="34" cy="28" r="2"/>'
    ),
    "tekenen": _svg(
        '<path class="accent" d="M12 34l20-20 6 6-20 20z"/>'
        '<path class="main" d="M30 14l4 4" stroke-width="3" stroke-linecap="round"/>'
        '<path class="main" d="M10 38h12" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "muziek": _svg(
        '<path class="main" d="M18 34V14l16-4v20" fill="none" stroke-width="3" stroke-linecap="round"/>'
        '<circle class="accent" cx="14" cy="34" r="4"/><circle class="accent" cx="30" cy="30" r="4"/>'
    ),
    "koken": _svg(
        '<path class="main" d="M14 20h20v14H14z"/><path class="accent" d="M18 20v-4M24 20v-6M30 20v-4" stroke-width="2.5" stroke-linecap="round"/>'
        '<path class="main" d="M12 34h24" stroke-width="2" stroke-linecap="round"/>'
    ),
    "fietsen-repareren": _svg(
        '<circle class="main" cx="16" cy="32" r="7" fill="none" stroke-width="3"/>'
        '<circle class="main" cx="32" cy="32" r="7" fill="none" stroke-width="3"/>'
        '<path class="accent" d="M16 32l8-12h6l4 12" stroke-width="2.5" stroke-linecap="round" fill="none"/>'
    ),
    "bouwen": _svg(
        '<path class="main" d="M10 36V18l14-10 14 10v18z"/>'
        '<path class="accent" d="M18 36V24h12v12" stroke-width="2" fill="none"/>'
    ),
    "techniek": _svg(
        '<path class="main" d="M30 12l6 6-12 12-8-2 2-8z"/>'
        '<path class="accent" d="M12 36l10-10" stroke-width="3" stroke-linecap="round"/>'
    ),
    "lezen": _svg(
        '<path class="main" d="M12 14c4-2 8-2 12 0 4-2 8-2 12 0v20c-4-2-8-2-12 0-4-2-8-2-12 0z"/>'
        '<path class="accent" d="M24 14v20" stroke-width="2"/>'
    ),
    "dansen": _svg(
        '<path class="main" d="M20 36l6-14 4 6 6-10" fill="none" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>'
        '<circle class="accent" cx="22" cy="12" r="4"/>'
    ),
    "theater": _svg(
        '<path class="main" d="M10 16h28l-4 18H14z"/>'
        '<path class="accent" d="M8 16h32" stroke-width="3" stroke-linecap="round"/>'
        '<circle class="accent" cx="18" cy="12" r="2"/><circle class="accent" cx="24" cy="10" r="2"/><circle class="accent" cx="30" cy="12" r="2"/>'
    ),
    "filmpjes": _svg(
        '<rect class="main" x="10" y="14" width="28" height="20" rx="4"/>'
        '<path class="accent" d="M22 20l8 4-8 4z"/>'
    ),
    "mode": _svg(
        '<path class="main" d="M16 12l8-4 8 4v22H16z"/>'
        '<path class="accent" d="M16 18h16" stroke-width="2"/>'
    ),
    "kleine-kinderen": _svg(
        '<circle class="accent" cx="24" cy="16" r="6"/>'
        '<path class="main" d="M12 38c3-8 18-8 24 0" fill="none" stroke-width="3" stroke-linecap="round"/>'
    ),
    "natuur": _svg(
        '<path class="main" d="M24 10c-8 6-12 14-10 22 2-6 6-10 10-10s8 4 10 10c2-8-2-16-10-22z"/>'
        '<path class="accent" d="M24 36v-6" stroke-width="3" stroke-linecap="round"/>'
    ),
    "autos": _svg(
        '<path class="main" d="M10 28l4-10h20l4 10z"/>'
        '<circle class="accent" cx="16" cy="30" r="4"/><circle class="accent" cx="32" cy="30" r="4"/>'
    ),
    "computers": _svg(
        '<rect class="main" x="10" y="12" width="28" height="18" rx="3"/>'
        '<path class="accent" d="M16 38h16" stroke-width="3" stroke-linecap="round"/>'
    ),
    "puzzels": _svg(
        '<rect class="main" x="12" y="12" width="10" height="10" rx="2"/>'
        '<rect class="accent" x="26" y="12" width="10" height="10" rx="2"/>'
        '<rect class="accent" x="12" y="26" width="10" height="10" rx="2"/>'
        '<rect class="main" x="26" y="26" width="10" height="10" rx="2"/>'
    ),
    "rekenen": _svg(
        '<rect class="main" x="12" y="10" width="24" height="28" rx="4"/>'
        '<path class="accent" d="M18 18h12M18 24h12M18 30h8" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "talen": _svg(
        '<circle class="main" cx="24" cy="24" r="14" fill="none" stroke-width="3"/>'
        '<path class="accent" d="M10 24h28M24 10c4 6 4 22 0 28M24 10c-4 6-4 22 0 28" fill="none" stroke-width="2"/>'
    ),
    "reizen": _svg(
        '<path class="main" d="M12 32l12-18 12 18z"/>'
        '<path class="accent" d="M24 14v18" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "programmeren": _svg(
        '<path class="main" d="M16 16l-8 8 8 8M32 16l8 8-8 8" fill="none" stroke-width="3" stroke-linecap="round"/>'
        '<path class="accent" d="M26 12l-4 24" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "voor-de-klas": _svg(
        '<path class="main" d="M12 32V16l12-6 12 6v16" fill="none" stroke-width="3"/>'
        '<path class="accent" d="M18 32v-8h12v8" stroke-width="2"/>'
    ),
    "lang-stilzitten": _svg(
        '<rect class="main" x="14" y="12" width="20" height="24" rx="4"/>'
        '<path class="accent" d="M18 20h12M18 26h12" stroke-width="2"/>'
    ),
    "hard-werken-kou": _svg(
        '<path class="main" d="M24 8v28" stroke-width="3" stroke-linecap="round"/>'
        '<path class="accent" d="M16 16h16M16 24h16M16 32h16" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "veel-lezen": _svg(
        '<rect class="main" x="14" y="12" width="20" height="26" rx="3"/>'
        '<path class="accent" d="M18 18h12M18 24h12M18 30h8" stroke-width="2"/>'
    ),
    "alleen-werken": _svg(
        '<circle class="accent" cx="24" cy="14" r="5"/>'
        '<path class="main" d="M14 38c2-7 14-7 20 0" fill="none" stroke-width="3" stroke-linecap="round"/>'
    ),
    "druk-lawaai": _svg(
        '<path class="main" d="M14 22h8l6-5v18l-6-5h-8z"/>'
        '<path class="accent" d="M32 18a6 6 0 010 12" fill="none" stroke-width="2.5"/>'
    ),
    "vies-worden": _svg(
        '<path class="main" d="M16 20c0 8 4 14 8 14s8-6 8-14" fill="none" stroke-width="3"/>'
        '<circle class="accent" cx="20" cy="18" r="2"/><circle class="accent" cx="28" cy="22" r="1.5"/>'
    ),
}

JOB = {
    "dierenverzorger": _svg(
        '<path class="main" d="M14 26c0-8 5-14 10-14s10 6 10 14-4 12-10 12-10-4-10-12z"/>'
        '<ellipse class="accent" cx="20" cy="24" rx="2" ry="3"/><ellipse class="accent" cx="28" cy="24" rx="2" ry="3"/>'
        '<path class="accent" d="M10 20h6M32 20h6" stroke-width="2.5" stroke-linecap="round"/>'
        '<path class="main" d="M22 32c1 2 3 2 4 0" fill="none" stroke-width="2"/>'
    ),
    "bioloog": _svg(
        '<path class="main" d="M18 36c-6-10-2-22 6-26 8 4 12 16 6 26" fill="none" stroke-width="3"/>'
        '<path class="accent" d="M12 20c6 2 10 8 10 16" fill="none" stroke-width="2.5"/>'
        '<circle class="accent" cx="30" cy="14" r="4"/>'
    ),
    "grafisch-ontwerper": _svg(
        '<rect class="main" x="12" y="14" width="24" height="18" rx="3"/>'
        '<path class="accent" d="M16 30l8-10 4 4 8-12" fill="none" stroke-width="2.5" stroke-linecap="round"/>'
        '<circle class="accent" cx="34" cy="18" r="3"/>'
    ),
    "hovenier": _svg(
        '<path class="main" d="M24 10v26" stroke-width="3" stroke-linecap="round"/>'
        '<path class="accent" d="M14 22c4-8 20-8 20 0-4 6-8 8-10 8s-6-2-10-8z"/>'
        '<path class="main" d="M10 36h28" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "brandweer": _svg(
        '<path class="main" d="M24 38c8-6 10-14 6-22-2 4-6 6-6 10 0-6-4-10-8-12 2 10-2 18 8 24z"/>'
        '<path class="accent" d="M18 36h12" stroke-width="3" stroke-linecap="round"/>'
    ),
}

DAY = {
    "start": _svg(
        '<circle class="accent" cx="30" cy="16" r="8"/>'
        '<path class="main" d="M8 34h32" stroke-width="2.5" stroke-linecap="round"/>'
        '<path class="accent" d="M14 28l4-6 4 4 6-10" fill="none" stroke-width="2.5" stroke-linecap="round"/>',
        "0 0 48 48",
    ),
    "morning": _svg(
        '<circle class="accent" cx="28" cy="14" r="9"/>'
        '<path class="main" d="M10 30c6-6 22-6 28 0" fill="none" stroke-width="3" stroke-linecap="round"/>'
        '<path class="accent" d="M12 36h24" stroke-width="2" stroke-linecap="round"/>'
    ),
    "pause": _svg(
        '<rect class="main" x="14" y="16" width="8" height="18" rx="2"/>'
        '<rect class="main" x="26" y="16" width="8" height="18" rx="2"/>'
        '<path class="accent" d="M12 36h24" stroke-width="2.5" stroke-linecap="round"/>'
    ),
    "afternoon": _svg(
        '<circle class="accent" cx="26" cy="18" r="7"/>'
        '<path class="main" d="M8 32c5-8 27-8 32 0" fill="none" stroke-width="3"/>'
        '<path class="accent" d="M16 12l4 4M32 12l-4 4" stroke-width="2" stroke-linecap="round"/>'
    ),
    "close": _svg(
        '<path class="main" d="M14 28a10 10 0 1 1 20 0" fill="none" stroke-width="3"/>'
        '<path class="accent" d="M24 18v-6M12 36h24" stroke-width="2.5" stroke-linecap="round"/>'
        '<circle class="accent" cx="20" cy="32" r="1.5"/><circle class="accent" cx="28" cy="30" r="1.5"/>'
    ),
}
DAY["talk"] = DAY["pause"]
DAY["plan"] = DAY["morning"]
DAY["handover"] = DAY["afternoon"]

PLUS = _svg(
    '<path class="main" d="M24 12v24M12 24h24" stroke-width="4" stroke-linecap="round"/>'
)

WORLD_DECO = {
    "zee": (
        '<span class="wdeco wd-shell wd1"></span><span class="wdeco wd-wave wd2"></span>'
        '<span class="wdeco wd-fish wd3"></span><span class="wdeco wd-shell wd4"></span>'
    ),
    "tuin": (
        '<span class="wdeco wd-leaf wd1"></span><span class="wdeco wd-flower wd2"></span>'
        '<span class="wdeco wd-leaf wd3"></span><span class="wdeco wd-petal wd4"></span>'
    ),
    "ster": (
        '<span class="wdeco wd-star wd1"></span><span class="wdeco wd-moon wd2"></span>'
        '<span class="wdeco wd-star wd3"></span><span class="wdeco wd-star wd4"></span>'
    ),
}

def chip_icon(key: str) -> str:
    return CHIP.get(key, CHIP["natuur"])

def job_icon(key: str) -> str:
    return JOB.get(key, JOB["dierenverzorger"])

def job_none_icon() -> str:
    return _svg(
        '<circle class="main" cx="16" cy="24" r="6" fill="none" stroke-width="2.5"/>'
        '<circle class="main" cx="32" cy="24" r="6" fill="none" stroke-width="2.5"/>'
        '<path class="accent" d="M22 24h4" stroke-width="3" stroke-linecap="round"/>'
    )

def day_icon(key: str) -> str:
    return DAY.get(key, DAY["morning"])

def world_deco(world_key: str) -> str:
    return WORLD_DECO.get(world_key, WORLD_DECO["zee"])


def category_art_img(category_slug: str) -> str:
    return f"img/jobs/{category_slug}.jpg"


def day_hero_art_img(category_slug: str = "zorg-dieren") -> str:
    slug = (category_slug or "zorg-dieren").strip()
    return f"img/jobs/{slug}-hero.jpg"


def day_scene_art(category_slug: str = "zorg-dieren") -> str:
    slug = (category_slug or "zorg-dieren").strip()
    return f"img/jobs/{slug}.jpg"


# Back-compat for single-category mockups (dierenverzorger pilot).
DAY_SCENE_ART = day_scene_art("zorg-dieren")
