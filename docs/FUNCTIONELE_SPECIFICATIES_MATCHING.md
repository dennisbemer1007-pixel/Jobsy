# Functionele specificaties: Matching, dagdelen, uren & Arbeidstijdenwet

**Product:** Lobsy (codebase: Jobsy)  
**Scope:** Kandidaat- en werkgeverskant  
**Status:** Functionele specificatie (implementatieklaar)  
**Versie:** 1.0  
**Gerelateerd:** `REQUIREMENTS.md`, bestaande beschikbaarheidsmatrix op `/candidate/profile`

---

## 0. Uitgangspunten

### 0.1 Productbelofte
Lobsy brengt werkgevers en werkzoekenden **hyper-lokaal** bij elkaar. Matching rust op drie pijlers naast bestaande reistijd/vervoer:

1. **Reistijd** (bestaand: PostGIS + OSRM)
2. **Uren per week** (nieuw: min/max + automatische urencategorie)
3. **Dagdelen / roosters** (nieuw op vacature; uitbreiding van bestaande kandidatenmatrix)

### 0.2 Geen harde minimumleeftijd in de UI
Werkgevers zetten **geen** minimumleeftijd op een vacature. Leeftijdsfiltering gebeurt **uitsluitend op de achtergrond**, feitelijk en strikt op basis van de **Arbeidstijdenwet** en gerelateerde arboregels, via verplichte **taak-vinkjes**.

Doel: leeftijdsdiscriminatie voorkomen in de wervings-UI, terwijl wettelijke bescherming van jongeren (15–17) wel wordt nageleefd.

### 0.3 Huidige staat vs. deze specificatie

| Onderdeel | Bestaat nu | Deze specificatie |
|-----------|------------|-------------------|
| Kandidaat dagdelen-matrix | Ja (`Ma`–`Zo` × `Ochtend`/`Middag`/`Avond`/`Nacht`) | Uitbreiden met vaste tijdsvensters + “Tijden in overleg” |
| Vacature dagdelen-matrix | Nee | Nieuw |
| Min/max uren/week | Nee | Nieuw (vacature + profiel) |
| Urencategorie-filters | Nee | Nieuw (afgeleid) |
| Wettelijke taak-vinkjes | Nee (alleen `WorkPermitConfirmed`) | Nieuw |
| Matchingspercentage | Nee | Nieuw |
| Gulden Middenweg bij solliciteren | Nee (harde gates) | Nieuw |
| Optioneel motivatieveld | Nee (`AboutMe` ≠ sollicitatiemotivatie) | Nieuw |
| Match-% in werkgeversdashboard | Nee | Nieuw |
| CSV/API: uren + taakvinkjes | Nee | Nieuw (verplicht); dagdelen optioneel |

---

## 1. Domeinmodel (gedeeld)

### 1.1 Dagdelen (canoniek)

| Code | Label (NL) | Tijdsvenster |
|------|------------|--------------|
| `Ochtend` | Ochtend | 06:00 – 12:00 |
| `Middag` | Middag | 12:00 – 18:00 |
| `Avond` | Avond | 18:00 – 23:00 |
| `Nacht` | Nacht | 23:00 – 06:00 |

**Dagen (canoniek, bestaande keys):** `Ma`, `Di`, `Wo`, `Do`, `Vr`, `Za`, `Zo`  
**UI-labels:** Maandag t/m Zondag.

### 1.2 SchedulePayload (JSON / domain value object)

```text
SchedulePayload {
  FlexibleTimes: bool                    // "Tijden in overleg / Variabele tijden"
  FlexibleSource: enum?                  // Manual | ImportEmpty | ApiEmpty | AtsEmpty
  Slots: Dictionary<DayCode, List<DayPartCode>>
}
```

**Invarianten:**

- Als `FlexibleTimes == true` → `Slots` mag leeg zijn; matching gebruikt **neutrale weging** voor dagdelen (zie §4).
- Als `FlexibleTimes == false` → minimaal **één** dagdeel geselecteerd (validatiefout anders).
- UI mag niet tegelijk “flexibel” én specifieke dagdelen forceren zonder duidelijke UX: bij handmatig aanklikken van een dagdeel wordt `FlexibleTimes` uitgeschakeld; bij inschakelen van flexibel worden specifieke selecties gewist of genegeerd (zie §2.3).

### 1.3 HoursRange

```text
HoursRange {
  MinHoursPerWeek: decimal   // verplicht, > 0
  MaxHoursPerWeek: decimal   // verplicht, ≥ MinHoursPerWeek
}
```

**Grenzen (product):**

| Veld | Min | Max | Opmerking |
|------|-----|-----|-----------|
| `MinHoursPerWeek` | 1 | 60 | Stap 0,5 toegestaan |
| `MaxHoursPerWeek` | 1 | 60 | ≥ min |

### 1.4 Urencategorie (afgeleid, niet handmatig)

Automatische indeling op basis van het **uurinterval** (gebruik de **midpoint** `(min+max)/2` voor categorisatie in filters; toon altijd het echte min–max-interval op kaarten).

| Categorie | Code | Midpoint-regel (default) |
|-----------|------|--------------------------|
| Bijbaan / oproep | `SideJob` | midpoint &lt; 12 |
| Parttime klein | `PartTimeSmall` | 12 ≤ midpoint &lt; 24 |
| Parttime groot | `PartTimeLarge` | 24 ≤ midpoint &lt; 32 |
| Fulltime | `FullTime` | midpoint ≥ 32 |

> Product mag thresholds later in platform settings zetten; defaults hierboven zijn MVP.

### 1.5 LegalTaskFlags (vacature, verplicht)

```text
LegalTaskFlags {
  WorksAfter19: bool                 // Werk na 19:00?
  NightShift23To06: bool             // Nachtdienst 23:00–06:00?
  AdultSupervisorPresent: bool       // Volwassen toezichthouder altijd aanwezig?
  HandlesMoneyOrClosing: bool        // Geld / kassa / zelfstandig sluiten?
  HeavyOrHazardousWork: bool         // Zwaar tilwerk / gevaarlijke handelingen/machines?
}
```

Alle vijf zijn **verplicht ja/nee** (geen “onbekend” bij publicatie of import).

### 1.6 MatchScoreBreakdown

```text
MatchScoreBreakdown {
  TotalPercent: int                  // 0–100, afgerond
  TravelScore: int                   // 0–100 gewogen deel → bijgedragen punten
  HoursScore: int
  DayPartsScore: int
  Weights: { Travel, Hours, DayParts }
  Details: {
    TravelMinutesEstimated: int?
    TravelWithinPreference: bool?
    HoursOverlapHours: decimal?      // overlapbreedte van de twee intervallen
    HoursCandidateRange: [min,max]
    HoursVacancyRange: [min,max]
    DayPartsMatched: List&lt;Day:Part&gt;
    DayPartsMissing: List&lt;Day:Part&gt;   // gevraagd door vacature, niet in profiel
    DayPartsNeutral: bool            // true bij FlexibleTimes
    LegalEligible: bool              // achtergrondcheck (alleen als leeftijd bekend)
    LegalBlockReasons: List&lt;string&gt;  // interne codes; UI-vriendelijke tekst via i18n
  }
  Advice: List&lt;ActionAdvice&gt;         // “Vink Avond op Vr aan”, etc.
  ViaSafetyNet: bool                 // sollicitatie doorgedrukt &lt; 50%
}
```

### 1.7 Weging (MVP-defaults)

| Component | Gewicht | Toelichting |
|-----------|---------|-------------|
| Reistijd | 40% | t.o.v. kandidaat `MaxTravelMinutes` (of actieve filter) |
| Uren-overlap | 30% | Interval-overlap tussen profiel en vacature |
| Dagdelen | 30% | Jaccard / dekking van gevraagde slots; neutraal bij flexibel |

Bij `FlexibleTimes` op de vacature: dagdelen-component scoort **neutraal 100% van dat gewicht** (geen straf, geen bonus t.o.v. andere kandidaten op dagdelen). Het totale percentage blijft daarmee reistijd + uren + volle dagdelen-bijdrage.

---

## 2. Dagdelen-matrix & “Tijden in overleg”

### 2.1 Doel
Eén herbruikbare interactieve matrix voor:

- **Werkgever** bij vacature aanmaken/bewerken (`/branch/vacancies/new` en latere edit-flow)
- **Kandidaat** in profiel (`/candidate/profile` — bestaande matrix uitbreiden)

### 2.2 UI-gedrag (beide kanten)

**Layout**

- Tabel: rijen = Maandag–Zondag; kolommen = Ochtend / Middag / Avond / Nacht.
- Onder of naast elke kolomkop: subtiele tijdsvenster-tekst (`06:00–12:00`, enz.).
- Cell = aanklikbare checkbox/toggle (bestaande pattern op kandidatenprofiel hergebruiken).

**Flexibele optie**

- Duidelijke toggle/checkbox: **"Tijden in overleg / Variabele tijden"**.
- Wanneer aan:
  - Matrix is disabled (visueel gedimd) of wordt geleegd.
  - Op banenkaart / vacaturetegel: label **"Tijden in overleg"**.
  - Matching: neutrale dagdelen-weging (§1.7).

**Handmatig door werkgever**

- Werkgever kan flexibel aanzetten bij wisselende roosters (retail, uitzend, horeca).

**Automatisch bij import/API**

- Als externe API, ATS of CSV **geen** specifieke dagdelen meestuurt (leeg / ontbrekend):
  - Zet `FlexibleTimes = true`
  - Zet `FlexibleSource = ImportEmpty | ApiEmpty | AtsEmpty`
  - Voorkomt validatiefouten bij koppelingen van grote retailers/uitzendbureaus

### 2.3 Interactieregels

| Actie | Resultaat |
|-------|-----------|
| Gebruiker vinkt ≥1 dagdeel aan terwijl flexibel aan stond | `FlexibleTimes → false`; slot wordt opgeslagen |
| Gebruiker zet flexibel aan | Slots legen (met confirm bij ≥1 selectie) |
| Opslaan zonder slots én zonder flexibel | Validatiefout: “Selecteer dagdelen of kies ‘Tijden in overleg’” |
| Kandidaat: 0 slots én niet flexibel | Profiel onvolledig voor matching-advies; bij solliciteren meenemen in soft/hard checks (§5) |

### 2.4 Weergave op de banenkaart

- Specifieke slots: compacte samenvatting (bijv. “Ma–Vr ochtend/middag”) of icoon + tooltip.
- Flexibel: badge/label **"Tijden in overleg"** (niet “onbekend” / niet leeg laten).

### 2.5 Data mapping (bestaand → nieuw)

Bestaande kandidaten-`Availability` (`Dictionary day → slots[]`) blijft de opslagvorm voor slots. Uitbreiding:

- Kandidaat: optioneel `FlexibleTimes` in preferences (of afgeleide: lege availability + flag).
- Vacature: nieuwe velden `ScheduleJson` / typed columns + `FlexibleTimes` + `FlexibleSource`.

---

## 3. Urenspecificatie

### 3.1 Verplichte velden

Zowel **vacature** als **kandidatenprofiel**:

| Veld | Verplicht | Validatie |
|------|-----------|-----------|
| Minimum uur/week | Ja | 1–60, ≤ max |
| Maximum uur/week | Ja | 1–60, ≥ min |

### 3.2 UI

- Twee numerieke inputs naast elkaar: **Min.** | **Max.**
- Live preview van urencategorie: “Dit valt onder: *Parttime klein*”.
- Helpertekst kandidaat: “Hoeveel uur wil je roughly per week werken?”
- Helpertekst werkgever: “Hoeveel uur per week omvat deze functie?”

### 3.3 Filters (banenkaart / zoeken)

Filters op urencategorie (multi-select):

- Bijbaan / oproep
- Parttime klein
- Parttime groot
- Fulltime

Een vacature matcht een filter als haar **afgeleide categorie** in de selectie zit (of interval overlapt de categorieband — productkeuze MVP: categorie op midpoint).

### 3.4 Matching op uren

Overlap van intervallen `[cMin, cMax]` en `[vMin, vMax]`:

```
overlap = max(0, min(cMax, vMax) - max(cMin, vMin))
unionSpan = max(cMax, vMax) - min(cMin, vMin)   // of max(lengte kandidaat, lengte vacature)
hoursScore01 = overlap == 0 ? 0 : overlap / max(vMax - vMin, 1)
```

MVP-aanbeveling: scoreer dekkingsgraad t.o.v. **vacature-interval** (hoe goed past de kandidaat bij wat de werkgever vraagt), begrensd 0–1, × 30% gewicht.

Geen overlap → uren-component = 0 (trekt totale match omlaag; kan Gulden Middenweg triggeren).

---

## 4. Wettelijke filters & taak-vinkjes (werkgever)

### 4.1 Principe
Bij vacature-aanmaak (UI, CSV, API) **geen** veld “minimumleeftijd”. Wel **vijf verplichte taakvragen** (ja/nee). Elk vinkje heeft een uitklapbaar **`[ i ]`**-icoon met exacte wettelijke toelichting.

### 4.2 Vragen + tooltips (canonieke copy)

#### 4.2.1 Werk na 19:00

- **Label:** `Wordt er gewerkt na 19:00 uur?`
- **Tooltip `[ i ]`:**  
  *“Indien aangevinkt, sluit het systeem sollicitanten van 15 jaar automatisch uit. Personen van 15 jaar mogen wettelijk niet na 19:00 uur werken.”*
- **Achtergrondregel:** als `WorksAfter19 == true` → blokkeer leeftijd **15** (bij bekende geboortedatum).

#### 4.2.2 Nachtdienst

- **Label:** `Wordt er gewerkt tussen 23:00 en 06:00 uur (Nachtdienst)?`
- **Tooltip `[ i ]`:**  
  *“Indien aangevinkt, sluit het systeem alle personen van 15, 16 en 17 jaar automatisch uit. Nachtdienst is wettelijk verboden voor iedereen onder de 18 jaar.”*
- **Achtergrondregel:** als `NightShift23To06 == true` → blokkeer leeftijd **15, 16, 17**.

#### 4.2.3 Toezichthouder

- **Label:** `Is er te allen tijde een volwassen toezichthouder/begeleider aanwezig?`
- **Tooltip `[ i ]`:**  
  *“Indien uitgeschakeld, sluit het systeem sollicitanten van 15 jaar automatisch uit. Zij mogen wettelijk nooit solowerk verrichten.”*
- **Achtergrondregel:** als `AdultSupervisorPresent == false` → blokkeer leeftijd **15**.

#### 4.2.4 Geld / kassa / sluiten

- **Label:** `Wordt er gewerkt met geld, kassasystemen of zelfstandig sluiten?`
- **Tooltip `[ i ]`:**  
  *“Indien van toepassing, kan dit op basis van wettelijk toezicht specifiek de jongste categorie van 15 jaar uitsluiten voor deze handelingen.”*
- **Achtergrondregel:** als `HandlesMoneyOrClosing == true` → blokkeer leeftijd **15**.

#### 4.2.5 Zwaar / gevaarlijk werk

- **Label:** `Omvat de taak zwaar tilwerk of gevaarlijke handelingen/machines?`
- **Tooltip `[ i ]`:**  
  *“Indien aangevinkt, filtert het systeem automatisch alle personen van 15, 16 en 17 jaar die volgens de strenge arboregels dit specifieke zware of gevaarlijke werk niet mogen verrichten.”*
- **Achtergrondregel:** als `HeavyOrHazardousWork == true` → blokkeer leeftijd **15, 16, 17**.

### 4.3 Afgeleide uitsluitingsleeftijden (intern, niet in UI)

| Leeftijd | Uitgesloten wanneer |
|----------|---------------------|
| 15 | `WorksAfter19` ∨ `NightShift23To06` ∨ ¬`AdultSupervisorPresent` ∨ `HandlesMoneyOrClosing` ∨ `HeavyOrHazardousWork` |
| 16 | `NightShift23To06` ∨ `HeavyOrHazardousWork` |
| 17 | `NightShift23To06` ∨ `HeavyOrHazardousWork` |
| ≥ 18 | Nooit via deze regels |

### 4.4 UI-regels werkgever

- Alle vijf moeten beantwoord zijn vóór **Publiceren** (Draft mag tijdelijk incompleet; publish-gate verplicht).
- `[ i ]` opent inline accordion of popover (niet alleen native `title`-tooltip — copy moet volledig leesbaar zijn).
- Geen zichtbare “min. leeftijd: 18”-badge op de publieke vacature. Eventueel intern audit-log van afgeleide regels.

### 4.5 Koppeling met dagdelen (soft consistentie)

- Als vacature **Nacht**-dagdeel heeft of `FlexibleTimes` met nachtelijke verwachting: adviseer (niet forceer) `NightShift23To06 = true`.
- Als **Avond** geselecteerd: adviseer check van `WorksAfter19` (avond loopt tot 23:00; werk na 19:00 is waarschijnlijk).  
  Productkeuze MVP: **waarschuwing**, geen harde blokkade (werkgever kan avonddienst vóór 19:00 bedoelen — zeldzaam maar mogelijk bij korte diensten).

---

## 5. Matchingspercentage & `[ i ]` op de banenkaart (kandidaat)

### 5.1 Wanneer tonen

- Ingelogde kandidaat met voldoende profielgegevens (locatie en/of uren+dagdelen) ziet per vacaturetegel een **Matchingspercentage**.
- Anoniem / leeftijd onbekend: percentage mag gebaseerd op beschikbare signalen (reistijd-filter + openbare vacature-uren/dagdelen); ontbrekende profieluren → uren-component neutraal of verborgen met hint “Maak profiel compleet voor een betere matchscore”.
- **Browse-vrijheid:** kandidaten zonder bekende leeftijd mogen **alle** vacatures bekijken; wettelijke uitsluiting grijpt pas bij solliciteren (of bij bekende DOB in achtergrondfilter van de lijst — zie §5.5).

### 5.2 Weergave op tegel

```
[ 72% ] [ i ]
```

- Percentage groot/leesbaar naast of op de vacaturekaart.
- Direct ernaast: uitklapbaar **`[ i ]`**.

### 5.3 Modal bij klik op `[ i ]`

**Titel:** “Waarom deze matchscore?”

**Inhoud:**

1. Totaalpercentage (groot)
2. Breakdown:
   - Reistijd: geschatte minuten vs. voorkeur; deelscore
   - Uren: “Jij: 8–16 u/w · Vacature: 12–20 u/w · Overlap: …”
   - Dagdelen: matched vs. ontbrekend (lijst), of “Tijden in overleg — neutrale weging”
3. Eventuele wettelijke status (alleen als DOB bekend): “Je voldoet aan de wettelijke taakeisen” / “Deze taken zijn wettelijk niet toegestaan op jouw leeftijd” (zonder discriminerende vacaturetekst)
4. **Actie-adviezen** (concreet):
   - “Zet Vr Avond aan in je beschikbaarheid”
   - “Verhoog je max. uren naar minstens 12”
   - “Deel je locatie voor een betere reistijdscore”
5. Primaire knop: **"Direct profiel aanpassen"** → `/candidate/profile` (idealiter deep-link naar uren/dagdelen-sectie)
6. Secundair: sluiten

### 5.4 Kleurcodering (kandidaat-tegel, optioneel consistent met werkgever)

| Score | Kleur | Betekenis |
|-------|-------|-----------|
| ≥ 70% | Groen | Sterke match |
| 50–69% | Oranje | Gedeeltelijke wrijving |
| &lt; 50% | Rood / aandacht | Zwakke match (Gulden Middenweg bij solliciteren) |

### 5.5 Wettelijke achtergrondfilter in zoekresultaten

- **Leeftijd onbekend:** toon vacature; geen leeftijdsblokkade in browse.
- **Leeftijd bekend + niet eligible:** vacature **verbergen of markeren als niet solliciteerbaar** (productkeuze MVP: verbergen in default lijst + uitleg in filters “Sommige vacatures zijn wettelijk niet beschikbaar op jouw leeftijd” — vermijd expliciete “18+”-labels op tegels).

---

## 6. Sollicitatie-validatie, Gulden Middenweg & motivatie (kandidaat)

### 6.1 Browse-vrijheid
Kandidaten zonder bekende leeftijd (`DateOfBirth` leeg) kunnen alle vacatures **bekijken**. Solliciteren triggert aanvulling/validatie.

### 6.2 Sollicitatieformulier — optioneel motivatieveld

Naast bestaande consents (`WorkPermitConfirmed`, voorwaarden):

- **Veld:** vrij tekstvak, optioneel
- **Label / prikkel (canonieke copy):**  
  *“Waarom wil jij hier graag aan de slag? (Optioneel – een kort berichtje laat een goede indruk achter en vergroot je kansen!)”*
- **Limiet:** bijv. 500 tekens
- **Opslag:** `Application.Motivation` (nieuw; los van `SnapshotAboutMe`)

### 6.3 Validatie bij “Solliciteren” (vóór verificatiemail)

Volgorde:

1. Bestaande harde eisen (rijbewijs, opleiding, minimum werkgevers) — blijven blokkeren.
2. Profielcompleetheid: uren min/max + dagdelen of flexibel; eventueel inline aanvullen.
3. **Arbeidstijdenwet-check** (als DOB bekend): bij niet-eligible → **harde blokkade** met vriendelijke uitleg (“Op basis van de wettelijke regels voor deze werkzaamheden kun je op deze leeftijd niet op deze vacature solliciteren.”). Geen werkgeverszichtbare discriminerende redencode naar buiten.
4. Als DOB ontbreekt: verplicht geboortedatum invullen vóór verzenden (nodig voor wettelijke check én loonzichtbaarheid — bestaand patroon versterken).
5. Bereken **MatchScore**.
6. Beslispad:

#### A. Sterke / voldoende match (score ≥ 50%)
- Groen licht.
- Maak draft application + verstuur **verificatiemail (OTP)** zoals nu.

#### B. Gulden Middenweg (score &lt; 50%)
- **Verificatiemail tijdelijk tegenhouden.**
- Pop-up met:
  - Actuele matchingsscore
  - Korte onderbouwing (uren/dagdelen/reistijd)
  - **Optie 1 (primair, sterk geadviseerd):** “Ja, pas mijn profiel aan” → profiel, daarna terug naar vacature
  - **Optie 2 (secundair):** “Toch doorgaan met solliciteren” (vangnet)
- Bij optie 2:
  - Zet `ViaSafetyNet = true` op de sollicitatie
  - Ga verder met OTP-verificatie
  - Werkgever ziet Rood/Aandacht-codering (§7)

#### C. Wettelijk niet eligible
- Geen vangnet. Sollicitatie wordt niet aangemaakt.

### 6.4 Relatie tot bestaande soft gates
Bestaande eisen (availability/AboutMe/werkervaring vóór solliciteren) blijven waar zinvol; uren min/max worden onderdeel van de profielgate. Motivatie is **niet** verplicht.

---

## 7. Matchingspercentage in het werkgeversdashboard

### 7.1 Locatie
`/branch/applicants` (en eventuele drilldowns per vacature).

### 7.2 Kaart / rij per sollicitatie

**Prominent:**

- Matchingspercentage **groot**
- Kleurcodering:
  - **Groen** = hoge match (≥ 70%)
  - **Oranje** = gedeeltelijke wrijving (50–69%)
  - **Rood / Aandacht** = via vangnet (`ViaSafetyNet`) of score &lt; 50%

### 7.3 Breakdown & context (per kandidaat)

Werkgever ziet (privacyregels progressive disclosure respecteren — reis/uren/dagdelen-signals mogen pre-accept; PII volgt bestaande Accept-flow):

| Blok | Inhoud |
|------|--------|
| Reistijd | Geschatte minuten + vervoer |
| Uren | Kandidaat min–max vs. vacature min–max + overlap |
| Dagdelen | Overlapmatrix of “Tijden in overleg” |
| Wettelijk | Bevestiging: “Kandidaat voldoet aan de wettelijke taakeisen voor deze vacature” (boolean; geen geboortedatum tonen) |
| Motivatie | Optioneel bericht **prominent** indien ingevuld |

### 7.4 Slim sorteren

- Default sort: **hoogste → laagste** matchpercentage.
- Toggle/opties: nieuwste eerst, hoogste match, vangnet eerst (aandacht).
- Filterchips: Alle / Sterke match / Wrijving / Vangnet.

### 7.5 Snapshot bij sollicitatie

Bij apply bevriezen (naast bestaande snapshots):

- `SnapshotHoursMin`, `SnapshotHoursMax`
- `SnapshotScheduleJson` / availability
- `MatchScoreTotal`, `MatchScoreBreakdownJson`
- `ViaSafetyNet`
- `Motivation`
- `LegalEligibleAtApply` (bool)

Zo blijft de werkgeversweergave stabiel als het profiel later wijzigt.

---

## 8. Systeemvereisten: bestanden, CSV, API, ATS

### 8.1 Verplicht vs. optioneel

| Veldgroep | CSV / API / ATS | Gedrag bij ontbreken |
|-----------|-----------------|----------------------|
| `MinHoursPerWeek`, `MaxHoursPerWeek` | **Verplicht** | Reject rij / 400 Bad Request |
| Dagdelen / schedule | **Optioneel** | Auto: `FlexibleTimes=true`, label “Tijden in overleg”, `FlexibleSource=ImportEmpty/ApiEmpty` |
| Vijf wettelijke taakvinkjes | **Verplicht** (ja/nee) | Reject; geen stille defaults die jongeren onterecht toelaten of uitsluiten |

### 8.2 CSV — nieuwe kolommen (canonieke NL-headers)

| Header | Verplicht | Waarden |
|--------|-----------|---------|
| `uren_min` | Ja | getal |
| `uren_max` | Ja | getal ≥ uren_min |
| `tijden_in_overleg` | Nee | `ja`/`nee` (default: ja als geen dagdelen) |
| `dagdelen` | Nee | compact formaat, zie §8.3 |
| `werk_na_19` | Ja | `ja`/`nee` |
| `nachtdienst` | Ja | `ja`/`nee` |
| `toezichthouder_aanwezig` | Ja | `ja`/`nee` |
| `geld_kassa_sluiten` | Ja | `ja`/`nee` |
| `zwaar_of_gevaarlijk` | Ja | `ja`/`nee` |

Aliases (EN) ondersteunen, analoog aan `VacancyCsvSchema`.

### 8.3 Dagdelen-formaat CSV (voorstel)

Compact, pipe-gescheiden:

```text
Ma:Ochtend+Middag|Di:Middag|Za:Avond+Nacht
```

Leeg + geen `tijden_in_overleg=nee` → flexibel.

### 8.4 External API (`POST/PUT api/external/vacancies`)

Breid `CreateVacancyRequest` uit met:

```json
{
  "minHoursPerWeek": 12,
  "maxHoursPerWeek": 24,
  "flexibleTimes": false,
  "schedule": { "Ma": ["Ochtend", "Middag"], "Vr": ["Avond"] },
  "legalTasks": {
    "worksAfter19": true,
    "nightShift23To06": false,
    "adultSupervisorPresent": true,
    "handlesMoneyOrClosing": true,
    "heavyOrHazardousWork": false
  }
}
```

Validatie:

- Uren verplicht.
- `legalTasks` alle keys verplicht boolean.
- Ontbrekende/lege `schedule` én `flexibleTimes` niet false → forceer `flexibleTimes=true`.

### 8.5 Publicatiegate (alle kanalen)

Geen `Active`/`PendingApproval`-publicatie zonder:

- geldige uren
- schedule of flexibel
- volledige legal flags

---

## 9. Functionele flows per rol

### 9.1 Kandidaat

| Stap | Scherm | Gedrag |
|------|--------|--------|
| Profiel vullen | `/candidate/profile` | Matrix + uren min/max (+ optioneel flexibel) |
| Zoeken | `/` banenkaart | Match-% + `[ i ]`; filters urencategorie; label “Tijden in overleg” |
| Score begrijpen | Modal | Breakdown + “Direct profiel aanpassen” |
| Solliciteren | Vacancy detail | Motivatie optioneel; validatie uren/dagdelen/wet; ≥50% → OTP; &lt;50% → Gulden Middenweg |
| Historie | `/candidate/applications` | Toon eigen match-% en of via vangnet |

### 9.2 Filiaalmanager / werkgever

| Stap | Scherm | Gedrag |
|------|--------|--------|
| Vacature maken | `/branch/vacancies/new` | Matrix of “Tijden in overleg”; uren; 5× taakvinkjes met `[ i ]` |
| CSV/API | Import / external API | §8 |
| Sollicitanten | `/branch/applicants` | Groot match-%; kleur; breakdown; motivatie; sort hoog→laag |
| Beoordelen | Accept/Reject | Bestaande PII-regels; match-context helpt twijfelgevallen |

### 9.3 Intermediair / enterprise / admin

- Zelfde vacaturevelden bij batch/CSV namens opdrachtgevers.
- Admin: optioneel platform settings voor urencategorie-drempels en match-gewichten (niet MVP-blokkerend).

---

## 10. Matchingalgoritme (normatief MVP)

### 10.1 Reistijdscore (0–1)

Laat `t` = geschatte reistijd in minuten, `T` = kandidaat max (of actieve filter).

```
travel01 = t == null ? 0.5 : clamp(1 - (t / T), 0, 1)   // t > T → 0
```

### 10.2 Urenscore (0–1)

Zie §3.4 — dekking t.o.v. vacature-interval.

### 10.3 Dagdelenscore (0–1)

Als vacature `FlexibleTimes`: `day01 = 1.0`.

Anders, laat `V` = set gevraagde (dag, part), `C` = set kandidaat:

```
day01 = |V| == 0 ? 1.0 : |V ∩ C| / |V|
```

(Ontbrekende gevraagde delen verlagen de score; extra kandidaat-delen straffen niet.)

### 10.4 Totaal

```
total = round(100 * (0.40*travel01 + 0.30*hours01 + 0.30*day01))
```

### 10.5 Legal eligibility (aparte as)

Niet in het percentage verwerken als “soft score”, maar als **harde gate** bij apply en optioneel als filter in discovery bij bekende leeftijd. Reden: wet ≠ voorkeur.

---

## 11. Fouten, edge cases & copy

| Situatie | Gedrag |
|----------|--------|
| Import zonder uren | Rij geweigerd met duidelijke fout |
| Import zonder dagdelen | Flexibel + label “Tijden in overleg” |
| Import zonder legal flags | Rij geweigerd |
| min &gt; max | Validatiefout |
| Score precies 50% | Geen Gulden Middenweg (drempel: &lt; 50) |
| Dubbele sollicitatie | Bestaande regels |
| Leeftijd wijzigt later | Nieuwe sollicitaties herberekend; oude snapshots blijven |
| Werkgever zet later nachtdienst aan | Bestaande sollicitaties behouden; nieuwe applies hertoetsen |

---

## 12. Acceptatiecriteria (samenvatting)

1. **Matrix** werkt op vacature én profiel; vaste tijdsblokken zichtbaar; flexibel handmatig + auto bij lege import.
2. **Uren** min/max verplicht; categorie automatisch; overlap in matchscore.
3. **Vijf taakvinkjes** met exacte `[ i ]`-copy; geen UI-minimumleeftijd; achtergrondfiltering volgens §4.3.
4. **Match-%** op banenkaart met `[ i ]`-modal, breakdown, adviezen, “Direct profiel aanpassen”.
5. **Solliciteren:** optionele motivatie; validatie vóór OTP; Gulden Middenweg &lt; 50%; wettelijke hard block.
6. **Werkgeversdashboard:** groot %, kleur, breakdown, wettelijke bevestiging, motivatie, sort hoog→laag.
7. **CSV/API:** uren + legal verplicht; dagdelen optioneel → “Tijden in overleg”.

---

## 13. Implementatie-aanknopingspunten (codebase)

| Onderdeel | Primair aanknopingspunt |
|-----------|-------------------------|
| Kandidaat matrix | `Jobsy.Web/Components/Pages/Candidate/Profile.razor` |
| Vacature create | `Jobsy.Web/Components/Pages/Branch/CreateVacancy.razor` |
| Banenkaart tegels | `VacancyDiscovery.razor`, `VacancyListItem` |
| Apply + OTP | `VacancyDetail.razor`, `ApplicationsController` |
| Applicants UI | `Branch/Applicants.razor` |
| CSV | `VacancyCsvSchema.cs`, `VacancyCsvParser.cs`, `CsvImport.razor` |
| External API | `ExternalVacanciesController`, `CreateVacancyRequest` |
| Domain | `Vacancy`, `User` prefs, `Application` uitbreiden |
| Nieuwe core service | bijv. `IMatchScoreService` + `LegalTaskEligibilityRules` in `Jobsy.Core` |

---

## 14. Open productkeuzes (niet blokkerend voor deze spec)

1. Vacatures wettelijk niet-eligible bij bekende leeftijd: **verbergen** vs. tonen-als-niet-solliciteerbaar.
2. Exacte urencategorie-drempels configureerbaar in admin settings.
3. Match-gewichten (40/30/30) configureerbaar.
4. Of “Avond”-selectie `WorksAfter19` hard forceert of alleen waarschuwt (MVP: waarschuwen).

---

*Einde functionele specificatie v1.0*
