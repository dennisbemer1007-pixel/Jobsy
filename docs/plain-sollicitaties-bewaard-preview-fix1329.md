# Plain Sollicitaties + Bewaard — preview fix (#1329)

Seed: Sam de Wit · Plain skin via `CandidatePublic:PlainSkin` (Acceptatie) / `?_plainSkin=1` (Development).

## Previews (na fix)

| Pagina | 1440 | 390 |
| --- | --- | --- |
| Sollicitaties | `/opt/cursor/artifacts/preview-sollicitaties-1440.png` | `/opt/cursor/artifacts/preview-sollicitaties-390.png` |
| Bewaard | `/opt/cursor/artifacts/preview-bewaard-1440.png` | `/opt/cursor/artifacts/preview-bewaard-390.png` |

Verticale ruimte (Playwright guard): onder header &lt; 48px, grootste lege strook &lt; 8px op alle vier captures.

## Eerlijke vergelijking mockup #878 (`docs/mockups/kandidaat/plain`)

| Aspect | Mockup | Deze previews |
| --- | --- | --- |
| Lettertype | Inter, body 18/17 | Inter via `--cp-font` + plain scope op app-shell |
| Accent | #A8481F knoppen/chips/tabs actief | `--brand`/`--cp-accent` overschreven in plain scope |
| Knophoogte | min. 52px | min-height 52px op chips, tabs, CTA’s |
| Sollicitatiekaarten | Compacte rij, geen lege band onder chevron | Grid rij 1 + status rij 2; chevron rechtsboven; `sollic-card__step` verborgen in plain |
| Bewaard lijst | Geen score-labels, geen rail-illustraties | Geen `kb-fit`, geen rail; rij-layout ook op desktop; geen lege thumb zonder foto |
| Segment-tabs | Sollicitaties-cluster | Zelfde `ApplicationsSsrClusterTabsSsr` op Sollicitaties én Bewaard (geen Shared-tab meer in plain) |
| Coach-pill | Mag content niet bedekken | Extra `padding-bottom` op main + coach boven bottom nav; klikaudit faalt op `blocked-by-coach-pill` |
| Header/shell | Bouwer 3 | Niet gewijzigd |

## Dennis-checklist (fixpoging 1)

- [x] Inter / accent / body 18–17 / knoppen 52px (CSS plain scope)
- [x] Sollicitaties 1440: compacte kaarten (geen lege chevron-band)
- [x] Sollicitaties 390: chips/titel wrap, status compact, bottom padding + coach clearance
- [x] Bewaard 1440: geen grijs media-vak zonder foto, sort-pictogram plain, `1 baan`, geen rail, footer-nav weg, geen Sterke match
- [x] Bewaard 390: geen broken img, toolbar wrap, padding onder content, tabs consistent
- [x] Sam de Wit in illustrated test sign-in

## Restrisico

- Live Acceptatie-data kan afwijken van stub (meer rijen, NDM-kaarten); visuele check op `acceptatie.lobsy.nl` na merge blijft nodig.
- Activiteiten-tab verschijnt alleen als platform `ActivityLog` aan staat (zelfde regel als Sollicitaties).
