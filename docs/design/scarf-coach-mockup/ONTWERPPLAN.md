# Ontwerpplan: coach-voorkeuren (SCARF intern)

**Status:** mockup + ontwerp — geen productiecode in deze PR.  
**Datum:** 10 november 2026.  
**Doel:** de kreeft-coach stemt toon, uitleg en tips af op wat een kandidaat als bedreiging of steun ervaart bij verandering — zonder scores, zonder matching, alleen voor de kandidaat zelf.

---

## 1. Waar in de app?

### Primair: Paspoort → tab **Gegevens** (`/candidate/paspoort?tab=data`)

- Nieuwe accordeon-sectie **“Hoe wil je tips van mij?”** onder de bestaande kaarten (persoonlijk, beschikbaarheid, werkstatus).
- Bestaande velden blijven zichtbaar; de vragen zijn geen los scherm maar een extra `detail-card passport-data__item` in dezelfde scroll-flow als vandaag (`PassportSsrShells.AppendDataPanel`).
- Deep link: `?tab=data&section=coach-voorkeuren` (analoog aan `availability` / `work-status`).

### Secundair (fase 2, optioneel)

- **Onboarding wizard** stap 4–5: korte versie (max. 3 vragen) + “Later in paspoort”.
- **Profiel-hub** (`/candidate/profile`) alleen wanneer paspoort-feature uit staat — zelfde sectie in `ProfileSsrShells`.
- **Scholieren (13+):** aparte **leerling-flow** (`LeerlingCoach`, groep 7/8/VO) heeft nu eigen copy; dezelfde vragen en opslag kunnen daar pas in fase 2 als Dennis de scholier-paspoort-flow gelijk wil trekken met kandidaat. In deze mockup tonen we alleen de **kandidaat-paspoort**-context; scholieren volgen dezelfde technische opslag maar andere route/UI wordt in bouw gefaseerd.

---

## 2. Vragen (B1, 9 stuks, geen jargon)

Gebruikers zien **niet** de term SCARF. Intern mappen we naar domeinen voor de coach-prompt.

| # | Domein (intern) | Vraag | Antwoordopties (meervoud, kiezen mag) |
|---|-----------------|-------|----------------------------------------|
| 1 | Zekerheid | Wat maakt je **onzeker** als je aan ander werk denkt? | Ik weet niet wat er daarna komt · Ik ben bang iets verkeerd te doen · Ik mis vaste uren of loon · Ik ken de mensen nog niet · Niks, dat hoeft niet |
| 2 | Zekerheid | Wat helpt jou om je **zeker** te voelen? | Duidelijke volgende stap · Vaste afspraken · Iemand die me uitlegt · Rustig tempo · Overslaan |
| 3 | Autonomie | Waar wil je **zelf over beslissen**? | Welke banen ik bekijk · Wanneer ik solliciteer · Hoe ik me voorbereid · Met wie ik praat · Alles mag rustig vanuit Lobsy |
| 4 | Autonomie | Wat voelt als **te veel sturing**? | Te veel stappen in één keer · Veel regels · Iemand die voor me kiest · Te snelle beslissing · Overslaan |
| 5 | Status | Wat is fijn dat anderen **van je zien** als je een stap zet? | Dat ik mijn best doe · Dat ik leer · Dat ik betrouwbaar ben · Dat ik eerlijk ben over wat ik kan · Overslaan |
| 6 | Verbondenheid | **Met wie** werk je het liefst of krijg je steun? | Alleen, met duidelijke uitleg · Met een collega of team · Met een begeleider · Met familie of vrienden · Overslaan |
| 7 | Eerlijkheid | Wat moet **eerlijk** aanvoelen voor jou? | Eerlijke uitleg over kansen · Geen beloftes die niet kloppen · Gelijke kans · Duidelijk wat ik moet doen · Overslaan |
| 8 | **Hoe denk jij?** | **Hoe leer jij iets nieuws het liefst?** | Eerst feiten en cijfers · Stap voor stap · Via mensen en gevoel · Eerst het grote plaatje |
| 9 | **Hoe denk jij?** | **Wat doe je eerst bij een nieuwe taak?** | Zelfde vier keuzes als vraag 8 |

### Hoe denk jij? — afbakening (geen test)

Twee korte vragen in **eigen B1-woordkeuze**; geen kopie van HCX, Herrmann/HBDI of andere denkstijl-producten.

- **Wel:** de coach past **uitlegvorm** aan (feiten, stappen, menselijk, overzicht) op basis van gekozen woorden; in overzicht onder **Zo denk je** alleen tekst, geen score.
- **Niet:** quadrant, type, percentiel, matchfactor; niets in paspoort voor werkgevers of matching; **geen validiteitsclaim** — dit is geen persoonlijkheidstest.
- **Opslag:** `thinking_learn`, `thinking_new_task` in `CoachTonePreferences.answers`.

**Interactie**

- Per vraag: `lobsy-choice-grid` / `lobsy-choice-tile` (bestaand patroon).
- Voortgang: “Vraag X van 9”.
- Knoppen: **Opslaan**, **Later**, **Alles overslaan**, **Mijn antwoorden wissen** (in bewerkmodus).
- Opgeslagen antwoorden zijn altijd zichtbaar en wijzigbaar in de accordeon.

---

## 3. Datamodel / opslag

**Niet** in partner-paspoort, werkgever-export, matching of RIASEC-scores.

```json
// CandidatePreferencesJson → nieuwe sleutel (alleen kandidaat + coach)
"CoachTonePreferences": {
  "version": 1,
  "updatedAtUtc": "2026-11-10T12:00:00Z",
  "skipped": false,
  "answers": {
    "certainty_worries": ["onbekende_toekomst", "vaste_afspraken_missen"],
    "certainty_helps": ["duidelijke_volgende_stap"],
    "autonomy_choose": ["welke_banen", "wanneer_solliciteren"],
    "autonomy_too_much": [],
    "status_seen_as": ["mijn_best_doen"],
    "relatedness_support": ["begeleider"],
    "fairness_important": ["eerlijke_kansen"],
    "thinking_learn": ["stap_voor_stap"],
    "thinking_new_task": ["via_mensen_gevoel"]
  }
}
```

- **Opslag:** `User.PreferencesJson` (zelfde pad als `SharedWorkPreferenceChoices`), via bestaande `PUT api/me/profile` of paspoort `save-data` uitbreiding.
- **Entity:** geen aparte tabel in v1; optioneel `CandidatePrivatePreferences` als we dislikes-achtige scheiding willen — niet nodig voor MVP.
- **Audit:** alleen `UpdatedAtUtc` in JSON; geen aparte partner-log.

**Feature flag (standaard uit):**

```csharp
// PlatformFeature.cs
CandidateCoachTonePreferences,  // default false
```

`FeatureVisible` op data-sectie; API negeert saves als flag uit (of 404 `feature_disabled`).

---

## 4. Hoe de kreeft het gebruikt

### 4.1 Prompt / toon (geen nieuwe feiten)

Uitbreiding van `AssistantChatService` / `CandidateFactGuard`:

1. **Factsheet** (`CandidateFactSheet`): alleen expliciete coach-voorkeuren als feiten (“kandidaat zegt: wil duidelijke volgende stap”) — geen afgeleide persoonlijkheidslabels.
2. **Tone addendum** (niet in user-facing tekst): als `certainty_helps` bevat `duidelijke_volgende_stap` → instructie: korte genummerde stappen, geen open einde. Als `autonomy_choose` sterk → bied 2–3 keuzes, vraag “wat wil jij?”. Als `fairness_important` bevat `geen_valse_beloftes` → geen optimistische claims over vacatures.
3. **Geen verzonnen feiten:** bestaande `CandidateFactGuard.StrictAddendum` + fallback `UnknownFactReply` blijven leidend; voorkeuren zijn **extra toegestane** citaten, geen vrijbrief.
4. **Scripted pad:** `CandidateCoachScript` kan voor FAQ’s (geen LLM) dezelfde toonregels gebruiken.

### 4.2 Wat de kandidaat ziet (voorbeelden in mockup)

| Profiel (uit antwoorden) | Verschil in kreeft-bericht |
|--------------------------|----------------------------|
| Hulp bij zekerheid (“duidelijke volgende stap”) | Genummerd: “1) Kijk naar … 2) Daarna …” — verwijst alleen naar eigen antwoord |
| Autonomie (“zelf banen kiezen”) | “Je mag kiezen: A … of B … Wat past nu bij jou?” |

Coach **noemt nooit** scores, percentiles of “jouw type”.

### 4.3 Uitsluitingen (hard)

| Wel | Niet |
|-----|------|
| Kreeft-panel chat voor ingelogde kandidaat | Partner-paspoort PDF/sectie |
| Tone in ontdekkingsreis journey-bubbles (optioneel fase 2) | Werkgever matching, kandidatenkaart, proactive matching |
| Leesbare/editbare antwoorden op Gegevens-tab | AI Act “profiling” voor beslissingen over toegang tot werk |

---

## 5. Privacy / AVG en AI Act

- **Rechtsgrond:** uitvoering overeenkomst / gerechtvaardigd belang (betere begeleiding); geen profiling voor recruitment-beslissingen.
- **Dataminimalisatie:** alleen gekozen chips; geen verplichte vrije tekst in v1.
- **Inzagerecht:** antwoorden staan op Gegevens-tab; export via bestaande profiel-export indien aanwezig (toevoegen aan export-lijst bij bouw).
- **Verwijderen:** “Mijn antwoorden wissen” leegt `CoachTonePreferences`; valt onder recht op gegevenswissing samen met profiel-update.
- **AI Act:** systeem is **niet** high-risk employment-AI voor ranking; voorkeuren zijn self-reported context voor **coaching-UI**, geen geautomatiseerde besluitvorming. Documenteer in intern DPIA-addendum bij productie.
- **Leeftijd 13+:** zelfde flow technisch mogelijk; scholieren-route apart beoordelen met ouderlijk toestemming-pad waar al verplicht (bestaand school-consent).

---

## 6. Testaanpak (bij bouw)

| Laag | Wat |
|------|-----|
| Unit | JSON serialisatie, flag-off negeert save, wissen leegt sleutel |
| `CandidateFactGuard` | Coach-antwoord met quote uit voorkeuren OK; verzonnen diploma blijft reject |
| E2E | Paspoort data: invullen, opslaan, herladen, wissen; kreeft-chat smoke (twee tone-fixtures) |
| Regressie | Partner PDF snapshot: **geen** coach-voorkeuren sectie |
| Accessibility | Accordeon keyboard, 44px targets, `aria-live` op coach-panel |

Mockup-screenshots in deze PR: visuele review door Dennis (geen geautomatiseerde UI-test op HTML).

---

## 7. Inschatting bouwwerk (realistisch)

| Onderdeel | Complexiteit |
|-----------|----------------|
| `PlatformFeature` + admin toggle | Klein |
| DTO + PreferencesJson + save endpoints | Klein–middel |
| SSR data-sectie (paspoort + accordeon) | Middel (volgt bestaande data-panel pattern) |
| `AssistantChatService` tone addendum + tests | Middel |
| i18n keys (NL eerst, EN later) | Klein |
| Scholier-route deelfase | Aparte PR (niet in MVP) |
| DPIA-notitie + docs | Klein |

**Totaal MVP (alleen kandidaat paspoort + coach):** ca. **1 gerichte PR** (UI + API + coach) + **1 follow-up PR** (tests hardening, export, scholier). Geen migratie: JSON-only.

---

## 8. Mockup-bestanden

| Bestand | Inhoud |
|---------|--------|
| `screenshots/01-*` | Vragenstap Gegevens (volle kaart, voortgang, icoon-tegels) |
| `screenshots/02-*` | Overzicht na invullen (samenvatting, aanpassen, wissen) |
| `screenshots/03-*` / `04-*` | Past bij? gevuld + kreeft-chat (zekerheid / autonomie) |
| `screenshots/05-kreeft-vergelijking-1440.png` | Beide antwoordstijlen naast elkaar |
| `previews/*.html` | Zelfde schermen als HTML |
| `tools/screenshots/capture-scarf-coach-mockup.mjs` | Generator |

PNG’s in repo: `docs/design/scarf-coach-mockup/screenshots/`.
