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

## 1. Beroeps- en competentietests

### 1.1 Gratis Quick-Scan (25 vragen)

| Eigenschap | Waarde |
|------------|--------|
| Duur | ca. 3 minuten |
| Items | 25 Likert (1–5) |
| Basis | Big Five / OCEAN (20) + RIASEC-interesses (5) |
| Output | Competentiescores 0–100% + RIASEC-tags + match-tags |
| Opslag | `CandidateCompetencies` gekoppeld aan user |
| Effect | Kandidaat zichtbaar in talentpool (als OpenForWork) / regionale kaart |

**RIASEC-tags (canoniek):** `Realistic`, `Investigative`, `Artistic`, `Social`, `Enterprising`, `Conventional`  
Quick-Scan meet de top-interesses via 5 compacte items (hoogste scores → tags).

### 1.2 Uitgebreide Diepte-Analyse (150 vragen — € 2,99)

| Eigenschap | Waarde |
|------------|--------|
| Items | 150 Likert |
| Basis | Big Five-facetten, RIASEC, praktische belastbaarheid/vaardigheden |
| Prijs | € 2,99 (iDEAL via Mollie; Dev-stub zonder API-key) |
| Upsell-copy | *“Wil je een diepgaand inzicht in jouw unieke werkstijl en een officiële PDF-rapportage voor je sollicitaties? Ontgrendel de uitgebreide diepte-analyse voor € 2,99.”* |
| Na betaling | Tags verrijken match-index; PDF-rapport in dashboard |

**Checkout-flow:** kandidaat → Mollie → webhook/return → `DeepAnalysisUnlocked` → antwoorden verwerken → PDF genereren.

### 1.3 Privacy

- Scores/tags zijn matchingmetadata, geen medische diagnose.
- Werkgever ziet in anonieme pool alleen geaggregeerde scores/tags, geen ruwe antwoorden.
- Ruwe antwoorden blijven bij de kandidaat; export/vergetelheid volgt AVG-paden.

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

Alleen employer-rollen met company-scope (`BranchManager`, `RegionalManager`, `EnterpriseManager`, `Intermediary`). Resultaten beperkt tot OpenForWork + actieve kandidaten met (minimaal) voltooide Quick-Scan.

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
- Platformmarge: vast **€ 2,00 per gewerkt uur** boven inkoopprijs NEN 4400-1 backoffice-partner (config: `FlexCommercialSettings.MarginPerHourEuro`).
- Verloning/juridisch risico via partner (Yellowstone e.d.) — Lobsy factureert alleen de marge-opslag in de commerciele afspraak (backoffice-integratie kan stubben).

### 4.2 Uitzendbureau-abonnement

- `AgencyAnnualSubscription`: **€ 4.000 / jaar**.
- Rechten: onbeperkt vacature plaatsen gekoppeld aan vestigingslocatie-pins (geen PushBom-spam / “pushbombs”).
- Actief abonnement → publish-kosten 0 voor Regular op geabonneerde vestigingen; PushBom blijft token-geprijsd of uitgeschakeld per settings.

---

## 5. Acceptatiecriteria (kern)

1. Quick-Scan = 25 vragen; afronden schrijft scores + RIASEC-tags.
2. Diepte-Analyse locked tot betaald; na unlock 150 vragen + PDF-flag.
3. Talentpool-API lekt geen PII vóór `ContactShared`.
4. Talentpool-API accepteert geen `minAge`/`maxAge`/`dateOfBirth`-filters.
5. ContactUnlock debiteert 1 token; refund na intrekken bij timeout/declined-unavailable.
6. Flex-publicatie kost 0 tokens; settings tonen € 2,00 marge.
7. Actief uitzend-jaarabonnement: carte blanche publish op vestigingspins.
