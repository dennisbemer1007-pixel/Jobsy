# Functionele specificaties: Lobsy Platform (Master)

**Product:** Lobsy (codebase: Jobsy)  
**Scope:** Dubbele test-engine, anonieme werkgever-zoekmachine, token-contactunlock, flex-marge & uitzend-abonnement  
**Status:** Functionele specificatie + implementatie  
**Versie:** 1.0  
**Gerelateerd:** `REQUIREMENTS.md`, `SECURITY.md` §B (AVG), `docs/FUNCTIONELE_SPECIFICATIES_MATCHING.md`

---

## 0. Uitgangspunten

1. Tests zitten in onboarding/profiel/dashboard — geen losse “test-tabbladen” als primaire UX.
2. Uitkomsten (scores + tags) voeden de C# matchingsengine en de anonieme talentpool.
3. Werkgevers zien kandidaten eerst **anoniem**; PII pas na geslaagde contactuitwisseling.
4. Geen hard leeftijdsfilter in de werkgevers-zoek-UI (gelijke behandeling); matching op competenties, reistijd, beschikbaarheid, rijbewijs.
5. Token-unlock is no-risk: 48-uurs reactievenster met refund-optie bij geen reactie / reeds voorzien.
6. Flex-vacatures: 0 tokens publiceren; marge € 2,00/uur boven backoffice-inkoop. Uitzendbureaus: € 4.000/jaar carte blanche op vestigingspins.

---

## 1. Gescheiden test-architectuur

Tests zitten in profiel/dashboard (geen losse menu-tabs). Elke engine heeft een **gratis Quick-Scan (25)** en een **betaalde diepte-analyse (150, € 2,99)**.

### 1.1 Competentietest — wie ben jij en wat kun je?

| Eigenschap | Quick-Scan | Diepte-analyse |
|------------|------------|----------------|
| Items | 25 Likert (Big Five / OCEAN) | **150 unieke** Likert (30 per OCEAN-trek: Openheid, Consciëntieusheid, Extraversie, Vriendelijkheid, Emotionele stabiliteit; mix reverse-items; geen “variant N”) |
| Output | Competentiescores 0–100% + match-tags | Verrijkte tags + PDF |
| Opslag | `CandidateCompetencies` | `CandidateDeepAnalyses` (`Kind=Competence`) |
| Upsell | — | *“Ontgrendel je uitgebreide competentie-analyse inclusief officiële PDF-rapportage voor € {prijs}.”* |

### 1.2 Beroepentest — wat wil je en welke baan past?

| Eigenschap | Quick-Scan | Diepte-analyse |
|------------|------------|----------------|
| Items | 25 Likert (RIASEC / Holland-code) | **150 unieke** Likert (25 per RIASEC-type; mix reverse-items) + carrière-advies in PDF |
| Output | Percentages per type, **Mijn Beroepen-kompas**, **top 10 actieve vacatures** | Loopbaan-PDF (Jip-en-Janneke, gekleurd Lobsy-logo) + algemene NL-beroepen (OpenAI) + zwaardere matchingweging |
| Opslag | `CandidateCareerInterests` | `CandidateDeepAnalyses` (`Kind=Career`) |
| Upsell | na gratis test | *“Wil je een diepgaand carrière-advies … Ontgrendel de uitgebreide beroepentest voor € {prijs}.”* |

**RIASEC-tags (canoniek, intern):** `Realistic`, `Investigative`, `Artistic`, `Social`, `Enterprising`, `Conventional`. Kandidaat-UI en PDF gebruiken geen vaktermen: *Aanpakken met je handen*, *Uitzoeken hoe het zit*, *Iets moois of nieuws maken*, *Mensen helpen*, *Aanjagen en verkopen*, *Netjes organiseren*.

**Loopbaan-PDF (na 150 vragen):** OpenAI-prompt analyseert de 150 antwoorden en levert algemene NL-beroepen in een strikte hiërarchie: **Super-match (95–100%, kernfit, nooit te laag)**, **Sterke keus (85–94%)**, **Handige verbreding (75–84%)**; Jip-en-Janneke (geen RIASEC/OCEAN/extraversie/neuroticisme); sectie **Wat betekent dit voor jou?** (werkplek, taken, banenkaart). JSON vult het PDF-template (gekleurd Lobsy-logo) en `CompassJson` (**Mijn Beroepen-kompas**). Zonder key: lokale catalogus.

**Profiel:** afronden schrijft scores/tags én `CompassJson` naar `CandidateCareerInterests`; Kompas-categorie **Mijn Beroepen-kompas**. Uitgebreide beroepentest verhoogt de interesse-weging op de banenkaart (quick-scan 0,25 / diepte 0,32, of 0,30 / 0,38 zonder competentiescores). Algemene beroepstags + zoeksleutels weegt 65% van de interesse-score (RIASEC 35%) om actuele vacatures (bijv. Den Haag / Westland) te herkennen.

**Checkout-flow:** kandidaat → Mollie/stub iDEAL → webhook/return → unlock → antwoorden verwerken → PDF.

### 1.3 Privacy

- Scores/tags zijn matchingmetadata, geen medische diagnose.
- Werkgever ziet in anonieme pool alleen geaggregeerde scores/tags, geen ruwe antwoorden.
- Ruwe antwoorden blijven bij de kandidaat; export/vergetelheid volgt AVG-paden (inclusief `CompassJson`).
- OpenAI ontvangt geen NAW: alleen Likert + richtingscores van de 150-vragen beroepentest.

---

## 2. Werkgever-zoekmachine (omgekeerd werven)

### 2.1 Anonieme resultaatkaart

Toont **niet:** naam, e-mail, telefoon, exact adres, geboortedatum.  
Toont **wel:** match-tags, competentiebanden, RIASEC, beschikbaarheidsamenvatting, rijbewijzen, geschatte reistijd vanaf vestiging, globale regio (stad/wijk-niveau).

### 2.2 Filters

- Competenties / beroepsinteresses (testtags)
- Maximale reistijd (+ vervoersmiddel)
- Beschikbaarheid (per direct / parttime / fulltime / seizoen; dagdelen uit profiel)
- Rijbewijs (ja/nee/type)
- **Geen** leeftijdsfilter in UI of API-query

### 2.3 Authz

Alleen employer-rollen met company-scope (`BranchManager`, `RegionalManager`, `EnterpriseManager`, `Intermediary`). Resultaten beperkt tot OpenForWork + actieve kandidaten met (minimaal) één voltooide test (competentie of beroep).

---

## 3. Token-flow & 48-uurs garantie

| Stap | Gedrag |
|------|--------|
| 1. Ontgrendeling | Werkgever kiest anoniem profiel → **1 token** (`TokenSpendReason.ContactUnlock`) |
| 2. Bericht | Beveiligd bericht/uitnodiging; kandidaat krijgt notificatie |
| 3a. Reactie ≤ 48u | Contactgegevens gedeeld (`ContactShared`); token blijft besteed |
| 3b. Geen reactie / reeds voorzien | Werkgever mag intrekken → **automatische token-refund** (`Grant` + note `ContactUnlockRefund`) |
| 4. Geen klik na contact | Geen refund — matchmaking is afgerond |

**Timer:** `RespondByUtc = CreatedAtUtc + 48h`. Hosted job markeert verlopen requests als `RefundEligible`; intrekken na deadline triggert refund. Dubbele refund is idempotent.

---

## 4. Monetization — Lobsy Flex & uitzend

### 4.1 Flex-inzet

- Vacaturekind `Flex`: publiceren kost **0 tokens**.
- Platformmarge: configureerbaar in Admin → Settings (default **€ 2,00 per gewerkt uur**) boven inkoopprijs NEN 4400-1 backoffice-partner (`FlexCommercialSettings`).
- Verloning/juridisch risico via partner (Yellowstone e.d.) — Lobsy factureert alleen de marge-opslag in de commerciele afspraak (backoffice-integratie kan stubben).

### 4.2 Uitzendbureau-abonnement

- `AgencyAnnualSubscription`: jaartarief admin-configureerbaar (default **€ 4.000 / jaar**).
- Rechten: onbeperkt vacature plaatsen gekoppeld aan vestigingslocatie-pins (geen PushBom-spam / “pushbombs”).
- Actief abonnement → publish-kosten 0 voor Regular op geabonneerde vestigingen; PushBom blijft token-geprijsd of uitgeschakeld per settings.

### 4.3 Admin-configureerbare bedragen

Alle bedragen onder **Admin → Settings → Lobsy Flex & talent**:

| Veld | Default |
|------|---------|
| Diepte-analyse | € 2,99 |
| Flex-marge / uur | € 2,00 |
| Uitzend-jaarabonnement | € 4.000 |
| ContactUnlock | 1 token (sync naar spend-costs) |
| Backoffice-partnernaam | Yellowstone |

---

## 5. Acceptatiecriteria (kern)

1. Competentie-Quick-Scan = 25 Big Five-vragen; afronden schrijft scores + match-tags.
2. Beroepen-Quick-Scan = 25 vragen; afronden schrijft scores + **Mijn Beroepen-kompas** + top 10 actieve vacatures.
3. Elke diepte-analyse is locked tot betaald; na unlock 150 vragen + PDF (beroep: match-banden + praktische vertaling, geen RIASEC/OCEAN in de kandidaattekst). Prijs admin-configureerbaar (default € 2,99).
4. Talentpool-API lekt geen PII vóór `ContactShared`.
5. Talentpool-API accepteert geen `minAge`/`maxAge`/`dateOfBirth`-filters.
6. ContactUnlock debiteert configureerbaar aantal tokens (default 1); refund na intrekken bij timeout/declined-unavailable.
7. Flex-publicatie kost 0 tokens; settings tonen configureerbare marge (default € 2,00).
8. Actief uitzend-jaarabonnement: carte blanche publish; jaartarief admin-configureerbaar (default € 4.000).
9. `/carriere` toont nooit een match-percentage: alleen een fit-band in woorden en stappen die de kandidaat zelf kan afronden.
10. Een stap afronden kan alleen op de stap die nu aan de beurt is; een eerdere stap moet eerst ongedaan worden gemaakt.
11. Contactgegevens van de kandidaat gaan pas naar de werkgever na een expliciete **Ja** op de deelvraag; de preview toont precies welke velden worden gedeeld.
12. Een kandidaat kan een cursus niet zelf afvinken; bewijs loopt via het paspoort.

---

## 6. Carrièreplan & contactverzoeken (kandidaat)

### 6.1 Carrièreplan (`/carriere`)

- Zonder plan is er één kaart: waar wil jij naartoe groeien? Suggesties komen uit echte beroepen, plus zoeken. Eén generatie per klik (de knop gaat direct op disabled).
- Het plan is een reeks stappen van "nu" naar de droombaan, getoond als groeiende schelpen. Per stap: wat je al hebt, wat je nog mist (in klauwen, geen percentage), een fit-band in woorden en echte cursussen (gratis eerst, maximaal één partnerlink).
- Stapdetail is deep-linkbaar via `?stap={n}`; alleen de stap die nu aan de beurt is heeft een afrondknop. Afronden is een rustig groeimoment (oude schaal valt, nieuwe gouden schaal), met "Toch nog niet klaar" ernaast.
- Bewijs voor cursussen en ervaring loopt via het paspoort; de kandidaat vinkt niets zelf af. De oude self-claim-endpoint antwoordt `410 use_passport_proof`.
- Droombaan wijzigen bewaart wat je al haalde ("Die tellen mee"). Het oude plan gaat **30 dagen** naar *Eerdere plannen* en kan vanuit de UI worden teruggezet.
- Werkgevers-feature UIT verbergt elke vacaturelink, -telling en -regel; het plan blijft werken.

### 6.2 Contactverzoeken (`/candidate/talent-contacts`)

- Statussen staan in woorden ("Wacht op jou", "Je zei ja", "Je zei nee", "Gestopt"), nooit als enum.
- Delen vraagt een expliciete **Ja**: de deelvraag toont eerst een preview met exact de naam / e-mail / telefoon die de werkgever krijgt (ontbrekend telefoonnummer leest "niet ingevuld"). Zonder bevestiging weigert de API met `400 confirm_share_required`.
- Afwijzen legt de reden vast (geen interesse / al voorzien) en deelt niets.
- Datums zijn Amsterdamse kloktijd in de taal van de gebruiker; API-fouten verschijnen als code-gestuurde tekst, nooit als ruwe foutmelding.

### 6.3 Hoe werkt Lobsy (kandidaat)

- Vijf stenen in volgorde: ontdekkingsreis → paspoort → carrière → banenkaart → sollicitaties, met per steen een status en een link. De steen waar je nu staat heeft `aria-current="step"`.
- De pagina is Candidate-only; andere rollen hebben hun eigen guide op `/hoe-werkt-lobsy`.
- Werkgevers UIT laat banenkaart en sollicitaties weg; een onbekende route laat de steen weg in plaats van naar een 404 te linken.
