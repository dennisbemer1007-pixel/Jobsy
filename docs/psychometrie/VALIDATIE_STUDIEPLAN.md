# Validatiestudieplan — Lobsy kandidaatstests

**Doel:** Empirische onderbouwing voor interne kwaliteitsborging en eventuele samenwerking met universiteit / COTAN-beoordelaar.  
**Geen onderdeel van een officiële COTAN-aanvraag** totdat een bevoegde commissie het instrument beoordeelt.

Referentiekader: COTAN Beoordelingssysteem (Evers, Lucassen, Meijer & Sijtsma, 2010) + Fairness-addendum (14-07-2021) + unproctored-addendum (01-12-2018), zie [cotandocumentatie.nl](https://www.cotandocumentatie.nl/cotan/beoordelingssysteem/).

---

## Fase 0 — Voorbereiding (documentatie, geen respondenten)

| Activiteit | Output | Bewijs-type |
|------------|--------|-------------|
| Constructdocument per test (doel, doelgroep, meetpretentie, verboden uses) | PDF handleiding concept | (a) code + doc |
| Itemregister koppelen aan `inventory.json` | Versiebeheer item-id | (a) |
| Ethiek / DPIA: geanonimiseerde export | Getekend protocol | organisatorisch |

---

## Fase 1 — Descriptief & betrouwbaarheid (Quick-Scan)

**Populatie:** NL werkzoekenden via Lobsy (acceptatie → productie-kopie), **alleen** `Status=Completed`, **geen** productie-PII in analyse-CSV.

| Analyse | Min. N (conservatief) | Methode | Script / SQL |
|---------|----------------------|---------|--------------|
| Item-frequenties, missing | 100 per test | descriptief | `analyze_responses.py` |
| Cronbach α + McDonald ω per schaal | **300** per test | 2 methoden (variance + avg inter-item r) | `analyze_responses.py` |
| Item-rest correlatie; flag < .30 | 300 | corr | idem |
| Test–retest (2–4 weken) | **100** (zelfde personen) | ICC(2,1) of r | apart script (nog toe te voegen) |
| Mini (wizard) vs volledige Quick-Scan | 300 | r per schaal, Bland-Altman | `mini_vs_full` in script |

**Acceptatie intern (conservatief, geïnspireerd op COTAN-toelichting selectie):**  
- Voor **matching met vacature-impact** streven α ≥ **.80** per schaal (Quick-Scan 5 items: realistisch .65–.75; bij <.70: items uitbreiden, niet alleen herformuleren).  
- COTAN noemt voor **hoge-stakes selectie** vaak α > .90 (Fundament/COTAN-documentatie); Lobsy is **geen** gecertificeerde selectietest — beleid moet dat expliciet maken.

**Data:** `export_anonymized_responses.sql` → CSV → `analyze_responses.py --csv …`.

---

## Fase 2 — Begripsvaliditeit

| Studie | Min. N | Design |
|--------|--------|--------|
| EFA/CFA Quick-Scan competentie (4–5 schalen) | **500** | bevestigende factoranalyse; fit CFI/TLI/RMSEA rapporteren |
| RIASEC Quick-Scan: 6 factoren | **500** | EFA; vergelijk met 6-dimensionele theorie |
| Convergent: parallel IPIP-20 (Big Five) | **150** | correlatie Lobsy-schalen ↔ IPIP (hypothese: Samenwerken↔A, Resultaat↔C, etc.) |
| Convergent: RIASEC short form (bijv. SDS-5 of IP) | **150** | correlatie per Holland-type |
| Schwartz values: korte PVQ-RR subset | **150** | correlatie drivers |
| Known-groups | **100+100** | beroepssector zelfrapport vs RIASEC-profiel (zwakke hypothese — voorzichtig interpreteren) |

**Output:** technisch rapport + bijstelling itemlijst.

---

## Fase 3 — Criteriumvaliditeit & fairness

| Studie | Min. N | Uitkomst |
|--------|--------|----------|
| Match% decielen vs **sollicitatie → hire** (met consent) | **200** hires + 400 no-hire | AUC, calibration; geen automatische reject op test |
| Match% vs **90-dagen behoud** (indien data) | **150** | correlatie / survival |
| DIF taal (nl vs en vs pl/ro/ar) | **200 per taalgroep** | Mantel-Haenszel of IRT-LR op itemniveau |
| DIF leeftijd / opleiding | **300** stratified | zelfde |
| Bias review vacature-inferentie | expert panel | audit `VacancyCompetencyProfile.cs` |

Fairness-addendum COTAN (2021): documenteer taal, toegankelijkheid, unproctored context (thuis invullen).

---

## Fase 4 — Normering

| Stap | Min. N | Opmerking |
|------|--------|-----------|
| Normsteekproef NL werkzoekenden | **800–1000** | stratificatie: leeftijd, opleiding, regio (Westland/Den Haag + landelijk) |
| Normtabellen per schaal (percentielen) | idem | scheiding ruwe score vs normscore UI |
| Hernormeringsinterval | — | COTAN: normen verouderen; plan elke **10–15 jaar** of na grote itemwijziging |

---

## Fase 5 — Diepteanalyse & AI

| Activiteit | N | Opmerking |
|------------|---|-----------|
| Betrouwbaarheid deep 150/200 (per domein 30+ items) | **200** compleet | apart exportfilter op `CandidateDeepAnalyses` |
| Menselijke review AI-rapport (diepteanalyse) | **50** cases | check hallucinaties vs scores |
| AI Act-documentatie | — | risicoclassificatie, mens-in-de-loop, logging |

---

## Roluniversiteit / COTAN-partner

1. Review constructdocument + itemlijst (Fase 0).  
2. Onafhankelijke analyse Fase 1–2 op geleverde CSV (blinded waar mogelijk).  
3. Co-auteurschap technisch rapport; optioneel COTAN-beoordelingsaanvraag **na** Fase 1–4 (partner beslist).

---

## Deliverables checklist

- [ ] Geanonimiseerde dataset + codebook  
- [ ] `analyze_responses.py` output op echte data (JSON)  
- [ ] Testhandleiding (afnemer + kandidaat)  
- [ ] Normtabellen (CSV)  
- [ ] DIF-rapport  
- [ ] Criteriumvaliditeit-rapport (match% vs hire)  
- [ ] Bijgewerkt `COTAN_BEOORDELING_LOBSY_TESTS.md` met empirische sectie (c)
