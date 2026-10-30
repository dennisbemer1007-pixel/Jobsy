# Plain-paginalijst (kandidaat)

Inventarisatie van kandidaatroutes onder `Jobsy.Web/Components/Pages/Candidate` die een ingelogde kandidaat kan openen. Geen redirects, POST-acties, onboarding (andere bouwer), paspoort/banenkaart-shell (andere bouwer), of Blazor-bestanden zonder `@page`.

**Plain (#878):** warme strakke shell (`cp-plain-app`, `candidate-plain-app.css` / `candidate-plain-public.css`) wanneer `CandidatePublic:PlainSkin` aan staat (Acceptatie). **Static SSR:** `[NoBlazorRuntime]`.

Laatste scan: branch `acceptatie`, map `Candidate/` + `@page`-directives.

---

## Prioriteit (plain-bouw volgorde)

### Sollicitaties-cluster

| Pagina | Route | Static SSR | Plain |
| --- | --- | --- | --- |
| Sollicitaties (tab) | `/candidate/applications` · `?tab=sollicitaties` (default) · filters `?filter=` · detail `?sel=` | ja | nee |
| Berichten (tab) | `/candidate/applications?tab=berichten` · `?thread=` | ja | nee |
| Mijn activiteiten (tab) | `/candidate/applications?tab=activiteiten` | ja | nee |
| Bewaard | `/candidate/liked` | ja | nee |

### Match

| Pagina | Route | Static SSR | Plain |
| --- | --- | --- | --- |
| Match & swipe | `/candidate/match` | ja | nee |

### Carrière

| Pagina | Route | Static SSR | Plain |
| --- | --- | --- | --- |
| Carrière-overzicht | `/carriere` | ja | nee |
| Carrière leren | `/carriere/leren` | nee (Blazor) | nee |
| Beroepentest | `/candidate/career` | nee (Blazor) | nee |
| Beroepsdag (ESCO) | `/candidate/beroep/{EscoId}/dag` | nee (Blazor) | nee |

### Kompas

| Pagina | Route | Static SSR | Plain |
| --- | --- | --- | --- |
| Mijn Lobsy Kompas | `/candidate/kompas` | ja | nee |

### Profiel

| Pagina | Route | Static SSR | Plain |
| --- | --- | --- | --- |
| Profiel | `/candidate/profile` · alias `/profiel` | ja | nee |
| Profiel bewerken | `/candidate/profile/bewerken` | ja | nee |
| Testdetail (vanuit profiel) | `/profiel/tests/{TestKey}` | nee (Blazor) | nee |

### Instellingen

| Pagina | Route | Static SSR | Plain |
| --- | --- | --- | --- |
| Instellingen | `/candidate/settings` (alias `/candidate/instellingen` → redirect) | ja | nee |

### Na de match (SSR, canoniek `-ssr`)

| Pagina | Route | Static SSR | Plain |
| --- | --- | --- | --- |
| Wacht / overzicht | `/candidate/applications/{ApplicationId}/after-match/wait-ssr` | ja | nee |
| Panel / outcome | `/candidate/applications/{ApplicationId}/after-match/panel-ssr` · `outcome-ssr` | ja | nee |
| Tijd kiezen | `/candidate/applications/{ApplicationId}/after-match/pick-slot-ssr` | ja | nee |
| Herplannen | `/candidate/applications/{ApplicationId}/after-match/reschedule-ssr` | ja | nee |
| Berichten | `/candidate/applications/{ApplicationId}/after-match/messages-ssr` | ja | nee |
| Bevestiging (scherm) | `/candidate/applications/{ApplicationId}/after-match/bevestiging-ssr` | ja | nee |
| Bevestigd | `/candidate/applications/{ApplicationId}/after-match/confirmed-ssr` | ja | nee |
| Geannuleerd | `/candidate/applications/{ApplicationId}/after-match/cancelled-ssr` | ja | nee |
| Herinneringen | `/candidate/applications/{ApplicationId}/after-match/herinneringen-ssr` | ja | nee |

*(Legacy Blazor op `/after-match/blazor`, `/bevestiging`, `/herinneringen` zonder `-ssr`: nog gerouteerd, niet plain; migratie naar bovenstaande SSR-paden.)*

---

## Overige kandidaatroutes (zelfde map)

| Pagina | Route | Static SSR | Plain |
| --- | --- | --- | --- |
| Paspoort | `/candidate/paspoort` (+ subroutes referenties) | ja | **ja** (andere bouwer) |
| Paspoort (EN alias) | `/candidate/passport` → redirect 308 naar `/candidate/paspoort` | ja | n.v.t. |
| Ontdekkingsreis | `/candidate/ontdekkingsreis` | ja | nee |
| Vacatures (legacy panel) | `/candidate/vacancies` | nee (Blazor) | nee |
| Gedeeld | `/candidate/shared` | nee (Blazor) | nee |
| Externe vacature toevoegen | `/candidate/external/add` | ja | nee |
| Externe vacature detail | `/candidate/external/{Id}` | ja | nee |
| Externe vacature solliciteren | `/candidate/external/{Id}/apply` | nee (Blazor) | nee |
| Competentietest | `/candidate/competencies` | nee (Blazor) | nee |
| Cultuur / DISC | `/candidate/culture` · `/candidate/disc` | nee (Blazor) | nee |
| Waarden | `/candidate/values` | nee (Blazor) | nee |
| Diepte-analyse | `/candidate/deep-analysis` · `/candidate/deep-analysis/{Kind}` | nee (Blazor) | nee |
| Diepte-analyse checkout | `/candidate/deep-analysis/checkout` | nee (Blazor) | nee |
| Talent-contacten | `/candidate/talent-contacts` | nee (Blazor) | nee |
| Proactieve matching | `/candidate/proactive-matching` | nee (Blazor) | nee |
| Adviseur werk | `/candidate/adviseur-werk` | ja | nee |
| Wat drijft jou | `/candidate/wat-drijft-jou` | ja | nee |
| Ondernemerscheck | `/candidate/ondernemerscheck` | ja | nee |
| Hoe werkt Lobsy | `/candidate/hoe-werkt-lobsy` | nee (Blazor) | nee |
| Uren (Maqqie) | `/candidate/uren` | nee (Blazor) | nee |
| Privacy | `/candidate/privacy` | nee (Blazor) | nee |
| Research consent | `/candidate/research-consent` | nee (Blazor) | nee |
| Share-in | `/candidate/share-in` | nee (Blazor) | nee |
| Kandidatenkaart-instellingen | `/candidate/kandidatenkaart-instellingen` | nee (Blazor) | nee |
| Binnenkort-placeholder | `/candidate/binnenkort` | nee (Blazor) | nee |
| Gevoelig-demo | `/candidate/gevoelig-demo` (+ `{Screen}`) | nee (Blazor) | nee |

**Banenkaart** (`/banenkaart`, `/banen`) staat buiten `Candidate/`; plain **ja** (andere bouwer).

---

## Niet in deze lijst (geen echte pagina of buiten scope)

| Soort | Voorbeelden |
| --- | --- |
| Alleen redirect | `/candidate/home` → `/home` · `/candidate/tests` · `/profiel/tests` → paspoort tab tests · `/candidate/start/nearby` · `/candidate/passport` |
| Actie-endpoints | `/candidate/actions/set-unavailable` · `/candidate/actions/withdraw-others` |
| Onboarding (andere bouwer) | `/candidate/start` · `/candidate/start/kompas` · `/candidate/start/wizard` · `/candidate/start/preview-strand` |
| Blazor zonder route | `Applications.razor` · `DiscoveryJourney.razor` · `ExternalVacancyDetail.razor` (SSR vervangt) |
| Blazor-duplicaat / legacy | `/candidate/match/blazor` · `/candidate/profile/bewerken/legacy` · `/candidate/applications/{id}/after-match/blazor` |

---

## Mockup-dekking (#878, branch PR 878)

Mockup HTML in `docs/mockups/kandidaat/plain/src/` dekt vooral onboarding, kompas (19), paspoort (20–21). Geen apart scherm voor sollicitaties, match, carrière, profiel of instellingen: die pagina’s gebruiken dezelfde plain-bouwstenen (Inter, accent `#A8481F`, body 18/17px, knoppen min. 52px, logo links, footer-nav volle breedte).
