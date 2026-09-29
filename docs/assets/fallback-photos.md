# Vacancy category fallback photos

Vacancies without a photo use `VacancyImageUrls.Placeholder()` →
`/images/vacancies/{slug}.webp` (≈800×533, under 80 KB), with a 400px companion
`{slug}-400.webp` for `srcset`.

All photos are used under the [Unsplash License](https://unsplash.com/license)
(commercial use allowed; attribution not required).

## Category → slug mapping

| Category (product) | Slug / files | Existing `WorkType` |
|---|---|---|
| horeca | `horeca` | `Horeca` |
| zorg | `zorg` | `Zorg` |
| groen / tuinbouw | `tuinbouw` | `Tuinbouw` |
| kantoor / administratie | `kantoor` | `Kantoor` |
| retail | `winkel` | `Winkel` |
| logistiek | `logistiek` | `Logistiek` |
| techniek | `bouw` | `Bouw` |
| schoonmaak | `schoonmaak` | `Schoonmaak` |
| onderwijs | `onderwijs` | *(string label only; no enum flag yet)* |
| overig | `flex` | default / `None` |
| *(factory line)* | `productie` | `Productie` |

Aliases in `NormalizeSlug`: `retail`→`winkel`, `groen`→`tuinbouw`,
`techniek`→`bouw`, `administratie`→`kantoor`, `overig`→`flex`,
`onderwijs`/`education`→`onderwijs`.

## Sources

| Slug | Source URL | License |
|---|---|---|
| horeca | https://unsplash.com/photos/photo-1554118811-1e0d58224f24 · café interior | Unsplash License |
| zorg | https://unsplash.com/photos/photo-1576091160399-112ba8d25d1d · healthcare / stethoscope | Unsplash License |
| tuinbouw | https://unsplash.com/photos/photo-1416879595882-3373a0480b5b · greenhouse plants | Unsplash License |
| kantoor | https://unsplash.com/photos/photo-1497366216548-37526070297c · open office | Unsplash License |
| winkel | https://unsplash.com/photos/photo-1441986300917-64674bd600d8 · retail store | Unsplash License |
| logistiek | https://unsplash.com/photos/photo-1586528116311-ad8dd3c8310d · warehouse pallets | Unsplash License |
| bouw | https://unsplash.com/photos/photo-1504307651254-35680f356dfd · construction site | Unsplash License |
| schoonmaak | https://unsplash.com/photos/photo-1581578731548-c64695cc6952 · professional cleaning | Unsplash License |
| onderwijs | https://unsplash.com/photos/photo-1580582932707-520aed937b7b · classroom | Unsplash License |
| flex | https://unsplash.com/photos/photo-1521737711867-e3b97375f902 · team / general work | Unsplash License |
| productie | https://unsplash.com/photos/photo-1565793298595-6a879b1d9492 · industrial equipment | Unsplash License |

CDN download used at import time:
`https://images.unsplash.com/{photo-id}?w=1200&h=800&fit=crop&q=80&auto=format`

## Files on disk

Under `Jobsy.Web/wwwroot/images/vacancies/`:

- `{slug}.webp` — primary (~800px wide, &lt; 80 KB)
- `{slug}-400.webp` — list/card `srcset` companion

Legacy `{slug}-{0\|1}.svg` icons are obsolete; `MockVacancyMedia.NeedsImageBackfill`
treats those paths as needing a one-time replace with the WebP placeholder.
